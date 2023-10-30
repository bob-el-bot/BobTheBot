using System.ComponentModel.DataAnnotations;

namespace Database.Types
{
    public class Server
    {
        [Key]
        public ulong Id { get; set; }

        public ulong? QuoteChannelId { get; set; }

        public bool Welcome { get; set; }
    }
}