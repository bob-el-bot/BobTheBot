public class FontConversion
{
    public enum FontTypes
    {
        medieval,
        fancy,
        slashed,
        flip,
        boxed
    }

    public static string alpha = "abcdefghijklmnopqrstuvwxyz";

    public static string Medieval(string text)
    {
        string[] fancyAlpha = { "𝖆", "𝖇", "𝖈", "𝖉", "𝖊", "𝖋", "𝖌", "𝖍", "𝖎", "𝖏", "𝖐", "𝖑", "𝖒", "𝖓", "𝖔", "𝖕", "𝖖", "𝖗", "𝖘", "𝖙", "𝖚", "𝖛", "𝖜", "𝖝", "𝖞", "𝖟" };

        return TextToFont(fancyAlpha, text);
    }

    public static string Fancy(string text)
    {
        string[] fancyAlpha = { "𝓪", "𝓫", "𝓬", "𝓭", "𝓮", "𝓯", "𝓰", "𝓱", "𝓲", "𝓳", "𝓴", "𝓵", "𝓶", "𝓷", "𝓸", "𝓹", "𝓺", "𝓻", "𝓼", "𝓽", "𝓾", "𝓿", "𝔀", "𝔁", "𝔂", "𝔃" };

        return TextToFont(fancyAlpha, text);
    }

    public static string Slashed(string text)
    {
        string[] slashedAlpha = { "̷a̷", "b̷", "c̷", "d̷", "e̷", "f̷", "g̷", "h̷", "i̷", "j̷", "k̷", "l̷", "m̷", "n̷", "o̷", "p̷", "q̷", "r̷", "s̷", "t̷", "u̷", "v̷", "w̷", "x̷", "y̷", " ̷z̷" };

        return TextToFont(slashedAlpha, text);
    }

    public static string Flip(string text)
    {
        string[] flipsAlpha = { "z", "ʎ", "x", "ʍ", "ʌ", "u", "ʇ", "s", "ɹ", "q", "p", "o", "u", "ɯ", "l", "ʞ", "ɾ", "ı", "ɥ", "ɓ", "ɟ", "ǝ", "p", "ɔ", "q", "ɐ" };

        return TextToFont(flipsAlpha, text);
    }

    public static string Boxed(string text)
    {
        string[] boxedAlpha = { "🄰", "🄱", "🄲", "🄳", "🄴", "🄵", "🄶", "🄷", "🄸", "🄹", "🄺", "🄻", "🄼", "🄽", "🄾", "🄿", "🅀", "🅁", "🅂", "🅃", "🅄", "🅅", "🅆", "🅇", "🅈", "🅉" };

        return TextToFont(boxedAlpha, text);
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