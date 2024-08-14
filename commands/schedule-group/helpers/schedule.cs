using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Commands.Helpers
{
    public static class Schedule
    {
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
                Console.WriteLine("Scheduled time has already passed or is now. Sending message immediately.");
                SendScheduledMessage(scheduledMessage).Wait();
            }
            else if (delay > maxDelay)
            {
                Console.WriteLine($"Scheduling message with ID: {scheduledMessage.Id} in chunks, total delay: {delay.TotalSeconds} seconds.");
                _ = ScheduleInChunks(delay);
            }
            else
            {
                Console.WriteLine($"Scheduling message with ID: {scheduledMessage.Id} to be sent in {delay.TotalSeconds} seconds.");
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
                    Console.WriteLine($"Message with ID: {scheduledMessage.Id} was missed by more than 1 hour. Deleting message.");
                    await context.RemoveScheduledMessage(scheduledMessage);
                    return;
                }

                await channel.SendMessageAsync(scheduledMessage.Message);
                await context.RemoveScheduledMessage(scheduledMessage);
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