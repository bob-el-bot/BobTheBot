using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;

namespace Bob.Commands.Helpers
{
    public static class GitHubRepositoryReader
    {
        private static readonly HttpClient http = new();

        public static async Task<Embed> GetRepositoryEmbed(string owner, string repo)
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BobTheBot/1.0");

            string repoUrl = $"https://api.github.com/repos/{owner}/{repo}";
            using var repoResp = await http.GetAsync(repoUrl);
            if (!repoResp.IsSuccessStatusCode)
                throw new InvalidOperationException("Repository not found or unreachable.");

            string repoJson = await repoResp.Content.ReadAsStringAsync();
            using var repoDoc = JsonDocument.Parse(repoJson);
            var data = repoDoc.RootElement;

            string name = data.GetProperty("full_name").GetString() ?? "Unknown";
            string htmlUrl = data.GetProperty("html_url").GetString() ?? "";
            string description = data.TryGetProperty("description", out var desc)
                ? (desc.GetString() ?? "No description.")
                : "No description.";
            int stars = data.GetProperty("stargazers_count").GetInt32();
            int forks = data.GetProperty("forks_count").GetInt32();
            int watchers = data.GetProperty("subscribers_count").GetInt32();
            int openIssues = data.GetProperty("open_issues_count").GetInt32();
            string language = data.TryGetProperty("language", out var lang)
                ? (lang.GetString() ?? "N/A")
                : "N/A";

            string createdAt = DateTime.Parse(data.GetProperty("created_at").GetString()!)
                .ToString("yyyy-MM-dd");
            DateTime pushedAtDt = DateTime.Parse(data.GetProperty("pushed_at").GetString()!);
            string pushedAt = pushedAtDt.ToString("yyyy-MM-dd");

            string license = data.TryGetProperty("license", out var lic) && lic.ValueKind == JsonValueKind.Object
                ? lic.GetProperty("name").GetString() ?? "Unlicensed"
                : "Unlicensed";

            string ownerAvatar = data.GetProperty("owner").GetProperty("avatar_url").GetString();
            bool archived = data.TryGetProperty("archived", out var arch) && arch.GetBoolean();
            bool isPrivate = data.GetProperty("private").GetBoolean();
            string defaultBranch = data.TryGetProperty("default_branch", out var dfb)
                ? dfb.GetString()
                : "main";
            string homepage = data.TryGetProperty("homepage", out var home)
                ? home.GetString()
                : null;
            int sizeKb = data.TryGetProperty("size", out var sz) ? sz.GetInt32() : 0;

            string[] topics =
                data.TryGetProperty("topics", out var topicsProp) && topicsProp.ValueKind == JsonValueKind.Array
                    ? topicsProp.EnumerateArray().Select(t => t.GetString() ?? "").ToArray()
                    : Array.Empty<string>();

            using var langResp =
            await http.GetAsync($"https://api.github.com/repos/{owner}/{repo}/languages");
            string langJson = await langResp.Content.ReadAsStringAsync();
            using var langDoc = JsonDocument.Parse(langJson);

            string langStats = "Unavailable";
            if (langDoc.RootElement.ValueKind == JsonValueKind.Object &&
                langDoc.RootElement.EnumerateObject().Any())
            {
                var langs = langDoc.RootElement.EnumerateObject().ToList();
                double total = langs.Sum(l => (double)l.Value.GetInt64());

                var usedColors = new HashSet<string>();

                var topLangs = langs
                    .OrderByDescending(l => l.Value.GetInt64())
                    .Take(5)
                    .Select(l =>
                    {
                        string name = l.Name;
                        double percentage = 100.0 * l.Value.GetInt64() / total;
                        string color = AssignUniqueColor(name, usedColors);
                        string bar = BuildLanguageBar(color, percentage);
                        return $"{name,-12} {bar} {percentage:0.#}%";
                    });

                langStats = $"```\n{string.Join('\n', topLangs)}\n```";
            }

            using var contribResp = await http.GetAsync($"https://api.github.com/repos/{owner}/{repo}/contributors?per_page=100");
            string contribJson = await contribResp.Content.ReadAsStringAsync();
            using var contribDoc = JsonDocument.Parse(contribJson);
            int contributors = contribDoc.RootElement.ValueKind == JsonValueKind.Array
                ? contribDoc.RootElement.GetArrayLength()
                : 0;

            (int recentAdds, int recentDels) = await GetRecentCodeFreqAsync(owner, repo);

