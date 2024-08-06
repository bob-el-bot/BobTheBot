using System;
using System.Threading.Tasks;
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
        public async Task ScheduleMessage([Summary("message", "The message you want to send. Markdown still works!")] string message, [Summary("month", "The month you want your message sent.")] int month, [Summary("day", "The day you want your message sent.")] int day, [Summary("hour", "The hour you want your message sent, in military time (if PM, add 12).")] int hour, [Summary("minute", "The minute you want your message sent.")] int minute, [Summary("timezone", "Your timezone.")] TimeStamp.Timezone timezone)
        {
            DateTime scheduledTime;
            TimeZoneInfo timeZoneInfo;

            try
            {
                // Convert month and day to current year
                int currentYear = DateTime.UtcNow.Year;

                // Create DateTime for the specified time in local time
                DateTime localDateTime = new(currentYear, month, day, hour, minute, 0, DateTimeKind.Unspecified);

                // Map enum to TimeZoneInfo
                if (!TimeStamp.TimezoneMappings.TryGetValue(timezone, out string timeZoneId))
                {
                    await RespondAsync("❌ Invalid timezone selected.");
                    return;
                }

                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

                // Convert the local time to UTC based on the specified timezone
                DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);

                // Round DateTime to avoid microsecond precision issues
                DateTime nowRounded = DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
                DateTime futureLimit = DateTime.UtcNow.AddMonths(1).AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
                utcDateTime = utcDateTime.AddTicks(-(utcDateTime.Ticks % TimeSpan.TicksPerSecond));

                // Check if the scheduled time is valid (future and within 1 month)
                if (utcDateTime <= nowRounded)
                {
                    await RespondAsync("🌌 You formed a rift in the spacetime continuum! Try scheduling the message **in the future**.");
                    return;
                }

                if (utcDateTime > futureLimit)
                {
                    await RespondAsync("📅 Scheduling is only allowed within 1 month into the future.");
                    return;
                }

                scheduledTime = utcDateTime;
            }
            catch (Exception ex)
            {
                await RespondAsync($"❌ An error occurred: {ex.Message}");
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

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}";
            });
        }
    }
}
