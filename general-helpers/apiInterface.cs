using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System;

namespace ApiInteractions
{
    /// <summary>
    /// Provides methods for interacting with APIs.
    /// </summary>
    public static class Interface
    {
        /// <summary>
        /// The product value used in the user agent header.
        /// </summary>
        public static readonly ProductInfoHeaderValue productValue = new("BobTheBot", "1.0");

        /// <summary>
        /// The comment value used in the user agent header.
        /// </summary>
        public static readonly ProductInfoHeaderValue commentValue = new("(+https://github.com/Quantam-Studios/BobTheBot)");

        public static readonly HttpClient Client = new();

        /// <summary>
        /// Enumeration of accept types for specifying the type of content to accept in the response.
        /// </summary>
        public enum AcceptTypes
        {
            /// <summary>
            /// Accept application/json content type.
            /// </summary>
            application_json,

            /// <summary>
            /// Accept text/plain content type.
            /// </summary>
            text_plain,

            /// <summary>
            /// Accept image content type.
            /// </summary>
            image,
        }

        /// <summary>
        /// Sends a GET request to the specified URL and returns the response content as a string.
        /// </summary>
        /// <param name="link">The URL to send the request to.</param>
        /// <param name="accept">The type of content to accept in the response.</param>
        /// <param name="token">Optional. The authentication token to include in the request header.</param>
        /// <returns>Returns the response content as a string.</returns>
        public static async Task<string> GetFromAPI(string link, AcceptTypes accept, string token = null)
        {
            // Formulate Request
            HttpRequestMessage request = new(HttpMethod.Get, link);
            MediaTypeWithQualityHeaderValue acceptValue = new(
                (accept == AcceptTypes.application_json) ? "application/json" :
                (accept == AcceptTypes.text_plain) ? "text/plain" :
                "image/*"
            );

            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            request.Headers.Accept.Add(acceptValue);

            // Add authentication token if provided
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Send the request
            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Return content as string
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Sends a GET request to the specified URL and returns the response content as a byte array (for images).
        /// </summary>
        /// <param name="link">The URL to send the request to.</param>
        /// <param name="token">Optional. The authentication token to include in the request header.</param>
        /// <returns>Returns the response content as a byte array.</returns>
        public static async Task<byte[]> GetFromAPI(string link, string token = null)
        {
            // Formulate Request
            HttpRequestMessage request = new(HttpMethod.Get, link);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

            // Add authentication token if provided
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Send the request
            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Return content as byte array
            return await response.Content.ReadAsByteArrayAsync();
        }

        /// <summary>
        /// Sends a POST request to the specified URL.
        /// </summary>
        /// <param name="link">The URL to send the request to.</param>
        /// <param name="token">The authentication token to include in the request header.</param>
        /// <param name="content">The content to include in the request body.</param>
        /// <returns>Returns the HTTP status code of the response.</returns>
        /// <remarks>Use this method only for authenticated requests.</remarks>
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