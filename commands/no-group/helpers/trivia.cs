using System;
using System.Text;
using System.Threading.Tasks;
using Challenges;
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
        public int player2Points;
        public string player2Answer;
        public int questions = 0;
        Question question;

        public Trivia(IUser player1, IUser player2) : base(GameType.Trivia, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
        {

        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Get a question
            question = await TriviaMethods.GetQuestion();

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = TriviaMethods.CreateQuestionEmbed($"### ⚔️ {Player1.Mention}'s Game of {Title}.", question, questions).Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Get a question
            question = await TriviaMethods.GetQuestion();

            // Set State
            State = GameState.Active;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(0.5));
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            await interaction.UpdateAsync(x => { x.Embed = TriviaMethods.CreateQuestionEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.", question, questions, dateTime).Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        public async Task Answer(bool isPlayer1, string answer, SocketMessageComponent component)
        {
            // Set Answer and Update Score.
            if (isPlayer1)
            {
                player1Answer = answer;
                if (question.correctAnswer == player1Answer)
                {
                    player1Points++;
                }
            }
            else
            {
                player2Answer = answer;
                if (question.correctAnswer == player2Answer)
                {
                    player2Points++;
                }
            }

            // Both players have answered
            if (player1Answer != null && player2Answer != null)
            {
                if (questions == TriviaMethods.TotalQuestions)
                {
                    await FinishGame(component);
                }
                else
                {
                    await NextQuestion(component);
                }
            }
        }

        public async Task AloneAnswer(string answer, SocketMessageComponent component)
        {
            // Set Answer and Update Score.
            player1Answer = answer;
            if (question.correctAnswer == player1Answer)
            {
                player1Points++;
            }

            if (questions == TriviaMethods.TotalQuestions)
            {
                await FinishGame(component);
            }
            else
            {
                await NextQuestion(component);
            }
        }

        private async Task NextQuestion(SocketMessageComponent component)
        {
            // reset game values
            player1Answer = null;
            player2Answer = null;

            // Get a question
            question = await TriviaMethods.GetQuestion();

            // update question total
            questions++;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(0.5));
            var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            EmbedBuilder embed;

            if (Player2.IsBot)
            {
                embed = TriviaMethods.CreateQuestionEmbed($"### ⚔️ {Player1.Mention}'s Game of {Title}.", question, questions);
            }
            else
            {
                embed = TriviaMethods.CreateQuestionEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.", question, questions, dateTime);
            }

            await component.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); x.Components = TriviaMethods.GetButtons(Id).Build(); });
        }

        private async Task FinishGame(SocketMessageComponent component)
        {
            await component.ModifyOriginalResponseAsync(x => { x.Embed = TriviaMethods.CreateFinalEmbed(GetFinalTitle(), player1Points, player2Points, Player2.IsBot).Build(); x.Components = TriviaMethods.GetButtons(Id, true).Build(); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;

            await Message.ModifyAsync(x => { x.Embed = TriviaMethods.CreateFinalEmbed(GetFinalTitle(true), player1Points, player2Points, Player2.IsBot).Build(); x.Components = TriviaMethods.GetButtons(Id, true).Build(); });
        }

        private string GetFinalTitle(bool forfeited = false)
        {
            if (Player2.IsBot)
            {
                return $"### ⚔️ {Player1.Mention}'s Completed Game of {Title}.";
            }
            else
            {
                // All ways for player1 to lose
                if (player1Points < player2Points || (forfeited && player1Answer == null && player1Points < player2Points))
                {
                    return $"### ⚔️ {Player1.Mention} Was Defeated By {Player2.Mention} in {Title}.";
                }
                else if ((forfeited && player1Points + player2Points == 0) || player1Points + player2Points == 0) // draw
                {
                    return $"### ⚔️ {Player1.Mention} Drew {Player2.Mention} in {Title}.";
                }
                else // else player1 won
                {
                    return $"### ⚔️ {Player1.Mention} Defeated {Player2.Mention} in {Title}.";
                }
            }
        }
    }
}