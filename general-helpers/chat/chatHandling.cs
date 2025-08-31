using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob;
using Bob.Database;
using Commands.Helpers;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

public static class ChatHandling
{
    public static async Task HandleMentionAsync(SocketMessage message)
    {
        string cleanedMessage = message.Content
            .Replace("<@705680059809398804>", "")
            .Trim();

        float[] embeddingArray = await OpenAI.GetEmbedding(cleanedMessage);
        var queryEmbedding = new Pgvector.Vector(embeddingArray);

        using var scope = Bot.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BobEntities>();

        // Detect if the query is temporal
        var (from, to) = DetectTemporalRange(cleanedMessage);

        // Hybrid retrieval (semantic + temporal if applicable)
        var relevantMemories = await dbContext.GetHybridMemoriesAsync(
            message.Author.Id.ToString(),
            queryEmbedding,
            from,
            to,
            semanticLimit: 5,
            temporalLimit: 5
        );

        // Build prompt
        var messages = new List<object>
{
    new
    {
        role = "system",
        content =
            "You are Bob, a helpful, friendly, and a little fancy Discord bot. " +
            "You have memory of past conversations. " +
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
            messages.Add(new { role = "user", content = mem.Content });
        }

        messages.Add(new { role = "user", content = cleanedMessage });

        // Get response
        string response = await OpenAI.PostToOpenAI(messages);

        // Store new memory
        await dbContext.StoreMemoryAsync(
            message.Author.Id.ToString(),
            cleanedMessage,
            queryEmbedding
        );

        await message.Channel.SendMessageAsync(response);
    }

    private static (DateTime? from, DateTime? to) DetectTemporalRange(string query)
    {
        var now = DateTime.UtcNow;

        if (query.Contains("last thing", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("last message", StringComparison.OrdinalIgnoreCase))
        {
            return (now.AddMinutes(-10), now); // last 10 minutes
        }

        if (query.Contains("yesterday", StringComparison.OrdinalIgnoreCase))
        {
            var yesterday = now.Date.AddDays(-1);
            return (yesterday, yesterday.AddDays(1));
        }

        if (query.Contains("last week", StringComparison.OrdinalIgnoreCase))
        {
            var start = now.Date.AddDays(-7);
            return (start, now);
        }

        if (query.Contains("last year", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateTime(now.Year - 1, 1, 1);
            var end = new DateTime(now.Year - 1, 12, 31, 23, 59, 59);
            return (start, end);
        }

        // Default: no temporal filter
        return (null, null);
    }
}