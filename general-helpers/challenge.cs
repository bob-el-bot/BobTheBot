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
        public static readonly Color color = Color.LighterGrey;
        public static Dictionary<ulong, Games.Game> Games { get; } = new();
        public static Dictionary<ulong, RockPaperScissors> RockPaperScissorsGames { get; } = new();

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
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            game.Message = msg;
            game.Id = msg.Id;

            // Add to Games List
            Games.Add(game.Id, game);
            AddToSpecificGameList(game);

            // Format Message
            var dateTime = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

            var embed = new EmbedBuilder
            {
                //Color = game.Player1Turn ? new Color(3447003) : new Color(15548997),
                Color = color,
                Description = $"### ⚔️ {game.Player1.Mention} Challenges {game.Player2.Mention} to {game.Title}.\nAccept or decline <t:{dateTime}:R>."
            };

            var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptChallenge:{game.Id}", style: ButtonStyle.Success)
            .WithButton(label: "🛡️ Decline", customId: $"declineChallenge:{game.Id}", style: ButtonStyle.Danger);

            // Start Challenge
            await game.Message.ModifyAsync(x => { x.Content = null; x.Embed = embed.Build(); x.Components = components.Build(); });
        }

        public static void AddToSpecificGameList(Games.Game game)
        {
            switch (game.Type)
            {
                case GameType.rps:
                    RockPaperScissors rps = (RockPaperScissors)game;
                    RockPaperScissorsGames.Add(game.Id, rps);
                    break;
                default:
                    break;
            }
        }
    }
}