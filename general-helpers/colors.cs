using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace ColorMethods
{
    public static class Colors
    {
        private static readonly Dictionary<string, string> colors = new() { { "red", "ED4245" }, { "orange", "FFA500" }, { "yellow", "FEE75C" }, { "green", "57F287" }, { "black", "23272A" }, { "pink", "EB459E" }, { "blue", "3498DB" }, { "grey", "95A5A6" }, { "gray", "95A5A6" }, { "white", "FFFFFF" }, { "purple", "8D52FD" } };

        public static Color TryGetColor(string input)
        {
            Color finalColor;

            try
            {
                finalColor = Convert.ToUInt32(StringToHex(input), 16);
            }
            catch
            {
                finalColor = 0;
            }

            return finalColor;
        }

        private static string StringToHex(string input)
        {
            return WordToHex(input) ?? HexStringToHex(input) ?? null;
        }

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

        private static string WordToHex(string input)
        {
            string lowerInput = input.ToLowerInvariant();

            string match = null;

            foreach (var color in colors)
            {
                if (lowerInput.Contains(color.Key))
                {
                    match = color.Value;
                    break;
                }
            }

            return match;
        }

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