            var builder = new EmbedBuilder()
                .WithTitle($"{name}{(archived ? " 📜 [Archived]" : string.Empty)}")
                .WithUrl(htmlUrl)
                .WithDescription(description)
                .WithThumbnailUrl(ownerAvatar)
                .WithColor(Bot.theme);

            string visibility = isPrivate ? "🔒 Private" : "🔓 Public";
            string activity = (DateTime.UtcNow - pushedAtDt).TotalDays < 30 ? "🟢 Active" : "🟡 Dormant";
            string branchBadge = $"🧭 Default Branch: `{defaultBranch}`";

            builder
                .AddField("ℹ️ Info", $"{visibility} • {activity} • {branchBadge}")
                .AddField("⭐ Stars", stars.ToString(), true)
                .AddField("🍴 Forks", forks.ToString(), true)
                .AddField("🐛 Issues", openIssues.ToString(), true)
                .AddField("👥 Contributors", contributors.ToString(), true)
                .AddField("💻 Languages", langStats);

            if (!string.IsNullOrWhiteSpace(homepage))
                builder.AddField("🌐 Website", $"[Visit]({homepage})", true);

            builder
                .AddField("📦 Size", $"{(sizeKb / 1024.0):0.0} MB", true)
                .AddField("📅 Created", createdAt, true)
                .AddField("🕒 Last Push", pushedAt, true)
                .AddField("📄 License", license, true);

            if (topics.Length > 0)
                builder.AddField("🧩 Topics", string.Join(", ", topics.Take(5)), false);

            if (recentAdds + recentDels > 0)
            {
                builder
                    .AddField("📈 Recent Additions", recentAdds.ToString("N0"), true)
                    .AddField("📉 Recent Deletions", recentDels.ToString("N0"), true);
            }

            builder.Footer = new EmbedFooterBuilder()
            {
                Text = "⚠️ Historical code-frequency data reflects cumulative changes, not exact SLOC."
            };

            return builder.Build();
        }

        private static readonly string[] ColorEmojis =
        [
            "🟥", "🟧", "🟨", "🟩", "🟦", "🟪", "🟫", "⬛", "⬜"
        ];

        private static string AssignUniqueColor(string langName, HashSet<string> usedColors)
        {
            if (string.IsNullOrWhiteSpace(langName))
                langName = "unknown";

            string key = langName.Trim().ToLowerInvariant();

            string preferred = key switch
            {
                "c#" or "csharp" => "🟪",
                "typescript" or "javascript" => "🟨",
                "python" or "shell" or "bash" => "🟩",
                "html" or "css" => "🟧",
                "java" or "kotlin" => "🟫",
                "rust" or "svelte" => "🟥",
                "go" or "golang" or "dart" => "🟦",
                "dockerfile" or "makefile" => "⬛",
                "json" or "yaml" or "toml" => "⬜",
                _ => null
            };

            if (preferred is not null && usedColors.Add(preferred))
                return preferred;

            foreach (string emoji in ColorEmojis)
            {
                if (usedColors.Add(emoji))
                    return emoji;
            }

            int index = Math.Abs(key.GetHashCode()) % ColorEmojis.Length;
            return ColorEmojis[index];
        }

        private static string BuildLanguageBar(string colorEmoji, double percentage)
        {
            int segments = (int)Math.Round(percentage / 10);
            segments = Math.Clamp(segments, 0, 10);

            string empty = "⬜";
            return string.Concat(Enumerable.Repeat(colorEmoji, segments)) +
                   string.Concat(Enumerable.Repeat(empty, 10 - segments));
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.#} {sizes[order]}";
        }

        private static async Task<(int adds, int dels)> GetRecentCodeFreqAsync(string owner, string repo)
        {
            string url = $"https://api.github.com/repos/{owner}/{repo}/stats/code_frequency";
            using var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return (0, 0);

            string json = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart().StartsWith("{"))
                return (0, 0); // 202 Accepted or empty

            using var doc = JsonDocument.Parse(json);
            var weeks = doc.RootElement.EnumerateArray().ToList();
            var recentWeeks = weeks.TakeLast(4); // last 4 weeks

            int adds = 0, dels = 0;
            foreach (var week in recentWeeks)
            {
                adds += week[1].GetInt32();
                dels += Math.Abs(week[2].GetInt32());
            }
            return (adds, dels);
        }
    }
}