using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using static Performance.Stats;
using Discord.WebSocket;

namespace Debug
{
    public static class Logger
    {
        public static async Task LogErrorToDiscord(SocketTextChannel channel, IInteractionContext ctx, SlashCommandInfo info, string errorReason = null)
        {
            string location = (ctx.Interaction.GuildId == null) ? "a DM" : (Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId) == null ? "User Install" : Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId).ToString());
            var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
            string methodName = info.MethodName;
            IUser user = ctx.User;
            StringBuilder commandUsage = new();
            commandUsage.Append($"{commandName}");
            string commandType = info.CommandType.ToString();

            if (ctx.Interaction is SocketSlashCommand command)
            {
                foreach (var option in command.Data.Options)
                {
                    commandUsage.Append($" {option.Name}: {option.Value ?? "null"}");
                }
            }

            var cpuUsage = await GetCpuUsageForProcess();
            var ramUsage = GetRamUsageForProcess();

            // Ensure message length doesn't exceed 2000 characters
            string errorMessage = (errorReason == null) ? "" : $"Error: ```cs\n{errorReason}```";
            string message = $"`{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | User: {user.GlobalName}, {user.Id}`\n```{commandUsage}```{errorMessage}Command type: **{commandType}** | Method name in code: **{methodName}**";

            if (message.Length > 2000)
            {
                // Calculate the maximum length for the non-error section
                int overMaxLengthBy = message.Length + " | **ERR TOO LONG**".Length - 2000;
                int maxErrorReasonLength = errorReason.Length - overMaxLengthBy;
                
                errorReason = errorReason[..maxErrorReasonLength];

                message = $"`{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | User: {user.GlobalName}, {user.Id}`\n```{commandUsage}```{(errorReason == null ? "" : $"Error: ```cs\n{errorReason}```")}Command type: **{commandType}** | Method name in code: **{methodName}** | **ERR TOO LONG**";
            }

            await channel.SendMessageAsync(message);
        }

        public static async Task LogErrorToDiscord(SocketTextChannel channel, IInteractionContext ctx, string errorReason)
        {
            string location = (ctx.Interaction.GuildId == null) ? "a DM" : (Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId) == null ? "User Install" : Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId).ToString());
            IUser user = ctx.User;

            var cpuUsage = await GetCpuUsageForProcess();
            var ramUsage = GetRamUsageForProcess();

            // Ensure message length doesn't exceed 2000 characters
            string errorMessage = $"Error: ```cs\n{errorReason}```";
            string message = $"`{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | User: {user.GlobalName}, {user.Id}`\n{errorMessage}";

            if (message.Length > 2000)
            {
                // Calculate the maximum length for the non-error section
                int overMaxLengthBy = message.Length + " | **ERR TOO LONG**".Length - 2000;
                int maxErrorReasonLength = errorReason.Length - overMaxLengthBy;
                
                errorReason = errorReason[..maxErrorReasonLength];

                message = $"`{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | User: {user.GlobalName}, {user.Id}`\nError: ```cs\n{errorReason}``` | **ERR TOO LONG**";
            }

            await channel.SendMessageAsync(message);
        }

        public static async Task LogFeedbackToDiscord(SocketTextChannel channel, string guildName, string[] reasons)
        {
            StringBuilder formattedReasons = new();

            foreach (var reason in reasons)
            {
                formattedReasons.Append($"{reason}, ");
            }

            await channel.SendMessageAsync($"`Location: {guildName}`\n**Reason(s):** {formattedReasons}");
        }

        public static async Task LogServerUseToDiscord(RestTextChannel channel, IInteractionContext ctx, SlashCommandInfo info, string errorReason = null)
        {
            string location = (ctx.Interaction.GuildId == null) ? "a DM" : (Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId) == null ? "User Install" : Bot.Client.GetGuild((ulong)ctx.Interaction.GuildId).ToString());
            var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
            string methodName = info.MethodName;
            IUser user = ctx.User;
            StringBuilder commandUsage = new();
            commandUsage.Append($"{commandName}");
            string commandType = info.CommandType.ToString();

            if (ctx.Interaction is SocketSlashCommand command)
            {
                foreach (var option in command.Data.Options)
                {
                    commandUsage.Append($" {option.Name}: {option.Value ?? "null"}");
                }
            }

            var cpuUsage = await GetCpuUsageForProcess();
            var ramUsage = GetRamUsageForProcess();

            await channel.SendMessageAsync($"`{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | User: {user.GlobalName}, {user.Id}`\n```{commandUsage}```{(errorReason == null ? "" : $"Error: ```cs\n{errorReason}```")}Command type: **{commandType}** | Method name in code: **{methodName}**");
        }
    }
}