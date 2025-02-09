using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Bob.Challenges;
using Discord;
using static Bob.ApiInteractions.Interface;
using Bob.Time.Timestamps;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// Represents a trivia question with its details and answers.
    /// </summary>
    public class Question
    {
        /// <summary>
        /// Gets or sets the text of the question.
        /// </summary>
        public string QuestionText { get; set; }

        /// <summary>
        /// Gets or sets the category of the question.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the difficulty level of the question.
        /// </summary>
        public string Difficulty { get; set; }

        /// <summary>
        /// Gets or sets the correct answer to the question.
        /// </summary>
        public string CorrectAnswer { get; set; }

        /// <summary>
        /// Gets or sets the array of possible answers to the question.
        /// </summary>
        public string[] Answers { get; set; } = new string[4];
    }

    /// <summary>
    /// Provides methods for managing and displaying trivia questions.
    /// </summary>
    public static class TriviaMethods
    {
        /// <summary>
        /// The maximum number of questions to fetch per API request.
        /// </summary>
        public const int MaxQuestionCount = 50;

        /// <summary>
        /// The minimum interval between API requests in seconds.
        /// </summary>
        public const int SecondsPerRequest = 6;

        /// <summary>
        /// The total number of questions for a game.
        /// </summary>
        public const int TotalQuestions = 5;

        private static DateTime lastRequestTime = DateTime.MinValue;
        private static readonly SemaphoreSlim requestSemaphore = new(1, 1);
        private static readonly Random random = new();
        private static readonly Queue<Question> questions = new();
        private static readonly string[] selector = ["🇦", "🇧", "🇨", "🇩"];

        /// <summary>
        /// Fetches new questions from the trivia API and populates the question queue.
        /// </summary>
        public static async Task GetNewQuestions()
        {
            await requestSemaphore.WaitAsync();
            try
            {
                TimeSpan elapsed = DateTime.Now - lastRequestTime;
                if (elapsed.TotalSeconds < SecondsPerRequest)
                {
                    await Task.Delay((int)((SecondsPerRequest - elapsed.TotalSeconds) * 1000));
                }

                string content = await GetFromAPI($"https://opentdb.com/api.php?amount={MaxQuestionCount}&type=multiple&encode=base64", AcceptTypes.application_json);
                lastRequestTime = DateTime.Now;

                var jsonData = JsonNode.Parse(content).AsObject();
                var results = jsonData["results"].AsArray();

                foreach (var result in results)
                {
                    var question = new Question
                    {
                        QuestionText = DecodeBase64(result["question"].ToString()),
                        Difficulty = DecodeBase64(result["difficulty"].ToString()),
                        Category = DecodeBase64(result["category"].ToString())
                    };

                    var incorrectAnswers = result["incorrect_answers"].AsArray()
                        .Select(e => DecodeBase64(e.ToString()))
                        .ToArray();

                    int correctAnswerIndex = random.Next(4);
                    question.CorrectAnswer = new[] { "a", "b", "c", "d" }[correctAnswerIndex];
                    question.Answers[correctAnswerIndex] = DecodeBase64(result["correct_answer"].ToString());

                    for (int i = 0, j = 0; i < 4; i++)
                    {
                        if (i != correctAnswerIndex)
                        {
                            question.Answers[i] = incorrectAnswers[j++];
                        }
                    }

                    questions.Enqueue(question);
                }
            }
            finally
            {
                requestSemaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves a question from the queue. If the queue is empty, fetches new questions from the API.
        /// </summary>
        /// <returns>A <see cref="Question"/> object representing a trivia question.</returns>
        public static async Task<Question> GetQuestion()
        {
            if (questions.Count == 0)
            {
                await GetNewQuestions();
            }
            return questions.Dequeue();
        }

        /// <summary>
        /// Formats the text of a question for display.
        /// </summary>
        /// <param name="question">The question to format.</param>
        /// <returns>A formatted string representing the question and its answers.</returns>
        private static string FormatQuestionText(Question question)
        {
            var finalText = new StringBuilder()
                .AppendLine($"**{question.QuestionText}**\n")
                .AppendJoin('\n', question.Answers.Select((a, i) => $"{selector[i]} {a}"))
                .AppendLine();

            return finalText.ToString();
        }

        /// <summary>
        /// Creates an embed for displaying a trivia question in Discord.
        /// </summary>
        /// <param name="game">The trivia game containing the question.</param>
        /// <param name="title">The title of the embed.</param>
        /// <returns>An <see cref="Embed"/> object representing the question embed.</returns>
        public static Embed CreateQuestionEmbed(Trivia game, string title)
        {
            var lastQuestion = game.Questions.Last();
            var description = new StringBuilder()
                .AppendLine(title)
                .AppendLine(game.Questions.Count > 1 ?
                    $"**{game.Player1.GlobalName}**: {game.Player1Chart} {(!game.Player2.IsBot ? $"**{game.Player2.GlobalName}**: {game.Player2Chart}" : "")}" : "")
                .AppendLine($"Question: {game.Questions.Count}/{TotalQuestions}\n")
                .AppendLine(FormatQuestionText(lastQuestion))
                .AppendLine($"(Ends {Timestamp.FromDateTime(game.ExpirationTime, Timestamp.Formats.Relative)}).")
                .ToString();

            return new EmbedBuilder
            {
                Color = Challenge.BothPlayersColor,
                Description = description
            }
            .AddField("Category", lastQuestion.Category, inline: true)
            .AddField("Difficulty", lastQuestion.Difficulty, inline: true)
            .WithFooter(GetFooter()).Build();
        }

        /// <summary>
        /// Creates an embed for displaying the final results of a trivia game in Discord.
        /// </summary>
        /// <param name="game">The trivia game containing the results.</param>
        /// <param name="forfeited">Indicates whether the game was forfeited.</param>
        /// <returns>An <see cref="Embed"/> object representing the final results embed.</returns>
        public static Embed CreateFinalEmbed(Trivia game, bool forfeited = false, string thumbnailUrl = "")
        {
            var embed = new EmbedBuilder
            {
                Color = Challenge.BothPlayersColor,
                Description = Challenge.CreateFinalTitle(game, GetWinner(game, forfeited)),
                ThumbnailUrl = thumbnailUrl
            };

            embed.AddField(game.Player1.GlobalName, game.Player1Chart ?? "🟥", inline: true);
            if (!game.Player2.IsBot)
            {
                embed.AddField(game.Player2.GlobalName, game.Player2Chart ?? "🟥", inline: true);
            }

            foreach (var q in game.Questions)
            {
                embed.AddField(q.QuestionText, q.Answers["abcd".IndexOf(q.CorrectAnswer)]);
            }

            return embed.Build();
        }

        /// <summary>
        /// Determines the winner of the trivia game.
        /// </summary>
        /// <param name="game">The trivia game.</param>
        /// <param name="forfeited">Indicates whether the game was forfeited.</param>
        /// <returns>A <see cref="Challenge.WinCases"/> value representing the winner of the game.</returns>
        public static Challenge.WinCases GetWinner(Trivia game, bool forfeited = false)
        {
            if (game.Player2.IsBot)
            {
                return Challenge.WinCases.None;
            }

            if (forfeited)
            {
                if (game.Player1Points == 0 && game.Player2Points == 0)
                {
                    return Challenge.WinCases.Tie;
                }

                if (game.Player1Answer == null && game.Player1Points < game.Player2Points)
                {
                    return Challenge.WinCases.Player2;
                }
            }

            return game.Player1Points switch
            {
                var p1 when p1 < game.Player2Points => Challenge.WinCases.Player2,
                var p1 when p1 == game.Player2Points => Challenge.WinCases.Tie,
                _ => Challenge.WinCases.Player1
            };
        }

        /// <summary>
        /// Gets the footer for the embed, indicating the trivia source.
        /// </summary>
        /// <returns>An <see cref="EmbedFooterBuilder"/> object representing the footer.</returns>
        private static EmbedFooterBuilder GetFooter() =>
            new() { Text = "Powered by Open Trivia Database (unaffiliated)." };

        /// <summary>
        /// Creates buttons for answering trivia questions in Discord.
        /// </summary>
        /// <param name="Id">The ID associated with the buttons.</param>
        /// <param name="disable">Indicates whether to disable the buttons.</param>
        /// <returns>A <see cref="ComponentBuilder"/> object representing the buttons.</returns>
        public static ComponentBuilder GetButtons(ulong Id, bool disable = false)
        {
            var buttons = new ComponentBuilder();
            buttons.WithButton(emote: new Emoji("\U0001F1E6"), customId: $"trivia:a:{Id}", style: ButtonStyle.Secondary, disabled: disable);
            buttons.WithButton(emote: new Emoji("\U0001F1E7"), customId: $"trivia:b:{Id}", style: ButtonStyle.Secondary, disabled: disable);
            buttons.WithButton(emote: new Emoji("\U0001F1E8"), customId: $"trivia:c:{Id}", style: ButtonStyle.Secondary, disabled: disable);
            buttons.WithButton(emote: new Emoji("\U0001F1E9"), customId: $"trivia:d:{Id}", style: ButtonStyle.Secondary, disabled: disable);

            return buttons;
        }

        /// <summary>
        /// Decodes a Base64-encoded string.
        /// </summary>
        /// <param name="encodedText">The Base64-encoded string.</param>
        /// <returns>The decoded string.</returns>
        private static string DecodeBase64(string encodedText) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(encodedText));
    }
}
