using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Discord;
using HtmlAgilityPack;

namespace Bob.Commands.Helpers
{
    public partial class Analyze
    {
        private static readonly int requestTimeoutSeconds = 10;
        public static readonly int maximumRedirectCount = 4;

        private static int _userAgentIndex = 0;
        private static readonly object _userAgentLock = new();

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

                string[] redirectSpecialCases = ["Meta-Refresh Redirect", "JavaScript Redirect", "URL Redirect"];
                if (l.SpecialCase != null && Array.Exists(redirectSpecialCases, sc => sc == l.SpecialCase) && !containsSpecialRedirect)
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
                    string errorMessage = l.SpecialCase != null ? $"**Failed to visit link:** {l.SpecialCase}" : "**Failed to visit link.**";
                    description.AppendLine($"❌ <{l.Link}> {errorMessage}");
                    if (!failed)
                    {
                        warnings.AppendLine($"- {l.SpecialCase ?? "For an unknown reason, Bob could not open this page (it might not exist)"}. ");
                        failed = true;
                    }
                }
                linkCount++;
            }

            string title = "🕵️ Analysis of ";
            int maxLength = 256;

            // Calculate available space for the title
            int linkLengthWithBrackets = link.Length + 2; // Account for "< >"
            int availableSpace = maxLength - linkLengthWithBrackets;

            if (availableSpace < 0)
            {
                // If the link alone exceeds the max length, truncate the link
                int maxLinkLength = maxLength - 5; // Reserve space for "<...>"
                if (maxLinkLength > 0)
                {
                    link = link[..Math.Min(maxLinkLength, link.Length)] + "..."; // Truncate and indicate with "..."
                }
                else
                {
                    throw new ArgumentException("The provided link is too long to fit within the title constraints.");
                }
                availableSpace = 0; // No space left for the title
            }

            // Truncate the title if necessary
            if (title.Length > availableSpace)
            {
                title = title[..availableSpace];
            }

            // Construct the final title with the link wrapped in <>
            title += $"{link}";

            var embed = new EmbedBuilder
            {
                Title = title,
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

        static readonly List<string> UserAgents =
        [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36"
        ];

        private static async Task<List<LinkInfo>> GetUrlTrail(string link)
        {
            List<LinkInfo> trail = [];
            HashSet<string> visitedUrls = [];
            int redirectCount = 0;
            Random random = new();

            while (redirectCount <= maximumRedirectCount && !string.IsNullOrWhiteSpace(link))
            {
                if (visitedUrls.Contains(link))
                {
                    trail.Add(new LinkInfo
                    {
                        Link = link,
                        Failed = true,
                        SpecialCase = "Redirect Loop Detected"
                    });
                    break;
                }
                visitedUrls.Add(link);

                // Validate the URL
                if (!Uri.TryCreate(link, UriKind.Absolute, out _))
                {
                    trail.Add(new LinkInfo { Link = link, Failed = true });
                    break; // Stop processing if the URL is invalid
                }

                using HttpClientHandler handler = new()
                {
                    AllowAutoRedirect = false,
                };

                using HttpClient httpClient = new(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);
                HttpRequestMessage request = new(HttpMethod.Head, link);
                string customUserAgent = GetNextUserAgent();
                httpClient.DefaultRequestHeaders.Add("User-Agent", customUserAgent);

                try
                {
                    // Send the request
                    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    bool hasCookies = response.Headers.TryGetValues("Set-Cookie", out _);

                    if (response.IsSuccessStatusCode)
                    {
                        // Process various cases
                        if (IsGitHubRepository(link))
                        {
                            trail.Add(new LinkInfo
                            {
                                Link = link,
                                StatusCode = response.StatusCode,
                                IsRedirect = false,
                                IsShortened = IsShortenedUrl(link, false),
                                ContainsCookies = hasCookies,
                                Failed = false
                            });
                            link = null;
                            continue;
                        }

                        HtmlDocument doc = new();
                        HtmlNode metaTag = doc.DocumentNode.SelectSingleNode("//meta[@http-equiv='refresh']");

                        if (metaTag != null)
                        {
                            string content = metaTag.GetAttributeValue("content", "");
                            string url = GetUrlFromContent(content);
                            trail.Add(new LinkInfo
                            {
                                Link = $"{link}",
                                StatusCode = response.StatusCode,
                                SpecialCase = "Meta-Refresh Redirect",
                                ContainsCookies = hasCookies,
                                IsRickRoll = HasRickRoll(link),
                                IsRedirect = true,
                                IsShortened = IsShortenedUrl(link, true)
                            });
                            link = url;
                            redirectCount++;
                            continue;
                        }

                        string code = await response.Content.ReadAsStringAsync();
                        doc.LoadHtml(code);
                        string jsRedirect = GetJavaScriptRedirectLink(code);

                        if (jsRedirect != null)
                        {
                            trail.Add(new LinkInfo
                            {
                                Link = $"{link}",
                                StatusCode = response.StatusCode,
                                SpecialCase = "JavaScript Redirect",
                                ContainsCookies = hasCookies,
                                IsRickRoll = HasRickRoll(link),
                                IsRedirect = true,
                                IsShortened = IsShortenedUrl(link, true)
                            });
                            link = jsRedirect;
                            redirectCount++;
                            continue;
                        }

                        string urlRedirect = GetUrlRedirectInformation(link);

                        if (urlRedirect != null)
                        {
                            trail.Add(new LinkInfo
                            {
                                Link = $"{link}",
                                StatusCode = response.StatusCode,
                                SpecialCase = "URL Redirect",
                                ContainsCookies = hasCookies,
                                IsRickRoll = HasRickRoll(link),
                                IsRedirect = true,
                                IsShortened = IsShortenedUrl(link, true)
                            });
                            link = urlRedirect;
                            redirectCount++;
                            continue;
                        }

                        bool isRedirect = (int)response.StatusCode >= 300 && (int)response.StatusCode <= 308;
                        trail.Add(new LinkInfo
                        {
                            Link = $"{link}",
                            StatusCode = response.StatusCode,
                            ContainsCookies = hasCookies,
                            IsRickRoll = HasRickRoll(link),
                            IsRedirect = isRedirect,
                            IsShortened = IsShortenedUrl(link, isRedirect)
                        });
                        link = null;
                    }
                    else
                    {
                        bool isRedirect = (int)response.StatusCode >= 300 && (int)response.StatusCode <= 308;
                        trail.Add(new LinkInfo
                        {
                            Link = $"{link}",
                            StatusCode = response.StatusCode,
                            ContainsCookies = hasCookies,
                            IsRickRoll = HasRickRoll(link),
                            IsRedirect = isRedirect,
                            IsShortened = IsShortenedUrl(link, isRedirect)
                        });

                        if (response.Headers.Location != null)
                        {
                            Uri baseUri = new(link);
                            Uri resolvedUri = new(baseUri, response.Headers.Location);
                            link = resolvedUri.ToString();
                            redirectCount++;
                        }
                        else
                        {
                            link = null;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    trail.Add(new LinkInfo
                    {
                        Link = link,
                        Failed = true,
                        SpecialCase = "Request Timeout"
                    });
                    break;
                }
                catch (HttpRequestException ex)
                {
                    trail.Add(new LinkInfo
                    {
                        Link = link,
                        Failed = true,
                        SpecialCase = $"Network Error: {ex.Message}"
                    });
                    break;
                }
                catch (Exception ex)
                {
                    trail.Add(new LinkInfo
                    {
                        Link = $"{link}",
                        Failed = true
                    });
                    Console.WriteLine($"Exception while processing link {link}: {ex.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                    link = null;
                }
            }

            if (redirectCount > maximumRedirectCount)
            {
                trail.Add(new LinkInfo
                {
                    Link = $"Unknown (Bob follows **up to {maximumRedirectCount}** redirects.)",
                    Failed = true
                });
            }

            return trail;
        }
        private static bool HasRickRoll(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            // Extract video ID using a regex
            var videoIdMatch = MyRegex().Match(url);
            if (!videoIdMatch.Success)
            {
                return false;
            }

            string videoId = videoIdMatch.Groups[1].Value;

            // Check if the video ID matches known RickRoll IDs
            return videoId == "dQw4w9WgXcQ" || videoId == "Yb6dZ1IFlKc";
        }

        private static bool IsGitHubRepository(string url)
        {
            // GitHub repository URL pattern (example)
            string gitHubRepoPattern = @"https://github.com/.*/.*";
            return Regex.IsMatch(url, gitHubRepoPattern);
        }

        private static string GetUrlRedirectInformation(string url)
        {
            try
            {
                // Parse the URL
                Uri uri = new(url);
                string query = uri.Query;

                if (string.IsNullOrEmpty(query))
                {
                    return null;
                }

                // Parse query parameters
                var queryParameters = HttpUtility.ParseQueryString(query);

                // Check for common redirect keywords
                foreach (string key in queryParameters.AllKeys)
                {
                    if (key != null && key.Contains("redirect", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Return the value associated with the redirect key
                        return queryParameters[key];
                    }

                    // Correct check for the 'q' parameter (destination URL in YouTube)
                    if (key.Equals("q", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return queryParameters[key];
                    }
                }

                return null; // No redirect information found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing URL: {ex.Message}");
                return null;
            }
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
            string[] shortenerDomains = ["dis.gd", "v.gd", "ow.ly", "bl.ink", "3.ly", "tiny.cc", "bit.ly", "tinyurl.com", "goo.gl", "shorturl.at", "t.ly", "youtu.be", "y2u.be", "t.co", "short.gy", "snip.ly", "Buff.ly", "redd.it", "rb.gy", "msha.ke", "trib.al", "is.gd"];

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

        private static string GetNextUserAgent()
        {
            lock (_userAgentLock)
            {
                var userAgent = UserAgents[_userAgentIndex];
                _userAgentIndex = (_userAgentIndex + 1) % UserAgents.Count;
                return userAgent;
            }
        }

        [GeneratedRegex(@"(?:v=|\/)([a-zA-Z0-9_-]{11})")]
        private static partial Regex MyRegex();
    }
}