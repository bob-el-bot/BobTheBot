using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Debug;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using TimeStamps;

namespace Commands.Helpers
{
    /// <summary>
    /// Contains helper methods for scheduling and managing scheduled messages.
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

        /// <summary>
        /// Schedules a task to send the message at the specified time.
        /// </summary>
        /// <param name="scheduledMessage">The message to be scheduled.</param>
        public static void ScheduleMessageTask(ScheduledMessage scheduledMessage)
        {
            TimeSpan maxDelay = TimeSpan.FromDays(30); // Maximum scheduling delay of 30 days
            var delay = scheduledMessage.TimeToSend - DateTime.UtcNow;

            // Helper method to schedule the task in chunks if the delay exceeds the maximum allowed.
            async Task ScheduleInChunks(TimeSpan totalDelay)
            {
                while (totalDelay > maxDelay)
                {
                    await Task.Delay(maxDelay);
                    totalDelay -= maxDelay;
                }
                await Task.Delay(totalDelay);
                await SendScheduledMessage(scheduledMessage);
            }

            // If the delay is negative or zero, send the message immediately.
            if (delay <= TimeSpan.Zero)
            {
                SendScheduledMessage(scheduledMessage).Wait();
            }
            // If the delay is greater than the maximum allowed, schedule in chunks.
            else if (delay > maxDelay)
            {
                _ = ScheduleInChunks(delay);
            }
            // Otherwise, schedule the message normally.
            else
            {
                _ = Task.Delay(delay).ContinueWith(async _ => await SendScheduledMessage(scheduledMessage));
            }
        }

        /// <summary>
        /// Sends the scheduled message to the designated channel.
        /// </summary>
        /// <param name="scheduledMessage">The message to be sent.</param>
        private static async Task SendScheduledMessage(ScheduledMessage scheduledMessage)
        {
            try
            {
                using var context = new BobEntities();
                var channel = (IMessageChannel)await Bot.Client.GetChannelAsync(scheduledMessage.ChannelId);

                if (channel == null)
                {
                    Console.WriteLine($"Channel with ID: {scheduledMessage.ChannelId} not found.");
                    return;
                }

                // Check if the message was missed by more than 1 hour.
                var timeSinceScheduled = DateTime.UtcNow - scheduledMessage.TimeToSend;
                if (timeSinceScheduled > TimeSpan.FromHours(1))
                {
                    await context.RemoveScheduledMessage(scheduledMessage.Id);
                    return;
                }

                await channel.SendMessageAsync(scheduledMessage.Message);
                await context.RemoveScheduledMessage(scheduledMessage.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while sending scheduled message: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all unsent scheduled messages from the database and schedules them.
        /// </summary>
        public static async Task LoadAndScheduleMessagesAsync()
        {
            using var context = new BobEntities();
            var unsentMessages = await context.ScheduledMessage
                .Where(m => m.TimeToSend > DateTime.UtcNow)
                .ToListAsync();

            foreach (var scheduledMessage in unsentMessages)
            {
                ScheduleMessageTask(scheduledMessage);
            }
        }
    }
}
