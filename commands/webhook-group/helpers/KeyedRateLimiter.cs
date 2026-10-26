using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Bob.Webhooks;

/// <summary>
/// Thread-safe token-bucket rate limiter keyed by an arbitrary string (a client IP, a webhook ID, ...).
/// Each key holds up to <c>capacity</c> tokens and regains <c>refillPerSecond</c> tokens per second.
/// Memory is bounded: once <c>maxKeys</c> keys are tracked, new keys share a single overflow bucket.
/// </summary>
public sealed class KeyedRateLimiter
{
    private sealed class Bucket
    {
        public double Tokens;
        public long LastRefill; // Stopwatch timestamp
    }

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private readonly Bucket _overflow;
    private readonly double _capacity;
    private readonly double _refillPerSecond;
    private readonly int _maxKeys;
    private int _sweeping;

    public KeyedRateLimiter(double capacity, double refillPerSecond, int maxKeys)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (refillPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(refillPerSecond));

        _capacity = capacity;
        _refillPerSecond = refillPerSecond;
        _maxKeys = maxKeys;
        _overflow = new Bucket { Tokens = capacity, LastRefill = Stopwatch.GetTimestamp() };
    }

    public int Count => _buckets.Count;

    /// <summary>Takes one token if available. When limited, <paramref name="retryAfterSeconds"/> says when to try again.</summary>
    public bool TryAcquire(string key, out int retryAfterSeconds)
    {
        var bucket = GetBucket(key);
        lock (bucket)
        {
            Refill(bucket, Stopwatch.GetTimestamp());
            if (bucket.Tokens >= 1)
            {
                bucket.Tokens -= 1;
                retryAfterSeconds = 0;
                return true;
            }

            retryAfterSeconds = SecondsUntilToken(bucket);
            return false;
        }
    }

    /// <summary>True if the key has no tokens left, without taking one. Pair with <see cref="Consume"/>.</summary>
    public bool IsLimited(string key, out int retryAfterSeconds)
    {
        if (!_buckets.TryGetValue(key, out var bucket))
        {
            if (_buckets.Count < _maxKeys)
            {
                retryAfterSeconds = 0;
                return false; // Never seen, so it has a full bucket
            }

            bucket = _overflow; // Untracked keys are charged to the overflow bucket, so check that one
        }

        lock (bucket)
        {
            Refill(bucket, Stopwatch.GetTimestamp());
            retryAfterSeconds = bucket.Tokens >= 1 ? 0 : SecondsUntilToken(bucket);
            return bucket.Tokens < 1;
        }
    }

    /// <summary>Takes one token whether or not one is available (never below zero). Used to charge for failures.</summary>
    public void Consume(string key)
    {
        var bucket = GetBucket(key);
        lock (bucket)
        {
            Refill(bucket, Stopwatch.GetTimestamp());
            bucket.Tokens = Math.Max(0, bucket.Tokens - 1);
        }
    }

    /// <summary>
    /// Forgets keys whose buckets have refilled completely. A full bucket behaves exactly like a new one,
    /// so dropping it changes no decisions; it only frees memory.
    /// </summary>
    public void Sweep()
    {
        if (Interlocked.Exchange(ref _sweeping, 1) == 1) return; // Another thread is already sweeping

        try
        {
            var now = Stopwatch.GetTimestamp();
            foreach (var (key, bucket) in _buckets)
            {
                lock (bucket)
                {
                    Refill(bucket, now);
                    if (bucket.Tokens >= _capacity)
                        _buckets.TryRemove(key, out _);
                }
            }
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private Bucket GetBucket(string key)
    {
        if (_buckets.TryGetValue(key, out var existing))
            return existing;

        if (_buckets.Count >= _maxKeys)
        {
            Sweep();
            if (_buckets.Count >= _maxKeys)
                return _overflow; // Still full: everyone new shares one bucket rather than growing memory
        }

        return _buckets.GetOrAdd(key, _ => new Bucket { Tokens = _capacity, LastRefill = Stopwatch.GetTimestamp() });
    }

    private void Refill(Bucket bucket, long now)
    {
        var elapsedSeconds = (now - bucket.LastRefill) / (double)Stopwatch.Frequency;
        if (elapsedSeconds <= 0) return;

        bucket.Tokens = Math.Min(_capacity, bucket.Tokens + elapsedSeconds * _refillPerSecond);
        bucket.LastRefill = now;
    }

    private int SecondsUntilToken(Bucket bucket) =>
        Math.Max(1, (int)Math.Ceiling((1 - bucket.Tokens) / _refillPerSecond));
}
