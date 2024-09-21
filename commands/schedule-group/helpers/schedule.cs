using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColorMethods;
using Database;
using Database.Types;
using Discord;
using Microsoft.EntityFrameworkCore;
using TimeStamps;

namespace Commands.Helpers
{
    public interface IScheduledItem
    {
        ulong Id { get; }
        ulong UserId { get; }
        ulong ChannelId { get; }
        DateTime TimeToSend { get; }
    }

    /// <summary>
    /// Contains helper methods for scheduling and managing scheduled messages and announcements.
    /// </summary>
    public static class Schedule
    {
        // Dictionary to keep track of scheduled tasks using their message ID
        public static readonly Dictionary<ulong, CancellationTokenSource> ScheduledTasks = new();

        private static readonly TimeSpan MaxDelay = TimeSpan.FromDays(30);

        /// <summary>
        /// Converts a local time specified by month, day, hour, and minute into UTC time, based on the provided timezone.
        /// </summary>
        /// <param name="month">The month of the local time.</param>
        /// <param name="day">The day of the local time.</param>
        /// <param name="hour">The hour of the local time (24-hour format).</param>
        /// <param name="minute">The minute of the local time.</param>
        /// <param name="timezone">The timezone in which the local time is specified.</param>
        /// <returns>The equivalent UTC time of the specified local time.</returns>
        public static DateTime ConvertToUtcTime(int month, int day, int hour, int minute, TimeStamp.Timezone timezone)
        {
            var localDateTime = new DateTime(DateTime.UtcNow.Year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            var timeZoneId = TimeStamp.TimezoneMappings[timezone];
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);
        }

        /// <summary>
        /// Builds an embed for editing a scheduled item.
        /// </summary>
        /// <param name="item">The scheduled item to build the embed for.</param>
        /// <returns>The configured EmbedBuilder instance.</returns>
        public static EmbedBuilder BuildEditEmbed(IScheduledItem item)
        {
            var embedBuilder = new EmbedBuilder
            {
                Title = item is ScheduledMessage ? $"🕖 Editing Message | {item.Id}" : $"🕖 Editing Announcement | {item.Id}",
                Description = item is ScheduledMessage message ? message.Message : (item as ScheduledAnnouncement)?.Description,
                Color = item is ScheduledMessage ? Bot.theme : Colors.TryGetColor((item as ScheduledAnnouncement)?.Color)
            };

            if (item is ScheduledAnnouncement announcement)
            {
                embedBuilder.AddField(name: "Title", value: announcement.Title);
            }

            embedBuilder.AddField(name: "Time", value: $"{TimeStamp.FromDateTime(item.TimeToSend, TimeStamp.Formats.Exact)}");

            return embedBuilder;
        }

        /// <summary>
        /// Builds the component buttons for editing and deleting a scheduled item.
        /// </summary>
        /// <param name="item">The scheduled item to build the components for.</param>
        /// <param name="disabled">Whether the buttons should be disabled.</param>
        /// <returns>The configured ComponentBuilder instance.</returns>
        public static ComponentBuilder BuildEditMessageComponents(IScheduledItem item, bool disabled = false)
        {
            string type = item == null ? "null" : (item is ScheduledMessage ? "Message" : "Announce");
            string itemId = item?.Id.ToString() ?? "null";

            return new ComponentBuilder()
                .WithButton(
                    label: "Edit",
                    customId: $"edit{type}Button:{itemId}",
                    style: ButtonStyle.Primary,
                    emote: Emoji.Parse("✍️"),
                    disabled: disabled
                )
                .WithButton(
                    label: "Delete",
                    customId: $"delete{type}Button:{itemId}",
                    style: ButtonStyle.Danger,
                    emote: Emoji.Parse("🗑️"),
                    disabled: disabled
                );
        }

