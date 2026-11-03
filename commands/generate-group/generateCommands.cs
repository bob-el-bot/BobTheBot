using System;
using System.IO;
using System.Threading.Tasks;
using Bob.ColorMethods;
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
            [Summary("theme", "The theme of the comment. Defaults to Dark.")] YouTubeCommentImageGenerator.Theme theme = YouTubeCommentImageGenerator.Theme.Dark,
            [Summary("background-color", "The background color as a name, hex, or RGB. Defaults to the theme.")] string backgroundColor = "",
            [Summary("text-color", "The text color as a name, hex, or RGB. Defaults to the theme.")] string textColor = "")
        {
            await DeferAsync();

            var (validBackground, background) = await ResolveColor(backgroundColor);
            var (validText, foreground) = validBackground ? await ResolveColor(textColor) : (false, null);
            if (!validBackground || !validText)
            {
                return;
            }

            if (username == "")
            {
                username = Context.User.Username;  // Default username if none provided
            }

            await GenerateAndSendAsync(avatar, avatarUrl, "avatar", "youtube_comment.png",
                url => YouTubeCommentImageGenerator.GenerateYouTubeCommentImage($"@{username}", url, comment, time, timeUnit, likes, theme, background, foreground));
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
            [Summary("theme", "The theme of the tweet. Defaults to Dark.")] TweetImageGenerator.Theme theme = TweetImageGenerator.Theme.Dark,
            [Summary("background-color", "The background color as a name, hex, or RGB. Defaults to the theme.")] string backgroundColor = "",
            [Summary("text-color", "The text color as a name, hex, or RGB. Defaults to the theme.")] string textColor = "")
        {
            await DeferAsync();

            var (validBackground, background) = await ResolveColor(backgroundColor);
            var (validText, foreground) = validBackground ? await ResolveColor(textColor) : (false, null);
            if (!validBackground || !validText)
            {
                return;
            }

            if (displayName == "")
            {
                displayName = Context.User.GlobalName ?? Context.User.Username;
            }

            if (handle == "")
            {
                handle = Context.User.Username;
            }

            await GenerateAndSendAsync(avatar, avatarUrl, "avatar", "tweet.png",
                url => TweetImageGenerator.GenerateTweetImage(displayName, handle, url, text, likes, reposts, verified, theme, background, foreground));
        }

        [SlashCommand("quote", "Bob will generate an inspirational quote image!")]
        public async Task Quote([Summary("quote", "The quote content.")][MaxLength(300)] string quote,
            [Summary("author", "Who said the quote. Defaults to your Discord display name.")][MinLength(1)][MaxLength(50)] string author = "",
            [Summary("avatar", "An image of the author. Defaults to your Discord PFP.")] IAttachment avatar = null,
            [Summary("avatar-url", "The URL of an image of the author, used if no avatar is uploaded.")] string avatarUrl = "",
            [Summary("grayscale", "Whether to make the image black and white. Defaults to true.")] bool grayscale = true,
            [Summary("background-color", "The background color as a name, hex, or RGB. Defaults to black.")] string backgroundColor = "",
            [Summary("text-color", "The text color as a name, hex, or RGB. Defaults to white.")] string textColor = "")
        {
            await DeferAsync();

            var (validBackground, background) = await ResolveColor(backgroundColor);
            var (validText, foreground) = validBackground ? await ResolveColor(textColor) : (false, null);
            if (!validBackground || !validText)
            {
                return;
            }

            if (author == "")
            {
                author = Context.User.GlobalName ?? Context.User.Username;
            }

            await GenerateAndSendAsync(avatar, avatarUrl, "avatar", "quote.png",
                url => QuoteImageGenerator.GenerateQuoteImage(quote, author, url, grayscale, background, foreground));
        }

        [SlashCommand("caption", "Bob will add a meme caption above an image!")]
        public async Task Caption([Summary("caption", "The caption text.")][MaxLength(200)] string caption,
            [Summary("image", "The image to caption. Defaults to your Discord PFP.")] IAttachment image = null,
            [Summary("image-url", "The URL of the image to caption, used if no image is attached.")] string imageUrl = "",
            [Summary("background-color", "The background color as a name, hex, or RGB. Defaults to white.")] string backgroundColor = "",
            [Summary("text-color", "The text color as a name, hex, or RGB. Defaults to black.")] string textColor = "")
        {
            await DeferAsync();

            var (validBackground, background) = await ResolveColor(backgroundColor);
            var (validText, foreground) = validBackground ? await ResolveColor(textColor) : (false, null);
            if (!validBackground || !validText)
            {
                return;
            }

            await GenerateAndSendAsync(image, imageUrl, "image", "caption.png",
                url => CaptionImageGenerator.GenerateCaptionImage(url, caption, background, foreground));
        }

        [SlashCommand("achievement", "Bob will generate an achievement unlocked image!")]
        public async Task Achievement([Summary("description", "What the achievement was for.")][MaxLength(100)] string description,
            [Summary("title", "The title of the achievement. Defaults to \"Achievement Unlocked!\".")][MinLength(1)][MaxLength(40)] string title = "Achievement Unlocked!",
            [Summary("icon", "An image you want as the icon. Defaults to your Discord PFP.")] IAttachment icon = null,
            [Summary("icon-url", "The URL of an image you want as the icon, used if no icon is uploaded.")] string iconUrl = "",
            [Summary("title-color", "The title color as a name, hex, or RGB. Defaults to yellow.")] string titleColor = "",
            [Summary("background-color", "The background color as a name, hex, or RGB. Defaults to dark gray.")] string backgroundColor = "",
            [Summary("text-color", "The text color as a name, hex, or RGB. Defaults to white.")] string textColor = "")
        {
            await DeferAsync();

            var (validBackground, background) = await ResolveColor(backgroundColor);
            var (validText, foreground) = validBackground ? await ResolveColor(textColor) : (false, null);
            var (validTitle, titleForeground) = validBackground && validText ? await ResolveColor(titleColor) : (false, null);
            if (!validBackground || !validText || !validTitle)
            {
                return;
            }

            await GenerateAndSendAsync(icon, iconUrl, "icon", "achievement.png",
                url => AchievementImageGenerator.GenerateAchievementImage(title, description, url, background, foreground, titleForeground));
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
        /// Resolves a user provided color name, hex, or RGB value. Sends an error followup if the color is invalid.
        /// </summary>
        /// <param name="input">The user provided color, or an empty string for no custom color.</param>
        /// <returns>Whether the color is valid, and the color or null if none was provided.</returns>
        private async Task<(bool Valid, SKColor? Color)> ResolveColor(string input)
        {
            if (input == "")
            {
                return (true, null);
            }

            Color? color = Colors.TryGetColor(input);
            if (color == null)
            {
                await FollowupAsync(text: $"❌ `{input}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex and RGB codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                return (false, null);
            }

            return (true, new SKColor(color.Value.R, color.Value.G, color.Value.B));
        }

        /// <summary>
        /// Resolves the image to use, generates an image from it, and sends the result as a followup attachment.
        /// Sends an error followup if the image cannot be resolved or loaded.
        /// </summary>
        /// <param name="attachment">The uploaded image, or null.</param>
        /// <param name="url">The user provided URL, or an empty string.</param>
        /// <param name="name">The name of the image option, used in error messages.</param>
        /// <param name="fileName">The file name of the sent image.</param>
        /// <param name="generate">Generates the image from the resolved image URL, returning null if the image could not be loaded.</param>
        private async Task GenerateAndSendAsync(IAttachment attachment, string url, string name, string fileName, Func<string, Task<SKBitmap>> generate)
        {
            url = await ResolveImageUrl(attachment, url, name);
            if (url == null)
            {
                return;
            }

            try
            {
                using var image = await generate(url);

                if (image == null)
                {
                    await FollowupAsync($"❌ Your provided {name} could not be loaded. Make sure it is a valid image (up to {ImageCache.MaxPixels / 1_000_000} megapixels).", ephemeral: true);
                    return;
                }

                // Encode the image and rewind the stream before sending
                using var ms = new MemoryStream();
                image.Encode(ms, SKEncodedImageFormat.Png, 100);
                ms.Seek(0, SeekOrigin.Begin);

                // Send the image as a Discord message attachment
                await FollowupWithFileAsync(ms, fileName);
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true);
            }
        }
    }
}