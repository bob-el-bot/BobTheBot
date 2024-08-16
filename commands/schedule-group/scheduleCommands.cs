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

                // Convert local time to UTC.
                scheduledTime = ConvertToUtcTime(month, day, hour, minute, timezone);
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
                x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}\n- ID: `{scheduledMessage.Id}`\n- You can edit your message with `/schedule edit` and the given ID.";
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

            Color? finalColor = Colors.TryGetColor(color);

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

                if (finalColor == null)
                {
                    await FollowupAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
                else if (title.Length > 256) // 256 is max characters in an embed title.
                {
                    await FollowupAsync($"❌ The announcement *cannot* be made because the title contains **{title.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **256** characters in embed titles.", ephemeral: true);
                    return;
                }
                else if (description.Length > 4000) // 4000 is the maximum length of an input field
                {
                    await FollowupAsync($"❌ The announcement *cannot* be made because the description contains **{description.Length}** characters.\n- Try having fewer characters.\n- To support editing, a limit of **4000** characters is set because of Discord's limitations in input fields.", ephemeral: true);
                    return;
                }

                // Convert local time to UTC.
                scheduledTime = ConvertToUtcTime(month, day, hour, minute, timezone);
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
                x.Content = $"✅ Announcement scheduled for {TimeStamp.FromDateTime(scheduledAnnouncement.TimeToSend, TimeStamp.Formats.Exact)}\n- ID: `{scheduledAnnouncement.Id}`\n- You can edit your announcement with `/schedule edit` and the given ID.";
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

        [ComponentInteraction("editAnnounceButton:*", true)]
        public async Task EditScheduledAnnounceButton(string id)
        {
            try
            {
                var announcementId = Convert.ToUInt64(id);

                using var context = new BobEntities();
                var announcement = await context.GetScheduledAnnouncement(announcementId);

                var modal = new EditAnnouncementModal
                {
                    EmbedTitle = announcement.Title,
                    Description = announcement.Description
                };

                await Context.Interaction.RespondWithModalAsync(modal: modal, customId: $"editAnnounceModal:{announcementId}");
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

            // Extract the original embed and its fields
            var originalEmbed = originalResponse.Embeds.First();
            var description = originalEmbed.Description;
            var color = originalEmbed.Color;
            var timeValue = originalEmbed.Fields.FirstOrDefault(f => f.Name == "Time").Value;
            var titleValue = originalEmbed.Fields.FirstOrDefault(f => f.Name == "Title").Value;

            // Build the new embed
            var embed = new EmbedBuilder
            {
                Title = "(Deleted) Scheduled Announcement",
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