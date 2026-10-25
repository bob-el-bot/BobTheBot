using System;
using System.Collections.Specialized;
using System.Text.Json;
using Discord;

namespace Bob.Webhooks;

/// <summary>
/// Base for JSON-based providers. Handles parsing, footer/timestamp defaults,
/// and falls back to a raw preview if the payload isn't what we expected.
/// </summary>
public abstract class WebhookStrategy : IWebhookStrategy
{
    public abstract string Provider { get; }
    public abstract string ServiceName { get; }
    public abstract string SetupHint { get; }

    public bool CanHandle(string provider) => string.Equals(provider, Provider, StringComparison.OrdinalIgnoreCase);

    /// <summary>Build the embed from a parsed payload. Return null to ignore the event.</summary>
    protected abstract EmbedBuilder Build(JsonElement root, NameValueCollection headers);

    /// <summary>Hook for providers that wrap their JSON (e.g. GitHub's form-encoded mode).</summary>
    protected virtual string Normalize(string body) => body;

    public Embed BuildEmbed(string body, NameValueCollection headers)
    {
        body = Normalize(body);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var builder = Build(doc.RootElement, headers);
            if (builder == null) return null;

            if (builder.Footer == null) builder.WithFooter(ServiceName);
            if (builder.Timestamp == null) builder.WithCurrentTimestamp();

            return builder.Build();
        }
        catch (Exception ex)
        {
            // Non-JSON body, unexpected shape, or an embed over Discord's limits
            if (ex is not JsonException)
                Console.WriteLine($"[Webhook:{Provider}] Falling back to raw preview: {ex.Message}");

            return RawPreview(body).Build();
        }
    }

    protected EmbedBuilder RawPreview(string body) => new EmbedBuilder()
        .WithTitle($"{ServiceName} Event")
        .WithDescription($"```json\n{WebhookJson.Truncate(WebhookJson.Pretty(body), 3900)}\n```")
        .WithColor(Bob.Bot.theme)
        .WithFooter(ServiceName)
        .WithCurrentTimestamp();
}

public static class WebhookJson
{
    /// <summary>Walks a dotted path like "payload.deployment.url".</summary>
    public static JsonElement? Get(JsonElement el, string path)
    {
        foreach (var key in path.Split('.'))
        {
            if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(key, out el))
                return null;
        }
        return el;
    }

    /// <summary>First non-empty string/number/bool value among the given paths.</summary>
    public static string Str(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            var value = Get(root, path);
            if (value == null) continue;

            var text = Scalar(value.Value);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return null;
    }

    public static bool? Bool(JsonElement root, string path) => Get(root, path)?.ValueKind switch
    {
        JsonValueKind.True  => true,
        JsonValueKind.False => false,
        _                   => null,
    };

    public static string Scalar(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        _                    => null,
    };

    public static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
        return text[..(max - 1)] + "…";
    }

    public static string FirstLine(string text) => text?.Split('\n')[0].Trim();

    public static string ShortSha(string sha) => string.IsNullOrEmpty(sha) ? sha : sha[..Math.Min(7, sha.Length)];

    public static string Pretty(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException) { return json; }
    }

    /// <summary>Maps common status words to a colour and icon.</summary>
    public static (Color Color, string Icon) Status(string status) => status?.ToLowerInvariant().Replace(' ', '_') switch
    {
        "success" or "succeeded" or "successful" or "ready" or "passed" or "ok" or "completed"
            or "deployed" or "finished" or "resolved" or "up" or "healthy" or "active"
            => (Color.Green, "✅"),
        "failed" or "failure" or "fail" or "error" or "errored" or "crashed" or "critical"
            or "down" or "fatal" or "unhealthy"
            => (Color.Red, "❌"),
        "warning" or "warn" or "degraded"
            => (Color.Orange, "⚠️"),
        "running" or "building" or "deploying" or "pending" or "queued" or "enqueued"
            or "in_progress" or "started" or "initializing" or "created" or "waiting"
            => (Color.Gold, "🔄"),
        "canceled" or "cancelled" or "stopped" or "skipped" or "removed" or "exited" or "sleeping"
            => (Color.LightGrey, "⏹"),
        _ => (Bob.Bot.theme, "📦"),
    };

    /// <summary>"application_name" / "commitHash" → "Application Name" / "Commit Hash".</summary>
    public static string Humanize(string key)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (c is '_' or '-' or '.') { sb.Append(' '); continue; }
            if (i > 0 && char.IsUpper(c) && char.IsLower(key[i - 1])) sb.Append(' ');
            sb.Append(sb.Length == 0 || sb[^1] == ' ' ? char.ToUpperInvariant(c) : c);
        }
        return sb.ToString();
    }
}
