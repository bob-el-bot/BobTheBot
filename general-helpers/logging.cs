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
            var location = ctx.Interaction.GuildId == null ? "a DM" : Bot.Client.GetGuild(ulong.Parse(ctx.Interaction.GuildId.ToString())).ToString();
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

        public static async Task LogFeedbackToDiscord(SocketTextChannel channel, string guildName, string[] reasons)
        {
            StringBuilder formattedReasons = new();

            foreach (var reason in reasons)
            {
                formattedReasons.Append($"{reason}, ");
            }

            await channel.SendMessageAsync($"`Location: {guildName}`\n**Reason(s):** {formattedReasons}");
        }
    }
}