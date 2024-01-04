using System;
using System.Threading.Tasks;
using Challenges;
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
        public int turns = 0;

        public TicTacToe(IUser player1, IUser player2) : base(GameType.TicTacToe, onePerChannel, player1, player2)
        {

        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;

            // Pick Turn
            isPlayer1Turn = TTTMethods.DetermineFirstTurn();

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Format Message
            var dateTime = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\nAnswer <t:{dateTime}:R>.").Build(); x.Components = TTTMethods.GetButtons(grid, turns, Id).Build(); });

            if (!isPlayer1Turn)
            {
                await TTTMethods.BotPlay(this);
            }
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Pick Turn
            isPlayer1Turn = TTTMethods.DetermineFirstTurn();

            var dateTime = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

            await interaction.UpdateAsync(x => { x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\nAnswer <t:{dateTime}:R>.").Build(); x.Components = TTTMethods.GetButtons(grid, turns, Id).Build(); });
        }

        public override Task EndGame()
        {
            Challenge.RemoveFromSpecificGameList(this);
            return Task.CompletedTask;
        }

        public async Task EndBotTurn(SocketMessageComponent component = null)
        {
            turns++;

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = TTTMethods.GetWinner(grid, turns);
            if (winner > 0 || turns >= 9)
            {
                properties = (x) =>
                {
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, GetFinalTitle(winner)).Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };

                // Remove Game
                await EndGame();
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
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.").Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };
            }

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

        public async Task EndTurn(SocketMessageComponent component)
        {
            turns++;

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = TTTMethods.GetWinner(grid, turns);
            if (winner > 0 || turns >= 9)
            {
                properties = (x) =>
                {
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, GetFinalTitle(winner)).Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };

                // Remove Game
                await EndGame();
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
                    x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.").Build();
                    x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
                };
            }

            await component.ModifyOriginalResponseAsync(properties);
        }

        private string GetFinalTitle(int winner)
        {
            // All ways for player1 to lose
            if (winner == 2)
            {
                return $"### ⚔️ {Player1.Mention} Was Defeated By {Player2.Mention} in {Title}.";
            }
            else if (winner == 0 && turns == 9) // draw
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