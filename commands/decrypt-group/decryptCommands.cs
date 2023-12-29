using System.Threading.Tasks;
using Discord.Interactions;
using SimpleCiphers;
using Commands.Helpers;

namespace Commands
{
    [EnabledInDm(true)]
    [Group("decrypt", "All commands relevant to decryption.")]
    public class DecryptGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(true)]
        [SlashCommand("atbash", "Bob will decrypt your message by swapping letters to their opposite position.")]
        public async Task Atbash([Summary("message", "the text you want to decrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Atbash(message));
        }

        [EnabledInDm(true)]
        [SlashCommand("caesar", "Bob will decrypt your message by shifting the letters the specified amount.")]
        public async Task Caesar([Summary("message", "the text you want to decrypt")] string message, [Summary("shift", "the amount of letters to shift by")] int shift)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Caesar(message, (uint)shift));
        }

        [EnabledInDm(true)]
        [SlashCommand("a1z26", "Bob will decrypt your message by swapping letters to their corresponding number.")]
        public async Task A1Z26([Summary("message", "the text you want to decrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.A1Z26(message));
        }
        
        [EnabledInDm(true)]
        [SlashCommand("morse", "Bob will decrypt your message using Morse code.")]
        public async Task Morse([Summary("message", "the text you want to decrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Morse(message));
        }

        [EnabledInDm(true)]
        [SlashCommand("vigenere", "Bob will decrypt your message using a specified key.")]
        public async Task Vigenere([Summary("message", "the text you want to decrypt")] string message, [Summary("key", "the key for the decryption")] string key)
        {
            await DeferAsync(ephemeral: true);
            await Decrypt.SendDecryptMessage(Context.Interaction, Decryption.Vigenere(message, key));
        }
    }
}