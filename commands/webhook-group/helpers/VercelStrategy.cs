using System.Collections.Specialized;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// Payload reference: https://vercel.com/docs/webhooks/webhooks-api
// Body is { type, id, createdAt, payload: { deployment: { name, url, meta }, target, links: { deployment, project } } }
public class VercelStrategy : WebhookStrategy
{
    public override string Provider => "vercel";
    public override string ServiceName => "Vercel";
    public override string SetupHint =>
        "In Vercel open **Team Settings → Webhooks** (Pro/Enterprise plans), paste the URL, and select the deployment events and projects.";

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var type = Str(root, "type") ?? "unknown";
        var name = Str(root, "payload.deployment.name", "payload.name", "payload.project.name") ?? "Vercel";

        var (label, icon, color) = type switch
        {
            "deployment.created"   => ("Deploying", "🔄", Color.Gold),
            "deployment.succeeded" => ("Deployed", "✅", Color.Green),
            "deployment.ready"     => ("Ready", "✅", Color.Green),
            "deployment.promoted"  => ("Promoted to Production", "🚀", Color.Green),
            "deployment.error"     => ("Deployment Failed", "❌", Color.Red),
            "deployment.canceled"  => ("Deployment Canceled", "⏹", Color.LightGrey),
            _                      => (Humanize(type), "▲", Color.DarkGrey),
        };

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} {name} — {label}", 256))
            .WithColor(color)
            .WithUrl(Str(root, "payload.links.deployment"));

        var target = Str(root, "payload.target", "payload.deployment.target");
        builder.AddField("Environment", target != null ? Humanize(target) : "Preview", inline: true);

        // Git metadata Vercel attaches to deployments from a connected repo
        var branch = Str(root, "payload.deployment.meta.githubCommitRef", "payload.deployment.meta.gitlabCommitRef", "payload.deployment.meta.bitbucketCommitRef");
        var sha    = Str(root, "payload.deployment.meta.githubCommitSha", "payload.deployment.meta.gitlabCommitSha", "payload.deployment.meta.bitbucketCommitSha");
        var msg    = Str(root, "payload.deployment.meta.githubCommitMessage", "payload.deployment.meta.gitlabCommitMessage", "payload.deployment.meta.bitbucketCommitMessage");
        var author = Str(root, "payload.deployment.meta.githubCommitAuthorName", "payload.deployment.meta.gitlabCommitAuthorName", "payload.deployment.meta.bitbucketCommitAuthorName");

        if (branch != null) builder.AddField("Branch", $"`{branch}`", inline: true);
        if (sha != null)    builder.AddField("Commit", $"`{ShortSha(sha)}`", inline: true);
        if (msg != null)    builder.WithDescription(Truncate(FirstLine(msg), 300) + (author != null ? $"\n— {author}" : ""));

        var url = Str(root, "payload.deployment.url");
        if (url != null && type is "deployment.succeeded" or "deployment.ready" or "deployment.promoted")
            builder.AddField("Live At", $"[{url}](https://{url})", inline: false);

        return builder;
    }
}
