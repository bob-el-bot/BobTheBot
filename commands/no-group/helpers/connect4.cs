using System;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.WebSocket;
using Games;
using Time.Timestamps;

namespace Commands.Helpers
{
    public class Connect4 : Games.Game
    {
        public override string Title { get; } = "Connect 4";
        private static readonly bool onePerChannel = false;

        public int[,] Grid { get; set; } = new int[7, 6];
        public int LastMoveColumn { get; set; }
        public int LastMoveRow { get; set; }
        public bool IsPlayer1Turn { get; set; }
        public int Turns { get; set; }

        public Connect4(IUser player1, IUser player2) : base(GameType.Connect4, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
        {

        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;
            State = GameState.Active;

            // Pick Turn
            IsPlayer1Turn = Challenge.DetermineFirstTurn();

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(5));

            Expired += Challenge.ExpireGame;

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in {Timestamp.FromDateTime(ExpirationTime, Timestamp.Formats.Relative)})\n{Connect4Methods.GetGrid(Grid)}"); x.Components = Connect4Methods.GetButtons(this).Build(); });

            if (!IsPlayer1Turn)
            {
                await Connect4Methods.BotPlay(this);
            }
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Set State
            State = GameState.Active;

            // Pick Turn
            IsPlayer1Turn = Challenge.DetermineFirstTurn();

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn ( Forfeit {Timestamp.FromDateTime(ExpirationTime, Timestamp.Formats.Relative)}).\n{Connect4Methods.GetGrid(Grid)}"); x.Components = Connect4Methods.GetButtons(this).Build(); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;
            Challenge.WinCases outcome = Connect4Methods.GetWinner(this, true);

            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, outcome);
                }

                await Message.ModifyAsync(x => { x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"{Challenge.CreateFinalTitle(this, outcome)}\n{Connect4Methods.GetGrid(Grid)}", Challenge.GetFinalThumnnailUrl(Player1, Player2, outcome)); x.Components = Connect4Methods.GetButtons(this, true).Build(); });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public async Task EndBotTurn(SocketMessageComponent component = null)
        {
            Turns++;

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = Connect4Methods.GetWinnerOutcome(Grid, Turns, LastMoveColumn, LastMoveRow);
            if (winner > 0 || Turns >= 42)
            {
                Challenge.WinCases outcome = Connect4Methods.GetWinner(this);

                properties = (x) =>
                {
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"{Challenge.CreateFinalTitle(this, outcome)}\n{Connect4Methods.GetGrid(Grid)}", Challenge.GetFinalThumnnailUrl(Player1, Player2, outcome));
                    x.Components = Connect4Methods.GetButtons(this).Build();
                };

                await FinishGame(component, properties);
            }
            else // not over
            {
                if (IsPlayer1Turn)
                {
                    IsPlayer1Turn = false;
                }
                else
                {
                    IsPlayer1Turn = true;
                }

                properties = (x) =>
                {
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in {Timestamp.FromDateTime(ExpirationTime, Timestamp.Formats.Relative)})\n{Connect4Methods.GetGrid(Grid)}");
                    x.Components = Connect4Methods.GetButtons(this).Build();
                };

                if (component != null)
                {
                    await component.ModifyOriginalResponseAsync(properties);
                }
                else
                {
                    await Message.ModifyAsync(properties);
                }

                if (!IsPlayer1Turn && winner == 0)
                {
                    await Connect4Methods.BotPlay(this);
                }
            }
        }

        private async Task FinishGame(SocketMessageComponent interaction, Action<MessageProperties> properties)
        {
            try
            {
                if (interaction != null)
                {
                    await interaction.ModifyOriginalResponseAsync(properties);
                }
                else
                {
                    await Message.ModifyAsync(properties);
                }

                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, Connect4Methods.GetWinner(this));
                }
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }

            // End Game
            _ = EndGame();
        }

        public async Task EndTurn(SocketMessageComponent component)
        {
            Turns++;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = Connect4Methods.GetWinnerOutcome(Grid, Turns, LastMoveColumn, LastMoveRow);
            if (winner > 0 || Turns >= 42)
            {
                Challenge.WinCases outcome = Connect4Methods.GetWinner(this);

                properties = (x) =>
                {
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"{Challenge.CreateFinalTitle(this, outcome)}\n{Connect4Methods.GetGrid(Grid)}", Challenge.GetFinalThumnnailUrl(Player1, Player2, outcome));
                    x.Components = Connect4Methods.GetButtons(this).Build();
                };

                await FinishGame(component, properties);
            }
            else // not over
            {
                if (IsPlayer1Turn)
                {
                    IsPlayer1Turn = false;
                }
                else
                {
                    IsPlayer1Turn = true;
                }

                properties = (x) =>
                {
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn (Forfeit {Timestamp.FromDateTime(ExpirationTime, Timestamp.Formats.Relative)}).\n{Connect4Methods.GetGrid(Grid)}");
                    x.Components = Connect4Methods.GetButtons(this).Build();
                };

                await component.ModifyOriginalResponseAsync(properties);
            }
        }
    }
}