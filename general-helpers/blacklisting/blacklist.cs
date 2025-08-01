using System;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Moderation
{
    public static class BlackList
    {
        private static readonly ulong AutoModerationChannelId = 1244738682825478185;
        private static readonly ulong ReportChannelId = 1245179479123562507;
        private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

        public enum Punishment
        {
            FiveMinutes,
            OneHour,
            OneDay,
            OneWeek,
            OneMonth,
            Permanent
        }

        /// <summary>
        /// Attempts to apply a temporary ban to a user.
        /// </summary>
        /// <param name="id">The ID of the user to ban.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <returns>The blacklisted user object.</returns>
        public static async Task<BlackListUser> StepBanUser(ulong id, string reason)
        {
            var user = await GetUser(id);

            Punishment nextPunishment = user != null ? GetNextPunishment(user.Expiration) : Punishment.FiveMinutes;

            return await BlackListUser(user, id, reason, nextPunishment);
        }

        /// <summary>
        /// Notifies about a user being banned.
        /// </summary>
        /// <param name="id">The ID of the banned user.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <param name="punishment">The punishment duration.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task NotifyBan(ulong id, string reason, Punishment punishment)
        {
            if (Bot.Client.GetChannel(AutoModerationChannelId) is IMessageChannel channel)
            {
                var message = punishment != Punishment.Permanent
                    ? $"`User: {id} has been banned for {punishment}`\n**Reason(s):**\n```{reason}```"
                    : $"`User {id} is recommended for a permanent ban`\n**Reason(s):**\n```{reason}```\n<@&1111721827807543367>";

                var components = new ComponentBuilder()
                    .WithButton(label: "See Details", customId: $"listBanDetails:{id}", style: ButtonStyle.Primary, emote: Emoji.Parse("🔎"))
                    .WithButton(label: "Remove Ban", customId: $"removeBan:{id}", style: ButtonStyle.Success, emote: Emoji.Parse("⚖️"));

                if (punishment == Punishment.Permanent)
                {
                    components.WithButton(label: "Permanently Ban", customId: $"permanentlyBan:{id}", style: ButtonStyle.Danger, emote: Emoji.Parse("⛓️"));
                }

                await channel.SendMessageAsync(message, components: components.Build());
            }
        }

        /// <summary>
        /// Notifies about a user being reported.
        /// </summary>
        /// <param name="id">The ID of the reported user.</param>
        /// <param name="message">The report message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task NotifyUserReport(ulong id, string message)
        {
            if (Bot.Client.GetChannel(ReportChannelId) is IMessageChannel channel)
            {
                var selectMenu = new SelectMenuBuilder
                {
                    MinValues = 1,
                    MaxValues = 1,
                    CustomId = $"banUser:{id}:{message}",
                    Placeholder = "Punish for...",
                };

                foreach (var time in Enum.GetValues<Punishment>())
                {
                    selectMenu.AddOption(new SelectMenuOptionBuilder
                    {
                        Label = time.ToString(),
                        Value = ((int)time).ToString()
                    });
                }

                var components = new ComponentBuilder().WithSelectMenu(selectMenu);
                await channel.SendMessageAsync($"`User {id} was reported.`\n**Message:**\n```{message}```\n<@&1111721827807543367>", components: components.Build());
            }
        }

        /// <summary>
        /// Notifies about a message being reported.
        /// </summary>
        /// <param name="dmChannelId">The ID of the direct message channel where the reported message is.</param>
        /// <param name="messageId">The ID of the reported message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task NotifyMessageReport(ulong dmChannelId, ulong messageId)
        {
            if (Bot.Client.GetChannel(ReportChannelId) is IMessageChannel channel)
            {
                var message = await Bot.Client.GetDMChannelAsync(dmChannelId).Result.GetMessageAsync(messageId);
                await channel.SendMessageAsync($"`Message was reported.`\n```{message.Content.Replace(ConfessFiltering.linkWarningMessage, "").Replace(ConfessFiltering.notificationMessage, "")}```\n<@&1111721827807543367>");
            }
        }

        /// <summary>
        /// Determines the next punishment based on the expiration date.
        /// </summary>
        /// <param name="expiration">The expiration date of the current punishment.</param>
        /// <returns>The next punishment.</returns>
        private static Punishment GetNextPunishment(DateTime? expiration)
        {
            if (expiration == null || expiration > DateTime.Now.AddMonths(1))
            {
                return Punishment.Permanent;
            }

            var remainingTime = expiration - DateTime.Now;

            if (remainingTime <= TimeSpan.FromMinutes(5))
            {
                return Punishment.OneHour;
            }

            if (remainingTime <= TimeSpan.FromHours(1))
            {
                return Punishment.OneDay;
            }

            if (remainingTime <= TimeSpan.FromDays(1))
            {
                return Punishment.OneWeek;
            }

            if (remainingTime <= TimeSpan.FromDays(7))
            {
                return Punishment.OneMonth;
            }

            return Punishment.Permanent;
        }

        /// <summary>
        /// Adds a user to the blacklist or updates existing user information.
        /// </summary>
        /// <param name="user">The user to be blacklisted or updated.</param>
        /// <param name="id">The ID of the user.</param>
        /// <param name="reason">The reason for blacklisting.</param>
        /// <param name="duration">The duration of the punishment.</param>
        /// <returns>The blacklisted user object.</returns>
        public static async Task<BlackListUser> BlackListUser(BlackListUser user, ulong id, string reason, Punishment duration)
        {
            DateTime? expiration = GetExpiration(duration);

            if (user == null)
            {
                user = new BlackListUser
                {
                    Id = id,
                    Reason = reason,
                    Expiration = expiration
                };
            }
            else
            {
                user.Reason = $"{user.Reason}\n{reason}";
                user.Expiration = duration != Punishment.Permanent ? expiration : GetExpiration(Punishment.OneMonth);
            }

            await UpdateUser(user);

            await NotifyBan(user.Id, reason, duration != Punishment.Permanent ? duration : Punishment.Permanent);

            return user;
        }

        /// <summary>
        /// Checks if a user is currently blacklisted.
        /// </summary>
        /// <param name="id">The ID of the user to check.</param>
        /// <returns>True if the user is blacklisted; otherwise, false.</returns>
        public static async Task<bool> IsBlacklisted(ulong id)
        {
            var user = await GetUser(id);

            if (user != null)
            {
                if (user.Expiration == null || user.Expiration > DateTime.Now)
                {
                    return true;
                }

                await RemoveUser(id);
            }

            return false;
        }

        /// <summary>
        /// Gets the expiration date based on the duration of the punishment.
        /// </summary>
        /// <param name="duration">The duration of the punishment.</param>
        /// <returns>The expiration date.</returns>
        public static DateTime? GetExpiration(Punishment duration)
        {
            DateTime now = DateTime.UtcNow;

            return duration switch
            {
                Punishment.FiveMinutes => now.AddMinutes(5),
                Punishment.OneHour => now.AddHours(1),
                Punishment.OneDay => now.AddDays(1),
                Punishment.OneWeek => now.AddDays(7),
                Punishment.OneMonth => now.AddMonths(1),
                Punishment.Permanent => DateTime.MaxValue,
                _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null)
            };
        }

        /// <summary>
        /// Gets the details of a user from the blacklist.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>A task representing the asynchronous operation, returning the blacklisted user if found; otherwise, null.</returns>
        public static async Task<BlackListUser> GetUser(ulong id)
        {
            if (Cache.TryGetValue(id, out BlackListUser cachedUser))
            {
                return cachedUser;
            }

            using var scope = Bot.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            var user = await context.GetUserFromBlackList(id);

            if (user != null)
            {
                Cache.Set(id, user, new MemoryCacheEntryOptions { AbsoluteExpiration = user.Expiration });
            }

            return user;
        }

        /// <summary>
        /// Removes a user from the blacklist.
        /// </summary>
        /// <param name="user">The user to remove from the blacklist.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RemoveUser(BlackListUser user)
        {
            using var scope = Bot.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            await context.RemoveUserFromBlackList(user);

            RemoveFromCache(user.Id);
        }

        /// <summary>
        /// Removes a user from the blacklist by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to remove from the blacklist.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RemoveUser(ulong id)
        {
            var user = await GetUser(id);
            if (user != null)
            {
                using var scope = Bot.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                await context.RemoveUserFromBlackList(user);
            }

            RemoveFromCache(id);
        }

        /// <summary>
        /// Updates the information of a blacklisted user.
        /// </summary>
        /// <param name="user">The updated user information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task UpdateUser(BlackListUser user)
        {
            user.Expiration = user.Expiration?.ToUniversalTime();

            var dbUser = await GetUser(user.Id);
            if (dbUser == null)
            {
                using var scope = Bot.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                await context.AddUserToBlackList(user);
            }
            else
            {
                using var scope = Bot.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                await context.SaveChangesAsync();
            }

            UpdateCache(user);
        }

        private static void RemoveFromCache(ulong id)
        {
            Cache.Remove(id);
        }

        private static void UpdateCache(BlackListUser user)
        {
            Cache.Set(user.Id, user, new MemoryCacheEntryOptions { AbsoluteExpiration = user.Expiration });
        }
    }
}
