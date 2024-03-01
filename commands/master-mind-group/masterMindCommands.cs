using System.Threading.Tasks;
using Commands.Helpers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Commands
{
    [EnabledInDm(false)]
    [Group("master-mind", "All commands relevant to the game Master Mind.")]
    public class MasterMindGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(false)]
        [SlashCommand("new-game", "Start a game of Master Mind (rules will be sent upon use of this command).")]
        public async Task NewGame()
        {
            if (MasterMindGeneral.currentGames != null && MasterMindGeneral.currentGames.Find(game => game.id == Context.Channel.Id) != null)
            {
                await RespondAsync(text: "❌ Only one game of Master Mind can be played per channel at a time.", ephemeral: true);
            }
            else // Display Rules / Initial Embed
            {
                MasterMindGame game = new();
                MasterMindGeneral.currentGames.Add(game);
                game.id = Context.Channel.Id;
                game.startUser = Context.User;

                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new(16415395),
                };
                embed.AddField(name: "How to Play.", value: "The goal of the game is to guess the correct randomly generated code. **Each code is 4 digits** long where each digit is an integer from **1-9**. Use the command `/master-mind guess` to make your guess. Be warned you only have **8 tries**!");

                // Begin Button
                var button = new ButtonBuilder
                {
                    Label = "Begin Game!",
                    Style = ButtonStyle.Success,
                    CustomId = "begin"
                };

                var builder = new ComponentBuilder().WithButton(button);

                await RespondAsync(embed: embed.Build(), components: builder.Build());
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("guess", "make a guess in an existing game of Master Mind")]
        public async Task Guess([Summary("guess", "Type a 4 digit guess as to what the answer is.")] string guess)
        {
            var game = MasterMindGeneral.currentGames.Find(game => game.id == Context.Channel.Id);
            if (game == null)
            {
                await RespondAsync(text: "❌ There is currently not a game of Master Mind in this channel. To make one use `/master-mind new-game`", ephemeral: true);
            }
            else if (MasterMindGeneral.currentGames.Count > 0 && game.isStarted == false)
            {
                await RespondAsync(text: "❌ Press \"Begin Game!\" to start guessing.", ephemeral: true);
            }
            else if (guess.Length != 4)
            {
                await RespondAsync(text: "❌ Your should have **exactly 4 digits** (No guesses were expended).", ephemeral: true);
            }
            else
            {
                // Set Values
                game.guessesLeft -= 1;

                // Get Result
                string result = MasterMindGeneral.GetResult(guess, game.key);

                // Ready Embed
                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new(16415395),
                };

                if (result == "🟩🟩🟩🟩") // it is solved
                {
                    embed.Title += " (solved)";
                    embed.Color = new(5763719);
                    embed.Description = MasterMindGeneral.GetCongrats();
                    embed.AddField(name: "Answer:", value: $"`{game.key}`", inline: true).AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true);
                    await game.message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
                    MasterMindGeneral.currentGames.Remove(game);
                }
                else if (game.guessesLeft <= 0) // lose game
                {
                    embed.Title += " (lost)";
                    embed.Color = new(15548997);
                    embed.Description = "You have lost, but don't be sad you can just start a new game with `/master-mind new-game`";
                    embed.AddField(name: "Answer:", value: $"`{game.key}`");
                    await game.message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
                    MasterMindGeneral.currentGames.Remove(game);
                }
                else
                {
                    embed.AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true).AddField(name: "Last Guess:", value: $"`{guess}`", inline: true).AddField(name: "Result:", value: $"{result}");
                    await game.message.ModifyAsync(x => { x.Embed = embed.Build(); });
                }

                // Respond
                await RespondAsync(text: "🎯 Guess Made.", ephemeral: true);
            }
        }

        [ComponentInteraction("begin", ignoreGroupNames: true)]
        public async Task MasterMindBeginButtonHandler()
        {
            await DeferAsync();
            // Get Game
            var game = MasterMindGeneral.currentGames.Find(game => game.id == Context.Interaction.ChannelId);

            // Set message
            var component = (SocketMessageComponent)Context.Interaction;
            game.message = component.Message;

            // Initialize Key
            game.key = MasterMindGeneral.CreateKey();

            // Initialize Embed  
            var embed = new EmbedBuilder
            {
                Title = "🧠 Master Mind",
                Color = new(16415395),
            };
            embed.AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true).AddField(name: "Last Guess:", value: "use `/master-mind guess`", inline: true).AddField(name: "Result:", value: "use `/master-mind guess`");

            // Forfeit Button
            var button = new ButtonBuilder
            {
                Label = "Forfeit",
                Style = ButtonStyle.Danger,
                CustomId = "quit"
            };
            var builder = new ComponentBuilder().WithButton(button);

            // Begin Game
            game.isStarted = true;

            // Edit Message For Beginning of Game.
            await component.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = builder.Build(); });
        }

        [ComponentInteraction("quit", ignoreGroupNames: true)]
        public async Task MasterMindQuitButtonHandler()
        {
            await DeferAsync();

            // Get Game
            var game = MasterMindGeneral.currentGames.Find(game => game.id == Context.Interaction.Channel.Id);

            if (game.startUser.Id == Context.Interaction.User.Id)
            {
                var embed = new EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new(15548997),
                    Description = "This was certainly difficult, try again with `/master-mind new-game`",
                };

                embed.Title += " (forfeited)";
                embed.AddField(name: "Answer:", value: $"`{game.key}`");
                await game.message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
                MasterMindGeneral.currentGames.Remove(game);
            }
            else
            {
                await FollowupAsync(text: $"❌ **Only** {game.startUser.Mention} can forfeit this game of Master Mind.\n- Only the user who started the game of Master Mind can forfeit.", ephemeral: true);
            }
        }
    }
}
