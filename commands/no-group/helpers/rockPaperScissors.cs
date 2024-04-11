using System;
using System.Threading.Tasks;
using Challenges;
using Database;
using Discord;
using Discord.WebSocket;
using Games;

namespace Commands.Helpers
{
    public class RockPaperScissors : Games.Game
    {
        public override string Title { get; } = "Rock Paper Scissors";
        public const bool onePerChannel = false;

        // -1 = null |  0 = rock | 1 = paper | 2 = scissors
        public int player1Choice = -1;
        public int player2Choice = -1;

        public RockPaperScissors(IUser player1, IUser player2) : base(GameType.RockPaperScissors, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
        {

        }

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
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            Expired += Challenge.ExpireGame;

            // Format Message
            var components = new ComponentBuilder().WithButton(label: "🪨 Rock", customId: $"rps:0:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "📃 Paper", customId: $"rps:1:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "✂️ Scissors", customId: $"rps:2:{Id}", style: ButtonStyle.Secondary);

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = CreateEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\nChoose <t:{dateTime}:R>.").Build(); x.Components = components.Build(); });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Set State
            State = GameState.Active;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            // Format Message
            var components = new ComponentBuilder().WithButton(label: "🪨 Rock", customId: $"rps:0:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "📃 Paper", customId: $"rps:1:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "✂️ Scissors", customId: $"rps:2:{Id}", style: ButtonStyle.Secondary);

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = CreateEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\nChoose <t:{dateTime}:R>.").Build(); x.Components = components.Build(); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;

            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, GetWinner(true));
                }

                await Message.ModifyAsync(x => { x.Embed = CreateEmbed(GetFinalTitle(true)).Build(); x.Components = null; });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public async Task FinishGame(SocketMessageComponent interaction)
        {
            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, GetWinner());
                }

                string[] options = { "🪨", "📃", "✂️" };
                await interaction.UpdateAsync(x => { x.Embed = CreateEmbed($"{GetFinalTitle()}\n{options[player1Choice]} **VS** {options[player2Choice]}").Build(); x.Components = null; });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }

            _ = EndGame();
        }

        private static EmbedBuilder CreateEmbed(string description)
        {
            return new EmbedBuilder
            {
                Color = Challenge.DefaultColor,
                Description = description
            };
        }

        private Challenge.WinCases GetWinner(bool forfeited = false)
        {
            // All ways for player1 to lose
            if ((player1Choice == 0 && player2Choice == 1) || (player1Choice == 1 && player2Choice == 2) || (player1Choice == 2 && player2Choice == 0))
            {
                return Challenge.WinCases.Player2;
            }
            else if (player1Choice == player2Choice || forfeited)
            {
                return Challenge.WinCases.Tie;
            }
            else // else player1 won
            {
                return Challenge.WinCases.Player1;
            }
        }

        private string GetFinalTitle(bool forfeited = false)
        {
            Challenge.WinCases winner = GetWinner(forfeited);

            // All ways for player1 to lose
            if (winner == Challenge.WinCases.Player2)
            {
                return $"### ⚔️ {Player1.Mention} Was Defeated By {Player2.Mention} in {Title}.";
            }
            else if (winner == Challenge.WinCases.Tie) // draw
            {
                return $"### ⚔️ {Player1.Mention} Drew {Player2.Mention} in {Title}.";
            }
            else // else player1 won
            {
                return $"### ⚔️ {Player1.Mention} Defeated {Player2.Mention} in {Title}.";
            }
        }

        private void BotPlay()
        {
            Random random = new();
            player2Choice = random.Next(0, 3);
        }
    }
}