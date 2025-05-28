using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bob.Database.Types;
using Discord;

namespace Bob.Commands.Helpers
{
    public static class ReactBoardMethods
    {
        public static bool isSetup(Server server) {
        public static bool isSetup(Server server)
        {
            return server.ReactBoardOn && server.ReactBoardChannelId.HasValue && server.ReactBoardEmoji != null && server.ReactBoardEmoji.Length > 0;
        }

        public static List<Embed> GetReactBoardEmbeds(Server server, IUserMessage reactedMessage, IGuildChannel sourceChannel)
        {
            // Define a common URL for grouping
            string commonUrl = "https://attachments.bobthebot.net";

            // Get image attachments
            var imageAttachments = reactedMessage.Attachments
                .Where(a => a.ContentType != null && a.ContentType.StartsWith("image/"))
                .ToList();

            // Prepare the main embed builder
            var mainEmbedBuilder = new EmbedBuilder()
                .WithAuthor(reactedMessage.Author.Username, reactedMessage.Author.GetAvatarUrl() ?? reactedMessage.Author.GetDefaultAvatarUrl())
                .WithDescription(reactedMessage.Content)
                .WithFooter(footer =>
                {
                    footer.WithText($"ID: {reactedMessage.Id} • {reactedMessage.CreatedAt.LocalDateTime.ToString("F")}");
                })
                .WithColor(Color.Orange)
                .WithUrl(commonUrl);

            var allEmbeds = new List<Embed>();

            if (imageAttachments.Count == 1)
            {
                // If there's only one image, embed it in the main embed
                mainEmbedBuilder.WithImageUrl(imageAttachments.First().Url);
                allEmbeds.Add(mainEmbedBuilder.Build());
            }
            else
            {
                // Multiple images: main embed first (without image)
                allEmbeds.Add(mainEmbedBuilder.Build());

                // Add each image as a separate embed
                var imageEmbeds = imageAttachments
                    .Select(a => new EmbedBuilder()
                        .WithImageUrl(a.Url)
                        .WithUrl(commonUrl)
                        .Build());

                allEmbeds.AddRange(imageEmbeds);
            }

            return allEmbeds;
        }

        public static MessageComponent GetReactBoardComponents(IUserMessage userMessage)
        {
            var jumpUrl = userMessage.GetJumpUrl();

            return new ComponentBuilder()
                .WithButton("View Original", null, ButtonStyle.Link, url: jumpUrl)
                .Build();
        }
    }
}