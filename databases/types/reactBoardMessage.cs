using System.ComponentModel.DataAnnotations;

namespace Bob.Database.Types
{
    public class ReactBoardMessage
    {
        [Key]
        public ulong OriginalMessageId { get; set; }
        public ulong GuildId { get; set; }
    }
}