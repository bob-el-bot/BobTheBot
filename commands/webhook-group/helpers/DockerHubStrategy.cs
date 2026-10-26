using System.Collections.Specialized;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

// Docker Hub sends { push_data: { tag, pusher, pushed_at }, repository: { repo_name, repo_url, ... } } on image push.
public class DockerHubStrategy : WebhookStrategy
{
    public override string Provider => "dockerhub";
    public override string ServiceName => "Docker Hub";
    public override string SetupHint =>
        "In Docker Hub open your repository's **Webhooks** tab, give it a name, paste the URL, and click **Create**.";

    private static readonly Color DockerBlue = new(0x1D63ED);

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        var repo   = Str(root, "repository.repo_name") ?? "image";
        var tag    = Str(root, "push_data.tag") ?? "latest";
        var pusher = Str(root, "push_data.pusher");

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"🐳 New image pushed: {repo}:{tag}", 256))
            .WithUrl(Str(root, "repository.repo_url"))
            .WithColor(DockerBlue)
            .AddField("Pull", $"```\ndocker pull {repo}:{tag}\n```");

        if (pusher != null) builder.AddField("Pushed By", pusher, inline: true);
        if (Str(root, "repository.star_count") is { } stars) builder.AddField("Stars", stars, inline: true);

        return builder;
    }
}
