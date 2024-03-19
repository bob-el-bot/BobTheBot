using System;
using System.Threading.Tasks;
using ColorMethods;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.VisualBasic;
using PremiumInterface;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("profile", "All profile commands.")]
    public class ProfileGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("display", "View a user's profile.")]
        public async Task Display([Summary("user", "The user whose profile you would like displayed (leave empty to display your own).")] SocketUser user = null)
        {
            user ??= Context.User;

            if (user.IsBot)
            {
                await RespondAsync("❌ Bot users do not have profiles, or stats for that matter...", ephemeral: true);
            }
            else
            {
                await DeferAsync();

                // Get Stats
                User userToDisplay;
                using var context = new BobEntities();
                userToDisplay = await context.GetUser(user.Id);

                float rpsWinPercent = (float)Math.Round(userToDisplay.TotalRockPaperScissorsGames != 0 ? userToDisplay.RockPaperScissorsWins / userToDisplay.TotalRockPaperScissorsGames * 100 : 0, 2);
                float tttWinPercent = (float)Math.Round(userToDisplay.TotalTicTacToeGames != 0 ? userToDisplay.TicTacToeWins / userToDisplay.TotalTicTacToeGames * 100 : 0, 2);
                float triviaWinPercent = (float)Math.Round(userToDisplay.TotalTriviaGames != 0 ? userToDisplay.TriviaWins / userToDisplay.TotalTriviaGames * 100 : 0, 2);

                // Check Premium
                bool hasPremium = Premium.IsValidPremium(userToDisplay.PremiumExpiration);

                Color color = hasPremium ? Colors.TryGetColor(userToDisplay.ProfileColor) : 0x2C2F33;

                // Create Embed
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder().WithName($"{user.Username}'s Profile").WithIconUrl(user.GetAvatarUrl()),
                    Color = color,
                    Description = hasPremium ? "💜✨ *Premium*" : null
                };

                embed.AddField(name: $"✂️ Rock Paper Scissors", value: $"Total Wins: `{userToDisplay.RockPaperScissorsWins}`\nTotal Games: `{userToDisplay.TotalRockPaperScissorsGames}`\nWin %: `{rpsWinPercent}%`", inline: true)
                .AddField(name: $"⭕ Tic Tac Toe", value: $"Total Wins: `{userToDisplay.TicTacToeWins}`\nTotal Games: `{userToDisplay.TotalTicTacToeGames}`\nWin %: `{tttWinPercent}%`", inline: true)
                .AddField(name: $"❓ Trivia", value: $"Total Wins: `{userToDisplay.TriviaWins}`\nTotal Games: `{userToDisplay.TotalTriviaGames}`\nWin %: `{triviaWinPercent}%`", inline: true);

                // Respond
                await FollowupAsync(embed: embed.Build());
            }
        }

        [SlashCommand("set-color", "Set your profile's embed color.")]
        public async Task SetColor([Summary("color", "A color name (purple), or valid hex code (#8D52FD).")] string color)
        {
            await DeferAsync(ephemeral: true);

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            Color finalColor = Colors.TryGetColor(color);

            // Check if the user has premium.
            if (Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n-{Premium.HasPremiumMessage}", ephemeral: true);
            }
            else if (finalColor == 0)
            {
                await FollowupAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                // Only write to DB if needed.
                if (user.ProfileColor != color)
                {
                    user.ProfileColor = color;
                    await context.UpdateUser(user);
                }

                // Create Embed
                var embed = new EmbedBuilder
                {
                    Title = "⬅️ Your new profile color.",
                    Color = finalColor,
                };

                // Respond
                await FollowupAsync(embed: embed.Build());
            }
        }
    }
}
