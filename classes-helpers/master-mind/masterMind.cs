using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

public class MasterMindGeneral
{
    public static List<MasterMindGame> currentGames = new List<MasterMindGame>();
   
    public static string GetCongrats()
    {
        Random random = new Random();
        string[] congrats = { "*You* did it!",  "*You* solved it!", "Great job! *you* beat it!" };
        return congrats[random.Next(0, congrats.Length)];
    }

    public static async Task BeginGame(SocketMessageComponent component)
    {
        // Get Game
        var game = currentGames.Find(game => game.id == component.Channel.Id);
        
        // Set message
        game.message = component.Message;
        // Set startUser
        Console.WriteLine(component.User);
        game.startUser = component.User.Username;
        // Initialize Key
        game.key = CreateKey();

        // Initialize Embed  
        var embed = new Discord.EmbedBuilder
        {
            Title = "🧠 Master Mind",
            Color = new Discord.Color(6689298),
            Footer = new Discord.EmbedFooterBuilder
            {
                Text = "Game started by " + game.startUser
            }
        };
        embed.AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true).AddField(name: "Last Guess:", value: "use `/master-mind guess`", inline: true).AddField(name: "Result:", value: "use `/master-mind guess`");

        // Edit Message For Beginning of Game.
        await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = new Discord.ComponentBuilder().WithButton(label: "Forfeit", style: Discord.ButtonStyle.Danger, customId: "quit").Build(); });
        Bot.Client.ButtonExecuted -= BeginGame;
        Bot.Client.ButtonExecuted += QuitGame;

        // Begin Game
        game.isStarted = true;
    }

    public static string CreateKey()
    {
        Random random = new Random();
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

    public static async Task QuitGame(SocketMessageComponent component)
    {
        // Get Game
        var game = currentGames.Find(game => game.id == component.Channel.Id);

        var embed = new Discord.EmbedBuilder
        {
            Title = "🧠 Master Mind",
            Color = new Discord.Color(6689298),
            Description = "This was certainly difficult, try again with `/master-mind new-game`",
            Footer = new Discord.EmbedFooterBuilder
            {
                Text = "Game started by " + game.startUser
            }
        };
        embed.Title += " (forfeited)";
        embed.AddField(name: "Answer:", value: $"`{game.key}`");
        await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = null; });
        MasterMindGeneral.currentGames.Remove(game);
        Bot.Client.ButtonExecuted -= QuitGame;
    }
}