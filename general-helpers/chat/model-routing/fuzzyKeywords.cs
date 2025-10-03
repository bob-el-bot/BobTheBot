using System;

namespace BobTheBot.Chat.Routing
{
    public static class FuzzyKeywords
    {
        private static readonly string[] ComplexKeywords =
        {
            "research", "explain", "explain how", "compare", "analysis", "analyze"
        };

        public static bool MatchesComplex(string query, int maxDistance = 2)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            query = query.ToLowerInvariant();

            foreach (var kw in ComplexKeywords)
            {
                if (query.Contains(kw))
                    return true;

                int distance = Levenshtein(query, kw);
                int allowedDistance = Math.Min(maxDistance, Math.Max(2, kw.Length / 4));

                if (distance <= allowedDistance)
                    return true;
            }

            return false;
        }

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
}