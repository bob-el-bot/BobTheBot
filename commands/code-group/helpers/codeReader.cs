using System;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static ApiInteractions.Interface;

namespace Commands.Helpers
{
    public static class CodeReader
    {
        /// <summary>
        /// Asynchronously retrieves a preview of the content from the provided link information.
        /// </summary>
        /// <param name="linkInfo">The FileLinkInfo object containing information about the link.</param>
        /// <returns>A formatted preview of the content.</returns>
        public static async Task<string> GetPreview(FileLinkInfo linkInfo)
        {
            // Send Request
            string content = await GetFromAPI(linkInfo.GetApiUrl(), AcceptTypes.application_json);

            // Parse Content
            JsonObject jsonData = JsonNode.Parse(content).AsObject();
            byte[] fileData = Convert.FromBase64String(jsonData["content"].ToString());
            string fileContent = Encoding.UTF8.GetString(fileData);

            return FormatResponse(fileContent, ref linkInfo.LineNumbers.Item1, ref linkInfo.LineNumbers.Item2);
        }

        /// <summary>
        /// Gets a preview of code lines from a file content.
        /// </summary>
        /// <param name="fileContent">The content of the file.</param>
        /// <param name="startLine">The start line number.</param>
        /// <param name="endLine">The end line number.</param>
        /// <returns>A string containing the preview of code lines.</returns>
        private static string FormatResponse(string fileContent, ref ushort? startLine, ref ushort? endLine)
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

        /// <summary>
        /// Returns a formatted string representing line numbers.
        /// </summary>
        /// <param name="lineNumbers">A tuple containing starting and ending line numbers.</param>
        /// <returns>A formatted string representing line numbers.</returns>
        public static string GetFormattedLineNumbers((ushort?, ushort?) lineNumbers)
        {
            if (lineNumbers.Item1 == lineNumbers.Item2)
            {
                return $"**#{lineNumbers.Item1}**";
            }

            return $"**#{lineNumbers.Item1}-{lineNumbers.Item2}**";
        }

        /// <summary>
        /// Creates a LinkInfo object from the provided URL.
        /// </summary>
        /// <param name="url">The URL to create the LinkInfo object from.</param>
        /// <param name="fromMessage">Specifies whether the URL is from a message.</param>
        /// <returns>The LinkInfo object created from the URL.</returns>
        public static FileLinkInfo CreateFileLinkInfo(string url, bool fromMessage = false)
        {
            FileLinkInfo link = new();
            Uri uri = new(url);

            // Extracting relevant components
            link.Organization = uri.Segments[1].Trim('/');
            link.Repository = uri.Segments[2].Trim('/');
            link.Branch = uri.Segments[4].Trim('/');
            link.File = fromMessage ? string.Join("/", uri.Segments[5..]).Replace("//", "/") : Uri.UnescapeDataString(string.Join("/", uri.Segments[5..]).Replace("//", "/"));
            link.LineNumbers = GetLineNumbers(uri.Fragment);

            return link;
        }
    }

    public class FileLinkInfo
    {
        public string Organization { get; set; }
        public string Repository { get; set; }
        public string Branch { get; set; }
        public string File { get; set; }

        public (ushort?, ushort?) LineNumbers;

        /// <summary>
        /// Generates the API URL based on the organization, repository, file, and branch.
        /// </summary>
        /// <returns>The generated API URL.</returns>
        public string GetApiUrl()
        {
            return $"https://api.github.com/repos/{this.Organization}/{this.Repository}/contents/{this.File}?ref={this.Branch}";
        }
    }
}