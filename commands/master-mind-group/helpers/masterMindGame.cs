using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using static Commands.Helpers.MasterMindMethods;

namespace Commands.Helpers
{
    /// <summary>
    /// Represents a single game of Master Mind, containing game logic and state.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MasterMindGame"/> class.
    /// </remarks>
    /// <param name="Id">The unique identifier for the game.</param>
    /// <param name="StartUser">The user who started the game.</param>
    /// <param name="mode">The game mode selected by the player.</param>
    public class MasterMindGame(ulong Id, IUser StartUser, MasterMindMethods.GameMode mode)
    {
        /// <summary>
        /// The secret key (code) that players attempt to guess.
        /// </summary>
        public MasterMindMethods.Color[] Key { get; set; }

        /// <summary>
        /// A collection of previous guesses and their associated results.
        /// </summary>
        public List<(string Result, MasterMindMethods.Color[] Guess)> Guesses { get; set; } = [];

        /// <summary>
        /// A unique identifier for the game instance.
        /// </summary>
        public ulong Id { get; set; } = Id;

        /// <summary>
        /// Indicates whether the game has started.
        /// </summary>
        public bool IsStarted { get; set; }

        /// <summary>
        /// The number of guesses the player has remaining.
        /// </summary>
        public int GuessesLeft { get; set; } = 8;

        /// <summary>
        /// The user who initiated the game.
        /// </summary>
        public IUser StartUser { get; set; } = StartUser;

        /// <summary>
        /// The Discord message used for game interaction.
        /// </summary>
        public SocketUserMessage Message { get; set; }

        /// <summary>
        /// The game mode selected by the player.
        /// </summary>
        public GameMode Mode { get; set; } = mode;

        /// <summary>
        /// Generates a string representation of the result for a player's guess.
        /// </summary>
        /// <param name="guess">The player's guess.</param>
        /// <returns>A string indicating the accuracy of the guess.</returns>
        public string GetResultString(MasterMindMethods.Color[] guess)
        {
            string result = "";

            switch (Mode)
            {
                case GameMode.Classic:
                    result = string.Concat(guess.Select((g, i) =>
                        g == Key[i] ? "⬛" : // Correct color in the correct position.
                        Key.Contains(g) ? "⬜" : // Correct color in the wrong position.
                        "🟫" // Incorrect color.
                    ));
                    break;

                case GameMode.Numeric:
                    // Step 1: Initialize counters
                    int correctPositions = 0;
                    int misplacedColors = 0;

                    // Step 2: Create arrays to track processed indices for the key and guess
                    bool[] keyProcessed = new bool[Key.Length];
                    bool[] guessProcessed = new bool[guess.Length];

                    // Step 3: First pass to count exact matches (⬛)
                    for (int i = 0; i < guess.Length; i++)
                    {
                        if (guess[i] == Key[i])
                        {
                            correctPositions++;
                            keyProcessed[i] = true; // Mark as processed in the key
                            guessProcessed[i] = true; // Mark as processed in the guess
                        }
                    }

                    // Step 4: Second pass to count misplaced colors (⬜)
                    for (int i = 0; i < guess.Length; i++)
                    {
                        if (!guessProcessed[i]) // Only consider unprocessed guesses
                        {
                            for (int j = 0; j < Key.Length; j++)
                            {
                                if (!keyProcessed[j] && guess[i] == Key[j])
                                {
                                    misplacedColors++;
                                    keyProcessed[j] = true; // Mark as processed in the key
                                    break;
                                }
                            }
                        }
                    }

                    // Step 5: Calculate incorrect colors (🟫)
                    int incorrectColors = guess.Length - correctPositions - misplacedColors;

                    // Step 6: Format the result string
                    result = $"⬛ `{correctPositions}` ⬜ `{misplacedColors}` 🟫 `{incorrectColors}`";
                    break;
            }

            return result;
        }

        /// <summary>
        /// Compares the player's guess with the key to determine if they match exactly.
        /// </summary>
        /// <param name="guess">The player's guess.</param>
        /// <returns>True if the guess matches the key; otherwise, false.</returns>
        public bool DoesGuessMatchKey(MasterMindMethods.Color[] guess)
        {
            return Enumerable.SequenceEqual(Key, guess);
        }
    }
}
