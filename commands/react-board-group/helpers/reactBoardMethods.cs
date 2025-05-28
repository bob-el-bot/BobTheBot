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
    /// <summary>
    /// Provides helper methods for managing the ReactBoard feature, including caching, embed generation, and UI components.
    /// </summary>
    public static partial class ReactBoardMethods
    {
        /// <summary>
        /// In-memory cache for storing ReactBoard message IDs per channel.
        /// </summary>
        private static readonly MemoryCache ReactBoardCache = new(new MemoryCacheOptions());

        /// <summary>
        /// Cache entry options with a sliding expiration of 12 hours.
        /// </summary>
        private static readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromHours(12)
        };

        /// <summary>
        /// Retrieves the set of message IDs currently on the ReactBoard for the specified channel.
        /// Fetches from cache if available, otherwise queries the latest 20 messages in the channel.
        /// </summary>
        /// <param name="boardChannel">The channel associated with the ReactBoard.</param>
        /// <returns>A set of message IDs on the ReactBoard.</returns>
        public static async Task<HashSet<ulong>> GetReactBoardMessageIdsAsync(ITextChannel boardChannel)
        {
            if (ReactBoardCache.TryGetValue(boardChannel.Id, out LinkedList<ulong> cachedList))
            {
                return [.. cachedList];
            }

            var messageIds = new LinkedList<ulong>();
            var messages = await boardChannel.GetMessagesAsync(limit: 20).FlattenAsync();
            var messageIdRegex = jumpToUrlMessageIdRegex();

            foreach (var message in messages)
            {
                if (message.Components.FirstOrDefault() is ActionRowComponent row &&
                    row.Components.FirstOrDefault() is ButtonComponent button &&
                    button.Style == ButtonStyle.Link &&
                    button.Url is not null)
                {
                    var match = messageIdRegex.Match(button.Url);
                    if (match.Success && ulong.TryParse(match.Groups[1].Value, out ulong id))
                    {
                        if (!messageIds.Contains(id))
                        {
                            messageIds.AddLast(id);
                        }
                    }
                }
            }

            // Limit to last 20
            while (messageIds.Count > 20)
            {
                messageIds.RemoveLast();
            }

            ReactBoardCache.Set(boardChannel.Id, messageIds, CacheOptions);

            return [.. messageIds];
        }

        /// <summary>
        /// Adds a message ID to the cache for the specified ReactBoard channel.
        /// If the cache for the channel exceeds 10 messages, the oldest is removed.
        /// </summary>
        /// <param name="boardChannel">The channel associated with the ReactBoard.</param>
        /// <param name="messageId">The message ID to add.</param>
        public static void AddToCache(ITextChannel boardChannel, ulong messageId)
        {
            if (!ReactBoardCache.TryGetValue(boardChannel.Id, out LinkedList<ulong> messageIds))
            {
                messageIds = new LinkedList<ulong>();
            }

            messageIds.Remove(messageId);
            messageIds.AddFirst(messageId);

            // Limit to last 20
            while (messageIds.Count > 20)
            {
                messageIds.RemoveLast();
            }

            ReactBoardCache.Set(boardChannel.Id, messageIds, CacheOptions);
        }

        /// <summary>
        /// Checks if a message is already present on the ReactBoard for the specified channel.
        /// </summary>
        /// <param name="boardChannel">The channel associated with the ReactBoard.</param>
        /// <param name="originalMessageId">The message ID to check.</param>
        /// <returns>True if the message is on the ReactBoard, false otherwise.</returns>
        public static async Task<bool> IsMessageOnBoardAsync(ITextChannel boardChannel, ulong originalMessageId)
        {
            var boardMessageIds = await GetReactBoardMessageIdsAsync(boardChannel);
            return boardMessageIds.Contains(originalMessageId);
        }

        /// <summary>
        /// Checks if the server has a valid ReactBoard setup, including channel ID and emoji.
        /// </summary>
        /// <param name="server">The server configuration to check.</param>
        /// <returns>True if the ReactBoard is properly set up, false otherwise.</returns>
        public static bool IsSetup(Server server)
        {
            return server.ReactBoardOn && server.ReactBoardChannelId.HasValue && server.ReactBoardEmoji != null && server.ReactBoardEmoji.Length > 0;
        }

        /// <summary>
        /// Generates a list of embeds for a given message, formatted for the ReactBoard.
        /// If the message has multiple images, each is embedded separately.
        /// </summary>
        /// <param name="reactedMessage">The original message that was reacted to.</param>
        /// <returns>A list of Discord embeds representing the message and its images.</returns>
        public static List<Embed> GetReactBoardEmbeds(IUserMessage reactedMessage)
        {
            string commonUrl = "https://attachments.bobthebot.net";

            var imageAttachments = reactedMessage.Attachments
                .Where(a => a.ContentType != null && a.ContentType.StartsWith("image/"))
                .ToList();

            var nonImageAttachments = reactedMessage.Attachments
                .Where(a => a.ContentType == null || !a.ContentType.StartsWith("image/"))
                .ToList();

            var mainEmbedBuilder = new EmbedBuilder()
                .WithAuthor(reactedMessage.Author.Username, reactedMessage.Author.GetAvatarUrl() ?? reactedMessage.Author.GetDefaultAvatarUrl())
                .WithDescription(reactedMessage.Content ?? "*No text content*")
                .WithFooter(footer =>
                {
                    footer.WithText($"{reactedMessage.CreatedAt.LocalDateTime:F}");
                })
                .WithColor(Bot.theme)
                .WithUrl(commonUrl);

            if (nonImageAttachments.Count != 0)
            {
                foreach (var attachment in nonImageAttachments)
                {
                    mainEmbedBuilder.AddField("Attachment", $"[{attachment.Filename}]({attachment.Url})");
                }
            }

            var allEmbeds = new List<Embed>();

            if (imageAttachments.Count == 1)
            {
                mainEmbedBuilder.WithImageUrl(imageAttachments.First().Url);
                allEmbeds.Add(mainEmbedBuilder.Build());
            }
            else
            {
                allEmbeds.Add(mainEmbedBuilder.Build());

                var imageEmbeds = imageAttachments
                    .Select(a => new EmbedBuilder()
                        .WithImageUrl(a.Url)
                        .WithUrl(commonUrl)
                        .Build());

                allEmbeds.AddRange(imageEmbeds);
            }

            return allEmbeds;
        }

        /// <summary>
        /// Generates a message component containing a "View Original" button linking to the original message.
        /// </summary>
        /// <param name="userMessage">The original user message to link to.</param>
        /// <returns>A message component with a link button.</returns>
        public static MessageComponent GetReactBoardComponents(IUserMessage userMessage)
        {
            var jumpUrl = userMessage.GetJumpUrl();

            return new ComponentBuilder()
                .WithButton("Jump to Message", null, ButtonStyle.Link, url: jumpUrl)
                .Build();
        }

        [GeneratedRegex(@"https:\/\/discord\.com\/channels\/\d+\/\d+\/(\d+)", RegexOptions.Compiled)]
        private static partial Regex jumpToUrlMessageIdRegex();
    }
}
