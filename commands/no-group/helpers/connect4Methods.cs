using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Challenges;
using Discord;

namespace Commands.Helpers
{
    /// <summary>
    /// Provides helper methods for implementing Connect 4 game logic.
    /// </summary>
    public static class Connect4Methods
    {
        /// <summary>
        /// Generates buttons for the Connect4 game interface.
        /// </summary>
        /// <param name="game">The Connect4 game instance.</param>
        /// <param name="forfeited">Flag indicating if the game was forfeited.</param>
        /// <returns>ComponentBuilder containing the buttons.</returns>
        public static ComponentBuilder GetButtons(Connect4 game, bool forfeited = false)
        {
            // Prepare Buttons
            var buttons = new ComponentBuilder();

            bool gameOver = GetWinnerOutcome(game.Grid, game.Turns, game.LastMoveColumn, game.LastMoveRow) > 0 || forfeited;

            // Top Row
            buttons.WithButton(label: "2", customId: $"connect4:2:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[1, 0] != 0));
            buttons.WithButton(label: "3", customId: $"connect4:3:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[2, 0] != 0));
            buttons.WithButton(label: "4", customId: $"connect4:4:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[3, 0] != 0));
            buttons.WithButton(label: "5", customId: $"connect4:5:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[4, 0] != 0));
            buttons.WithButton(label: "6", customId: $"connect4:6:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[5, 0] != 0));

            // Bottom Row
            buttons.WithButton(label: "1", customId: $"connect4:1:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[0, 0] != 0), row: 1);
            buttons.WithButton(label: "\U0000200E", customId: $"connect4:disabled1:{game.Id}", style: ButtonStyle.Secondary, disabled: true, row: 1);
            buttons.WithButton(label: "\U0000200E", customId: $"connect4:disabled2:{game.Id}", style: ButtonStyle.Secondary, disabled: true, row: 1);
            buttons.WithButton(label: "\U0000200E", customId: $"connect4:disabled3:{game.Id}", style: ButtonStyle.Secondary, disabled: true, row: 1);
            buttons.WithButton(label: "7", customId: $"connect4:7:{game.Id}", style: ButtonStyle.Secondary, disabled: gameOver || (game.Grid[6, 0] != 0), row: 1);

            return buttons;
        }

        /// <summary>
        /// Generates a string representation of the Connect4 game grid.
        /// </summary>
        /// <param name="grid">The Connect4 game grid.</param>
        /// <returns>String representation of the grid.</returns>
        public static string GetGrid(int[,] grid)
        {
            StringBuilder result = new();

            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 7; column++)
                {
                    if (grid[column, row] == 1)
                    {
                        result.Append("🔵");
                    }
                    else if (grid[column, row] == 2)
                    {
                        result.Append("🔴");
                    }
                    else
                    {
                        result.Append('⚫');
                    }
                }

                result.AppendLine();
            }

            result.AppendLine("1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣");

            return result.ToString();
        }

        /// <summary>
        /// Determines the winner of the Connect4 game.
        /// </summary>
        /// <param name="game">The Connect4 game instance.</param>
        /// <param name="forfeited">Flag indicating if the game was forfeited.</param>
        /// <returns>The winner of the game.</returns>
        public static Challenge.WinCases GetWinner(Connect4 game, bool forfeited = false)
        {
            int winner = GetWinnerOutcome(game.Grid, game.Turns, game.LastMoveColumn, game.LastMoveRow);

            // All ways for player1 to lose
            if (winner == 2 || (forfeited && game.IsPlayer1Turn))
            {
                return Challenge.WinCases.Player2;
            }
            else if (winner == -1 && game.Turns == 42 || (forfeited && game.Turns == 0)) // draw
            {
                return Challenge.WinCases.Tie;
            }
            else // else player1 won
            {
                return Challenge.WinCases.Player1;
            }
        }

        /// <summary>
        /// Evaluates the outcome of the Connect4 game.
        /// </summary>
        /// <param name="grid">The Connect4 game grid.</param>
        /// <param name="turns">The number of turns in the game.</param>
        /// <param name="lastMoveColumn">The column of the last move.</param>
        /// <param name="lastMoveRow">The row of the last move.</param>
        /// <returns>The outcome of the game (0 for no winner, 1 for player 1, 2 for player 2).</returns>
        public static int GetWinnerOutcome(int[,] grid, int turns, int lastMoveColumn, int lastMoveRow)
        {
            if (turns < 7) // Minimum turns needed for a win in Connect4 is 7
            {
                return 0;
            }

            int player = grid[lastMoveColumn, lastMoveRow];
            if (player == 0)
            {
                return 0;
            }

            // Directions: horizontal, vertical, diagonal (bottom-left to top-right), diagonal (top-left to bottom-right)
            int[][] directions =
            [
                [1, 0],   // Horizontal
                [0, 1],   // Vertical
                [1, 1],   // Diagonal (bottom-left to top-right)
                [1, -1]   // Diagonal (top-left to bottom-right)
            ];

            foreach (var dir in directions)
            {
                int count = 1;
                for (int i = -1; i <= 1; i += 2) // Check both directions for each axis
                {
                    int colDir = dir[0] * i;
                    int rowDir = dir[1] * i;

                    for (int j = 1; j < 4; j++)
                    {
                        int col = lastMoveColumn + j * colDir;
                        int row = lastMoveRow + j * rowDir;

                        if (col >= 0 && col < grid.GetLength(0) && row >= 0 && row < grid.GetLength(1) && grid[col, row] == player)
                        {
                            count++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (count >= 4)
                {
                    return player;
                }
            }

            return 0;
        }

        /// <summary>
        /// Performs a bot move in the Connect4 game.
        /// </summary>
        /// <param name="game">The Connect4 game instance.</param>
        /// <returns>Task representing the asynchronous bot move.</returns>
        public static async Task BotPlay(Connect4 game)
        {
            try
            {
                // First move: Place token in a random column
                if (game.Turns == 0 || game.Turns == 1)
                {
                    int[] randomMove = GetRandomValidMove(game.Grid);
                    PlaceToken(game.Grid, randomMove[0], 2);
                    game.LastMoveColumn = randomMove[0];
                    game.LastMoveRow = randomMove[1];
                    await game.EndBotTurn();
                    return;
                }

                var chosenMove = Minimax(game.Grid, game.Turns, 4, true, int.MinValue, int.MaxValue, game.LastMoveColumn, game.LastMoveRow).Move;

                if (chosenMove != null && IsValidMove(game.Grid, chosenMove[0]))
                {
                    PlaceToken(game.Grid, chosenMove[0], 2); // Bot is player 2
                    game.LastMoveColumn = chosenMove[0];
                    game.LastMoveRow = chosenMove[1];
                    await game.EndBotTurn();
                }
                else
                {
                    // Fallback to a random move if Minimax fails
                    int[] fallbackMove = GetRandomValidMove(game.Grid);
                    PlaceToken(game.Grid, fallbackMove[0], 2);
                    game.LastMoveColumn = fallbackMove[0];
                    game.LastMoveRow = fallbackMove[1];
                    await game.EndBotTurn();
                }
            }
            catch (Exception ex)
            {
                // Log the exception and make a random move as a fallback
                Console.WriteLine($"BotPlay encountered an error: {ex.Message}");
                int[] fallbackMove = GetRandomValidMove(game.Grid);
                PlaceToken(game.Grid, fallbackMove[0], 2);
                game.LastMoveColumn = fallbackMove[0];
                game.LastMoveRow = fallbackMove[1];
                await game.EndBotTurn();
            }
        }

        // AI Methods
        // Minimax algorithm with alpha-beta pruning
        private static readonly Random random = new();

        private static (int Score, int[] Move) Minimax(int[,] grid, int turns, int depth, bool isMaximizing, int alpha, int beta, int lastMoveColumn, int lastMoveRow)
        {
            int winner = GetWinnerOutcome(grid, turns, lastMoveColumn, lastMoveRow);
            if (winner == 2)
            {
                return (1000 - depth, null); // Favor winning faster
            }
            if (winner == 1)
            {
                return (-1000 + depth, null); // Favor opponent winning slower
            }
            if (turns == grid.GetLength(0) * grid.GetLength(1))
            {
                return (0, null); // Draw
            }

            if (depth == 0)
            {
                return (EvaluateGrid(grid, lastMoveColumn, lastMoveRow), null); // Evaluate the current grid state
            }

            int bestScore = isMaximizing ? int.MinValue : int.MaxValue;
            int[] bestMove = null;

            for (int col = 0; col < grid.GetLength(0); col++)
            {
                if (IsValidMove(grid, col))
                {
                    int row = GetNextAvailableRow(grid, col);
                    grid[col, row] = isMaximizing ? 2 : 1;
                    turns++;
                    var (score, _) = Minimax(grid, turns, depth - 1, !isMaximizing, alpha, beta, col, row);
                    grid[col, row] = 0;
                    turns--;

                    if (isMaximizing)
                    {
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMove = [col, row];
                        }
                        alpha = Math.Max(alpha, score);
                    }
                    else
                    {
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestMove = [col, row];
                        }
                        beta = Math.Min(beta, score);
                    }

                    if (beta <= alpha)
                    {
                        break;
                    }
                }
            }

            return (bestScore, bestMove);
        }

        private static int EvaluateGrid(int[,] grid, int lastMoveColumn, int lastMoveRow)
        {
            int score = 0;
            int player = grid[lastMoveColumn, lastMoveRow]; // The current player
            int opponent = player == 1 ? 2 : 1;            // The opponent player

            // Define directions to check: horizontal, vertical, diagonal (both directions)
            int[][] directions =
            [
                [1, 0], // Horizontal
                [0, 1], // Vertical
                [1, 1], // Diagonal (bottom-left to top-right)
                [1, -1] // Diagonal (top-left to bottom-right)
            ];

            foreach (var dir in directions)
            {
                // Evaluate for the bot's tokens
                score += EvaluateDirection(grid, lastMoveColumn, lastMoveRow, dir[0], dir[1], player);

                // Evaluate for the opponent's tokens (negative impact)
                score -= EvaluateDirection(grid, lastMoveColumn, lastMoveRow, dir[0], dir[1], opponent);
            }

            return score;
        }

        private static int EvaluateDirection(int[,] grid, int startCol, int startRow, int colDir, int rowDir, int player)
        {
            int score = 0;

            // Check sequences starting from the last move
            int count = 1; // Include the starting token
            int emptySpaces = 0;

            // Forward direction
            int col = startCol + colDir;
            int row = startRow + rowDir;

            while (col >= 0 && col < grid.GetLength(0) && row >= 0 && row < grid.GetLength(1))
            {
                if (grid[col, row] == player)
                {
                    count++;
                }
                else if (grid[col, row] == 0) // Empty space
                {
                    emptySpaces++;
                    break; // Stop after encountering an empty space
                }
                else // Opponent's token
                {
                    break;
                }

                col += colDir;
                row += rowDir;
            }

            // Backward direction
            col = startCol - colDir;
            row = startRow - rowDir;

            while (col >= 0 && col < grid.GetLength(0) && row >= 0 && row < grid.GetLength(1))
            {
                if (grid[col, row] == player)
                {
                    count++;
                }
                else if (grid[col, row] == 0) // Empty space
                {
                    emptySpaces++;
                    break; // Stop after encountering an empty space
                }
                else // Opponent's token
                {
                    break;
                }

                col -= colDir;
                row -= rowDir;
            }

            // Score weighting: Favor sequences that can lead to Connect 4
            if (count >= 4)
            {
                score += 1000; // Winning move
            }
            else if (count == 3 && emptySpaces > 0)
            {
                score += 100; // Strong position
            }
            else if (count == 2 && emptySpaces > 0)
            {
                score += 10; // Decent position
            }

            return score;
        }

        private static bool IsValidMove(int[,] grid, int col)
        {
            return grid[col, 0] == 0; // Check if the top row of the column is empty
        }

        private static int GetNextAvailableRow(int[,] grid, int col)
        {
            for (int row = grid.GetLength(1) - 1; row >= 0; row--)
            {
                if (grid[col, row] == 0)
                {
                    return row;
                }
            }
            return -1; // Should not happen if checked with IsValidMove first
        }

        private static void PlaceToken(int[,] grid, int col, int player)
        {
            int row = GetNextAvailableRow(grid, col);
            if (row != -1)
            {
                grid[col, row] = player;
            }
        }

        private static int[] GetRandomValidMove(int[,] grid)
        {
            int columns = grid.GetLength(0);
            List<int> validCols = [];

            for (int col = 0; col < columns; col++)
            {
                if (IsValidMove(grid, col))
                {
                    validCols.Add(col);
                }
            }

            int selectedCol = validCols[random.Next(validCols.Count)];
            return [selectedCol, GetNextAvailableRow(grid, selectedCol)];
        }
    }
}