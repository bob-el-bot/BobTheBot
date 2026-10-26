using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// GitLab puts the event type in the body as `object_kind`. In-progress pipelines and edits are ignored.
public class GitLabStrategy : WebhookStrategy
{
    public override string Provider => "gitlab";
    public override string ServiceName => "GitLab";
    public override string SetupHint =>
        "In your project go to **Settings → Webhooks → Add new webhook**, paste the URL, and tick the triggers you want.";

    private static readonly Color Orange = new(0xFC6D26);
    private static readonly Color Purple = new(0x6E49CB);

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var kind    = Str(root, "object_kind") ?? "unknown";
        var project = Str(root, "project.path_with_namespace") ?? "GitLab";
        var web     = Str(root, "project.web_url");

        var builder = kind switch
        {
            "push"          => Push(root, project, web),
            "tag_push"      => new EmbedBuilder()
                                   .WithTitle($"🏷️ [{project}] Tag `{Str(root, "ref")?.Replace("refs/tags/", "")}` pushed")
                                   .WithUrl(web).WithColor(Orange),
            "merge_request" => MergeRequest(root, project),
            "issue"         => Issue(root, project),
            "note"          => Note(root, project),
            "pipeline"      => Pipeline(root, project, web),
            "release"       => Release(root, project),
            "deployment"    => Deployment(root, project),
            _               => new EmbedBuilder().WithTitle($"📦 [{project}] {Humanize(kind)}").WithUrl(web).WithColor(Orange),
        };

        if (builder == null) return null;

        var user = Str(root, "user.name", "user_name");
        if (user != null)
            builder.WithAuthor(user, Str(root, "user.avatar_url", "user_avatar"));

        return builder.WithFooter($"GitLab • {project}");
    }

    private static EmbedBuilder Push(JsonElement root, string project, string web)
    {
        var branch  = Str(root, "ref")?.Replace("refs/heads/", "");
        var commits = Get(root, "commits") is { ValueKind: JsonValueKind.Array } arr ? arr.EnumerateArray().ToList() : [];
        var total   = int.TryParse(Str(root, "total_commits_count"), out var t) ? t : commits.Count;
        if (total == 0) return null; // branch deletion / creation with no commits

        var sb = new StringBuilder();
        foreach (var c in commits.Take(5))
            sb.AppendLine($"[`{ShortSha(Str(c, "id"))}`]({Str(c, "url")}) {Truncate(FirstLine(Str(c, "message", "title")), 70)} by {Str(c, "author.name")}");
        if (total > 5) sb.AppendLine($"…and {total - 5} more");

        var before = Str(root, "before");
        var after  = Str(root, "after");

        return new EmbedBuilder()
            .WithTitle(Truncate($"[{project}:{branch}] {total} new commit{(total == 1 ? "" : "s")}", 256))
            .WithUrl(web != null && before != null && after != null ? $"{web}/-/compare/{before}...{after}" : web)
            .WithDescription(sb.ToString())
            .WithColor(Orange);
    }

    private static EmbedBuilder MergeRequest(JsonElement root, string project)
    {
        var (label, icon, color) = Str(root, "object_attributes.action") switch
        {
            "open"     => ("opened", "🟢", Color.Green),
            "reopen"   => ("reopened", "🟢", Color.Green),
            "merge"    => ("merged", "🟣", Purple),
            "close"    => ("closed", "🔴", Color.Red),
            "approved" => ("approved", "👍", Color.Green),
            _          => (null, null, default),
        };
        if (label == null) return null;

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} [{project}] MR !{Str(root, "object_attributes.iid")} {label}: {Str(root, "object_attributes.title")}", 256))
            .WithUrl(Str(root, "object_attributes.url"))
            .WithColor(color)
            .AddField("Branch", $"`{Str(root, "object_attributes.source_branch")}` → `{Str(root, "object_attributes.target_branch")}`", inline: true);

        if (label == "opened" && Str(root, "object_attributes.description") is { } desc)
            builder.WithDescription(Truncate(desc, 500));

        return builder;
    }

    private static EmbedBuilder Issue(JsonElement root, string project)
    {
        var action = Str(root, "object_attributes.action");
        var (label, icon, color) = action switch
        {
            "open"   => ("opened", "🟢", Color.Green),
            "reopen" => ("reopened", "🟢", Color.Green),
            "close"  => ("closed", "🔴", Color.Red),
            _        => (null, null, default),
        };
        if (label == null) return null;

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} [{project}] Issue #{Str(root, "object_attributes.iid")} {label}: {Str(root, "object_attributes.title")}", 256))
            .WithUrl(Str(root, "object_attributes.url"))
            .WithColor(color);

        if (action == "open" && Str(root, "object_attributes.description") is { } desc)
            builder.WithDescription(Truncate(desc, 500));

        return builder;
    }

    private static EmbedBuilder Note(JsonElement root, string project)
    {
        var target = Str(root, "object_attributes.noteable_type") switch
        {
            "MergeRequest" => $"MR !{Str(root, "merge_request.iid")}",
            "Issue"        => $"issue #{Str(root, "issue.iid")}",
            "Commit"       => $"commit `{ShortSha(Str(root, "commit.id"))}`",
            var other      => other ?? "item",
        };

        return new EmbedBuilder()
            .WithTitle(Truncate($"💬 [{project}] New comment on {target}", 256))
            .WithUrl(Str(root, "object_attributes.url"))
            .WithDescription(Truncate(Str(root, "object_attributes.note"), 500))
            .WithColor(Orange);
    }

    private static EmbedBuilder Pipeline(JsonElement root, string project, string web)
    {
        var status = Str(root, "object_attributes.status");
        if (status is not ("success" or "failed" or "canceled")) return null; // skip pending/running noise

        var (color, icon) = Status(status);
        var id = Str(root, "object_attributes.id");

        var builder = new EmbedBuilder()
            .WithTitle($"{icon} [{project}] Pipeline #{id} {status}")
            .WithUrl(Str(root, "object_attributes.url") ?? (web != null ? $"{web}/-/pipelines/{id}" : null))
            .WithColor(color)
            .AddField("Ref", $"`{Str(root, "object_attributes.ref")}`", inline: true)
            .AddField("Commit", $"`{ShortSha(Str(root, "object_attributes.sha"))}`", inline: true);

        if (Str(root, "object_attributes.duration") is { } duration)
            builder.AddField("Duration", $"{duration}s", inline: true);

        return builder;
    }

    private static EmbedBuilder Release(JsonElement root, string project)
    {
        if (Str(root, "action") is not ("create" or null)) return null;

        return new EmbedBuilder()
            .WithTitle(Truncate($"🚀 [{project}] Release {Str(root, "name", "tag")}", 256))
            .WithUrl(Str(root, "url"))
            .WithDescription(Truncate(Str(root, "description"), 1500))
            .AddField("Tag", $"`{Str(root, "tag")}`", inline: true)
            .WithColor(Color.Green);
    }

    private static EmbedBuilder Deployment(JsonElement root, string project)
    {
        var status = Str(root, "status");
        var (color, icon) = Status(status);

        return new EmbedBuilder()
            .WithTitle($"{icon} [{project}] Deployment to {Str(root, "environment")} {status}")
            .WithUrl(Str(root, "environment_external_url", "deployable_url"))
            .WithColor(color)
            .AddField("Commit", $"`{Str(root, "short_sha") ?? "?"}`", inline: true);
    }
}
