using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PremiumInterface;
using Time.Timestamps;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("quote", "All quoting commands.")]
    public class QuoteGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("new", "Create a quote.")]
        public async Task New([Summary("quote", "The text you want quoted. Quotation marks (\") will be added.")] string quote, [Summary("user", "The user who the quote belongs to.")] SocketUser user, [Summary("tag1", "A tag for sorting quotes later on (needs premium).")] string tag1 = "", [Summary("tag2", "A tag for sorting quotes later on (needs premium).")] string tag2 = "", [Summary("tag3", "A tag for sorting quotes later on (needs premium).")] string tag3 = "")
        {
            await DeferAsync(ephemeral: true);

            Server server;
            using (var context = new BobEntities())
            {
                server = await context.GetServer(Context.Guild.Id);
            }

            if (server.QuoteChannelId == null)
            {
                await FollowupAsync(text: "❌ Use `/quote channel` first (a quote channel is not set in this server).\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                return;
            }

            var channel = Context.Guild.GetChannel((ulong)server.QuoteChannelId);

            if (channel == null)
            {
                await FollowupAsync(text: "❌ The currently set quote channel no longer exists.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                return;
            }

            var permissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(channel);

            if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks)
            {
                await FollowupAsync(text: $"❌ Bob is either missing permissions to view, send messages, *or* embed links in the channel <#{server.QuoteChannelId}>.\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`, and `Embed Links`.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (quote.Length > (server.MaxQuoteLength ?? 4096) || quote.Length < server.MinQuoteLength) // 4096 is max characters in an embed description.
            {
                await FollowupAsync($"❌ The quote *cannot* be made because it contains **{quote.Length}** characters.\n- this server's maximum quote length is **{server.MaxQuoteLength}**.\n- this server's minimum quote length is **{server.MinQuoteLength}**.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
            }
            else if (!string.IsNullOrWhiteSpace(tag1) || !string.IsNullOrWhiteSpace(tag2) || !string.IsNullOrWhiteSpace(tag3) && Premium.IsPremium(Context.Interaction.Entitlements) == false) // contains tags and does not have premium
            {
                await FollowupAsync($"❌ You cannot add tags.\n- Get ✨ premium to use **tags**.", components: Premium.GetComponents());
            }
            else
            {
                // Format Quote
                string formattedQuote = quote;
                if (quote[0] != '"' && quote[^1] != '"')
                {
                    formattedQuote = "\"" + quote + "\"";
                }

                // Check if the quote contains any mentions or links
                bool containsMentionsOrLinks = quote.Contains("<@") || quote.Contains("<#") || quote.Contains("http");

                // Create embed
                EmbedBuilder embed = new();

                if (formattedQuote.Length <= 256 && !containsMentionsOrLinks)
                {
                    // If the quote is short enough and does not contain mentions or links, use title
                    embed = new EmbedBuilder
                    {
                        Title = $"{formattedQuote}",
                        Description = $"-{user.Mention}, {Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow, Timestamp.Formats.Relative)}"
                    };
                }
                else
                {
                    // Use description for quote to fit up to 4096 characters or contains mentions/links
                    string description = $"**{formattedQuote}**\n-{user.Mention}, {Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow, Timestamp.Formats.Relative)}";
                    embed = new EmbedBuilder
                    {
                        Title = "",
                        Description = description
                    };
                }

                embed.Color = new Color(0x2B2D31);
                embed.WithAuthor(new EmbedAuthorBuilder().WithName(user.GlobalName).WithIconUrl(user.GetAvatarUrl()));

                // Footer
                StringBuilder footerText = new();
                if (!string.IsNullOrWhiteSpace(tag1) || !string.IsNullOrWhiteSpace(tag2) || !string.IsNullOrWhiteSpace(tag3))
                {
                    footerText.Append("Tag(s): ");
                    List<string> tags = new();

                    if (!string.IsNullOrWhiteSpace(tag1))
                    {
                        tags.Add(tag1);
                    }
                    if (!string.IsNullOrWhiteSpace(tag2))
                    {
                        tags.Add(tag2);
                    }
                    if (!string.IsNullOrWhiteSpace(tag3))
                    {
                        tags.Add(tag3);
                    }

                    footerText.Append(string.Join(", ", tags)); // Join the tags with ", "
                    footerText.Append(" | ");
                }
                footerText.Append($"Quoted by {Context.User.GlobalName}");
                embed.WithFooter(footer => footer.Text = footerText.ToString());

                // Respond
                await FollowupAsync(text: $"🖊️ Quote made.", ephemeral: true);

                // Send quote in quotes channel of server
                await ((ISocketMessageChannel)channel).SendMessageAsync(embed: embed.Build());
            }
        }

        [MessageCommand(name: "Quote")]
        public async Task Quote(IMessage message)
        {
            await DeferAsync(ephemeral: true);

            // Parse Message
            string quote = message.Content;
            SocketUser user = (SocketUser)message.Author;

            Server server;
            using (var context = new BobEntities())
            {
                server = await context.GetServer(Context.Guild.Id);
            }

            if (server.QuoteChannelId == null)
            {
                await FollowupAsync(text: "❌ Use `/quote channel` first (a quote channel is not set in this server).\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                return;
            }

            var channel = Context.Guild.GetChannel((ulong)server.QuoteChannelId);

            if (channel == null)
            {
                await FollowupAsync(text: "❌ The currently set quote channel no longer exists.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }

            var permissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(channel);

            if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks)
            {
                await FollowupAsync(text: $"❌ Bob is either missing permissions to view, send messages, *or* embed links in the channel <#{server.QuoteChannelId}>.\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`, and `Embed Links`.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (quote == null || quote == "")
            {
                await FollowupAsync(text: "❌ The message you tried quoting is invalid. \n- Embeds can't be quoted. \n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (quote.Length > (server.MaxQuoteLength ?? 4096) || quote.Length < server.MinQuoteLength) // 4096 is max characters in an embed description.
            {
                await FollowupAsync($"❌ The quote *cannot* be made because it contains **{quote.Length}** characters.\n- this server's maximum quote length is **{server.MaxQuoteLength}**.\n- this server's minimum quote length is **{server.MinQuoteLength}**.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
            }
            else
            {
                // Format Quote
                string formattedQuote = quote;
                if (quote[0] != '"' && quote[^1] != '"')
                {
                    formattedQuote = $"\"{quote}\"";
                }

                // Check if the quote contains any mentions or links
                bool containsMentionsOrLinks = quote.Contains("<@") || quote.Contains("<#") || quote.Contains("http");

                // Create embed
                EmbedBuilder embed = new();

                if (formattedQuote.Length <= 256 && !containsMentionsOrLinks)
                {
                    // If the quote is short enough and does not contain mentions or links, use title
                    embed = new EmbedBuilder
                    {
                        Title = $"{formattedQuote}",
                        Description = $"-{user.Mention}, {Timestamp.FromDateTimeOffset(message.Timestamp, Timestamp.Formats.Relative)}"
                    };
                }
                else
                {
                    // Use description for quote to fit up to 4096 characters or contains mentions/links
                    string description = $"**{formattedQuote}**\n-{user.Mention}, {Timestamp.FromDateTimeOffset(message.Timestamp, Timestamp.Formats.Relative)}";
                    embed = new EmbedBuilder
                    {
                        Title = "",
                        Description = description
                    };
                }

                if (embed.Description.Length > 4096)
                {
                    await FollowupAsync($"❌ The quote *cannot* be made because it contains **{embed.Description.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
                }
                else
                {
                    embed.Color = new Color(0x2B2D31);
                    embed.WithAuthor(new EmbedAuthorBuilder().WithName(user.GlobalName).WithIconUrl(user.GetAvatarUrl()));

                    // Orignal Message Field
                    embed.AddField(name: "Original Message", value: $"{message.GetJumpUrl()}");

                    // Footer
                    string footerText = $"Quoted by {Context.User.GlobalName}";
                    embed.WithFooter(footer => footer.Text = footerText);

                    // Respond
                    await FollowupAsync(text: $"🖊️ Quote made.", ephemeral: true);

                    // Send quote in quotes channel of server
                    await ((ISocketMessageChannel)channel).SendMessageAsync(embed: embed.Build());
                }
            }
        }

        [SlashCommand("channel", "Configure /quote channel.")]
        public async Task Settings([Summary("channel", "The quotes channel for the server.")][ChannelTypes(ChannelType.Text)] SocketChannel channel)
        {
            var permissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel);

            // Check permissions
            if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
            {
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if Bob has permission to send messages in given channel
            else if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks)
            {
                await RespondAsync(text: $"❌ Bob is either missing permissions to view, send messages, *or* embed links in the channel <#{channel.Id}>.\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`, and `Embed Links`.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
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
                await FollowupAsync(text: $"❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if the user has premium.
            else if (Premium.IsPremium(Context.Interaction.Entitlements) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.", components: Premium.GetComponents(), ephemeral: true);
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
                await FollowupAsync(text: $"❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: `Manage Channels`\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if the user has premium.
            else if (Premium.IsPremium(Context.Interaction.Entitlements) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.", components: Premium.GetComponents(), ephemeral: true);
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
