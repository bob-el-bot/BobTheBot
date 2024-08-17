using System;
using System.ComponentModel.DataAnnotations;
using Commands.Helpers;

namespace Database.Types
{
    public class ScheduledAnnouncement : IScheduledItem
    {
        [Key]
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public DateTime TimeToSend { get; set; }
    }
}