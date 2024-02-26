using System;
using System.Threading.Tasks;
using Challenges;
using Discord;

namespace Commands.Helpers
{
    public static class TTTMethods
    {

        public static bool DetermineFirstTurn()
        {
            Random random = new();
            if (random.Next(0, 2) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static ComponentBuilder GetButtons(int[,] grid, int turns, ulong Id, bool forfeited = false)
        {
            // Prepare Buttons
            var buttons = new ComponentBuilder();

            for (int y = 0; y <= 2; y++)
            {
                for (int x = 0; x <= 2; x++)
                {
                    if (grid[x, y] > 0)
                    {
                        buttons.WithButton(label: $"{(grid[x, y] == 1 ? "O" : "X")}", customId: $"ttt:{x}-{y}:{Id}", style: grid[x, y] == 1 ? ButtonStyle.Primary : ButtonStyle.Danger, row: y, disabled: true);
                    }
                    else
                    {
                        buttons.WithButton(label: "\U0000200E", customId: $"ttt:{x}-{y}:{Id}", style: ButtonStyle.Secondary, row: y, disabled: GetWinner(grid, turns) > 0 || turns == 9 || forfeited);
                    }
                }
            }

            return buttons;
        }

        public static EmbedBuilder CreateEmbed(bool isPlayer1Turn, string description)
        {
            return new EmbedBuilder
            {
                Color = isPlayer1Turn ? Challenge.Player1Color : Challenge.Player2Color,
                Description = description
            };
        }

        public static int GetWinner(int[,] grid, int turns)
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

        // BOT
        public static async Task BotPlay(TicTacToe game)
        {
            int[] winningMove = FindWinningMove(game.grid, game.turns, 2);
            int[] blockingMove = FindWinningMove(game.grid, game.turns, 1);

            int[] chosenMove = winningMove ?? blockingMove ?? Minimax(game.grid, game.turns, 2) ?? GetRandomValidMove(game.grid);

            // Check if the chosen move is valid and within bounds
            if (chosenMove[0] >= 0 && chosenMove[0] < 3 && chosenMove[1] >= 0 && chosenMove[1] < 3 && game.grid[chosenMove[0], chosenMove[1]] == 0)
            {
                game.grid[chosenMove[0], chosenMove[1]] = 2;
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
                        if (GetWinner(grid, turns) == player)
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
            int[] bestMove = new int[] { -1, -1 };
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
            int winner = GetWinner(grid, turns);
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
                    return new int[] { row, col };
                }
            }
        }
    }
}