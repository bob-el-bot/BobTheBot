using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Challenges;
using Discord;
using Time.Timestamps;

namespace Commands.Helpers
{
    public static class WordleMethods
    {
        private static readonly HashSet<string> validWords = new(File.ReadLines("./commands/wordle-group/helpers/wordlist.txt"));
        private static readonly HashSet<string> possibleAnswers = new(File.ReadLines("./commands/wordle-group/helpers/answerlist.txt"));

        private static readonly Random random = new();

        public const int GuessCount = 5;

        /// <summary>
        /// Checks if a guess is valid.
        /// </summary>
        public static bool IsValidGuess(string word)
        {
            if (word.Length != 5)
            {
                return false;
            }

            return validWords.Contains(word);
        }

        /// <summary>
        /// Returns the result of a guess.
        /// </summary>
        /// <param name="word">The word to guess.</param>
        /// <param name="guess">The guess.</param>
        /// <returns>A string containing the result of the guess.</returns>
        public static string GetResult(string word, string guess)
        {
            StringBuilder result = new();

            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == guess[i])
                {
                    result.Append("🟩");
                }
                else if (word.Contains(guess[i].ToString()))
                {
                    result.Append("🟧");
                }
                else
                {
                    result.Append('⬛');
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns a random word from the list of possible answers.
        /// </summary>
        public static string GetRandomWord()
        {
            return possibleAnswers.ElementAt(random.Next(possibleAnswers.Count));
        }

        /// <summary>
        /// Creates an embed for a Wordle game.
        /// </summary>
        /// <param name="game">The Wordle game.</param>
        /// <param name="isSolved">Whether the game is solved. Defaults to false</param>
        /// <returns>An embed for the Wordle game.</returns>
        public static Embed CreateEmbed(Wordle game, bool isSolved = false)
        {
            EmbedBuilder defaultEmbed = new()
            {
                Description = $"### 💡 {game.Player1.Mention}'s Game of {game.Title}\n(Ends in {Timestamp.FromDateTime(game.ExpirationTime, Timestamp.Formats.Relative)})",
                Color = isSolved ? Challenge.SinglePlayerWinColor : game.GuessesLeft == 0 ? Challenge.SinglePlayerLoseColor : Challenge.BothPlayersColor,
            };

            defaultEmbed.AddField("Guesses:", GetDescriptionString(game.Guesses), true);
            defaultEmbed.AddField("Guesses Left:", $"`{game.GuessesLeft}`", true);

            // if (isSolved)
            // {
            //     defaultEmbed.Description += $" (solved)\n{GetCongrats()}";
            // }
            // else if (game.GuessesLeft <= 0)
            // {
            //     defaultEmbed.Description += " (lost)\nYou have lost, but don't be sad you can just start a new game with `/wordle new-game`";
            //     defaultEmbed.AddField("Answer:", $"{GetFormattedGuess(game.Word)}");
            // }

            return defaultEmbed.Build();
        }

        /// <summary>
        /// Returns a string containing the description of the guesses made in a game.
        /// </summary>
        /// <param name="guesses">The list of guesses made.</param>
        /// <returns>A string containing the description of the guesses made.</returns>
        private static string GetDescriptionString(List<(string, string)> guesses)
        {
            if (guesses == null || guesses.Count == 0)
            {
                return "No guesses made yet, use `/wordle guess`.";
            }

            StringBuilder description = new();

            foreach (var guess in guesses)
            {
                description.Append($"{guess.Item1} {GetFormattedGuess(guess.Item2)}\n");
            }

            return description.ToString();
        }

        private static string GetFormattedGuess(string guess)
        {
            StringBuilder formattedGuess = new();

            foreach (char letter in guess)
            {
                formattedGuess.Append($"`{letter}` ");
            }

            return formattedGuess.ToString();
        }

        public static string CreateFinalTitle(Wordle game)
        {
            StringBuilder stringBuilder = new();

            if (Challenge.WordleGames.ContainsKey(game.Id) == false)
            {
                stringBuilder.AppendLine($"### 💡 {game.Player1.Mention}'s Wordle Expired!");
                stringBuilder.AppendLine($"**Answer:** {GetFormattedGuess(game.Word)}");
            }
            else if (game.GuessesLeft > 0)
            {
                stringBuilder.AppendLine($"### 💡 {game.Player1.Mention} Guessed the Wordle in: {GuessCount - game.GuessesLeft}!");
                stringBuilder.AppendLine(GetCongrats());
            }
            else
            {
                stringBuilder.AppendLine($"### 💡 {game.Player1.Mention} lost {game.Title}!");
                stringBuilder.AppendLine("You have lost, but don't be sad you can just start a new game with `/wordle new-game`");
                stringBuilder.AppendLine($"**Answer:** {GetFormattedGuess(game.Word)}");
            }

            stringBuilder.AppendLine(GetDescriptionString(game.Guesses));

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns a random congratulatory message.
        /// </summary>
        /// <returns>A string containing a congratulatory message.</returns>
        public static string GetCongrats()
        {
            string[] congrats = { "*You* did it!", "*You* solved it!", "Great job! *you* got it!" };
            return congrats[random.Next(0, congrats.Length)];
        }
    }
}