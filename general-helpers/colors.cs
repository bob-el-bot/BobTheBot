using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace ColorMethods
{
    public static class Colors
    {
        /// <summary>
        /// Dictionary storing color names and their corresponding hex values.
        /// </summary>
        private static readonly Dictionary<string, string> colors = new()
        { 
            { "red", "ED4245" }, 
            { "orange", "FFA500" }, 
            { "yellow", "FEE75C" }, 
            { "green", "57F287" }, 
            { "black", "23272A" }, 
            { "pink", "EB459E" }, 
            { "blue", "3498DB" }, 
            { "grey", "95A5A6" }, 
            { "gray", "95A5A6" }, 
            { "white", "FFFFFF" }, 
            { "purple", "8D52FD" } 
        };

        /// <summary>
        /// Tries to get a <see cref="Color"/> object from a string input.
        /// </summary>
        /// <param name="input">The input string which can be a color name or a hex value.</param>
        /// <returns>A <see cref="Color"/> object representing the color, or black if the conversion fails.</returns>
        public static Color TryGetColor(string input)
        {
            Color finalColor;

            try
            {
                // Convert the input string to a hexadecimal string and then to a Color object
                finalColor = new Color(Convert.ToUInt32(StringToHex(input), 16));
            }
            catch
            {
                // If conversion fails, return a default Color (black)
                finalColor = new Color(0);
            }

            return finalColor;
        }

        /// <summary>
        /// Converts a string to a hexadecimal color string.
        /// </summary>
        /// <param name="input">The input string which can be a color name or a hex value.</param>
        /// <returns>The corresponding hex string if valid; otherwise, null.</returns>
        private static string StringToHex(string input)
        {
            // Try to convert a named color to hex, or a hex string to hex
            return WordToHex(input) ?? HexStringToHex(input) ?? null;
        }

        /// <summary>
        /// Converts a hex string (e.g., "FFA500") to a hexadecimal string without '#'.
        /// </summary>
        /// <param name="input">The hex string input.</param>
        /// <returns>The cleaned hex string if valid; otherwise, null.</returns>
        private static string HexStringToHex(string input)
        {
            // Remove the '#' character if present
            if (input.StartsWith("#"))
            {
                input = input[1..];
            }

            // Ensure the string has a valid length
            if (input.Length % 2 != 0 || input.Length > 6)
            {
                return null;
            }

            StringBuilder hexBuilder = new();

            for (int i = 0; i < input.Length; i += 2)
            {
                string hexPair = input.Substring(i, 2);
                hexBuilder.Append(hexPair);
            }

            return hexBuilder.ToString();
        }

        /// <summary>
        /// Converts a named color string to a hexadecimal color string.
        /// </summary>
        /// <param name="input">The color name input.</param>
        /// <returns>The corresponding hex string if a match is found; otherwise, null.</returns>
        private static string WordToHex(string input)
        {
            string lowerInput = input.ToLowerInvariant();

            if (colors.TryGetValue(lowerInput, out string hexValue))
            {
                return hexValue;
            }

            return null;
        }

        /// <summary>
        /// Gets a string listing all the supported color names.
        /// </summary>
        /// <returns>A string listing all supported color names.</returns>
        public static string GetSupportedColorsString()
        {
            StringBuilder colorString = new();

            int count = 0;
            foreach (var color in colors)
            {
                colorString.Append(color.Key);
                if (++count < colors.Count - 1)
                {
                    colorString.Append(", ");
                }
                else if (count == colors.Count - 1)
                {
                    colorString.Append(", and ");
                }
            }

            return colorString.ToString();
        }
    }
}
