using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Interactions;
using Bob.Moderation;

namespace Bob.Commands.Helpers
{
    public static class MasterMindMethods
    {
        // List of currently active Master Mind games
        public static List<MasterMindGame> CurrentGames { get; set; } = [];

        // Random number generator for various game elements
        private static readonly Random random = new();

        // Colors used for the game embeds
        public static readonly Discord.Color DefaultColor = new(16415395); // Default embed color
        private static readonly Discord.Color WinColor = new(5763719);     // Embed color for winning
        private static readonly Discord.Color LoseColor = new(15548997);   // Embed color for losing

        /// <summary>
        /// Represents the color options available in Master Mind, with corresponding emoji display.
        /// </summary>
        public enum Color
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

        public enum GameMode
        {
            [ChoiceDisplay("Classic | Positional feedback (⬛⬜🟫⬜)")]
            Classic,
            [ChoiceDisplay("Numeric | Aggregate feedback (Correct: 1, Misplaced: 2, Incorrect: 1)")]
            Numeric
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
        private static readonly List<SelectOptionBuilder> Difficulty =
        [
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
        ];

        /// <summary>
        /// Creates a select menu for choosing the game difficulty.
        /// </summary>
        /// <param name="mode">The game mode to use.</param>
        /// <returns>A select menu for choosing the game difficulty.</returns>
        public static string GetRules(GameMode mode)
        {
            StringBuilder rules = new();

            rules.Append("The goal of the game is to guess the correct randomly generated code. Each code consists of 4 colors, chosen from 6 possible colors (duplicates are allowed). Use the command `/mastermind guess` to make your guess. After each guess you will be given feedback on how close you are to the correct code. The feedback is as follows:\n");

            switch (mode)
            {
                case GameMode.Classic: 
                    rules.Append(@"- ⬛ = Color is in the correct position.
- ⬜ = Color is in the wrong position.
- 🟫 = Color is not in the code.");
                    break;
                case GameMode.Numeric:
                    rules.Append(@"- ⬛ = How many of colors are correct and in the correct position.
- ⬜ = How many colors are correct, but in the wrong position.
- 🟫 = How many colors are not in the code.");
                    break;
            }

            rules.Append(@"
You can pick a difficulty level:

- Easy: 10 tries.
- Medium: 8 tries.
- Hard: 6 tries.

Good luck cracking the code!");

            return rules.ToString();
        }

        /// <summary>
        /// Creates a select menu component for choosing the game's difficulty level.
        /// </summary>
        /// <param name="disabled">Indicates whether the select menu should be disabled.</param>
        /// <returns>A message component containing the difficulty select menu.</returns>
        public static MessageComponent CreateDifficultySelectMenu(bool disabled = false)
        {
            var components = new ComponentBuilder();

            var selectMenu = new SelectMenuBuilder
            {
                MinValues = 1,
                MaxValues = 1,
                CustomId = "mastermind-difficulty",
                Placeholder = "Select Difficulty...",
                IsDisabled = disabled
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
            string[] congrats = ["*You* did it!", "*You* solved it!", "Great job! *you* beat it!"];
            return congrats[random.Next(0, congrats.Length)];
        }

        /// <summary>
        /// Generates a random secret code (key) for the Master Mind game.
        /// </summary>
        /// <returns>An array of <see cref="Color"/> representing the secret key.</returns>
        public static Color[] CreateKey()
        {
            Color[] key = new Color[4];

            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (Color)random.Next(0, Enum.GetValues<Color>().Length);
            }

            return key;
        }

        /// <summary>
        /// Creates the forfeit button component and builds it for the Mastermind game.
        /// </summary>
        /// <param name="disabled">Indicates whether the button should be disabled.</param>
        /// <returns>A <see cref="MessageComponent"/> containing the button.</returns>
        public static MessageComponent GetForfeitButton(bool disabled = false)
        {
            var button = new ButtonBuilder
            {
                Label = "Forfeit",
                Style = ButtonStyle.Danger,
                CustomId = "quit",
                IsDisabled = disabled
            };

            return new ComponentBuilder().WithButton(button).Build();
        }

        /// <summary>
        /// Converts an array of colors into a string representation using emojis.
        /// </summary>
        /// <param name="colors">The array of colors to convert.</param>
        /// <returns>A string representation of the colors.</returns>
        public static string GetColorsString(Color[] colors)
        {
            StringBuilder colorString = new();

            foreach (var color in colors)
            {
                colorString.Append(GetColorString(color));
            }

            return colorString.ToString();
        }

        private static string GetColorString(Color color)
        {
            return color switch
            {
                Color.Red => "🟥",
                Color.Orange => "🟧",
                Color.Yellow => "🟨",
                Color.Green => "🟩",
                Color.Blue => "🟦",
                Color.Purple => "🟪",
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }

        /// <summary>
        /// Retrieves a game instance by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the game.</param>
        /// <returns>The game instance associated with the identifier.</returns>
        public static MasterMindGame GetGame(ulong id)
        {
            return CurrentGames.FirstOrDefault(game => game.Id == id);
        }

        private static string GetDescriptionString(List<(string, Color[])> guesses)
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
                Footer = new() { Text = GetFooterString(game.Mode) }
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

        private static string GetFooterString(GameMode mode)
        {
            return mode switch
            {
                GameMode.Classic => "⬛ = Color is in the correct position ⬜ = Color is in the wrong position 🟫 = Color is not in the code",
                GameMode.Numeric => "⬛ = How many of colors are correct and in the correct position ⬜ = How many colors are correct, but in the wrong position 🟫 = How many colors are not in the code",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
}
