using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Moderation
{
    public static class BlackList
    {
        private static readonly ulong AutoModerationChannelId = 1244738682825478185;
        private static readonly ulong ReportChannelId = 1245179479123562507;

        public enum Punishment
        {
            FiveMinutes,
            OneHour,
            OneDay,
            OneWeek,
            OneMonth,
            Permanent
        }

        public static async Task<BlackListUser> StepBanUser(ulong id, string reason, DiscordSocketClient client)
        {
            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(id);

            Punishment nextPunishment = Punishment.FiveMinutes;

            if (user != null)
            {
                nextPunishment = GetNextPunishment(user.Expiration);
            }

            return await BlackListUser(user, id, reason, nextPunishment, client);
        }

        private static async Task NotifyBan(ulong id, string reason, Punishment punishment, DiscordSocketClient client)
        {

            if (client.GetChannel(AutoModerationChannelId) is IMessageChannel channel)
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
                    message = $"`User {id} is recommended for a permanant ban`\n**Reason(s):**\n```{reason}```\n<@&1111721827807543367>";
                }

                await channel.SendMessageAsync(message, components: components.Build());
            }
        }

        private static async Task NotifyPermanentBan(ulong id, string reason, DiscordSocketClient client)
        {
            await NotifyBan(id, reason, Punishment.Permanent, client);
        }

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

        public static async Task NotifyMessageReport(string message)
        {
            if (Bot.Client.GetChannel(ReportChannelId) is IMessageChannel channel)
            {
                await channel.SendMessageAsync($"`Message was reported.`\n**Message:**\n```{message}```\n<@&1111721827807543367>");
            }
        }

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

        public static async Task<BlackListUser> BlackListUser(BlackListUser user, ulong id, string reason, Punishment duration, DiscordSocketClient client)
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
            }
            else
            {
                if (duration != Punishment.Permanent)
                {
                    user.Expiration = GetExpiration(duration);
                    user.Reason = $"{user.Reason}\n {reason}";
                    await context.UpdateUserFromBlackList(user);

                    await NotifyBan(user.Id, reason, duration, client);
                }
                else
                {
                    if (GetPunishmentFromExpiration(user.Expiration) != Punishment.Permanent)
                    {
                        user.Expiration = GetExpiration(Punishment.OneMonth);
                        await context.UpdateUserFromBlackList(user);

                        await NotifyPermanentBan(user.Id, reason, client);
                    }
                }
            }

            return user;
        }

        public static async Task UnblacklistUser(ulong id)
        {
            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(id);
            await context.RemoveUserFromBlackList(user);
        }

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
