using System;
using System.ComponentModel.DataAnnotations;

namespace Database.Types
{
    public class ScheduledMessage
    {
        [Key]
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public string Message { get; set; }
        public DateTime TimeToSend { get; set; }
    }
}