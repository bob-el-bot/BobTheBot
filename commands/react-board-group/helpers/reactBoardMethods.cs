using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bob.Database.Types;
using Discord;
using Microsoft.Extensions.Caching.Memory;

namespace Bob.Commands.Helpers
{
    public static class ReactBoardMethods
    {
        private static readonly MemoryCache ReactBoardCache = new MemoryCache(new MemoryCacheOptions());

        private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(12)
        };

        public static async Task<HashSet<ulong>> GetReactBoardMessageIdsAsync(ITextChannel boardChannel)
        {
            if (ReactBoardCache.TryGetValue(boardChannel.Id, out HashSet<ulong> cachedIds))
            {
                return cachedIds;
            }

            var messageIds = new LinkedList<ulong>();

            var messages = await boardChannel.GetMessagesAsync(limit: 50).FlattenAsync();

            foreach (var message in messages)
            {
                foreach (var embed in message.Embeds)
                {
                    var footerText = embed.Footer?.Text;
                    if (footerText != null)
                    {
                        var match = Regex.Match(footerText, @"ID:\s*(\d+)");
                        if (match.Success && ulong.TryParse(match.Groups[1].Value, out var id))
                        {
                            messageIds.AddLast(id);
                        }
                    }
                }
            }

            // Keep only the latest 10
            while (messageIds.Count > 10)
            {
                messageIds.RemoveFirst();
            }

            var finalSet = new HashSet<ulong>(messageIds);
            ReactBoardCache.Set(boardChannel.Id, finalSet, CacheOptions);

            return finalSet;
        }

        public static void AddToCache(ITextChannel boardChannel, ulong messageId)
        {
            if (!ReactBoardCache.TryGetValue(boardChannel.Id, out HashSet<ulong> messageIds))
            {
                messageIds = new HashSet<ulong>();
            }

            // Enforce the 10-message limit
            if (messageIds.Count >= 10)
            {
                // Remove the oldest item (approximation)
                messageIds.Remove(messageIds.First());
            }

            messageIds.Add(messageId);

            ReactBoardCache.Set(boardChannel.Id, messageIds, CacheOptions);
        }

        public static async Task<bool> IsMessageOnBoardAsync(ITextChannel boardChannel, ulong originalMessageId)
        {
            var boardMessageIds = await GetReactBoardMessageIdsAsync(boardChannel);
            return boardMessageIds.Contains(originalMessageId);
        }

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