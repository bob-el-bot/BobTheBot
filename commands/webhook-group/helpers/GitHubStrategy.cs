using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// The event name comes from the X-GitHub-Event header; noisy actions (labeled, synchronize, etc.) are ignored.
public class GitHubStrategy : WebhookStrategy
{
    public override string Provider => "github";
    public override string ServiceName => "GitHub";
    public override string SetupHint =>
        "In your repo go to **Settings → Webhooks → Add webhook**, paste the URL, set **Content type** to `application/json`, and choose your events.";

    private static readonly Color Purple = new(0x8957E5);

    // GitHub's default content type is form-encoded: payload=<url-encoded json>
    protected override string Normalize(string body) =>
        body.StartsWith("payload=") ? WebUtility.UrlDecode(body["payload=".Length..]) : body;

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var evt    = headers["X-GitHub-Event"] ?? "unknown";
        var action = Str(root, "action");
        var repo   = Str(root, "repository.full_name") ?? "GitHub";

        var builder = evt switch
        {
            "ping"          => Ping(root, repo),
            "push"          => Push(root, repo),
            "pull_request"  => PullRequest(root, repo, action),
            "issues"        => Issue(root, repo, action),
            "issue_comment" => action == "created" ? Comment(root, repo) : null,
            "release"       => action == "published" ? Release(root, repo) : null,
            "workflow_run"  => action == "completed" ? WorkflowRun(root, repo) : null,
            "star"          => action == "created" ? Star(root, repo) : null,
            "fork"          => Fork(root, repo),
            "create" or "delete" => RefChange(root, repo, evt),
            _               => Other(root, repo, evt, action),
        };

        if (builder == null) return null;

        var login = Str(root, "sender.login");
        if (login != null)
            builder.WithAuthor(login, Str(root, "sender.avatar_url"), Str(root, "sender.html_url"));

