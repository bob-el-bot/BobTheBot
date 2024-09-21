using System;
using System.Linq;
using System.Threading.Tasks;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using PremiumInterface;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("welcome", "All welcome commands.")]
    public class WelcomeGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("toggle", "Enable or disable Bob welcoming users to your server!")]
        public async Task WelcomeToggle([Summary("welcome", "If checked (true), Bob will send welcome messages.")] bool welcome)
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if user is an administrator
            if (!discordUser.GuildPermissions.Administrator)
            {
                // Ensure system channel is set
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You **need** to set a *System Messages* channel in settings for Bob to greet users.", ephemeral: true);
                    return;
                }

                // Check user permissions for managing the system channel
                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}>.\n- Ask a user with **Manage Channel** permission.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
            }

            // If the system channel is null
            if (systemChannel != null)
            {
                // Check if Bob has permission to send messages in the system channel
                var bobPermissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(systemChannel);
                if (!bobPermissions.SendMessages || !bobPermissions.ViewChannel)
                {
                    await FollowupAsync($"❌ Bob cannot view or send messages in <#{systemChannel.Id}>.\n- Give Bob `View Channel` and `Send Messages` permissions.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
            }

            // Update server welcome information
            using (var context = new BobEntities())
            {
                var server = await context.GetServer(Context.Guild.Id);
                if (server.Welcome != welcome)
                {
                    server.Welcome = welcome;
                    await context.UpdateServer(server);
                }
            }

            // Send response based on welcome setting
            if (welcome)
            {
                await FollowupAsync(systemChannel == null
                    ? "❌ Bob knows to welcome users now, but you **need** to set a *System Messages* channel in settings for this to take effect."
                    : $"✅ Bob will now greet people in <#{systemChannel.Id}>", ephemeral: true);
            }
            else
            {
                await FollowupAsync("✅ Bob will not greet people anymore.", ephemeral: true);
            }
        }

        [SlashCommand("set-message", "Create a custom welcome message for your server!")]
        public async Task SetCustomWelcomeMessage([Summary("message", "Whatever you want said to your users. Type @ where you want the ping.")] string message)
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if the user has manage channels permissions.
            if (!discordUser.GuildPermissions.Administrator)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You do not have a **System Messages** channel set in your server.\n- You can change this in the **Overview** tab of your server's settings.", ephemeral: true);
                    return;
                }

                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission **Manage Channel**.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
            }

            // Check if the user has premium.
            if (!Premium.IsPremium(Context.Interaction.Entitlements))
            {
                await FollowupAsync($"✨ This is a *premium* feature.", ephemeral: true);
                return;
            }

            // Check if the message is within Discord's length requirements.
            if (message.Length + (34 * message.Count(c => c == '@')) > 2000)
            {
                await FollowupAsync($"❌ The message length either exceeds Discord's **2000** character limit or could when mentions are inserted.\n- Try shortening your message.\n- Every mention is assumed to be a length of **32** characters plus **3** formatting characters.", ephemeral: true);
                return;
            }

            // Update server welcome information.
            using var context = new BobEntities();
            var server = await context.GetServer(Context.Guild.Id);

            // Only write to DB if needed.
            if (server.CustomWelcomeMessage != message)
            {
                server.CustomWelcomeMessage = message;
                await context.UpdateServer(server);
            }

            if (server.Welcome)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync($"❌ Bob knows to welcome users now, and what to say, but you **need** to set a *System Messages* channel in settings for this to take effect.\nYour welcome message will look like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                }
                else
                {
                    await FollowupAsync($"✅ Bob will now greet people in <#{systemChannel.Id}> like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                }
            }
            else
            {
                await FollowupAsync($"✅ Bob knows what to say, but you **need** to enable welcome messages with `/welcome toggle` for it to take effect.\nYour welcome message will look like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
            }
        }

        [SlashCommand("remove-message", "Removes the custom welcome message from your server. Does not disable general welcome messages.")]
        public async Task RemoveCustomWelcomeMessage()
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if the user has manage channels permissions.
            if (!discordUser.GuildPermissions.Administrator)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You do not have a **System Messages** channel set in your server.\n- You can change this in the **Overview** tab of your server's settings.", ephemeral: true);
                    return;
                }

                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission **Manage Channel**.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
            }
            // Update server welcome information.
            else
            {
                Server server;
                using var context = new BobEntities();
                server = await context.GetServer(Context.Guild.Id);

                // Only write to DB if needed.
                if (!string.IsNullOrEmpty(server.CustomWelcomeMessage))
                {
                    server.CustomWelcomeMessage = "";
                    await context.UpdateServer(server);
                }

                await FollowupAsync(text: $"✅ Bob will no longer greet people with the custom message.", ephemeral: true);
            }
        }
    }
}
