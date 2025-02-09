using System.Linq;
using System.Text.RegularExpressions;

namespace Bob.Commands.Helpers
{
    public static class GitHubLinkParse
    {
        public enum GitHubLinkType
        {
            Unknown,
            CodeFile,
            PullRequest,
            Issue
        }

        public class GitHubLink
        {
            public string Url { get; set; }
            public GitHubLinkType Type { get; set; }
        }

        /// <summary>
        /// Gets GitHub link from the given message.
        /// </summary>
        /// <param name="message">The message to search for GitHub links.</param>
        /// <returns>
        /// A GitHubLink object containing the URL and type if a GitHub link is found in the message;
        /// otherwise, returns null.
        /// </returns>
        public static GitHubLink GetUrl(string message)
        {
            string pattern = @"(https?://\S+)|(www\.\S+)";

            MatchCollection matches = Regex.Matches(message, pattern);

            foreach (Match match in matches.Cast<Match>())
            {
                string url = match.Value;
                GitHubLinkType type = GetLinkType(url);

                if (type != GitHubLinkType.Unknown)
                {
                    return new GitHubLink { Url = url, Type = type };
                }
            }

            // No GitHub links found in the message
            return null;
        }

        private static GitHubLinkType GetLinkType(string url)
        {
            if (IsCodeFileUrl(url))
            {
                return GitHubLinkType.CodeFile;
            }
            else if (IsPullRequestUrl(url))
            {
                return GitHubLinkType.PullRequest;
            }
            else if (IsIssueUrl(url))
            {
                return GitHubLinkType.Issue;
            }
            else
            {
                return GitHubLinkType.Unknown;
            }
        }

        private static bool IsGitHubUrl(string url)
        {
            return url.StartsWith("https://github.com/") || url.StartsWith("http://github.com/");
        }

        private static bool IsCodeFileUrl(string url)
        {
            return url.Contains("/blob/");
        }

        private static bool IsPullRequestUrl(string url)
        {
            return url.Contains("/pull/");
        }

        private static bool IsIssueUrl(string url)
        {
            return url.Contains("/issues/");
        }
    }
}
