using System;
using System.IO;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Debug;
using Discord;
using Discord.Interactions;
using SkiaSharp;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("generate", "All commands relevant to generation.")]
    public class GenerateGroup : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("youtube-comment", "Bob will generate a Youtube comment image!")]
        public async Task YoutubeComment([Summary("comment", "The comment content.")] string comment,
            [Summary("username", "The username for the commenter. Defaults to your Discord username.")][MinLength(1)][MaxLength(50)] string username = "",
            [Summary("avatar", "An image you want to have as the profile picture. Defaults to your Discord PFP.")] IAttachment avatar = null,
            [Summary("avatar-url", "The URL of an image you want to have as the profile picture, used if no avatar is uploaded.")] string avatarUrl = "",
            [Summary("likes", "The like count for the comment. Defaults to 1000.")][MinValue(0)][MaxValue(999999999)] int likes = 1000,
            [Summary("time", "The amount of time of the specified unit. Defaults to 1.")][MinValue(0)][MaxValue(999999999)] int time = 1,
            [Summary("time-unit", "The unit of time. Defaults to Hour.")] YouTubeCommentImageGenerator.TimeUnit timeUnit = YouTubeCommentImageGenerator.TimeUnit.Hour,
            [Summary("theme", "The theme of the comment. Defaults to Dark.")] YouTubeCommentImageGenerator.Theme theme = YouTubeCommentImageGenerator.Theme.Dark)
        {
            await DeferAsync();

            if (username == "")
            {
                username = Context.User.Username;  // Default username if none provided
            }

            avatarUrl = await ResolveImageUrl(avatar, avatarUrl, "avatar");
            if (avatarUrl == null)
            {
                return;
            }

            try
            {
                // Generate the image
                using var image = await YouTubeCommentImageGenerator.GenerateYouTubeCommentImage($"@{username}", avatarUrl, comment, time, timeUnit, likes, theme);

                if (image == null)
                {
                    await FollowupAsync(ImageErrorMessage("avatar"), ephemeral: true);
                    return;
                }

                await SendImageAsync(image, "youtube_comment.png");
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true); 
            }
        }

        [SlashCommand("tweet", "Bob will generate a tweet image!")]
        public async Task Tweet([Summary("text", "The tweet content.")][MaxLength(280)] string text,
            [Summary("display-name", "The display name for the poster. Defaults to your Discord display name.")][MinLength(1)][MaxLength(50)] string displayName = "",
            [Summary("handle", "The handle for the poster (without the @). Defaults to your Discord username.")][MinLength(1)][MaxLength(15)] string handle = "",
            [Summary("avatar", "An image you want to have as the profile picture. Defaults to your Discord PFP.")] IAttachment avatar = null,
            [Summary("avatar-url", "The URL of an image you want to have as the profile picture, used if no avatar is uploaded.")] string avatarUrl = "",
            [Summary("likes", "The like count for the tweet. Defaults to 1000.")][MinValue(0)][MaxValue(999999999)] int likes = 1000,
            [Summary("reposts", "The repost count for the tweet. Defaults to 100.")][MinValue(0)][MaxValue(999999999)] int reposts = 100,
            [Summary("verified", "Whether to show a verified badge. Defaults to false.")] bool verified = false,
            [Summary("theme", "The theme of the tweet. Defaults to Dark.")] TweetImageGenerator.Theme theme = TweetImageGenerator.Theme.Dark)
        {
            await DeferAsync();

            if (displayName == "")
            {
                displayName = Context.User.GlobalName ?? Context.User.Username;
            }

            if (handle == "")
            {
                handle = Context.User.Username;
            }

            avatarUrl = await ResolveImageUrl(avatar, avatarUrl, "avatar");
            if (avatarUrl == null)
            {
                return;
            }

            try
            {
                using var image = await TweetImageGenerator.GenerateTweetImage(displayName, handle, avatarUrl, text, likes, reposts, verified, theme);

                if (image == null)
                {
                    await FollowupAsync(ImageErrorMessage("avatar"), ephemeral: true);
                    return;
                }

                await SendImageAsync(image, "tweet.png");
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true);
            }
        }

        [SlashCommand("quote", "Bob will generate an inspirational quote image!")]
        public async Task Quote([Summary("quote", "The quote content.")][MaxLength(300)] string quote,
            [Summary("author", "Who said the quote. Defaults to your Discord display name.")][MinLength(1)][MaxLength(50)] string author = "",
            [Summary("avatar", "An image of the author. Defaults to your Discord PFP.")] IAttachment avatar = null,
            [Summary("avatar-url", "The URL of an image of the author, used if no avatar is uploaded.")] string avatarUrl = "",
            [Summary("grayscale", "Whether to make the image black and white. Defaults to true.")] bool grayscale = true)
        {
            await DeferAsync();

            if (author == "")
            {
                author = Context.User.GlobalName ?? Context.User.Username;
            }

            avatarUrl = await ResolveImageUrl(avatar, avatarUrl, "avatar");
            if (avatarUrl == null)
            {
                return;
            }

            try
            {
                using var image = await QuoteImageGenerator.GenerateQuoteImage(quote, author, avatarUrl, grayscale);

                if (image == null)
                {
                    await FollowupAsync(ImageErrorMessage("avatar"), ephemeral: true);
                    return;
                }

                await SendImageAsync(image, "quote.png");
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true);
            }
        }

        /// <summary>
        /// Resolves the image to use from an uploaded attachment or a URL, preferring the attachment and defaulting to the user's avatar.
        /// Sends an error followup and returns null if the attachment is not an image or the URL is invalid.
        /// </summary>
        /// <param name="attachment">The uploaded image, or null.</param>
        /// <param name="url">The user provided URL, or an empty string.</param>
        /// <param name="name">The name of the image option, used in the error message.</param>
        private async Task<string> ResolveImageUrl(IAttachment attachment, string url, string name)
        {
            if (attachment != null)
            {
                if (attachment.ContentType == null || !attachment.ContentType.StartsWith("image/"))
                {
                    await FollowupAsync($"❌ The uploaded {name} is not an image.", ephemeral: true);
                    return null;
                }

                return attachment.Url;
            }

            if (url == "")
            {
                return Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl();
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                await FollowupAsync($"❌ The provided {name} URL is not valid.", ephemeral: true);
                return null;
            }

            return url;
        }

        /// <summary>
        /// The error message for when an image could not be downloaded or decoded.
        /// </summary>
        /// <param name="name">The name of the image option.</param>
        private static string ImageErrorMessage(string name)
        {
            return $"❌ Your provided {name} could not be loaded. Make sure it is a valid image (up to {ImageCache.MaxPixels / 1_000_000} megapixels).";
        }

        /// <summary>
        /// Encodes an image as a PNG and sends it as a followup attachment.
        /// </summary>
        private async Task SendImageAsync(SKBitmap image, string fileName)
        {
            using var ms = new MemoryStream();
            image.Encode(ms, SKEncodedImageFormat.Png, 100);
            // Rewind the stream before sending
            ms.Seek(0, SeekOrigin.Begin);

            // Send the image as a Discord message attachment
            await FollowupWithFileAsync(ms, fileName);
        }
    }
}