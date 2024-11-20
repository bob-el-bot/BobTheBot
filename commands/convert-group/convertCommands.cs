using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Commands.Helpers;
using Debug;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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
                await RespondAsync($"❌ An unexpected error occurred: {ex.Message}\n- Try again later.\n- The developers have been notified, but you can join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and provide us with more details if you want.");

                await Logger.LogErrorToDiscord(Context, $"{ex}");
                return;
            }
        }

        [ComponentInteraction("suggestUnit:*", true)]
        public async Task EditScheduledMessageButton(string type)
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
        public async Task EditMessageModalHandler(string type, SuggestUnitModal modal)
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

                await RespondAsync($"{TimeConversion.GetClosestTimeEmoji(destinationDateTime)} {Timestamp.FromDateTime(TimeConverter.ConvertToUtcTime(month, day, hour, minute, sourceTimezone), Timestamp.Formats.Exact)} in {sourceTimezone.ToDisplayName()} is {Timestamp.FromDateTime(destinationDateTime, Timestamp.Formats.Exact)} in {destinationTimezone.ToDisplayName()}.");
            }
            catch (TimeZoneNotFoundException)
            {
                await RespondAsync("❌ One of the specified timezones is invalid. Please check your input.", ephemeral: true);
            }
            catch (Exception ex)
            {
                await RespondAsync($"❌ An unexpected error occurred: {ex.Message}", ephemeral: true);
            }
        }

        [SlashCommand("qr-code", "Bob will convert a link or text to a qr code.")]
        public async Task ConvertToQRCode(string content, QRCodeConverter.ErrorCorrectionLevel errorCorrectionLevel = QRCodeConverter.ErrorCorrectionLevel.L)
        {
            // Check if the content exceeds the maximum size for the specified error correction level
            if (QRCodeConverter.IsPayloadTooLarge(content, errorCorrectionLevel))
            {
                // Get the maximum allowed size for the current ECC level and version
                int maxAllowedSize = QRCodeConverter.MaxPayloadSizes[39, (int)QRCodeConverter.MapErrorCorrectionLevel(errorCorrectionLevel)];

                // Calculate the payload size of the content in bytes
                int payloadSize = Encoding.UTF8.GetByteCount(content);

                // Calculate how much smaller the content needs to be in bytes
                int sizeDifference = payloadSize - maxAllowedSize;
                int charEstimation = QRCodeConverter.GetCharacterEstimation(sizeDifference);

                var messageBuilder = new StringBuilder();

                messageBuilder.AppendLine("❌ The QR Code **could not** be generated because the given content exceeds the maximum size of a QR code.");

                // Suggest lowering ECC level if ECC level is higher than Low
                if (errorCorrectionLevel > QRCodeConverter.ErrorCorrectionLevel.L)
                {
                    messageBuilder.AppendLine("- Try lowering the error correction level.");
                }

                // Add suggestion for shortening content and the character estimation
                messageBuilder.AppendLine($"- Try shortening the content by at least **{sizeDifference} bytes** (approximately **{charEstimation} characters**).");

                // Send the constructed message
                await RespondAsync(messageBuilder.ToString(), ephemeral: true);
                return;
            }

            MemoryStream stream = null;

            try
            {
                await DeferAsync();

                // Generate the QR code as an optimized PNG (this will return a MemoryStream)
                stream = QRCodeConverter.CreateQRCodePng(content, eccLevel: errorCorrectionLevel);

                // Ensure the stream is at the beginning before using it
                stream.Seek(0, SeekOrigin.Begin);

                // Create an embed for the QR code
                var embed = new EmbedBuilder()
                    .WithTitle("QR Code Generated")
                    .WithImageUrl("attachment://qr-code.png")
                    .WithFooter($"Error correction level: {QRCodeConverter.GetErrorCorrectionLevelDisplay(errorCorrectionLevel)}")
                    .WithColor(new Color(0x2B2D31))
                    .Build();

                // Send the QR code image file to Discord
                await FollowupWithFileAsync(fileStream: stream, fileName: "qr-code.png", embed: embed);
            }
            catch (Exception ex)
            {
                await FollowupAsync($"❌ An unexpected error occurred: {ex.Message}\n- Try again later.\n- The developers have been notified, but you can join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and provide us with more details if you want.");

                await Logger.LogErrorToDiscord(Context, ex.ToString());
                return;
            }
            finally
            {
                // Ensure the stream is disposed of even if an exception occurs
                stream?.Dispose();
            }
        }
    }
}