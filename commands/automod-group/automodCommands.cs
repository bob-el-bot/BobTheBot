using System;
using System.Collections.Generic;
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
    public class AutomodGroup : InteractionModuleBase<ShardedInteractionContext>
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
                properties.Name = "Bad Word Detection Via Bob 1/2";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.KeywordFilter = CensoredWordSets.AllWords.Take(1000).ToArray();
            }

            void rulesContinued(AutoModRuleProperties properties)
            {
                // Configure your rule properties here
                properties.Name = "Bad Word Detection Via Bob 2/2";
                properties.Actions = new AutoModRuleActionProperties[] { actionProperties };
                properties.TriggerType = AutoModTriggerType.Keyword;
                properties.EventType = AutoModEventType.MessageSend;
                properties.KeywordFilter = CensoredWordSets.AllWords.Skip(1000).ToArray();
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

        [SlashCommand("remove", "Remove a specific Automod action from your server.")]
        public async Task AutoModRemoval([Autocomplete(typeof(AutomodAutocompleteHandler))] string ruleId)
        {
            await DeferAsync(ephemeral: true);

            var rules = await Context.Guild.GetAutoModRulesAsync();

            var ruleToRemove = rules.FirstOrDefault(r => r.Id == ulong.Parse(ruleId));

            if (ruleToRemove == null)
            {
                await FollowupAsync(text: $"❌ Automod rule with ID: {ruleId} not found.", ephemeral: true);
                return;
            }

            if (ruleToRemove.Creator == null || ruleToRemove.Creator.Id == 1)
            {
                await FollowupAsync(
                    text: $"❌ This automod rule could not be removed.\n- It may be enforced by Discord which means it can not be removed.",
                    ephemeral: true);
                return;
            }

            await ruleToRemove.DeleteAsync();

            await FollowupAsync(text: $"✅ Automod rule `{ruleToRemove.Name}` with ID: `{ruleId}` has been removed.", ephemeral: true);
        }

        [SlashCommand("remove-all", "Remove all deletable AutoMod rules from your server.")]
        public async Task AutoModRemoveAll()
        {
            await DeferAsync(ephemeral: true);

            var rules = await Context.Guild.GetAutoModRulesAsync();

            int deletedCount = 0;
            int skippedCount = 0;
            List<string> deletedNames = [];
            List<string> skippedNames = [];

            foreach (var rule in rules)
            {
                if (rule.Creator == null || rule.Creator.Id == 1)
                {
                    skippedCount++;
                    skippedNames.Add(rule.Name);
                    continue;
                }

                try
                {
                    await rule.DeleteAsync();
                    deletedCount++;
                    deletedNames.Add(rule.Name);
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    skippedNames.Add($"{rule.Name} (error: {ex.Message})");
                }
            }

            var response = $"🧹 AutoMod cleanup completed:\n\n" +
                           $"✅ **Deleted**: {deletedCount} rule(s)\n" +
                           $"❌ **Skipped**: {skippedCount} rule(s)\n";

            if (deletedNames.Count > 0)
            {
                response += $"\n**Deleted Rules:**\n- {string.Join("\n- ", deletedNames)}";
            }

            if (skippedNames.Count > 0)
            {
                response += $"\n\n**Skipped Rules:**\n- {string.Join("\n- ", skippedNames)}";
            }

            await FollowupAsync(text: response, ephemeral: true);
        }
    }
}