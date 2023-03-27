public class Encryption
{
    public enum CipherTypes
    {
        Atbash,
        Caesar,
        A1Z26
    }

    public static string alpha = "abcdefghijklmnopqrstuvwxyz";

    public static string Atbash(string text)
    {
        string finalText = "";

        foreach (char letter in text)
        {
            if (alpha.IndexOf(letter) > -1)
            {
                int finalVal = 25 - alpha.IndexOf(letter);
                finalText += alpha[finalVal];
            }
            else
            {
                finalText += letter;
            }
        }

        return $"🔒 {finalText}";
    }

    public static string Caesar(string text)
    {
        string finalText = "";
        int shift = 3;

        foreach (char letter in text)
        {
            if (alpha.IndexOf(letter) > -1)
            {
                if (alpha.IndexOf(letter) + shift <= 25)
                    finalText += alpha[alpha.IndexOf(letter) + shift];
                else
                    finalText += alpha[alpha.IndexOf(letter) + shift - 26];
            }
            else
                finalText += letter;
        }
        return $"🔒 {finalText}";
    }

    public static string A1Z26(string text)
    {
        string finalText = "";

        int place = 0;
        foreach (char letter in text)
        {
            if (alpha.IndexOf(letter) > -1)
            {
                int letterVal = alpha.IndexOf(letter) + 1;
                finalText += letterVal.ToString();
                if (text.Length > place + 1)
                {
                    if (alpha.IndexOf(text[place + 1]) > -1)
                    {
                        finalText += "-";
                    }
                }
                place += 1;
            }
            else
            {
                place += 1;
                finalText += letter;
            }
        }

        return $"🔒 {finalText}";
    }
}