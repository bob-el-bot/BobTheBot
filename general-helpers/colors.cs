using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Bob.ColorMethods
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
            { "purple", "8D52FD" },
            { "cyan", "00FFFF" },
            { "magenta", "FF00FF" },
            { "brown", "996633" },
            { "lime", "CCFF66" },
            { "teal", "008080" },
            { "navy", "000080" },
            { "maroon", "800000" },
            { "olive", "808000" },
            { "turquoise", "40E0D0" },
            { "gold", "FFCC00" },
            { "silver", "C0C0C0" }
        };

        /// <summary>
        /// Tries to get a <see cref="Color"/> object from a string input.
        /// </summary>
        /// <param name="input">The input string which can be a color name, hex value, or RGB value.</param>
        /// <returns>A <see cref="Color?"/> object representing the color, or null if the conversion fails.</returns>
        public static Color? TryGetColor(string input)
        {
            Color? finalColor;

            try
            {
                // Convert the input string to a hexadecimal string and then to a Color object
                string hexValue = StringToHex(input) ?? RgbToHex(input);

                if (hexValue != null)
                {
                    finalColor = new Color(Convert.ToUInt32(hexValue, 16));
                }
                else
                {
                    finalColor = null;
                }
            }
            catch
            {
                finalColor = null;
            }

            return finalColor;
        }

        /// <summary>
        /// Converts a string to a hexadecimal color string.
        /// </summary>
        /// <param name="input">The input string which can be a color name, hex value, or RGB value.</param>
        /// <returns>The corresponding hex string if valid; otherwise, null.</returns>
        private static string StringToHex(string input)
        {
            return WordToHex(input) ?? HexStringToHex(input) ?? null;
        }

        /// <summary>
        /// Converts RGB string (e.g., "255,0,0") to a hexadecimal string.
        /// </summary>
        /// <param name="input">The RGB string input.</param>
        /// <returns>The corresponding hex string if valid; otherwise, null.</returns>
        private static string RgbToHex(string input)
        {
            var rgbComponents = input.Split(',');

            if (rgbComponents.Length == 3 &&
                int.TryParse(rgbComponents[0], out int r) &&
                int.TryParse(rgbComponents[1], out int g) &&
                int.TryParse(rgbComponents[2], out int b) &&
                r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
            {
                return $"{r:X2}{g:X2}{b:X2}";
            }

            return null;
        }

        /// <summary>
        /// Converts a hex string (e.g., "FFA500") to a hexadecimal string without '#' or invalid formats.
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
            if (input.Length != 6) // Ensure it's exactly 6 characters for RGB hex
            {
                return null;
            }

            return input.ToUpper(); // return in uppercase for consistency
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
