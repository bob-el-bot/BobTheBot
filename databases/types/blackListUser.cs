using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TimeStamps;

namespace Database.Types
{
    public class BlackListUser
    {
        [Key]
        public ulong Id { get; set; }
        public DateTime? _expiration;
        public string Reason { get; set; }

        public DateTime? Expiration
        {
            get => _expiration;
            set => _expiration = value?.ToUniversalTime();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"**Expiration:**\n{(Expiration.HasValue && Expiration != DateTime.MaxValue ? Expiration.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never | Permanent")}");
            sb.AppendLine($"**Reason(s):**\n{Reason}");

            return sb.ToString();
        }

        public string FormatAsString()
        {
            StringBuilder sb = new();

            // Ensure Expiration is in UTC
            DateTime? expirationUtc = Expiration;

            string expirationString = expirationUtc.HasValue && expirationUtc != DateTime.MaxValue
                ? TimeStamp.FromDateTime((DateTime)expirationUtc, TimeStamp.Formats.Relative)
                : "Never | Permanent";

            sb.AppendLine($"**Expiration:**\n{expirationString}");
            sb.AppendLine($"**Reason(s):**\n{Reason}");

            return sb.ToString();
        }
    }
}