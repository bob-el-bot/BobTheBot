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
        public async Task ScheduleMessage(string message, int month, int day, int hour, int minute, int timezoneOffset)
        {
            DateTime currentDate = DateTime.UtcNow;
            DateTime localTime;
            DateTime utcTime;
            int year;

            try
            {
                // Determine the year
                if (month < currentDate.Month || (month == currentDate.Month && day < currentDate.Day))
                {
                    // If the month and day have already passed this year, it means the date is next year
                    year = currentDate.Year + 1;
                }
                else
                {
                    // Otherwise, it is still this year
                    year = currentDate.Year;
                }

                // Create local time with inferred year
                localTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

                // Convert to UTC using the provided timezone offset
                utcTime = localTime.AddMinutes(-timezoneOffset);

                // Ensure that the UTC time is always used for scheduling and comparisons
                if (utcTime <= DateTime.UtcNow)
                {
                    await RespondAsync("🌌 You formed a rift in the spacetime continuum! Try scheduling the message **in the future**.");
                    return;
                }

                // Define the maximum allowed scheduling period (1 month)
                DateTime maxAllowedDate = DateTime.UtcNow.AddMonths(1);
                if (utcTime > maxAllowedDate)
                {
                    await RespondAsync("🕰️ Scheduling time cannot exceed 1 month into the future.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await RespondAsync($"❌ Invalid date or time. Error: {ex.Message}");
                return;
            }

            await RespondAsync("Scheduling message...");
            var response = await GetOriginalResponseAsync();

            // Ensure that all DateTime values are in UTC
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            // Create a new ScheduledMessage object
            var scheduledMessage = new ScheduledMessage
            {
                Id = response.Id,
                Message = message,
                IsSent = false,
                TimeToSend = utcTime, // Ensure this is UTC
                ChannelId = Context.Channel.Id,
                ServerId = Context.Guild.Id,
                UserId = Context.User.Id
            };

            using (var context = new BobEntities())
            {
                await context.AddScheduledMessage(scheduledMessage);
            }

            Schedule.ScheduleMessageTask(scheduledMessage);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}";
            });
        }
    }
}
