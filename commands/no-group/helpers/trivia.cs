using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bob.Challenges;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.WebSocket;
using Bob.Games;

namespace Bob.Commands.Helpers
{
    public class Trivia(IUser player1, IUser player2) : Games.Game(GameType.Trivia, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
    {
        public override string Title { get; } = "Trivia";
        private static readonly bool onePerChannel = false;

        public int Player1Points { get; set; }
        public string Player1Answer { get; set; }
        public string Player1Chart { get; set; }
        public int Player2Points { get; set; }
        public string Player2Answer { get; set; }
        public string Player2Chart { get; set; }
        public List<Question> Questions { get; set; } = [];

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

            await Message.ModifyAsync(x => { x.Content = null; x.Components = TriviaMethods.CreateQuestionEmbedCV2(this, $"### ⚔️ {Player1.Mention}'s Game of {Title}."); x.Flags = MessageFlags.ComponentsV2; });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Get a question
            Questions.Add(await TriviaMethods.GetQuestion());

            // Set State
            State = GameState.Active;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(0.5));

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Components = TriviaMethods.CreateQuestionEmbedCV2(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}."); });
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
            await component.ModifyOriginalResponseAsync(x => { x.Components = TriviaMethods.CreateQuestionEmbedCV2(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}."); });
        }

        private async Task NextQuestion(SocketMessageComponent component)
        {
            // Reset game values
            Player1Answer = null;
            Player2Answer = null;

            // Get a question
            Questions.Add(await TriviaMethods.GetQuestion());

            if (Player2.IsBot == false)
            {
                // Reset Expiration Time.
                UpdateExpirationTime(TimeSpan.FromMinutes(0.5));
            }

            await component.ModifyOriginalResponseAsync(x => { x.Components = TriviaMethods.CreateQuestionEmbedCV2(this, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}."); });
        }

        private async Task FinishGame(SocketMessageComponent component)
        {
            _ = EndGame();
            Challenge.WinCases outcome = TriviaMethods.GetWinner(this);

            // If not a bot match update stats.
            if (!Player2.IsBot)
            {
                Challenge.DecrementUserChallenges(Player1.Id);
                Challenge.DecrementUserChallenges(Player2.Id);

                await Challenge.UpdateUserStats(this, outcome);
            }

            try
            {
                await component.ModifyOriginalResponseAsync(x => { x.Components = TriviaMethods.CreateFinalEmbedCV2(this, forfeited: true, thumbnailUrl: Challenge.GetFinalThumbnailUrl(Player1, Player2, outcome)); });
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
            Challenge.WinCases outcome = TriviaMethods.GetWinner(this, true);

            // If not a bot match update stats.
            if (!Player2.IsBot)
            {
                Challenge.DecrementUserChallenges(Player1.Id);
                Challenge.DecrementUserChallenges(Player2.Id);

                await Challenge.UpdateUserStats(this, outcome);
            }

            try
            {
                await Message.ModifyAsync(x => { x.Components = TriviaMethods.CreateFinalEmbedCV2(this, forfeited: true, thumbnailUrl: Challenge.GetFinalThumbnailUrl(Player1, Player2, outcome)); });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Do nothing Because the game will already be deleted.
            }
        }
    }
}