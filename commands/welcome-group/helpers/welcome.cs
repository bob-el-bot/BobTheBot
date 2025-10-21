using System;
using System.Text;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    public static class Welcome
    {
        private static readonly string[] Greetings =
        [
            "Welcome @!",
            "Who invited this guy? Just kidding, welcome @!",
            "Happy to have you here @!",
            "Looking good @!",
            "@ is here, everybody play cool.",
            "@ has entered the building.",
            "Never fear, @ is here.",
            "A wild @ appeared.",
            "Everybody get loud because @ is here!",
            "@ has graced us with their presence.",
            "@ is not the droid we're looking for. Also... they are here!",
            "Stand down, it's just @.",
            "Make way for @!",
            "@ is here, in the flesh!",
            "Open the gate for @!",
            "Prepare yourselves, @ has joined.",
            "Look what the cat dragged in, @ is here.",
            "Speak of the devil, @ joined.",
            "Better late than never, @ joined.",
            "@ has revealed themselves from the shadows."
        ];

        private static readonly Random Random = new();

        /// <summary>
        /// Prepares the final welcome message for a user by either formatting a custom
        /// message (if provided) or selecting a random predefined one.
        /// </summary>
        /// <param name="customMessage">
        /// The custom welcome message template. Use '@' as a placeholder to insert the user's mention.
        /// If null or whitespace, a randomized greeting will be used instead.
        /// </param>
        /// <param name="mention">The Discord mention string for the new user.</param>
        /// <returns>
        /// A complete welcome message string with the user's mention inserted.
        /// </returns>
        public static string PrepareWelcomeMessage(string customMessage, string mention)
        {
            var messageText = !string.IsNullOrWhiteSpace(customMessage)
                ? FormatCustomMessage(customMessage ?? string.Empty, mention)
                : GetRandomMessage(mention);

            return messageText;
        }

        /// <summary>
        /// Formats a custom message by replacing occurrences of '@' with a specified mention string.
        /// </summary>
        /// <param name="message">The original message containing '@' placeholders.</param>
        /// <param name="mention">The string to replace the '@' placeholders.</param>
        /// <returns>A string with '@' placeholders replaced by the mention string.</returns>
        public static string FormatCustomMessage(string message, string mention)
        {
            StringBuilder finalText = new();

            foreach (char c in message)
            {
                // If character is '@', append mention string
                if (c == '@')
                {
                    finalText.Append(mention);
                }
                else // Otherwise, append original character
                {
                    finalText.Append(c);
                }
            }

            return finalText.ToString();
        }

        /// <summary>
        /// Generates a random welcome message with a mention string included.
        /// </summary>
        /// <param name="mention">The string representing the mention.</param>
        /// <returns>A randomly selected welcome message containing the mention.</returns>=
        private static string GetRandomMessage(string mention)
        {
            var template = Greetings[Random.Next(Greetings.Length)];
            return template.Replace("@", mention);
        }
    }
}