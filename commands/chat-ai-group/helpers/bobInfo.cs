
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ApiInteractions;
using Commands.Helpers;
using Newtonsoft.Json;

public static class BobInfoService
{
    public class BobInfo
    {
        public string Name { get; set; } = "Bob";
        public string FavoriteColor { get; set; } = "purple, specifically (#8D52FD)";
        public DateTime CreationDate { get; set; } = new DateTime(2020, 5, 1);
        public string Creator { get; set; } = "Zach Goodson";
        public string Website { get; set; } = "https://bobthebot.net";
        public string DocsPage { get; set; } = "https://docs.bobthebot.net";
        public string DiscordUsername { get; set; } = "@vorlon.";
        public string CommunityServer { get; set; } = "https://discord.gg/HvGMRZD8jQ";
        public string PersonalWebsite { get; set; } = "https://quantamstudios.dev";
    }

    private static readonly string ChromaUrl = Environment.GetEnvironmentVariable("CHROMA_URL");

    public static async Task StoreBobInfo()
    {
        var bobInfo = new BobInfo();

        // Serialize the Bob info to a message to send to Chroma
        var bobInfoMessage = $"Bob's Name: {bobInfo.Name}\nFavorite Color: {bobInfo.FavoriteColor}\nCreated on: {bobInfo.CreationDate.ToShortDateString()}\nCreator: {bobInfo.Creator}\nWebsite: {bobInfo.Website}\nDocs: {bobInfo.DocsPage}";

        // Convert the message to an embedding
        var embedding = await GetEmbedding(bobInfoMessage);

        var body = new
        {
            embeddings = embedding,
            metadata = new { userId = "Bob", message = bobInfoMessage, timestamp = DateTime.UtcNow.ToString() }
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await Interface.Client.PostAsync($"{ChromaUrl}/add", content);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<float[]> GetEmbedding(string message)
    {
        // Get the embedding from OpenAI API or another embedding model
        return await OpenAI.GetEmbedding(message);
    }
}

