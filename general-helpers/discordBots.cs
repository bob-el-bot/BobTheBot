using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class DiscordBots
{
    public async Task PostStats()
    {
        // Formulate Request
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.bots.gg/api/v1/bots/705680059809398804/stats");

        var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/Quantam-Studios/BobTheBot)");
        var acceptValue = new MediaTypeWithQualityHeaderValue("application/json");
        var authenticationValue = new AuthenticationHeaderValue($"{Config.GetDiscordBotsToken()}");
        request.Headers.UserAgent.Add(productValue);
        request.Headers.UserAgent.Add(commentValue);
        request.Headers.Accept.Add(acceptValue);
        request.Headers.Authorization = authenticationValue;
        request.Content = new StringContent("{\"guildCount\":" + Bot.Client.Guilds.Count.ToString() + "}", System.Text.Encoding.UTF8, "application/json");

        // POST
        var req = await httpClient.SendAsync(request);
        Console.WriteLine("Discord Bots GG POST status: " + req.StatusCode);
    }
}