using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadgeInterface;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.WebSocket;
using Games;
using PremiumInterface;
using TimeStamps;

namespace Challenges
{
    public static class Challenge
    {
        /// <summary>
        /// The default color for challenge messages (Grey).
        /// </summary>
        public static readonly Color DefaultColor = Color.LighterGrey;
        /// <summary>
        /// The color for player 1 in challenge messages (Blue).
        /// </summary>
        public static readonly Color Player1Color = Color.Blue;
        /// <summary>
        /// The color for player 2 in challenge messages (Red).
        /// </summary>
        public static readonly Color Player2Color = Color.Red;
        /// <summary>
        /// The color for messages involving both players (Purple).
        /// </summary>
        public static readonly Color BothPlayersColor = Color.Purple;

        // Caches
        public static Dictionary<ulong, Games.Game> Games { get; } = new();
        public static Dictionary<ulong, RockPaperScissors> RockPaperScissorsGames { get; } = new();
        public static Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = new();
        public static Dictionary<ulong, Trivia> TriviaGames { get; } = new();
        public static Dictionary<ulong, Connect4> Connect4Games { get; set; } = new();
        public static Dictionary<ulong, uint?> UserChallenges { get; } = new();

        /// <summary>
        /// Checks if a user can challenge another user asynchronously.
        /// </summary>
        /// <param name="player1Id">The ID of the challenger.</param>
        /// <param name="player2Id">The ID of the user being challenged.</param>
        /// <returns>A tuple indicating whether the challenge is possible and a message explaining why or why not.</returns>
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
                return (false, $"❌ You are already in a challenge.\n- Get ✨ premium to play **unlimited** multiplayer games.\n- {Premium.HasPremiumMessage}");
            }

