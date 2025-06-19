
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

        // await ChromaService.CreateCollection("bobInfo");

        var facts = new[]
        {
        ("name", $"My name is {bobInfo.Name}."),
        ("favorite_color", $"My favorite color is {bobInfo.FavoriteColor}."),
        ("creation_date", $"I was created on {bobInfo.CreationDate.ToShortDateString()}."),
        ("creator", $"I was created by {bobInfo.Creator}."),
        ("website", $"My website is {bobInfo.Website}."),
        ("docs", $"You can find my documentation at {bobInfo.DocsPage}."),
        ("discord_username", $"My creator's Discord username is {bobInfo.DiscordUsername}."),
        ("community_server", $"Join my community server here: {bobInfo.CommunityServer}."),
        ("personal_website", $"My creator's personal website is {bobInfo.PersonalWebsite}.")
    };

        foreach (var (key, fact) in facts)
        {
            // Get the embedding for the current fact
            var embedding = await GetEmbedding(fact);

            // Prepare the metadata
            _ = new
            {
                key,
                message = fact,
                userId = "Bob",
                timestamp = DateTime.UtcNow.ToString()
            };

            // Add the embedding to the collection
            await ChromaService.AddToCollection(
                collectionUUID: "4bdfa78e-41c8-4cbb-a107-e3d38662744e",
                embedding: embedding,
                document: fact,
                id: key,  // Use the key as the ID
                metadataKey: "message",
                metadataValue: fact
            );
        }
    }

    public static async Task<string> RetrieveBobInfo(string query)
    {
        var embedding = await GetEmbedding(query);

        var body = new { embeddings = embedding, top_k = 1 }; // Retrieve the most relevant info
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var response = await Interface.Client.PostAsync($"{ChromaUrl}/heartbeat", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

        // Extract the most relevant message from Chroma
        var bestMatch = jsonResponse?.results?[0]?.metadata?.message;
        return bestMatch ?? "I don't have that information yet!";
    }

    private static async Task<float[]> GetEmbedding(string message)
    {
        // Get the embedding from OpenAI API or another embedding model
        return await OpenAI.GetEmbedding(message);
    }
}

