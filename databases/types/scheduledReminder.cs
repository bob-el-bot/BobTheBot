using System;
using System.ComponentModel.DataAnnotations;
using Bob.Commands.Helpers;

namespace Bob.Database.Types
{
    public class ScheduledReminder : IScheduledItem
    {
        [Key]
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public ulong ChannelId => 0; // Not used; reminders are delivered via DM
        public string Message { get; set; }
        public DateTime TimeToSend { get; set; }
    }
}
