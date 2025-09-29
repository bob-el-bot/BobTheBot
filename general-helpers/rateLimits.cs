using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace BobTheBot.RateLimits;

public static class RateLimitHandling
{
    private static volatile bool UnderLoad = false;
    private static DateTime UnderLoadUntil = DateTime.MinValue;

    /// <summary>
    /// Indicates whether the bot is currently in an "under load" state
    /// due to hitting the global rate limit bucket.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the bot is under load and within the cooldown window; 
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsUnderload()
    {
        return UnderLoad && DateTime.UtcNow < UnderLoadUntil;
    }

    /// <summary>
    /// Callback invoked when a REST request triggers a rate limit.
    /// Sets the bot into an "under load" state if the global bucket is hit,
    /// causing subsequent sends to prefer webhooks for a short cooldown period.
    /// </summary>
    /// <param name="info">
    /// Rate limit information provided by Discord.NET for the request.
    /// </param>
    public static async Task GlobalRatelimitCallback(IRateLimitInfo info)
    {
        if (info.IsGlobal)
        {
            Console.WriteLine($"[RateLimit] Global bucket hit. Delaying {info.RetryAfter}s");
            UnderLoad = true;
            UnderLoadUntil = DateTime.UtcNow.AddSeconds(5);
        }

        await Task.CompletedTask;
    }
}