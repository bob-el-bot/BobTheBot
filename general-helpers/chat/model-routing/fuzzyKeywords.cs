using System;

public static class FuzzyKeywords
{
    private static readonly string[] ComplexKeywords =
    {
        "research", "explain", "analyze", "analyse",
        "reason", "compare", "contrast", "evaluate"
    };

    public static bool MatchesComplex(string query, double maxDistanceRatio = 0.25)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        foreach (var keyword in ComplexKeywords)
        {
            int distance = Levenshtein(query, keyword);

            // distance normalized by keyword length
            double ratio = (double)distance / keyword.Length;

            if (ratio <= maxDistanceRatio)
                return true;        // typo‑close enough
        }

        return false;
    }

    // Basic Levenshtein distance implementation
    private static int Levenshtein(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[a.Length, b.Length];
    }
}