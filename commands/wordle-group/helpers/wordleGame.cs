using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.WebSocket;
using Games;
using Time.Timestamps;

namespace Commands.Helpers
{
    public class Wordle : Games.Game
    {
        public override string Title { get; } = "Wordle";
        private static readonly bool onePerChannel = true;

        public string Word { get; set; }
        public List<(string Result, string Guess)> Guesses { get; set; }
        public int GuessesLeft { get; set; } = WordleMethods.GuessCount;

        public Wordle(IUser player1, IUser player2) : base(GameType.Wordle, onePerChannel, TimeSpan.FromMinutes(10), player1, player2)
        {
            Guesses = new();
        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = interaction.Channel.Id;
            State = GameState.Active;

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(10));

            Expired += Challenge.ExpireGame;

            // Get a word
            Word = WordleMethods.GetRandomWord();
            Console.WriteLine(Word);

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = WordleMethods.CreateEmbed(this); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;
            Challenge.WinCases outcome = Challenge.WinCases.None;

            try
            {
                Challenge.DecrementUserChallenges(Player1.Id);

                await Message.ModifyAsync(x => { x.Embed = Challenge.CreateEmbed(WordleMethods.CreateFinalTitle(this), Challenge.SinglePlayerLoseColor, Challenge.GetFinalThumbnailUrl(Player1, Player2, outcome, true)); x.Components = null; });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public async Task FinishGame(bool lost)
        {
            Challenge.WinCases outcome = lost ? Challenge.WinCases.None : Challenge.WinCases.Player1;

            try
            {
                Challenge.DecrementUserChallenges(Player1.Id);

                await Message.ModifyAsync(x => { x.Embed = Challenge.CreateEmbed(WordleMethods.CreateFinalTitle(this), lost ? Challenge.SinglePlayerLoseColor : Challenge.SinglePlayerWinColor, Challenge.GetFinalThumbnailUrl(Player1, Player2, outcome, true)); x.Components = null; });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }

            _ = EndGame();
        }
    }
}