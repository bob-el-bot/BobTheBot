using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static Bob.ApiInteractions.Interface;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// Provides quotes for /random quote. Quotes come from a community mirror of the quotable API,
    /// falling back to a bundled copy of the same MIT licensed dataset (https://github.com/quotable-io/data).
    /// </summary>
    public static class RandomQuote
    {
        public record Quote(string Content, string Author);

        private record LocalQuote(string Content, string Author, string[] Tags);

        private record LocalQuoteFile(List<LocalQuote> Quotes);

        private const string ApiUrl = "https://api.quotable.kurokeita.dev/api/quotes/random";

        // Discord requires a response within 3 seconds, so leave time for the fallback.
        private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// All valid prompts grouped by category (used by /quote-prompts). Each prompt is a slug of a quotable tag name.
        /// </summary>
        public static readonly (string Category, string[] Prompts)[] PromptCategories =
        [
            ("Life & Philosophy", ["age", "change", "character", "courage", "failure", "faith", "gratitude", "happiness", "honor", "inspirational", "life", "love", "pain", "perseverance", "philosophy", "self", "self-help", "spirituality", "truth", "virtue", "wisdom"]),
            ("Work & Success", ["business", "competition", "creativity", "ethics", "genius", "imagination", "leadership", "motivational", "opportunity", "success", "work"]),
            ("Society & Politics", ["conservative", "education", "freedom", "politics", "power-quotes", "religion", "social-justice", "society", "tolerance", "war"]),
            ("Health & Wellness", ["health", "sports", "wellness"]),
            ("Science & Technology", ["mathematics", "science", "technology"]),
            ("Famous & Historical", ["famous-quotes", "film", "history", "literature"]),
            ("Emotions & Humor", ["humorous", "sadness", "stupidity", "weakness"]),
            ("Miscellaneous", ["family", "friendship", "future", "generosity", "knowledge", "nature", "time"]),
        ];

        /// <summary>
        /// All valid prompts in alphabetical order.
        /// </summary>
        public static readonly string[] Prompts = [.. PromptCategories.SelectMany(c => c.Prompts).Order()];

        private static readonly Lazy<List<LocalQuote>> LocalQuotes = new(LoadLocalQuotes);

        private static readonly Random random = new();

        /// <summary>
        /// Normalizes a user provided prompt (e.g. "Famous Quotes" -> "famous-quotes").
        /// </summary>
        public static string NormalizePrompt(string prompt)
        {
            return (prompt ?? "").Trim().ToLowerInvariant().Replace(' ', '-');
        }

        /// <summary>
        /// Checks whether a normalized prompt is valid. An empty prompt means any quote.
        /// </summary>
        public static bool IsValidPrompt(string prompt)
        {
            return prompt == "" || Prompts.Contains(prompt);
        }

        /// <summary>
        /// Gets a random quote for a valid, normalized prompt. Uses the API when it responds in time, otherwise the bundled dataset.
        /// </summary>
        /// <param name="prompt">A normalized prompt from <see cref="Prompts"/>, or an empty string for any quote.</param>
        /// <returns>A quote, or null if no quote matches the prompt.</returns>
        public static async Task<Quote> GetQuoteAsync(string prompt)
        {
            try
            {
                using CancellationTokenSource cts = new(ApiTimeout);

                // The API filters on the tag's display name ("Famous Quotes"), which is case sensitive.
                string url = prompt == "" ? ApiUrl : $"{ApiUrl}?tags={Uri.EscapeDataString(GetTagName(prompt))}";
                string content = await GetFromAPI(url, AcceptTypes.application_json, cancellationToken: cts.Token);

                // No matches is returned as {}
                var quote = JsonNode.Parse(content)?["quote"];
                var quoteContent = quote?["content"]?.ToString();
                var author = quote?["author"]?["name"]?.ToString();

                if (!string.IsNullOrWhiteSpace(quoteContent) && !string.IsNullOrWhiteSpace(author))
                {
                    return new Quote(quoteContent, author);
                }
            }
            catch (Exception ex)
            {
                // Any failure (network, timeout, unexpected response) falls back to the bundled quotes.
                Console.WriteLine($"An error occurred while fetching the quote, using bundled quotes: {ex.Message}");
            }

            return GetLocalQuote(prompt);
        }

        private static Quote GetLocalQuote(string prompt)
        {
            var matches = prompt == ""
                ? LocalQuotes.Value
                : LocalQuotes.Value.Where(q => q.Tags.Any(t => NormalizePrompt(t) == prompt)).ToList();

            if (matches.Count == 0)
            {
                return null;
            }

            var quote = matches[random.Next(matches.Count)];
            return new Quote(quote.Content, quote.Author);
        }

        private static string GetTagName(string prompt)
        {
            // Prompts are slugs of the tag names, so recover the name from the bundled data.
            return LocalQuotes.Value
                .SelectMany(q => q.Tags)
                .FirstOrDefault(t => NormalizePrompt(t) == prompt) ?? prompt;
        }

        private static List<LocalQuote> LoadLocalQuotes()
        {
            using var stream = typeof(RandomQuote).Assembly.GetManifestResourceStream("quotes.json");
            var file = JsonSerializer.Deserialize<LocalQuoteFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return file.Quotes;
        }
    }
}
