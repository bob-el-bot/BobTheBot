using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore.Query;

namespace Commands.Helpers
{
    public class Analyze
    {
        public static readonly int maximumRedirectCount = 4;

        public class Link
        {
            public string link;
            public HttpStatusCode statusCode;
            public string specialCase;
            public bool isRickRoll;
            public bool isRedirect;
            public bool failed = false;
        }

        public static async Task<Embed> AnalyzeLink(string link)
        {
            List<Link> trail = await GetUrlTrail(link);
            StringBuilder description = new();
            description.AppendLine("**Clicking this URL will bring you to these places:**\n");

            // Warnings
            StringBuilder warnings = new();
            bool isRickRoll = false;
            // concerning if above 3
            bool highRedirectCount = trail.Count > 3;
            if (highRedirectCount)
            {
                warnings.AppendLine("- There is a concerning amount of redirects (more than 3). ");
            }
            bool containsRedirects = trail.Count > 1;
            if (containsRedirects)
            {
                warnings.AppendLine("- You will get redirected.");
            }
            bool containsSpecialRedirect = false;
            bool failed = false;

            // Format Description
            int linkCount = 1;
            foreach (Link l in trail)
            {
                if (l.isRickRoll && isRickRoll == false)
                {
                    warnings.AppendLine("- You will get rick-rolled. ");
                    isRickRoll = true;
                }

                if (l.specialCase != null && containsSpecialRedirect == false)
                {
                    warnings.AppendLine("- Contains a hard-coded redirect. ");
                    containsSpecialRedirect = true;
                }

                if (!l.failed)
                {
                    description.Append($"{(linkCount == trail.Count ? "📍" : "⬇️")} {l.link} **Status Code:** `{(int)l.statusCode} {l.statusCode}{(l.specialCase != null ? $" - {l.specialCase}" : "")}`\n**Is Rick Roll?** {(l.isRickRoll ? "true" : "false")} **Is Redirect?** {(l.isRedirect ? "true" : "false")}\n");
                }
                else
                {
                    description.Append($"❌ {l.link} **Failed to visit link.**\n");
                    if (failed == false)
                    {
                        warnings.AppendLine("- For an unknown reason, Bob could not open this page (it might not exist). ");
                        failed = true;
                    }
                }
                linkCount++;
            }

            var embed = new EmbedBuilder
            {
                Title = $"🕵️ Analysis of {link}",
                Description = description.ToString(),
                Footer = new EmbedFooterBuilder
                {
                    Text = "Bob can't gauruntee a link is safe."
                },
                Color = Bot.theme
            };

            string adviceEmoji = failed && !containsRedirects ? "⁉️" : (isRickRoll || highRedirectCount || failed ? "🚫" : (containsRedirects || containsSpecialRedirect ? "⚠️" : "✅"));
            embed.AddField(name: $"{adviceEmoji} Warnings", value: $"{(warnings.Length == 0 ? "Bob hasn't found anything to worry about, however that does mean not it is safe for certain." : warnings.ToString())}\n✅ = Not Suspicious ⚠️ = Potentially Suspicious 🚫 = Suspicious ⁉️ = Unknown");

            return embed.Build();
        }

        private static async Task<List<Link>> GetUrlTrail(string link)
        {
            List<Link> trail = new();

            int redirectCount = 0;
            while (redirectCount <= maximumRedirectCount && !string.IsNullOrWhiteSpace(link))
            {
                HttpClientHandler handler = new()
                {
                    AllowAutoRedirect = false,
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
                                link = $"<{link}>",
                                statusCode = req.StatusCode,
                                specialCase = "Meta-Refresh Redirect",
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                                isRedirect = true
                            };
                            trail.Add(newLink);
                            link = url;
                            redirectCount++;
                        }
                        else
                        {
                            Link newLink = new()
                            {
                                link = $"<{link}>",
                                statusCode = req.StatusCode,
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                                isRedirect = (int)req.StatusCode >= 300 && (int)req.StatusCode <= 308
                            };
                            trail.Add(newLink);
                            link = null;
                        }
                    }
                    else
                    {
                        Link newLink = new()
                        {
                            link = $"<{link}>",
                            statusCode = req.StatusCode,
                            isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                            isRedirect = (int)req.StatusCode >= 300 && (int)req.StatusCode <= 308
                        };
                        trail.Add(newLink);

                        if (req.Headers.Location != null)
                        {
                            link = req.Headers.Location.ToString();
                            redirectCount++;
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
                        link = $"<{link}>",
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

            if (redirectCount > maximumRedirectCount)
            {
                Link newLink = new()
                {
                    link = $"Unknown (Bob follows **up to {maximumRedirectCount}** redirects.)",
                    failed = true
                };
                trail.Add(newLink);
            }

            return trail;
        }
    }
}