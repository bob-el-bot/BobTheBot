using System;
using System.Linq;
using System.Threading.Tasks;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using TimeStamps;

namespace Commands
{
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("schedule", "All schedule commands.")]
    public class ScheduleGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("message", "Bob will send your message at a specified time.")]
        public async Task ScheduleMessage([Summary("message", "The message you want to send. Markdown still works!")] string message, [Summary("channel", "The channel for the message to be sent in.")][ChannelTypes(ChannelType.Text)] SocketChannel channel, [Summary("month", "The month you want your message sent.")] int month, [Summary("day", "The day you want your message sent.")] int day, [Summary("hour", "The hour you want your message sent, in military time (if PM, add 12).")] int hour, [Summary("minute", "The minute you want your message sent.")] int minute, [Summary("timezone", "Your timezone.")] TimeStamp.Timezone timezone)
        {
            DateTime scheduledTime;
            TimeZoneInfo timeZoneInfo;

            try
            {
                // Check if Bob has permission to send messages in given channel
                if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
                {
                    await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }

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
                ChannelId = channel.Id,
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
                x.Content = $"✅ Message scheduled for {TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}\n- ID: `{scheduledMessage.Id}`";
            });
        }

        [SlashCommand("edit", "Bob will allow you to edit any messages or announcements you have scheduled.")]
        public async Task EditScheduledMessage([Summary("id", "The ID of the scheduled message or announcement.")] string id)
        {
            await DeferAsync();

            // Attempt to parse the ID into a ulong
            if (!ulong.TryParse(id, out ulong parsedId))
            {
                await FollowupAsync($"❌ The provided ID `{id}` is invalid. Please provide a valid message or announcement ID.", ephemeral: true);
                return;
            }

            using var context = new BobEntities();

            // Attempt to find the scheduled message by parsed ID in the database
            var scheduledMessage = await context.GetScheduledMessage(parsedId);

            // Check if the scheduled message exists
            if (scheduledMessage == null)
            {
                await FollowupAsync($"❌ No scheduled message or announcement found with the provided ID `{id}`.");
                return;
            }

            // Check if the user is the owner of the message
            if (scheduledMessage.UserId != Context.User.Id)
            {
                await FollowupAsync("❌ You can only edit your own scheduled messages or announcements.");
                return;
            }

            var embed = new EmbedBuilder
            {
                Title = $"Editing Message ID {scheduledMessage.Id}",
                Description = scheduledMessage.Message,
                Color = Bot.theme
            };
            embed.AddField(name: "Time", value: $"{TimeStamp.FromDateTime(scheduledMessage.TimeToSend, TimeStamp.Formats.Exact)}");

            var components = new ComponentBuilder()
                    .WithButton(label: "Edit", customId: $"editMessageButton:{id}", style: ButtonStyle.Primary, emote: Emoji.Parse("✍️"))
                    .WithButton(label: "Delete", customId: $"deleteMessageButton:{id}", style: ButtonStyle.Danger, emote: Emoji.Parse("🗑️"));

            await FollowupAsync(embed: embed.Build(), components: components.Build());
        }

        [ComponentInteraction("editMessageButton:*", true)]
        public async Task EditScheduledMessageButton(string id)
        {
            try
            {
                var messageId = Convert.ToUInt64(id);
                await Context.Interaction.RespondWithModalAsync<EditMessageModal>(customId: $"editMessageModal:{messageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public class EditMessageModal : IModal
        {
            public string Title => "Edit Message";

            [InputLabel("Message Content")]
            [ModalTextInput("editMessageModal_content", TextInputStyle.Paragraph, "ff", maxLength: 2000)]
            public string Content { get; set; }
        }

        // Responds to the modal.
        [ModalInteraction("editMessageModal:*", true)]
        public async Task EditMessageModalHandler(string id, EditMessageModal modal)
        {
            await DeferAsync();

            var originalResponse = await Context.Interaction.GetOriginalResponseAsync();
            var messageId = Convert.ToUInt64(id);

            using var context = new BobEntities();
            var message = await context.GetScheduledMessage(messageId);
            message.Message = modal.Content;
            await context.UpdateScheduledMessage(message);

            // Create Embed
            var ogEmbed = originalResponse.Embeds.First();
            var embed = new EmbedBuilder()
            {
                Title = ogEmbed.Title,
                Description = modal.Content,
                Color = Bot.theme
            };
            embed.AddField(name: "Time", value: ogEmbed.Fields.First().Value);

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        }

        [ComponentInteraction("deleteMessageButton:*", true)]
        public async Task DeleteMessageButtonHandler(string id)
        {
            await DeferAsync();
            
            var originalResponse = await Context.Interaction.GetOriginalResponseAsync();
            var messageId = Convert.ToUInt64(id);

            using var context = new BobEntities();
            var message = await context.GetScheduledMessage(messageId);
            await context.RemoveScheduledMessage(message);

            // Create Embed
            var ogEmbed = originalResponse.Embeds.First();
            var embed = new EmbedBuilder()
            {
                Title = $"(Deleted) Message ID {message.Id}",
                Description = message.Message,
                Color = Bot.theme
            };
            embed.AddField(name: "Time", value: ogEmbed.Fields.First().Value);

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); x.Components = null; });
        }
    }
}
