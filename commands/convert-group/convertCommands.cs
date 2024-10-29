using System;
using System.Threading.Tasks;
using Commands.Helpers;
using Debug;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using UnitsNet;

namespace Commands
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("convert", "All conversion commands.")]
    public class ConvertGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("units", "Bob will convert units for you.")]
        public async Task ConvertUnit(Conversion.UnitType unitType, string amount, string fromUnit, string toUnit)
        {
            try
            {
                // Parse the quantity from user input
                if (double.TryParse(amount, out double value))
                {
                    Type unitEnumType = Conversion.GetUnitEnumType(unitType);

                    // Attempt parsing units with enhanced logic
                    if (Conversion.TryParseUnit(fromUnit, unitEnumType, out Enum fromUnitEnum) && Conversion.TryParseUnit(toUnit, unitEnumType, out Enum toUnitEnum))
                    {
                        IQuantity quantity = Quantity.From(value, fromUnitEnum);
                        IQuantity convertedQuantity = quantity.ToUnit(toUnitEnum);

                        await RespondAsync($"{Conversion.GetUnitTypeEmoji(unitType)} `{value}` **{fromUnit}** is equal to `{convertedQuantity.Value}` **{toUnit}**.");
                    }
                    else
                    {
                        // Provide feedback on invalid units with a list of valid units
                        string validUnits = Conversion.GetValidUnits(unitType);
                        await RespondAsync($"❌ Invalid unit specified. Please use a valid unit for the specified quantity type:\n- {validUnits}\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)");
                    }
                }
                else
                {
                    await RespondAsync("❌ Invalid amount specified.\n- Please provide a numeric value.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)");
                }
            }
            catch (Exception ex)
            {
                await RespondAsync($"❌ An unexpected error occurred: {ex.Message}\n- Try again later.\n- The developers have been notified, but you can join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and provide us with more details if you want.");
                SocketTextChannel logChannel = (SocketTextChannel)Bot.Client.GetGuild(Bot.supportServerId).GetChannel(Bot.Token != "${{TEST_TOKEN}}" ? Bot.systemLogChannelId : Bot.devLogChannelId);
                await Logger.LogErrorToDiscord(logChannel, Context, $"{ex}");
                return;
            }
        }
    }
}