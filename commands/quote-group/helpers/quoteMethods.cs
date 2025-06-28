using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Bob.PremiumInterface;
using Bob.Time.Timestamps;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Commands.Helpers
{
    public static class QuoteMethods
    {
        private static readonly Color DefaultColor = new(0x2C2F33);

        public static async Task<Server> GetServerAsync(ulong guildId)
        {
            using var scope = Bot.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            return await context.GetServer(guildId);
        }

        /// <summary>
        /// Validates that the server has a quote channel set, that the channel exists, 
        /// and that the bot has the necessary permissions in that channel.
        /// </summary>
        /// <param name="server">The server configuration object containing the QuoteChannelId.</param>
        /// <param name="context">The interaction context for accessing guild and bot information.</param>
        /// <returns>True if validation passes; otherwise, false.</returns>
        public static async Task<bool> ValidateServerAndChannel(Server server, SocketInteractionContext context)
        {
            if (server.QuoteChannelId == null)
            {
                await context.Interaction.FollowupAsync($"❌ Use {Help.GetCommandMention("quote channel")} first (a quote channel is not set in this server).", ephemeral: true);
                return false;
            }

            var channel = context.Guild.GetChannel((ulong)server.QuoteChannelId);
            if (channel == null)
            {
                await context.Interaction.FollowupAsync("❌ The currently set quote channel no longer exists.", ephemeral: true);
                return false;
            }

            var permissions = context.Guild.GetUser(context.Client.CurrentUser.Id).GetPermissions(channel);
            if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks)
            {
                await context.Interaction.FollowupAsync($"❌ Bob is missing required permissions in the channel <#{server.QuoteChannelId}>.", ephemeral: true);
                return false;
            }

            return true;
        }

        public static async Task<bool> ValidateServerAndChannel(Server server, ShardedInteractionContext context)
        {
            if (server.QuoteChannelId == null)
            {
                await context.Interaction.FollowupAsync($"❌ Use {Help.GetCommandMention("quote channel")} first (a quote channel is not set in this server).", ephemeral: true);
                return false;
            }

            var channel = context.Guild.GetChannel((ulong)server.QuoteChannelId);
            if (channel == null)
            {
                await context.Interaction.FollowupAsync("❌ The currently set quote channel no longer exists.", ephemeral: true);
                return false;
            }

            var permissions = context.Guild.GetUser(context.Client.CurrentUser.Id).GetPermissions(channel);
            if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks)
            {
                await context.Interaction.FollowupAsync($"❌ Bob is missing required permissions in the channel <#{server.QuoteChannelId}>.", ephemeral: true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that the quote length is within the allowed range for the server settings and Discord limits.
        /// </summary>
        /// <param name="quote">The text of the quote to validate.</param>
        /// <param name="server">The server configuration containing max and min quote lengths.</param>
        /// <param name="context">The interaction context for sending a follow-up message if invalid.</param>
        /// <returns>True if the quote length is valid; otherwise, false.</returns>
        public static async Task<bool> ValidateQuoteLength(string quote, Server server, SocketInteractionContext context)
        {
            if (quote.Length > (server.MaxQuoteLength ?? 4096) || quote.Length < server.MinQuoteLength)
            {
                await context.Interaction.FollowupAsync($"❌ The quote *cannot* be made because it contains **{quote.Length}** characters.\n- this server's maximum quote length is **{server.MaxQuoteLength}**.\n- this server's minimum quote length is **{server.MinQuoteLength}**.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
                return false;
            }

            return true;
        }

        public static async Task<bool> ValidateQuoteLength(string quote, Server server, ShardedInteractionContext context)
        {
            if (quote.Length > (server.MaxQuoteLength ?? 4096) || quote.Length < server.MinQuoteLength)
            {
                await context.Interaction.FollowupAsync($"❌ The quote *cannot* be made because it contains **{quote.Length}** characters.\n- this server's maximum quote length is **{server.MaxQuoteLength}**.\n- this server's minimum quote length is **{server.MinQuoteLength}**.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates whether the user can add tags to the quote, checking if they have a premium subscription.
        /// </summary>
        /// <param name="tag1">The first tag (optional).</param>
        /// <param name="tag2">The second tag (optional).</param>
        /// <param name="tag3">The third tag (optional).</param>
        /// <param name="context">The interaction context for checking premium status and sending feedback.</param>
        /// <returns>True if tags are allowed; otherwise, false.</returns>
        public static async Task<bool> ValidateTags(string tag1, string tag2, string tag3, SocketInteractionContext context)
        {
            if ((!string.IsNullOrWhiteSpace(tag1) || !string.IsNullOrWhiteSpace(tag2) || !string.IsNullOrWhiteSpace(tag3)) && await Premium.IsPremiumAsync(context.Interaction.Entitlements, context.User.Id) == false)
            {
                await context.Interaction.FollowupAsync("❌ You cannot add tags without a premium subscription.", components: Premium.GetComponents());
                return false;
            }

            return true;
        }

        public static async Task<bool> ValidateTags(string tag1, string tag2, string tag3, ShardedInteractionContext context)
        {
            if ((!string.IsNullOrWhiteSpace(tag1) || !string.IsNullOrWhiteSpace(tag2) || !string.IsNullOrWhiteSpace(tag3)) && await Premium.IsPremiumAsync(context.Interaction.Entitlements, context.User.Id) == false)
            {
                await context.Interaction.FollowupAsync("❌ You cannot add tags without a premium subscription.", components: Premium.GetComponents());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a Discord embed for a quote, formatting it based on the quote's length and content.
        /// </summary>
        /// <param name="quote">The text of the quote.</param>
        /// <param name="user">The user who originally sent the quote.</param>
        /// <param name="timestamp">The timestamp when the original quote was made.</param>
        /// <param name="quotedByUsername">The username of the person quoting.</param>
        /// <param name="tag1">Optional first tag.</param>
        /// <param name="tag2">Optional second tag.</param>
        /// <param name="tag3">Optional third tag.</param>
        /// <param name="originalMessageUrl">Optional URL to the original message.</param>
        /// <returns>The constructed quote embed.</returns>
        public static Embed CreateQuoteEmbed(string quote, SocketUser user, DateTimeOffset timestamp, string quotedByUsername, string tag1 = "", string tag2 = "", string tag3 = "", string originalMessageUrl = null)
        {
            var formattedQuote = quote.Length <= 4094 && quote[0] != '"' && quote[^1] != '"' ? $"\"{quote}\"" : quote;
            var containsMentionsOrLinks = quote.Contains("<@") || quote.Contains("<#") || quote.Contains("http");

            EmbedBuilder embed = new();

            if (formattedQuote.Length <= 256 && !containsMentionsOrLinks)
            {
                embed.Title = formattedQuote;
                embed.Description = $"-{user.Mention}, {Timestamp.FromDateTimeOffset(timestamp, Timestamp.Formats.Relative)}";
            }
            else
            {
                var description = formattedQuote.Length <= 4092 ? $"**{formattedQuote}**" : formattedQuote;
                var footer = $"\n-{user.Mention}, {Timestamp.FromDateTimeOffset(timestamp, Timestamp.Formats.Relative)}";

                if (description.Length + footer.Length > 4096)
                {
                    embed.AddField("Sent By", user.Mention, true);
                    embed.AddField("Time", Timestamp.FromDateTimeOffset(timestamp, Timestamp.Formats.Relative), true);
                }
                else
                {
                    description += footer;
                }

                embed.Description = description;
            }

            embed.Color = DefaultColor;
            embed.WithAuthor(user.GlobalName ?? user.Username, user.GetAvatarUrl());

            if (!string.IsNullOrWhiteSpace(originalMessageUrl))
            {
                embed.AddField("Original Message", originalMessageUrl);
            }

            StringBuilder footerText = new();

            if (!string.IsNullOrWhiteSpace(tag1) || !string.IsNullOrWhiteSpace(tag2) || !string.IsNullOrWhiteSpace(tag3))
            {
                footerText.Append($"Tag(s): {string.Join(", ", new[] { tag1, tag2, tag3 }.Where(tag => !string.IsNullOrWhiteSpace(tag)))} | ");
            }

            footerText.Append($"Quoted by {quotedByUsername}");
            embed.WithFooter(footerText.ToString());

            return embed.Build();
        }

        /// <summary>
        /// Sends a quote embed to the specified channel and confirms success to the user.
        /// </summary>
        /// <param name="channelId">The ID of the channel to send the quote to.</param>
        /// <param name="embed">The embed to send.</param>
        /// <param name="successMessage">The message to display upon successful sending.</param>
        /// <param name="context">The interaction context for sending a follow-up message.</param>
        public static async Task SendQuoteAsync(ulong? channelId, Embed embed, string successMessage, SocketInteractionContext context)
        {
            var channel = context.Guild.GetChannel((ulong)channelId);
            await ((ISocketMessageChannel)channel).SendMessageAsync(embed: embed);
            await context.Interaction.FollowupAsync($"🖊️ {successMessage} <#{channelId}>.", ephemeral: true);
        }

        public static async Task SendQuoteAsync(ulong? channelId, Embed embed, string successMessage, ShardedInteractionContext context)
        {
            var channel = context.Guild.GetChannel((ulong)channelId);
            await ((ISocketMessageChannel)channel).SendMessageAsync(embed: embed);
            await context.Interaction.FollowupAsync($"🖊️ {successMessage} <#{channelId}>.", ephemeral: true);
        }
    }
}