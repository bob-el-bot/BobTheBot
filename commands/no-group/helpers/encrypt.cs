using System.Text;
using Discord.Interactions;

namespace Commands.Helpers {
    public class Ciphers
    {
        public enum CipherTypes
        {
            [ChoiceDisplay("Atbash")]
            Atbash,
            [ChoiceDisplay("Caesar")]
            Caesar,
            [ChoiceDisplay("A1-Z26")]
            A1Z26,
            [ChoiceDisplay("Morse Code")]
            Morse
        }
    }
}