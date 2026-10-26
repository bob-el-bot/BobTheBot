using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bob.Database;
using Discord;
using Discord.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Webhooks;

/// <summary>
/// Public HTTP listener for inbound webhooks (POST /ingest/{id}); every other request is answered as an
/// uptime check. It shares a process with the bot, so every stage is bounded: a flood of traffic gets
/// cheap rejections instead of using up the threads, database connections, or Discord rate limits that
/// Bob's own Discord traffic depends on.
/// </summary>
public static class WebhookListener
{
    private static class Limits
    {
        // Requests being handled at once. Over this, requests get an immediate 503.
        public const int MaxInFlight = 64;

        // Largest accepted body. Real webhook payloads are a few KB; Railway's edge has no body limit.
        public const int MaxBodyBytes = 512 * 1024;

        // Railway lets uploads run for 5 minutes, which would let slow senders hold slots.
        public static readonly TimeSpan BodyReadTimeout = TimeSpan.FromSeconds(10);

        // Every request from one IP, including uptime checks: 600/min sustained, bursts of 120.
        // Generous on purpose: services like GitHub send every repo's webhooks from a few shared IPs.
        public static readonly KeyedRateLimiter PerIp = new(capacity: 120, refillPerSecond: 10, maxKeys: 50_000);

        // Failed webhook requests from one IP (unknown ID, malformed, oversized): 12/min, bursts of 20.
        // Real senders almost never fail, so this cuts off ID guessing without touching real traffic.
        public static readonly KeyedRateLimiter FailuresPerIp = new(capacity: 20, refillPerSecond: 0.2, maxKeys: 50_000);

        // Deliveries to one webhook, including ones that are ignored: 120/min, bursts of 60.
        // Bounds the work a leaked URL can cause.
        public static readonly KeyedRateLimiter RequestsPerWebhook = new(capacity: 60, refillPerSecond: 2, maxKeys: 20_000);

        // Discord allows each webhook 5 posts per 2 seconds, and each channel 30 messages per minute.
        // Staying under both means webhook posts never get 429s from Discord, which count toward Discord's
        // per-IP invalid-request limit that would also block the bot.
        public static readonly KeyedRateLimiter PostsPerWebhook = new(capacity: 5, refillPerSecond: 0.5, maxKeys: 20_000);
        public static readonly KeyedRateLimiter PostsPerChannel = new(capacity: 5, refillPerSecond: 0.5, maxKeys: 20_000);

        // All webhook posts to Discord together: 10/s sustained, bursts of 20, far below Discord's limits.
        public static readonly KeyedRateLimiter PostsGlobal = new(capacity: 20, refillPerSecond: 10, maxKeys: 1);

        public const int DiscordTimeoutMs = 10_000;
        public static readonly TimeSpan MaintenanceInterval = TimeSpan.FromMinutes(1);
    }

    private static readonly List<IWebhookStrategy> _strategies =
    [
        new CoolifyStrategy(),
        new GitHubStrategy(),
        new GitLabStrategy(),
        new VercelStrategy(),
        new RailwayStrategy(),
        new NetlifyStrategy(),
        new DockerHubStrategy(),
        new GenericStrategy(),
    ];

    private static readonly SemaphoreSlim _inFlight = new(Limits.MaxInFlight, Limits.MaxInFlight);

    // Railway's edge sets X-Real-IP to the caller's address. Anywhere else the header is caller-controlled.
    private static readonly bool _trustRealIpHeader =
        Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT_NAME") != null ||
        Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null;

    // Rejection counts, logged once per maintenance interval instead of once per request
    private static readonly ConcurrentDictionary<string, long> _rejections = new();
    private static Timer _maintenance;

    public static IWebhookStrategy GetStrategy(string provider) =>
        _strategies.Find(s => s.CanHandle(provider)) ?? _strategies.Find(s => s.CanHandle("generic"));

