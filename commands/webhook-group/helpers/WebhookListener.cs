using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bob.Database;
using Bob.Database.Types;
using Discord.Webhook;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Webhooks;

public static class WebhookListener
{
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

    public static IWebhookStrategy GetStrategy(string provider) =>
        _strategies.Find(s => s.CanHandle(provider)) ?? _strategies.Find(s => s.CanHandle("generic"));

    public static async Task StartAsync()
    {
        // Railway routes public traffic to $PORT; this listener also replaces Uptime's "Bob is Alive!" check
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        var listener = TryStart($"http://+:{port}/")
            // Windows denies binding all interfaces without admin / a netsh urlacl, but localhost is allowed
            ?? (OperatingSystem.IsWindows() ? TryStart($"http://localhost:{port}/") : null);

        if (listener == null) return;

        Console.WriteLine($"[WebhookListener] Listening on {listener.Prefixes.First()}");

        while (true)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebhookListener] Accept error: {ex.Message}");
                continue;
            }

            _ = Task.Run(() => HandleAsync(ctx));
        }
    }

    private static async Task HandleAsync(HttpListenerContext ctx)
    {
        var req  = ctx.Request;
        var resp = ctx.Response;

        try
        {
            // Health check
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath is "/" or "/health")
            {
                await WriteResponse(resp, 200, "Bob is Alive!");
                return;
            }

            // Only accept POST /ingest/{guid}
            if (req.HttpMethod != "POST" || !req.Url.AbsolutePath.StartsWith("/ingest/"))
            {
                await WriteResponse(resp, 404, "Not Found");
                return;
            }

            var segments = req.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2 || !Guid.TryParse(segments[1], out var id))
            {
                await WriteResponse(resp, 400, "Bad Request");
                return;
            }

            using var body = new StreamReader(req.InputStream, req.ContentEncoding);
            var rawJson = await body.ReadToEndAsync();

            WebhookInfo info;
            using (var scope = Bob.Bot.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BobEntities>();
                info = await db.WebhookInfo.FirstOrDefaultAsync(w => w.Id == id && w.IsActive);
            }

            if (info == null)
            {
                await WriteResponse(resp, 404, "Webhook not found");
                return;
            }

            // Validate HMAC secret if one is configured
            if (!string.IsNullOrEmpty(info.Secret))
            {
                var signature = req.Headers["X-Hub-Signature-256"] ?? req.Headers["X-Webhook-Signature"];
                if (!ValidateSignature(rawJson, info.Secret, signature))
                {
                    await WriteResponse(resp, 401, "Invalid signature");
                    return;
                }
            }

            var embed = GetStrategy(info.Provider).BuildEmbed(rawJson, req.Headers);

            // Strategy chose to skip this event (e.g. a noisy GitHub action) — still acknowledge it
            if (embed == null)
            {
                await WriteResponse(resp, 200, "Ignored");
                return;
            }

            try
            {
                using var webhookClient = new DiscordWebhookClient(info.DiscordWebhookId, info.DiscordWebhookToken);
                await webhookClient.SendMessageAsync(embeds: [embed]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebhookListener] Discord post failed for {info.Id}: {ex.Message}");
                await WriteResponse(resp, 500, "Failed to post to Discord");
                return;
            }

            await WriteResponse(resp, 200, "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebhookListener] Unhandled error: {ex}");
            await WriteResponse(resp, 500, "Internal Server Error");
        }
        finally
        {
            resp.Close();
        }
    }

    private static HttpListener TryStart(string prefix)
    {
        var listener = new HttpListener();
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

    private static bool ValidateSignature(string body, string secret, string signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var keyBytes  = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(keyBytes);
        var hash      = hmac.ComputeHash(bodyBytes);
        var expected  = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        // Compare the signature value whether or not it includes the "sha256=" prefix
        var provided = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature
            : "sha256=" + signature;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
    }

    private static async Task WriteResponse(HttpListenerResponse resp, int statusCode, string body)
    {
        resp.StatusCode = statusCode;
        resp.ContentType = "text/plain";
        var bytes = Encoding.UTF8.GetBytes(body);
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes);
    }
}
