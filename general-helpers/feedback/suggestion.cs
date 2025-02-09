using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Bob.Debug;
using Bob.Commands.Helpers;

namespace Bob.Feedback
{
    public class Suggestion : InteractionModuleBase<SocketInteractionContext>
    {
        public static async Task SuggestUnitToDiscord(IInteractionContext ctx, UnitConversion.UnitType unitType, string suggestion)
        {
            IUser user = ctx.User;

            await Logger.feedbackChannel.Value.SendMessageAsync($"Unit of type: **{unitType}** suggestion from `{user.Id}`\n```{suggestion}```");
        }
    }
}