    public static async Task StartAsync()
    {
        // Railway routes public traffic to $PORT; this listener also answers uptime checks
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        var listener = TryStart($"http://+:{port}/")
            // Windows denies binding all interfaces without admin / a netsh urlacl, but localhost is allowed
            ?? (OperatingSystem.IsWindows() ? TryStart($"http://localhost:{port}/") : null);

        if (listener == null) return;

        Console.WriteLine($"[WebhookListener] Listening on {listener.Prefixes.First()}");
        _maintenance = new Timer(_ => RunMaintenance(), null, Limits.MaintenanceInterval, Limits.MaintenanceInterval);

        while (true)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebhookListener] Accept error: {ex.Message}");
                await Task.Delay(100); // Don't spin if accepting keeps failing
                continue;
            }

            // Admission control: never queue work beyond the cap, answer immediately instead
            if (!_inFlight.Wait(0))
            {
                Reject("busy");
                RespondImmediately(ctx.Response, 503, retryAfterSeconds: 5);
                continue;
            }

            _ = Task.Run(async () =>
            {
                try { await HandleAsync(ctx); }
                finally { _inFlight.Release(); }
            });
        }
    }

    private static async Task HandleAsync(HttpListenerContext ctx)
    {
        var req  = ctx.Request;
        var resp = ctx.Response;
        var aborted = false;

        try
        {
            var client = ClientKey(req);

            if (!Limits.PerIp.TryAcquire(client, out var ipRetry))
            {
                Reject("rate-limited-ip");
                await Respond(resp, 429, "Too Many Requests", ipRetry);
                return;
            }

            // Anything that isn't a webhook delivery is an uptime check (e.g. Better Stack), whatever the method or path
            if (req.HttpMethod != "POST" || !req.Url.AbsolutePath.StartsWith("/ingest/"))
            {
                if (req.HttpMethod == "HEAD")
                    resp.StatusCode = 200; // HEAD responses carry no body
                else
                    await Respond(resp, 200, "Bob is Alive!");
                return;
            }

            if (Limits.FailuresPerIp.IsLimited(client, out var failRetry))
            {
                Reject("too-many-failures");
                await Respond(resp, 429, "Too Many Requests", failRetry);
                return;
            }

            var segments = req.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 || !Guid.TryParse(segments[1], out var id))
            {
                Limits.FailuresPerIp.Consume(client);
                await Respond(resp, 400, "Bad Request");
                return;
            }

            // Reject declared oversize bodies before reading anything
            if (req.ContentLength64 > Limits.MaxBodyBytes)
            {
                Limits.FailuresPerIp.Consume(client);
                Reject("too-large");
                await Respond(resp, 413, "Payload Too Large");
                return;
            }

            WebhookLookup lookup;
            Database.Types.WebhookInfo info;
            try
            {
                (lookup, info) = await WebhookCache.GetAsync(id);
            }
            catch (Exception)
            {
                // Database trouble is expected under load or during an outage; counted in the summary, not logged per request
                Reject("db-error");
                await Respond(resp, 503, "Service Unavailable", retryAfterSeconds: 30);
                return;
            }

            if (lookup == WebhookLookup.Busy)
            {
                Reject("db-busy");
                await Respond(resp, 503, "Service Unavailable", retryAfterSeconds: 5);
                return;
            }
            if (lookup == WebhookLookup.NotFound)
            {
                Limits.FailuresPerIp.Consume(client);
                Reject("not-found");
                await Respond(resp, 404, "Webhook not found");
                return;
            }

            var hookKey = info.Id.ToString();
            if (!Limits.RequestsPerWebhook.TryAcquire(hookKey, out var hookRetry))
            {
                Reject("rate-limited-webhook");
                await Respond(resp, 429, "Too Many Requests", hookRetry);
                return;
            }

            var (body, bodyResult) = await ReadBodyAsync(req.InputStream, Limits.MaxBodyBytes, Limits.BodyReadTimeout);
            if (bodyResult == BodyReadResult.TimedOut)
            {
                Reject("slow-body");
                resp.Abort(); // Drop the connection; the pending read fails and is discarded
                aborted = true;
                return;
            }
            if (bodyResult == BodyReadResult.TooLarge)
            {
                Limits.FailuresPerIp.Consume(client);
                Reject("too-large");
                await Respond(resp, 413, "Payload Too Large");
                return;
            }

            if (!string.IsNullOrEmpty(info.Secret))
            {
                var signature = req.Headers["X-Hub-Signature-256"] ?? req.Headers["X-Webhook-Signature"];
                if (!ValidateSignature(body, info.Secret, signature))
                {
                    Limits.FailuresPerIp.Consume(client);
                    await Respond(resp, 401, "Invalid signature");
                    return;
                }
            }

            var embed = GetStrategy(info.Provider).BuildEmbed(Encoding.UTF8.GetString(body), req.Headers);

            // Strategy chose to skip this event (e.g. a noisy GitHub action), so acknowledge it without posting
            if (embed == null)
            {
                await Respond(resp, 200, "Ignored");
                return;
            }

            // Only actual posts spend Discord budget, so ignored events can't starve real ones
            if (!Limits.PostsPerWebhook.TryAcquire(hookKey, out var postRetry) ||
                !Limits.PostsPerChannel.TryAcquire(info.ChannelId.ToString(), out postRetry))
            {
                Reject("rate-limited-discord");
                await Respond(resp, 429, "Too Many Requests", postRetry);
                return;
            }
            if (!Limits.PostsGlobal.TryAcquire("all", out var globalRetry))
            {
                Reject("discord-busy");
                await Respond(resp, 503, "Service Unavailable", globalRetry);
                return;
            }

            try
            {
                var webhookClient = await WebhookCache.GetClientAsync(info);
                await webhookClient.SendMessageAsync(embeds: [embed], options: new RequestOptions
                {
                    Timeout = Limits.DiscordTimeoutMs,
                    RetryMode = RetryMode.RetryRatelimit,
                });
            }
            catch (Exception ex) when (IsWebhookGone(ex))
            {
                // Discord webhook (or its channel) was deleted, so stop accepting deliveries for it
                Console.WriteLine($"[WebhookListener] Discord webhook for {info.Id} ({info.DisplayName}) no longer exists, deactivating");
                await DeactivateAsync(info.Id);
                await Respond(resp, 410, "Discord webhook no longer exists");
                return;
            }
            catch (Exception ex) when (ex is TimeoutException or RateLimitedException)
            {
                Reject("discord-timeout");
                await Respond(resp, 503, "Service Unavailable", retryAfterSeconds: 30);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebhookListener] Discord post failed for {info.Id}: {ex.Message}");
                await Respond(resp, 500, "Failed to post to Discord");
                return;
            }

            await Respond(resp, 200, "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebhookListener] Unhandled error: {ex}");
            try { await Respond(resp, 500, "Internal Server Error"); } catch { /* client already gone */ }
        }
        finally
        {
            if (!aborted)
            {
                try { resp.Close(); } catch { /* client already gone */ }
            }
        }
    }

    internal enum BodyReadResult { Ok, TooLarge, TimedOut }

    /// <summary>
    /// Reads at most <paramref name="maxBytes"/> from the body, whatever Content-Length claims, and gives up
    /// after <paramref name="timeout"/>. On timeout the caller must abort the connection.
    /// </summary>
    internal static async Task<(byte[] Body, BodyReadResult Result)> ReadBodyAsync(Stream input, int maxBytes, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        var timedOut = Task.Delay(Timeout.Infinite, deadline.Token);
        using var buffer = new MemoryStream();

        // A fresh array per read: a timed-out read can still complete later and must not write into a reused buffer
        while (true)
        {
            var chunk = new byte[16 * 1024];
            var read = input.ReadAsync(chunk, 0, chunk.Length, deadline.Token);

            // HttpListener's request stream may not honour cancellation, so race the read against the deadline too
            if (await Task.WhenAny(read, timedOut) != read)
                return (null, BodyReadResult.TimedOut);

            int count;
            try { count = await read; }
            catch (OperationCanceledException) { return (null, BodyReadResult.TimedOut); }

            if (count == 0)
                return (buffer.ToArray(), BodyReadResult.Ok);

            if (buffer.Length + count > maxBytes)
                return (null, BodyReadResult.TooLarge);

            buffer.Write(chunk, 0, count);
        }
    }

    /// <summary>The caller's IP, with IPv6 grouped by /64 so one host can't dodge limits by rotating addresses.</summary>
    private static string ClientKey(HttpListenerRequest req)
    {
        IPAddress ip = null;
        if (_trustRealIpHeader && IPAddress.TryParse(req.Headers["X-Real-IP"]?.Trim(), out var forwarded))
            ip = forwarded;
        ip ??= req.RemoteEndPoint?.Address;

        if (ip == null) return "unknown";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (ip.AddressFamily != AddressFamily.InterNetworkV6) return ip.ToString();

        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }

    internal static bool IsWebhookGone(Exception ex) => ex switch
    {
        // Thrown by the DiscordWebhookClient constructor when Discord returns 404 for the webhook
        InvalidOperationException ioe when ioe.Message.Contains("Could not find a webhook") => true,
        HttpException { HttpCode: HttpStatusCode.NotFound or HttpStatusCode.Unauthorized } => true,
        HttpException { DiscordCode: DiscordErrorCode.UnknownWebhook or DiscordErrorCode.InvalidWebhookToken } => true,
        _ => false,
    };

    internal static async Task DeactivateAsync(Guid id)
    {
        WebhookCache.Invalidate(id);

        try
        {
            using var scope = Bob.Bot.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BobEntities>();
            var info = await db.WebhookInfo.FindAsync(id);
            if (info == null) return;

            info.IsActive = false;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebhookListener] Failed to deactivate {id}: {ex.Message}");
        }
    }

    private static void Reject(string reason) => _rejections.AddOrUpdate(reason, 1, (_, n) => n + 1);

    private static void RunMaintenance()
    {
        try
        {
            Limits.PerIp.Sweep();
            Limits.FailuresPerIp.Sweep();
            Limits.RequestsPerWebhook.Sweep();
            Limits.PostsPerWebhook.Sweep();
            Limits.PostsPerChannel.Sweep();
            WebhookCache.Prune();

            var counts = _rejections.Keys
                .Select(reason => (reason, count: _rejections.TryRemove(reason, out var n) ? n : 0))
                .Where(r => r.count > 0)
                .OrderByDescending(r => r.count)
                .ToList();

            if (counts.Count > 0)
                Console.WriteLine($"[WebhookListener] Rejected in the last minute: {string.Join(", ", counts.Select(r => $"{r.reason}={r.count}"))} (tracking {Limits.PerIp.Count} IPs)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebhookListener] Maintenance failed: {ex.Message}");
        }
    }

    private static HttpListener TryStart(string prefix)
    {
        var listener = new HttpListener { IgnoreWriteExceptions = true }; // Clients hanging up mid-response are normal
        listener.Prefixes.Add(prefix);

        try
        {
            listener.Start();
            return listener;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebhookListener] Failed to start on {prefix}: {ex.Message}");
            listener.Close();
            return null;
        }
    }

    private static bool ValidateSignature(byte[] body, string secret, string signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        // Sign the raw bytes exactly as received; re-encoding a decoded string can change them
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = "sha256=" + Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();

        // Compare the signature value whether or not it includes the "sha256=" prefix
        var provided = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature.ToLowerInvariant()
            : "sha256=" + signature.ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));
    }

    private static async Task Respond(HttpListenerResponse resp, int statusCode, string body, int retryAfterSeconds = 0)
    {
        resp.StatusCode = statusCode;
        resp.ContentType = "text/plain";
        if (retryAfterSeconds > 0)
            resp.AddHeader("Retry-After", retryAfterSeconds.ToString());

        var bytes = Encoding.UTF8.GetBytes(body);
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes);
    }

    /// <summary>Answers without reading the request or leaving the accept loop waiting on the client.</summary>
    private static void RespondImmediately(HttpListenerResponse resp, int statusCode, int retryAfterSeconds)
    {
        try
        {
            resp.StatusCode = statusCode;
            resp.AddHeader("Retry-After", retryAfterSeconds.ToString());
            resp.ContentLength64 = 0;
            resp.Close();
        }
        catch
        {
            try { resp.Abort(); } catch { /* already gone */ }
        }
    }
}
