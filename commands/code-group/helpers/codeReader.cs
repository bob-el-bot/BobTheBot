using System.Text;

namespace Commands.Helpers
{
    public static class CodeReader
    {
        public static string GetLines(string fileContent, short startLine, short endLine)
        {
            if (startLine != endLine)
            {
                var allLines = fileContent.Split("\n");
                StringBuilder lines = new();
                for (int i = startLine - 1; i < endLine; i++)
                {
                    lines.Append((i + 1).ToString() + " " + allLines[i] + "\n");
                }
                return lines.ToString();
            }
            else if (startLine == endLine)
            {
                var allLines = fileContent.Split("\n");
                return startLine.ToString() + " " + allLines[startLine - 1];
            }
            return "There was an error parsing the requested lines of code.";
        }
    }
}