using System.Collections.Specialized;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// Payload reference: https://coolify.io/docs/core/notifications/channels/webhook/payload-reference
// Every event has `success`, `event` and `message`; the rest depends on the event.
public class CoolifyStrategy : WebhookStrategy
{
    public override string Provider => "coolify";
    public override string ServiceName => "Coolify";
    public override string SetupHint =>
        "In Coolify go to **Notifications → Webhook**, paste the URL, enable it, then pick the events you want.";

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var evt     = Str(root, "event") ?? "unknown";
        var success = Bool(root, "success") ?? true;
        var message = Str(root, "message");

        var (color, icon) = evt switch
        {
            "test"                           => (Bob.Bot.theme, "🧪"),
            "status_changed"                 => (Color.Gold, "🔁"),
            "container_restarted"            => (Color.Gold, "🔁"),
            "backup_success_with_s3_warning" => (Color.Orange, "⚠️"),
            "high_disk_usage"                => (Color.Orange, "💾"),
            "traefik_version_outdated"       => (Color.Orange, "⚠️"),
            "server_patch_check"             => (Color.Orange, "🩹"),
            _ => success ? (Color.Green, "✅") : (Color.Red, "❌"),
        };

        // Whatever the event is about: an app, database, task, container or server
        var subject = Str(root, "application_name", "database_name", "task_name", "container_name", "server_name");

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} {Label(evt)}{(subject != null ? $" — {subject}" : "")}", 256))
            .WithColor(color);

        if (message != null) builder.WithDescription(Truncate(message, 2000));

        var link = Str(root, "deployment_url", "url");
        if (link != null && link.StartsWith("http")) builder.WithUrl(link);

        AddIfPresent(builder, root, "Project", "project");
        AddIfPresent(builder, root, "Environment", "environment");
        AddIfPresent(builder, root, "Server", "server_name", skipIf: subject);
        AddIfPresent(builder, root, "Database Type", "database_type");
        AddIfPresent(builder, root, "Frequency", "frequency");

        var prId = Str(root, "pull_request_id");
        if (prId != null) builder.AddField("Pull Request", $"#{prId}", inline: true);

        var site = Str(root, "preview_fqdn", "fqdn");
        if (site != null)
        {
            var siteUrl = site.StartsWith("http") ? site : $"https://{site}";
            builder.AddField("Live At", $"[{site.Replace("https://", "").Replace("http://", "")}]({siteUrl})", inline: true);
        }

        var output = Str(root, "output", "cleanup_message");
        if (output != null) builder.AddField("Output", $"```\n{Truncate(output, 1000)}\n```");

        return builder;
    }

    private static void AddIfPresent(EmbedBuilder builder, JsonElement root, string label, string key, string skipIf = null)
    {
        var value = Str(root, key);
        if (value != null && value != skipIf)
            builder.AddField(label, Truncate(value, 1024), inline: true);
    }

    private static string Label(string evt) => evt switch
    {
        "deployment_success"             => "Deployed",
        "deployment_failed"              => "Deployment Failed",
        "status_changed"                 => "Status Changed",
        "container_stopped"              => "Container Stopped",
        "container_restarted"            => "Container Restarted",
        "restart_limit_reached"          => "Restart Limit Reached",
        "backup_success"                 => "Backup Succeeded",
        "backup_success_with_s3_warning" => "Backup Succeeded (S3 Warning)",
        "backup_failed"                  => "Backup Failed",
        "task_success"                   => "Scheduled Task Succeeded",
        "task_failed"                    => "Scheduled Task Failed",
        "docker_cleanup_success"         => "Docker Cleanup Done",
        "docker_cleanup_failed"          => "Docker Cleanup Failed",
        "server_reachable"               => "Server Reachable",
        "server_unreachable"             => "Server Unreachable",
        "high_disk_usage"                => "High Disk Usage",
        "server_patch_check"             => "Server Updates Available",
        "server_patch_check_error"       => "Server Patch Check Failed",
        "traefik_version_outdated"       => "Traefik Outdated",
        "test"                           => "Test Notification",
        _                                => Humanize(evt),
    };
}
