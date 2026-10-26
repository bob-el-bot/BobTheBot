using System;
using System.Collections.Generic;
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

        private string CheckBot(ITextChannel channel)
        {
            if (Context.Guild.CurrentUser.GetPermissions(channel).ManageWebhooks)
                return null;

            return $"❌ Bob cannot manage webhooks in {channel.Mention}.\n- Give Bob the `Manage Webhooks` permission.";
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

            if ((CheckUser(channel) ?? CheckBot(channel)) is { } error)
            {
                await FollowupAsync(error, ephemeral: true);
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

        [SlashCommand("edit", "Change a registered webhook's name, channel, provider, or avatar.")]
        public async Task EditWebhook(
            [Summary("webhook", "The webhook to edit")][Autocomplete(typeof(WebhookAutocompleteHandler))] string webhook,
            [Summary("name", "New friendly name (optional)")][MaxLength(70)] string name = null,
            [Summary("channel", "Move notifications to a different channel (optional)")][ChannelTypes(ChannelType.Text, ChannelType.News)] ITextChannel channel = null,
            [Summary("provider", "Change which service's format to expect (optional)")] WebhookProvider? provider = null,
            [Summary("avatar", "New profile picture (optional)")] IAttachment avatar = null,
            [Summary("remove-avatar", "Go back to the default profile picture (optional)")] bool removeAvatar = false)
        {
            await DeferAsync(ephemeral: true);

            if (name == null && channel == null && provider == null && avatar == null && !removeAvatar)
            {
                await FollowupAsync("❌ Nothing to change.\n- Give at least one of `name`, `channel`, `provider`, `avatar`, or `remove-avatar`.", ephemeral: true);
                return;
            }

            if (avatar != null && removeAvatar)
            {
                await FollowupAsync("❌ Pick either `avatar` or `remove-avatar`, not both.", ephemeral: true);
                return;
            }

            var (hook, findError) = await FindWebhookAsync(webhook);
            if (findError != null)
            {
                await FollowupAsync(findError, ephemeral: true);
                return;
            }

            var currentChannel = Context.Guild.GetTextChannel(hook.ChannelId);
            if (currentChannel != null && CheckUser(currentChannel) is { } currentError)
            {
                await FollowupAsync(currentError, ephemeral: true);
                return;
            }

            var moving = channel != null && channel.Id != hook.ChannelId;
            if (moving && (CheckUser(channel) ?? CheckBot(channel)) is { } moveError)
            {
                await FollowupAsync(moveError, ephemeral: true);
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

            // Name and avatar live on the Discord webhook itself; provider is only stored by Bob
            using (image)
            {
                try
                {
                    await ModifyDiscordWebhookAsync(hook, name, moving ? channel : null, image, removeAvatar);
                }
                catch (Exception ex) when (WebhookListener.IsWebhookGone(ex))
                {
                    await WebhookListener.DeactivateAsync(hook.Id);
                    await FollowupAsync($"❌ The Discord webhook for **{hook.DisplayName}** was deleted outside of Bob, so it has been removed.\n- Use {Help.GetCommandMention("webhook create")} to set it up again.", ephemeral: true);
                    return;
                }
                catch (Discord.Net.HttpException)
                {
                    await FollowupAsync($"❌ Discord rejected the change.\n- Make sure Bob has the `Manage Webhooks` permission in <#{hook.ChannelId}>{(moving ? $" and {channel.Mention}" : "")}.", ephemeral: true);
                    return;
                }
            }

            var changes = new List<string>();
            if (name != null && name != hook.DisplayName) { changes.Add($"Name: **{hook.DisplayName}** → **{name}**"); hook.DisplayName = name; }
            if (moving) { changes.Add($"Channel: <#{hook.ChannelId}> → {channel.Mention}"); hook.ChannelId = channel.Id; }
            if (provider is { } p && p.Key() != hook.Provider)
            {
                changes.Add($"Provider: {WebhookListener.GetStrategy(hook.Provider).ServiceName} → {WebhookListener.GetStrategy(p.Key()).ServiceName}");
                hook.Provider = p.Key();
            }
            if (avatar != null) changes.Add("Avatar: updated");
            if (removeAvatar) changes.Add("Avatar: reset to default");

            await _db.SaveChangesAsync();

            var embed = new EmbedBuilder()
                .WithTitle($"Webhook Updated: {hook.DisplayName}")
                .WithDescription(changes.Count > 0 ? string.Join("\n", changes) : "Nothing was different, so nothing changed.")
                .WithColor(Bob.Bot.theme);

            if (avatar != null) embed.WithThumbnailUrl(avatar.Url);

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }

        /// <summary>
        /// Applies name/avatar/channel changes to the Discord webhook. Moving channels needs Bob's own
        /// permissions; everything else goes through the webhook's token so it works without them.
        /// </summary>
        private async Task ModifyDiscordWebhookAsync(WebhookInfo hook, string name, ITextChannel moveTo, Image? image, bool removeAvatar)
        {
            void Apply(WebhookProperties props)
            {
                if (name != null) props.Name = $"Bob | {name}";
                if (image != null) props.Image = image;
                if (removeAvatar) props.Image = null;
                if (moveTo != null) props.Channel = new Optional<ITextChannel>(moveTo);
            }

            if (name == null && image == null && !removeAvatar && moveTo == null)
                return; // Provider-only edit

            if (moveTo != null)
            {
                var restHook = await Context.Guild.GetWebhookAsync(hook.DiscordWebhookId)
                    ?? throw new InvalidOperationException("Could not find a webhook with the supplied credentials.");
                await restHook.ModifyAsync(Apply);
                return;
            }

            using var client = new DiscordWebhookClient(hook.DiscordWebhookId, hook.DiscordWebhookToken);
            await client.ModifyWebhookAsync(Apply);
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
