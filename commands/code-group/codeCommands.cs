using System;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Commands.Helpers;
using Discord;
using Discord.Interactions;
using static ApiInteractions.Interface;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("code", "All commands relevant to code.")]
    public class CodeGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("preview", "Preview specific lines from a file on GitHub right on Discord.")]
        public async Task Preview([Summary("link", "A GitHub Link to specific lines of code.")] string link)
        {
            try
            {
                // Add HTTP if missing.
                if ((link.Contains('.', StringComparison.Ordinal) && link.Length < 7) || (link.Length >= 7 && link[..7] != "http://" && link.Length >= 8 && link[..8] != "https://"))
                {
                    link = $"https://{link}";
                }

                // Parse Link using Uri class
                Uri uri = new(link);

                // Check if the host is github.com
                if (uri.Host != "github.com")
                {
                    throw new InvalidOperationException("Invalid GitHub link format");
                }

                // Extracting relevant components
                string organization = uri.Segments[1].Trim('/');
                string repository = uri.Segments[2].Trim('/');
                string branch = uri.Segments[4].Trim('/');
                string file = string.Join("/", uri.Segments[5..]).Replace("//", "/");

                // If these values are null then show as many lines as possible from the beginning.
                (ushort?, ushort?) lineNumbers = CodeReader.GetLineNumbers(uri.Fragment);

                // Send Request
                string content = await GetFromAPI($"https://api.github.com/repos/{organization}/{repository}/contents/{file}?ref={branch}", AcceptTypes.application_json);

                // Parse Content
                JsonObject jsonData = JsonNode.Parse(content).AsObject();
                byte[] fileData = Convert.FromBase64String(jsonData["content"].ToString());
                string fileContent = System.Text.Encoding.UTF8.GetString(fileData);
                string previewLines = CodeReader.GetPreview(fileContent, ref lineNumbers.Item1, ref lineNumbers.Item2);

                // Format final response
                string formattedLineNumbers = $"**#{lineNumbers.Item1}-{lineNumbers.Item2}**";
                string preview = $"🔎 Showing {formattedLineNumbers} of [{repository}/{branch}/{file}](<{link}>)\n```{file[(file.IndexOf('.') + 1)..]}\n{previewLines}```";

                // Check if message is too long for Discord API.
                if (preview.Length > 2000)
                {
                    await RespondAsync(text: $"❌ The preview of lines {lineNumbers} *cannot* be shown because it contains **{preview.Length}** characters.\n- Try previewing fewer lines.\n- Discord has a limit of **2000** characters.", ephemeral: true);
                }
                else
                {
                    await RespondAsync(text: preview);
                }
            }
            catch
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/` or `github.com/`.\n- Your link needs line specifications. Put `#L15` or `#L15-L18` at the end of the link to the file. (see below).\n- If you are sharing a single line it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15`\n- If you are sharing multiple lines it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15-L18`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
        }

        [MessageCommand("Preview Code")]
        public async Task PreviewCodeMessage(IMessage message)
        {
            string link;

            string pattern = @"(https?://\S+)|(www\.\S+)";

            // Create a Regex object with the pattern
            Regex regex = new(pattern);

            // Find all matches in the input string
            MatchCollection matches = regex.Matches(message.Content);

            if (matches.Count == 0)
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/` or `github.com/`.\n- Your link needs line specifications. Put `#L15` or `#L15-L18` at the end of the link to the file. (see below).\n- If you are sharing a single line it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15`\n- If you are sharing multiple lines it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15-L18`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                link = matches[0].Value;

                try
                {
                    // Parse Link using Uri class
                    Uri uri = new(link);

                    // Check if the host is github.com
                    if (uri.Host != "github.com")
                    {
                        throw new InvalidOperationException("Invalid GitHub link format | Host other than GitHub");
                    }

                    // Extracting relevant components
                    string organization = uri.Segments[1].Trim('/');
                    string repository = uri.Segments[2].Trim('/');
                    string branch = uri.Segments[4].Trim('/');
                    string file = Uri.UnescapeDataString(string.Join("/", uri.Segments[5..]).Replace("//", "/"));

                    // If these values are null then show as many lines as possible from the beginning.
                    (ushort?, ushort?) lineNumbers = CodeReader.GetLineNumbers(uri.Fragment);

                    // Send Request
                    string content = await GetFromAPI($"https://api.github.com/repos/{organization}/{repository}/contents/{file}?ref={branch}", AcceptTypes.application_json);

                    // Parse Content
                    JsonObject jsonData = JsonNode.Parse(content).AsObject();
                    byte[] fileData = Convert.FromBase64String(jsonData["content"].ToString());
                    string fileContent = System.Text.Encoding.UTF8.GetString(fileData);
                    string previewLines = CodeReader.GetPreview(fileContent, ref lineNumbers.Item1, ref lineNumbers.Item2);

                    // Format final response
                    string formattedLineNumbers = $"**#{lineNumbers.Item1}-{lineNumbers.Item2}**";
                    string preview = $"🔎 Showing {formattedLineNumbers} of [{repository}/{branch}/{file}](<{link}>)\n```{file[(file.IndexOf('.') + 1)..]}\n{previewLines}```";

                    // Check if message is too long for Discord API.
                    if (preview.Length > 2000)
                    {
                        await RespondAsync(text: $"❌ The preview of lines {lineNumbers} *cannot* be shown because it contains **{preview.Length}** characters.\n- Try previewing fewer lines.\n- Discord has a limit of **2000** characters.", ephemeral: true);
                    }
                    else
                    {
                        await RespondAsync(text: preview);
                    }
                }
                catch
                {
                    await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/` or `github.com/`.\n- Your link needs line specifications. Put `#L15` or `#L15-L18` at the end of the link to the file. (see below).\n- If you are sharing a single line it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15`\n- If you are sharing multiple lines it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15-L18`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
            }
        }
    }
}