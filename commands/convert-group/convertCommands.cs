using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Commands.Helpers;
using Debug;
using Discord;
using Discord.Interactions;
using Feedback.Models;
using Time.Timestamps;
using Time.Timezones;
using UnitsNet;

namespace Commands
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("convert", "All conversion commands.")]
    public class ConvertGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("units", "Bob will convert units for you.")]
        public async Task ConvertUnit(UnitConversion.UnitType unitType, string amount, string fromUnit, string toUnit)
        {
            try
            {
                // Parse the quantity from user input
                if (double.TryParse(amount, out double value))
                {
                    Type unitEnumType = UnitConversion.GetUnitEnumType(unitType);

                    // Attempt parsing units with enhanced logic
                    if (UnitConversion.TryParseUnit(fromUnit, unitEnumType, out Enum fromUnitEnum) && UnitConversion.TryParseUnit(toUnit, unitEnumType, out Enum toUnitEnum))
                    {
                        IQuantity quantity = Quantity.From(value, fromUnitEnum);
                        IQuantity convertedQuantity = quantity.ToUnit(toUnitEnum);

                        await RespondAsync($"{UnitConversion.GetUnitTypeEmoji(unitType)} `{value}` **{fromUnit}** is equal to `{convertedQuantity.Value}` **{toUnit}**.");
                    }
                    else
                    {
                        // Provide feedback on invalid units with a list of valid units
                        string validUnits = UnitConversion.GetValidUnits(unitType);
                        await RespondAsync($"❌ Invalid unit specified. Please use a valid unit for the specified quantity type like:\n- {validUnits}\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", components: UnitConversion.GetSuggestionButton(unitType), ephemeral: true);
                    }
                }
                else
                {
                    await RespondAsync("❌ Invalid amount specified.\n- Please provide a numeric value.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, false);
            }
        }

        [ComponentInteraction("suggestUnit:*", true)]
        public async Task SuggestUnitButton(string type)
        {
            try
            {
                var modal = new SuggestUnitModal
                {
                    Content = "Please provide a brief description of the unit you would like to suggest."
                };

                await Context.Interaction.RespondWithModalAsync(modal: modal, customId: $"suggestUnitModal:{type}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [ModalInteraction("suggestUnitModal:*", true)]
        public async Task SuggestUnitModalHandler(string type, SuggestUnitModal modal)
        {
            await DeferAsync();

            var unitType = (UnitConversion.UnitType)Enum.Parse(typeof(UnitConversion.UnitType), type);

            await Feedback.Suggestion.SuggestUnitToDiscord(Context, unitType, modal.Content);

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "✅ Suggestion made successfully!\n- This will be manually reviewed as soon as possible.\n- Thanks for the idea!"; x.Components = null; });
        }

        [SlashCommand("timezones", "Convert time from one timezone to another.")]
        public async Task ConvertTime(
            [Summary("month", "The month for the time you want to convert.")][MinValue(1)][MaxValue(12)] int month,
            [Summary("day", "The day for the time you want to convert.")][MinValue(1)][MaxValue(31)] int day,
            [Summary("hour", "The hour for the time you want to convert, in 24-hour format.")][MinValue(0)][MaxValue(23)] int hour,
            [Summary("minute", "The minute for the time you want to convert.")][MinValue(0)][MaxValue(59)] int minute,
            [Summary("from-timezone", "The timezone to convert from.")] Timezone sourceTimezone,
            [Summary("to-timezone", "The timezone you want to convert to.")] Timezone destinationTimezone)
        {
            try
            {
                // Validate the day based on the month and current year
                if (day < 1 || day > DateTime.DaysInMonth(DateTime.UtcNow.Year, month))
                {
                    await RespondAsync($"❌ Please enter a valid day between **1** and **{DateTime.DaysInMonth(DateTime.UtcNow.Year, month)}**.", ephemeral: true);
                    return;
                }

                var destinationDateTime = TimeConverter.ConvertBetweenTimezones(month, day, hour, minute, sourceTimezone, destinationTimezone);
                var sourceDateTime = new DateTime(DateTime.UtcNow.Year, month, day, hour, minute, 0, DateTimeKind.Unspecified);

                await RespondAsync($"{TimeConversion.GetClosestTimeEmoji(destinationDateTime)} {Timestamp.FromDateTime(sourceDateTime, Timestamp.Formats.Exact)} in {sourceTimezone.ToDisplayName()} is {Timestamp.FromDateTime(destinationDateTime, Timestamp.Formats.Exact)} in {destinationTimezone.ToDisplayName()}.");
            }
            catch (TimeZoneNotFoundException)
            {
                await RespondAsync("❌ One of the specified timezones is invalid. Please check your input.", ephemeral: true);
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, false);
            }
        }

        [SlashCommand("qr-code", "Bob will convert a link or text to a QR code.")]
        public async Task ConvertToQRCode(string content, QRCodeConverter.ErrorCorrectionLevel errorCorrectionLevel = QRCodeConverter.ErrorCorrectionLevel.L)
        {
            // Calculate the payload size of the content in bytes and determine the QR version
            int payloadSize = Encoding.UTF8.GetByteCount(content);
            int qrVersion = QRCodeConverter.GetSuitableVersion(payloadSize, errorCorrectionLevel);

            // If QR version is invalid, handle the error
            if (qrVersion == -1)
            {
                await RespondAsync(QRCodeConverter.GetPayloadSizeErrorMessage(payloadSize, errorCorrectionLevel), ephemeral: true);
                return;
            }

            // Initialize the stream variable
            MemoryStream stream = null;

            try
            {
                await DeferAsync();

                // Generate the QR code as an optimized PNG
                stream = QRCodeConverter.CreateQRCodePng(content, suitableVersion: qrVersion, eccLevel: errorCorrectionLevel);

                // Create and send the embed with the QR code image
                var embed = new EmbedBuilder()
                    .WithTitle("🔳 QR Code Generated")
                    .WithImageUrl("attachment://qr-code.png")
                    .WithFooter($"Error correction level: {QRCodeConverter.GetErrorCorrectionLevelDisplay(errorCorrectionLevel)} | Size: {stream.Length} bytes")
                    .WithColor(new Color(0x2B2D31))
                    .Build();

                await FollowupWithFileAsync(fileStream: stream, fileName: "qr-code.png", embed: embed);
            }
            catch (Exception ex)
            {
                await Logger.HandleUnexpectedError(Context, ex, true);
            }
            finally
            {
                // Ensure the stream is disposed of even if an exception occurs
                stream?.Dispose();
            }
        }
    }
}