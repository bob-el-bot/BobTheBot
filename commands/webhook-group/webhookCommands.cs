using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Bob.Webhooks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [DefaultMemberPermissions(GuildPermission.ManageGuild)] // Discord only honours this on the top-level group
    [Group("webhook", "Manage inbound webhooks from external services.")]
    public class WebhookGroup : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly BobEntities _db;
        public WebhookGroup(BobEntities db) { _db = db; }

        // Public address the listener is reachable at — only used to show users their ingest URL
        private static string BaseUrl => Environment.GetEnvironmentVariable("WEBHOOK_BASE_URL")?.TrimEnd('/');

        // ── /webhook create ────────────────────────────────────────────────────

        [SlashCommand("create", "Register a new inbound webhook for an external service.")]
        public async Task CreateWebhook(
            [Summary("channel", "Channel where notifications will be posted")][ChannelTypes(ChannelType.Text, ChannelType.News)] ITextChannel channel,
            [Summary("name", "A friendly name for this webhook")] string name,
            [Summary("provider", "Which service will be sending payloads")]
            [Choice("GitHub", "github")]
            [Choice("GitLab", "gitlab")]
            [Choice("Vercel", "vercel")]
            [Choice("Railway", "railway")]
            [Choice("Netlify", "netlify")]
            [Choice("Coolify", "coolify")]
            [Choice("Docker Hub", "dockerhub")]
            [Choice("Generic (any JSON)", "generic")]
            string provider)
        {
            await DeferAsync(ephemeral: true);

            if (string.IsNullOrEmpty(BaseUrl))
            {
                await FollowupAsync("❌ Webhooks aren't configured on this instance of Bob (missing `WEBHOOK_BASE_URL`).", ephemeral: true);
                return;
            }

            // The user must be allowed to create webhooks there themselves, not just use this command
            if (!((SocketGuildUser)Context.User).GetPermissions(channel).ManageWebhooks)
            {
                await FollowupAsync($"❌ You need the `Manage Webhooks` permission in {channel.Mention}.", ephemeral: true);
                return;
            }

            var perms = Context.Guild.CurrentUser.GetPermissions(channel);
            if (!perms.ManageWebhooks)
            {
                await FollowupAsync($"❌ I need the `Manage Webhooks` permission in {channel.Mention}.", ephemeral: true);
                return;
            }

            // Create a Discord webhook in the target channel so the listener can post there
            var discordWebhook = await channel.CreateWebhookAsync($"Bob — {name}");

            var info = new WebhookInfo
            {
                Provider           = provider,
                GuildId            = Context.Guild.Id,
                ChannelId          = channel.Id,
                DiscordWebhookId   = discordWebhook.Id,
                DiscordWebhookToken = discordWebhook.Token,
                DisplayName        = name,
                CreatedBy          = Context.User.Id,
            };

            _db.WebhookInfo.Add(info);
            await _db.SaveChangesAsync();

            var ingestUrl = $"{BaseUrl}/ingest/{info.Id}";
            var strategy  = WebhookListener.GetStrategy(provider);

            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Webhook Registered")
                .WithColor(Bob.Bot.theme)
                .AddField("Provider", strategy.ServiceName, inline: true)
                .AddField("Channel", channel.Mention, inline: true)
                .AddField("Point your service here", $"```\n{ingestUrl}\n```")
                .AddField("Setup", strategy.SetupHint)
                .WithFooter("Keep this URL private — anyone who has it can post to your channel.")
                .Build(), ephemeral: true);
        }

        // ── /webhook list ──────────────────────────────────────────────────────

        [SlashCommand("list", "List all registered webhooks for this server.")]
        public async Task ListWebhooks()
        {
            var hooks = await _db.WebhookInfo
                .Where(w => w.GuildId == Context.Guild.Id && w.IsActive)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            if (hooks.Count == 0)
            {
                await RespondAsync($"No webhooks registered. Use {Help.GetCommandMention("webhook create")} to add one.", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();
            foreach (var h in hooks)
                sb.AppendLine($"**{h.DisplayName}** (`{h.Provider}`) → <#{h.ChannelId}> — `{h.Id.ToString()[..8]}…`");

            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle($"Registered Webhooks ({hooks.Count})")
                .WithDescription(sb.ToString())
                .WithColor(Bob.Bot.theme)
                .Build(), ephemeral: true);
        }

        // ── /webhook delete ────────────────────────────────────────────────────

        [SlashCommand("delete", "Remove a registered webhook.")]
        public async Task DeleteWebhook(
            [Summary("name", "Name of the webhook to remove (partial match)")] string name)
        {
            var all = await _db.WebhookInfo
                .Where(w => w.GuildId == Context.Guild.Id && w.IsActive)
                .ToListAsync();

            var matches = all
                .Where(w => w.DisplayName.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                await RespondAsync($"❌ No active webhook matching `{name}`.", ephemeral: true);
                return;
            }
            if (matches.Count > 1)
            {
                var names = string.Join(", ", matches.Select(w => $"`{w.DisplayName}`"));
                await RespondAsync($"❌ Multiple matches: {names}. Be more specific.", ephemeral: true);
                return;
            }

            var hook = matches[0];

            // Try to delete the underlying Discord webhook so it can't be misused
            try
            {
                var discordHook = await Context.Guild.GetWebhookAsync(hook.DiscordWebhookId);
                if (discordHook != null)
                    await discordHook.DeleteAsync();
            }
            catch { /* webhook may have already been deleted manually */ }

            hook.IsActive = false;
            await _db.SaveChangesAsync();

            await RespondAsync($"✅ Webhook **{hook.DisplayName}** deleted.", ephemeral: true);
        }
    }
}
