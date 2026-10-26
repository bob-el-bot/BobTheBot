using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Bob.Webhooks;
using Discord;
using Discord.Interactions;
using Discord.Webhook;
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

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        // Discord accepts these formats for webhook avatars
        private static readonly string[] AvatarTypes = ["image/png", "image/jpeg", "image/gif", "image/webp"];
        private const int MaxAvatarBytes = 8 * 1024 * 1024;

        /// <summary>The user must be allowed to manage webhooks in the channel themselves, not just run this command.</summary>
        private string CheckUser(ITextChannel channel)
        {
            if (((SocketGuildUser)Context.User).GetPermissions(channel).ManageWebhooks)
                return null;

            return $"❌ You do not have permissions to manage webhooks in {channel.Mention}.\n- Ask a user with `Manage Webhooks` permission.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)";
        }

        /// <summary>
        /// Autocomplete submits the webhook's ID. If the user typed instead of picking a suggestion,
        /// fall back to an exact (then unique partial) name match.
        /// </summary>
        private async Task<(WebhookInfo Hook, string Error)> FindWebhookAsync(string input)
        {
            var hooks = await _db.WebhookInfo
                .Where(w => w.GuildId == Context.Guild.Id && w.IsActive)
                .ToListAsync();

            if (Guid.TryParse(input, out var id) && hooks.FirstOrDefault(w => w.Id == id) is { } byId)
                return (byId, null);

            if (hooks.FirstOrDefault(w => w.DisplayName.Equals(input, StringComparison.OrdinalIgnoreCase)) is { } exact)
                return (exact, null);

            var matches = hooks.Where(w => w.DisplayName.Contains(input, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count switch
            {
                1 => (matches[0], null),
                0 => (null, $"❌ No active webhook matching `{input}`.\n- Use {Help.GetCommandMention("webhook list")} to see this server's webhooks."),
                _ => (null, $"❌ Multiple webhooks match `{input}`: {string.Join(", ", matches.Select(w => $"`{w.DisplayName}`"))}.\n- Pick one from the suggestions instead."),
            };
        }

        /// <summary>Downloads an attachment for use as a webhook avatar. The caller disposes the image.</summary>
        private static async Task<(Image? Image, string Error)> LoadAvatarAsync(IAttachment avatar)
        {
            if (!AvatarTypes.Contains(avatar.ContentType?.Split(';')[0]))
                return (null, "❌ The avatar must be a PNG, JPEG, GIF, or WebP image.");

            if (avatar.Size > MaxAvatarBytes)
                return (null, "❌ The avatar must be smaller than 8 MB.");

            try
            {
                var bytes = await _http.GetByteArrayAsync(avatar.Url);
                return (new Image(new MemoryStream(bytes)), null);
            }
            catch (Exception)
            {
                return (null, "❌ Bob couldn't download that image.\n- Try uploading it again.");
            }
        }

        [SlashCommand("create", "Register a new inbound webhook for an external service.")]
        public async Task CreateWebhook(
            [Summary("channel", "Channel where notifications will be posted")][ChannelTypes(ChannelType.Text, ChannelType.News)] ITextChannel channel,
            [Summary("name", "A friendly name for this webhook")][MaxLength(70)] string name,
            [Summary("provider", "Which service will be sending payloads")] WebhookProvider provider,
            [Summary("avatar", "Profile picture for the webhook's messages (optional)")] IAttachment avatar = null)
        {
            await DeferAsync(ephemeral: true);

            if (string.IsNullOrEmpty(BaseUrl))
            {
                await FollowupAsync("❌ Webhooks aren't configured on this instance of Bob (missing `WEBHOOK_BASE_URL`).", ephemeral: true);
                return;
            }

            if (CheckUser(channel) is { } error)
            {
                await FollowupAsync(error, ephemeral: true);
                return;
            }

            if (!Context.Guild.CurrentUser.GetPermissions(channel).ManageWebhooks)
            {
                await FollowupAsync($"❌ Bob cannot create webhooks in {channel.Mention}.\n- Give Bob the `Manage Webhooks` permission.", ephemeral: true);
                return;
            }

            Image? image = null;
            if (avatar != null)
            {
                (image, var avatarError) = await LoadAvatarAsync(avatar);
                if (avatarError != null)
                {
                    await FollowupAsync(avatarError, ephemeral: true);
                    return;
                }
            }

            // Create a Discord webhook in the target channel so the listener can post there
            IWebhook discordWebhook;
            using (image)
                discordWebhook = await channel.CreateWebhookAsync($"Bob | {name}", image?.Stream);

            var info = new WebhookInfo
            {
                Provider            = provider.Key(),
                GuildId             = Context.Guild.Id,
                ChannelId           = channel.Id,
                DiscordWebhookId    = discordWebhook.Id,
                DiscordWebhookToken = discordWebhook.Token,
                DisplayName         = name,
                CreatedBy           = Context.User.Id,
            };

            _db.WebhookInfo.Add(info);
            await _db.SaveChangesAsync();

            var ingestUrl = $"{BaseUrl}/ingest/{info.Id}";
            var strategy  = WebhookListener.GetStrategy(info.Provider);

            var embed = new EmbedBuilder()
                .WithTitle("Webhook Registered")
                .WithColor(Bob.Bot.theme)
                .AddField("Provider", strategy.ServiceName, inline: true)
                .AddField("Channel", channel.Mention, inline: true)
                .AddField("Point your service here", $"```\n{ingestUrl}\n```")
                .AddField("Setup", strategy.SetupHint)
                .WithFooter("Keep this URL private. Anyone who has it can post to your channel.");

            if (avatar != null) embed.WithThumbnailUrl(avatar.Url);

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }

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
                sb.AppendLine($"**{h.DisplayName}** (`{h.Provider}`) → <#{h.ChannelId}> (`{h.Id.ToString()[..8]}…`)");

            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle($"Registered Webhooks ({hooks.Count})")
                .WithDescription(sb.ToString())
                .WithColor(Bob.Bot.theme)
                .Build(), ephemeral: true);
        }

        [SlashCommand("delete", "Remove a registered webhook.")]
        public async Task DeleteWebhook(
            [Summary("webhook", "The webhook to remove")][Autocomplete(typeof(WebhookAutocompleteHandler))] string webhook)
        {
            var (hook, findError) = await FindWebhookAsync(webhook);
            if (findError != null)
            {
                await RespondAsync(findError, ephemeral: true);
                return;
            }

            var hookChannel = Context.Guild.GetTextChannel(hook.ChannelId);
            if (hookChannel != null && CheckUser(hookChannel) is { } error)
            {
                await RespondAsync(error, ephemeral: true);
                return;
            }

            // Delete the underlying Discord webhook using its own token, so this works even if Bob lost Manage Webhooks
            try
            {
                using var discordHook = new DiscordWebhookClient(hook.DiscordWebhookId, hook.DiscordWebhookToken);
                await discordHook.DeleteWebhookAsync();
            }
            catch { /* webhook may have already been deleted manually */ }

            hook.IsActive = false;
            await _db.SaveChangesAsync();

            await RespondAsync($"✅ Webhook **{hook.DisplayName}** deleted.", ephemeral: true);
        }
    }
}
