using System;
using System.Text.RegularExpressions;

namespace BobTheBot.Chat.TemporalHandling;

public static class TemporalRangeDetector
{
    private static readonly Regex DaysAgoRegex =
        new(@"\b(\d+)\s+days?\s+ago\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LastNDaysRegex =
        new(@"\blast\s+(\d+)\s+days?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RollingWeekRegex =
        new(@"\b(the\s+last\s+week|this\s+past\s+week|over\s+the\s+last\s+week|in\s+the\s+last\s+week|past\s+week)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CalendarWeekRegex =
        new(@"\b(last\s+week|previous\s+week|the\s+prior\s+week)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RollingMonthRegex =
        new(@"\b(the\s+last\s+month|this\s+past\s+month|over\s+the\s+last\s+month|in\s+the\s+last\s+month|past\s+month)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CalendarMonthRegex =
        new(@"\b(last\s+month|previous\s+month|the\s+prior\s+month)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RollingYearRegex =
        new(@"\b(this\s+last\s+year|the\s+last\s+year|in\s+the\s+last\s+year|over\s+the\s+last\s+year|past\s+year|in\s+the\s+past\s+year)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CalendarYearRegex =
        new(@"\b(last\s+year|previous\s+year|the\s+prior\s+year)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TemporalRangeResult DetectTemporalRange(
        string query,
        DateTime? reference = null
    )
    {
        var now = (reference ?? DateTime.UtcNow).Date; // normalize to UTC midnight
        if (string.IsNullOrWhiteSpace(query))
            return new TemporalRangeResult(TemporalMode.None, null, null);

        query = QueryNormalizer.Normalize(query);

        if (query.Length <= 20)
        {
            var fuzzy = FuzzyKeyword.ClosestMatch(query);
            if (fuzzy != null)
                query = fuzzy;
        }

        if (query.Contains("last thing") || query.Contains("last message"))
            return new TemporalRangeResult(TemporalMode.LastThing, null, null);

        if (query.Contains("last time"))
            return new TemporalRangeResult(TemporalMode.LastTime, null, null);

        if (query.Contains("yesterday"))
        {
            var start = now.AddDays(-1);
            return new TemporalRangeResult(TemporalMode.Range, start, start.AddDays(1).AddTicks(-1));
        }

        if (query.Contains("today"))
        {
            return new TemporalRangeResult(TemporalMode.Range, now, now.AddDays(1).AddTicks(-1));
        }

        if (RollingWeekRegex.IsMatch(query))
        {
            var start = now.AddDays(-7);
            return new TemporalRangeResult(TemporalMode.Range, start, now.AddTicks(-1));
        }

        if (CalendarWeekRegex.IsMatch(query))
        {
            var today = now;
            int daysSinceMonday = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var thisMonday = today.AddDays(-daysSinceMonday);
            var lastMonday = thisMonday.AddDays(-7);
            var lastSunday = thisMonday.AddTicks(-1);
            return new TemporalRangeResult(TemporalMode.Range, lastMonday, lastSunday);
        }

        if (RollingMonthRegex.IsMatch(query))
        {
            var start = now.AddMonths(-1);
            return new TemporalRangeResult(TemporalMode.Range, start, now.AddTicks(-1));
        }

        if (CalendarMonthRegex.IsMatch(query))
        {
            var start = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var end = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
            return new TemporalRangeResult(TemporalMode.Range, start, end);
        }

        if (RollingYearRegex.IsMatch(query))
        {
            var start = now.AddYears(-1);
            return new TemporalRangeResult(TemporalMode.Range, start, now.AddTicks(-1));
        }

        if (CalendarYearRegex.IsMatch(query))
        {
            var start = new DateTime(now.Year - 1, 1, 1);
            var end   = new DateTime(now.Year - 1, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            return new TemporalRangeResult(TemporalMode.Range, start, end);
        }

        var match = DaysAgoRegex.Match(query);
        if (match.Success)
        {
            int days = int.Parse(match.Groups[1].Value);
            var start = now.AddDays(-days);
            return new TemporalRangeResult(TemporalMode.Range, start, start.AddDays(1).AddTicks(-1));
        }

        match = LastNDaysRegex.Match(query);
        if (match.Success)
        {
            int days = int.Parse(match.Groups[1].Value);
            var start = now.AddDays(-days);
            return new TemporalRangeResult(TemporalMode.Range, start, now.AddTicks(-1));
        }

        return new TemporalRangeResult(TemporalMode.None, null, null);
    }
}