using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ApiInteractions;
using Commands.Helpers;
using DotNetEnv;
using Newtonsoft.Json;

namespace Commands.Helpers
{
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

        public static async Task<string> GetContext(string query, ulong userId)
        {
            using var client2 = new HttpClient();
            string chromaApiKey2 = Environment.GetEnvironmentVariable("CHROMA_TOKEN");

            // Add the authorization header if required
            client2.DefaultRequestHeaders.Add("Authorization", $"Bearer {chromaApiKey2}");
            var response2 = await client2.GetAsync($"{ChromaUrl}/api/v1/heartbeat");
            Console.WriteLine("Response: " + await response2.Content.ReadAsStringAsync());

            // Get the embedding for the query
            var embedding = await GetEmbedding(query);

            // Log the embedding to verify it's correct
            Console.WriteLine("Embedding: " + string.Join(", ", embedding));

            if (embedding == null || embedding.Length == 0)
            {
                Console.WriteLine("Error: Embedding is empty or null.");
                return null;
            }

            // Prepare the query parameters with the embedding wrapped in an array (ChromaDB expects a list of lists)
            var queryParams = new Dictionary<string, object>
    {
        { "query_embeddings", new[] { embedding } },  // ✅ Fix: Wrap in an array
        { "n_results", 5 },  // Optional: Specify the number of results
        { "where", new Dictionary<string, string> { { "userId", userId.ToString() } } } // Filtering by userId
    };

            // Prepare the POST request body
            var content = new StringContent(JsonConvert.SerializeObject(queryParams), Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            string chromaApiKey = Environment.GetEnvironmentVariable("CHROMA_TOKEN");

            // Add the authorization header if required
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {chromaApiKey}");

            // Specify the collection name
            string collectionName = "4bdfa78e-41c8-4cbb-a107-e3d38662744e";  // Your collection name here

            // Build the full URL to query the collection
            string url = $"{ChromaUrl}/collections/{collectionName}/query";

            // Log the request for debugging
            Console.WriteLine("Request URL: " + url);
            Console.WriteLine("Request JSON:\n" + JsonConvert.SerializeObject(queryParams));

            // Perform the POST request
            var response = await client.PostAsync(url, content);

            // Check if the response status is successful
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error {response.StatusCode}: " + await response.Content.ReadAsStringAsync());
                return null;
            }

            // Read and return the response body
            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public static async Task CreateCollection(string collectionName)
        {
            var body = new
            {
                name = collectionName
            };

            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            string chromaApiKey = Environment.GetEnvironmentVariable("CHROMA_TOKEN");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {chromaApiKey}");

            var response = await client.PostAsync($"{ChromaUrl}/collections", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Create Collection Response:\n" + responseText);

            response.EnsureSuccessStatusCode();
        }

        public static async Task AddToCollection(string collectionUUID, float[] embedding, string document, string id, string metadataKey, string metadataValue)
        {
            var body = new
            {
                ids = new[] { id },
                documents = new[] { document },
                embeddings = new[] { embedding },
                metadatas = new[]
                {
            new Dictionary<string, string> { { metadataKey, metadataValue } }
        }
            };

            var json = JsonConvert.SerializeObject(body);
            Console.WriteLine("Request JSON:\n" + json);  // ✅ Log the request JSON

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            string chromaApiKey = Environment.GetEnvironmentVariable("CHROMA_TOKEN");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {chromaApiKey}");

            var response = await client.PostAsync($"{ChromaUrl}/collections/{collectionUUID}/add", content);
            var responseText = await response.Content.ReadAsStringAsync();  // ✅ Get the full response

            Console.WriteLine("Response JSON:\n" + responseText);  // ✅ Print error message

            response.EnsureSuccessStatusCode();
        }

        public static async Task<string> QueryCollection(string collectionName, float[][] queryEmbeddings, int nResults, Dictionary<string, string> filters)
        {
            var body = new
            {
                collection = collectionName,
                query_embeddings = queryEmbeddings,
                n_results = nResults,
                where = filters
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            string chromaApiKey = Environment.GetEnvironmentVariable("CHROMA_TOKEN");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {chromaApiKey}");

            var response = await client.PostAsync($"{ChromaUrl}/query", content);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}