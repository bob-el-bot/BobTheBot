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
    public class MasterMindGame
    {
        /// <summary>
        /// The secret key (code) that players attempt to guess.
        /// </summary>
        public Colors[] Key { get; set; }

        /// <summary>
        /// A collection of previous guesses and their associated results.
        /// </summary>
        public List<(string Result, Colors[] Guess)> Guesses { get; set; }

        /// <summary>
        /// A unique identifier for the game instance.
        /// </summary>
        public ulong Id { get; set; }

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
        public IUser StartUser { get; set; }

        /// <summary>
        /// The Discord message used for game interaction.
        /// </summary>
        public SocketUserMessage Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterMindGame"/> class.
        /// </summary>
        /// <param name="Id">The unique identifier for the game.</param>
        /// <param name="StartUser">The user who started the game.</param>
        public MasterMindGame(ulong Id, IUser StartUser)
        {
            this.Id = Id;
            this.StartUser = StartUser;
            Guesses = new();
        }

        /// <summary>
        /// Generates a string representation of the result for a player's guess.
        /// </summary>
        /// <param name="guess">The player's guess.</param>
        /// <returns>A string indicating the accuracy of the guess.</returns>
        public string GetResultString(Colors[] guess)
        {
            var result = guess.Select((g, i) =>
                g == Key[i] ? "⬛" : // Correct color in the correct position.
                Key.Contains(g) ? "⬜" : // Correct color in the wrong position.
                "🔳" // Incorrect color.
            );

            return string.Concat(result);
        }

        /// <summary>
        /// Compares the player's guess with the key to determine if they match exactly.
        /// </summary>
        /// <param name="guess">The player's guess.</param>
        /// <returns>True if the guess matches the key; otherwise, false.</returns>
        public bool DoesGuessMatchKey(Colors[] guess)
        {
            return Enumerable.SequenceEqual(Key, guess);
        }
    }
}
