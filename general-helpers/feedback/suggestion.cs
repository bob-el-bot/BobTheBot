using System;
using System.Threading.Tasks;
using Commands.Helpers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Feedback
{
    public class Suggestion : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly ulong suggestionChannelId = 1301279825264115832;

        private static readonly Lazy<SocketTextChannel> suggestionChannel = new(() =>
        {
            // Fetch the channel only once when first accessed
            return (SocketTextChannel)Bot.Client.GetGuild(Bot.supportServerId).GetChannel(suggestionChannelId);
        });

        public static async Task SuggestUnitToDiscord(IInteractionContext ctx, UnitConversion.UnitType unitType, string suggestion)
        {
            IUser user = ctx.User;

            await suggestionChannel.Value.SendMessageAsync($"Unit of type: **{unitType}** suggestion from `{user.Id}`\n```{suggestion}```");
        }
    }
}
