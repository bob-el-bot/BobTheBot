using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Commands.Helpers
{
    public class Analyze
    {
        public class Link
        {
            public string link;
            public HttpStatusCode statusCode;
            public string specialCase;
            public bool isRickRoll;
            public bool isRedirect;
            public bool failed = false;
        }

        public static async Task<List<Link>> GetUrlTrail(string link)
        {
            List<Link> trail = new List<Link>();

            while (!string.IsNullOrWhiteSpace(link))
            {
                HttpClientHandler handler = new()
                {
                    AllowAutoRedirect = false
                };
                HttpClient httpClient = new(handler);

                HttpRequestMessage request = new(HttpMethod.Head, link);

                request.Headers.UserAgent.Add(ApiInteractions.Interface.productValue);
                request.Headers.UserAgent.Add(ApiInteractions.Interface.commentValue);

                try
                {
                    var req = await httpClient.GetAsync(link);

                    if (req.IsSuccessStatusCode)
                    {
                        string code = await req.Content.ReadAsStringAsync();

                        HtmlDocument doc = new();
                        doc.LoadHtml(code);

                        HtmlNode metaTag = doc.DocumentNode.SelectSingleNode("//meta[@http-equiv='refresh']");
                        if (metaTag != null)
                        {
                            string content = metaTag.GetAttributeValue("content", "");
                            string url = GetUrlFromContent(content);
                            Link newLink = new()
                            {
                                link = link,
                                statusCode = req.StatusCode,
                                specialCase = "Meta-Refresh Redirect",
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ" ? true : false,
                                isRedirect = true
                            };
                            trail.Add(newLink);
                            link = url;
                        }
                        else
                        {
                            Link newLink = new()
                            {
                                link = link,
                                statusCode = req.StatusCode,
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ" ? true : false,
                                isRedirect = ((int)req.StatusCode >= 300 && (int)req.StatusCode <= 308) ? true : false
                            };
                            trail.Add(newLink);
                            link = null;
                        }
                    }
                    else
                    {
                        Link newLink = new()
                        {
                            link = link,
                            statusCode = req.StatusCode,
                            isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ" ? true : false,
                            isRedirect = ((int)req.StatusCode >= 300 && (int)req.StatusCode <= 308) ? true : false
                        };
                        trail.Add(newLink);

                        if (req.Headers.Location != null)
                        {
                            link = req.Headers.Location.ToString();
                        }
                        else
                        {
                            link = null;
                        }
                    }
                }
                catch
                {
                    Link newLink = new()
                    {
                        link = link,
                        failed = true
                    };
                    trail.Add(newLink);
                    link = null;
                }
            }

            static string GetUrlFromContent(string content)
            {
                // Extracting the URL from the content attribute value
                int urlIndex = content.IndexOf("url=", StringComparison.OrdinalIgnoreCase);
                if (urlIndex != -1)
                {
                    return content[(urlIndex + 4)..].Trim('"', '\'');
                }
                return string.Empty;
            }

            return trail;
        }
    }
}