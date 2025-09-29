using System.Collections.Generic;

namespace BobTheBot.Chat.TemporalHandling;

using System.Text.RegularExpressions;

public static class QueryNormalizer
{
    private static readonly Dictionary<string, string> Replacements = new()
    {
        { "yesturday", "yesterday" },
        { "tmrw", "tomorrow" },
        { "tmr", "tomorrow" },
        { "tomoro", "tomorrow" },
        { "wk", "week" },
        { "mo", "month" },
        { "mon", "month" },
        { "yr", "year" },
        { "yer", "year" },
        { "2d", "2 days" },
        { "3d", "3 days" }
    };

    public static string Normalize(string query)
    {
        query = query.ToLowerInvariant().Trim();

        foreach (var kvp in Replacements)
        {
            query = Regex.Replace(
                query,
                $@"\b{Regex.Escape(kvp.Key)}\b",
                kvp.Value,
                RegexOptions.IgnoreCase
            );
        }

        return query;
    }
}