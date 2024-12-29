using System;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.WebSocket;
using Games;
using Time.Timestamps;

namespace Commands.Helpers
{
    public class RockPaperScissors(IUser player1, IUser player2) : Games.Game(GameType.RockPaperScissors, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
    {
        public override string Title { get; } = "Rock Paper Scissors";
        private static readonly bool onePerChannel = false;

        // -1 = null |  0 = rock | 1 = paper | 2 = scissors
        public int Player1Choice { get; set; } = -1;
        public int Player2Choice { get; set; } = -1;
        private static readonly Random random = new();

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;
            State = GameState.Active;
            BotPlay();

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));

            Expired += Challenge.ExpireGame;

            // Format Message
            var components = new ComponentBuilder().WithButton(label: "Rock", emote: Emote.Parse("<:rock:1323055527290474588>"), customId: $"rps:0:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "📃 Paper", customId: $"rps:1:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "✂️ Scissors", customId: $"rps:2:{Id}", style: ButtonStyle.Secondary);

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = Challenge.CreateEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\nChoose {Timestamp.FromDateTime(ExpirationTime, Timestamp.Formats.Relative)}.", Challenge.DefaultColor); x.Components = components.Build(); });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Set State
            State = GameState.Active;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));

            // Format Message
            var components = new ComponentBuilder().WithButton(label: "Rock", emote: Emote.Parse("<:rock:1323055527290474588>"), customId: $"rps:0:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "📃 Paper", customId: $"rps:1:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "✂️ Scissors", customId: $"rps:2:{Id}", style: ButtonStyle.Secondary);

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = Challenge.CreateEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\nChoose  {Timestamp.FromDateTime(ExpirationTime, Timestamp.Formats.Relative)}.", Challenge.DefaultColor); x.Components = components.Build(); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;
            Challenge.WinCases outcome = GetWinner(true);

            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, outcome);
                }

                await Message.ModifyAsync(x => { x.Embed = Challenge.CreateEmbed(Challenge.CreateFinalTitle(this, outcome), GetColor(outcome), Challenge.GetFinalThumbnailUrl(Player1, Player2, outcome)); x.Components = null; });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public async Task FinishGame(SocketMessageComponent interaction)
        {
            Challenge.WinCases outcome = GetWinner();

            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, outcome);
                }

                string[] options = ["🪨", "📃", "✂️"];
                await interaction.UpdateAsync(x => { x.Embed = Challenge.CreateEmbed($"{Challenge.CreateFinalTitle(this, outcome)}\n{options[Player1Choice]} **VS** {options[Player2Choice]}", GetColor(outcome), Challenge.GetFinalThumbnailUrl(Player1, Player2, outcome)); x.Components = null; });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }

            _ = EndGame();
        }

        private Challenge.WinCases GetWinner(bool forfeited = false)
        {
            // All ways for player1 to lose
            if ((Player1Choice == 0 && Player2Choice == 1) || (Player1Choice == 1 && Player2Choice == 2) || (Player1Choice == 2 && Player2Choice == 0))
            {
                return Challenge.WinCases.Player2;
            }
            else if (Player1Choice == Player2Choice || forfeited)
            {
                return Challenge.WinCases.Tie;
            }
            else // else player1 won
            {
                return Challenge.WinCases.Player1;
            }
        }

        private static Color GetColor(Challenge.WinCases outcome)
        {
            return outcome switch
            {
                Challenge.WinCases.Player1 => Challenge.Player1Color,
                Challenge.WinCases.Player2 => Challenge.Player2Color,
                Challenge.WinCases.Tie => Challenge.BothPlayersColor,
                _ => Challenge.DefaultColor,
            };
        }

        private void BotPlay()
        {
            Player2Choice = random.Next(0, 3);
        }
    }
}