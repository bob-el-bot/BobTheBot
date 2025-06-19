using System.Threading.Tasks;
using System.Linq;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("mastermind", "All commands relevant to the game Master Mind.")]
    public class MasterMindGroup : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("new-game", "Start a game of Master Mind (rules will be sent upon use of this command).")]
        public async Task NewGame(MasterMindMethods.GameMode mode = MasterMindMethods.GameMode.Classic)
        {
            if (MasterMindMethods.CurrentGames != null && MasterMindMethods.GetGame(Context.Channel.Id) != null)
            {
                await RespondAsync(text: "❌ Only one game of Master Mind can be played per channel at a time.", ephemeral: true);
            }
            else // Display Rules / Initial Embed
            {
                MasterMindGame game = new(Context.Channel.Id, Context.User, mode);
                MasterMindMethods.CurrentGames.Add(game);

                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = MasterMindMethods.DefaultColor,
                };
                embed.AddField(name: "How to Play.", value: MasterMindMethods.GetRules(mode), inline: false);

                await RespondAsync(embed: embed.Build(), components: MasterMindMethods.CreateDifficultySelectMenu());
            }
        }

        [SlashCommand("guess", "make a guess in an existing game of Master Mind")]
        public async Task Guess([Summary("color1", "The first color in your guess.")] MasterMindMethods.Color color1, [Summary("color2", "The second color in your guess.")] MasterMindMethods.Color color2, [Summary("color3", "The third color in your guess.")] MasterMindMethods.Color color3, [Summary("color4", "The fourth color in your guess.")] MasterMindMethods.Color color4)
        {
            var game = MasterMindMethods.GetGame(Context.Channel.Id);
            
            if (game == null)
            {
                await RespondAsync(text: $"❌ There is currently not a game of Master Mind in this channel. To make one use {Help.GetCommandMention("mastermind new-game")}", ephemeral: true);
            }
            else if (MasterMindMethods.CurrentGames.Count > 0 && game.IsStarted == false)
            {
                await RespondAsync(text: "❌ Select your difficulty to start guessing.", ephemeral: true);
            }
            else
            {
                // Set Values
                game.GuessesLeft -= 1;
                var guess = new[] { color1, color2, color3, color4 };
                game.Guesses.Add((game.GetResultString(guess), guess));

                // Edit Game Message Embed
                if (game.DoesGuessMatchKey(guess)) // it is solved
                {
                    await game.Message.ModifyAsync(x => { x.Embed = MasterMindMethods.CreateEmbed(game, true); x.Components = null; });
                    MasterMindMethods.CurrentGames.Remove(game);
                }
                else if (game.GuessesLeft <= 0) // lose game
                {
                    await game.Message.ModifyAsync(x => { x.Embed = MasterMindMethods.CreateEmbed(game); x.Components = null; });
                    MasterMindMethods.CurrentGames.Remove(game);
                }
                else
                {
                    await game.Message.ModifyAsync(x => { x.Embed = MasterMindMethods.CreateEmbed(game); });
                }

                // Respond
                await RespondAsync(text: "🎯 Guess Made.", ephemeral: true);
            }
        }

        [ComponentInteraction("mastermind-difficulty", ignoreGroupNames: true)]
        public async Task MasterMindBeginButtonHandler()
        {
            await DeferAsync();
            // Get Game
            var game = MasterMindMethods.GetGame(Context.Interaction.Channel.Id);

            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            if (game == null)
            {
                await component.Message.ModifyAsync(x => { x.Components = MasterMindMethods.CreateDifficultySelectMenu(true); });
                await FollowupAsync(text: $"❌ This game does not exist any more.\n- To make a new one use {Help.GetCommandMention("mastermind new-game")}.", ephemeral: true);
                return;
            }

            // Set message
            game.Message = component.Message;

            // Initialize Guesses Left
            game.GuessesLeft = int.Parse(component.Data.Values.FirstOrDefault());

            // Initialize Key
            game.Key = MasterMindMethods.CreateKey();

            // Initialize Embed  
            var embed = new EmbedBuilder
            {
                Title = "🧠 Master Mind",
                Color = MasterMindMethods.DefaultColor,
                Description = $"Make your first guess with {Help.GetCommandMention("mastermind guess")}.",
            };
            embed.AddField(name: "Guesses Left:", value: $"`{game.GuessesLeft}`", inline: true);

            // Begin Game
            game.IsStarted = true;

            // Edit Message For Beginning of Game.
            await component.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = MasterMindMethods.GetForfeitButton(); });
        }

        [ComponentInteraction("quit", ignoreGroupNames: true)]
        public async Task MasterMindQuitButtonHandler()
        {
            await DeferAsync();

            // Get Game
            var game = MasterMindMethods.GetGame(Context.Interaction.Channel.Id);

            if (game == null)
            {
                SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;
                await component.Message.ModifyAsync(x => { x.Components = MasterMindMethods.GetForfeitButton(true); });
                await FollowupAsync(text: $"❌ This game does not exist any more.\n- To make a new one use {Help.GetCommandMention("mastermind new-game")}.", ephemeral: true);
                return;
            }

            if (game.StartUser.Id == Context.Interaction.User.Id)
            {
                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new(15548997),
                    Description = $"This was certainly difficult, try again with {Help.GetCommandMention("mastermind new-game")}",
                };

                embed.Title += " (forfeited)";
                embed.AddField(name: "Answer:", value: MasterMindMethods.GetColorsString(game.Key));
                await game.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = MasterMindMethods.GetForfeitButton(true); });
                MasterMindMethods.CurrentGames.Remove(game);
            }
            else
            {
                await FollowupAsync(text: $"❌ **Only** {game.StartUser.Mention} can forfeit this game of Master Mind.\n- Only the user who started the game of Mastermind can forfeit.", ephemeral: true);
            }
        }
    }
}
