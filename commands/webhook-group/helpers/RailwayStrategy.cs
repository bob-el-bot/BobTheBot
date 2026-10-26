using System.Collections.Specialized;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// Payload reference: https://docs.railway.com/guides/webhooks
// Current format: { type: "Deployment.failed", details: { status, branch, commit* }, resource: { project, service, environment }, severity }
// Older format (still accepted): { type: "DEPLOY", status, project, service, environment, deployment: { meta } }
public class RailwayStrategy : WebhookStrategy
{
    public override string Provider => "railway";
    public override string ServiceName => "Railway";
    public override string SetupHint =>
        "In Railway open your project's **Settings → Webhooks**, paste the URL, and save.";

    private static readonly Color Purple = new(0x853BCE);

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var type     = Str(root, "type") ?? "unknown";
        var status   = Str(root, "details.status", "status");
        var project  = Str(root, "resource.project.name", "project.name") ?? "Railway";
        var service  = Str(root, "resource.service.name", "service.name");
        var env      = Str(root, "resource.environment.name", "environment.name");
        var severity = Str(root, "severity");

        // "Deployment.failed" → "failed"; fall back to the status for the old format
        var evt = type.Contains('.') ? type[(type.IndexOf('.') + 1)..] : status;
        var (color, icon) = Status(status ?? evt ?? severity);
        if (color == Bob.Bot.theme) color = Purple;

        var subject = service != null ? $"{project} / {service}" : project;
        var label   = Humanize((type.Contains('.') ? type.Replace('.', ' ') : $"{type} {status}".Trim()).ToLowerInvariant());

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} {subject} — {label}", 256))
            .WithColor(color);

        var projectId = Str(root, "resource.project.id", "project.id");
        if (projectId != null) builder.WithUrl($"https://railway.com/project/{projectId}");

        if (env != null)      builder.AddField("Environment", env, inline: true);
        if (status != null)   builder.AddField("Status", $"`{status}`", inline: true);
        if (severity != null && severity != "INFO") builder.AddField("Severity", severity, inline: true);

        var branch = Str(root, "details.branch", "deployment.meta.branch");
        var sha    = Str(root, "details.commitHash", "deployment.meta.commitHash");
        var msg    = Str(root, "details.commitMessage", "deployment.meta.commitMessage");
        var author = Str(root, "details.commitAuthor", "deployment.meta.commitAuthor");

        if (branch != null) builder.AddField("Branch", $"`{branch}`", inline: true);
        if (sha != null)    builder.AddField("Commit", $"`{ShortSha(sha)}`", inline: true);
        if (msg != null)    builder.WithDescription(Truncate(FirstLine(msg), 300) + (author != null ? $"\n— {author}" : ""));

        return builder;
    }
}
