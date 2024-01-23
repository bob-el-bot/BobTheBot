using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Commands.Helpers;
using Discord;
using Discord.WebSocket;
using Games;

namespace Challenges
{
    public static class Challenge
    {
        public static readonly Color DefaultColor = Color.LighterGrey;
        public static readonly Color Player1Color = Color.Blue;
        public static readonly Color Player2Color = Color.Red;
        public static readonly Color BothPlayersColor = Color.Purple;
        public static Dictionary<ulong, Games.Game> Games { get; } = new();
        public static Dictionary<ulong, RockPaperScissors> RockPaperScissorsGames { get; } = new();
        public static Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = new();
        public static Dictionary<ulong, Trivia> TriviaGames { get; } = new();

        public static bool CanChallenge(ulong player1Id, ulong player2Id)
        {
            if (player1Id == player2Id)
            {
                return false;
            }

            return true;
        }

        public static async Task SendMessage(SocketInteraction interaction, Games.Game game)
        {
            // Loading Message
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            game.Message = msg;
            game.Id = game.OnePerChannel ? interaction.Channel.Id : msg.Id;
            game.State = GameState.Challenge;

            // Expiration Timer.
            var dateTime = new DateTimeOffset(game.ExpirationTime).ToUnixTimeSeconds();

            // Add to Games List
            AddToSpecificGameList(game);

            // Format Message
            var embed = new EmbedBuilder
            {
                Color = DefaultColor,
                Description = $"### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\nAccept or decline <t:{dateTime}:R>."
            };

            var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptChallenge:{game.Id}", style: ButtonStyle.Success)
            .WithButton(label: "🛡️ Decline", customId: $"declineChallenge:{game.Id}", style: ButtonStyle.Danger);

            // Start Challenge
            game.Expired += ExpireGame;
            await game.Message.ModifyAsync(x => { x.Content = $"> ### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\n> Accept or decline <t:{dateTime}:R>."; x.Components = components.Build(); });
        }

        public static void AddToSpecificGameList(Games.Game game)
        {
            switch (game.Type)
            {
                case GameType.RockPaperScissors:
                    RockPaperScissorsGames.Add(game.Id, (RockPaperScissors)game);
                    break;
                case GameType.TicTacToe:
                    TicTacToeGames.Add(game.Id, (TicTacToe)game);
                    break;
                case GameType.Trivia:
                    TriviaGames.Add(game.Id, (Trivia)game);
                    break;
                default:
                    break;
            }

            Games.Add(game.Id, game);
        }

        public static void RemoveFromSpecificGameList(Games.Game game)
        {
            switch (game.Type)
            {
                case GameType.RockPaperScissors:
                    RockPaperScissorsGames.Remove(game.Id);
                    break;
                case GameType.TicTacToe:
                    TicTacToeGames.Remove(game.Id);
                    break;
                case GameType.Trivia:
                    TriviaGames.Remove(game.Id);
                    break;
                default:
                    break;
            }

            Games.Remove(game.Id);
        }

        public static async void ExpireGame(Games.Game game)
        {
            switch (game.State)
            {
                case GameState.Challenge:
                    // Format Message
                    var embed = new EmbedBuilder
                    {
                        Color = DefaultColor,
                        Description = $"### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\n{game.Player2.Mention} did not respond."
                    };

                    var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptedChallenge", style: ButtonStyle.Success, disabled: true)
                    .WithButton(label: "🛡️ Decline", customId: $"declinedChallenge", style: ButtonStyle.Danger, disabled: true);

                    await game.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Content = null; x.Components = components.Build(); });
                    break;
                case GameState.SettingRules:
                    break;
                case GameState.Active:
                    await game.EndGameOnTime();
                    break;
                case GameState.Ended:
                    break;
            }

            RemoveFromSpecificGameList(game);
            game.Dispose();
        }
    }
}