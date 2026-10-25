using System;
using System.ComponentModel.DataAnnotations;

namespace Bob.Database.Types;

public class WebhookInfo
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();  // Used as the /ingest/{id} URL segment

    [Required]
    public string Provider { get; set; }  // "coolify", "generic"

    [Required]
    public ulong GuildId { get; set; }

    [Required]
    public ulong ChannelId { get; set; }

    // Discord webhook credentials for posting notifications into the channel
    public ulong DiscordWebhookId { get; set; }
    public string DiscordWebhookToken { get; set; }

    public string DisplayName { get; set; }

    // Optional HMAC secret — if set, incoming requests must include a matching X-Hub-Signature header
    public string Secret { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ulong CreatedBy { get; set; }
}
