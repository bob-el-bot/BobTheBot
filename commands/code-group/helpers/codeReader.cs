using System;
using System.Text;

namespace Commands.Helpers
{
    public static class CodeReader
    {
        /// <summary>
        /// Gets a preview of code lines from a file content.
        /// </summary>
        /// <param name="fileContent">The content of the file.</param>
        /// <param name="startLine">The start line number.</param>  w
        /// <param name="endLine">The end line number.</param>
        /// <returns>A string containing the preview of code lines.</returns>
        public static string GetPreview(string fileContent, ref ushort? startLine, ref ushort? endLine)
        {
            if (startLine == null && endLine == null)
            {
                startLine = 1;
                return GetPreviewForUnspecifiedLines(fileContent, ref endLine);
            }

            if (startLine != endLine)
            {
                return GetPreviewForSpecifiedLines(fileContent, startLine.Value, endLine.Value);
            }

            return GetPreviewForSingleLine(fileContent, startLine.Value);
        }

        private static string GetPreviewForUnspecifiedLines(string fileContent, ref ushort? endLine)
        {
            var allLines = fileContent.Split("\n");
            StringBuilder lines = new();

            for (int i = 0; i < allLines.Length; i++)
            {
                string line = (i + 1).ToString() + " " + allLines[i] + "\n";

                if (lines.Length + line.Length < 1750)
                {
                    lines.Append(line);
                    endLine = (ushort?)(i + 1);
                }
                else
                {
                    break;
                }
            }

            return lines.ToString();
        }

        private static string GetPreviewForSpecifiedLines(string fileContent, ushort startLine, ushort endLine)
        {
            StringBuilder lines = new();

            string[] allLines = fileContent.Split("\n");
            for (int i = startLine - 1; i < endLine; i++)
            {
                lines.AppendLine($"{i + 1} {allLines[i]}");
            }

            return lines.ToString();
        }

        private static string GetPreviewForSingleLine(string fileContent, ushort line)
        {
            string[] allLines = fileContent.Split("\n");
            return $"{line} {allLines[line - 1]}";
        }

        /// <summary>
        /// Gets the line numbers specified in the GitHub link fragment.
        /// </summary>
        /// <param name="fragment">The fragment of the GitHub link containing line numbers.</param>
        /// <returns>A tuple representing the start and end line numbers. (null, null) if fragment is empty or null.</returns>
        public static (ushort?, ushort?) GetLineNumbers(string fragment)
        {
            // Check if fragment exists (if not, as many lines as can be shown will be.)
            if (fragment == "")
            {
                return (null, null);
            }

            // Remove L
            fragment = fragment.TrimStart('L');

            // Check if there are line number specifiers (there need to be)
            if (!fragment.Contains('L'))
            {
                throw new InvalidOperationException("Invalid GitHub link format | Missing line numbers.");
            }

            // If it contains character specifiers get rid of them. example: (#L15C12-L18C17)
            if (fragment.Contains('C'))
            {
                if (fragment.Contains('-'))
                {
                    if (fragment[..fragment.IndexOf('-', StringComparison.Ordinal)].Contains('C', StringComparison.Ordinal))
                    {
                        fragment = fragment[..fragment.IndexOf("C", StringComparison.Ordinal)] + fragment[fragment.IndexOf("-", StringComparison.Ordinal)..];
                    }
                    if (fragment.Contains('C', StringComparison.Ordinal))
                    {
                        fragment = fragment[..fragment.IndexOf("C", StringComparison.Ordinal)];
                    }
                }
                else
                {
                    fragment = fragment[..fragment.IndexOf("C", StringComparison.Ordinal)];
                }
            }

            fragment = fragment.Replace("L", "");
            ushort? startLine = null;
            ushort? endLine = null;
            if (fragment.Contains('-'))
            {
                startLine = ushort.Parse(fragment[1..fragment.IndexOf("-", StringComparison.Ordinal)]);
                endLine = ushort.Parse(fragment[(fragment.IndexOf("-", StringComparison.Ordinal) + 1)..]);
            }
            else if (ushort.TryParse(fragment[1..], out _))
            {
                startLine = ushort.Parse(fragment[1..]);
                endLine = ushort.Parse(fragment[1..]);
            }

            if (endLine == null || startLine == null)
            {
                throw new InvalidOperationException("Invalid GitHub link format | The value of statLine or endLine is null.");
            }
            else if (endLine < startLine)
            {
                throw new InvalidOperationException("Invalid GitHub link format | The value of endLine is less than the startLine.");
            }

            return ((ushort)startLine, (ushort)endLine);
        }
    }
}