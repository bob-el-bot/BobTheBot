public class FontConversion
{
    public enum FontTypes
    {
        fancy,
        slashed,
        flip
    }

    public static string alpha = "abcdefghijklmnopqrstuvwxyz";

    public static string Fancy(string text)
    {
        string[] fancyAlpha = { "𝖆", "𝖇", "𝖈", "𝖉", "𝖊", "𝖋", "𝖌", "𝖍", "𝖎", "𝖏", "𝖐", "𝖑", "𝖒", "𝖓", "𝖔", "𝖕", "𝖖", "𝖗", "𝖘", "𝖙", "𝖚", "𝖛", "𝖜", "𝖝", "𝖞", "𝖟" };

        return TextToFont(fancyAlpha, text);
    }

    public static string Slashed(string text)
    {
        string[] slashedAlpha = { "̷a̷", "b̷", "c̷", "d̷", "e̷", "f̷", "g̷", "h̷", "i̷", "j̷", "k̷", "l̷", "m̷", "n̷", "o̷", "p̷", "q̷", "r̷", "s̷", "t̷", "u̷", "v̷", "w̷", "x̷", "y̷", "z" };

        return TextToFont(slashedAlpha, text);
    }

    public static string Flip(string text)
    {
        string[] flipsAlpha = { "z", "ʎ", "x", "ʍ", "ʌ", "u", "ʇ", "s", "ɹ", "q", "p", "o", "u", "ɯ", "l", "ʞ", "ɾ", "ı", "ɥ", "ɓ", "ɟ", "ǝ", "p", "ɔ", "q", "ɐ" };

        return TextToFont(flipsAlpha, text);
    }

    public static string TextToFont(string[] font, string text)
    {
        string finalText = "";

        foreach (char letter in text)
        {
            if (alpha.Contains(letter))
            {
                int letterIndex = alpha.IndexOf(letter);
                finalText += font[letterIndex];
            }
            else
                finalText += letter;
        }

        return finalText;
    }

}