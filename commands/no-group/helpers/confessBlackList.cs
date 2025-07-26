using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Cinsor;
using Cinsor.Models;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// Represents the result of filtering a message for banned words.
    /// </summary>
    public class FilterResult
    {
        /// <summary>
        /// Gets or sets the list of words that matched the blacklist.
        /// </summary>
        public List<string> BlacklistMatches { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of words that need to be marked as spoilers.
        /// </summary>
        public List<string> WordsToCensor { get; set; } = [];
    }

    /// <summary>
    /// Provides methods for filtering messages for banned words and other criteria.
    /// </summary>
    public static partial class ConfessFiltering
    {
        private static readonly CensorEngine cinsorEngine = new();
        private static readonly CensorOptions cinsorSettings = new()
        {
            Preset = CensorPreset.ObfuscationSafe,
            //CensorBehavior = (text, index, word) => $"||{word}||",
        };

        static ConfessFiltering()
        {
            cinsorEngine.AddPatterns(CensoredWordSets.WordsToCensor);
            cinsorEngine.BuildFailureLinks();
        }

        /// <summary>
        /// Warning message to be appended when a link is detected in the message.
        /// </summary>
        public static readonly string linkWarningMessage = "⚠️ **Click links with caution.**";
        /// <summary>
        /// Notification message prefix to be added to anonymous messages.
        /// </summary>
        public static readonly string notificationMessage = "**You were sent a message:**";

        [GeneratedRegex(@"(http|https|ftp|ftps):\/\/([\w\p{L}\p{N}\.-]+)\.([\p{L}\p{N}]{2,})([^\s]*)?", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex GetUrlRegex();

        private static readonly Regex UrlRegex = GetUrlRegex();

        /// <summary>
        /// Determines whether the provided string is considered "blank."
        /// </summary>
        /// <param name="message">The string to check.</param>
        /// <returns>
        /// <c>true</c> if the string is null, empty, or consists solely of whitespace characters,
        /// including various Unicode space characters, invisible formatting, or control characters;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBlank(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return true;
            }

            foreach (char ch in message)
            {
                var category = char.GetUnicodeCategory(ch);

                if (!char.IsWhiteSpace(ch) && ch != '\0' &&
                    category != UnicodeCategory.SpaceSeparator &&
                    category != UnicodeCategory.Format &&
                    category != UnicodeCategory.Control &&
                    category != UnicodeCategory.OtherNotAssigned)
                {
                    return false; // Found a non-blank character, early exit
                }
            }

            return true; // All characters are blank or non-visible
        }

        /// <summary>
        /// Determines whether the specified message contains a link.
        /// </summary>
        /// <param name="message">The message to check.</param>
        /// <returns><c>true</c> if the message contains a link; otherwise, <c>false</c>.</returns>
        public static bool ContainsLink(string message)
        {
            return !string.IsNullOrWhiteSpace(message) && UrlRegex.IsMatch(message);
        }

        /// <summary>
        /// Checks a message for banned words and returns the results.
        /// </summary>
        /// <param name="message">The message to check.</param>
        /// <returns>A <see cref="CensorResult"/> containing the lists of blacklisted words and words to censor.</returns>
        public static CensorResult ContainsBannedWords(string message)
        {
            // List<string> foundWords = [];
            // List<string> wordsToMarkSpoilers = [];

            var cinsorResult = cinsorEngine.Censor(message, cinsorSettings);

            foreach (var match in cinsorResult.Matches)
            {
                // Check if the matched word is in the banned words list
                if (CensoredWordSets.WordsToCensor.Contains(match.Word))
                {
                    Console.WriteLine(match.Word);
                }
            }

            // foreach (string word in message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            // {
            //     if (CensoredWordSets.BannedWords.Contains(word))
            //     {
            //         if (CensoredWordSets.WordsToCensor.Contains(word))
            //         {
            //             wordsToMarkSpoilers.Add(word);
            //         }
            //         else
            //         {
            //             foundWords.Add(word);
            //         }
            //     }
            // }

            return cinsorResult;
        }

        private static string NormalizeObfuscatedSingleWords(string input, HashSet<char> obfuscationChars)
        {
            var tokens = input
                .Split(obfuscationChars.ToArray(), StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine("Tokens: " + string.Join(", ", tokens));

            if (tokens.All(t => t.Length == 1 && char.IsLetter(t[0])))
                return string.Concat(tokens).ToLowerInvariant();

            return input.ToLowerInvariant();
        }

        /// <summary>
        /// Formats a list of words into a comma-separated string.
        /// </summary>
        /// <param name="words">The list of words to format.</param>
        /// <returns>A comma-separated string of words.</returns>
        public static string FormatBannedWords(List<string> words)
        {
            if (words == null || words.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", words);
        }

        /// <summary>
        /// Marks specified words in a message as spoilers.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="wordsToCensor">The list of words to mark as spoilers.</param>
        /// <returns>The message with specified words marked as spoilers.</returns>
        public static string MarkSpoilers(string message, List<string> wordsToCensor)
        {
            foreach (string word in wordsToCensor)
            {
                string escapedWord = Regex.Escape(word);
                string pattern = $@"\b({escapedWord})\b";
                message = Regex.Replace(message, pattern, "||$1||", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            return message;
        }
    }
}
