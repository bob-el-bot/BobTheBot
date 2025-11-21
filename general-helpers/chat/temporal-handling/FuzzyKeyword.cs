using System;

namespace BobTheBot.Chat.TemporalHandling;

public static class FuzzyKeyword
{
    private static readonly string[] Keywords =
    {
        "yesterday", "today", "tomorrow",
        "last week", "the last week", "past week", "previous week",
        "last month", "the last month", "past month", "previous month",
        "last year", "the last year", "this past year", "past year", "previous year",
        "last thing", "the last thing", "last subject", "last time", "the last time", "last session", "the last session",
        "recently", "recent", "last chat", "last conversation", "last talk", "last discussion",
        "what did we last talk about", "when did we last talk", "what was our last conversation"
    };

    public static string ClosestMatch(string query, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        query = Normalize(query);

        string bestMatch = null;
        int bestDistance = int.MaxValue;

        foreach (var kw in Keywords)
        {
            int distance = Levenshtein(query, kw);

            int allowedDistance = Math.Min(maxDistance, Math.Max(2, kw.Length / 4));

            if (distance <= allowedDistance && distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = kw;
            }
        }

        return bestMatch ?? string.Empty;
    }

    private static string Normalize(string input) =>
        string.Join(" ", input
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static int Levenshtein(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }

        return dp[a.Length, b.Length];
    }
}