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
    [DefaultMemberPermissions(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    [Group("admin", "All commands relevant to administration features.")]
    public class AdminGroup(BobEntities dbContext) : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("info", "Displays this server's configuration and how to update each setting.")]
        public async Task InfoAsync()
        {
            var server = await dbContext.GetOrCreateServerAsync(Context.Guild.Id);

            var embed = AdminUtils.BuildSettingsEmbed(server, Context.Guild);

            var component = new ComponentBuilder()
                .WithButton(Help.SupportServerButton)
                .WithButton(Help.DocsButton)
                .Build();

            await RespondAsync(embed: embed, components: component);
        }

        [Group("confess", "All commands relevant to confession administration features.")]
        public class ConfessGroup(BobEntities dbContext) : InteractionModuleBase<ShardedInteractionContext>
        {
            [SlashCommand("filter-toggle", "Enable or disable censoring and/or blocking of /confess messages in this server.")]
            public async Task ConfessionsFilterToggle([Summary("enable", "If enabled (true), Bob will censor and/or block flagged messages sent in this server with /confess.")] bool enable)
            {
                await DeferAsync(ephemeral: true);

                var server = await dbContext.GetOrCreateServerAsync(Context.Guild.Id);

                if (server.ConfessFilteringOff == enable)
                {
                    server.ConfessFilteringOff = !enable;
                    await dbContext.SaveChangesAsync();
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
                        $"Users can still individually decide if they want to filter messages sent to them.\n" +
                        $"\n❗ **Flagged words will not be blocked, and users will not be punished. Use with caution.**",
                        ephemeral: true);
                }
            }
        }
    }
}