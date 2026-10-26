using System.Collections.Specialized;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// Netlify posts the deploy object itself: { name, state, deploy_ssl_url, admin_url, branch, commit_ref, title, context, error_message, ... }
public class NetlifyStrategy : WebhookStrategy
{
    public override string Provider => "netlify";
    public override string ServiceName => "Netlify";
    public override string SetupHint =>
        "In Netlify open **Site configuration → Notifications → Deploy notifications → Add notification → HTTP POST request**, choose an event, and paste the URL. Add one per event you want.";

    private static readonly Color Teal = new(0x00AD9F);

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var name    = Str(root, "name") ?? "Netlify site";
        var state   = Str(root, "state");
        var context = Str(root, "context");

        var (label, icon, color) = state switch
        {
            "ready"                => ("Deployed", "✅", Color.Green),
            "error"                => ("Deploy Failed", "❌", Color.Red),
            "building" or "enqueued" or "processing" or "uploading" or "preparing"
                                   => ("Deploying", "🔄", Color.Gold),
            "new"                  => ("Deploy Started", "🔄", Color.Gold),
            _                      => (state != null ? Humanize(state) : "Deploy Update", "🌐", Teal),
        };

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} {name} | {label}", 256))
            .WithColor(color)
            .WithUrl(Str(root, "deploy_ssl_url", "ssl_url", "url"));

        if (context != null) builder.AddField("Context", Humanize(context), inline: true);
        if (Str(root, "branch") is { } branch) builder.AddField("Branch", $"`{branch}`", inline: true);

        var sha = Str(root, "commit_ref");
        if (sha != null)
        {
            var commitUrl = Str(root, "commit_url");
            builder.AddField("Commit", commitUrl != null ? $"[`{ShortSha(sha)}`]({commitUrl})" : $"`{ShortSha(sha)}`", inline: true);
        }

        var error = Str(root, "error_message");
        var title = Str(root, "title");
        if (state == "error" && error != null)
            builder.WithDescription($"```\n{Truncate(error, 1000)}\n```");
        else if (title != null)
            builder.WithDescription(Truncate(FirstLine(title), 300) + (Str(root, "committer") is { } who ? $"\nby {who}" : ""));

        var admin = Str(root, "admin_url");
        var id    = Str(root, "id");
        if (admin != null)
            builder.AddField("Logs", $"[Open in Netlify]({admin}{(id != null ? $"/deploys/{id}" : "")})", inline: true);

        return builder;
    }
}
