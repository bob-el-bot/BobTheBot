using System;
using System.Linq;
using System.Threading.Tasks;
using ColorMethods;
using Commands.Helpers;
using Database;
using Database.Types;
using Debug;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PremiumInterface;
using Time.Timestamps;
using Time.Timezones;
using static Commands.Helpers.Schedule;

namespace Commands
{
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("schedule", "All schedule commands.")]
    public class ScheduleGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("message", "Bob will send your message at a specified time.")]
        public async Task ScheduleMessage([Summary("message", "The message you want to send. Markdown still works!")][MinLength(1)][MaxLength(2000)] string message,
            [Summary("channel", "The channel for the message to be sent in.")][ChannelTypes(ChannelType.Text)] SocketChannel channel,
            [Summary("month", "The month you want your message sent.")][MinValue(1)][MaxValue(12)] int month,
            [Summary("day", "The day you want your message sent.")][MinValue(1)][MaxValue(31)] int day,
            [Summary("hour", "The hour you want your message sent, in military time (if PM, add 12).")][MinValue(0)][MaxValue(23)] int hour,
            [Summary("minute", "The minute you want your message sent.")][MinValue(0)][MaxValue(59)] int minute,
            [Summary("timezone", "Your timezone.")] Timezone timezone)
        {
            DateTime scheduledTime;
            var context = new BobEntities();

            await DeferAsync();

            var user = await context.GetUser(Context.User.Id);

            try
            {
                if (Premium.IsPremium(Context.Interaction.Entitlements) == false && user.TotalScheduledMessages >= Premium.MaxScheduledMessages)
                {
                    await FollowupAsync(text: $"✨ This is a *premium* feature.\n- You already have a scheduled message, please upgrade to premium for unlimited scheduled messages.", components: Premium.GetComponents(), ephemeral: true);
                    return;
                }

                // Validate day
                if (day < 1 || day > DateTime.DaysInMonth(DateTime.UtcNow.Year, month))
                {
                    await FollowupAsync($"❌ Please enter a valid day between **1** and **{DateTime.DaysInMonth(DateTime.UtcNow.Year, month)}**.", ephemeral: true);
                    return;
                }

                if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages ||
                    !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
                {
                    await FollowupAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }

                // Convert local time to UTC.
                scheduledTime = TimeConverter.ConvertToUtcTime(month, day, hour, minute, timezone);
                if (scheduledTime <= DateTime.UtcNow)
                {
                    await FollowupAsync("🌌 Please schedule the message for a future time.");
                    return;
                }
                if (scheduledTime > DateTime.UtcNow.AddMonths(1))
                {
                    await FollowupAsync("📅 Scheduling is only allowed within 1 month into the future.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await FollowupAsync($"❌ An unexpected error occurred: {ex.Message}\n- Try again later.\n- The developers have been notified, but you can join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and provide us with more details if you want.");

                await Logger.LogErrorToDiscord(Context, $"{ex}");
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
            user.TotalScheduledMessages += 1;
            await context.UpdateUser(user);

            ScheduleTask(scheduledMessage);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = $"✅ Message scheduled for {Timestamp.FromDateTime(scheduledMessage.TimeToSend, Timestamp.Formats.Exact)}\n- ID: `{scheduledMessage.Id}`\n- You can edit your message with `/schedule edit` and the given ID.";
            });
        }

        [SlashCommand("announcement", "Bob will send an embed at a specified time.")]
        public async Task ScheduleAnnouncement([Summary("title", "The title of the announcement (the title of the embed).")][MinLength(1)][MaxLength(256)] string title,
           [Summary("description", "The anouncement (the description of the embed).")][MinLength(1)][MaxLength(4000)] string description,
           [Summary("color", "A color name (purple), or a valid hex code (#8D52FD) or valid RGB code (141, 82, 253).")] string color,
           [Summary("channel", "The channel for the message to be sent in.")][ChannelTypes(ChannelType.Text)] SocketChannel channel,
            [Summary("month", "The month you want your message sent.")][MinValue(1)][MaxValue(12)] int month,
            [Summary("day", "The day you want your message sent.")][MinValue(1)][MaxValue(31)] int day,
            [Summary("hour", "The hour you want your message sent, in military time (if PM, add 12).")][MinValue(0)][MaxValue(23)] int hour,
            [Summary("minute", "The minute you want your message sent.")][MinValue(0)][MaxValue(59)] int minute,
           [Summary("timezone", "Your timezone.")] Timezone timezone)
        {
            DateTime scheduledTime;
            Color? finalColor = Colors.TryGetColor(color);
            var context = new BobEntities();

            await DeferAsync();

            var user = await context.GetUser(Context.User.Id);

            try
            {
                // Check if the user has premium.
                if (Premium.IsPremium(Context.Interaction.Entitlements) == false)
                {
                    await FollowupAsync(text: $"✨ This is a *premium* feature.", components: Premium.GetComponents(), ephemeral: true);
                    return;
                }

                // Validate day
                if (day < 1 || day > DateTime.DaysInMonth(DateTime.UtcNow.Year, month))
                {
                    await FollowupAsync($"❌ Please enter a valid day between **1** and **{DateTime.DaysInMonth(DateTime.UtcNow.Year, month)}**.", ephemeral: true);
                    return;
                }

                if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages ||
                    !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
                {
                    await FollowupAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }

                if (finalColor == null)
                {
                    await FollowupAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex and RGB codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }

                // Convert local time to UTC.
                scheduledTime = TimeConverter.ConvertToUtcTime(month, day, hour, minute, timezone);
                if (scheduledTime <= DateTime.UtcNow)
                {
                    await FollowupAsync("🌌 Please schedule the message for a future time.");
                    return;
                }
                if (scheduledTime > DateTime.UtcNow.AddMonths(1))
                {
                    await FollowupAsync("📅 Scheduling is only allowed within 1 month into the future.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await FollowupAsync($"❌ An unexpected error occurred: {ex.Message}\n- Try again later.\n- The developers have been notified, but you can join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and provide us with more details if you want.");

                await Logger.LogErrorToDiscord(Context, $"{ex}");
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

            await context.AddScheduledAnnouncement(scheduledAnnouncement);
            user.TotalScheduledAnnouncements += 1;
            await context.UpdateUser(user);

            ScheduleTask(scheduledAnnouncement);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = $"✅ Announcement scheduled for {Timestamp.FromDateTime(scheduledAnnouncement.TimeToSend, Timestamp.Formats.Exact)}\n- ID: `{scheduledAnnouncement.Id}`\n- You can edit your announcement with `/schedule edit` and the given ID.";
            });
        }

        [SlashCommand("edit", "Bob will allow you to edit any messages or announcements you have scheduled.")]
        public async Task EditScheduledItem([Summary("id", "The ID of the scheduled message or announcement.")] string id)
        {
            await DeferAsync();

            if (!ulong.TryParse(id, out ulong parsedId))
            {
                await FollowupAsync($"❌ The provided ID `{id}` is invalid. Please provide a valid message or announcement ID.", ephemeral: true);
                return;
            }

            using var context = new BobEntities();
            IScheduledItem scheduledItem = await context.GetScheduledMessage(parsedId);
            if (scheduledItem == null)
            {
                scheduledItem = await context.GetScheduledAnnouncement(parsedId);
            }

            if (scheduledItem == null)
            {
                await FollowupAsync($"❌ No scheduled message or announcement found with the provided ID `{id}`.");
                return;
            }

            if (scheduledItem.UserId != Context.User.Id)
            {
                await FollowupAsync("❌ You can only edit your own scheduled messages or announcements.");
                return;
            }

            var embed = BuildEditEmbed(scheduledItem);
            var components = BuildEditMessageComponents(scheduledItem);

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

                if (message == null)
                {
                    var component = (SocketMessageComponent)Context.Interaction;

                    await component.UpdateAsync(x =>
                    {
                        x.Components = BuildEditMessageComponents(null, true).Build();
                    });
                }
                else
                {
                    var modal = new EditMessageModal
                    {
                        Content = message.Message
                    };

                    await Context.Interaction.RespondWithModalAsync(modal: modal, customId: $"editMessageModal:{messageId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [ComponentInteraction("editAnnounceButton:*", true)]
        public async Task EditScheduledAnnounceButton(string id)
        {
            try
            {
                var announcementId = Convert.ToUInt64(id);

                using var context = new BobEntities();
                var announcement = await context.GetScheduledAnnouncement(announcementId);

                if (announcement == null)
                {
                    var component = (SocketMessageComponent)Context.Interaction;

                    await component.UpdateAsync(x =>
                    {
                        x.Components = BuildEditMessageComponents(null, true).Build();
                    });
                }
                else
                {
                    var modal = new EditAnnouncementModal
                    {
                        EmbedTitle = announcement.Title,
                        Description = announcement.Description
                    };

                    await Context.Interaction.RespondWithModalAsync(modal: modal, customId: $"editAnnounceModal:{announcementId}");
                }
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

            var embed = BuildEditEmbed(message);
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        }

        [ModalInteraction("editAnnounceModal:*", true)]
        public async Task EditAnnouncementModalHandler(string id, EditAnnouncementModal modal)
        {
            await DeferAsync();

            var announcementId = Convert.ToUInt64(id);

            using var context = new BobEntities();
            var announcement = await context.GetScheduledAnnouncement(announcementId);
            announcement.Title = modal.EmbedTitle;
            announcement.Description = modal.Description;
            await context.UpdateScheduledAnnouncement(announcement);

            var embed = BuildEditEmbed(announcement);
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

            // Cancel the corresponding task if it exists
            if (ScheduledTasks.TryGetValue(messageId, out var cts))
            {
                cts.Cancel();
                ScheduledTasks.Remove(messageId); // Remove the task from tracking
            }

            var embed = new EmbedBuilder
            {
                Title = "🕖 (Deleted) Scheduled Message",
                Description = originalResponse.Embeds.First().Description,
                Color = Bot.theme
            }
            .AddField(name: "Time", value: originalResponse.Embeds.First().Fields.First().Value);

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
                x.Components = BuildEditMessageComponents(null, true).Build();
            });
        }

        [ComponentInteraction("deleteAnnounceButton:*", true)]
        public async Task DeleteAnnouncementButtonHandler(string id)
        {
            await DeferAsync();

            var originalResponse = await Context.Interaction.GetOriginalResponseAsync();
            var announcementId = Convert.ToUInt64(id);

            using var context = new BobEntities();
            await context.RemoveScheduledAnnouncement(announcementId);

            // Cancel the corresponding task if it exists
            if (ScheduledTasks.TryGetValue(announcementId, out var cts))
            {
                cts.Cancel();
                ScheduledTasks.Remove(announcementId); // Remove the task from tracking
            }

            // Extract the original embed and its fields
            var originalEmbed = originalResponse.Embeds.First();
            var description = originalEmbed.Description;
            var color = originalEmbed.Color;
            var timeValue = originalEmbed.Fields.FirstOrDefault(f => f.Name == "Time").Value;
            var titleValue = originalEmbed.Fields.FirstOrDefault(f => f.Name == "Title").Value;

            // Build the new embed
            var embed = new EmbedBuilder
            {
                Title = "🕖 (Deleted) Scheduled Announcement",
                Description = description,
                Color = color
            }
            .AddField(name: "Title", value: titleValue)
            .AddField(name: "Time", value: timeValue);

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
                x.Components = BuildEditMessageComponents(null, true).Build();
            });
        }
    }
}
