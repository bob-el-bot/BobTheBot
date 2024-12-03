using System.Threading.Tasks;
using System.Linq;
using Commands.Helpers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("mastermind", "All commands relevant to the game Master Mind.")]
    public class MasterMindGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("new-game", "Start a game of Master Mind (rules will be sent upon use of this command).")]
        public async Task NewGame()
        {
            if (MasterMindMethods.CurrentGames != null && MasterMindMethods.GetGame(Context.Channel.Id) != null)
            {
                await RespondAsync(text: "❌ Only one game of Master Mind can be played per channel at a time.", ephemeral: true);
            }
            else // Display Rules / Initial Embed
            {
                MasterMindGame game = new(Context.Channel.Id, Context.User);
                MasterMindMethods.CurrentGames.Add(game);

                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = MasterMindMethods.DefaultColor,
                };
                embed.AddField(name: "How to Play.", value: @"
The goal of the game is to guess the correct randomly generated code. Each code consists of 4 colors, chosen from 6 possible colors (duplicates are allowed). Use the command `/mastermind guess` to make your guess. 
After each guess you will be given feedback on how close you are to the correct code. The feedback is as follows:
- ⬛ = Color is in the correct position.
- ⬜ = Color is in the wrong position.
- 🔳 = Color is not in the code.

You can pick a difficulty level:

- Easy: 10 tries.
- Medium: 8 tries.
- Hard: 6 tries.

Good luck cracking the code!");

                await RespondAsync(embed: embed.Build(), components: MasterMindMethods.CreateDifficultySelectMenu());
            }
        }

        [SlashCommand("guess", "make a guess in an existing game of Master Mind")]
        public async Task Guess([Summary("color1", "The first color in your guess.")] MasterMindMethods.Colors color1, [Summary("color2", "The second color in your guess.")] MasterMindMethods.Colors color2, [Summary("color3", "The third color in your guess.")] MasterMindMethods.Colors color3, [Summary("color4", "The fourth color in your guess.")] MasterMindMethods.Colors color4)
        {
            var game = MasterMindMethods.GetGame(Context.Channel.Id);
            
            if (game == null)
            {
                await RespondAsync(text: "❌ There is currently not a game of Master Mind in this channel. To make one use `/mastermind new-game`", ephemeral: true);
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

            // Set message
            var component = (SocketMessageComponent)Context.Interaction;
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
                Description = "Make your first guess with `/mastermind guess`.",
            };
            embed.AddField(name: "Guesses Left:", value: $"`{game.GuessesLeft}`", inline: true);

            // Forfeit Button
            var button = new ButtonBuilder
            {
                Label = "Forfeit",
                Style = ButtonStyle.Danger,
                CustomId = "quit"
            };
            var builder = new ComponentBuilder().WithButton(button);

            // Begin Game
            game.IsStarted = true;

            // Edit Message For Beginning of Game.
            await component.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = builder.Build(); });
        }

        [ComponentInteraction("quit", ignoreGroupNames: true)]
        public async Task MasterMindQuitButtonHandler()
        {
            await DeferAsync();

            // Get Game
            var game = MasterMindMethods.GetGame(Context.Interaction.Channel.Id);

            if (game.StartUser.Id == Context.Interaction.User.Id)
            {
                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new(15548997),
                    Description = "This was certainly difficult, try again with `/mastermind new-game`",
                };

                embed.Title += " (forfeited)";
                embed.AddField(name: "Answer:", value: MasterMindMethods.GetColorsString(game.Key));
                await game.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
                MasterMindMethods.CurrentGames.Remove(game);
            }
            else
            {
                await FollowupAsync(text: $"❌ **Only** {game.StartUser.Mention} can forfeit this game of Master Mind.\n- Only the user who started the game of Master Mind can forfeit.", ephemeral: true);
            }
        }
    }
}
