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
        public DateTime? Expiration { get; set; }
        public string Reason { get; set; }

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

            sb.AppendLine($"**Expiration:**\n{(Expiration.HasValue && Expiration != DateTime.MaxValue ? TimeStamp.FromDateTime((DateTime)Expiration, TimeStamp.Formats.Relative) : "Never | Permanent")}");
            sb.AppendLine($"**Reason(s):**\n{Reason}");

            return sb.ToString();
        }
    }
}