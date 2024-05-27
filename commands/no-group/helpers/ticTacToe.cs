using System;
using System.Threading.Tasks;
using Challenges;
using Database;
using Discord;
using Discord.WebSocket;
using Games;
using TimeStamps;

namespace Commands.Helpers
{
    public class TicTacToe : Games.Game
    {
        public override string Title { get; } = "Tic Tac Toe";
        public const bool onePerChannel = false;

        public int[,] Grid = new int[3, 3];
        public bool IsPlayer1Turn;
        public int Turns;

        public TicTacToe(IUser player1, IUser player2) : base(GameType.TicTacToe, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
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

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)}"); x.Components = TTTMethods.GetButtons(Grid, Turns, Id).Build(); });

            if (!IsPlayer1Turn)
            {
                await TTTMethods.BotPlay(this);
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

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn (Forfeit {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)})."); x.Components = TTTMethods.GetButtons(Grid, Turns, Id).Build(); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;
            Challenge.WinCases outcome = TTTMethods.GetWinner(Grid, Turns, IsPlayer1Turn, true);

            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, outcome);
                }

                await Message.ModifyAsync(x => { x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, Challenge.CreateFinalTitle(this, outcome), Challenge.GetFinalThumnnailUrl(Player1, Player2, outcome)); x.Components = TTTMethods.GetButtons(Grid, Turns, Id, true).Build(); });
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
            int winner = TTTMethods.GetWinnerOutcome(Grid, Turns);
            if (winner > 0 || Turns >= 9)
            {
                Challenge.WinCases outcome = TTTMethods.GetWinner(Grid, Turns, IsPlayer1Turn);

                properties = (x) =>
                {
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, Challenge.CreateFinalTitle(this, outcome), Challenge.GetFinalThumnnailUrl(Player1, Player2, outcome));
                    x.Components = TTTMethods.GetButtons(Grid, Turns, Id).Build();
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
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)})");
                    x.Components = TTTMethods.GetButtons(Grid, Turns, Id).Build();
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
                    await TTTMethods.BotPlay(this);
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

                    await Challenge.UpdateUserStats(this, TTTMethods.GetWinner(Grid, Turns, IsPlayer1Turn));
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
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = TTTMethods.GetWinnerOutcome(Grid, Turns);
            if (winner > 0 || Turns >= 9)
            {
                Challenge.WinCases outcome = TTTMethods.GetWinner(Grid, Turns, IsPlayer1Turn);

                properties = (x) =>
                {
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, Challenge.CreateFinalTitle(this, outcome), Challenge.GetFinalThumnnailUrl(Player1, Player2, outcome));
                    x.Components = TTTMethods.GetButtons(Grid, Turns, Id).Build();
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
                    x.Embed = Challenge.CreateTurnBasedEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn (Forfeit {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)}).");
                    x.Components = TTTMethods.GetButtons(Grid, Turns, Id).Build();
                };

                await component.ModifyOriginalResponseAsync(properties);
            }
        }
    }
}