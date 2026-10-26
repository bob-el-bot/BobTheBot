using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Bob.Database;
using Bob.Database.Types;
using Discord.Webhook;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Webhooks;

public enum WebhookLookup { Found, NotFound, Busy }

/// <summary>
/// Caches webhook lookups (including misses) and the Discord client used to post for each webhook, so
/// repeated deliveries don't hit the database or re-fetch the Discord webhook every time. Database access
/// is capped so a flood of webhook traffic can never take all of Bob's database connections.
/// </summary>
internal static class WebhookCache
{
    private static readonly TimeSpan FoundTtl     = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NotFoundTtl  = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DbWaitLimit  = TimeSpan.FromSeconds(2);
    private const int MaxConcurrentDbLookups      = 8;
    private const int MaxEntries                  = 10_000;

    private sealed record Entry(WebhookInfo Info, DateTime ExpiresAt); // Info == null caches "not found"

    private static readonly ConcurrentDictionary<Guid, Entry> _entries = new();
    private static readonly ConcurrentDictionary<Guid, Lazy<Task<DiscordWebhookClient>>> _clients = new();
    private static readonly SemaphoreSlim _dbGate = new(MaxConcurrentDbLookups, MaxConcurrentDbLookups);

    public static async Task<(WebhookLookup Result, WebhookInfo Info)> GetAsync(Guid id)
    {
        if (_entries.TryGetValue(id, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Info != null ? (WebhookLookup.Found, cached.Info) : (WebhookLookup.NotFound, null);

        if (!await _dbGate.WaitAsync(DbWaitLimit))
            return (WebhookLookup.Busy, null);

        WebhookInfo info;
        try
        {
            using var scope = Bob.Bot.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BobEntities>();
            info = await db.WebhookInfo.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id && w.IsActive);
        }
        finally
        {
            _dbGate.Release();
        }

        if (_entries.Count >= MaxEntries)
            Prune();

        // When full of live entries, skip caching misses: they are what a random-ID flood produces
        if (info != null || _entries.Count < MaxEntries)
            _entries[id] = new Entry(info, DateTime.UtcNow + (info != null ? FoundTtl : NotFoundTtl));

        return info != null ? (WebhookLookup.Found, info) : (WebhookLookup.NotFound, null);
    }

    /// <summary>
    /// The Discord client for posting as this webhook, created once and reused. Creating one fetches the
    /// webhook from Discord, so this throws if the Discord webhook was deleted.
    /// </summary>
    public static async Task<DiscordWebhookClient> GetClientAsync(WebhookInfo info)
    {
        // The client's constructor makes a blocking HTTP call. Run it on a dedicated thread so a burst of
        // new webhooks can't tie up thread-pool threads that Bob's gateway connection needs.
        var lazy = _clients.GetOrAdd(info.Id, _ => new Lazy<Task<DiscordWebhookClient>>(() =>
            Task.Factory.StartNew(
                () => new DiscordWebhookClient(info.DiscordWebhookId, info.DiscordWebhookToken),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)));

        try
        {
            return await lazy.Value;
        }
        catch
        {
            _clients.TryRemove(new(info.Id, lazy)); // Don't cache a failure; the next delivery retries
            throw;
        }
    }

    /// <summary>Call after a webhook is edited, deleted, or deactivated so the next delivery sees the change.</summary>
    public static void Invalidate(Guid id)
    {
        _entries.TryRemove(id, out _);
        // Not disposed here: a delivery may still be using it. It is collected once unreferenced.
        _clients.TryRemove(id, out _);
    }

    /// <summary>Drops expired entries, and the clients of webhooks that are no longer cached.</summary>
    public static void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var (id, entry) in _entries)
        {
            if (entry.ExpiresAt <= now)
                _entries.TryRemove(id, out _);
        }

        foreach (var id in _clients.Keys)
        {
            if (!_entries.ContainsKey(id))
                _clients.TryRemove(id, out _);
        }
    }

    public static int Count => _entries.Count;
}
