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

        public int Player1Points;
        public string Player1Answer;
        public string Player1Chart;
        public int Player2Points;
        public string Player2Answer;
        public string Player2Chart;
        public List<Question> Questions = new();

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
            Questions.Add(await TriviaMethods.GetQuestion());

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention}'s Game of {Title}."); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Get a question
            Questions.Add(await TriviaMethods.GetQuestion());

            // Set State
            State = GameState.Active;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(0.5));

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}."); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        public async Task Answer(bool isPlayer1, string answer, SocketMessageComponent component)
        {
            // Set Answer and Update Score.
            if (isPlayer1)
            {
                Player1Answer = answer;
                if (Questions.Last().CorrectAnswer == Player1Answer)
                {
                    Player1Points++;
                    Player1Chart += "🟩";
                }
                else
                {
                    Player1Chart += "🟥";
                }
            }
            else
            {
                Player2Answer = answer;
                if (Questions.Last().CorrectAnswer == Player2Answer)
                {
                    Player2Points++;
                    Player2Chart += "🟩";
                }
                else
                {
                    Player2Chart += "🟥";
                }
            }

            // Both players have answered
            if (Player1Answer != null && Player2Answer != null)
            {
                if (Questions.Count == TriviaMethods.TotalQuestions)
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
            Player1Answer = answer;
            if (Questions.Last().CorrectAnswer == Player1Answer)
            {
                Player1Points++;
                Player1Chart += "🟩";
            }
            else
            {
                Player1Chart += "🟥";
            }

            if (Questions.Count == TriviaMethods.TotalQuestions)
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

            await component.ModifyOriginalResponseAsync(x => { x.Embed = TriviaMethods.CreateQuestionEmbed(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}."); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        private async Task NextQuestion(SocketMessageComponent component)
        {
            // reset game values
            Player1Answer = null;
            Player2Answer = null;

            // Get a question
            Questions.Add(await TriviaMethods.GetQuestion());

            Embed embed;

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

            await component.ModifyOriginalResponseAsync(x => { x.Embed = embed; x.Components = TriviaMethods.GetButtons(Id).Build(); });
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