using System;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using PremiumInterface;

namespace Commands
{
    [EnabledInDm(true)]
    [Group("profile", "All profile commands.")]
    public class ProfileGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(true)]
        [SlashCommand("display", "View a user's profile.")]
        public async Task Display([Summary("user", "The user whose profile you would like displayed (leave empty to display your own).")] SocketUser user = null)
        {
            user = (user != null ? user : Context.User);

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

                float rpsWinPercent = (float)Math.Round(userToDisplay.RockPaperScissorsWins / userToDisplay.TotalRockPaperScissorsGames * 100, 2);
                float tttWinPercent = (float)Math.Round(userToDisplay.TicTacToeWins / userToDisplay.TotalTicTacToeGames * 100, 2);
                float triviaWinPercent = (float)Math.Round(userToDisplay.TriviaWins / userToDisplay.TotalTriviaGames * 100, 2);

                // Create Embed
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder().WithName($"{user.Username}'s Profile").WithIconUrl(user.GetAvatarUrl()),
                    Color = new Color(0x2C2F33),
                    Description = Premium.IsValidPremium(userToDisplay.PremiumExpiration) ? "💜✨ *Premium*" : null
                };

                embed.AddField(name: $"✂️ Rock Paper Scissors", value: $"Total Wins: `{userToDisplay.RockPaperScissorsWins}`\nTotal Games: `{userToDisplay.TotalRockPaperScissorsGames}`\nWin %: `{rpsWinPercent}%`", inline: true)
                .AddField(name: $"⭕ Tic Tac Toe", value: $"Total Wins: `{userToDisplay.TicTacToeWins}`\nTotal Games: `{userToDisplay.TotalTicTacToeGames}`\nWin %: `{tttWinPercent}%`", inline: true)
                .AddField(name: $"❓ Trivia", value: $"Total Wins: `{userToDisplay.TriviaWins}`\nTotal Games: `{userToDisplay.TotalTriviaGames}`\nWin %: `{triviaWinPercent}%`", inline: true);

                // Respond
                await FollowupAsync(embed: embed.Build());
            }
        }
    }
}
