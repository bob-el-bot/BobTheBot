using System;
using System.Linq;
using System.Threading.Tasks;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord.Interactions;
using PremiumInterface;

namespace Commands
{
    [EnabledInDm(false)]
    [Group("welcome", "All welcome commands.")]
    public class WelcomeGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(false)]
        [SlashCommand("toggle", "Enable or disable Bob welcoming users to your server!")]
        public async Task WelcomeToggle([Summary("welcome", "If checked (true), Bob will send welcome messages.")] bool welcome)
        {
            await DeferAsync(ephemeral: true);

            if (Context.Guild.SystemChannel == null)
            {
                await FollowupAsync(text: $"❌ You **need** to set a *System Messages* channel in settings in order for Bob to greet people.", ephemeral: true);
            }
            // Check if the user has manage channels permissions
            else if (!Context.Guild.GetUser(Context.User.Id).GetPermissions(Context.Guild.SystemChannel).ManageChannel == false)
            {
                await FollowupAsync(text: $"❌ You do not have permissions to manage <#{Context.Guild.SystemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission **Manage Channel**.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if Bob has permission to send messages in given channel
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.SystemChannel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.SystemChannel).ViewChannel)
            {
                await FollowupAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{Context.Guild.SystemChannel.Id}> (The system channel where welcome messages are sent)\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Update server welcome information.
            else
            {
                Server server;
                using (var context = new BobEntities())
                {
                    server = await context.GetServer(Context.Guild.Id);
                    server.Welcome = welcome;
                    await context.UpdateServer(server);
                }

                if (welcome)
                {
                    if (Context.Guild.SystemChannel == null)
                    {
                        await FollowupAsync(text: $"❌ Bob knows to welcome users now, but you **need** to set a *System Messages* channel in settings for this to take effect.", ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync(text: $"✅ Bob will now greet people in <#{Context.Guild.SystemChannel.Id}>", ephemeral: true);
                    }
                }
                else
                {
                    await FollowupAsync(text: $"✅ Bob will not greet people anymore.", ephemeral: true);
                }
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("set-message", "Create a custom welcome message for your server!")]
        public async Task SetCustomWelcomeMessage([Summary("message", "Whatever you want said to your users. Type @ where you want the ping.")] string message)
        {
            await DeferAsync(ephemeral: true);

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            // Check if the user has manage channels permissions.
            if (Context.Guild.GetUser(Context.User.Id).GetPermissions(Context.Guild.SystemChannel).ManageChannel == false)
            {
                await FollowupAsync(text: $"❌ You do not have permissions to manage <#{Context.Guild.SystemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission **Manage Channel**.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if the message is within Discord's length requirements.
            else if (message.Length + (34 * message.Count(c => c == '@')) > 2000)
            {
                await FollowupAsync(text: $"❌ The message length either exceeds Discord's **2000** character limit or could when mentions are inserted.\n- Try shortening your message.\n- Every mention is assumed to be a length of **32** characters plus **3** formatting characters.", ephemeral: true);
            }
            // Check if the user has premium.
            else if (Premium.HasValidPremium(user.PremiumExpiration) == false)
            {
                await RespondWithPremiumRequiredAsync();
            }
            // Update server welcome information.
            else
            {
                Server server;
                server = await context.GetServer(Context.Guild.Id);
                server.CustomWelcomeMessage = message;
                await context.UpdateServer(server);

                if (server.Welcome)
                {
                    if (Context.Guild.SystemChannel == null)
                    {
                        await FollowupAsync(text: $"❌ Bob knows to welcome users now, and what to say, but you **need** to set a *System Messages* channel in settings for this to take effect.\nYour welcome message will look like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync(text: $"✅ Bob will now greet people in <#{Context.Guild.SystemChannel.Id}> like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                    }
                }
                else
                {
                    await FollowupAsync(text: $"✅ Bob knows what to say, but you **need** to enable welcome messages with `/welcome toggle` for it to take effect.\nYour welcome message will look like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                }
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("remove-message", "Removes the custom welcome message from your server. Does not disable general welcome messages.")]
        public async Task RemoveCustomWelcomeMessage()
        {
            await DeferAsync(ephemeral: true);

            // Check if the user has manage channels permissions.
            if (Context.Guild.GetUser(Context.User.Id).GetPermissions(Context.Guild.SystemChannel).ManageChannel == false)
            {
                await FollowupAsync(text: $"❌ You do not have permissions to manage <#{Context.Guild.SystemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission **Manage Channel**.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Update server welcome information.
            else
            {
                Server server;
                using var context = new BobEntities();
                server = await context.GetServer(Context.Guild.Id);

                // Only write to DB if needed.
                if (server.CustomWelcomeMessage != null && server.CustomWelcomeMessage != "")
                {
                    server.CustomWelcomeMessage = "";
                    await context.UpdateServer(server);
                }

                await FollowupAsync(text: $"✅ Bob will no longer greet people with the custom message.", ephemeral: true);
            }
        }
    }
}