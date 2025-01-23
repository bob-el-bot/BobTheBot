using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ColorHelper;
using ColorMethods;
using Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("preview", "All commands relevant to previewing.")]
    public class PreviewGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("code", "Preview specific lines from a file on GitHub right on Discord.")]
        public async Task CodePreview([Summary("link", "A GitHub Link to specific lines of code.")] string link)
        {
            // Add HTTP if missing.
            if ((link.Contains('.', StringComparison.Ordinal) && link.Length < 7) || (link.Length >= 7 && link[..7] != "http://" && link.Length >= 8 && link[..8] != "https://"))
            {
                link = $"https://{link}";
            }

            await CodeReader.Respond(Context.Interaction, link);
        }

        [MessageCommand("Preview Code")]
        public async Task PreviewCodeMessage(IMessage message)
        {
            string pattern = @"(https?://\S+)|(www\.\S+)";

            // Create a Regex object with the pattern
            Regex regex = new(pattern);

            // Find all matches in the input string
            MatchCollection matches = regex.Matches(message.Content);

            if (matches.Count == 0)
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/`.\n- If you preview a link with no line specificaions, Bob will automatically show as many lines as possible from the start.\n- For line specifications, put `#L15` or `#L15-L18` at the end of the link to the file. (see below).\n- If you are sharing a single line it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15`\n- If you are sharing multiple lines it could look like this: `https://github.com/bob-el-bot/website/blob/main/index.html#L15-L18`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                string link = matches[0].Value;

                await CodeReader.Respond(Context.Interaction, link);
            }
        }

        [SlashCommand("color", "Preview what a color looks like, and get more information.")]
        public async Task Color([Summary("color", "A color name (purple), or a valid hex code (#8D52FD) or valid RGB code (141, 82, 253).")] string color)
        {
            // Get Values
            Color? finalColor = Colors.TryGetColor(color);

            if (finalColor == null)
            {
                await RespondAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex and RGB codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                return;
            }

            string hex = finalColor.Value.ToString();
            CMYK cmyk = ColorConverter.HexToCmyk(new HEX(hex));
            HSL hsl = ColorConverter.HexToHsl(new HEX(hex));
            HSV hsv = ColorConverter.HexToHsv(new HEX(hex));
            RGB rgb = ColorConverter.HexToRgb(new HEX(hex));

            // Make Color Image
            using MemoryStream imageStream = ColorPreview.CreateColorImage(100, 100, hex);

            // Make Embed
            var embed = new EmbedBuilder { };
            embed.AddField("Hex", $"```{hex}```")
            .AddField("RGB", $"```R: {rgb.R}, G: {rgb.G}, B: {rgb.B}```")
            .AddField("CMYK", $"```C: {cmyk.C}, M: {cmyk.M}, Y: {cmyk.Y}, K: {cmyk.K}```")
            .AddField("HSL", $"```H: {hsl.H}, S: {hsl.S}, L: {hsl.L}```")
            .AddField("HSV", $"```H: {hsv.H}, S: {hsv.S}, V: {hsv.V}```")
            .WithThumbnailUrl("attachment://image.webp")
            .WithColor(finalColor.Value);

            await RespondWithFileAsync(imageStream, "image.webp", "", embed: embed.Build());

            imageStream.Dispose();
        }

        [SlashCommand("pull-request", "Preview a pull request from GitHub right on Discord.")]
        public async Task PullRequestPreview([Summary("link", "A GitHub Link to a specific pull request.")] string link)
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
                if (uri.Host.Contains("github.com") == false)
                {
                    throw new InvalidOperationException("Invalid GitHub link format | Host other than GitHub");
                }

                PullRequestInfo pullRequestInfo = PullRequestReader.CreatePullRequestInfo(link);
                await RespondAsync(embed: await PullRequestReader.GetPreview(pullRequestInfo));
            }
            catch
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/` or `github.com/`.\n- Your link must be to a pull request.\n- A valid link could look like this: `https://github.com/bob-el-bot/BobTheBot/pull/149`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
        }

        [SlashCommand("issue", "Preview an issue from GitHub right on Discord.")]
        public async Task IssuePreview([Summary("link", "A GitHub Link to a specific issue.")] string link)
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
                if (uri.Host.Contains("github.com") == false)
                {
                    throw new InvalidOperationException("Invalid GitHub link format | Host other than GitHub");
                }

                IssueInfo issueInfo = IssueReader.CreateIssueInfo(link);
                await RespondAsync(embed: await IssueReader.GetPreview(issueInfo));
            }
            catch
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://github.com/` or `github.com/`.\n- Your link must be to an issue.\n- A valid link could look like this: `https://github.com/bob-el-bot/BobTheBot/issues/153`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
        }

        [SlashCommand("message", "Preview a message from any server that Bob is in.")]
        public async Task MessagePreview([Summary("link", "A Discord message link.")] string link)
        {
            try
            {
                // Parse the Discord message link
                var messageInfo = MessageReader.CreateMessageInfo(link);

                // Get the message preview
                var embed = await MessageReader.GetPreview(messageInfo);

                // Respond with the embed
                if (embed != null)
                {
                    await RespondAsync(embed: embed);
                }
                else
                {
                    await RespondAsync(text: "❌ Unable to fetch message preview.\n- This probably means Bob is not in the server of the given message.\n- It could mean that Bob needs the permissions `View Channel` or `View Message History` in the channel of the given message.\n- It could also mean the message has no text, and no embed with a title or description.", ephemeral: true);
                }
            }
            catch
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know: \n- Your link needs to start with `https://discord.com/channels/`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
        }
    }
}