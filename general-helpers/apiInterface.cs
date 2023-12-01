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

        public enum AcceptTypes
        {
            application_json,
            text_plain,
            image,
        }

        /// <summary>
        /// Sends a GET request to the specified URL.
        /// <returns>Returns an HttpRepsonseMessage as a string.</returns>
        /// <remarks>Do not use this method for authenticated requests.</remarks>
        /// </summary>
        public static async Task<string> GetFromAPI(string link, AcceptTypes accept)
        {
            // Formulate Request
            HttpClient httpClient = new();

            HttpRequestMessage request = new(HttpMethod.Get, link);
            MediaTypeWithQualityHeaderValue acceptValue = new((accept == AcceptTypes.application_json) ? "application/json" : (accept == AcceptTypes.text_plain) ? "text/plain" : "image/*");

            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // Send Request (Get The Quote)
            var resp = await httpClient.SendAsync(request);
            // Read In Content
            return await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Sends a POST request to the specified URL.
        /// <returns>Returns an HttpRepsonseCode.</returns>
        /// <remarks>Only use this method for authenticated requests.</remarks>
        /// </summary>
        public static async Task<HttpStatusCode> PostToAPI(string link, string token, StringContent content)
        {
            // Formulate Request
            HttpClient httpClient = new();

            HttpRequestMessage request = new(HttpMethod.Post, link);
            MediaTypeWithQualityHeaderValue acceptValue = new("application/json");

            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);
            request.Headers.Authorization = new AuthenticationHeaderValue(token);
            request.Content = content;

            // POST
            var req = await httpClient.SendAsync(request);
            return req.StatusCode;
        }
    }
}