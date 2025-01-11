using System;
using System.IO;
using System.Threading.Tasks;
using Commands.Helpers;
using Debug;
using Discord;
using Discord.Interactions;
using SkiaSharp;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("generate", "All commands relevant to generation.")]
    public class GenerateGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("youtube-comment", "Bob will generate a Youtube comment image!")]
        public async Task YoutubeComment([Summary("comment", "The comment content")] string comment,
            [Summary("username", "The username for the commenter. Defaults to your Discord username.")][MinLength(1)][MaxLength(50)] string username = "",
            [Summary("avatar-url", "The URL of an image you want to have as the profile picture. Defaults to your Discord PFP.")] string avatarUrl = "",
            [Summary("likes", "The like count for the comment. Defaults to 1000.")][MinValue(0)][MaxValue(999999999)] int likes = 1000,
            [Summary("time", "The amount of time of the specified unit. Defaults to 1.")][MinValue(0)][MaxValue(999999999)] int time = 1,
            [Summary("time-unit", "The unit of time. Defaults to Hour")] YouTubeCommentImageGenerator.TimeUnit timeUnit = YouTubeCommentImageGenerator.TimeUnit.Hour,
            [Summary("theme", "The theme of the comment. Defaults to Dark.")] YouTubeCommentImageGenerator.Theme theme = YouTubeCommentImageGenerator.Theme.Dark)
        {
            await DeferAsync();

            if (username == "")
            {
                username = Context.User.Username;  // Default username if none provided
            }

            if (avatarUrl == "")
            {
                avatarUrl = Context.User.GetAvatarUrl();  // Default avatar URL if none provided
            }
            else if (!Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute))
            {
                await FollowupAsync("❌ The provided avatar URL is not valid.", ephemeral: true);
                return;
            }

            try
            {
                // Generate the image
                var image = await YouTubeCommentImageGenerator.GenerateYouTubeCommentImage($"@{username}", avatarUrl, comment, time, timeUnit, likes, theme);

                if (image == null)
                {
                    await FollowupAsync($"❌ An error occurred while getting your provided avatar URL `{avatarUrl}`.", ephemeral: true);
                    return;
                }

                // Convert the generated image to a memory stream
                using var ms = new MemoryStream();
                image.Encode(ms, SKEncodedImageFormat.Png, 100);
                // Rewind the stream before sending
                ms.Seek(0, SeekOrigin.Begin);

                // Send the image as a Discord message attachment
                await FollowupWithFileAsync(ms, "youtube_comment.png");
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true); 
            }
        }
    }
}