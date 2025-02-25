using System;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Interactions;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("admin", "All commands relevant to administration features.")]
    public class AdminGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [Group("confess", "All commands relevant to confession administration features.")]
        public class ConfessGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("filter-toggle", "Enable or disable censoring and/or blocking of /confess messages in this server.")]
            public async Task ConfessionsFilterToggle([Summary("enable", "If enabled (true), Bob will censor and/or block flagged messages sent in this server with /confess.")] bool enable)
            {
                await DeferAsync(ephemeral: true);

                var discordUser = Context.Guild.GetUser(Context.User.Id);

                // Check if user is an administrator
                if (!discordUser.GuildPermissions.Administrator)
                {
                    await FollowupAsync(text: "❌ You must have the `Administrator` permission to use this command.", ephemeral: true);
                    return;
                }

                Server server;
                using (var context = new BobEntities())
                {
                    server = await context.GetServer(Context.Guild.Id);

                    if (server.ConfessFilteringOff == enable)
                    {
                        server.ConfessFilteringOff = !enable;
                        await context.UpdateServer(server);
                    }
                }

                if (enable)
                {
                    await FollowupAsync(text:
                        $"✅ **Confession filtering enabled!**\n" +
                        $"Bob will now **censor and/or block** flagged messages sent with {Help.GetCommandMention("confess")}.\n" +
                        $"\n🚨 **Users who attempt to bypass the filter will receive progressive punishments (a step-ban on confess usage).**",
                        ephemeral: true);
                }
                else
                {
                    await FollowupAsync(text:
                        $"✅ **Confession filtering disabled!**\n" +
                        $"Messages sent with {Help.GetCommandMention("confess")} will no longer be filtered.\n" +
                        $"\n❗ **Flagged words will not be blocked, and users will not be punished. Use with caution.**",
                        ephemeral: true);
                }
            }
        }
    }
}