using System;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PremiumInterface;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("auto", "All commands relevant to automatic features.")]
    public class AutoGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("publish-announcements", "Bob will automatically publish all messages in announcement channels.")]
        public async Task PublishAnnouncements([Summary("publish", "If checked (true), Bob will auto publish.")] bool publish, [Summary("channel", "Channel to change settings for.")][ChannelTypes(ChannelType.News)] SocketChannel channel)
        {
            await DeferAsync(ephemeral: true);

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            SocketNewsChannel givenNewsChannel = (SocketNewsChannel)channel;

            // Check if the user has Send Message channels permissions.
            IGuildUser fetchedUser = (IGuildUser)Context.User;
            var userPerms = fetchedUser.GetPermissions(givenNewsChannel);
            if (userPerms.SendMessages == false)
            {
                await FollowupAsync(text: $"❌ You do not have the **Send Messages** permission in {givenNewsChannel.Mention}\n- Try asking a user with the permission **Send Messages**.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if the user has premium.
            else if (publish == true && Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", ephemeral: true);
            }
            // Update news channel information.
            else
            {
                NewsChannel newsChannel;
                newsChannel = await context.GetNewsChannel(channel.Id);

                if (publish == true)
                {
                    // Only write to DB if needed.
                    if (newsChannel == null)
                    {
                        newsChannel = new()
                        {
                            Id = channel.Id,
                            ServerId = Context.Guild.Id
                        };

                        await context.AddNewsChannel(newsChannel);
                    }

                    IGuildUser fetchedBot = (IGuildUser)await Context.Channel.GetUserAsync(Bot.Client.CurrentUser.Id);
                    var botPerms = fetchedBot.GetPermissions(givenNewsChannel);
                    if (botPerms.SendMessages == false)
                    {
                        await FollowupAsync(text: $"❌ Bob knows to auto publish now, but you **need** to give Bob the **Send Messages** permission in {givenNewsChannel.Mention} settings for this to take effect.", ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync(text: $"✅ Bob will now auto publish in {givenNewsChannel.Mention}.", ephemeral: true);
                    }
                }
                else
                {
                    // Only write to DB if needed.
                    if (newsChannel != null)
                    {
                        await context.RemoveNewsChannel(newsChannel);
                    }

                    await FollowupAsync(text: $"✅ Bob will no longer auto publish in {givenNewsChannel.Mention}", ephemeral: true);
                }
            }
        }

        [SlashCommand("preview-github", "Bob will automatically preview valid GitHub links (code files, issues, and pull requests).")]
        public async Task PreviewGitHub([Summary("preview", "If checked (true), Bob will auto preview.")] bool preview)
        {
            await DeferAsync(ephemeral: true);

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            // Check if the user has premium.
            if (preview == true && Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", ephemeral: true);
            }
            // Update github preview information.
            else
            {
                Server server;
                server = await context.GetServer(Context.Guild.Id);

                // Only write to DB if needed.
                if (server.AutoEmbedGitHubLinks != preview)
                {
                    server.AutoEmbedGitHubLinks = preview;
                    await context.UpdateServer(server);
                }

                if (preview == true)
                {
                    await FollowupAsync(text: $"✅ Bob will now auto preview GitHub links.", ephemeral: true);
                }
                else
                {
                    await FollowupAsync(text: $"✅ Bob will no longer auto preview GitHub links.", ephemeral: true);
                }
            }
        }

        [SlashCommand("preview-messages", "Bob will automatically preview valid Discord message links.")]
        public async Task PreviewMessages([Summary("preview", "If checked (true), Bob will auto preview.")] bool preview)
        {
            await DeferAsync(ephemeral: true);

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            // Check if the user has premium.
            if (preview == true && Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", ephemeral: true);
            }
            // Update github preview information.
            else
            {
                Server server;
                server = await context.GetServer(Context.Guild.Id);

                // Only write to DB if needed.
                if (server.AutoEmbedMessageLinks != preview)
                {
                    server.AutoEmbedMessageLinks = preview;
                    await context.UpdateServer(server);
                }

                if (preview == true)
                {
                    await FollowupAsync(text: $"✅ Bob will now auto preview Discord message links.", ephemeral: true);
                }
                else
                {
                    await FollowupAsync(text: $"✅ Bob will no longer auto preview Discord message links.", ephemeral: true);
                }
            }
        }
    }
}