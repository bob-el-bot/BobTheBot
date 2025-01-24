using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ApiInteractions;
using Commands.Helpers;
using Newtonsoft.Json;

public class ChromaService
{
    private static readonly string ChromaUrl = Environment.GetEnvironmentVariable("CHROMA_URL");

    public static async Task StoreMessage(string message, ulong userId)
    {
        var embedding = await GetEmbedding(message);

        var body = new
        {
            embeddings = embedding,
            metadata = new { userId = userId.ToString(), message = message, timestamp = DateTime.UtcNow.ToString() }
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await Interface.Client.PostAsync($"{ChromaUrl}/add", content);
        response.EnsureSuccessStatusCode();
    }


    private static async Task<float[]> GetEmbedding(string message)
    {
        // Get the embedding from OpenAI API or a model of your choice
        return await OpenAI.GetEmbedding(message);
    }

    /// <summary>
    /// Get the context of a message
    /// </summary>
    /// <param name="query">The message to get the context for</param>
    /// <param name="userId">The user ID</param>
    /// <returns>The context of the message</returns>

    public static async Task<string> GetContext(string query, ulong userId)
    {
        var embedding = await GetEmbedding(query);

        var body = new
        {
            query = embedding,
            userId = userId.ToString()
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await Interface.Client.PostAsync($"{ChromaUrl}/query", content);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }

}
