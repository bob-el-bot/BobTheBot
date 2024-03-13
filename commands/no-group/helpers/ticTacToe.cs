using System;
using System.Threading.Tasks;
using Challenges;
using Database;
using Discord;
using Discord.WebSocket;
using Games;

namespace Commands.Helpers
{
    public class TicTacToe : Games.Game
    {
        public override string Title { get; } = "Tic Tac Toe";
        public const bool onePerChannel = false;

        public int[,] grid = new int[3, 3];
        public bool isPlayer1Turn;
        public int turns;

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
            isPlayer1Turn = TTTMethods.DetermineFirstTurn();

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(5));
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            Expired += Challenge.ExpireGame;

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in <t:{dateTime}:R>)").Build(); x.Components = TTTMethods.GetButtons(grid, turns, Id).Build(); });

            if (!isPlayer1Turn)
            {
                await TTTMethods.BotPlay(this);
            }
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Set State
            State = GameState.Active;

            // Pick Turn
            isPlayer1Turn = TTTMethods.DetermineFirstTurn();

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn ( Forfeit <t:{dateTime}:R>).").Build(); x.Components = TTTMethods.GetButtons(grid, turns, Id).Build(); });
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

                    float winner = TTTMethods.GetWinner(grid, turns, isPlayer1Turn, true);

                    // Update User Info
                    using var context = new BobEntities();
                    var userIds = new[] { Player1.Id, Player2.Id };
                    var users = await context.GetUsers(userIds);

                    foreach (var user in users)
                    {
                        user.TotalTicTacToeGames++;

                        if ((user.Id == Player1.Id && winner == 1.0f) || (user.Id == Player2.Id && winner == 2.0f))
                        {
                            user.TicTacToeWins++;
                        }
                        else if (winner == 0.5f)
                        {
                            user.TicTacToeWins += 0.5f;
                        }
                    }

                    await context.UpdateUsers(users);
                }
                await Message.ModifyAsync(x => { x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, GetFinalTitle(true)).Build(); x.Components = TTTMethods.GetButtons(grid, turns, Id, true).Build(); });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public async Task EndBotTurn(SocketMessageComponent component = null)
        {
            turns++;

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = TTTMethods.GetWinnerOutcome(grid, turns);
            if (winner > 0 || turns >= 9)
            {
                properties = (x) =>
                {
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, GetFinalTitle()).Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };

                await FinishGame(component, properties);
            }
            else // not over
            {
                if (isPlayer1Turn)
                {
                    isPlayer1Turn = false;
                }
                else
                {
                    isPlayer1Turn = true;
                }

                var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

                properties = (x) =>
                {
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in <t:{dateTime}:R>)").Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };

                if (component != null)
                {
                    await component.ModifyOriginalResponseAsync(properties);
                }
                else
                {
                    await Message.ModifyAsync(properties);
                }

                if (!isPlayer1Turn && winner == 0)
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

                    float winner = TTTMethods.GetWinner(grid, turns, isPlayer1Turn);

                    // Update User Info
                    using var context = new BobEntities();
                    var userIds = new[] { Player1.Id, Player2.Id };
                    var users = await context.GetUsers(userIds);

                    foreach (var user in users)
                    {
                        user.TotalTicTacToeGames++;

                        if ((user.Id == Player1.Id && winner == 1.0f) || (user.Id == Player2.Id && winner == 2.0f))
                        {
                            user.TicTacToeWins++;
                        }
                        else if (winner == 0.5f)
                        {
                            user.TicTacToeWins += 0.5f;
                        }
                    }

                    await context.UpdateUsers(users);
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
            turns++;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = TTTMethods.GetWinnerOutcome(grid, turns);
            if (winner > 0 || turns >= 9)
            {
                properties = (x) =>
                {
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, GetFinalTitle()).Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };

                await FinishGame(component, properties);
            }
            else // not over
            {
                if (isPlayer1Turn)
                {
                    isPlayer1Turn = false;
                }
                else
                {
                    isPlayer1Turn = true;
                }

                properties = (x) =>
                {
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn (Forfeit <t:{dateTime}:R>).").Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };

                await component.ModifyOriginalResponseAsync(properties);
            }
        }

        private string GetFinalTitle(bool forfeited = false)
        {
            float winner = TTTMethods.GetWinner(grid, turns, isPlayer1Turn, forfeited);

            // All ways for player1 to lose
            if (winner == 2.0f)
            {
                return $"### ⚔️ {Player1.Mention} Was Defeated By {Player2.Mention} in {Title}.";
            }
            else if (winner == 0.5f && turns == 9 || (forfeited && turns == 0)) // draw
            {
                return $"### ⚔️ {Player1.Mention} Drew {Player2.Mention} in {Title}.";
            }
            else // else player1 won
            {
                return $"### ⚔️ {Player1.Mention} Defeated {Player2.Mention} in {Title}.";
            }
        }
    }
}