        /// <summary>
        /// Schedules a task to send a scheduled item at its specified time.
        /// </summary>
        /// <typeparam name="T">The type of scheduled item (message or announcement).</typeparam>
        /// <param name="scheduledItem">The scheduled item to be sent.</param>
        public static void ScheduleTask<T>(T scheduledItem) where T : IScheduledItem
        {
            var delay = scheduledItem.TimeToSend - DateTime.UtcNow;

            if (delay <= TimeSpan.Zero)
            {
                // If the item should already have been sent, send it immediately.
                SendScheduledItem(scheduledItem).Wait();
                return;
            }

            var cts = new CancellationTokenSource();
            ScheduledTasks[scheduledItem.Id] = cts; // Cache CancellationTokenSource only when necessary

            if (delay > MaxDelay)
            {
                // Schedule in chunks to handle long delays
                _ = ScheduleInChunks(scheduledItem, delay, cts.Token);
            }
            else
            {
                // Schedule the item with a single delay
                _ = Task.Delay(delay, cts.Token).ContinueWith(async _ => await SendScheduledItem(scheduledItem), cts.Token);
            }
        }

        private static async Task ScheduleInChunks<T>(T scheduledItem, TimeSpan totalDelay, CancellationToken token) where T : IScheduledItem
        {
            while (totalDelay > MaxDelay)
            {
                await Task.Delay(MaxDelay, token); // Delay for max allowed chunk
                totalDelay -= MaxDelay;
            }
            await Task.Delay(totalDelay, token); // Delay for the remaining time
            await SendScheduledItem(scheduledItem);
        }

        /// <summary>
        /// Sends the scheduled item to the specified channel and removes it from the database.
        /// </summary>
        /// <typeparam name="T">The type of scheduled item (message or announcement).</typeparam>
        /// <param name="scheduledItem">The scheduled item to be sent.</param>
        private static async Task SendScheduledItem<T>(T scheduledItem) where T : IScheduledItem
        {
            try
            {
                using var context = new BobEntities();
                var channel = Bot.Client.GetChannel(scheduledItem.ChannelId) as IMessageChannel;

                if (channel == null)
                {
                    Console.WriteLine($"Channel with ID: {scheduledItem.ChannelId} not found.");
                    return;
                }

                var dbUser = await context.GetUser(scheduledItem.UserId);

                if (scheduledItem is ScheduledMessage message)
                {
                    var messageToSend = await context.GetScheduledMessage(scheduledItem.Id);
                    if (messageToSend != null)
                    {
                        await channel.SendMessageAsync(messageToSend.Message);
                        dbUser.TotalScheduledMessages -= 1;
                    }
                }
                else if (scheduledItem is ScheduledAnnouncement announcement)
                {
                    var announcementToSend = await context.GetScheduledAnnouncement(scheduledItem.Id);
                    if (announcementToSend != null)
                    {
                        var embed = await BuildAnnouncementEmbed(announcementToSend);
                        await channel.SendMessageAsync(embed: embed);
                        dbUser.TotalScheduledAnnouncements -= 1;
                    }
                }

                // Update and remove from database in one batch
                await context.UpdateUser(dbUser);
                await context.RemoveScheduledItem(scheduledItem);
                ScheduledTasks.Remove(scheduledItem.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while sending scheduled item: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all unsent scheduled items from the database and schedules them.
        /// </summary>
        /// <typeparam name="T">The type of scheduled item (message or announcement).</typeparam>
        public static async Task LoadAndScheduleItemsAsync<T>() where T : class, IScheduledItem
        {
            using var context = new BobEntities();
            var unsentItems = await context.Set<T>().ToListAsync();

            foreach (var item in unsentItems)
            {
                ScheduleTask(item);
            }
        }

        /// <summary>
        /// Builds the embed for an announcement.
        /// </summary>
        private static async Task<Embed> BuildAnnouncementEmbed(ScheduledAnnouncement announcement)
        {
            var user = await Bot.Client.GetUserAsync(announcement.UserId);

            var embed = new EmbedBuilder
            {
                Title = announcement.Title,
                Color = Colors.TryGetColor(announcement.Color),
                Description = Announcement.FormatDescription(announcement.Description),
                Footer = new EmbedFooterBuilder
                {
                    IconUrl = user.GetAvatarUrl(),
                    Text = $"Announced by {user.GlobalName}."
                }
            };

            return embed.Build();
        }
    }
}