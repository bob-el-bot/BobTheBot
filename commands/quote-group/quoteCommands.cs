using System;
using System.Text;
using System.Threading.Tasks;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Commands
{
    [EnabledInDm(false)]
    [Group("quote", "All quoting commands.")]
    public class QuoteGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(false)]
        [SlashCommand("new", "Create a quote.")]
        public async Task New([Summary("quote", "The text you want quoted. Quotation marks (\") will be added.")] string quote, [Summary("user", "The user who the quote belongs to.")] SocketUser user, [Summary("tag1", "A tag for sorting quotes later on.")] string tag1 = "", [Summary("tag2", "A tag for sorting quotes later on.")] string tag2 = "", [Summary("tag3", "A tag for sorting quotes later on.")] string tag3 = "")
        {
            await DeferAsync(ephemeral: true);
            
            Server server = await Bot.DB.GetServer(Context.Guild.Id);

            if (server.QuoteChannelId == null)
            {
                await FollowupAsync(text: "❌ Use `/quote channel` first (a quote channel is not set in this server).\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (Context.Guild.GetChannel((ulong)server.QuoteChannelId) == null)
            {
                await FollowupAsync(text: "❌ The currently set quote channel no longer exists.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.GetChannel((ulong)server.QuoteChannelId)).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.GetChannel((ulong)server.QuoteChannelId)).ViewChannel)
            {
                await FollowupAsync(text: $"❌ Bob is either missing permissions to view *or* send messages in the channel <#{server.QuoteChannelId}>.\n- Try giving Bob the following pemrissions: `View Channel`, `Send Messages`.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (quote.Length > 4096) // 4096 is max characters in an embed description.
            {
                await FollowupAsync($"❌ The quote *cannot* be made because it contains **{quote.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
            }
            else
            {
                // Date
                var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Fomrat Quote
                string formattedQuote = quote;
                if (quote[0] != '"' && quote[^1] != '"')
                {
                    formattedQuote = "\"" + quote + "\"";
                }

                // Create embed
                EmbedBuilder embed = new();
                if (formattedQuote.Length <= 256) // 256 characters is the maximum size of an embed title
                {
                    embed = new EmbedBuilder
                    {
                        Title = $"{formattedQuote}",
                        Color = new Color(2895667),
                        Description = $"-{user.Mention}, <t:{dateTime}:R>"
                    };
                }
                else  // use description for quote to fit up to 4096 characters
                {
                    string description = $"**{formattedQuote}**\n-{user.Mention}, <t:{dateTime}:R>";

                    if (description.Length > 4096)
                    {
                        await FollowupAsync($"❌ The quote *cannot* be made because it contains **{description.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
                    }
                    else
                    {
                        embed = new EmbedBuilder
                        {
                            Title = "",
                            Color = new Color(2895667),
                            Description = description
                        };

                        // Footer
                        StringBuilder footerText = new();
                        if (tag1 != "" || tag2 != "" || tag3 != "")
                        {
                            footerText.Append("Tag(s): ");
                            string[] tags = { tag1, tag2, tag3 };
                            for (int index = 0; index < tags.Length; index++)
                            {
                                if (tags[index] != "")
                                {
                                    footerText.Append(tags[index]);
                                    if (index < tags.Length - 1)
                                    {
                                        footerText.Append(", ");
                                    }
                                }
                            }
                            footerText.Append(" | ");
                        }
                        footerText.Append($"Quoted by {Context.User.GlobalName}");
                        embed.WithFooter(footer => footer.Text = footerText.ToString());
                    }
                }

                // Respond
                await FollowupAsync(text: $"🖊️ Quote made.", ephemeral: true);

                // Send quote in quotes channel of server
                var channel = (ISocketMessageChannel)Context.Guild.GetChannel((ulong)server.QuoteChannelId);

                await channel.SendMessageAsync(embed: embed.Build());
            }
        }

        [EnabledInDm(false)]
        [MessageCommand(name: "Quote")]
        public async Task Quote(IMessage message)
        {
            await DeferAsync(ephemeral: true);

            // Parse Message
            string quote = message.Content;
            SocketUser user = (SocketUser)message.Author;

            Server server = await Bot.DB.GetServer(Context.Guild.Id);

            if (server.QuoteChannelId == null)
            {
                await FollowupAsync(text: "❌ Use `/quote channel` first (a quote channel is not set in this server).\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (Context.Guild.GetChannel((ulong)server.QuoteChannelId) == null)
            {
                await FollowupAsync(text: "❌ The currently set quote channel no longer exists.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.GetChannel((ulong)server.QuoteChannelId)).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.GetChannel((ulong)server.QuoteChannelId)).ViewChannel)
            {
                await FollowupAsync(text: $"❌ Bob is either missing permissions to view *or* send messages in the channel <#{server.QuoteChannelId}>.\n- Try giving Bob the following pemrissions: `View Channel`, `Send Messages`.\n- Use `/quote channel` to set a new channel.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (quote == null || quote == "")
            {
                await FollowupAsync(text: "❌ The message you tried quoting is invalid. \n- Embeds can't be quoted. \n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (quote.Length > 4096) // 4096 is max characters in an embed description.
            {
                await FollowupAsync($"❌ The quote *cannot* be made because it contains **{quote.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
            }
            else
            {
                // Date
                var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Fomrat Quote
                string formattedQuote = quote;
                if (quote[0] != '"' && quote[^1] != '"')
                {
                    formattedQuote = "\"" + quote + "\"";
                }

                // Create embed
                EmbedBuilder embed;
                if (formattedQuote.Length <= 256) // 256 characters is the maximum size of an embed title
                {
                    embed = new EmbedBuilder
                    {
                        Title = $"{formattedQuote}",
                        Color = new Color(2895667),
                        Description = $"-{user.Mention}, <t:{dateTime}:R>"
                    };
                }
                else  // use description for quote to fit up to 4096 characters
                {
                    embed = new EmbedBuilder
                    {
                        Title = "",
                        Color = new Color(2895667),
                        Description = $"**{formattedQuote}**\n-{user.Mention}, <t:{dateTime}:R>"
                    };
                }

                if (embed.Description.Length > 4096)
                {
                    await FollowupAsync($"❌ The quote *cannot* be made because it contains **{embed.Description.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
                }
                else
                {
                    // Footer
                    string footerText = $"Quoted by {Context.User.GlobalName}";
                    embed.WithFooter(footer => footer.Text = footerText);

                    // Respond
                    await FollowupAsync(text: $"🖊️ Quote made.", ephemeral: true);

                    // Send quote in quotes channel of server
                    var channel = (ISocketMessageChannel)Context.Guild.GetChannel((ulong)server.QuoteChannelId);

                    await channel.SendMessageAsync(embed: embed.Build());
                }
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("channel", "Configure /quote channel.")]
        public async Task Settings([Summary("channel", "The quotes channel for the server.")] SocketChannel channel)
        {
            // Check permissions
            if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
            {
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: **Manage Channels**\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if the channel is a text channel
            if (channel.GetChannelType() != ChannelType.Text)
            {
                await RespondAsync(text: $"❌ The channel <#{channel.Id}> is not a text channel\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if Bob has permission to send messages in given channel
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
            {
                await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- Try giving Bob the following pemrissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                await DeferAsync(ephemeral: true);
                Server server = await Bot.DB.GetServer(Context.Guild.Id);

                // Set the channel for this server
                server.QuoteChannelId = channel.Id;
                await Bot.DB.UpdateServer(server);
                await FollowupAsync(text: $"✅ <#{channel.Id}> is now the quote channel for the server.", ephemeral: true);
            }
        }
    }
}