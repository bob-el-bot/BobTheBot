using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using static Bob.Monitoring.PerformanceStats;

namespace Bob.Debug
{
    public static class Logger
    {
        private static readonly ulong logChannelId = 1160105468082004029;
        private static readonly ulong devLogChannelId = 1196575302143459388;
        private static readonly ulong feedbackChannelId = 1301279825264115832;
        private const string ErrTooLongSuffix = " | **ERR TOO LONG**";

        // Lazy channels so we fetch them only once:
        public static readonly Lazy<SocketTextChannel> feedbackChannel = new(() =>
            (SocketTextChannel)Bot.Client
                .GetGuild(Bot.supportServerId)
                .GetChannel(feedbackChannelId)
        );

        private static readonly Lazy<SocketTextChannel> logChannel = new(() =>
            (SocketTextChannel)Bot.Client
                .GetGuild(Bot.supportServerId)
                .GetChannel(
                    Bot.Token == Environment.GetEnvironmentVariable("TEST_DISCORD_TOKEN")
                        ? devLogChannelId
                        : logChannelId
                    )
        );

        /// <summary>
        /// Builds the common log header: timestamp, CPU/RAM usage, shard, location, user.
        /// </summary>
        private static async Task<string> BuildHeaderAsync(IInteractionContext ctx)
        {
            bool isGuild = ctx.Interaction.GuildId != null;
            string location = !isGuild
                ? "a DM"
                : (Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId)?.ToString() ?? "User Install");
            int? shardId = !isGuild
                ? null
                : (ctx as ShardedInteractionContext).Client.GetShardIdFor(ctx.Guild);

            var cpu = await GetCpuUsageForProcess();
            var ram = GetRamUsageForProcess();
            IUser user = ctx.User;

            return $"`{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpu, ram)} | " +
                   $"Shard: {(shardId == null ? "N" : shardId.ToString())} | " +
                   $"Location: {location} | User: {user.GlobalName}, {user.Id}`";
        }

        /// <summary>
        /// Builds a triple‐backtick block of command usage (name + all options).
        /// </summary>
        private static string BuildCommandUsage(SlashCommandInfo info, SocketSlashCommand command)
        {
            var sb = new StringBuilder();
            string name = info.IsTopLevelCommand
                ? $"/{info.Name}"
                : $"/{info.Module.SlashGroupName} {info.Name}";
            sb.Append(name);

            if (command != null)
            {
                foreach (var opt in command.Data.Options)
                    sb.Append($" {opt.Name}: {opt.Value ?? "null"}");
            }

            return $"```{sb}```";
        }

        /// <summary>
        /// If the full message would exceed Discord’s 2000‐char limit,
        /// trim the errorReason down so that adding ErrTooLongSuffix fits.
        /// </summary>
        private static void TrimErrorReason(ref string errorReason, int currentTotalLength)
        {
            int over = currentTotalLength + ErrTooLongSuffix.Length - 2000;

            if (over > 0 && !string.IsNullOrEmpty(errorReason) && errorReason.Length > over)
            {
                errorReason = errorReason[..^over];
            }
        }

        public static async Task LogErrorToDiscord(IInteractionContext ctx, SlashCommandInfo info, string errorReason = null)
        {
            var header = await BuildHeaderAsync(ctx);
            var commandSlash = ctx.Interaction as SocketSlashCommand;
            var usageBlock = BuildCommandUsage(info, commandSlash);

            string errorBlock = string.IsNullOrEmpty(errorReason)
                ? ""
                : $"Error: ```cs\n{errorReason}```";

            string details = $"Command type: **{info.CommandType}** | Method name in code: **{info.MethodName}**";

            string message = $"{header}\n{usageBlock}{errorBlock}{details}";

            if (message.Length > 2000)
            {
                TrimErrorReason(ref errorReason, message.Length);
                errorBlock = string.IsNullOrEmpty(errorReason)
                    ? ""
                    : $"Error: ```cs\n{errorReason}```";
                message = $"{header}\n{usageBlock}{errorBlock}{details}{ErrTooLongSuffix}";
            }

            await logChannel.Value.SendMessageAsync(message);
        }

        public static async Task LogErrorToDiscord(IInteractionContext ctx, string errorReason)
        {
            var header = await BuildHeaderAsync(ctx);
            string errorBlock = $"Error: ```cs\n{errorReason}```";
            string message = $"{header}\n{errorBlock}";

            if (message.Length > 2000)
            {
                TrimErrorReason(ref errorReason, message.Length);
                message = $"{header}\nError: ```cs\n{errorReason}```{ErrTooLongSuffix}";
            }

            await logChannel.Value.SendMessageAsync(message);
        }

        public static async Task LogFeedbackToDiscord(string guildName, string[] reasons)
        {
            // preserve the trailing comma+space exactly as before
            var formatted = string.Join(", ", reasons) + (reasons.Length > 0 ? ", " : "");
            await feedbackChannel.Value
                .SendMessageAsync($"`Location: {guildName}`\n**Reason(s):** {formatted}");
        }

        public static async Task LogServerUseToDiscord(RestTextChannel channel, IInteractionContext ctx, SlashCommandInfo info, string errorReason = null)
        {
            var header = await BuildHeaderAsync(ctx);
            var commandSlash = ctx.Interaction as SocketSlashCommand;
            var usageBlock = BuildCommandUsage(info, commandSlash);

            string errorBlock = string.IsNullOrEmpty(errorReason)
                ? ""
                : $"Error: ```cs\n{errorReason}```";
            string details = $"Command type: **{info.CommandType}** | Method name in code: **{info.MethodName}**";

            // Note: original version did *not* trim the server‐use log
            string message = $"{header}\n{usageBlock}{errorBlock}{details}";
            await channel.SendMessageAsync(message);
        }

        public static async Task HandleUnexpectedError(IInteractionContext ctx, Exception ex, bool deferred, bool ephemeral = true)
        {
            var reply =
                $"❌ An unexpected error occurred: {ex.Message}\n" +
                "- Try again later.\n" +
                "- The developers have been notified, but you can join " +
                "[Bob's Official Server](https://discord.gg/HvGMRZD8jQ) " +
                "and provide us with more details if you want.";

            if (deferred)
            {
                await ctx.Interaction.FollowupAsync(reply, ephemeral: ephemeral);
            }
            else
            {
                await ctx.Interaction.RespondAsync(reply, ephemeral: ephemeral);
            }

            // Still notify dev log:
            await LogErrorToDiscord(ctx, ex.ToString());
        }
    }
}
