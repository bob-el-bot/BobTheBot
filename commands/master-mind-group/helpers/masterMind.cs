using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

public class MasterMindGeneral
{
    public static List<MasterMindGame> currentGames = new();
   
    public static string GetCongrats()
    {
        Random random = new();
        string[] congrats = { "*You* did it!",  "*You* solved it!", "Great job! *you* beat it!" };
        return congrats[random.Next(0, congrats.Length)];
    }

    public static string CreateKey()
    {
        Random random = new();
        const string digits = "123456789";
        string key = digits[random.Next(0, digits.Length)].ToString() + digits[random.Next(0, digits.Length)] + digits[random.Next(0, digits.Length)] + digits[random.Next(0, digits.Length)];
        return key;
    }

    public static string GetResult(string guess, string key)
    {
        string result = "";
        for (int i = 0; i < guess.Length; i++)
        {
            if (guess[i] == key[i])
                result += "🟩";
            else
                result += "🟥";
        }

        return result;
    }
}