using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [RequireBotPermission(GuildPermission.ManageGuild)]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)]
    [Group("automod", "All commands relevant to automod features.")]
    public class AutomodGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("links", "Add link auto moderation. Prevent links from being sent in this server.")]
        public async Task AutoModLinks()
        {
            await DeferAsync(ephemeral: true);

            if (!await Automod.CanBeAdded(Context.Guild, 1))
            {
                await FollowupAsync(text: $"❌ Links {Automod.keyWordTiggersMessage}", ephemeral: true);
                return;
            }

            var actionProperties = new AutoModRuleActionProperties
            {
                CustomMessage = $"Links are prohibited in this server.",
                Type = AutoModActionType.BlockMessage
            };

            void rules(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Link Detection Via Bob";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.RegexPatterns = Automod.Patterns.LinkPatterns;
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Links are now prohibited in your server.", ephemeral: true);
        }

        [SlashCommand("phone-numbers", "Add phone number auto moderation. Prevent phone numbers from being sent in this server.")]
        public async Task AutoModPhoneNumbers([Summary("strict", "If checked (true) numbers like 1234567890 will be blocked.")] bool strict)
        {
            await DeferAsync(ephemeral: true);

            if (!await Automod.CanBeAdded(Context.Guild, 1))
            {
                await FollowupAsync(text: $"❌ Phone number {Automod.keyWordTiggersMessage}", ephemeral: true);
                return;
            }

            var actionProperties = new AutoModRuleActionProperties
            {
                CustomMessage = $"Phone numbers are prohibited in this server.",
                Type = AutoModActionType.BlockMessage
            };

            void rules(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Phone Number Detection Via Bob";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;

                if (strict)
                {
                    properties.RegexPatterns = Automod.Patterns.StrictPhoneNumberPatterns;
                }
                else
                {
                    properties.RegexPatterns = Automod.Patterns.RelaxedPhoneNumberPatterns;
                }
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Phone numbers are now prohibited in your server.", ephemeral: true);
        }

        [SlashCommand("zalgo-text", "Add zalgo-text auto moderation. Prevent glitchy text from being sent in this server.")]
        public async Task AutoModZalgoText()
        {
            await DeferAsync(ephemeral: true);

            if (!await Automod.CanBeAdded(Context.Guild, 1))
            {
                await FollowupAsync(text: $"❌ Zalgo text (glitchy text) {Automod.keyWordTiggersMessage}", ephemeral: true);
                return;
            }

            var actionProperties = new AutoModRuleActionProperties
            {
                CustomMessage = $"Glitchy text is prohibited in this server.",
                Type = AutoModActionType.BlockMessage
            };

            void rules(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Zalgo Text Detection Via Bob";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.RegexPatterns = Automod.Patterns.ZalgoTextPatterns;
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Zalgo text (glitchy text) is now prohibited in your server.", ephemeral: true);
        }

        [SlashCommand("bad-words", "Add bad word auto moderation. Prevent bad words from being sent in this server.")]
        public async Task AutoModBadWords()
        {
            await DeferAsync(ephemeral: true);

            if (!await Automod.CanBeAdded(Context.Guild, 2))
            {
                await FollowupAsync(text: $"❌ Bad word {Automod.keyWordTiggersMessage}\n- Note, that this command adds two new rules due to how many words it checks for.", ephemeral: true);
                return;
            }

            var actionProperties = new AutoModRuleActionProperties
            {
                CustomMessage = $"This phrase is prohibited in this server.",
                Type = AutoModActionType.BlockMessage
            };

            void rules(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Bad Word Detection Via Bob 1";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.KeywordFilter = ConfessFiltering.BannedWords.Take(1000).ToArray();
            }

            void rulesContinued(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Bad Word Detection Via Bob 2";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.KeywordFilter = ConfessFiltering.BannedWords.Skip(1000).ToArray();
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await Context.Guild.CreateAutoModRuleAsync(rulesContinued);
            await FollowupAsync(text: $"✅ Bad words are now prohibited in your server.", ephemeral: true);
        }
        
        [SlashCommand("invite-links", "Add invite link auto moderation. Prevent invites from being sent in this server.")]
        public async Task AutoModInvites()
        {
            await DeferAsync(ephemeral: true);

            if (!await Automod.CanBeAdded(Context.Guild, 1))
            {
                await FollowupAsync(text: $"❌ Invite link {Automod.keyWordTiggersMessage}", ephemeral: true);
                return;
            }

            var actionProperties = new AutoModRuleActionProperties
            {
                CustomMessage = $"Server Invites are prohibited in this server.",
                Type = AutoModActionType.BlockMessage
            };

            void rules(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Invite Link Detection Via Bob";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.RegexPatterns = Automod.Patterns.InviteLinkPatterns;
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Invite links are now prohibited in your server.", ephemeral: true);
        }
    }
}