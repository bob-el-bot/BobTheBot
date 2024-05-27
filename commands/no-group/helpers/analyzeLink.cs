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

        public class LinkInfo
        {
            public string Link { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public string SpecialCase { get; set; }
            public bool IsRickRoll { get; set; }
            public bool IsRedirect { get; set; }
            public bool IsShortened { get; set; }
            public bool ContainsCookies { get; set; }
            public bool Failed { get; set; }
        }

        public static async Task<Embed> AnalyzeLink(string link)
        {
            List<LinkInfo> trail = await GetUrlTrail(link);
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
            foreach (LinkInfo l in trail)
            {
                if (l.IsRickRoll && !isRickRoll)
                {
                    warnings.AppendLine("- You will get rick-rolled. ");
                    isRickRoll = true;
                }

                if (l.SpecialCase != null && !containsSpecialRedirect)
                {
                    warnings.AppendLine("- Contains a hard-coded redirect. ");
                    containsSpecialRedirect = true;
                }

                if (l.ContainsCookies && !containsCookies)
                {
                    warnings.AppendLine("- Contains cookies (these can be malicious, or safe). ");
                    containsCookies = true;
                }

                if (!l.Failed)
                {
                    description.AppendLine($"{(linkCount == trail.Count ? "📍" : "⬇️")} <{l.Link}> **Status Code:** `{(int)l.StatusCode} {l.StatusCode}{(l.SpecialCase != null ? $" - {l.SpecialCase}" : "")}`\n**Is Redirect?** {(l.IsRedirect ? "true" : "false")} **Has Cookies?** {(l.ContainsCookies ? "true" : "false")} **Is Short URL?** {(l.IsShortened ? "true" : "false")} **Is Rick Roll?** {(l.IsRickRoll ? "true" : "false")} ");
                }
                else
                {
                    description.AppendLine($"❌ <{l.Link}> **Failed to visit link.**");
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
            embed.AddField(name: $"{adviceEmoji} Warnings", value: $"{(warnings.Length == 0 ? "Bob hasn't found anything to worry about, however that does not mean it is safe for certain." : warnings.ToString())}\n✅ = Not Suspicious ⚠️ = Potentially Suspicious 🚫 = Suspicious ⁉️ = Unknown");

            return embed.Build();
        }

        private static async Task<List<LinkInfo>> GetUrlTrail(string link)
        {
            List<LinkInfo> trail = new();

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

                    bool hasCookies = req.Headers.TryGetValues("Set-Cookie", out _);

                    if (req.IsSuccessStatusCode)
                    {
                        string code = await req.Content.ReadAsStringAsync();

                        HtmlDocument doc = new();
                        doc.LoadHtml(code);

                        string jsRedirect = GetJavaScriptRedirectLink(code);
                        HtmlNode metaTag = doc.DocumentNode.SelectSingleNode("//meta[@http-equiv='refresh']");

                        if (IsGitHubRepository(link))
                        {
                            LinkInfo gitHubRepoLink = new()
                            {
                                Link = link,
                                StatusCode = req.StatusCode,
                                IsRedirect = false,
                                IsShortened = IsShortenedUrl(link, false),
                                ContainsCookies = hasCookies,
                                Failed = false
                            };
                            trail.Add(gitHubRepoLink);
                            link = null;
                        }
                        else if (metaTag != null)
                        {
                            string content = metaTag.GetAttributeValue("content", "");
                            string url = GetUrlFromContent(content);
                            LinkInfo newLink = new()
                            {
                                Link = $"{link}",
                                StatusCode = req.StatusCode,
                                SpecialCase = "Meta-Refresh Redirect",
                                ContainsCookies = hasCookies,
                                IsRickRoll = HasRickRoll(link),
                                IsRedirect = true,
                                IsShortened = IsShortenedUrl(link, true)
                            };
                            trail.Add(newLink);
                            link = url;
                            redirectCount++;
                        }
                        else if (jsRedirect != null)
                        {
                            LinkInfo newLink = new()
                            {
                                Link = $"{link}",
                                StatusCode = req.StatusCode,
                                SpecialCase = "JavaScript Redirect",
                                ContainsCookies = hasCookies,
                                IsRickRoll = HasRickRoll(link),
                                IsRedirect = true,
                                IsShortened = IsShortenedUrl(link, true)
                            };
                            trail.Add(newLink);
                            link = jsRedirect;
                            redirectCount++;
                        }
                        else
                        {
                            bool isRedirect = (int)req.StatusCode >= 300 && (int)req.StatusCode <= 308;
                            LinkInfo newLink = new()
                            {
                                Link = $"{link}",
                                StatusCode = req.StatusCode,
                                ContainsCookies = hasCookies,
                                IsRickRoll = HasRickRoll(link),
                                IsRedirect = isRedirect,
                                IsShortened = IsShortenedUrl(link, isRedirect)
                            };
                            trail.Add(newLink);
                            link = null;
                        }
                    }
                    else
                    {
                        bool isRedirect = (int)req.StatusCode >= 300 && (int)req.StatusCode <= 308;
                        LinkInfo newLink = new()
                        {
                            Link = $"{link}",
                            StatusCode = req.StatusCode,
                            ContainsCookies = hasCookies,
                            IsRickRoll = HasRickRoll(link),
                            IsRedirect = isRedirect,
                            IsShortened = IsShortenedUrl(link, isRedirect)
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
                    LinkInfo newLink = new()
                    {
                        Link = $"{link}",
                        Failed = true
                    };
                    trail.Add(newLink);
                    link = null;
                }
            }

            if (redirectCount > maximumRedirectCount)
            {
                LinkInfo newLink = new()
                {
                    Link = $"Unknown (Bob follows **up to {maximumRedirectCount}** redirects.)",
                    Failed = true
                };
                trail.Add(newLink);
            }

            return trail;
        }

        private static bool HasRickRoll(string url)
        {
            return url.Contains("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        }

        private static bool IsGitHubRepository(string url)
        {
            // GitHub repository URL pattern (example)
            string gitHubRepoPattern = @"https://github.com/.*/.*";
            return Regex.IsMatch(url, gitHubRepoPattern);
        }

        private static string GetJavaScriptRedirectLink(string htmlContent)
        {
            // Regular expression pattern to match script tags
            string scriptPattern = @"<script\b[^>]*>([\s\S]*?)<\/script>";
            var scriptMatches = Regex.Matches(htmlContent, scriptPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Loop through each script tag
            foreach (Match scriptMatch in scriptMatches.Cast<Match>())
            {
                string scriptContent = scriptMatch.Groups[1].Value; // Extract script content

                // Check if the script content contains window.location.href
                if (scriptContent.Contains("window.location.href"))
                {
                    // Regular expression pattern to check if window.location.href is not inside onclick function
                    string redirectLinkPattern = @"window\.location\.href\s*=\s*[""']([^""']+)";

                    // Add a negative lookahead to exclude window.location.href inside onclick functions
                    redirectLinkPattern += @"(?![\s\S]*?onclick)";

                    // Search for window.location.href outside onclick functions
                    var redirectLinkMatch = Regex.Match(scriptContent, redirectLinkPattern);

                    // If a match is found, return the redirected link
                    if (redirectLinkMatch.Success)
                    {
                        return redirectLinkMatch.Groups[1].Value;
                    }
                }
            }

            // If no redirected link is found, return null
            return null;
        }

        private static bool IsShortenedUrl(string url, bool isRedirect)
        {
            return ContainsShortenedDomain(url) && isRedirect;
        }

        private static bool ContainsShortenedDomain(string url)
        {
            // List of common URL shortener domains
            string[] shortenerDomains = { "dis.gd", "v.gd", "ow.ly", "bl.ink", "3.ly", "tiny.cc", "bit.ly", "tinyurl.com", "goo.gl", "shorturl.at", "t.ly", "youtu.be", "y2u.be", "t.co", "short.gy", "snip.ly", "Buff.ly", "redd.it", "rb.gy", "msha.ke", "trib.al" };

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