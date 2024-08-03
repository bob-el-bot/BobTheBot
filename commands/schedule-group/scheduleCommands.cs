using System;
using System.Threading.Tasks;
using Commands.Attributes;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using TimeStamps;

namespace Commands
{
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("schedule", "All schedule commands.")]
    public class ScheduleGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("message", "Bob will send your message at a specified time.")]
        public async Task ScheduleMessage(string message, int year, int month, int day, int hour, int minute)
        {
            DateTime scheduledTime;
            try
            {
                scheduledTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
                if (scheduledTime <= DateTime.UtcNow)
                {
                    await RespondAsync("🌌 You formed a rift in the spacetime continuum! Try scheduling the message **in the future**.");
                    return;
                }
            }
            catch (Exception)
            {
                await RespondAsync("❌ Invalid date or time. Please provide a valid date and time in UTC.");
                return;
            }

            await RespondAsync("Scheduling message...");
            var response = await GetOriginalResponseAsync();

            var scheduledMessage = new ScheduledMessage
            {
                Id = response.Id,
                Message = message,
                IsSent = false,
                TimeToSend = scheduledTime,
                ChannelId = Context.Channel.Id,
                ServerId = Context.Guild.Id,
                UserId = Context.User.Id
            };

            using (var context = new BobEntities())
            {
                await context.AddScheduledMessage(scheduledMessage);
            }

            Schedule.ScheduleMessageTask(scheduledMessage);

            await ModifyOriginalResponseAsync(x => { x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}"; });
        }
    }
}
