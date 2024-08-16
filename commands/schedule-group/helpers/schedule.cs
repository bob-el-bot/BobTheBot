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
        ulong ChannelId { get; }
        DateTime TimeToSend { get; }
    }

    /// <summary>
    /// Contains helper methods for scheduling and managing scheduled messages and announcements.
    /// </summary>
    public static class Schedule
    {
        /// <summary>
        /// Modal used for editing a scheduled message's content.
        /// </summary>
        public class EditMessageModal : IModal
        {
            /// <summary>
            /// Title of the modal displayed to the user.
            /// </summary>
            public string Title => "Edit Message";

            /// <summary>
            /// The content of the message to be edited.
            /// </summary>
            [InputLabel("Message Content")]
            [ModalTextInput("editMessageModal_content", TextInputStyle.Paragraph, "ff", maxLength: 2000)]
            public string Content { get; set; }
        }

        /// <summary>
        /// Builds an embed for displaying details of a message being edited.
        /// </summary>
        /// <param name="message">The scheduled message to be edited.</param>
        /// <returns>An embed showing the message content and time to be sent.</returns>
        public static EmbedBuilder BuildEditMessageEmbed(ScheduledMessage message)
        {
            return new EmbedBuilder
            {
                Title = $"Editing Message ID {message.Id}",
                Description = $"```{message.Message}```",
                Color = Bot.theme
            }
            .AddField(name: "Time", value: $"{TimeStamp.FromDateTime(message.TimeToSend, TimeStamp.Formats.Exact)}");
        }

        /// <summary>
        /// Builds components (buttons) for editing or deleting a scheduled message.
        /// </summary>
        /// <param name="id">The ID of the scheduled message.</param>
        /// <param name="disabled">Indicates if the buttons should be disabled.</param>
        /// <returns>A component builder with edit and delete buttons.</returns>
        public static ComponentBuilder BuildEditMessageComponents(string id, bool disabled = false)
        {
            return new ComponentBuilder()
                .WithButton(label: "Edit", customId: $"editMessageButton:{id}", style: ButtonStyle.Primary, emote: Emoji.Parse("✍️"), disabled: disabled)
                .WithButton(label: "Delete", customId: $"deleteMessageButton:{id}", style: ButtonStyle.Danger, emote: Emoji.Parse("🗑️"), disabled: disabled);
        }

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

                var timeSinceScheduled = DateTime.UtcNow - scheduledItem.TimeToSend;
                if (timeSinceScheduled > TimeSpan.FromHours(1))
                {
                    await context.RemoveScheduledItem(scheduledItem);
                    return;
                }

                if (scheduledItem is ScheduledMessage scheduledMessage)
                {
                    await channel.SendMessageAsync(scheduledMessage.Message);
                }
                else if (scheduledItem is ScheduledAnnouncement scheduledAnnouncement)
                {
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
            }
        }

        /// <summary>
        /// Loads all unsent scheduled items from the database and schedules them.
        /// </summary>
        /// <typeparam name="T">The type of scheduled item (message or announcement).</typeparam>
        /// <param name="items">The items to be loaded and scheduled.</param>
        public static async Task LoadAndScheduleItemsAsync<T>() where T : class, IScheduledItem
        {
            using var context = new BobEntities();
            var unsentItems = await context.Set<T>().Where(m => m.TimeToSend > DateTime.UtcNow).ToListAsync();

            foreach (var item in unsentItems)
            {
                ScheduleTask(item);
            }
        }
    }

    /// <summary>
    /// Represents a base class for scheduled items such as messages and announcements.
    /// </summary>
    public abstract class ScheduledItem
    {
        public string Id { get; set; }
        public ulong ChannelId { get; set; }
        public DateTime TimeToSend { get; set; }
    }
}
