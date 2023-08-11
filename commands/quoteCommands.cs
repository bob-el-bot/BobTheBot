    // [EnabledInDm(false)]
    // [Group("quote", "All quoting commands.")]
    // public class QuoteCommands : InteractionModuleBase<SocketInteractionContext>
    // {
    //     [EnabledInDm(false)]
    //     [SlashCommand("new", "Create a quote.")]
    //     public async Task New([Summary("quote", "The text you want quoted. Quotation marks (\") will be added.")] string quote, [Summary("user", "The user who the quote belongs to.")] SocketUser user, [Summary("tag1", "A tag for sorting quotes later on.")] string tag1 = "", [Summary("tag2", "A tag for sorting quotes later on.")] string tag2 = "", [Summary("tag3", "A tag for sorting quotes later on.")] string tag3 = "")
    //     {
    //         // Date
    //         var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    //         // Fomrat Quote
    //         string formattedQuote = quote;
    //         if (quote[0] != '"' && quote[quote.Length - 1] != '"')
    //         {
    //             formattedQuote = "\"" + quote + "\"";
    //         }

    //         // Create embed
    //         var embed = new Discord.EmbedBuilder
    //         {
    //             Title = $"{formattedQuote}",
    //             Color = new Discord.Color(2895667),
    //             Description = $"-{user.Mention}, <t:{dateTime}:R>"
    //         };

    //         // Footer
    //         string footerText = "";
    //         if (tag1 != "" || tag2 != "" || tag3 != "")
    //         {
    //             footerText += "Tag(s): ";
    //             string[] tags = { tag1, tag2, tag3 };
    //             for (int index = 0; index < tags.Length; index++)
    //             {
    //                 if (tags[index] != "")
    //                 {
    //                     footerText += tags[index];
    //                     if (index < tags.Length - 1)
    //                         footerText += ", ";
    //                 }
    //             }
    //             footerText += " | ";
    //         }
    //         footerText += $"Quoted by {Context.User.GlobalName}";
    //         embed.WithFooter(footer => footer.Text = footerText);

    //         // Respond
    //         await RespondAsync(text: $"🖊️ The quote: **{formattedQuote}**\n-{user.Mention}", ephemeral: true);

    //         // Send quote in quotes channel of server
    //         await Context.Channel.SendMessageAsync(embed: embed.Build());
    //     }

    //     [EnabledInDm(false)]
    //     [SlashCommand("channel", "Configure /quote channel.")]
    //     public async Task Settings([Summary("channel", "The quotes channel for the server.")] SocketChannel channel)
    //     {
    //         // Check permissions
    //         if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
    //             await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: **Manage Channels**\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
    //         // Check if Bob has permission to send messages in given channel
    //         else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.ViewChannel)
    //             await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
    //         else
    //         {
    //             // Check if Bob has a record for this server in DB.
    //             await RespondAsync(text: $"✅ <#{channel.Id}> is now the quote channel for the server.", ephemeral: true);
    //         }
    //     }
    // }