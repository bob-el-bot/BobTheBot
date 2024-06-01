using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Moderation
{
    /// <summary>
    /// Provides functionality for managing the blacklist of users and applying punishments.
    /// </summary>
    public static class BlackList
    {
        private static readonly ulong AutoModerationChannelId = 1244738682825478185;
        private static readonly ulong ReportChannelId = 1245179479123562507;

        /// <summary>
        /// Defines the different types of punishments that can be applied to users.
        /// </summary>
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
        /// Increases the punishment for a user step by step.
        /// </summary>
        /// <param name="id">The ID of the user to ban.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <returns>A task representing the asynchronous operation, with a <see cref="BlackListUser"/> result.</returns>
        public static async Task<BlackListUser> StepBanUser(ulong id, string reason)
        {
            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(id);

            Punishment nextPunishment = Punishment.FiveMinutes;

            if (user != null)
            {
                nextPunishment = GetNextPunishment(user.Expiration);
            }

            return await BlackListUser(user, id, reason, nextPunishment);
        }

        /// <summary>
        /// Notifies the AutoModeration channel about a user's ban.
        /// </summary>
        /// <param name="id">The ID of the user being banned.</param>
        /// <param name="reason">The reason for the ban.</param>
        /// <param name="punishment">The type of punishment applied.</param>
        private static async Task NotifyBan(ulong id, string reason, Punishment punishment)
        {
            if (Bot.Client.GetChannel(AutoModerationChannelId) is IMessageChannel channel)
            {
                string message;
                ComponentBuilder components = new();
                components.WithButton(label: "See Details", customId: $"listBanDetails:{id}", style: ButtonStyle.Primary, emote: Emoji.Parse("🔎"));
                components.WithButton(label: "Remove Ban", customId: $"removeBan:{id}", style: ButtonStyle.Success, emote: Emoji.Parse("⚖️"));

                if (punishment != Punishment.Permanent)
                {
                    message = $"`User: {id} has been banned for {punishment}`\n**Reason(s):**\n```{reason}```";
                }
                else
                {
                    components.WithButton(label: "Permanently Ban", customId: $"permanentlyBan:{id}", style: ButtonStyle.Danger, emote: Emoji.Parse("⛓️"));
                    message = $"`User {id} is recommended for a permanent ban`\n**Reason(s):**\n```{reason}```\n<@&1111721827807543367>";
                }

                await channel.SendMessageAsync(message, components: components.Build());
            }
        }

        /// <summary>
        /// Notifies the AutoModeration channel about a user's permanent ban.
        /// </summary>
        /// <param name="id">The ID of the user being permanently banned.</param>
        /// <param name="reason">The reason for the ban.</param>
        private static async Task NotifyPermanentBan(ulong id, string reason)
        {
            await NotifyBan(id, reason, Punishment.Permanent);
        }

        /// <summary>
        /// Notifies the Report channel about a user report.
        /// </summary>
        /// <param name="id">The ID of the user being reported.</param>
        /// <param name="message">The report message.</param>
        public static async Task NotifyUserReport(ulong id, string message)
        {
            if (Bot.Client.GetChannel(ReportChannelId) is IMessageChannel channel)
            {
                ComponentBuilder components = new();

                var selectMenu = new SelectMenuBuilder
                {
                    MinValues = 1,
                    MaxValues = 1,
                    CustomId = $"banUser:{id}:{message}",
                    Placeholder = "Punish for...",
                };

                foreach (var time in Enum.GetValues(typeof(Punishment)))
                {
                    selectMenu.AddOption(new SelectMenuOptionBuilder
                    {
                        Label = time.ToString(),
                        Value = ((int)time).ToString()
                    });
                }

                components.WithSelectMenu(selectMenu);

                message = $"`User {id} was reported.`\n**Message:**\n```{message}```\n<@&1111721827807543367>";

                await channel.SendMessageAsync(message, components: components.Build());
            }
        }

        /// <summary>
        /// Notifies the Report channel about a message report.
        /// </summary>
        /// <param name="dmChannelId">The ID of the DM channel containing the reported message.</param>
        /// <param name="messageId">The ID of the reported message.</param>
        public static async Task NotifyMessageReport(ulong dmChannelId, ulong messageId)
        {
            if (Bot.Client.GetChannel(ReportChannelId) is IMessageChannel channel)
            {
                var message = await Bot.Client.GetDMChannelAsync(dmChannelId).Result.GetMessageAsync(messageId);

                await channel.SendMessageAsync($"`Message was reported.`\n```{message.Content.Replace(ConfessFiltering.linkWarningMessage, "").Replace(ConfessFiltering.notificationMessage, "")}```\n<@&1111721827807543367>");
            }
        }

        /// <summary>
        /// Determines the next punishment level based on the current expiration date.
        /// </summary>
        /// <param name="expiration">The expiration date of the current punishment.</param>
        /// <returns>The next level of punishment.</returns>
        private static Punishment GetNextPunishment(DateTime? expiration)
        {
            if (expiration == null || expiration > DateTime.UtcNow.AddMonths(1))
            {
                return Punishment.Permanent;
            }

            var remainingTime = expiration - DateTime.UtcNow;
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
        /// Blacklists a user and applies the specified punishment.
        /// </summary>
        /// <param name="user">The user to be blacklisted.</param>
        /// <param name="id">The ID of the user to be blacklisted.</param>
        /// <param name="reason">The reason for blacklisting the user.</param>
        /// <param name="duration">The duration of the punishment.</param>
        /// <returns>A task representing the asynchronous operation, with a <see cref="BlackListUser"/> result.</returns>
        public static async Task<BlackListUser> BlackListUser(BlackListUser user, ulong id, string reason, Punishment duration)
        {
            using var context = new BobEntities();

            if (user == null)
            {
                user = new BlackListUser
                {
                    Id = id,
                    Reason = reason,
                    Expiration = GetExpiration(duration)
                };

                await context.AddUserToBlackList(user);
                await NotifyBan(user.Id, reason, duration);
            }
            else
            {
                if (duration != Punishment.Permanent)
                {
                    user.Expiration = GetExpiration(duration);
                    user.Reason = $"{user.Reason}\n{reason}";
                    await context.UpdateUserFromBlackList(user);
                    await NotifyBan(user.Id, reason, duration);
                }
                else
                {
                    if (GetPunishmentFromExpiration(user.Expiration) != Punishment.Permanent)
                    {
                        user.Expiration = GetExpiration(Punishment.OneMonth);
                        await context.UpdateUserFromBlackList(user);
                        await NotifyPermanentBan(user.Id, reason);
                    }
                }
            }

            return user;
        }

        /// <summary>
        /// Removes a user from the blacklist.
        /// </summary>
        /// <param name="id">The ID of the user to be removed from the blacklist.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task UnblacklistUser(ulong id)
        {
            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(id);
            await context.RemoveUserFromBlackList(user);
        }

        /// <summary>
        /// Checks if a user is blacklisted.
        /// </summary>
        /// <param name="id">The ID of the user to check.</param>
        /// <returns>A task representing the asynchronous operation, with a result of <c>true</c> if the user is blacklisted; otherwise, <c>false</c>.</returns>
        public static async Task<bool> IsBlacklisted(ulong id)
        {
            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(id);

            if (user != null)
            {
                if (user.Expiration == null || user.Expiration > DateTime.UtcNow)
                {
                    return true;
                }

                // If the user's blacklist duration has expired, remove them from the database and cache
                await UnblacklistUser(id);
            }

            return false;
        }

        /// <summary>
        /// Gets the punishment level based on the expiration date.
        /// </summary>
        /// <param name="expiration">The expiration date of the current punishment.</param>
        /// <returns>The current level of punishment.</returns>
        private static Punishment GetPunishmentFromExpiration(DateTime? expiration)
        {
            if (expiration == null)
            {
                return Punishment.Permanent;
            }

            TimeSpan timeUntilExpiration = expiration.Value - DateTime.UtcNow;

            if (timeUntilExpiration <= TimeSpan.FromMinutes(5))
            {
                return Punishment.FiveMinutes;
            }
            else if (timeUntilExpiration <= TimeSpan.FromHours(1))
            {
                return Punishment.OneHour;
            }
            else if (timeUntilExpiration <= TimeSpan.FromDays(1))
            {
                return Punishment.OneDay;
            }
            else if (timeUntilExpiration <= TimeSpan.FromDays(7))
            {
                return Punishment.OneWeek;
            }
            else if (timeUntilExpiration <= TimeSpan.FromDays(31))
            {
                return Punishment.OneMonth;
            }
            else
            {
                return Punishment.Permanent;
            }
        }

        /// <summary>
        /// Gets the expiration date based on the duration of the punishment.
        /// </summary>
        /// <param name="duration">The duration of the punishment.</param>
        /// <returns>The expiration date of the punishment.</returns>
        public static DateTime? GetExpiration(Punishment duration)
        {
            return duration switch
            {
                Punishment.FiveMinutes => DateTime.UtcNow.AddMinutes(5),
                Punishment.OneHour => DateTime.UtcNow.AddHours(1),
                Punishment.OneDay => DateTime.UtcNow.AddDays(1),
                Punishment.OneWeek => DateTime.UtcNow.AddDays(7),
                Punishment.OneMonth => DateTime.UtcNow.AddMonths(1),
                Punishment.Permanent => DateTime.MaxValue,
                _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null)
            };
        }
    }
}
