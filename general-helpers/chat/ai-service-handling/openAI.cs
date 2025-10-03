using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Bob.ApiInteractions;
using Newtonsoft.Json;

namespace Commands.Helpers
{
    public static class OpenAI
    {
        public static async Task<string> PostToOpenAI(List<object> messages)
        {
            using HttpClient client = new();

            HttpRequestMessage request = new(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string apiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY") ?? throw new InvalidOperationException("API Key is missing.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = CreateContent(messages);

            HttpResponseMessage response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
            string assistantMessage = jsonResponse?.choices[0]?.message?.content;

            return assistantMessage;
        }

        private static StringContent CreateContent(List<object> messages)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages
            };

            string jsonContent = JsonConvert.SerializeObject(requestBody);
            return new StringContent(jsonContent, Encoding.UTF8, "application/json");
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

            // Create the HttpRequestMessage
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            // Add the Authorization header
            requestMessage.Headers.Add("Authorization", "Bearer " + apiKey);

            using (var client = new HttpClient())
            {
                // Send the request
                var response = await client.SendAsync(requestMessage);

                // Ensure the request was successful
                response.EnsureSuccessStatusCode();

                // Deserialize the response to get the embeddings
                var responseBody = await response.Content.ReadAsStringAsync();
                var embeddingsResponse = JsonConvert.DeserializeObject<EmbeddingsResponse>(responseBody);

                // Return the embeddings array
                return embeddingsResponse.Data[0].Embedding;
            }
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
