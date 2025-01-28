using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using SkiaSharp;

namespace Commands.Helpers
{
    public static class Welcome
    {
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
        /// <returns>A randomly selected welcome message containing the mention.</returns>
        public static string GetRandomMessage(string mention)
        {
            // Get random greeting
            Random random = new();
            string[] greetings = [$"Welcome {mention}!", $"Who invited this guy? Just kidding, welcome {mention}!", $"Happy to have you here {mention}!", $"Looking good {mention}!", $"{mention} is here, everybody play cool.", $"{mention} has entered the building.", $"Never fear, {mention} is here.", $"A wild {mention} appeared.", $"Everybody get loud because {mention} is here!", $"{mention} has graced us with their presence.", $"{mention} is not the droid we're looking for. Also... they are here!", $"Stand down, it's just {mention}.", $"Make way for {mention}!", $"{mention} is here, in the flesh!", $"Open the gate for {mention}!", $"Prepare yourselves, {mention} has joined.", $"Look what the cat dragged in, {mention} is here.", $"Speak of the devil, {mention} joined.", $"Better late than never, {mention} joined.", $"{mention} has revealed themselves from the shadows."];
            return greetings[random.Next(0, greetings.Length)];
        }

        /// <summary>
        /// Converts an image to WebP format.
        /// </summary>
        /// <param name="imageData">The byte array of the input image.</param>
        /// <param name="quality">Compression quality (0-100).</param>
        /// <returns>Byte array of the WebP image.</returns>
        public static byte[] ConvertToWebP(byte[] imageData, int quality = 80)
        {
            using var inputStream = new SKMemoryStream(imageData);
            using var bitmap = SKBitmap.Decode(inputStream) ?? throw new InvalidOperationException("Invalid image format");
            using var webpStream = new SKDynamicMemoryWStream();
            bitmap.Encode(webpStream, SKEncodedImageFormat.Webp, quality);

            return webpStream.DetachAsData().ToArray();
        }
    }
}