            return (true, "Loading...");
        }

        /// <summary>
        /// Sends a challenge message to the specified interaction.
        /// </summary>
        /// <param name="interaction">The interaction context.</param>
        /// <param name="game">The game being challenged.</param>
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

            // Add to Games List
            AddToSpecificGameList(game);

            // Format Message
            var embed = new EmbedBuilder
            {
                Color = DefaultColor,
                Description = $"### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\nAccept or decline {TimeStamp.FromDateTime(game.ExpirationTime, TimeStamp.Formats.Relative)}."
            };

            var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptChallenge:{game.Id}", style: ButtonStyle.Success)
            .WithButton(label: "🛡️ Decline", customId: $"declineChallenge:{game.Id}", style: ButtonStyle.Danger);

            // Start Challenge
            game.Expired += ExpireGame;
            await game.Message.ModifyAsync(x => { x.Content = null; x.Embed = embed.Build(); x.Components = components.Build(); });
        }

        /// <summary>
        /// Adds the game to the specific game list based on its type.
        /// </summary>
        /// <param name="game">The game to be added.</param>
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
                case GameType.Connect4:
                    Connect4Games.Add(game.Id, (Connect4)game);
                    break;
                default:
                    break;
            }

            Games.Add(game.Id, game);
        }

        /// <summary>
        /// Removes the game from the specific game list based on its type.
        /// </summary>
        /// <param name="game">The game to be removed.</param>
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
                case GameType.Connect4:
                    Connect4Games.Remove(game.Id);
                    break;
                default:
                    break;
            }

            Games.Remove(game.Id);
        }

        /// <summary>
        /// Handles the expiration of a game challenge.
        /// </summary>
        /// <param name="game">The game that has expired.</param>
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

        /// <summary>
        /// Updates the specific game-related statistics of a user based on the game type and outcome.
        /// </summary>
        /// <param name="gameType">The type of the game.</param>
        /// <param name="user">The user whose statistics will be updated.</param>
        /// <param name="winner">The winner of the game (Player1, Player2, or Tie).</param>
        /// <param name="isPlayer1">Specifies whether the user is Player 1 in the game.</param>
        /// <returns>The updated user object.</returns>
        private static User UpdateSpecificGameUserStats(GameType gameType, User user, WinCases winner, bool isPlayer1)
        {
            switch (gameType)
            {
                case GameType.RockPaperScissors:
                    user.TotalRockPaperScissorsGames++;
                    break;
                case GameType.TicTacToe:
                    user.TotalTicTacToeGames++;
                    break;
                case GameType.Trivia:
                    user.TotalTriviaGames++;
                    break;
                default:
                    break;
            }

            user = UpdateGameUserStats(user, winner, gameType, isPlayer1);
            return user;
        }

        /// <summary>
        /// Updates the general game-related statistics of a user based on the game outcome.
        /// </summary>
        /// <param name="user">The user whose statistics will be updated.</param>
        /// <param name="winner">The winner of the game (Player1, Player2, or Tie).</param>
        /// <param name="gameType">The type of the game.</param>
        /// <param name="isPlayer1">Specifies whether the user is Player 1 in the game.</param>
        /// <returns>The updated user object.</returns>
        private static User UpdateGameUserStats(User user, WinCases winner, GameType gameType, bool isPlayer1)
        {
            if (winner == WinCases.Player1 || winner == WinCases.Player2 || winner == WinCases.Tie)
            {
                if ((isPlayer1 && winner == WinCases.Player1) || (!isPlayer1 && winner == WinCases.Player2))
                {
                    user.WinStreak++;
                }
                else
                {
                    user.WinStreak = 0;
                }

                switch (gameType)
                {
                    case GameType.RockPaperScissors:
                        if (isPlayer1 && winner == WinCases.Player1)
                        {
                            user.RockPaperScissorsWins++;
                        }
                        else if (!isPlayer1 && winner == WinCases.Player2)
                        {
                            user.RockPaperScissorsWins++;
                        }
                        else if (winner == WinCases.Tie)
                        {
                            user.RockPaperScissorsWins += 0.5f;
                        }
                        break;
                    case GameType.TicTacToe:
                        if (isPlayer1 && winner == WinCases.Player1)
                        {
                            user.TicTacToeWins++;
                        }
                        else if (!isPlayer1 && winner == WinCases.Player2)
                        {
                            user.TicTacToeWins++;
                        }
                        else if (winner == WinCases.Tie)
                        {
                            user.TicTacToeWins += 0.5f;
                        }
                        break;
                    case GameType.Trivia:
                        if (isPlayer1 && winner == WinCases.Player1)
                        {
                            user.TriviaWins++;
                        }
                        else if (!isPlayer1 && winner == WinCases.Player2)
                        {
                            user.TriviaWins++;
                        }
                        else if (winner == WinCases.Tie)
                        {
                            user.TriviaWins += 0.5f;
                        }
                        break;
                }
            }

            return user;
        }

        /// <summary>
        /// Updates the statistics of users involved in a game and checks if they qualify for any badges.
        /// </summary>
        /// <param name="game">The game being played.</param>
        /// <param name="winner">The winner of the game.</param>
        public static async Task UpdateUserStats(Games.Game game, WinCases winner)
        {
            using var context = new BobEntities();
            var userIds = new[] { game.Player1.Id, game.Player2.Id };
            var users = await context.GetUsers(userIds);

            users[0] = UpdateSpecificGameUserStats(game.Type, users[0], winner, true);
            users[1] = UpdateSpecificGameUserStats(game.Type, users[1], winner, false);

            users = Badge.CheckGivingUserBadge(users, Badges.Badges.Winner3);

            await context.UpdateUsers(users);
        }

        /// <summary>
        /// Gets the number of challenges for a specific user.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>The number of challenges for the user.</returns>
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

        /// <summary>
        /// Increments the number of challenges for a specific user.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
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

        /// <summary>
        /// Decrements the number of challenges for a specific user.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
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

        /// <summary>
        /// Adds a user to the user challenges dictionary if not already present.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        public static void AddUserChallenges(ulong id)
        {
            if (!UserChallenges.ContainsKey(id))
            {
                UserChallenges[id] = 0;
            }
        }

        public enum WinCases
        {
            Player1,
            Player2,
            Tie,
            None
        }
    }
}