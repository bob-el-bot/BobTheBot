using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System;

namespace ApiInteractions
{
    public static class Interface
    {
        public static readonly ProductInfoHeaderValue productValue = new("BobTheBot", "1.0");
        public static readonly ProductInfoHeaderValue commentValue = new("(+https://github.com/Quantam-Studios/BobTheBot)");

        private static readonly HttpClient Client = new();

        public enum AcceptTypes
        {
            application_json,
            text_plain,
            image,
        }

        /// <summary>
        /// Sends a GET request to the specified URL.
        /// <returns>Returns an HttpResponseMessage as a string.</returns>
        /// </summary>
        public static async Task<string> GetFromAPI(string link, AcceptTypes accept, string token = null)
        {
            // Formulate Request
            HttpRequestMessage request = new(HttpMethod.Get, link);
            MediaTypeWithQualityHeaderValue acceptValue = new((accept == AcceptTypes.application_json) ? "application/json" : (accept == AcceptTypes.text_plain) ? "text/plain" : "image/*");

            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // if authentication is given, use it.
            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(token);
            }

            // Send Request (Get The Quote)
            var resp = await Client.SendAsync(request);
            // Read In Content
            return await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Sends a POST request to the specified URL.
        /// <returns>Returns an HttpResponseCode.</returns>
        /// <remarks>Only use this method for authenticated requests.</remarks>
        /// </summary>
        public static async Task<HttpStatusCode> PostToAPI(string link, string token, StringContent content)
        {
            // Formulate Request
            HttpRequestMessage request = new(HttpMethod.Post, link);
            MediaTypeWithQualityHeaderValue acceptValue = new("application/json");

            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);
            request.Headers.Authorization = new AuthenticationHeaderValue(token);
            request.Content = content;

            // POST
            var req = await Client.SendAsync(request);
            return req.StatusCode;
        }
    }
}