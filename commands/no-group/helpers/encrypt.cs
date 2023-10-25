using System.Text;
using Discord.Interactions;

namespace Commands.Helpers {
    public class Encryption
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

        private static readonly string alpha = "abcdefghijklmnopqrstuvwxyz";
        private static readonly string ALPHA = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string num = "0123456789";
        private static readonly (string, string)[] morseAlpha = { ("a", ".-"), ("b", "-..."), ("c", "-.-."), ("d", "-.."), ("e", "."), ("f", "..-."), ("g", "--."), ("h", "...."), ("i", ".."), ("j", ".---"), ("k", "-.-"), ("l", ".-.."), ("m", "--"), ("n", "-."), ("o", "---"), ("p", ".--."), ("q", "--.-"), ("r", ".-."), ("s", "..."), ("t", "-"), ("u", "..-"), ("v", "...-"), ("w", ".--"), ("x", "-..-"), ("y", "-.--"), ("z", "--..") };
        private static readonly (int, string)[] morseNum = { (0, "-----"), (1, ".----"), (2, "..---"), (3, "...--"), (4, "....-"), (5, "....."), (6, "-...."), (7, "--..."), (8, "---.."), (9, "----.") };
        private static readonly (char, string)[] morseSym = { ('.', ".-.-.-"), (',', "--..--"), ('\'', ".----."), ('?', "..--.."), ('!', "-.-.--"), ('/', "-..-."), ('(', "-.--."), (')', "-.--.-"), ('&', ".-..."), (':', "---..."), (';', "-.-.-."), ('=', "-...-"), ('+', ".-.-."), ('-', "-....-"), ('_', "..--.-"), ('"', ".-..-."), ('$', "...-..-"), ('@', ".--.-.") };
        private static readonly string morseAcceptedSym = ".,'?!/()&:;=+-_\"$@";

        public static string Atbash(string text)
        {
            StringBuilder finalText = new();

            foreach (char letter in text)
            {
                if (alpha.IndexOf(letter) > -1)
                {
                    int finalVal = 25 - alpha.IndexOf(letter);
                    finalText.Append(alpha[finalVal]);
                }
                else if (ALPHA.IndexOf(letter) > -1)
                {
                    int finalVal = 25 - ALPHA.IndexOf(letter);
                    finalText.Append(ALPHA[finalVal]);
                }
                else
                {
                    finalText.Append(letter);
                }
            }

            return $"🔒 {finalText}";
        }

        public static string Caesar(string text)
        {
            StringBuilder finalText = new();
            int shift = 3;

            foreach (char letter in text)
            {
                if (alpha.IndexOf(letter) > -1)
                {
                    if (alpha.IndexOf(letter) + shift <= 25)
                    {
                        finalText.Append(alpha[alpha.IndexOf(letter) + shift]);
                    }
                    else
                    {
                        finalText.Append(alpha[alpha.IndexOf(letter) + shift - 26]);
                    }
                }
                else if (ALPHA.IndexOf(letter) > -1)
                {
                    if (ALPHA.IndexOf(letter) + shift <= 25)
                    {
                        finalText.Append(ALPHA[ALPHA.IndexOf(letter) + shift]);
                    }
                    else
                    {
                        finalText.Append(ALPHA[ALPHA.IndexOf(letter) + shift - 26]);
                    }
                }
                else
                {
                    finalText.Append(letter);
                }
            }
            return $"🔒 {finalText}";
        }

        public static string A1Z26(string text)
        {
            StringBuilder finalText = new();

            int place = 0;
            foreach (char letter in text)
            {
                if (alpha.IndexOf(letter) > -1 || ALPHA.IndexOf(letter) > -1)
                {
                    int letterVal = (alpha.IndexOf(letter) > -1) ? alpha.IndexOf(letter) + 1 : ALPHA.IndexOf(letter) + 1;
                    finalText.Append(letterVal);
                    if (text.Length > place + 1)
                    {
                        if (alpha.IndexOf(text[place + 1]) > -1 || ALPHA.IndexOf(text[place + 1]) > -1)
                        {
                            finalText.Append('-');
                        }
                    }
                    place += 1;
                }
                else
                {
                    place += 1;
                    finalText.Append(letter);
                }
            }

            return $"🔒 {finalText}";
        }

        public static string Morse(string text)
        {
            string lowerText = text.ToLower();
            StringBuilder finalText = new();

            foreach (char character in lowerText)
            {
                if (alpha.IndexOf(character) > -1) // alphabet
                {
                    for (int i = 0; i < morseAlpha.Length; i++)
                    {
                        if (character.ToString() == morseAlpha[i].Item1)
                        {
                            finalText.Append(" " + morseAlpha[i].Item2);
                        }
                    }
                }
                else if (num.IndexOf(character) > -1) //numbers
                {
                    for (int i = 0; i < morseNum.Length; i++)
                    {
                        if (character == morseNum[i].Item1)
                        {
                            finalText.Append(" " + morseNum[i].Item2);
                        }
                    }
                }
                else if (morseAcceptedSym.IndexOf(character) > -1) // symbols
                {
                    for (int i = 0; i < morseSym.Length; i++)
                    {
                        if (character == morseSym[i].Item1)
                        {
                            finalText.Append(" " + morseSym[i].Item2);
                        }
                    }
                }
                else if (character == ' ') // spaces
                {
                    finalText.Append(" / ");
                }
                else // other
                {
                    finalText.Append(" " + character);
                }
            }

            return $"🔒{finalText}";
        }
    }
}