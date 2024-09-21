using System;
using System.Linq;
using System.Threading.Tasks;
using ColorMethods;
using Database;
using Database.Types;
using Debug;
using Discord;
using Discord.Interactions;
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
            // Create a local DateTime object with the specified month, day, hour, and minute.
            var localDateTime = new DateTime(DateTime.UtcNow.Year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

            // Retrieve the system timezone ID from the TimezoneMappings dictionary.
            var timeZoneId = TimeStamp.TimezoneMappings[timezone];

            // Find the TimeZoneInfo object for the specified timezone.
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            // Convert the local DateTime to UTC using the TimeZoneInfo.
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
            TimeSpan maxDelay = TimeSpan.FromDays(30); // Maximum scheduling delay of 30 days
            var delay = scheduledItem.TimeToSend - DateTime.UtcNow;

            async Task ScheduleInChunks(TimeSpan totalDelay)
            {
                while (totalDelay > maxDelay)
                {
                    await Task.Delay(maxDelay);
                    totalDelay -= maxDelay;
                }
                await Task.Delay(totalDelay);
                await SendScheduledItem(scheduledItem);
            }

            if (delay <= TimeSpan.Zero)
            {
                SendScheduledItem(scheduledItem).Wait();
            }
            else if (delay > maxDelay)
            {
                _ = ScheduleInChunks(delay);
            }
            else
            {
                _ = Task.Delay(delay).ContinueWith(async _ => await SendScheduledItem(scheduledItem));
            }
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
                var channel = (IMessageChannel)await Bot.Client.GetChannelAsync(scheduledItem.ChannelId);

                if (channel == null)
                {
                    Console.WriteLine($"Channel with ID: {scheduledItem.ChannelId} not found.");
                    return;
                }

                var dbUser = await context.GetUser(scheduledItem.UserId);

                var timeSinceScheduled = DateTime.UtcNow - scheduledItem.TimeToSend;
                if (timeSinceScheduled > TimeSpan.FromHours(1))
                {
                    await context.RemoveScheduledItem(scheduledItem);

                    if (scheduledItem is ScheduledMessage)
                    {
                        dbUser.TotalScheduledMessages -= 1;
                    }
                    else if (scheduledItem is ScheduledAnnouncement)
                    {
                        dbUser.TotalScheduledAnnouncements -= 1;
                    }

                    await context.UpdateUser(dbUser);
                    return;
                }

                if (scheduledItem is ScheduledMessage scheduledMessage)
                {
                    await channel.SendMessageAsync(scheduledMessage.Message);
                    dbUser.TotalScheduledMessages -= 1;
                    await context.UpdateUser(dbUser);
                }
                else if (scheduledItem is ScheduledAnnouncement scheduledAnnouncement)
                {
                    dbUser.TotalScheduledAnnouncements -= 1;
                    await context.UpdateUser(dbUser);

                    var user = await Bot.Client.GetUserAsync(scheduledAnnouncement.UserId);

                    var embed = new EmbedBuilder
                    {
                        Title = scheduledAnnouncement.Title,
                        Color = Colors.TryGetColor(scheduledAnnouncement.Color),
                        Description = Announcement.FormatDescription(scheduledAnnouncement.Description),
                        Footer = new EmbedFooterBuilder
                        {
                            IconUrl = user.GetAvatarUrl(),
                            Text = $"Announced by {user.GlobalName}."
                        }
                    };

                    await channel.SendMessageAsync(embed: embed.Build());
                }

                // Remove the item after sending
                await context.RemoveScheduledItem(scheduledItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while sending scheduled item: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
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
    }
}