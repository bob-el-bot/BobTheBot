using System.Linq;
using System.Threading.Tasks;
using Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Commands
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
                properties.RegexPatterns = new string[] { @"(http|https|ftp|ftps):\/\/([\w.-]+)\.([a-zA-Z]{2,})([\w\.\&\?\:\%\=\#\/\-]*)?" };
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Links are now prohibited in your server.", ephemeral: true);
        }

        [SlashCommand("phone-numbers", "Add phone number auto moderation. Prevent phone numbers from being sent in this server.")]
        public async Task AutoModPhoneNumbers([Summary("strict", "If checked (true) numbers like 1234567890 will be blocked.")] bool strict)
        {
            await DeferAsync(ephemeral: true);

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

                string pattern;
                if (strict)
                {
                    pattern = @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$";
                }
                else
                {
                    pattern = @"^\s*(?:\+?(\d{1,3}))?[-. (]*\d{1,3}[-. )]+\d{1,3}[-. ]+\d{1,4}(?: *x(\d+))?\s*$";
                }
                properties.RegexPatterns = new string[] { pattern };
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Phone numbers are now prohibited in your server.", ephemeral: true);
        }

        [SlashCommand("zalgo-text", "Add zalgo-text auto moderation. Prevent glitchy text from being sent in this server.")]
        public async Task AutoModZalgoText()
        {
            await DeferAsync(ephemeral: true);

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
                properties.RegexPatterns = new string[] { @"[\p{M}\p{S}\p{C}]" };
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Zalgo text (glitchy text) is now prohibited in your server.", ephemeral: true);
        }

        [SlashCommand("bad-words", "Add bad word auto moderation. Prevent bad words from being sent in this server.")]
        public async Task AutoModBadWords()
        {
            await DeferAsync(ephemeral: true);

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
                properties.RegexPatterns = new string[] { @"\b(?:https?://)?(?:www\.)?(?:discord\.(?:gg|io|me|li|com(?:/invite)?))/?(?:invite/)?([a-zA-Z0-9-]+)\b" };
            }

            await Context.Guild.CreateAutoModRuleAsync(rules);
            await FollowupAsync(text: $"✅ Invite links are now prohibited in your server.", ephemeral: true);
        }
    }
}