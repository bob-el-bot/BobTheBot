using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// A cache for guild users.
    /// </summary>
    public static class CachedUsers
    {
        private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
        private static readonly ConcurrentDictionary<ulong, Task> OnGoingDownloads = new();

        /// <summary>
        /// Adds a guild ID to the cache with a 1-hour expiration. 
        /// If the guild ID is not already in the cache, it triggers a download of all users in the guild.
        /// Subsequent calls for the same guild will wait for the ongoing download to complete if already in progress.
        /// If the context is not in a guild, it will do nothing.
        /// </summary>
        /// <param name="ctx">The context to add the guild ID from.</param>
        public static async Task AddGuildUsersAsync(SocketInteractionContext ctx)
        {
            if (ctx.Guild != null) // Guild context
            {
                if (!Cache.TryGetValue(ctx.Guild.Id, out _))
                {
                    var downloadTask = OnGoingDownloads.GetOrAdd(ctx.Guild.Id, async _ =>
                    {
                        var guild = Bot.Client.GetGuild(ctx.Guild.Id);
                        if (guild != null)
                        {
                            await guild.DownloadUsersAsync();
                        }

                        Cache.Set(ctx.Guild.Id, true, TimeSpan.FromHours(1)); // Add to cache after completion
                        OnGoingDownloads.TryRemove(ctx.Guild.Id, out Task _); // Clean up
                    });

                    await downloadTask;
                }
            }
        }

        /// <summary>
        /// Adds a guild ID to the cache with a 1-hour expiration.
        /// If the guild ID is not already in the cache, it triggers a download of all users in the guild.
        /// Subsequent calls for the same guild will wait for the ongoing download to complete if already in progress.
        /// If the context is not in a guild, it will do nothing.
        /// </summary>
        /// <param name="ctx">The context to add the guild ID from.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task AddGuildUsersAsync(ShardedInteractionContext ctx)
        {
            if (ctx.Guild != null) // Guild context
            {
                if (!Cache.TryGetValue(ctx.Guild.Id, out _))
                {
                    var downloadTask = OnGoingDownloads.GetOrAdd(ctx.Guild.Id, async _ =>
                    {
                        var guild = Bot.Client.GetGuild(ctx.Guild.Id);
                        if (guild != null)
                        {
                            await guild.DownloadUsersAsync();
                        }

                        Cache.Set(ctx.Guild.Id, true, TimeSpan.FromHours(1)); // Add to cache after completion
                        OnGoingDownloads.TryRemove(ctx.Guild.Id, out Task _); // Clean up
                    });

                    await downloadTask;
                }
            }
        }

        /// <summary>
        /// Gets a random member from a specified Guild, DM, or Group DM context.
        /// </summary>
        /// <param name="ctx">The context to get the random member from.</param>
        /// <returns>A random member (SocketUser) from the context.</returns>
        public static SocketUser GetRandomMember(SocketInteractionContext ctx)
        {
            var members = GetMembers(ctx);
            if (members.Count != 0)
            {
                var random = new Random();
                return members[random.Next(members.Count)];
            }

            // Fallback: return the user who called the command (just in case)
            return ctx.User;
        }

        /// <summary>
        /// Gets a random member from a specified Guild, DM, or Group DM context.
        /// </summary>
        /// <param name="ctx">The context to get the random member from.</param>
        /// <returns>A random member (SocketUser) from the context.</returns>
        public static SocketUser GetRandomMember(ShardedInteractionContext ctx)
        {
            var members = GetMembers(ctx);
            if (members.Count != 0)
            {
                var random = new Random();
                return members[random.Next(members.Count)];
            }

            // Fallback: return the user who called the command (just in case)
            return ctx.User;
        }

        /// <summary>
        /// Helper method to retrieve the list of members from the context.
        /// </summary>
        /// <param name="ctx">The context to retrieve members from.</param>
        /// <returns>A list of members (SocketUser).</returns>
        private static List<SocketUser> GetMembers(SocketInteractionContext ctx)
        {
            if (ctx.Guild != null) // In a guild context
            {
                return ctx.Guild.Users.Cast<SocketUser>().ToList();
            }
            else if (ctx.Channel is SocketGroupChannel groupChannel) // In a group DM context
            {
                return groupChannel.Users.Cast<SocketUser>().ToList();
            }
            else if (ctx.Channel is SocketDMChannel dmChannel) // In a single DM context
            {
                return dmChannel.Users.ToList();
            }

            // Default fallback if no context found (this case should never be hit if context is valid)
            return [];
        }

        /// <summary>
        /// Helper method to retrieve the list of members from the context.
        /// </summary>
        /// <param name="ctx">The context to retrieve members from.</param>
        /// <returns>A list of members (SocketUser).</returns>
        private static List<SocketUser> GetMembers(ShardedInteractionContext ctx)
        {
            if (ctx.Guild != null) // In a guild context
            {
                return ctx.Guild.Users.Cast<SocketUser>().ToList();
            }
            else if (ctx.Channel is SocketGroupChannel groupChannel) // In a group DM context
            {
                return groupChannel.Users.Cast<SocketUser>().ToList();
            }
            else if (ctx.Channel is SocketDMChannel dmChannel) // In a single DM context
            {
                return dmChannel.Users.ToList();
            }

            // Default fallback if no context found (this case should never be hit if context is valid)
            return [];
        }
    }
}