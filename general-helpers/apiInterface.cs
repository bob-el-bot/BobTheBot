using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

public static class APIInterface
{
    private static readonly ProductInfoHeaderValue productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
    private static readonly ProductInfoHeaderValue commentValue = new ProductInfoHeaderValue("(+https://github.com/Quantam-Studios/BobTheBot)");

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
        var httpClient = new HttpClient();

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, link);
        request.Headers.UserAgent.Add(productValue);
        request.Headers.UserAgent.Add(commentValue);
        var acceptValue = new MediaTypeWithQualityHeaderValue((accept == AcceptTypes.application_json) ? "application/json" : (accept == AcceptTypes.text_plain) ? "text/plain" : "image/*");
        request.Headers.Accept.Add(acceptValue);

        // Send Request (Get The Quote)
        var resp = await httpClient.SendAsync(request);
        // Read In Content
        return await resp.Content.ReadAsStringAsync();
    }
}