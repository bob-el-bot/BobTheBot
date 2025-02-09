using System.Threading.Tasks;
using Discord.Interactions;
using SimpleCiphers;
using Bob.Commands.Helpers;
using Discord;
using System.Linq;
using System;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("encrypt", "All commands relevant to encryption.")]
    public class EncryptGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("atbash", "Bob will encrypt your message by swapping letters to their opposite position.")]
        public async Task Atbash([Summary("message", "the text you want to encrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Encrypt.SendEncryptMessage(Context.Interaction, Encryption.Atbash(message));
        }

        [SlashCommand("caesar", "Bob will encrypt your message by shifting the letters the specified amount.")]
        public async Task Caesar([Summary("message", "the text you want to encrypt")] string message, [Summary("shift", "the amount of letters to shift by")] int shift)
        {
            await DeferAsync(ephemeral: true);
            await Encrypt.SendEncryptMessage(Context.Interaction, Encryption.Caesar(message, (uint)Math.Abs(shift)));
        }

        [SlashCommand("a1z26", "Bob will encrypt your message by swapping letters to their corresponding number.")]
        public async Task A1Z26([Summary("message", "the text you want to encrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Encrypt.SendEncryptMessage(Context.Interaction, Encryption.A1Z26(message));
        }

        [SlashCommand("morse", "Bob will encrypt your message using Morse code.")]
        public async Task Morse([Summary("message", "the text you want to encrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Encrypt.SendEncryptMessage(Context.Interaction, Encryption.Morse(message));
        }

        [SlashCommand("vigenere", "Bob will encrypt your message using a specified key.")]
        public async Task Vigenere([Summary("message", "the text you want to encrypt")] string message, [Summary("key", "the key for the encryption")] string key)
        {
            await DeferAsync(ephemeral: true);
            if (key.Any(char.IsLetter) == false)
            {
                await FollowupAsync(text: "❌ The message *cannot* be encrypted because the key contains non-alphabetic characters.", ephemeral: true);
            }
            else
            {
                await Encrypt.SendEncryptMessage(Context.Interaction, Encryption.Vigenere(message, key));
            }
        }

        [SlashCommand("binary", "Bob will encrypt your message by representing each character in binary.")]
        public async Task Binary([Summary("message", "the text you want to encrypt")] string message)
        {
            await DeferAsync(ephemeral: true);
            await Encrypt.SendEncryptMessage(Context.Interaction, Encryption.Binary(message));
        }
    }
}