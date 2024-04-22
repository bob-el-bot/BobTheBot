using System.Threading.Tasks;
using Discord.Interactions;
using SimpleCiphers;
using Commands.Helpers;
using Discord;
using System.Linq;
using System;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("decrypt", "All commands relevant to decryption.")]
    public class DecryptGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("atbash", "Bob will decrypt your message by swapping letters to their opposite position.")]
        public async Task Atbash([Summary("message", "the text you want to decrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Atbash(message));
        }

        [SlashCommand("caesar", "Bob will decrypt your message by shifting the letters the specified amount.")]
        public async Task Caesar([Summary("message", "the text you want to decrypt")] string message, [Summary("shift", "the amount of letters to shift by")] int shift)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Caesar(message, (uint)Math.Abs(shift)));
        }

        [SlashCommand("a1z26", "Bob will decrypt your message by swapping letters to their corresponding number.")]
        public async Task A1Z26([Summary("message", "the text you want to decrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.A1Z26(message));
        }

        [SlashCommand("morse", "Bob will decrypt your message using Morse code.")]
        public async Task Morse([Summary("message", "the text you want to decrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Morse(message));
        }

        [SlashCommand("vigenere", "Bob will decrypt your message using a specified key.")]
        public async Task Vigenere([Summary("message", "the text you want to decrypt")] string message, [Summary("key", "the key for the decryption")] string key)
        {
            await DeferAsync(ephemeral: true);

            if (key.Any(char.IsDigit))
            {
                await FollowupAsync(text: "❌ The message *cannot* be decrypted because the key contains numbers.", ephemeral: true);
            }
            else
            {
                await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Vigenere(message, key));
            }
        }
    }
}