using System;
using System.Threading.Tasks;
using Challenges;
using Discord;

namespace Commands.Helpers
{
    /// <summary>
    /// Provides helper methods for implementing Tic Tac Toe (TTT) game logic.
    /// </summary>
    public static class TTTMethods
    {
        private static readonly Random Random = new();

        /// <summary>
        /// Determines the player who goes first.
        /// </summary>
        /// <returns>True if Player 1 goes first, otherwise false.</returns>
        public static bool DetermineFirstTurn()
        {
            return Random.Next(0, 2) == 1;
        }

        /// <summary>
        /// Generates button components for displaying Tic Tac Toe grid.
        /// </summary>
        /// <param name="grid">Current state of the game grid.</param>
        /// <param name="turns">Number of turns taken.</param>
        /// <param name="id">Unique identifier for the game.</param>
        /// <param name="forfeited">Indicates if the game was forfeited.</param>
        /// <returns>A <see cref="ComponentBuilder"/> object representing the buttons.</returns>
        public static ComponentBuilder GetButtons(int[,] grid, int turns, ulong id, bool forfeited = false)
        {
            var buttons = new ComponentBuilder();
            bool gameOver = GetWinnerOutcome(grid, turns) > 0 || turns == 9 || forfeited;

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    bool isOccupied = grid[x, y] > 0;
                    string label = isOccupied ? (grid[x, y] == 1 ? "O" : "X") : "\U0000200E";
                    var style = isOccupied ? (grid[x, y] == 1 ? ButtonStyle.Primary : ButtonStyle.Danger) : ButtonStyle.Secondary;

                    buttons.WithButton(label, $"ttt:{x}-{y}:{id}", style, row: y, disabled: isOccupied || gameOver);
                }
            }

            return buttons;
        }

        /// <summary>
        /// Creates an embed for displaying game information.
        /// </summary>
        /// <param name="isPlayer1Turn">Indicates if it's Player 1's turn.</param>
        /// <param name="description">Description of the game state.</param>
        /// <returns>An <see cref="EmbedBuilder"/> object representing the embed.</returns>
        public static Embed CreateEmbed(bool isPlayer1Turn, string description)
        {
            return new EmbedBuilder
            {
                Color = isPlayer1Turn ? Challenge.Player1Color : Challenge.Player2Color,
                Description = description
            }.Build();
        }

        /// <summary>
        /// Determines the winner of the game based on the current state.
        /// </summary>
        /// <param name="grid">Current state of the game grid.</param>
        /// <param name="turns">Number of turns taken.</param>
        /// <param name="isPlayer1Turn">Indicates if it's Player 1's turn.</param>
        /// <param name="forfeited">Indicates if the game was forfeited.</param>
        /// <returns>The winner of the game as a <see cref="Challenge.WinCases"/> enum value.</returns>
        public static Challenge.WinCases GetWinner(int[,] grid, int turns, bool isPlayer1Turn, bool forfeited = false)
        {
            int winner = GetWinnerOutcome(grid, turns);

            if (winner == 2 || (forfeited && isPlayer1Turn))
            {
                return Challenge.WinCases.Player2;
            }
            else if (winner == 0 && turns == 9 || (forfeited && turns == 0))
            {
                return Challenge.WinCases.Tie;
            }
            else
            {
                return Challenge.WinCases.Player1;
            }
        }


        /// <summary>
        /// Determines the outcome of the game.
        /// </summary>
        /// <param name="grid">Current state of the game grid.</param>
        /// <param name="turns">Number of turns taken.</param>
        /// <returns>
        /// The winner of the game:
        /// 0 for no winner,
        /// 1 for Player 1,
        /// 2 for Player 2.
        /// </returns>
        public static int GetWinnerOutcome(int[,] grid, int turns)
        {
            if (turns < 3)
            {
                return 0;
            }

            for (int i = 0; i < 3; i++)
            {
                if (grid[i, 0] == grid[i, 1] && grid[i, 1] == grid[i, 2] && grid[i, 0] != 0)
                {
                    return grid[i, 0];
                }
                if (grid[0, i] == grid[1, i] && grid[1, i] == grid[2, i] && grid[0, i] != 0)
                {
                    return grid[0, i];
                }
            }

            if (grid[0, 0] == grid[1, 1] && grid[1, 1] == grid[2, 2] && grid[0, 0] != 0)
            {
                return grid[0, 0];
            }
            if (grid[0, 2] == grid[1, 1] && grid[1, 1] == grid[2, 0] && grid[0, 2] != 0)
            {
                return grid[0, 2];
            }

            return 0;
        }

        /// <summary>
        /// Allows the bot to make its move in the Tic Tac Toe game.
        /// </summary>
        /// <param name="game">Instance of the TicTacToe class representing the game.</param>
        public static async Task BotPlay(TicTacToe game)
        {
            int[] move = FindWinningMove(game.grid, 2) ??
                         FindWinningMove(game.grid, 1) ??
                         GetOptimalMove(game.grid, 2) ??
                         GetRandomValidMove(game.grid);

            if (IsValidMove(game.grid, move))
            {
                game.grid[move[0], move[1]] = 2;
                await game.EndBotTurn();
            }
            else
            {
                await BotPlay(game);
            }
        }

        private static int[] FindWinningMove(int[,] grid, int player)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (grid[i, j] == 0)
                    {
                        grid[i, j] = player;

                        if (GetWinnerOutcome(grid, -1) == player)
                        {
                            grid[i, j] = 0;
                            return new[] { i, j };
                        }

                        grid[i, j] = 0;
                    }
                }
            }

            return null;
        }

        private static int[] GetOptimalMove(int[,] grid, int player)
        {
            int bestScore = player == 1 ? int.MaxValue : int.MinValue;
            int[] bestMove = null;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (grid[i, j] == 0)
                    {
                        grid[i, j] = player;
                        int score = Minimax(grid, 0, player == 2);
                        grid[i, j] = 0;

                        if ((player == 2 && score > bestScore) || (player == 1 && score < bestScore))
                        {
                            bestScore = score;
                            bestMove = new[] { i, j };
                        }
                    }
                }
            }

            return bestMove;
        }

        private static int Minimax(int[,] grid, int depth, bool isMaximizing)
        {
            int winner = GetWinnerOutcome(grid, -1);
            if (winner != 0)
            {
                return winner == 1 ? -1 : 1;
            }

            if (isMaximizing)
            {
                int maxEval = int.MinValue;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[i, j] == 0)
                        {
                            grid[i, j] = 2;
                            int eval = Minimax(grid, depth + 1, false);
                            grid[i, j] = 0;
                            maxEval = Math.Max(maxEval, eval);
                        }
                    }
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[i, j] == 0)
                        {
                            grid[i, j] = 1;
                            int eval = Minimax(grid, depth + 1, true);
                            grid[i, j] = 0;
                            minEval = Math.Min(minEval, eval);
                        }
                    }
                }
                return minEval;
            }
        }

        private static int[] GetRandomValidMove(int[,] grid)
        {
            while (true)
            {
                int row = Random.Next(0, 3);
                int col = Random.Next(0, 3);

                if (grid[row, col] == 0)
                {
                    return new[] { row, col };
                }
            }
        }

        private static bool IsValidMove(int[,] grid, int[] move)
        {
            return move != null && move[0] >= 0 && move[0] < 3 && move[1] >= 0 && move[1] < 3 && grid[move[0], move[1]] == 0;
        }
    }
}
