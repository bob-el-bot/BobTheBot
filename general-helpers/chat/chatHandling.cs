using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bob;
using Bob.Database;
using Commands.Helpers;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using BobTheBot.RateLimits;

namespace BobTheBot.Chat;

public static partial class ChatHandling
{
    private static readonly ConcurrentDictionary<ulong, DiscordWebhookClient> WebhookCache = new();
    private static readonly HashSet<ulong> NoWebhookChannels = [];

    public static async Task HandleMentionAsync(SocketMessage message)
    {
        string cleanedMessage = message.Content
            .Replace("<@705680059809398804>", "")
            .Trim();

        float[] embeddingArray = await OpenAI.GetEmbedding(cleanedMessage);
        var queryEmbedding = new Pgvector.Vector(embeddingArray);

        using var scope = Bot.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BobEntities>();

        var (from, to) = DetectTemporalRange(cleanedMessage);

        var relevantMemories = await dbContext.GetHybridMemoriesAsync(
            message.Author.Id.ToString(),
            queryEmbedding,
            from,
            to,
            semanticLimit: 5,
            temporalLimit: 5
        );

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content =
                    "You are Bob, a helpful, friendly, and a little fancy Discord bot. " +
                    "You have memory of past conversations. " +
                    "You have access to the user’s past conversation history and long-term memory. " +
                    "Treat each new message as part of an ongoing conversation **if it makes sense**. " +
                    "If the user’s message is unrelated to past context, answer it as a fresh question. " +
                    "When asked about past interactions, use the provided memory context to recall details naturally. " +
                    "Always respond in a way that feels continuous and conversational, like Jarvis from Iron Man.\n " +
                    "Discord messages may contain Markdown-style formatting:\n" +
                    "- **bold** text is wrapped in double asterisks\n" +
                    "- *italic* text is wrapped in single asterisks or underscores\n" +
                    "- `inline code` is wrapped in backticks\n" +
                    "- ```code blocks``` are wrapped in triple backticks\n" +
                    "- > blockquotes start with a greater-than sign\n" +
                    "- <@123456789> are user mentions (treat them as the user's name if known)\n" +
                    "- :emoji: are emojis\n\n" +
                    "When responding, preserve the formatting so it renders correctly in Discord."
            }
        };

        foreach (var mem in relevantMemories.OrderBy(m => m.CreatedAt))
        {
            messages.Add(new { role = "system", content = $"[Memory from {mem.CreatedAt:u}] {mem.UserMessage}" });
            if (!string.IsNullOrEmpty(mem.BotResponse))
                messages.Add(new { role = "system", content = $"[Bob’s reply] {mem.BotResponse}" });
        }

        messages.Add(new { role = "user", content = cleanedMessage });

        string response = await OpenAI.PostToOpenAI(messages);

        await dbContext.StoreMemoryAsync(
            message.Author.Id.ToString(),
            cleanedMessage,
            response,
            queryEmbedding
        );

        await SafeSendAsync(message.Channel, response);
    }

    private static async Task SafeSendAsync(ISocketMessageChannel channel, string response)
    {
        if (response.Length <= 2000)
        {
            await SendSingleAsync(channel, null, response);
        }
        else if (response.Length <= 8000)
        {
            foreach (var chunk in SplitDiscordMessage(response))
            {
                await SendSingleAsync(channel, null, chunk);
            }
        }
        else
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response));

            DiscordWebhookClient webhookClient = null;
            if (RateLimitHandling.IsUnderload())
            {
                webhookClient = await GetOrCreateWebhookClientAsync(channel);
            }

            if (webhookClient != null)
            {
                await webhookClient.SendFileAsync(stream, "response.txt",
                    text: "This was too long for Discord, here’s the full text:",
                    username: "BobTheBot",
                    avatarUrl: Bot.Client.CurrentUser.GetAvatarUrl());
            }
            else
            {
                await channel.SendFileAsync(stream, "response.txt",
                    text: "This was too long for Discord, here’s the full text:");
            }
        }
    }

    private static async Task SendSingleAsync(ISocketMessageChannel channel, DiscordWebhookClient? webhookClient, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (RateLimitHandling.IsUnderload() && webhookClient == null)
        {
            webhookClient = await GetOrCreateWebhookClientAsync(channel);
        }

        if (webhookClient != null)
        {
            await webhookClient.SendMessageAsync(
                text: text,
                username: "BobTheBot",
                avatarUrl: Bot.Client.CurrentUser.GetAvatarUrl());
        }
        else
        {
            var options = new RequestOptions { RatelimitCallback = RateLimitHandling.GlobalRatelimitCallback };
            await channel.SendMessageAsync(text, options: options);
        }
    }

    private static async Task<DiscordWebhookClient> GetOrCreateWebhookClientAsync(ISocketMessageChannel channel)
    {
        if (WebhookCache.TryGetValue(channel.Id, out var cached))
            return cached;

        if (channel is not SocketTextChannel textChannel)
            return null;

        try
        {
            var webhooks = await textChannel.GetWebhooksAsync();
            var webhook = webhooks.FirstOrDefault(w => w.Name == "BobWebhook")
                          ?? await textChannel.CreateWebhookAsync("BobWebhook");

            var client = new DiscordWebhookClient(webhook);
            WebhookCache[channel.Id] = client;
            return client;
        }
        catch
        {
            NoWebhookChannels.Add(channel.Id);
            return null;
        }
    }

    private static (DateTime? from, DateTime? to) DetectTemporalRange(string query)
    {
        var now = DateTime.UtcNow;

        if (query.Contains("last thing", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("last message", StringComparison.OrdinalIgnoreCase))
            return (now.AddMinutes(-10), now);

        if (query.Contains("yesterday", StringComparison.OrdinalIgnoreCase))
        {
            var yesterday = now.Date.AddDays(-1);
            return (yesterday, yesterday.AddDays(1));
        }

        if (query.Contains("last week", StringComparison.OrdinalIgnoreCase))
            return (now.Date.AddDays(-7), now);

        if (query.Contains("last year", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateTime(now.Year - 1, 1, 1);
            var end = new DateTime(now.Year - 1, 12, 31, 23, 59, 59);
            return (start, end);
        }

        return (null, null);
    }

    public static IEnumerable<string> SplitDiscordMessage(string text, int chunkSize = 2000)
    {
        var result = new List<string>();
        var lines = text.Split('\n');
        var current = new List<string>();
        int currentLength = 0;

        bool inCodeBlock = false;
        string codeBlockLang = null;

        foreach (var line in lines)
        {
            int lineLength = line.Length + 1;
            if (currentLength + lineLength > chunkSize)
            {
                if (inCodeBlock)
                {
                    string closing = "```";
                    int closingLen = closing.Length + 1;

                    if (currentLength + closingLen <= chunkSize)
                    {
                        current.Add(closing);
                        currentLength += closingLen;
                    }
                    else
                    {
                        while (current.Count > 0 && currentLength + closingLen > chunkSize)
                        {
                            string removed = current[^1];
                            current.RemoveAt(current.Count - 1);
                            currentLength -= removed.Length + 1;
                            lines = [removed, .. lines.SkipWhile(l => l != line)];
                            break;
                        }
                        current.Add(closing);
                        currentLength += closingLen;
                    }
                }

                result.Add(string.Join("\n", current));

                current.Clear();
                currentLength = 0;

                if (inCodeBlock)
                {
                    string reopen = codeBlockLang != null ? $"```{codeBlockLang}" : "```";
                    current.Add(reopen);
                    currentLength += reopen.Length + 1;
                }
            }

            current.Add(line);
            currentLength += lineLength;

            if (line.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    inCodeBlock = true;
                    codeBlockLang = line.Length > 3 ? line.Substring(3).Trim() : null;
                }
                else
                {
                    inCodeBlock = false;
                    codeBlockLang = null;
                }
            }
        }
        if (current.Count > 0)
        {
            if (inCodeBlock && !current[^1].StartsWith("```"))
            {
                current.Add("```");
            }
            result.Add(string.Join("\n", current));
        }

        return result;
    }

    [GeneratedRegex(@"^```.*$", RegexOptions.Multiline)]
    public static partial Regex TripleBacktick();
}