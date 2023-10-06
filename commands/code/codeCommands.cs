using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;

[EnabledInDm(true)]
[Group("code", "All commands relevant to code.")]
public class CodeCommands : InteractionModuleBase<SocketInteractionContext>
{
    [EnabledInDm(true)]
    [SlashCommand("preview", "Preview specific lines from a file on GitHub right on Discord.")]
    public async Task Preview([Summary("link", "A GitHub Link to specific lines of code.")] string link)
    {
        try
        {
            // Parse Link
            string formattedLink;
            if (link[..19] == "https://github.com/")
                formattedLink = link[19..];
            else if (link[..11] == "github.com/")
            {
                formattedLink = link[11..];
                link = "https://" + link;
            }
            else
                throw new Exception();
            //bob-el-bot/website/blob/main/index.html#L15-L18
            string organization = formattedLink[..formattedLink.IndexOf("/")];
            formattedLink = formattedLink[(formattedLink.IndexOf("/") + 1)..];
            //website/blob/main/index.html#L15-L18
            string repository = formattedLink[..formattedLink.IndexOf("/")];
            formattedLink = formattedLink[(formattedLink.IndexOf("/") + 1)..];
            //blob/main/index.html#L15-L18
            formattedLink = formattedLink[(formattedLink.IndexOf("/") + 1)..];
            //main/index.html#L15-L18
            string branch = formattedLink[..formattedLink.IndexOf("/")];
            formattedLink = formattedLink[(formattedLink.IndexOf("/") + 1)..];
            //index.html#L15-L18
            string file = formattedLink[..formattedLink.IndexOf("#")];
            formattedLink = formattedLink[formattedLink.IndexOf("#")..];
            //#L15-L18
            string lineNumbers = formattedLink;

            // Check if there are line number specifiers (there need to be)
            if (!lineNumbers.Contains('L')) throw new Exception();

            // If it contains character specifiers get rid of them. example: (#L15C12-L18C17)
            if (lineNumbers.Contains('C'))
            {
                if (lineNumbers.Contains('-'))
                {
                    if (lineNumbers[..lineNumbers.IndexOf('-')].Contains('C')) lineNumbers = lineNumbers[..lineNumbers.IndexOf("C")] + lineNumbers[lineNumbers.IndexOf("-")..];
                    if (lineNumbers.Contains('C')) lineNumbers = lineNumbers[..lineNumbers.IndexOf("C")];
                }
                else lineNumbers = lineNumbers[..lineNumbers.IndexOf("C")];
            }

            lineNumbers = lineNumbers.Replace("L", "");
            short? startLine = null;
            short? endLine = null;
            if (lineNumbers.Contains('-'))
            {
                startLine = short.Parse(lineNumbers[1..lineNumbers.IndexOf("-")]);
                endLine = short.Parse(lineNumbers[(lineNumbers.IndexOf("-") + 1)..]);
            }
            else if (short.TryParse(lineNumbers[1..], out _))
            {
                startLine = short.Parse(lineNumbers[1..]);
                endLine = short.Parse(lineNumbers[1..]);
            }

            if (endLine == null || startLine == null)
                throw new Exception();
            else if (endLine < startLine)
                throw new Exception();

            // Send Request
            string content = await APIInterface.GetFromAPI($"https://api.github.com/repos/{organization}/{repository}/contents/{file}?ref={branch}", APIInterface.AcceptTypes.application_json);

            // Parse Content
            var jsonData = JsonNode.Parse(content).AsObject();
            var fileData = Convert.FromBase64String(jsonData["content"].ToString());
            var fileContent = System.Text.Encoding.UTF8.GetString(fileData);
            string previewLines = GetLines(fileContent, (short)startLine, (short)endLine);

            // Format final response
            string preview = $"Showing {lineNumbers} of [{file}](<{link}>) on {branch} branch.\n```{file[(file.IndexOf('.') + 1)..]}\n{previewLines}```";

            // Check if message is too long for Discord API.
            if (preview.Length >= 2000)
            {
                await RespondAsync(text: $"❌ The preview of lines {lineNumbers} *cannot* be shown because it contains **{preview.Length}** characters.\n- Try previewiing fewer lines.\n- Discord has a limit of **2000** characters.", ephemeral: true);
            }
            else
                await RespondAsync(text: preview);
        }
        catch (Exception e)
        {
            await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/` or `github.com/`.\n- Your link needs line specifications. Put `#L15` or `#L15-L18` at the end of the link to the file. (see below).\n- If you are sharing a single line it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15`\n- If you are sharing multiple lines it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15-L18`\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            Console.WriteLine(e);
        }
    }

    private static string GetLines(string fileContent, short startLine, short endLine)
    {
        if (startLine != endLine)
        {
            var allLines = fileContent.Split("\n");
            string lines = "";
            for (int i = startLine - 1; i < endLine; i++)
            {
                lines += (i + 1).ToString() + " " + allLines[i] + "\n";
            }
            return lines;
        }
        else if (startLine == endLine)
        {
            var allLines = fileContent.Split("\n");
            return startLine.ToString() + " " + allLines[startLine - 1];
        }
        return "There was an error parsing the requested lines of code.";
    }
}