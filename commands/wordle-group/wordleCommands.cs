using System;
using System.Threading.Tasks;
using Bob.Challenges;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("wordle", "All commands relevant to the game Wordle.")]
    public class WordleGroup : InteractionModuleBase<ShardedInteractionContext>
    {
        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        [SlashCommand("new-game", "Start a new game of Wordle.")]
        public async Task NewGame()
        {
            if (Challenge.WordleGames != null && Challenge.WordleGames.ContainsKey(Context.Channel.Id))
            {
                await RespondAsync(text: "❌ Only one game of Wordle can be played per channel at a time.", ephemeral: true);
            }
            else
            {
                await DeferAsync();
                Wordle game = new(Context.User, null);
                await game.StartBotGame(Context.Interaction);
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        [SlashCommand("guess", "Start a new game of Wordle.")]
        public async Task Guess([Summary("word", "The word you want to guess.")][MinLength(5)][MaxLength(5)] string guess)
        {
            Challenge.WordleGames.TryGetValue(Convert.ToUInt64(Context.Channel.Id), out Wordle game);

            if (game == null)
            {
                await RespondAsync(text: $"❌ No game of Wordle is currently active in this channel.\n- Use {Help.GetCommandMention("wordle new-game")} to begin one.", ephemeral: true);
                return;
            }

            if (game.Player1.Id != Context.User.Id)
            {
                await RespondAsync(text: "❌ You are not the player in this game.", ephemeral: true);
                return;
            }

            if (!WordleMethods.IsValidGuess(guess))
            {
                await RespondAsync(text: "❌ The word you guessed is not valid.", ephemeral: true);
                return;
            }

            game.GuessesLeft--;

            game.Guesses.Add((WordleMethods.GetResult(game.Word, guess), guess));

            if (game.Word == guess)
            {
                await game.FinishGame(false);
            }
            else if (game.GuessesLeft <= 0) // lose game
            {
                await game.FinishGame(true);
            }
            else
            {
                await game.Message.ModifyAsync(x => { x.Embed = WordleMethods.CreateEmbed(game); });
            }

            await RespondAsync(text: "🎯 Guess Made.", ephemeral: true);
        }
    }
}