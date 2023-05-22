using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class RockPaperScissors
{

    public static SelectMenuBuilder RPSOptions = new SelectMenuBuilder()
    .WithPlaceholder("Select an option")
    .WithCustomId("RPSOptions")
    .WithMaxValues(1)
    .WithMinValues(1)
    .AddOption("🪨 Rock", "0")
    .AddOption("📃 Paper", "1")
    .AddOption("✂️ Scissors", "2");

    public static async Task RPSButtonHandler(SocketMessageComponent component) {
        switch(component.Data.CustomId) {
            case "RPS":
                await component.ModifyOriginalResponseAsync((Discord.MessageProperties props) => {props.Content = $"";});
            break;
        }
    }

    public static async Task RPSSelectMenuHandler(SocketMessageComponent component)
    {
        string result = PlayRPS(string.Join("", component.Data.Values));
        await component.UpdateAsync(x => {x.Content = result + component.User.Mention;});
        Bot.Client.SelectMenuExecuted -= RockPaperScissors.RPSSelectMenuHandler;
    }

    public static string PlayRPS(string userChoice)
    {
        string[] options = { "🪨", "📃", "✂️"};
        Random random = new Random();
        string botOption = options[random.Next(0, RPSOptions.Options.Count)];

        string userOption = options[Int32.Parse(userChoice)];
        string resultMeaning = "";

        if (userOption == botOption)
        {
            resultMeaning = "*That's a draw!* Let's play again!";
            return $"{userOption} **VS** {botOption} " + resultMeaning;
        }

        if ((userOption == "0" && botOption == "📃") || (userOption == "📃" && botOption == "✂️") || (userOption == "✂️" && botOption == "🪨"))
        {
            resultMeaning = "*I win!* Let's play again!";
        }else {
            resultMeaning = "*You beat me!* Let's play again!";
        }

        return $"{userOption} **VS** {botOption} " + resultMeaning + " ";
    }
}