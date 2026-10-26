using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Bob.Webhooks;

/// <summary>
/// Non-blocking log output for the webhook listener. Console writes are synchronous and serialized, so
/// writing one line per request during a flood would leave request threads (and the thread pool Bob's
/// gateway shares) waiting on the console. Instead, lines go into a bounded queue that a single dedicated
/// thread writes out in batches. If the queue ever fills, new lines are counted and reported as dropped
/// rather than growing memory or blocking.
/// </summary>
internal static class WebhookLog
{
    private const int QueueCapacity = 10_000;
    private const int MaxBatchLines = 500;

    private static readonly Channel<string> _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(QueueCapacity)
    {
        FullMode = BoundedChannelFullMode.Wait, // With Wait, TryWrite returns false when full, so drops can be counted
        SingleReader = true,
    });

    private static long _dropped;

    static WebhookLog()
    {
        var writer = new Thread(WriteLoop) { IsBackground = true, Name = "WebhookLog" };
        writer.Start();
    }

    /// <summary>Queues a line for output. Never blocks.</summary>
    public static void Write(string line)
    {
        if (!_queue.Writer.TryWrite(line))
            Interlocked.Increment(ref _dropped);
    }

    /// <summary>
    /// Makes a caller-controlled value safe for a single log line: no control characters (so it can't
    /// start a fake line), no quotes, and a bounded length.
    /// </summary>
    public static string Clean(string value, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(value)) return "-";

        var sb = new StringBuilder(Math.Min(value.Length, maxLength + 1));
        foreach (var c in value)
        {
            if (sb.Length >= maxLength) { sb.Append('…'); break; }
            sb.Append(char.IsControl(c) || c == '"' ? '?' : c);
        }
        return sb.ToString();
    }

    private static void WriteLoop()
    {
        var reader = _queue.Reader;
        var batch = new StringBuilder();

        // This thread exists only to wait on the queue and the console, so blocking it is fine
        while (reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
        {
            batch.Clear();
            var lines = 0;
            while (lines < MaxBatchLines && reader.TryRead(out var line))
            {
                batch.AppendLine(line);
                lines++;
            }

            var dropped = Interlocked.Exchange(ref _dropped, 0);
            if (dropped > 0)
                batch.AppendLine($"[Webhook] log queue was full, dropped {dropped} line(s)");

            try { Console.Out.Write(batch.ToString()); }
            catch { /* Never let logging take the listener down */ }
        }
    }
}
