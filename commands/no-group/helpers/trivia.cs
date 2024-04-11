using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Challenges;
using Database;
using Database.Types;
using Discord;
using Discord.WebSocket;
using Games;

namespace Commands.Helpers
{
    public class Trivia : Games.Game
    {
        public override string Title { get; } = "Trivia";
        public const bool onePerChannel = false;

        public int player1Points;
        public string player1Answer;
        public string player1Chart;
        public int player2Points;
        public string player2Answer;
        public string player2Chart;
        public List<Question> questions = new();

        public Trivia(IUser player1, IUser player2) : base(GameType.Trivia, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
        {

        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;
            State = GameState.Active;

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(5));

            Expired += Challenge.ExpireGame;

            // Get a question
            questions.Add(await TriviaMethods.GetQuestion());

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention}'s Game of {Title}.").Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Get a question
            questions.Add(await TriviaMethods.GetQuestion());

            // Set State
            State = GameState.Active;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(0.5));

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.").Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        public async Task Answer(bool isPlayer1, string answer, SocketMessageComponent component)
        {
            // Set Answer and Update Score.
            if (isPlayer1)
            {
                player1Answer = answer;
                if (questions.Last().correctAnswer == player1Answer)
                {
                    player1Points++;
                    player1Chart += "🟩";
                }
                else
                {
                    player1Chart += "🟥";
                }
            }
            else
            {
                player2Answer = answer;
                if (questions.Last().correctAnswer == player2Answer)
                {
                    player2Points++;
                    player2Chart += "🟩";
                }
                else
                {
                    player2Chart += "🟥";
                }
            }

            // Both players have answered
            if (player1Answer != null && player2Answer != null)
            {
                if (questions.Count == TriviaMethods.TotalQuestions)
                {
                    await FinishGame(component);
                }
                else
                {
                    await NextQuestion(component);
                }
            }
            else
            {
                await UpdateQuestion(component);
            }
        }

        public async Task AloneAnswer(string answer, SocketMessageComponent component)
        {
            // Set Answer and Update Score.
            player1Answer = answer;
            if (questions.Last().correctAnswer == player1Answer)
            {
                player1Points++;
                player1Chart += "🟩";
            }
            else
            {
                player1Chart += "🟥";
            }

            if (questions.Count == TriviaMethods.TotalQuestions)
            {
                await FinishGame(component);
            }
            else
            {
                await NextQuestion(component);
            }
        }

        private async Task UpdateQuestion(SocketMessageComponent component)
        {
            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(0.5));

            EmbedBuilder embed;
            embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.");

            await component.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        private async Task NextQuestion(SocketMessageComponent component)
        {
            // reset game values
            player1Answer = null;
            player2Answer = null;

            // Get a question
            questions.Add(await TriviaMethods.GetQuestion());

            EmbedBuilder embed;

            if (Player2.IsBot)
            {
                embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention}'s Game of {Title}.");
            }
            else
            {
                // Reset Expiration Time.
                UpdateExpirationTime(TimeSpan.FromMinutes(0.5));

                embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.");
            }

            await component.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        private async Task FinishGame(SocketMessageComponent component)
        {
            _ = EndGame();

            // If not a bot match update stats.
            if (!Player2.IsBot)
            {
                Challenge.DecrementUserChallenges(Player1.Id);
                Challenge.DecrementUserChallenges(Player2.Id);

                await Challenge.UpdateUserStats(this, TriviaMethods.GetWinner(this));
            }

            try
            {
                await component.ModifyOriginalResponseAsync(x => { x.Embed = TriviaMethods.CreateFinalEmbed(this).Build(); x.Components = TriviaMethods.GetButtons(Id, true).Build(); });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;

            // If not a bot match update stats.
            if (!Player2.IsBot)
            {
                Challenge.DecrementUserChallenges(Player1.Id);
                Challenge.DecrementUserChallenges(Player2.Id);

                await Challenge.UpdateUserStats(this, TriviaMethods.GetWinner(this, true));
            }

            try
            {
                await Message.ModifyAsync(x => { x.Embed = TriviaMethods.CreateFinalEmbed(this, true).Build(); x.Components = TriviaMethods.GetButtons(Id, true).Build(); });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }
    }
}