        return builder.WithFooter($"GitHub • {repo}");
    }

    private static EmbedBuilder Ping(JsonElement root, string repo) => new EmbedBuilder()
        .WithTitle($"🔗 Webhook connected to {repo}")
        .WithDescription(Str(root, "zen") is { } zen ? $"*{zen}*" : null)
        .WithUrl(Str(root, "repository.html_url"))
        .WithColor(Color.Green);

    private static EmbedBuilder Push(JsonElement root, string repo)
    {
        var gitRef = Str(root, "ref") ?? "";
        var isTag  = gitRef.StartsWith("refs/tags/");
        var name   = gitRef.Replace("refs/heads/", "").Replace("refs/tags/", "");

        if (Bool(root, "deleted") == true)
            return new EmbedBuilder()
                .WithTitle($"🗑️ [{repo}] {(isTag ? "Tag" : "Branch")} `{name}` deleted")
                .WithColor(Color.LightGrey);

        if (isTag)
            return new EmbedBuilder()
                .WithTitle($"🏷️ [{repo}] Tag `{name}` pushed")
                .WithUrl(Str(root, "compare"))
                .WithColor(Bob.Bot.theme);

        var commits = Get(root, "commits") is { ValueKind: JsonValueKind.Array } arr ? arr.EnumerateArray().ToList() : [];
        if (commits.Count == 0) return null; // e.g. a branch created with no new commits

        var sb = new StringBuilder();
        foreach (var c in commits.Take(5))
        {
            var sha = ShortSha(Str(c, "id"));
            sb.AppendLine($"[`{sha}`]({Str(c, "url")}) {Truncate(FirstLine(Str(c, "message")), 70)} by {Str(c, "author.username", "author.name")}");
        }
        if (commits.Count > 5) sb.AppendLine($"…and {commits.Count - 5} more");

        return new EmbedBuilder()
            .WithTitle(Truncate($"[{repo}:{name}] {commits.Count} new commit{(commits.Count == 1 ? "" : "s")}", 256))
            .WithUrl(Str(root, "compare"))
            .WithDescription(sb.ToString())
            .WithColor(Bob.Bot.theme);
    }

    private static EmbedBuilder PullRequest(JsonElement root, string repo, string action)
    {
        var merged = Bool(root, "pull_request.merged") == true;
        var (label, icon, color) = action switch
        {
            "opened"           => ("opened", "🟢", Color.Green),
            "reopened"         => ("reopened", "🟢", Color.Green),
            "ready_for_review" => ("ready for review", "👀", Color.Green),
            "closed" when merged => ("merged", "🟣", Purple),
            "closed"           => ("closed", "🔴", Color.Red),
            _                  => (null, null, default),
        };
        if (label == null) return null;

        var number = Str(root, "pull_request.number");
        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} [{repo}] PR #{number} {label}: {Str(root, "pull_request.title")}", 256))
            .WithUrl(Str(root, "pull_request.html_url"))
            .WithColor(color)
            .AddField("Branch", $"`{Str(root, "pull_request.head.ref")}` → `{Str(root, "pull_request.base.ref")}`", inline: true);

        if (action == "opened")
        {
            var body = Str(root, "pull_request.body");
            if (body != null) builder.WithDescription(Truncate(body, 500));
            builder.AddField("Changes", $"+{Str(root, "pull_request.additions") ?? "?"} / -{Str(root, "pull_request.deletions") ?? "?"}", inline: true);
        }

        return builder;
    }

    private static EmbedBuilder Issue(JsonElement root, string repo, string action)
    {
        var (icon, color) = action switch
        {
            "opened" or "reopened" => ("🟢", Color.Green),
            "closed"               => ("🔴", Color.Red),
            _                      => (null, default),
        };
        if (icon == null) return null;

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{icon} [{repo}] Issue #{Str(root, "issue.number")} {action}: {Str(root, "issue.title")}", 256))
            .WithUrl(Str(root, "issue.html_url"))
            .WithColor(color);

        if (action == "opened" && Str(root, "issue.body") is { } body)
            builder.WithDescription(Truncate(body, 500));

        return builder;
    }

    private static EmbedBuilder Comment(JsonElement root, string repo) => new EmbedBuilder()
        .WithTitle(Truncate($"💬 [{repo}] New comment on #{Str(root, "issue.number")}: {Str(root, "issue.title")}", 256))
        .WithUrl(Str(root, "comment.html_url"))
        .WithDescription(Truncate(Str(root, "comment.body"), 500))
        .WithColor(Bob.Bot.theme);

    private static EmbedBuilder Release(JsonElement root, string repo)
    {
        var tag  = Str(root, "release.tag_name");
        var name = Str(root, "release.name");
        var pre  = Bool(root, "release.prerelease") == true;

        return new EmbedBuilder()
            .WithTitle(Truncate($"🚀 [{repo}] {(pre ? "Pre-release" : "Release")} {name ?? tag}", 256))
            .WithUrl(Str(root, "release.html_url"))
            .WithDescription(Truncate(Str(root, "release.body"), 1500))
            .AddField("Tag", $"`{tag}`", inline: true)
            .WithColor(pre ? Color.Orange : Color.Green);
    }

    private static EmbedBuilder WorkflowRun(JsonElement root, string repo)
    {
        var conclusion = Str(root, "workflow_run.conclusion");
        var (color, icon) = Status(conclusion);

        return new EmbedBuilder()
            .WithTitle(Truncate($"{icon} [{repo}] {Str(root, "workflow_run.name")} {conclusion}", 256))
            .WithUrl(Str(root, "workflow_run.html_url"))
            .WithColor(color)
            .AddField("Branch", $"`{Str(root, "workflow_run.head_branch")}`", inline: true)
            .AddField("Commit", $"`{ShortSha(Str(root, "workflow_run.head_sha"))}`", inline: true)
            .AddField("Trigger", Str(root, "workflow_run.event") ?? "?", inline: true);
    }

    private static EmbedBuilder Star(JsonElement root, string repo) => new EmbedBuilder()
        .WithTitle($"⭐ [{repo}] New star!")
        .WithUrl(Str(root, "repository.html_url"))
        .WithDescription($"Now at **{Str(root, "repository.stargazers_count")}** stars.")
        .WithColor(Color.Gold);

    private static EmbedBuilder Fork(JsonElement root, string repo) => new EmbedBuilder()
        .WithTitle($"🍴 [{repo}] Forked to {Str(root, "forkee.full_name")}")
        .WithUrl(Str(root, "forkee.html_url"))
        .WithColor(Bob.Bot.theme);

    private static EmbedBuilder RefChange(JsonElement root, string repo, string evt) => new EmbedBuilder()
        .WithTitle($"{(evt == "create" ? "🌱" : "🗑️")} [{repo}] {Str(root, "ref_type")} `{Str(root, "ref")}` {evt}d")
        .WithColor(evt == "create" ? Color.Green : Color.LightGrey);

    private static EmbedBuilder Other(JsonElement root, string repo, string evt, string action) => new EmbedBuilder()
        .WithTitle(Truncate($"📦 [{repo}] {Humanize(evt)}{(action != null ? $" {action}" : "")}", 256))
        .WithUrl(Str(root, "repository.html_url"))
        .WithColor(Bob.Bot.theme);
}
