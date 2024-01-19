using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
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

        public const int TotalQuestions = 4;

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

                int answerLocation = random.Next(0, 5);
                int incorrectAnswersIndex = 0;
                string[] letters = { "a", "b", "c", "d" };
                for (int i = 0; i < incorrectAnswers.Length + 1; i++)
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

        public static EmbedBuilder CreateQuestionEmbed(string title, Question question, int questionCount, long expiration = 0)
        {
            StringBuilder description = new();
            description.AppendLine(title);
            description.AppendLine($"Question: {questionCount + 1}/{TotalQuestions + 1}");
            description.Append(FormatQuestionText(question));

            if (expiration != 0)
            {
                description.AppendLine($"(Forfeit <t:{expiration}:R>).");
            }

            var embed = new EmbedBuilder
            {
                Color = Challenge.BothPlayersColor,
                Description = description.ToString()
            };

            embed.AddField(name: "Category", value: question.category, inline: true).AddField(name: "Difficulty", value: question.difficulty, inline: true);

            return embed;
        }

        public static EmbedBuilder CreateFinalEmbed(string title, int player1Points, int player2Points, bool singlePlayer = false)
        {
            var embed = new EmbedBuilder
            {
                Color = Challenge.BothPlayersColor,
                Description = title
            };

            embed.AddField(name: "Player 1", value: $"{player1Points}/{TotalQuestions + 1}", inline: true);

            if (!singlePlayer)
            {
                embed.AddField(name: "Player 2", value: $"{player2Points}/{TotalQuestions + 1}", inline: true);
            }

            return embed;
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