using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Interactions;
using Moderation;

namespace Commands.Helpers
{
    public static class MasterMindMethods
    {
        // List of currently active Master Mind games
        public static List<MasterMindGame> CurrentGames { get; set; } = new();

        // Random number generator for various game elements
        private static readonly Random random = new();

        // Colors used for the game embeds
        public static readonly Color DefaultColor = new(16415395); // Default embed color
        private static readonly Color WinColor = new(5763719);     // Embed color for winning
        private static readonly Color LoseColor = new(15548997);   // Embed color for losing

        /// <summary>
        /// Represents the color options available in Master Mind, with corresponding emoji display.
        /// </summary>
        public enum Colors
        {
            [ChoiceDisplay("🟥 Red")]
            Red,
            [ChoiceDisplay("🟧 Orange")]
            Orange,
            [ChoiceDisplay("🟨 Yellow")]
            Yellow,
            [ChoiceDisplay("🟩 Green")]
            Green,
            [ChoiceDisplay("🟦 Blue")]
            Blue,
            [ChoiceDisplay("🟪 Purple")]
            Purple
        }

        // Internal class for representing select menu options
        private class SelectOptionBuilder
        {
            public string Label { get; set; } // Option label
            public string Value { get; set; } // Option value
            public string Description { get; set; } // Description of the option
            public Emoji Emote { get; set; } // Associated emoji
        }

        // Predefined difficulty options for the game
        private static readonly List<SelectOptionBuilder> Difficulty = new()
        {
            new SelectOptionBuilder
            {
                Label = "Easy (10 attempts)",
                Value = "10",
                Description = "10 attempts",
                Emote = new Emoji("🌞")
            },
            new SelectOptionBuilder
            {
                Label = "Medium (8 attempts)",
                Value = "8",
                Description = "8 attempts",
                Emote = new Emoji("🤔")
            },
            new SelectOptionBuilder
            {
                Label = "Hard (6 attempts)",
                Value = "6",
                Description = "6 attempts",
                Emote = new Emoji("💀")
            }
        };

        /// <summary>
        /// Creates a select menu component for choosing the game's difficulty level.
        /// </summary>
        /// <returns>A message component containing the difficulty select menu.</returns>
        public static MessageComponent CreateDifficultySelectMenu()
        {
            var components = new ComponentBuilder();

            var selectMenu = new SelectMenuBuilder
            {
                MinValues = 1,
                MaxValues = 1,
                CustomId = "mastermind-difficulty",
                Placeholder = "Select Difficulty...",
            };

            foreach (var option in Difficulty)
            {
                selectMenu.Options.Add(new SelectMenuOptionBuilder
                {
                    Label = option.Label,
                    Value = option.Value,
                    Description = option.Description,
                    Emote = option.Emote
                });
            }

            return components.WithSelectMenu(selectMenu).Build();
        }

        /// <summary>
        /// Returns a random congratulatory message.
        /// </summary>
        /// <returns>A string containing a congratulatory message.</returns>
        public static string GetCongrats()
        {
            string[] congrats = { "*You* did it!", "*You* solved it!", "Great job! *you* beat it!" };
            return congrats[random.Next(0, congrats.Length)];
        }

        /// <summary>
        /// Generates a random secret code (key) for the Master Mind game.
        /// </summary>
        /// <returns>An array of <see cref="Colors"/> representing the secret key.</returns>
        public static Colors[] CreateKey()
        {
            Colors[] key = new Colors[4];

            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (Colors)random.Next(0, Enum.GetValues(typeof(Colors)).Length);
            }

            return key;
        }

        /// <summary>
        /// Converts an array of colors into a string representation using emojis.
        /// </summary>
        /// <param name="colors">The array of colors to convert.</param>
        /// <returns>A string representation of the colors.</returns>
        public static string GetColorsString(Colors[] colors)
        {
            StringBuilder colorString = new();

            foreach (var color in colors)
            {
                colorString.Append(GetColorString(color));
            }

            return colorString.ToString();
        }

        private static string GetColorString(Colors color)
        {
            return color switch
            {
                Colors.Red => "🟥",
                Colors.Orange => "🟧",
                Colors.Yellow => "🟨",
                Colors.Green => "🟩",
                Colors.Blue => "🟦",
                Colors.Purple => "🟪",
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }

        private static string GetDescriptionString(List<(string, Colors[])> guesses)
        {
            StringBuilder description = new();

            foreach (var guess in guesses)
            {
                description.Append($"{guess.Item1} {GetColorsString(guess.Item2)}\n");
            }

            return description.ToString();
        }

        /// <summary>
        /// Creates an embed reflecting the current state of a Master Mind game.
        /// </summary>
        /// <param name="game">The current game instance.</param>
        /// <param name="isSolved">Indicates whether the game has been solved.</param>
        /// <returns>An embed summarizing the game's state.</returns>
        public static Embed CreateEmbed(MasterMindGame game, bool isSolved = false)
        {
            EmbedBuilder defaultEmbed = new()
            {
                Title = "🧠 Master Mind",
                Color = isSolved ? WinColor : game.GuessesLeft == 0 ? LoseColor : DefaultColor,
                Footer = new() { Text = "⬛ = Color is in the correct position ⬜ = Color is in the wrong position 🔳 = Color is not in the code" }
            };

            defaultEmbed.AddField("Board:", GetDescriptionString(game.Guesses), true);
            defaultEmbed.AddField("Guesses Left:", $"`{game.GuessesLeft}`", true);

            if (isSolved)
            {
                defaultEmbed.Title += " (solved)";
                defaultEmbed.Description += GetCongrats();
            }
            else if (game.GuessesLeft <= 0)
            {
                defaultEmbed.Title += " (lost)";
                defaultEmbed.Description += "You have lost, but don't be sad you can just start a new game with `/master-mind new-game`";
                defaultEmbed.AddField("Answer:", $"{GetColorsString(game.Key)}");
            }

            return defaultEmbed.Build();
        }
    }
}
