using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
namespace Commands.Helpers
{
    public static class Automod
    {
        public static readonly string keyWordTiggersMessage = "auto moderation **cannot** be added because Discord's maximum of **6 KeyWord triggered rules** has already been reached or will be exceeded with this rule added.\n- Try deleting an automod rule which uses the **KeyWord trigger**.";

        public static async Task<bool> canBeAdded(SocketGuild guild, int newRulesCount)
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
    }
}