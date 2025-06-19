using System.ComponentModel.DataAnnotations;

namespace Bob.Database.Types
{
    public class NewsChannel
    {
        [Key]
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }
    }
}