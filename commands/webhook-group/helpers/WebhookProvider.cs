using Discord.Interactions;

namespace Bob.Webhooks;

/// <summary>
/// Providers offered in /webhook create and /webhook edit. The lowercase name is the key stored in
/// WebhookInfo.Provider and matched by each strategy's <see cref="IWebhookStrategy.Provider"/>.
/// </summary>
public enum WebhookProvider
{
    GitHub,
    GitLab,
    Vercel,
    Railway,
    Netlify,
    Coolify,
    [ChoiceDisplay("Docker Hub")] DockerHub,
    [ChoiceDisplay("Generic (any JSON)")] Generic,
}

public static class WebhookProviderExtensions
{
    public static string Key(this WebhookProvider provider) => provider.ToString().ToLowerInvariant();
}
