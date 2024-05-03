using System.ComponentModel.DataAnnotations;

namespace Database.Types
{
    public class Server
    {
        [Key]
        public ulong Id { get; set; }

        public ulong? QuoteChannelId { get; set; }
        public uint? MaxQuoteLength { get; set; }
        public uint MinQuoteLength { get; set; }

        public bool Welcome { get; set; }
        public string CustomWelcomeMessage { get; set; }

        public bool AutoEmbedGitHubLinks { get; set; }
        public bool AutoEmbedMessageLinks { get; set; }

        // Constructor to set default values
        public Server()
        {
            MaxQuoteLength = 4096;
            MinQuoteLength = 0;
        }
    }
}