using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using HtmlAgilityPack;

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
            public bool isShortened;
            public bool containsCookies;
            public bool failed;
        }

        public static async Task<Embed> AnalyzeLink(string link)
        {
            List<Link> trail = await GetUrlTrail(link);
            StringBuilder description = new();
            description.AppendLine("**Clicking this URL will bring you to these places:**\n");

            // Warnings
            StringBuilder warnings = new();
            bool isRickRoll = false;
            bool highRedirectCount = trail.Count > 3;
            if (highRedirectCount)
            {
                warnings.AppendLine("- There is a concerning amount of redirects (more than 3). ");
            }

            bool containsCookies = false;
            bool containsSpecialRedirect = false;
            bool failed = false;

            // Format Description
            int linkCount = 1;
            foreach (Link l in trail)
            {
                if (l.isRickRoll && !isRickRoll)
                {
                    warnings.AppendLine("- You will get rick-rolled. ");
                    isRickRoll = true;
                }

                if (l.specialCase != null && !containsSpecialRedirect)
                {
                    warnings.AppendLine("- Contains a hard-coded redirect. ");
                    containsSpecialRedirect = true;
                }

                if (l.containsCookies && !containsCookies)
                {
                    warnings.AppendLine("- Contains cookies (these can be malicious, or safe). ");
                    containsCookies = true;
                }

                if (!l.failed)
                {
                    description.AppendLine($"{(linkCount == trail.Count ? "📍" : "⬇️")} <{l.link}> **Status Code:** `{(int)l.statusCode} {l.statusCode}{(l.specialCase != null ? $" - {l.specialCase}" : "")}`\n**Is Redirect?** {(l.isRedirect ? "true" : "false")} **Has Cookies?** {(l.containsCookies ? "true" : "false")} **Is Short URL?** {(l.isShortened ? "true" : "false")} **Is Rick Roll?** {(l.isRickRoll ? "true" : "false")} ");
                }
                else
                {
                    description.AppendLine($"❌ <{l.link}> **Failed to visit link.**");
                    if (!failed)
                    {
                        warnings.AppendLine("- For an unknown reason, Bob could not open this page (it might not exist). ");
                        failed = true;
                    }
                }
                linkCount++;
            }

            var embed = new EmbedBuilder
            {
                Title = $"🕵️ Analysis of <{link}>",
                Description = description.ToString(),
                Footer = new EmbedFooterBuilder
                {
                    Text = "Bob can't guarantee a link is safe."
                },
                Color = Bot.theme
            };

            string adviceEmoji = failed ? "⁉️" : (isRickRoll || highRedirectCount || failed ? "🚫" : (containsSpecialRedirect || containsCookies ? "⚠️" : "✅"));
            embed.AddField(name: $"{adviceEmoji} Warnings", value: $"{(warnings.Length == 0 ? "Bob hasn't found anything to worry about, however that does mean not it is safe for certain." : warnings.ToString())}\n✅ = Not Suspicious ⚠️ = Potentially Suspicious 🚫 = Suspicious ⁉️ = Unknown");

            return embed.Build();
        }

        private static async Task<List<Link>> GetUrlTrail(string link)
        {
            List<Link> trail = new();

            int redirectCount = 0;
            while (redirectCount <= maximumRedirectCount && !string.IsNullOrWhiteSpace(link))
            {
                CookieContainer cookies = new();
                HttpClientHandler handler = new()
                {
                    AllowAutoRedirect = false,
                    CookieContainer = cookies
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

                        string jsRedirect = GetJavaScriptRedirectLink(code);
                        HtmlNode metaTag = doc.DocumentNode.SelectSingleNode("//meta[@http-equiv='refresh']");

                        if (metaTag != null)
                        {
                            string content = metaTag.GetAttributeValue("content", "");
                            string url = GetUrlFromContent(content);
                            Link newLink = new()
                            {
                                link = $"{link}",
                                statusCode = req.StatusCode,
                                specialCase = "Meta-Refresh Redirect",
                                containsCookies = cookies.GetCookies(new Uri(link)).Count != 0,
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                                isRedirect = true,
                                isShortened = IsShortenedUrl(link, true)
                            };
                            trail.Add(newLink);
                            link = url;
                            redirectCount++;
                        }
                        else if (jsRedirect != null)
                        {
                            Link newLink = new()
                            {
                                link = $"{link}",
                                statusCode = req.StatusCode,
                                specialCase = "JavaScript Redirect",
                                containsCookies = cookies.GetCookies(new Uri(link)).Count != 0,
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                                isRedirect = true,
                                isShortened = IsShortenedUrl(link, true)
                            };
                            trail.Add(newLink);
                            link = jsRedirect;
                            redirectCount++;
                        }
                        else
                        {
                            bool isRedirect = (int)req.StatusCode >= 300 && (int)req.StatusCode <= 308;
                            Link newLink = new()
                            {
                                link = $"{link}",
                                statusCode = req.StatusCode,
                                containsCookies = cookies.GetCookies(new Uri(link)).Count != 0,
                                isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                                isRedirect = isRedirect,
                                isShortened = IsShortenedUrl(link, isRedirect)
                            };
                            trail.Add(newLink);
                            link = null;
                        }
                    }
                    else
                    {
                        bool isRedirect = (int)req.StatusCode >= 300 && (int)req.StatusCode <= 308;
                        Link newLink = new()
                        {
                            link = $"{link}",
                            statusCode = req.StatusCode,
                            containsCookies = cookies.GetCookies(new Uri(link)).Count != 0,
                            isRickRoll = link == "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                            isRedirect = isRedirect,
                            isShortened = IsShortenedUrl(link, isRedirect)
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
                        link = $"{link}",
                        failed = true
                    };
                    trail.Add(newLink);
                    link = null;
                }
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

        private static string GetJavaScriptRedirectLink(string htmlContent)
        {
            string scriptPattern = @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>";
            var scriptMatches = Regex.Matches(htmlContent, scriptPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match scriptMatch in scriptMatches.Cast<Match>())
            {
                string scriptContent = scriptMatch.Value;

                if (scriptContent.Contains("window.location.href"))
                {
                    string redirectLinkPattern = @"window\.location\.href\s*=\s*['""]([^'""]+)['""]";
                    var redirectLinkMatch = Regex.Match(scriptContent, redirectLinkPattern);

                    if (redirectLinkMatch.Success)
                    {
                        return redirectLinkMatch.Groups[1].Value;
                    }
                }
            }

            return null;
        }

        private static bool IsShortenedUrl(string url, bool isRedirect)
        {
            return ContainsShortenedDomain(url) && isRedirect;
        }

        private static bool ContainsShortenedDomain(string url)
        {
            // List of common URL shortener domains
            string[] shortenerDomains = { "dis.gd", "v.gd", "ow.ly", "bl.ink", "3.ly", "tiny.cc", "bit.ly", "tinyurl.com", "goo.gl", "shorturl.at", "t.ly", "youtu.be", "y2u.be", "t.co", "short.gy", "snip.ly", "Buff.ly", "redd.it", "rb.gy", "msha.ke", };

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                string domain = uri.Host.ToLower();

                if (Array.Exists(shortenerDomains, d => domain.Contains(d)))
                {
                    string path = uri.AbsolutePath.Trim('/');
                    return !string.IsNullOrEmpty(path);
                }
            }

            return false;
        }

        private static string GetUrlFromContent(string content)
        {
            int urlIndex = content.IndexOf("url=", StringComparison.OrdinalIgnoreCase);
            if (urlIndex != -1)
            {
                return content[(urlIndex + 4)..].Trim('"', '\'');
            }
            return string.Empty;
        }
    }
}