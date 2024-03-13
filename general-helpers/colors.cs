using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ColorMethods
{
    public static class Colors
    {
        public static string StringToHex(string input)
        {
            return WordToHex(input) ?? HexStringToHex(input);
        }

        private static string HexStringToHex(string input)
        {
            // Remove the '#' character if present
            if (input.StartsWith("#"))
            {
                input = input[1..];
            }

            // Ensure the string has a valid length
            if (input.Length % 2 != 0)
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
            Dictionary<string, string> colors = new() { { "red", "ED4245" }, { "orange", "FFA500" }, { "yellow", "FEE75C" }, { "green", "57F287" }, { "black", "23272A" }, { "pink", "EB459E" }, { "blue", "3498DB" }, { "grey", "95A5A6" }, { "gray", "95A5A6" }, { "white", "FFFFFF" } };

            string match = null;

            foreach (var color in colors)
            {
                match = Regex.Replace(input, color.Key, color.Value, RegexOptions.IgnoreCase) ?? null;
            }

            colors.TryGetValue(match, out string output);
            return output;
        }
    }
}