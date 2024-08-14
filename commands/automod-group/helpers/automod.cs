using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
namespace Commands.Helpers
{
    public static class Automod
    {
        public static readonly string keyWordTiggersMessage = "auto moderation **cannot** be added because Discord's maximum of **6 KeyWord triggered rules** has already been reached or will be exceeded with this rule added.\n- Try deleting an automod rule which uses the **KeyWord trigger**.";

        /// <summary>
        /// Determines if new auto moderation rules can be added to the specified guild.
        /// </summary>
        /// <param name="guild">The guild to check for existing auto moderation rules.</param>
        /// <param name="newRulesCount">The number of new rules to be added.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains <c>true</c> if the new rules can be added without exceeding the limit; 
        /// otherwise, <c>false</c>.
        /// </returns>
        public static async Task<bool> CanBeAdded(SocketGuild guild, int newRulesCount)
        {
            IAutoModRule[] existingRules = await guild.GetAutoModRulesAsync();

            int totalKeyWordTriggerTypeRules = 0;
            foreach (var rule in existingRules)
            {
                if (rule.TriggerType == AutoModTriggerType.Keyword)
                {
                    totalKeyWordTriggerTypeRules++;
                }
            }

            if (totalKeyWordTriggerTypeRules + newRulesCount > 6)
            {
                return false;
            }

            return true;
        }

        public static class Patterns
            {
            public static readonly string[] LinkPatterns =
            {
                @"(http|https|ftp|ftps):\/\/([\w.-]+)\.([a-zA-Z]{2,})([\w\.\&\?\:\%\=\#\/\-]*)?"
            };

            public static readonly string[] StrictPhoneNumberPatterns =
            {
                @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$"
            };

            public static readonly string[] RelaxedPhoneNumberPatterns =
            {
                @"^\s*(?:\+?(\d{1,3}))?[-. (]*\d{1,3}[-. )]+\d{1,3}[-. ]+\d{1,4}(?: *x(\d+))?\s*$"
            };

            public static readonly string[] ZalgoTextPatterns =
            {
                @"[\u0300-\u036F\u0489\u1AB0-\u1AFF\u1DC0-\u1DFF\u20D0-\u20FF\u2CEF-\u2CF1\u2DE0-\u2DFF\uA66F-\uA67F\uFE20-\uFE2F]"
            };

            public static readonly string[] InviteLinkPatterns =
            {
                @"\b(?:https?://)?(?:www\.)?(?:discord\.(?:gg|io|me|li|com(?:/invite)?))/?(?:invite/)?([a-zA-Z0-9-]+)\b"
            };
        }
    }
}