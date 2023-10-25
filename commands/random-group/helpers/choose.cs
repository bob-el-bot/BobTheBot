using System;
using System.Collections.Generic;

namespace Commands.Helpers
{
    public static class Choose
    {
        public static List<string> TestAdd(string option, List<string> choices)
        {
            if (option != "")
            {
                choices.Add(option);
            }
            return choices;
        }

        public static string GetRandomDecisionText()
        {
            string[] responses = { "Hmm... *this* one: ", "*Definitely* this: ", "No... no... maybe... *oooo*: ", "This one caught my eye: " };

            Random random = new();

            return responses[random.Next(0, responses.Length)];
        }
    }
}