using System;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Bob.PremiumInterface;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("quote", "All quoting commands.")]
    public class QuoteGroup : InteractionModuleBase<ShardedInteractionContext>
    {

        [SlashCommand("new", "Create a quote.")]
        public async Task New(
            [Summary("quote", "The text you want quoted. Quotation marks (\") will be added.")] string quote,
            [Summary("user", "The user who the quote belongs to (defaults to you).")] SocketUser user = null,
            [Summary("tag1", "A tag for sorting quotes later on (needs premium).")] string tag1 = "",
            [Summary("tag2", "A tag for sorting quotes later on (needs premium).")] string tag2 = "",
            [Summary("tag3", "A tag for sorting quotes later on (needs premium).")] string tag3 = "")
        {
            await DeferAsync(ephemeral: true);

            var server = await QuoteMethods.GetServerAsync(Context.Guild.Id);
            if (await QuoteMethods.ValidateServerAndChannel(server, Context) == false)
            {
                return;
            }

            if (await QuoteMethods.ValidateQuoteLength(quote, server, Context) == false)
            {
                return;
            }

            if (await QuoteMethods.ValidateTags(tag1, tag2, tag3, Context) == false)
            {
                return;
            }

            if (user == null) {
                user = Context.User;
            }

            var embed = QuoteMethods.CreateQuoteEmbed(quote, user, DateTimeOffset.UtcNow, Context.User.GlobalName, tag1, tag2, tag3);
            await QuoteMethods.SendQuoteAsync(server.QuoteChannelId, embed, "Quote made in", Context);
        }

        [MessageCommand(name: "Quote")]
        public async Task Quote(IMessage message)
        {
            await DeferAsync(ephemeral: true);

            var quote = message.Content;
            var user = (SocketUser)message.Author;

            var server = await QuoteMethods.GetServerAsync(Context.Guild.Id);
            if (await QuoteMethods.ValidateServerAndChannel(server, Context) == false)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(quote))
            {
                await FollowupAsync("❌ The message you tried quoting is invalid.", ephemeral: true);
                return;
            }

            if (await QuoteMethods.ValidateQuoteLength(quote, server, Context) == false)
            {
                return;
            }

            var embed = QuoteMethods.CreateQuoteEmbed(quote, user, message.Timestamp, Context.User.GlobalName, originalMessageUrl: message.GetJumpUrl());
            await QuoteMethods.SendQuoteAsync(server.QuoteChannelId, embed, "Quote made.", Context);
        }

        [SlashCommand("channel", "Configure /quote channel.")]
        public async Task Settings([Summary("channel", "The quotes channel for the server.")][ChannelTypes(ChannelType.Text)] SocketChannel channel)
        {
            var permissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel);

            // Check permissions
            if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
            {
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`.", ephemeral: true);
            }
            // Check if Bob has permission to send messages in given channel
            else if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks)
            {
                await RespondAsync(text: $"❌ Bob is either missing permissions to view, send messages, *or* embed links in the channel <#{channel.Id}>.\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`, and `Embed Links`.\n- Use {Help.GetCommandMention("quote channel")} to set a new channel.", ephemeral: true);
            }
            else
            {
                await DeferAsync(ephemeral: true);

                using (var context = new BobEntities())
                {
                    Server server = await context.GetServer(Context.Guild.Id);

                    // Set the channel for this server
                    server.QuoteChannelId = channel.Id;
                    await context.UpdateServer(server);
                }

                await FollowupAsync(text: $"✅ <#{channel.Id}> is now the quote channel for the server.", ephemeral: true);
            }
        }

        [SlashCommand("set-max-length", "Set a maximum character length for quotes.")]
        public async Task SetMaxQuoteLength([Summary("length", "The number of characters you would like, at most, to be in a quote (Discord has a limit 4096).")] int length)
        {
            await DeferAsync(ephemeral: true);

            using var context = new BobEntities();
            Server server = await context.GetServer(Context.Guild.Id);

            // Check if the user has manage channels permissions.
            if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
            {
                await FollowupAsync(text: $"❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`.", ephemeral: true);
            }
            // Check if the user has premium.
            else if (await Premium.IsPremiumAsync(Context.Interaction.Entitlements, Context.User.Id) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", components: Premium.GetComponents(), ephemeral: true);
            }
            // Check if the message is within Discord's length requirements.
            else if (length > 4096)
            {
                await FollowupAsync(text: $"❌ The length **{length}** exceeds Discord's **4096** character limit in embeds.\n- Try making your max length smaller.", ephemeral: true);
            }
            else if (length < 1)
            {
                await FollowupAsync(text: $"❌ The length **{length}** is too small\n- Try making your max length bigger than 0.", ephemeral: true);
            }
            else if (length < server.MinQuoteLength)
            {
                await FollowupAsync(text: $"❌ You cannot make your maximum quote length smaller than your minimum quote length.\n- Try making your max length bigger than **{server.MinQuoteLength}** (your minimum length value).", ephemeral: true);
            }
            // Update server welcome information.
            else
            {
                // Only write to DB if needed.
                if (server.MaxQuoteLength != length)
                {
                    server.MaxQuoteLength = (uint)length;
                    await context.UpdateServer(server);
                }

                await FollowupAsync(text: $"✅ Your server now has a maximum quote length of **{length}**.");
            }
        }

        [SlashCommand("set-min-length", "Set a minimum character length for quotes.")]
        public async Task SetMinQuoteLength([Summary("length", "The number of characters you would like, at least, to be in a quote.")] int length)
        {
            await DeferAsync(ephemeral: true);

            using var context = new BobEntities();
            Server server = await context.GetServer(Context.Guild.Id);

            // Check if the user has manage channels permissions.
            if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
            {
                await FollowupAsync(text: $"❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`.", ephemeral: true);
            }
            // Check if the user has premium.
            else if (await Premium.IsPremiumAsync(Context.Interaction.Entitlements, Context.User.Id) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", components: Premium.GetComponents(), ephemeral: true);
            }
            // Check if the message is within Discord's length requirements.
            else if (length > 4096)
            {
                await FollowupAsync(text: $"❌ The length **{length}** exceeds Discord's **4096** character limit in embeds.\n- Try making your minimum length smaller.", ephemeral: true);
            }
            else if (length < 1)
            {
                await FollowupAsync(text: $"❌ The length **{length}** is too small.\n- Try making your minimum length bigger than 0.", ephemeral: true);
            }
            else if (length > server.MaxQuoteLength)
            {
                await FollowupAsync(text: $"❌ You cannot make your minimum quote length bigger than your maximum quote length.\n- Try making your minimum length smaller than **{server.MaxQuoteLength}** (your maximum length value).", ephemeral: true);
            }
            // Update server welcome information.
            else
            {
                // Only write to DB if needed.
                if (server.MinQuoteLength != length)
                {
                    server.MinQuoteLength = (uint)length;
                    await context.UpdateServer(server);
                }

                await FollowupAsync(text: $"✅ Your server now has a minimum quote length of **{length}**.");
            }
        }
    }
}
