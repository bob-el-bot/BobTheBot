using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bob.Database;
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

        private static readonly ConcurrentDictionary<ulong, object> ChannelLocks = new();

        /// <summary>
        /// Retrieves the lock object associated with the specified channel ID.
        /// Ensures thread-safe access for operations related to the channel in the ReactBoard cache.
        /// </summary>
        /// <param name="channelId">The unique identifier of the channel.</param>
        /// <returns>An object that serves as the lock for the specified channel.</returns>

        private static object GetLockForChannel(ulong channelId)
        {
            return ChannelLocks.GetOrAdd(channelId, _ => new object());
        }

        /// <summary>
        /// Retrieves the set of message IDs currently on the ReactBoard for the specified channel.
        /// Checks cache, then DB, then falls back to fetching from Discord if needed.
        /// </summary>
        /// <param name="boardChannel">The channel associated with the ReactBoard.</param>
        /// <param name="guildId">The guild/server ID.</param>
        /// <returns>A set of message IDs on the ReactBoard.</returns>
        public static async Task<HashSet<ulong>> GetReactBoardMessageIdsAsync(ITextChannel boardChannel, ulong guildId)
        {
            var lockObj = GetLockForChannel(boardChannel.Id);

            // 1. Try cache
            lock (lockObj)
            {
                if (ReactBoardCache.TryGetValue(boardChannel.Id, out LinkedList<ulong> cachedList))
                {
                    return [.. cachedList];
                }
            }

            // 2. Try DB
            using (var db = new BobEntities())
            {
                var allreactBoardMessages = await db.GetAllReactBoardMessagesForGuildAsync(guildId);

                if (allreactBoardMessages.Count > 0)
                {
                    var messageIds = new LinkedList<ulong>(allreactBoardMessages.Select(x => x.OriginalMessageId));
                    lock (lockObj)
                    {
                        ReactBoardCache.Set(boardChannel.Id, messageIds, CacheOptions);
                    }
                    return [.. messageIds];
                }
            }

            Console.WriteLine($"[ReactBoard] No cached or DB entries found for channel {boardChannel.Id} in guild {guildId}. Fetching from Discord...");

            // 3. Fallback: Fetch from Discord, parse, write to DB, cache
            var messageIdsFromDiscord = new LinkedList<ulong>();
            var messages = await boardChannel.GetMessagesAsync(limit: 50).FlattenAsync();
            var messageIdRegex = JumpToUrlMessageIdRegex();
            var dbInserts = new List<ReactBoardMessage>();

            foreach (var message in messages)
            {
                if (message.Components.FirstOrDefault() is ActionRowComponent row &&
                    row.Components.FirstOrDefault() is ButtonComponent button &&
                    button.Style == ButtonStyle.Link &&
                    button.Url is not null)
                {
                    var match = messageIdRegex.Match(button.Url);
                    if (match.Success && ulong.TryParse(match.Groups[1].Value, out ulong origId))
                    {
                        if (!messageIdsFromDiscord.Contains(origId))
                        {
                            messageIdsFromDiscord.AddLast(origId);
                            dbInserts.Add(new ReactBoardMessage
                            {
                                GuildId = guildId,
                                OriginalMessageId = origId
                            });
                        }
                    }
                }
            }

            // Write to DB
            if (dbInserts.Count > 0)
            {
                using var db = new BobEntities();
                await db.AddMultipleReactBoardMessagesAsync(dbInserts);
            }

            // Cache
            lock (lockObj)
            {
                ReactBoardCache.Set(boardChannel.Id, messageIdsFromDiscord, CacheOptions);
            }

            return [.. messageIdsFromDiscord];
        }

        /// <summary>
        /// Adds a message ID to the cache and DB for the specified ReactBoard channel.
        /// </summary>
        /// <param name="boardChannel">The channel associated with the ReactBoard.</param>
        /// <param name="originalMessageId">The original message ID.</param>
        /// <param name="boardMessageId">The message ID in the board channel.</param>
        public static async Task AddToCacheAndDbAsync(ITextChannel boardChannel, ulong originalMessageId)
        {
            var lockObj = GetLockForChannel(boardChannel.Id);
            lock (lockObj)
            {
                if (!ReactBoardCache.TryGetValue(boardChannel.Id, out LinkedList<ulong> messageIds))
                {
                    messageIds = new LinkedList<ulong>();
                }

                messageIds.Remove(originalMessageId);
                messageIds.AddFirst(originalMessageId);

                while (messageIds.Count > 50)
                {
                    messageIds.RemoveLast();
                }

                ReactBoardCache.Set(boardChannel.Id, messageIds, CacheOptions);
            }

            using var db = new BobEntities();
            var existing = await db.GetReactBoardMessageAsync(originalMessageId);
            if (existing == null)
            {
                var entry = new ReactBoardMessage
                {
                    GuildId = boardChannel.GuildId,
                    OriginalMessageId = originalMessageId
                };

                await db.AddReactBoardMessageAsync(entry);
            }
        }

        /// <summary>
        /// Checks if a message is already present on the ReactBoard for the specified channel.
        /// </summary>
        /// <param name="boardChannel">The channel associated with the ReactBoard.</param>
        /// <param name="originalMessageId">The message ID to check.</param>
        /// <returns>True if the message is on the ReactBoard, false otherwise.</returns>
        public static async Task<bool> IsMessageOnBoardAsync(ITextChannel boardChannel, ulong originalMessageId)
        {
            var boardMessageIds = await GetReactBoardMessageIdsAsync(boardChannel, boardChannel.GuildId);
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
                    footer.WithText(
    $"{reactedMessage.CreatedAt.UtcDateTime.ToString("dddd, MMMM d, yyyy h:mm:ss tt 'UTC'", CultureInfo.InvariantCulture)}"
                    );
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

        /// <summary>
        /// Extracts the ID of a custom Discord emoji from its string representation.
        /// </summary>
        /// <param name="emojiString">The emoji string, typically in the format &lt;:name:id&gt;.</param>
        /// <returns>
        /// The emoji ID as a string if the input matches the expected format; otherwise, <c>null</c>.
        /// </returns>
        public static string GetEmojiIdFromString(string emojiString)
        {
            var match = EmojiIdRegex().Match(emojiString);
            return match.Success ? match.Groups[1].Value : null;
        }

        [GeneratedRegex(@"https:\/\/discord\.com\/channels\/\d+\/\d+\/(\d+)", RegexOptions.Compiled)]
        private static partial Regex JumpToUrlMessageIdRegex();

        [GeneratedRegex(@"<:.+?:(\d+)>")]
        private static partial Regex EmojiIdRegex();
    }
}
