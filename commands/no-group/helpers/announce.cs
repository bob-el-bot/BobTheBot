using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Commands.Helpers
{
    public static class Announcement
    {
        public static string FormatDescription(string description)
        {
            string[] delimiters = [" - ", "### ",];
            string pattern = string.Join("|", Array.ConvertAll(delimiters, Regex.Escape));
            string[] lines = Regex.Split(description, "(?=" + pattern + ")");

            StringBuilder output = new();
            foreach (string line in lines)
            {
                output.AppendLine(line);
            }

            return output.ToString();
        }
    }
}