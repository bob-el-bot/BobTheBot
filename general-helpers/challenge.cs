using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.WebSocket;
using Games;
using PremiumInterface;

namespace Challenges
{
    public static class Challenge
    {
        public static readonly Color DefaultColor = Color.LighterGrey;
        public static readonly Color Player1Color = Color.Blue;
        public static readonly Color Player2Color = Color.Red;
        public static readonly Color BothPlayersColor = Color.Purple;

        // Caches
        public static Dictionary<ulong, Games.Game> Games { get; } = new();
        public static Dictionary<ulong, RockPaperScissors> RockPaperScissorsGames { get; } = new();
        public static Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = new();
        public static Dictionary<ulong, Trivia> TriviaGames { get; } = new();
        public static Dictionary<ulong, uint?> UserChallenges { get; } = new();
        // public static Dictionary<ulong, Connect4> Connect4Games { get; } = new();

        public static async Task<(bool, string)> CanChallengeAsync(ulong player1Id, ulong player2Id)
        {
            using var context = new BobEntities();
            User user = await context.GetUser(player1Id);

            if (player1Id == player2Id)
            {
                return (false, "❌ You cannot play yourself...");
            }

            uint player1Challenges = GetUserChallenges(player1Id);

            // If a user is already in a challenge and is not premium they cannot challenge.
            if (player1Challenges >= Premium.ChallengeLimit && Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                return (false, "❌ You are already in a challenge.\n- Get ✨ premium to play **unlimited** multiplayer games.\n- If you have premium (💜 **thanks so much!**) simply use `/premium` to unlock all of the features.");
            }

            return (true, "Loading...");
        }

        public static async Task SendMessage(SocketInteraction interaction, Games.Game game)
        {
            // Loading Message
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Update User Info
            IncrementUserChallenges(game.Player1.Id);

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
                // case GameType.Connect4:
                //     Connect4Games.Add(game.Id, (Connect4)game);
                //     break;
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
                // case GameType.Connect4:
                //     Connect4Games.Remove(game.Id);
                //     break;
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
                    try
                    {
                        // Format Message
                        var embed = new EmbedBuilder
                        {
                            Color = DefaultColor,
                            Description = $"### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\n{game.Player2.Mention} did not respond."
                        };

                        var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptedChallenge", style: ButtonStyle.Success, disabled: true)
                        .WithButton(label: "🛡️ Decline", customId: $"declinedChallenge", style: ButtonStyle.Danger, disabled: true);

                        await game.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Content = null; x.Components = components.Build(); });

                        // Update User Info
                        DecrementUserChallenges(game.Player1.Id);
                    }
                    catch (Exception)
                    {
                        // Do nothing Because the challenge will already be deleted.
                    }
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

        public static uint GetUserChallenges(ulong id)
        {
            UserChallenges.TryGetValue(id, out uint? challenges);

            if (challenges == null)
            {
                UserChallenges.Add(key: id, value: 0);
                return 0;
            }
            else
            {
                return (uint)challenges;
            }
        }

        public static void IncrementUserChallenges(ulong id)
        {
            if (UserChallenges.ContainsKey(id))
            {
                uint? value = UserChallenges[id];
                if (value.HasValue)
                {
                    UserChallenges[id] = value + 1;
                }
                else
                {
                    UserChallenges[id] = 1;
                }
            }
            else
            {
                UserChallenges[id] = 1;
            }
        }

        public static void DecrementUserChallenges(ulong id)
        {
            if (UserChallenges.ContainsKey(id))
            {
                uint? value = UserChallenges[id];
                if (value.HasValue && value > 0)
                {
                    UserChallenges[id] = value - 1;
                }
            }
            else
            {
                UserChallenges[id] = 0;
            }
        }

        public static void AddUserChallenges(ulong id)
        {
            if (!UserChallenges.ContainsKey(id))
            {
                UserChallenges[id] = 0;
            }
        }
    }
}