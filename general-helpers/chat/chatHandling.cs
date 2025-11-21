using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bob;
using Bob.Database;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using BobTheBot.RateLimits;
using BobTheBot.Chat.TemporalHandling;
using BobTheBot.Chat.AiServiceHandling;
using BobTheBot.Chat.Routing;
using System;

namespace BobTheBot.Chat;

public static partial class ChatHandling
{
    private static readonly ConcurrentDictionary<ulong, DiscordWebhookClient> WebhookCache = new();
    private static readonly HashSet<ulong> NoWebhookChannels = [];

    public static async Task HandleMentionAsync(SocketMessage message)
    {
        var channel = message.Channel;
        await channel.TriggerTypingAsync();

        string cleanedMessage = message.Content
            .Replace("<@705680059809398804>", "")
            .Trim();

        float[] embeddingArray = await OpenAI.GetEmbedding(cleanedMessage);
        var queryEmbedding = new Pgvector.Vector(embeddingArray);

        var imageAttachments = message.Attachments
            .Where(static a => Regex.IsMatch(a.Filename, @"\.(png|jpe?g|gif|webp|bmp|tiff)$",
                RegexOptions.IgnoreCase))
            .Select(a => new ImageAttachment
            {
                Url = a.Url,
                MimeType = a.ContentType ?? "image/png",
                FileName = a.Filename
            })
            .ToList();

        using var scope = Bot.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BobEntities>();

        var temporalResult = TemporalRangeDetector.DetectTemporalRange(cleanedMessage);
        var from = temporalResult.From;
        var to = temporalResult.To;

        var messages = new List<object>
    {
        new
        {
            role = "system",
            content =
                "You are Bob, a helpful, friendly, and a little fancy Discord bot. " +
                "You have memory of past conversations and access to the user’s past conversation history and long-term memory. " +
                "Treat each new message as part of an ongoing conversation **if it makes sense**. " +
                "If the user’s message is unrelated to past context, answer it as a fresh question. " +
                "When asked about past interactions, use the provided memory context to recall details naturally. " +
                "If no past memories exist for a requested time period, clearly say so instead of assuming continuity. " +
                "Always respond in a way that feels continuous and conversational, like Jarvis from Iron Man.\n\n" +
                "Discord messages may contain Markdown formatting:\n" +
                "- **bold** -> double asterisks\n" +
                "- *italic* -> single asterisks or underscores\n" +
                "- `inline code` -> backticks\n" +
                "- ```code blocks``` -> triple backticks\n" +
                "- > blockquotes -> greater-than sign\n" +
                "- <@123456789> are user mentions\n" +
                "- :emoji: are emojis\n\n" +
                "IMPORTANT: If no memories exist for a requested time, you must directly state that no past conversations are available. " +
                "Do not pretend continuity if there is no memory data."
        }
    };

        // --- Gather relevant hybrid memories ---
        var hybridResult = await dbContext.GetHybridMemoriesAsync(
            message.Author.Id.ToString(),
            queryEmbedding,
            temporalResult.From,
            temporalResult.To,
            semanticLimit: 5,
            temporalLimit: 5
        );

        // Consistent system note injection for temporal recall
        if (temporalResult.Mode is TemporalMode.LastThing or TemporalMode.LastTime)
        {
            var userId = message.Author.Id.ToString();
            var lastMemories = await dbContext.GetRecentConversationAsync(
                userId,
                limit: temporalResult.Mode == TemporalMode.LastThing ? 1 : 5
            );

            Console.WriteLine(
                $"hybridResult.Total: {hybridResult.Memories.Count}, lastMemories: {lastMemories.Count}");

            if (lastMemories.Count == 0)
            {
                messages.Add(new
                {
                    role = "system",
                    content =
                        $"[Memory system note] No previous conversations were found for {temporalResult.Mode}. " +
                        "Politely explain there are no saved conversations for this timeframe."
                });
            }
            else
            {
                var ts = lastMemories.Last().CreatedAt.ToLocalTime();

                // single clear intro line, like the original version
                messages.Add(new
                {
                    role = "system",
                    content =
                        $"We last talked on {ts:f}. Here is the record of that discussion."
                });

                // each pair exactly like the working build
                foreach (var mem in lastMemories.OrderBy(m => m.CreatedAt))
                {
                    messages.Add(new
                    {
                        role = "system",
                        content = $"[Memory from {mem.CreatedAt:u}] {mem.UserMessage}"
                    });

                    if (!string.IsNullOrWhiteSpace(mem.BotResponse))
                    {
                        messages.Add(new
                        {
                            role = "system",
                            content = $"[Bob’s reply] {mem.BotResponse}"
                        });
                    }
                }
            }
        }

        // Generic hybrid memories + system notes (same style)
        if (temporalResult.Mode != TemporalMode.None && hybridResult.TemporalCount == 0)
        {
            messages.Add(new
            {
                role = "system",
                content = $"[Memory system note] No past memories were found for {temporalResult.Mode}. " +
                          "Do NOT claim a conversation happened. Politely explain there are no saved conversations in that timeframe."
            });
        }
        else
        {
            foreach (var mem in hybridResult.Memories.OrderBy(m => m.CreatedAt))
            {
                messages.Add(new
                {
                    role = "system",
                    content = $"[Memory from {mem.CreatedAt:u}] {mem.UserMessage}"
                });

                if (!string.IsNullOrEmpty(mem.BotResponse))
                {
                    messages.Add(new
                    {
                        role = "system",
                        content = $"[Bob’s reply] {mem.BotResponse}"
                    });
                }
            }
        }

        // Add current user query
        messages.Add(new { role = "user", content = cleanedMessage });

        // Send through model
        string response = await ModelRouter.GetResponseAsync(messages, imageAttachments);

        // Store image summaries, if any
        if (imageAttachments.Count > 0 &&
            !string.IsNullOrWhiteSpace(response) &&
            !response.Contains("too large or unreadable", StringComparison.OrdinalIgnoreCase))
        {
            string imageSummary = response.Trim();
            string imageLabel = "[Image Upload: " +
                string.Join(", ", imageAttachments.Select(i => i.FileName)) +
                "]";

            float[] embedding = await OpenAI.GetEmbedding(imageSummary);
            var vec = new Pgvector.Vector(embedding);

            await dbContext.StoreMemoryAsync(
                message.Author.Id.ToString(),
                imageLabel,
                imageSummary,
                vec
            );
        }

        // Store the full interaction
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

    private static async Task SendSingleAsync(ISocketMessageChannel channel, DiscordWebhookClient webhookClient, string text)
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