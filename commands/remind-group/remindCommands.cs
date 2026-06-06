using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bob.Database;
using Bob.Database.Types;
using Bob.Debug;
using Bob.PremiumInterface;
using Bob.Time.Timestamps;
using Bob.Time.Timezones;
using Discord;
using Discord.Interactions;
using static Bob.Commands.Helpers.Schedule;
using Bob.Commands.Helpers;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
    [Group("remind", "All reminder commands.")]
    public class RemindGroup(BobEntities dbContext) : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("set", "Bob will DM you a reminder at a specified time.")]
        public async Task SetReminder(
            [Summary("message", "What you want to be reminded of.")][MinLength(1)][MaxLength(1984)] string message,
            [Summary("month", "The month for the reminder.")][MinValue(1)][MaxValue(12)] int month,
            [Summary("day", "The day for the reminder.")][MinValue(1)][MaxValue(31)] int day,
            [Summary("hour", "The hour for the reminder, in military time (if PM, add 12).")][MinValue(0)][MaxValue(23)] int hour,
            [Summary("minute", "The minute for the reminder.")][MinValue(0)][MaxValue(59)] int minute,
            [Summary("timezone", "Your timezone.")][Autocomplete(typeof(TimezoneAutocompleteHandler))] string timezoneStr)
        {
            await DeferAsync(ephemeral: true);

            if (!TimezoneExtensions.FromChoiceDisplay.TryGetValue(timezoneStr, out var timezone))
            {
                await FollowupAsync("❌ Invalid timezone selected.", ephemeral: true);
                return;
            }

            DateTime scheduledTime;

            try
            {
                if (day < 1 || day > DateTime.DaysInMonth(DateTime.UtcNow.Year, month))
                {
                    await FollowupAsync($"❌ Please enter a valid day.\n- Month **{month}** only has **{DateTime.DaysInMonth(DateTime.UtcNow.Year, month)}** days.", ephemeral: true);
                    return;
                }

                scheduledTime = TimeConverter.ConvertToUtcTime(month, day, hour, minute, timezone);

                if (scheduledTime <= DateTime.UtcNow)
                {
                    await FollowupAsync("❌ Please set the reminder for a future time.\n- The time you entered has already passed.", ephemeral: true);
                    return;
                }

                if (scheduledTime > DateTime.UtcNow.AddMonths(1))
                {
                    await FollowupAsync("❌ Reminders can only be set within **1 month** into the future.", ephemeral: true);
                    return;
                }
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true);
                return;
            }

            var user = await dbContext.GetUserOrNew(Context.User.Id);

            if (Premium.IsPremium(Context.Interaction.Entitlements, user) == false && user.TotalReminders >= Premium.MaxReminders)
            {
                await FollowupAsync(
                    text: $"✨ This is a *premium* feature.\n- You already have a reminder set. Upgrade to premium for unlimited reminders.\n- {Premium.HasPremiumMessage}",
                    components: Premium.GetComponents(),
                    ephemeral: true);
                return;
            }

            var reminder = new ScheduledReminder
            {
                Id = Context.Interaction.Id,
                UserId = Context.User.Id,
                Message = message,
                TimeToSend = scheduledTime
            };

            await dbContext.AddScheduledReminder(reminder);
            await dbContext.UpsertUserAsync(user.Id, u => u.TotalReminders += 1);

            ScheduleTask(reminder);

            await FollowupAsync($"✅ Reminder set for {Timestamp.FromDateTime(reminder.TimeToSend, Timestamp.Formats.Exact)}\n- I'll DM you when it's time.\n- ID: `{reminder.Id}`\n- Delete it with {Help.GetCommandMention("remind delete")} and the given ID.", ephemeral: true);
        }

        [SlashCommand("in", "Bob will DM you a reminder after a relative amount of time.")]
        public async Task RemindIn(
            [Summary("message", "What you want to be reminded of.")][MinLength(1)][MaxLength(1984)] string message,
            [Summary("days", "Number of days from now.")][MinValue(0)][MaxValue(30)] int days = 0,
            [Summary("hours", "Number of hours from now.")][MinValue(0)][MaxValue(23)] int hours = 0,
            [Summary("minutes", "Number of minutes from now.")][MinValue(0)][MaxValue(59)] int minutes = 0)
        {
            await DeferAsync(ephemeral: true);

            if (days == 0 && hours == 0 && minutes == 0)
            {
                await FollowupAsync("❌ Please specify at least one of: `days`, `hours`, or `minutes`.", ephemeral: true);
                return;
            }

            var duration = TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);

            if (duration > TimeSpan.FromDays(30))
            {
                await FollowupAsync("❌ Reminders can only be set within **30 days** into the future.", ephemeral: true);
                return;
            }

            var user = await dbContext.GetUserOrNew(Context.User.Id);

            if (Premium.IsPremium(Context.Interaction.Entitlements, user) == false && user.TotalReminders >= Premium.MaxReminders)
            {
                await FollowupAsync(
                    text: $"✨ This is a *premium* feature.\n- You already have a reminder set. Upgrade to premium for unlimited reminders.\n- {Premium.HasPremiumMessage}",
                    components: Premium.GetComponents(),
                    ephemeral: true);
                return;
            }

            var reminder = new ScheduledReminder
            {
                Id = Context.Interaction.Id,
                UserId = Context.User.Id,
                Message = message,
                TimeToSend = DateTime.UtcNow + duration
            };

            await dbContext.AddScheduledReminder(reminder);
            await dbContext.UpsertUserAsync(user.Id, u => u.TotalReminders += 1);

            ScheduleTask(reminder);

            await FollowupAsync($"✅ Reminder set for {Timestamp.FromDateTime(reminder.TimeToSend, Timestamp.Formats.Exact)}\n- I'll DM you when it's time.\n- ID: `{reminder.Id}`\n- Delete it with {Help.GetCommandMention("remind delete")} and the given ID.", ephemeral: true);
        }

        [SlashCommand("delete", "Delete one of your scheduled reminders.")]
        public async Task DeleteReminder([Summary("id", "The ID of the reminder to delete.")] string id)
        {
            await DeferAsync(ephemeral: true);

            if (!ulong.TryParse(id, out ulong parsedId))
            {
                await FollowupAsync($"❌ The provided ID `{id}` is invalid.\n- Find your reminder IDs with {Help.GetCommandMention("remind list")}.", ephemeral: true);
                return;
            }

            var reminder = await dbContext.GetScheduledReminder(parsedId);

            if (reminder == null)
            {
                await FollowupAsync($"❌ No reminder found with ID `{id}`.\n- View your reminders with {Help.GetCommandMention("remind list")}.", ephemeral: true);
                return;
            }

            if (reminder.UserId != Context.User.Id)
            {
                await FollowupAsync("❌ You can only delete your own reminders.", ephemeral: true);
                return;
            }

            await dbContext.RemoveScheduledReminder(parsedId);
            await dbContext.UpsertUserAsync(Context.User.Id, u =>
            {
                if (u.TotalReminders > 0)
                    u.TotalReminders -= 1;
            });

            if (ScheduledTasks.TryGetValue(parsedId, out var cts))
            {
                cts.Cancel();
                ScheduledTasks.Remove(parsedId);
            }

            await FollowupAsync($"✅ Reminder `{id}` deleted.", ephemeral: true);
        }

        [SlashCommand("list", "List your upcoming reminders.")]
        public async Task ListReminders()
        {
            await DeferAsync(ephemeral: true);

            var reminders = await dbContext.GetScheduledRemindersForUser(Context.User.Id);

            if (reminders.Count == 0)
            {
                await FollowupAsync($"📭 You have no upcoming reminders.\n- Set one with {Help.GetCommandMention("remind set")} or {Help.GetCommandMention("remind in")}.", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("⏰ **Your Upcoming Reminders:**");

            foreach (var r in reminders)
            {
                string preview = r.Message.Length > 60 ? r.Message[..57] + "..." : r.Message;
                sb.AppendLine($"- `{r.Id}` | {Timestamp.FromDateTime(r.TimeToSend, Timestamp.Formats.Exact)} | {preview}");
            }

            await FollowupAsync(sb.ToString(), ephemeral: true);
        }
    }
}
