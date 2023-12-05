using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace Commands.Helpers
{
    public static class Debug
    {
        public static async Task Log(RestTextChannel channel, IInteractionContext ctx, SlashCommandInfo info, string errorReason = null)
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

            await channel.SendMessageAsync($"`{DateTime.Now:dd/MM. H:mm:ss} | Location: {location} | User: {user.GlobalName}, {user.Id}`\n```{commandUsage}```{(errorReason == null ? "" : "Error: ```cs\n{errorReason}```")}Command type: **{commandType}** | Method name in code: **{methodName}**");
        }
    }
}