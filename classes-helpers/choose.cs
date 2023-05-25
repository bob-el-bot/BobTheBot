using System;
using System.Collections.Generic;

public class Choose
{
    public static List<string> TestAdd(string option, List<string> choices)
    {
        if (option != "") choices.Add(option);
        return choices;
    }

    public static string GetRandomDecisionText()
    {
        string[] responses = { "Hmm... *this* one: ", "Definitely this: ", "No... no... maybe... *ooo0*: ", "This one caught my eye: " };

        var random = new Random();
        
        return responses[random.Next(0, responses.Length)];
    }
}