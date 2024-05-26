using System;
using System.Text;
using System.Threading.Tasks;
using Challenges;
using Discord;

namespace Commands.Helpers
{
    public static class Connect4Methods
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

        public static ComponentBuilder GetButtons(int[,] grid, ulong Id, bool forfeited = false)
        {
            // Prepare Buttons
            var buttons = new ComponentBuilder();

            // Top Row
            buttons.WithButton(label: "2", customId: $"connect4:2:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[1, 0] != 0));
            buttons.WithButton(label: "3", customId: $"connect4:3:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[2, 0] != 0));
            buttons.WithButton(label: "4", customId: $"connect4:4:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[3, 0] != 0));
            buttons.WithButton(label: "5", customId: $"connect4:5:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[4, 0] != 0));
            buttons.WithButton(label: "6", customId: $"connect4:6:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[5, 0] != 0));

            // Bottom Row
            buttons.WithButton(label: "1", customId: $"connect4:1:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[0, 0] != 0), row: 1);
            buttons.WithButton(label: "\U0000200E", customId: $"connect4:disabled1:{Id}", style: ButtonStyle.Secondary, disabled: true, row: 1);
            buttons.WithButton(label: "\U0000200E", customId: $"connect4:disabled2:{Id}", style: ButtonStyle.Secondary, disabled: true, row: 1);
            buttons.WithButton(label: "\U0000200E", customId: $"connect4:disabled3:{Id}", style: ButtonStyle.Secondary, disabled: true, row: 1);
            buttons.WithButton(label: "7", customId: $"connect4:7:{Id}", style: ButtonStyle.Secondary, disabled: forfeited || (grid[6, 0] != 0), row: 1);

            return buttons;
        }

        public static Embed CreateEmbed(bool isPlayer1Turn, string description)
        {
            return new EmbedBuilder
            {
                Color = isPlayer1Turn ? Challenge.Player1Color : Challenge.Player2Color,
                Description = description
            }.Build();
        }

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
                        result.Append('⚪');
                    }
                }

                result.AppendLine();
            }

            result.AppendLine("1️⃣2️⃣3️⃣4️⃣5️⃣6️⃣7️⃣");

            return result.ToString();
        }

        public static int GetWinner(int[,] grid, int turns, int lastMoveCol, int lastMoveRow)
        {
            // Only perform checks if the number of turns is greater than or equal to 7
            if (turns < 7)
            {
                return 0;
            }

            int rows = grid.GetLength(1);
            int columns = grid.GetLength(0);
            int player = grid[lastMoveCol, lastMoveRow];

            // Direction vectors for horizontal, vertical, and two diagonals
            int[][] directions = new int[][]
            {
                new int[] { 1, 0 },   // Horizontal
                new int[] { 0, 1 },   // Vertical
                new int[] { 1, 1 },   // Diagonal (bottom-left to top-right)
                new int[] { 1, -1 }   // Diagonal (top-left to bottom-right)
            };

            // Check each direction
            foreach (var dir in directions)
            {
                int count = 1;

                // Check in the positive direction
                count += CountConsecutiveTokens(grid, lastMoveCol, lastMoveRow, dir[0], dir[1], player);

                // Check in the negative direction
                count += CountConsecutiveTokens(grid, lastMoveCol, lastMoveRow, -dir[0], -dir[1], player);

                // If 4 or more in a row are found, return the player as the winner
                if (count >= 4)
                {
                    return player;
                }
            }

            // No winner
            return 0;
        }

        // Helper method to count consecutive tokens in a given direction
        private static int CountConsecutiveTokens(int[,] grid, int startCol, int startRow, int colDir, int rowDir, int player)
        {
            int count = 0;
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            int col = startCol + colDir;
            int row = startRow + rowDir;

            while (col >= 0 && col < columns && row >= 0 && row < rows && grid[col, row] == player)
            {
                count++;
                col += colDir;
                row += rowDir;
            }

            return count;
        }
    }
}