using System.Linq;
using System.Text.RegularExpressions;

namespace Bob.Commands.Helpers
{
    public static class DiscordMessageLinkParse
    {
        public enum DiscordLinkType
        {
            Unknown,
            ChannelMessage
        }

        public class DiscordLink
        {
            public string Url { get; set; }
            public DiscordLinkType Type { get; set; }
        }

        /// <summary>
        /// Gets Discord message link from the given message.
        /// </summary>
        /// <param name="message">The message to search for Discord message links.</param>
        /// <returns>
        /// A DiscordLink object containing the URL and type if a Discord message link is found in the message;
        /// otherwise, returns null.
        /// </returns>
        public static DiscordLink GetUrl(string message)
        {
            string pattern = @"(https?://discord\.com/channels/\d+/\d+/\d+)";

            MatchCollection matches = Regex.Matches(message, pattern);

            foreach (Match match in matches.Cast<Match>())
            {
                string url = match.Value;

                string[] parts = url.Split('/');
                ulong guildId = ulong.Parse(parts[4]);

                if (Bot.Client.GetGuild(guildId) != null)
                {
                    DiscordLinkType type = GetLinkType(url);

                    if (type != DiscordLinkType.Unknown)
                    {
                        return new DiscordLink { Url = url, Type = type };
                    }
                }
            }

            // No Discord message links found in the message
            return null;
        }

        private static DiscordLinkType GetLinkType(string url)
        {
            if (IsChannelMessageUrl(url))
            {
                return DiscordLinkType.ChannelMessage;
            }
            else
            {
                return DiscordLinkType.Unknown;
            }
        }

        private static bool IsChannelMessageUrl(string url)
        {
            return url.Contains("https://discord.com/channels/") && Regex.IsMatch(url, @"https?://discord\.com/channels/\d+/\d+/\d+");
        }
    }
}
