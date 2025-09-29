using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Caching.Memory;

namespace Bob.Commands.Helpers;

/// <summary>
/// Provides an in-memory cache for Discord messages, allowing efficient retrieval and
/// minimizing redundant API calls. Handles concurrent downloads and stores message
/// metadata such as reaction counts.
/// </summary>
public static class CachedMessages
{
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions
    {
        SizeLimit = 1000
    });

    private static readonly ConcurrentDictionary<ulong, Task<CachedMessageInfo>> OnGoingDownloads = new();

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(12),
        Size = 1
    };

    /// <summary>
    /// Retrieves a cached message if available, or downloads it from Discord and caches it.
    /// Ensures that only one download occurs per message at a time.
    /// </summary>
    /// <param name="channel">The channel containing the message.</param>
    /// <param name="messageId">The ID of the message to retrieve.</param>
    /// <returns>
    /// A <see cref="CachedMessageInfo"/> object containing the message and its reaction counts,
    /// or <c>null</c> if the message could not be found.
    /// </returns>
    public static async Task<CachedMessageInfo> GetOrDownloadAsync(IMessageChannel channel, ulong messageId)
    {
        if (Cache.TryGetValue(messageId, out CachedMessageInfo cached))
        {
            return cached;
        }

        var downloadTask = OnGoingDownloads.GetOrAdd(messageId, _ => DownloadAndCacheAsync(channel, messageId));
        try
        {
            var messageInfo = await downloadTask;
            return messageInfo;
        }
        finally
        {
            OnGoingDownloads.TryRemove(messageId, out _);
        }
    }

    /// <summary>
    /// Downloads a message from Discord, creates a <see cref="CachedMessageInfo"/> object,
    /// and stores it in the cache.
    /// </summary>
    /// <param name="channel">The channel containing the message.</param>
    /// <param name="messageId">The ID of the message to download.</param>
    /// <returns>
    /// A <see cref="CachedMessageInfo"/> object if the message is found; otherwise, <c>null</c>.
    /// </returns>
    private static async Task<CachedMessageInfo> DownloadAndCacheAsync(IMessageChannel channel, ulong messageId)
    {
        var msg = await channel.GetMessageAsync(messageId) as IUserMessage;
        
        if (msg != null)
        {
            var info = new CachedMessageInfo(msg);
            Cache.Set(messageId, info, CacheOptions);
            
            return info;
        }

        return null;
    }
}