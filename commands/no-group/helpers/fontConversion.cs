using System.Text;
using Discord.Interactions;

namespace Commands.Helpers
{
    public class FontConversion
    {
        public enum FontTypes
        {
            [ChoiceDisplay("𝖒𝖊𝖉𝖎𝖊𝖛𝖆𝖑")]
            medieval,
            [ChoiceDisplay("𝓯𝓪𝓷𝓬𝔂")]
            fancy,
            [ChoiceDisplay("s̷l̷̷a̷s̷h̷e̷d̷")]
            slashed,
            [ChoiceDisplay("𝕠𝕦𝕥𝕝𝕚𝕟𝕖𝕕")]
            outlined,
            [ChoiceDisplay("ɟןıddǝp")]
            flipped,
            [ChoiceDisplay("🄱🄾🅇🄴🄳")]
            boxed
        }

        private static readonly string alpha = "abcdefghijklmnopqrstuvwxyz";
        private static readonly string ALPHA = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Medieval(string text)
        {
            string[] lowerMedievalAlpha = ["𝖆", "𝖇", "𝖈", "𝖉", "𝖊", "𝖋", "𝖌", "𝖍", "𝖎", "𝖏", "𝖐", "𝖑", "𝖒", "𝖓", "𝖔", "𝖕", "𝖖", "𝖗", "𝖘", "𝖙", "𝖚", "𝖛", "𝖜", "𝖝", "𝖞", "𝖟"];
            string[] upperMedievalAlpha = ["𝕬", "𝕭", "𝕮", "𝕯", "𝕰", "𝕱", "𝕲", "𝕳", "𝕴", "𝕵", "𝕶", "𝕷", "𝕸", "𝕹", "𝕺", "𝕻", "𝕼", "𝕽", "𝕾", "𝕿", "𝖀", "𝖁", "𝖂", "𝖃", "𝖄", "𝖅"];

            return TextToFont(upperMedievalAlpha, lowerMedievalAlpha, text);
        }

        public static string Fancy(string text)
        {
            string[] lowerFancyAlpha = ["𝓪", "𝓫", "𝓬", "𝓭", "𝓮", "𝓯", "𝓰", "𝓱", "𝓲", "𝓳", "𝓴", "𝓵", "𝓶", "𝓷", "𝓸", "𝓹", "𝓺", "𝓻", "𝓼", "𝓽", "𝓾", "𝓿", "𝔀", "𝔁", "𝔂", "𝔃"];
            string[] upperFancyAlpha = ["𝓐", "𝓑", "𝓒", "𝓓", "𝓔", "𝓕", "𝓖", "𝓗", "𝓘", "𝓙", "𝓚", "𝓛", "𝓜", "𝓝", "𝓞", "𝓟", "𝓠", "𝓡", "𝓢", "𝓣", "𝓤", "𝓥", "𝓦", "𝓧", "𝓨", "𝓩"];
            return TextToFont(upperFancyAlpha, lowerFancyAlpha, text);
        }

        public static string Slashed(string text)
        {
            string[] lowerSlashedAlpha = ["̷a̷", "b̷", "c̷", "d̷", "e̷", "f̷", "g̷", "h̷", "i̷", "j̷", "k̷", "l̷", "m̷", "n̷", "o̷", "p̷", "q̷", "r̷", "s̷", "t̷", "u̷", "v̷", "w̷", "x̷", "y̷", " ̷z̷"];
            string[] upperSlashedAlpha = ["A̷", "B̷", "C̷", "D̷", "E̷", "F̷", "G̷", "H̷", "I̷", "J̷", "K̷", "L̷", "M̷", "N̷", "O̷", "P̷", "Q̷", "R̷", "S̷", "T̷", "U̷", "V̷", "W̷", "X̷", "Y̷", "Z̷"];

            return TextToFont(upperSlashedAlpha, lowerSlashedAlpha, text);
        }

        public static string Outlined(string text)
        {
            string[] upperOutLineAlpha = ["𝔸", "𝔹", "ℂ", "𝔻", "𝔼", "𝔽", "𝔾", "ℍ", "𝕀", "𝕁", "𝕂", "𝕃", "𝕄", "ℕ", "𝕆", "ℙ", "ℚ", "ℝ", "𝕊", "𝕋", "𝕌", "𝕍", "𝕎", "𝕏", "𝕐", "ℤ"];
            string[] lowerOutLineAlpha = ["𝕒", "𝕓", "𝕔", "𝕕", "𝕖", "𝕗", "𝕘", "𝕙", "𝕚", "𝕛", "𝕜", "𝕝", "𝕞", "𝕟", "𝕠", "𝕡", "𝕢", "𝕣", "𝕤", "𝕥", "𝕦", "𝕧", "𝕨", "𝕩", "𝕪", "𝕫"];

            return TextToFont(upperOutLineAlpha, lowerOutLineAlpha, text);
        }

        public static string Flipped(string text)
        {
            string[] upperOutLineAlpha = ["∀", "ᗺ", "Ɔ", "ᗡ", "Ǝ", "Ⅎ", "⅁", "H", "I", "ſ", "ꓘ", "˥", "W", "N", "O", "Ԁ", "ტ", "ᴚ", "S", "⊥", "∩", "Λ", "M", "X", "⅄", "Z"];
            string[] lowerOutLineAlpha = ["ɐ", "q", "ɔ", "p", "ǝ", "ɟ", "ƃ", "ɥ", "ı", "ɾ", "ʞ", "ן", "ɯ", "u", "o", "d", "b", "ɹ", "s", "ʇ", "n", "ʌ", "ʍ", "x", "ʎ", "z"];

            return TextToFont(upperOutLineAlpha, lowerOutLineAlpha, text);
        }

        public static string Boxed(string text)
        {
            string[] boxedAlpha = ["🄰", "🄱", "🄲", "🄳", "🄴", "🄵", "🄶", "🄷", "🄸", "🄹", "🄺", "🄻", "🄼", "🄽", "🄾", "🄿", "🅀", "🅁", "🅂", "🅃", "🅄", "🅅", "🅆", "🅇", "🅈", "🅉"];

            return TextToFont(boxedAlpha, boxedAlpha, text);
        }

        public static string TextToFont(string[] FONT, string[] font, string text)
        {
            StringBuilder finalText = new();

            foreach (char letter in text)
            {
                if (alpha.Contains(letter))
                {
                    int letterIndex = alpha.IndexOf(letter);
                    finalText.Append(font[letterIndex]);
                }
                else if (ALPHA.Contains(letter))
                {
                    int letterIndex = ALPHA.IndexOf(letter);
                    finalText.Append(FONT[letterIndex]);
                }
                else
                {
                    finalText.Append(letter);
                }
            }
            return finalText.ToString();
        }
    }
}