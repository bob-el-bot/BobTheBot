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
    public static class Schedule
    {
        public class EditMessageModal : IModal
        {
            public string Title => "Edit Message";

            [InputLabel("Message Content")]
            [ModalTextInput("editMessageModal_content", TextInputStyle.Paragraph, "ff", maxLength: 2000)]
            public string Content { get; set; }
        }

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

        public static ComponentBuilder BuildEditMessageComponents(string id, bool disabled = false)
        {
            return new ComponentBuilder()
                .WithButton(label: "Edit", customId: $"editMessageButton:{id}", style: ButtonStyle.Primary, emote: Emoji.Parse("✍️"), disabled: disabled)
                .WithButton(label: "Delete", customId: $"deleteMessageButton:{id}", style: ButtonStyle.Danger, emote: Emoji.Parse("🗑️"), disabled: disabled);
        }

        public static void ScheduleMessageTask(ScheduledMessage scheduledMessage)
        {
            TimeSpan maxDelay = TimeSpan.FromDays(30); // Maximum scheduling delay of 30 days
            var delay = scheduledMessage.TimeToSend - DateTime.UtcNow;

            // If the delay exceeds the maximum allowed, break it into chunks
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

            // If delay is negative or zero, send the message immediately
            if (delay <= TimeSpan.Zero)
            {
                SendScheduledMessage(scheduledMessage).Wait();
            }
            else if (delay > maxDelay)
            {
                _ = ScheduleInChunks(delay);
            }
            else
            {
                _ = Task.Delay(delay).ContinueWith(async _ => await SendScheduledMessage(scheduledMessage));
            }
        }

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

                // Check if the message was missed by more than 1 hour
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