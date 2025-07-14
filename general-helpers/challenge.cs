using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.BadgeInterface;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.WebSocket;
using Bob.Games;
using Bob.PremiumInterface;
using Bob.Time.Timestamps;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Challenges
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
        /// <summary>
        /// The color for when a single player game is won (Green).
        /// </summary>
        public static readonly Color SinglePlayerWinColor = Color.Green;
        /// <summary>
        /// The color for when a single player game is lost (Red).
        /// </summary>
        public static readonly Color SinglePlayerLoseColor = Color.Red;

        // Caches
        public static Dictionary<ulong, Games.Game> Games { get; } = [];
        public static Dictionary<ulong, RockPaperScissors> RockPaperScissorsGames { get; } = [];
        public static Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = [];
        public static Dictionary<ulong, Trivia> TriviaGames { get; } = [];
        public static Dictionary<ulong, Connect4> Connect4Games { get; set; } = [];
        public static Dictionary<ulong, Wordle> WordleGames { get; set; } = [];
        public static Dictionary<ulong, uint?> UserChallenges { get; } = [];

        /// <summary>
        /// Checks if a user can challenge another user asynchronously.
        /// </summary>
        /// <param name="player1Id">The ID of the challenger.</param>
        /// <param name="player2Id">The ID of the user being challenged.</param>
        /// <returns>A tuple indicating whether the challenge is possible and a message explaining why or why not.</returns>
        public static async Task<(bool, string)> CanChallengeAsync(ulong player1Id, ulong player2Id)
        {
            using var scope = Bot.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
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
            await interaction.RespondAsync(text: $"{game.Player2.Mention}\n⚔️ *Creating Challenge...*", allowedMentions: AllowedMentions.All);

            // Update User Info
            IncrementUserChallenges(game.Player1.Id);

            game.Message = await interaction.GetOriginalResponseAsync();
            game.Id = game.OnePerChannel ? interaction.Channel.Id : game.Message.Id;
            game.State = GameState.Challenge;

            AddToSpecificGameList(game);
            game.Expired += ExpireGame;

            switch (game.Type)
            {
                case GameType.Trivia:
                    var componentsV2 = new ComponentBuilderV2()
                        .WithContainer(new ContainerBuilder()
                            .WithTextDisplay($"### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\nAccept or decline {Timestamp.FromDateTime(game.ExpirationTime, Timestamp.Formats.Relative)}.")
                            .WithAccentColor(DefaultColor)
                            .WithActionRow([
                                new ButtonBuilder(label: "⚔️ Accept", customId: $"acceptChallenge:{game.Id}", style: ButtonStyle.Success),
                                new ButtonBuilder(label: "🛡️ Decline", customId: $"declineChallenge:{game.Id}", style: ButtonStyle.Danger),
                            ]))
                        .Build();

                    await interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = null;
                        x.Embed = null;
                        x.Components = componentsV2;
                        x.Flags = MessageFlags.ComponentsV2;
                    });

                    break;
                default:
                    // Format Message
                    var embed = new EmbedBuilder
                    {
                        Color = DefaultColor,
                        Description = $"### ⚔️ {game.Player1.Mention} Challenges You to {game.Title}.\nAccept or decline {Timestamp.FromDateTime(game.ExpirationTime, Timestamp.Formats.Relative)}."
                    };

                    var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptChallenge:{game.Id}", style: ButtonStyle.Success)
                    .WithButton(label: "🛡️ Decline", customId: $"declineChallenge:{game.Id}", style: ButtonStyle.Danger);

                    await interaction.ModifyOriginalResponseAsync(x => { x.Content = game.Player2.Mention; x.Embed = embed.Build(); x.Components = components.Build(); });
                    break;
            }
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
                case GameType.Wordle:
                    WordleGames.Add(game.Id, (Wordle)game);
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
                case GameType.Wordle:
                    WordleGames.Remove(game.Id);
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
                            Description = $"### ⚔️ {game.Player1.Mention} Challenged {game.Player2.Mention} to {game.Title}.\n{game.Player2.Mention} did not respond."
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
        /// Determines the first turn randomly.
        /// </summary>
        /// <returns>True if player 1 starts, false if player 2 starts.</returns>
        public static bool DetermineFirstTurn()
        {
            Random random = new();
            if (random.Next(0, 2) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates an embed for Connect4 game messages.
        /// </summary>
        /// <param name="isPlayer1Turn">Flag indicating if it's player 1's turn.</param>
        /// <param name="description">Description for the embed.</param>
        /// <returns>The created embed.</returns>
        public static Embed CreateTurnBasedEmbed(bool isPlayer1Turn, string description, string thumbnailUrl = "", WinCases winner = WinCases.None)
        {
            return CreateEmbed(description, GetTurnBasedColor(isPlayer1Turn, winner), thumbnailUrl);
        }

        /// <summary>
        /// Gets the color for the turn-based game embed based on the turn and winner.
        /// </summary>
        /// <param name="isPlayer1Turn">Flag indicating if it's player 1's turn.</param>
        /// <param name="winner">The winner of the game.</param>
        /// <returns>The color for the embed.</returns>
        private static Color GetTurnBasedColor(bool isPlayer1Turn, WinCases winner)
        {
            return winner switch
            {
                WinCases.Player1 => Player1Color,
                WinCases.Player2 => Player2Color,
                WinCases.Tie => BothPlayersColor,
                _ => isPlayer1Turn ? Player1Color : Player2Color,
            };
        }

        /// <summary>
        /// Creates an embed with the specified description, color, and optional thumbnail URL.
        /// </summary>
        /// <param name="description">The description text to include in the embed.</param>
        /// <param name="color">The color of the embed.</param>
        /// <param name="thumbnailUrl">The optional URL of the thumbnail image to include in the embed. Default is an empty string.</param>
        /// <returns>An <see cref="Embed"/> object with the specified properties.</returns>
        public static Embed CreateEmbed(string description, Color color, string thumbnailUrl = "")
        {
            return new EmbedBuilder
            {
                Color = color,
                Description = description,
                ThumbnailUrl = thumbnailUrl
            }.Build();
        }

        /// <summary>
        /// Generates the title for the final outcome of a game.
        /// </summary>
        /// <param name="game">The game instance.</param>
        /// <param name="winner">The winner of the game.</param>
        /// <returns>The title for the final outcome.</returns>
        public static string CreateFinalTitle(Games.Game game, WinCases winner)
        {
            switch (winner)
            {
                case WinCases.Player2:
                    return $"## 🏆 {game.Player2.Mention} Wins!\n**against** {game.Player1.Mention}";
                case WinCases.Tie:
                    return $"## 🤝 {game.Player1.Mention} Drew {game.Player2.Mention}!";
                case WinCases.Player1:
                    return $"## 🏆 {game.Player1.Mention} Wins!\n**against** {game.Player2.Mention}";
                default:
                    if (game.Type == GameType.Trivia)
                    {
                        var triviaGame = (Trivia)game;
                        return $"## 💡 {game.Player1.Mention} got {triviaGame.Player1Points}/{TriviaMethods.TotalQuestions}!";
                    }
                    else
                    {
                        return $"## 🤝 {game.Player1.Mention} Drew {game.Player2.Mention}!";
                    }
            }
        }

        /// <summary>
        /// Generates the final thumbnail URL based on the winning player.
        /// </summary>
        /// <param name="player1">The first player involved in the match.</param>
        /// <param name="player2">The second player involved in the match.</param>
        /// <param name="winner">The result of the match indicating the winner.</param>
        /// <param name="singlePlayer">Flag indicating if the game is single player.</param>
        /// <returns>
        /// A string representing the thumbnail URL of the winning player's avatar. 
        /// If there is a tie, an empty string is returned. 
        /// If the winner is not specified, an empty string is returned.
        /// </returns>
        public static string GetFinalThumbnailUrl(IUser player1, IUser player2, WinCases winner, bool singlePlayer = false)
        {
            return (singlePlayer, winner) switch
            {
                (true, WinCases.Player1) => player1.GetDisplayAvatarUrl(),
                (_, WinCases.Player1) => player1.GetDisplayAvatarUrl(),
                (_, WinCases.Player2) => player2.GetDisplayAvatarUrl(),
                (_, WinCases.Tie) => "",
                _ => "",
            };
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
                case GameType.Connect4:
                    user.TotalConnect4Games++;
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
            // Increment win streak if the user won, otherwise reset it
            if ((isPlayer1 && winner == WinCases.Player1) || (!isPlayer1 && winner == WinCases.Player2))
            {
                user.WinStreak++;
            }
            else
            {
                user.WinStreak = 0;
            }

            // Update win counts based on game outcome, including ties
            switch (gameType)
            {
                case GameType.RockPaperScissors:
                    if ((winner == WinCases.Player1 && isPlayer1) || (winner == WinCases.Player2 && !isPlayer1))
                    {
                        user.RockPaperScissorsWins += 1.0f;
                    }
                    else if (winner == WinCases.Tie)
                    {
                        user.RockPaperScissorsWins += 0.5f;
                    }
                    break;
                case GameType.TicTacToe:
                    if ((winner == WinCases.Player1 && isPlayer1) || (winner == WinCases.Player2 && !isPlayer1))
                    {
                        user.TicTacToeWins += 1.0f;
                    }
                    else if (winner == WinCases.Tie)
                    {
                        user.TicTacToeWins += 0.5f;
                    }
                    break;
                case GameType.Trivia:
                    if ((winner == WinCases.Player1 && isPlayer1) || (winner == WinCases.Player2 && !isPlayer1))
                    {
                        user.TriviaWins += 1.0f;
                    }
                    else if (winner == WinCases.Tie)
                    {
                        user.TriviaWins += 0.5f;
                    }
                    break;
                case GameType.Connect4:
                    if ((winner == WinCases.Player1 && isPlayer1) || (winner == WinCases.Player2 && !isPlayer1))
                    {
                        user.Connect4Wins += 1.0f;
                    }
                    else if (winner == WinCases.Tie)
                    {
                        user.Connect4Wins += 0.5f;
                    }
                    break;
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
            using var scope = Bot.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            var userIds = new[] { game.Player1.Id, game.Player2.Id };
            var users = await context.GetUsers(userIds);

            // Ensure users[0] is Player1 and users[1] is Player2
            User player1 = users.First(u => u.Id == game.Player1.Id);
            User player2 = users.First(u => u.Id == game.Player2.Id);

            // Update stats accordingly
            player1 = UpdateSpecificGameUserStats(game.Type, player1, winner, true);
            player2 = UpdateSpecificGameUserStats(game.Type, player2, winner, false);

            var updatedUsers = Badge.CheckGivingUserBadge([player1, player2], Bob.Badges.Badges.Winner3);

            await context.UpdateUsers(updatedUsers);
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