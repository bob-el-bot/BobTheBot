using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.Interactions;
using static ApiInteractions.Interface;

namespace Commands.Helpers
{
    public class Question
    {
        public string question;
        public string category;
        public string difficulty;
        public string correctAnswer;
        public string[] answers = new string[4];
    }

    public static class TriviaMethods
    {
        // API management
        public const int MaxQuestionCount = 50;
        public const int SecondsPerRequest = 6;
        private static DateTime lastRequestTime = DateTime.MinValue;
        private static readonly object lockObject = new();

        public const int TotalQuestions = 5;

        private static readonly Random random = new();

        private static readonly Queue<Question> questions = new();

        public static async Task GetNewQuestions()
        {
            TimeSpan elapsed;

            lock (lockObject)
            {
                elapsed = DateTime.Now - lastRequestTime;

                // If less than 6 seconds have passed, release the lock and wait for the remaining time
                if (elapsed.TotalSeconds < SecondsPerRequest)
                {
                    Monitor.Exit(lockObject);
                    int remainingMilliseconds = (int)((SecondsPerRequest - elapsed.TotalSeconds) * 1000);

                    Task.Delay(remainingMilliseconds).Wait();

                    Monitor.Enter(lockObject);
                }
            }

            // Get Questions
            var content = await GetFromAPI($"https://opentdb.com/api.php?amount={MaxQuestionCount}&type=multiple&encode=base64", AcceptTypes.application_json);

            // Update the last request time after waiting or immediately if no wait was needed
            lastRequestTime = DateTime.Now;

            // Parse all questions and add to the list.
            var jsonData = JsonNode.Parse(content).AsObject();
            var results = jsonData["results"].AsArray();

            foreach (var result in results)
            {

                Question question = new()
                {
                    question = Encoding.UTF8.GetString(Convert.FromBase64String(result["question"].ToString())),
                    difficulty = Encoding.UTF8.GetString(Convert.FromBase64String(result["difficulty"].ToString())),
                    category = Encoding.UTF8.GetString(Convert.FromBase64String(result["category"].ToString())),
                };

                var incorrectAnswers = result["incorrect_answers"].AsArray().Select(e => Encoding.UTF8.GetString(Convert.FromBase64String(e.ToString()))).ToArray();

                int answerLocation = random.Next(0, 4);
                int incorrectAnswersIndex = 0;
                string[] letters = { "a", "b", "c", "d" };
                for (int i = 0; i < letters.Length; i++)
                {
                    if (i == answerLocation)
                    {
                        question.correctAnswer = letters[i];
                        question.answers[i] = Encoding.UTF8.GetString(Convert.FromBase64String(result["correct_answer"].ToString()));
                    }
                    else
                    {
                        question.answers[i] = incorrectAnswers[incorrectAnswersIndex];
                        incorrectAnswersIndex++;
                    }
                }

                // Add question to Queue
                questions.Enqueue(question);
            }
        }

        public static async Task<Question> GetQuestion()
        {
            if (questions.Count > 0)
            {
                return questions.Dequeue();
            }
            else
            {
                await GetNewQuestions();
                return questions.Dequeue();
            }
        }

        private static string FormatQuestionText(Question question)
        {
            StringBuilder finalText = new();

            finalText.AppendLine($"**{question.question}**\n");

            string[] letters = { "🇦", "🇧", "🇨", "🇩" };
            for (int i = 0; i < question.answers.Length; i++)
            {
                finalText.AppendLine($"{letters[i]} {question.answers[i]}\n");
            }

            return finalText.ToString();
        }

        public static EmbedBuilder CreateQuestionEmbed(Trivia game, string title, long expiration = 0)
        {
            StringBuilder description = new();
            description.AppendLine(title);

            if (game.questions.Count > 1)
            {
                description.AppendLine($"**{game.Player1.GlobalName}**: {game.player1Chart} {(!game.Player2.IsBot ? $"**{game.Player2.GlobalName}**: {game.player2Chart}" : "")}");
            }

            description.AppendLine($"Question: {game.questions.Count}/{TotalQuestions}\n");
            description.AppendLine(FormatQuestionText(game.questions.Last()));

            if (expiration != 0)
            {
                description.AppendLine($"(Forfeit <t:{expiration}:R>).");
            }

            var embed = new EmbedBuilder
            {
                Color = Challenge.BothPlayersColor,
                Description = description.ToString()
            };

            embed.AddField(name: "Category", value: game.questions.Last().category, inline: true).AddField(name: "Difficulty", value: game.questions.Last().difficulty, inline: true);

            embed.Footer = GetFooter();

            return embed;
        }

        public static EmbedBuilder CreateFinalEmbed(Trivia game, bool forfeited = false)
        {
            var embed = new EmbedBuilder
            {
                Color = Challenge.BothPlayersColor,
                Description = GetFinalTitle(game, forfeited)
            };

            // Show Player Charts
            embed.AddField(name: game.Player1.GlobalName, value: game.player1Chart, inline: true);

            if (!game.Player2.IsBot)
            {
                embed.AddField(name: game.Player2.GlobalName, value: game.player2Chart, inline: true);
            }

            // Show Questions with Correct Answers
            string letters = "abcd";
            foreach (Question q in game.questions)
            {
                embed.AddField(name: q.question, value: $"{q.answers[letters.IndexOf(q.correctAnswer)]}");
            }

            return embed;
        }

        private static string GetFinalTitle(Trivia game, bool forfeited = false)
        {
            if (game.Player2.IsBot)
            {
                return $"### ⚔️ {game.Player1.Mention}'s Completed Game of {game.Title}.";
            }
            else
            {
                // All ways for player1 to lose
                if (game.player1Points < game.player2Points || (forfeited && game.player1Answer == null && game.player1Points < game.player2Points))
                {
                    return $"### ⚔️ {game.Player1.Mention} Was Defeated By {game.Player2.Mention} in {game.Title}.";
                }
                else if ((forfeited && game.player1Points + game.player2Points == 0) || game.player1Points == game.player2Points) // draw
                {
                    return $"### ⚔️ {game.Player1.Mention} Drew {game.Player2.Mention} in {game.Title}.";
                }
                else // else player1 won
                {
                    return $"### ⚔️ {game.Player1.Mention} Defeated {game.Player2.Mention} in {game.Title}.";
                }
            }
        }

        public static EmbedFooterBuilder GetFooter()
        {
            var footer = new EmbedFooterBuilder
            {
                Text = "Powered by Open Trivia Database (unaffiliated)."
            };

            return footer;
        }

        public static ComponentBuilder GetButtons(ulong Id, bool disable = false)
        {
            var buttons = new ComponentBuilder();
            buttons.WithButton(emote: new Emoji("\U0001F1E6"), customId: $"trivia:a:{Id}", style: ButtonStyle.Secondary, disabled: disable);
            buttons.WithButton(emote: new Emoji("\U0001F1E7"), customId: $"trivia:b:{Id}", style: ButtonStyle.Secondary, disabled: disable);
            buttons.WithButton(emote: new Emoji("\U0001F1E8"), customId: $"trivia:c:{Id}", style: ButtonStyle.Secondary, disabled: disable);
            buttons.WithButton(emote: new Emoji("\U0001F1E9"), customId: $"trivia:d:{Id}", style: ButtonStyle.Secondary, disabled: disable);

            return buttons;
        }
    }
}