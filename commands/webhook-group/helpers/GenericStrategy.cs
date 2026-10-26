using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using Discord;
using static Bob.Webhooks.WebhookJson;

namespace Bob.Webhooks;

/// <summary>
/// Best-effort formatter for any JSON payload: picks out common title / message / status / url
/// keys and turns the remaining simple values into embed fields.
/// </summary>
public class GenericStrategy : WebhookStrategy
{
    public override string Provider => "generic";
    public override string ServiceName => "Webhook";
    public override string SetupHint =>
        "Send a `POST` with a JSON body. Keys like `title`, `message`, `status`, `url` and `color` are picked up automatically; everything else becomes a field.";

    private static readonly string[] TitleKeys   = ["title", "subject", "name", "event", "type", "alert", "action"];
    private static readonly string[] MessageKeys = ["message", "text", "description", "body", "content", "summary", "details"];
    private static readonly string[] UrlKeys     = ["url", "link", "html_url", "href"];
    private static readonly string[] StatusKeys  = ["status", "state", "level", "severity", "result", "conclusion"];
    private static readonly string[] ImageKeys   = ["image", "image_url", "thumbnail"];

    private const int MaxFields = 15;

    protected override EmbedBuilder Build(JsonElement root, NameValueCollection headers)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return RawPreview(root.GetRawText()).WithTitle("Incoming Webhook");

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string Take(string[] keys)
        {
            foreach (var key in keys)
            {
                var value = Str(root, key);
                if (value != null) { used.Add(key); return value; }
            }
            return null;
        }

        var title   = Take(TitleKeys);
        var message = Take(MessageKeys);
        var url     = Take(UrlKeys);
        var status  = Take(StatusKeys);
        var image   = Take(ImageKeys);
        var success = Bool(root, "success");
        if (success != null) used.Add("success");

        var (color, icon) = Status(status ?? (success == null ? null : success.Value ? "success" : "failed"));
        if (TryParseColor(Str(root, "color") ?? Str(root, "colour"), out var custom))
        {
            color = custom;
            used.Add("color"); used.Add("colour");
        }

        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"{(status != null || success != null ? icon + " " : "")}{title ?? "Incoming Webhook"}", 256))
            .WithColor(color);

        if (message != null) builder.WithDescription(Truncate(message, 4000));
        if (IsHttpUrl(url)) builder.WithUrl(url);
        if (IsHttpUrl(image)) builder.WithThumbnailUrl(image);
        if (status != null) builder.AddField("Status", $"`{Truncate(status, 100)}`", inline: true);

        // Flatten remaining simple values (one level of nesting) into fields
        var fields = new List<(string Name, string Value)>();
        foreach (var prop in root.EnumerateObject())
        {
            if (used.Contains(prop.Name)) continue;
            Collect(prop.Name, prop.Value, depth: 0, fields);
        }

        foreach (var (name, value) in fields.Take(MaxFields))
            builder.AddField(Truncate(Humanize(name), 256), Truncate(value, 1024), inline: value.Length <= 40);

        if (fields.Count > MaxFields)
            builder.WithFooter($"Webhook • {fields.Count - MaxFields} more field(s) not shown");

        // Nothing usable at all — show the payload instead of an empty card
        if (message == null && builder.Fields.Count == 0)
            builder.WithDescription($"```json\n{Truncate(Pretty(root.GetRawText()), 3900)}\n```");

        return builder;
    }

    private static void Collect(string name, JsonElement value, int depth, List<(string, string)> fields)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object when depth == 0:
                foreach (var child in value.EnumerateObject())
                    Collect($"{name} {child.Name}", child.Value, depth + 1, fields);
                break;

            case JsonValueKind.Array:
                var items = value.EnumerateArray().ToList();
                if (items.Count > 0 && items.All(i => Scalar(i) != null))
                    fields.Add((name, string.Join(", ", items.Select(Scalar))));
                else if (items.Count > 0)
                    fields.Add((name, $"{items.Count} item(s)"));
                break;

            case JsonValueKind.Null or JsonValueKind.Undefined or JsonValueKind.Object:
                break;

            default:
                var text = Scalar(value);
                if (!string.IsNullOrWhiteSpace(text)) fields.Add((name, text));
                break;
        }
    }

    private static bool IsHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool TryParseColor(string value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var hex = value.TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            color = new Color(rgb);
            return true;
        }
        if (uint.TryParse(value, out var dec) && dec <= 0xFFFFFF)
        {
            color = new Color(dec);
            return true;
        }
        return false;
    }
}
