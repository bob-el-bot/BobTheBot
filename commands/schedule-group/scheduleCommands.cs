using System;
using System.Linq;
using System.Threading.Tasks;
using ColorMethods;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PremiumInterface;
using TimeStamps;
using static Commands.Helpers.Schedule;

namespace Commands
{
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("schedule", "All schedule commands.")]
    public class ScheduleGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("message", "Bob will send your message at a specified time.")]
        public async Task ScheduleMessage([Summary("message", "The message you want to send. Markdown still works!")] string message,
            [Summary("channel", "The channel for the message to be sent in.")][ChannelTypes(ChannelType.Text)] SocketChannel channel,
            [Summary("month", "The month you want your message sent.")] int month,
            [Summary("day", "The day you want your message sent.")] int day,
            [Summary("hour", "The hour you want your message sent, in military time (if PM, add 12).")] int hour,
            [Summary("minute", "The minute you want your message sent.")] int minute,
            [Summary("timezone", "Your timezone.")] TimeStamp.Timezone timezone)
        {
            DateTime scheduledTime;
            TimeZoneInfo timeZoneInfo;

            await DeferAsync();

            var context = new BobEntities();
            var user = await context.GetUser(Context.User.Id);

            try
            {
                if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages ||
                    !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
                {
                    await FollowupAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }

                int currentYear = DateTime.UtcNow.Year;
                DateTime localDateTime = new(currentYear, month, day, hour, minute, 0, DateTimeKind.Unspecified);
                TimeStamp.TimezoneMappings.TryGetValue(timezone, out string timeZoneId);
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);

                DateTime nowRounded = DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
                DateTime futureLimit = DateTime.UtcNow.AddMonths(1).AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
                utcDateTime = utcDateTime.AddTicks(-(utcDateTime.Ticks % TimeSpan.TicksPerSecond));

                if (utcDateTime <= nowRounded)
                {
                    await FollowupAsync("🌌 You formed a rift in the spacetime continuum! Try scheduling the message **in the future**.");
                    return;
                }

                if (utcDateTime > futureLimit)
                {
                    await FollowupAsync("📅 Scheduling is only allowed within 1 month into the future.");
                    return;
                }

                scheduledTime = utcDateTime;
            }
            catch (Exception ex)
            {
                await FollowupAsync($"❌ An error occurred: {ex.Message}");
                return;
            }

            await FollowupAsync("Scheduling message...");
            var response = await GetOriginalResponseAsync();

            var scheduledMessage = new ScheduledMessage
            {
                Id = response.Id,
                Message = message,
                TimeToSend = scheduledTime,
                ChannelId = channel.Id,
                ServerId = Context.Guild.Id,
                UserId = Context.User.Id
            };

            await context.AddScheduledMessage(scheduledMessage);

            ScheduleTask(scheduledMessage);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}\n- ID: `{scheduledMessage.Id}`";
            });
        }

        [SlashCommand("announcement", "Bob will send an embed at a specified time.")]
        public async Task ScheduleAnnouncement([Summary("title", "The title of the announcement (the title of the embed).")] string title,
           [Summary("description", "The anouncement (the description of the embed).")] string description,
           [Summary("color", "A color name (purple), or valid hex code (#8D52FD).")] string color,
           [Summary("channel", "The channel for the message to be sent in.")][ChannelTypes(ChannelType.Text)] SocketChannel channel,
           [Summary("month", "The month you want your message sent.")] int month,
           [Summary("day", "The day you want your message sent.")] int day,
           [Summary("hour", "The hour you want your message sent, in military time (if PM, add 12).")] int hour,
           [Summary("minute", "The minute you want your message sent.")] int minute,
           [Summary("timezone", "Your timezone.")] TimeStamp.Timezone timezone)
        {
            DateTime scheduledTime;
            TimeZoneInfo timeZoneInfo;

            Color finalColor = Colors.TryGetColor(color);

            try
            {
                await DeferAsync();

                var context = new BobEntities();
                var user = await context.GetUser(Context.User.Id);

                // Check if the user has premium.
                if (Premium.IsValidPremium(user.PremiumExpiration) == false)
                {
                    await FollowupAsync(text: $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", ephemeral: true);
                    return;
                }

                if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages ||
                    !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
                {
                    await FollowupAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }

                if (finalColor == 0)
                {
                    await RespondAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else if (title.Length > 256) // 256 is max characters in an embed title.
                {
                    await RespondAsync($"❌ The announcement *cannot* be made because it contains **{title.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **256** characters in embed titles.", ephemeral: true);
                }
                else if (description.Length > 4096) // 4096 is max characters in an embed description.
                {
                    await RespondAsync($"❌ The announcement *cannot* be made because it contains **{description.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
                }

                int currentYear = DateTime.UtcNow.Year;
                DateTime localDateTime = new(currentYear, month, day, hour, minute, 0, DateTimeKind.Unspecified);
                TimeStamp.TimezoneMappings.TryGetValue(timezone, out string timeZoneId);
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);

                DateTime nowRounded = DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
                DateTime futureLimit = DateTime.UtcNow.AddMonths(1).AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
                utcDateTime = utcDateTime.AddTicks(-(utcDateTime.Ticks % TimeSpan.TicksPerSecond));

                if (utcDateTime <= nowRounded)
                {
                    await FollowupAsync("🌌 You formed a rift in the spacetime continuum! Try scheduling the message **in the future**.");
                    return;
                }

                if (utcDateTime > futureLimit)
                {
                    await FollowupAsync("📅 Scheduling is only allowed within 1 month into the future.");
                    return;
                }

                scheduledTime = utcDateTime;
            }
            catch (Exception ex)
            {
                await FollowupAsync($"❌ An error occurred: {ex.Message}");
                return;
            }

            await FollowupAsync("Scheduling announcement...");
            var response = await GetOriginalResponseAsync();

            var scheduledAnnouncement = new ScheduledAnnouncement
            {
                Id = response.Id,
                Description = description,
                Title = title,
                Color = color,
                TimeToSend = scheduledTime,
                ChannelId = channel.Id,
                ServerId = Context.Guild.Id,
                UserId = Context.User.Id
            };

            using (var context = new BobEntities())
            {
                await context.AddScheduledAnnouncement(scheduledAnnouncement);
            }

            ScheduleTask(scheduledAnnouncement);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledAnnouncement.TimeToSend, TimeStamp.Formats.Exact)}\n- ID: `{scheduledAnnouncement.Id}`";
            });
        }

        [SlashCommand("edit", "Bob will allow you to edit any messages or announcements you have scheduled.")]
        public async Task EditScheduledMessage([Summary("id", "The ID of the scheduled message or announcement.")] string id)
        {
            await DeferAsync();

            if (!ulong.TryParse(id, out ulong parsedId))
            {
                await FollowupAsync($"❌ The provided ID `{id}` is invalid. Please provide a valid message or announcement ID.", ephemeral: true);
                return;
            }

            using var context = new BobEntities();
            var scheduledMessage = await context.GetScheduledMessage(parsedId);

            if (scheduledMessage == null)
            {
                await FollowupAsync($"❌ No scheduled message or announcement found with the provided ID `{id}`.");
                return;
            }

            if (scheduledMessage.UserId != Context.User.Id)
            {
                await FollowupAsync("❌ You can only edit your own scheduled messages or announcements.");
                return;
            }

            var embed = BuildEditMessageEmbed(scheduledMessage);
            var components = BuildEditMessageComponents(id);

            await FollowupAsync(embed: embed.Build(), components: components.Build());
        }

        [ComponentInteraction("editMessageButton:*", true)]
        public async Task EditScheduledMessageButton(string id)
        {
            try
            {
                var messageId = Convert.ToUInt64(id);

                using var context = new BobEntities();
                var message = await context.GetScheduledMessage(messageId);

                var modal = new EditMessageModal
                {
                    Content = message.Message
                };

                await Context.Interaction.RespondWithModalAsync(modal: modal, customId: $"editMessageModal:{messageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [ModalInteraction("editMessageModal:*", true)]
        public async Task EditMessageModalHandler(string id, EditMessageModal modal)
        {
            await DeferAsync();

            var messageId = Convert.ToUInt64(id);

            using var context = new BobEntities();
            var message = await context.GetScheduledMessage(messageId);
            message.Message = modal.Content;
            await context.UpdateScheduledMessage(message);

            var embed = BuildEditMessageEmbed(message);
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        }

        [ComponentInteraction("deleteMessageButton:*", true)]
        public async Task DeleteMessageButtonHandler(string id)
        {
            await DeferAsync();

            var originalResponse = await Context.Interaction.GetOriginalResponseAsync();
            var messageId = Convert.ToUInt64(id);

            using var context = new BobEntities();
            await context.RemoveScheduledMessage(messageId);

            var embed = new EmbedBuilder
            {
                Title = "(Deleted) Scheduled Message",
                Description = originalResponse.Embeds.First().Description,
                Color = Bot.theme
            }
            .AddField(name: "Time", value: originalResponse.Embeds.First().Fields.First().Value);

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
                x.Components = BuildEditMessageComponents(id, true).Build();
            });
        }
    }
}
