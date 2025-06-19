using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ApiInteractions;
using Newtonsoft.Json;

namespace Commands.Helpers
{
    public static class OpenAI
    {
        public static async Task<string> PostToOpenAI(string content)
        {
            using HttpClient client = new();

            HttpRequestMessage request = new(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string apiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY") ?? throw new InvalidOperationException("API Key is missing.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = CreateContent(content);

            HttpResponseMessage response = await client.SendAsync(request);

            // Throws an exception if the request fails
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize the response to get the assistant's content
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
            string assistantMessage = jsonResponse?.choices[0]?.message?.content;

            return assistantMessage; // Return the assistant's response
        }

        private static StringContent CreateContent(string content)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini", // You can parameterize this if needed
                messages = new[]
                {
                    new { role = "system", content = "You are BobTheBot, you mainly go by Bob, you are a helpful friend who is very nice and a little fancy." },
                    new { role = "user", content = content }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestBody); // Serialize the object to JSON
            return new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        }

        public static async Task<float[]> GetEmbedding(string content)
        {
            // Get the OpenAI API key from your environment variables
            string apiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY") ?? throw new InvalidOperationException("API Key is missing.");

            // Form the request body
            var requestBody = new
            {
                model = "text-embedding-ada-002",
                input = content
            };

            // Send the POST request to OpenAI's embedding endpoint
            var contentJson = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            contentJson.Headers.Add("Authorization", "Bearer " + apiKey);

            var response = await Interface.Client.PostAsync("https://api.openai.com/v1/embeddings", contentJson);

            // Ensure the request was successful
            response.EnsureSuccessStatusCode();

            // Deserialize the response to get the embeddings
            var responseBody = await response.Content.ReadAsStringAsync();
            var embeddingsResponse = JsonConvert.DeserializeObject<EmbeddingsResponse>(responseBody);

            // Return the embeddings array
            return embeddingsResponse.Data[0].Embedding;
        }

        // Class to hold the structure of the response
        public class EmbeddingsResponse
        {
            public DataItem[] Data { get; set; }
        }

        public class DataItem
        {
            public float[] Embedding { get; set; }
        }
    }
}
