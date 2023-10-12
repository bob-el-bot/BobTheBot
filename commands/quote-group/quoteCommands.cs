using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.VisualBasic;

[EnabledInDm(false)]
[Group("quote", "All quoting commands.")]
public class QuoteCommands : InteractionModuleBase<SocketInteractionContext>
{
    [EnabledInDm(false)]
    [SlashCommand("new", "Create a quote.")]
    public async Task New([Summary("quote", "The text you want quoted. Quotation marks (\") will be added.")] string quote, [Summary("user", "The user who the quote belongs to.")] SocketUser user, [Summary("tag1", "A tag for sorting quotes later on.")] string tag1 = "", [Summary("tag2", "A tag for sorting quotes later on.")] string tag2 = "", [Summary("tag3", "A tag for sorting quotes later on.")] string tag3 = "")
    {
        var server = await Bot.DB.GetServer(Context.Guild.Id);

        if (server.QuoteChannelId == null)
        {
            await RespondAsync(text: "❌ Use `/quote channel` first (a quote channel is not set in this server).\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
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
            var embed = new EmbedBuilder
            {
                Title = $"{formattedQuote}",
                Color = new Color(2895667),
                Description = $"-{user.Mention}, <t:{dateTime}:R>"
            };

            // Footer
            string footerText = "";
            if (tag1 != "" || tag2 != "" || tag3 != "")
            {
                footerText += "Tag(s): ";
                string[] tags = { tag1, tag2, tag3 };
                for (int index = 0; index < tags.Length; index++)
                {
                    if (tags[index] != "")
                    {
                        footerText += tags[index];
                        if (index < tags.Length - 1)
                            footerText += ", ";
                    }
                }
                footerText += " | ";
            }
            footerText += $"Quoted by {Context.User.GlobalName}";
            embed.WithFooter(footer => footer.Text = footerText);

            // Respond
            await RespondAsync(text: $"🖊️ The quote: **{formattedQuote}**\n-{user.Mention}", ephemeral: true);

            // Send quote in quotes channel of server
            var channel = (ISocketMessageChannel)Context.Guild.GetChannel((ulong)server.QuoteChannelId);

            await channel.SendMessageAsync(embed: embed.Build());
        }
    }

    [EnabledInDm(false)]
    [MessageCommand(name: "Quote")]
    public async Task Quote(IMessage message)
    {
        // Parse Message
        string quote = message.Content;
        SocketUser user = (SocketUser)message.Author;

        var server = await Bot.DB.GetServer(Context.Guild.Id);

        if (server.QuoteChannelId == null)
        {
            await RespondAsync(text: "❌ Use `/quote channel` first (a quote channel is not set in this server).\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        }
        else if (quote == null || quote == "")
        {
            await RespondAsync(text: "❌ The message you tried quoting is invalid. \n- Embeds can't be quoted. \n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
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
            var embed = new EmbedBuilder
            {
                Title = $"{formattedQuote}",
                Color = new Color(2895667),
                Description = $"-{user.Mention}, <t:{dateTime}:R>"
            };

            // Footer
            string footerText = "";
            footerText += $"Quoted by {Context.User.GlobalName}";
            embed.WithFooter(footer => footer.Text = footerText);

            // Respond
            await RespondAsync(text: $"🖊️ The quote: **{formattedQuote}**\n-{user.Mention}", ephemeral: true);

            // Send quote in quotes channel of server
            var channel = (ISocketMessageChannel)Context.Guild.GetChannel((ulong) server.QuoteChannelId);

            await channel.SendMessageAsync(embed: embed.Build());
        }
    }

    [EnabledInDm(false)]
    [SlashCommand("channel", "Configure /quote channel.")]
    public async Task Settings([Summary("channel", "The quotes channel for the server.")] SocketChannel channel)
    {
        // Check permissions
        if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
            await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: **Manage Channels**\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        // Check if the channel is a text channel
        if (channel.GetChannelType() != ChannelType.Text)
            await RespondAsync(text: $"❌ The channel <#{channel.Id}> is not a text channel\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        // Check if Bob has permission to send messages in given channel
        else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)channel).ViewChannel)
            await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        else
        {
            await DeferAsync(ephemeral: true);
            var server = await Bot.DB.GetServer(Context.Guild.Id);

            // Set the channel for this server
            server.QuoteChannelId = channel.Id;
            await Bot.DB.UpdateServer(server);
            await FollowupAsync(text: $"✅ <#{channel.Id}> is now the quote channel for the server.", ephemeral: true);
        }
    }
}