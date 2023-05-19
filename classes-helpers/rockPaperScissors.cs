using System;
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
    .AddOption("🪨 Rock", "🪨")
    .AddOption("📃 Paper", "📃")
    .AddOption("✂️ Scissors", "✂️");

    public static async Task RPSButtonHandler(SocketMessageComponent component) {
        switch(component.Data.CustomId) {
            case "RPS":

                await component.ModifyOriginalResponseAsync((Discord.MessageProperties props) => {props.Content = $"";});
            break;
        }
    }

    public static async Task RPSSelectMenuHandler(SocketMessageComponent component)
    {
        string result = PlayRPS(component.Data.Value);
        await component.ModifyOriginalResponseAsync((Discord.MessageProperties props) => {props.Content = result;});
    }

    public static string PlayRPS(string userOption)
    {
        string[] options = { "🪨", "📃", "✂️"};
        Random random = new Random();
        string botOption = options[random.Next(0, RPSOptions.Options.Count)];


        string resultMeaning = "";

        if (userOption == botOption)
        {
            resultMeaning = "*That's a draw!* Let's play again!";
        }

        if ((userOption == "🪨" && botOption == "📃") || (userOption == "📃" && botOption == "✂️") || (userOption == "✂️" && botOption == "🪨"))
        {
            resultMeaning = "*I win!* Let's play again!";
        }else {
            resultMeaning = "*You beat me!* Let's play again!";
        }

        return $"{userOption} **VS** {botOption} " + resultMeaning;
    }
}