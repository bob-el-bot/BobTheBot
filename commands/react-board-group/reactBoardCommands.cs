using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.PremiumInterface;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("react-board", "All react board commands.")]
    public class ReactBoardGroup : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("toggle", "Toggle the react board for this server.")]
        public async Task Toggle([Summary("enable", "If checked (true), the react-board will enable.")] bool enable)
        {
            await DeferAsync(ephemeral: true);

            var discordUser = Context.Guild.GetUser(Context.User.Id);

            using var context = new BobEntities();
            var server = await context.GetServer(Context.Guild.Id);

            SocketTextChannel reactBoardChannel = null;
            if (server.ReactBoardChannelId is ulong channelId)
            {
                reactBoardChannel = Context.Guild.GetChannel(channelId) as SocketTextChannel;
            }

            // Check if user is an administrator
            if (!discordUser.GuildPermissions.Administrator)
            {
                // Check user permissions for managing the ReactBoard Channel
                if (!discordUser.GetPermissions(reactBoardChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{reactBoardChannel.Id}>.\n- Ask a user with `Manage Channel` permission.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
            }

            // Check Bob's permissions in ReactBoard Channel
            if (reactBoardChannel != null)
            {
                var bobPermissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(reactBoardChannel);
                if (!bobPermissions.SendMessages || !bobPermissions.ViewChannel)
                {
                    await FollowupAsync($"❌ Bob cannot view or send messages in <#{reactBoardChannel.Id}>.\n- Give Bob `View Channel` and `Send Messages` permissions.", ephemeral: true);
                    return;
                }
            }

            // Check if the user has premium but allow for disabling as non-premium.
            if (enable && await Premium.IsPremiumAsync(Context.Interaction.Entitlements, Context.User.Id) == false)
            {
                await FollowupAsync($"✨ This is a *premium* feature (atleast for now).\n- {Premium.HasPremiumMessage}", components: Premium.GetComponents(), ephemeral: true);
                return;
            }

            // Update the server's react board setting in the database
            if (server.ReactBoardOn != enable)
            {
                server.ReactBoardOn = enable;
                await context.UpdateServer(server);
            }

            // Respond to the user
            if (enable)
            {
                await FollowupAsync(reactBoardChannel == null
                    ? $"✅ The react board has been enabled, but you **need** to set a react board channel with {Help.GetCommandMention("react-board channel")} for Bob to post messages."
                    : $"✅ The react board is now enabled in <#{reactBoardChannel.Id}>.", ephemeral: true);
            }
            else
            {
                await FollowupAsync("✅ The react board has been disabled.", ephemeral: true);
            }
        }

        [SlashCommand("channel", "Set the channel for the react board.")]
        public async Task SetChannel([Summary("channel", "The channel to set for the react board.")] ITextChannel channel)
        {
            var user = Context.Guild.GetUser(Context.User.Id);
            var bob = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var channelPermissions = bob.GetPermissions(channel as IGuildChannel);

            // Check if user has ManageChannels permission
            if (!user.GuildPermissions.ManageChannels)
            {
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`.", ephemeral: true);
                return;
            }

            // Check Bob's permissions in the target channel
            if (!channelPermissions.ViewChannel || !channelPermissions.SendMessages || !channelPermissions.EmbedLinks)
            {
                await RespondAsync(text: $"❌ Bob is missing permissions in <#{channel.Id}>.\n- Required: `View Channel`, `Send Messages`, `Embed Links`.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            // Update database with the new channel ID
            using var context = new BobEntities();
            var server = await context.GetServer(Context.Guild.Id);

            server.ReactBoardChannelId = channel.Id;
            await context.UpdateServer(server);

            await FollowupAsync(text: $"✅ The react board channel has been set to <#{channel.Id}>.", ephemeral: true);
        }

        [SlashCommand("emoji", "Set the emoji which triggers the react board.")]
        public async Task SetEmoji([Summary("emoji", "The emoji to set for the react board.")] string emoji)
        {
            var user = Context.Guild.GetUser(Context.User.Id);

            // Check if user has ManageChannels permission
            if (!user.GuildPermissions.ManageChannels)
            {
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            // Update database with the new emoji
            using var context = new BobEntities();
            var server = await context.GetServer(Context.Guild.Id);

            server.ReactBoardEmoji = emoji;
            await context.UpdateServer(server);

            await FollowupAsync(text: $"✅ The react board emoji has been set to {emoji}.", ephemeral: true);
        }

        [SlashCommand("minimum-reactions", "Set the minimum reactions required to post on the react board.")]
        public async Task SetMinimumReactions([Summary("minimum_reactions", "The minimum reactions required to post on the react board.")] uint minimumReactions)
        {
            var user = Context.Guild.GetUser(Context.User.Id);

            // Check if user has ManageChannels permission
            if (!user.GuildPermissions.ManageChannels)
            {
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            // Update database with the new minimum reactions
            using var context = new BobEntities();
            var server = await context.GetServer(Context.Guild.Id);
            
            server.ReactBoardMinimumReactions = minimumReactions;
            await context.UpdateServer(server);

            await FollowupAsync(text: $"✅ The minimum reactions required to post on the react board has been set to {minimumReactions}.", ephemeral: true);
        }
    }
}
