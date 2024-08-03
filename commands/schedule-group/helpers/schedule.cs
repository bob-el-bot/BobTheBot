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
    public class Schedule
    {
        public static void ScheduleMessageTask(ScheduledMessage scheduledMessage)
        {
            var delay = scheduledMessage.TimeToSend - DateTime.UtcNow;
            if (delay <= TimeSpan.Zero)
            {
                Console.WriteLine("Scheduled time has already passed. Skipping message.");
                return;
            }

            Console.WriteLine($"Scheduling message with ID: {scheduledMessage.Id} to be sent in {delay.TotalSeconds} seconds.");

            _ = Task.Delay(delay).ContinueWith(async _ =>
            {
                try
                {
                    using var context = new BobEntities();
                    var channel = (IMessageChannel) await Bot.Client.GetChannelAsync(scheduledMessage.ChannelId);

                    if (channel == null)
                    {
                        Console.WriteLine($"Channel with ID: {scheduledMessage.ChannelId} not found.");
                        return;
                    }

                    if (!scheduledMessage.IsSent)
                    {
                        await channel.SendMessageAsync(scheduledMessage.Message);
                        scheduledMessage.IsSent = true;
                        await context.RemoveScheduledMessage(scheduledMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        public static async Task LoadAndScheduleMessagesAsync()
        {
            using var context = new BobEntities();
            var unsentMessages = await context.ScheduledMessage
                .Where(m => !m.IsSent && m.TimeToSend > DateTime.UtcNow)
                .ToListAsync();

            foreach (var scheduledMessage in unsentMessages)
            {
                ScheduleMessageTask(scheduledMessage);
            }
        }
    }
}