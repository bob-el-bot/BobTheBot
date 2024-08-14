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

            // All ways for player1 to lose
            if (winner == 2 || (forfeited && isPlayer1Turn))
            {
                return Challenge.WinCases.Player2;
            }
            else if (winner == 0 && turns == 9 || (forfeited && turns == 0)) // draw
            {
                return Challenge.WinCases.Tie;
            }
            else // else player1 won
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
            if (turns == -1 || turns >= 3)
            {
                // Check rows, columns, and diagonals
                for (int i = 0; i < 3; i++)
                {
                    // Check rows and columns
                    if (grid[i, 0] == grid[i, 1] && grid[i, 1] == grid[i, 2] && grid[i, 0] != 0)
                    {
                        return grid[i, 0]; // row 
                    }
                    if (grid[0, i] == grid[1, i] && grid[1, i] == grid[2, i] && grid[0, i] != 0)
                    {
                        return grid[0, i]; // column 
                    }
                }

                // Check diagonals
                if (grid[0, 0] == grid[1, 1] && grid[1, 1] == grid[2, 2] && grid[0, 0] != 0)
                {
                    return grid[0, 0]; //top-left to bottom-right
                }
                if (grid[0, 2] == grid[1, 1] && grid[1, 1] == grid[2, 0] && grid[0, 2] != 0)
                {
                    return grid[0, 2]; // top-right to bottom-left
                }
            }

            return 0; // no winner
        }

        /// <summary>
        /// Allows the bot to make its move in the Tic Tac Toe game.
        /// </summary>
        /// <param name="game">Instance of the TicTacToe class representing the game.</param>
        public static async Task BotPlay(TicTacToe game)
        {
            int[] winningMove = FindWinningMove(game.Grid, game.Turns, 2);
            int[] blockingMove = FindWinningMove(game.Grid, game.Turns, 1);

            int[] chosenMove = winningMove ?? blockingMove ?? Minimax(game.Grid, game.Turns, 2) ?? GetRandomValidMove(game.Grid);

            // Check if the chosen move is valid and within bounds
            if (chosenMove[0] >= 0 && chosenMove[0] < 3 && chosenMove[1] >= 0 && chosenMove[1] < 3 && game.Grid[chosenMove[0], chosenMove[1]] == 0)
            {
                game.Grid[chosenMove[0], chosenMove[1]] = 2;
                await game.EndBotTurn();
            }
            else
            {
                await BotPlay(game);
            }
        }

        private static int[] FindWinningMove(int[,] grid, int turns, int player)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (grid[i, j] == 0)
                    {
                        grid[i, j] = player;

                        // Check if the move results in a win
                        if (GetWinnerOutcome(grid, turns) == player)
                        {
                            grid[i, j] = 0; // Reset the move
                            return new int[] { i, j };
                        }

                        grid[i, j] = 0; // Reset the move
                    }
                }
            }

            return null; // No winning move found
        }

        private static int[] Minimax(int[,] currentGrid, int turns, int player)
        {
            int[] bestMove = new [] { -1, -1 };
            int bestScore = (player == 1) ? int.MaxValue : int.MinValue;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (currentGrid[i, j] == 0)
                    {
                        currentGrid[i, j] = player;
                        int score = MinimaxScore(currentGrid, turns, 1, player == 1 ? 2 : 1);
                        currentGrid[i, j] = 0;

                        if ((player == 1 && score < bestScore) || (player == 2 && score > bestScore))
                        {
                            bestScore = score;
                            bestMove[0] = i;
                            bestMove[1] = j;
                        }
                    }
                }
            }

            // Additional check to ensure the selected move is within bounds
            if (bestMove[0] < 0 || bestMove[0] >= 3 || bestMove[1] < 0 || bestMove[1] >= 3)
            {
                return null; // Indicate that no valid move was found
            }

            return bestMove;
        }

        private static int MinimaxScore(int[,] grid, int turns, int depth, int player)
        {
            int winner = GetWinnerOutcome(grid, turns);
            if (winner == 1)
            {
                return -1;
            }

            if (winner == 2)
            {
                return 1;
            }

            if (player == 1)
            {
                int bestScore = int.MaxValue;

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[i, j] == 0)
                        {
                            grid[i, j] = player;
                            int score = MinimaxScore(grid, turns, depth + 1, player == 1 ? 2 : 1);
                            grid[i, j] = 0;

                            bestScore = Math.Min(bestScore, score);
                        }
                    }
                }

                return bestScore;
            }
            else
            {
                int bestScore = int.MinValue;

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[i, j] == 0)
                        {
                            grid[i, j] = player;
                            int score = MinimaxScore(grid, turns, depth + 1, player == 1 ? 2 : 1);
                            grid[i, j] = 0;

                            bestScore = Math.Max(bestScore, score);
                        }
                    }
                }

                return bestScore;
            }
        }

        private static int[] GetRandomValidMove(int[,] currentGrid)
        {
            Random random = new();

            while (true)
            {
                int row = random.Next(0, 3);
                int col = random.Next(0, 3);

                if (currentGrid[row, col] == 0)
                {
                    return new [] { row, col };
                }
            }
        }
    }
}

