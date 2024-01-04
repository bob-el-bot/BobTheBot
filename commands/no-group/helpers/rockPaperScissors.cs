using System;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.WebSocket;
using Games;

namespace Commands.Helpers
{
    public class RockPaperScissors : Games.Game
    {
        public override string Title { get; } = "Rock Paper Scissors";
        public const bool onePerChannel = false;

        // -1 = null |  0 = rock | 1 = paper | 2 = scissors
        public int player1Choice = -1;
        public int player2Choice = -1;

        public RockPaperScissors(IUser player1, IUser player2) : base(GameType.RockPaperScissors, onePerChannel, player1, player2)
        {

        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;
            BotPlay();

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Format Message
            var dateTime = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

            var components = new ComponentBuilder().WithButton(label: "🪨 Rock", customId: $"rps:0:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "📃 Paper", customId: $"rps:1:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "✂️ Scissors", customId: $"rps:2:{Id}", style: ButtonStyle.Secondary);

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = CreateEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\nAnswer <t:{dateTime}:R>.").Build(); x.Components = components.Build(); });
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            var dateTime = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();

            var components = new ComponentBuilder().WithButton(label: "🪨 Rock", customId: $"rps:0:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "📃 Paper", customId: $"rps:1:{Id}", style: ButtonStyle.Secondary)
            .WithButton(label: "✂️ Scissors", customId: $"rps:2:{Id}", style: ButtonStyle.Secondary);

            await interaction.UpdateAsync(x => { x.Embed = CreateEmbed($"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\nAnswer <t:{dateTime}:R>.").Build(); x.Components = components.Build(); });
        }

        public override async Task EndGame()
        {
            string[] options = { "🪨", "📃", "✂️" };

            await Message.ModifyAsync(x => { x.Embed = CreateEmbed($"{GetFinalTitle()}\n{options[player1Choice]} **VS** {options[player2Choice]}").Build(); x.Components = null; });
            
            Challenge.RemoveFromSpecificGameList(this);
        }

        private static EmbedBuilder CreateEmbed(string description)
        {
            return new EmbedBuilder
            {
                Color = Challenge.DefaultColor,
                Description = description
            };
        }

        private string GetFinalTitle()
        {
            // All ways for player1 to lose
            if ((player1Choice == 0 && player2Choice == 1) || (player1Choice == 1 && player2Choice == 2) || (player1Choice == 2 && player2Choice == 0))
            {
                return $"### ⚔️ {Player1.Mention} Was Defeated By {Player2.Mention} in {Title}.";
            }
            else if (player1Choice == player2Choice)
            {
                return $"### ⚔️ {Player1.Mention} Drew {Player2.Mention} in {Title}.";
            }
            else // else player1 won
            {
                return $"### ⚔️ {Player1.Mention} Defeated {Player2.Mention} in {Title}.";
            }
        }

        private void BotPlay()
        {
            Random random = new();
            player2Choice = random.Next(0, 3);
        }
    }
}