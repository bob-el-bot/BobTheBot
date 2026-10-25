using System.Collections.Specialized;
using Discord;

namespace Bob.Webhooks;

public interface IWebhookStrategy
{
    string Provider { get; }      // Key stored in WebhookInfo.Provider
    string ServiceName { get; }   // Display name
    string SetupHint { get; }     // Shown in /webhook create to explain where to paste the URL
    bool CanHandle(string provider);

    /// <summary>Returns the embed to post, or null if this event should be ignored.</summary>
    Embed BuildEmbed(string body, NameValueCollection headers);
}
