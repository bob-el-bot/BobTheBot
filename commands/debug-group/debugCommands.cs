using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Commands.Attributes;
using Discord;
using Discord.Interactions;
using Discord.Rest;

namespace Commands
{
    [EnabledInDm(false)]
    [DontAutoRegister]
    [RequireGuild(Bot.supportServerId)]
    [Group("debug", "All commands relevant to debugging.")]
    public class DebugGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(false)]
        [DontAutoRegister]
        [RequireGuild(Bot.supportServerId)]
        [Group("log", "All debug commands for logging.")]
        public class LogGroup : InteractionModuleBase<SocketInteractionContext>
        {
            public static Dictionary<ulong, IGuild> serversToLog = new();
            public static Dictionary<ulong, RestTextChannel> serverLogChannels = new();

            [EnabledInDm(false)]
            [SlashCommand("server", "Log all usage of Bob from a specific server (Use again to turn off logs).")]
            public async Task ServerLog(string serverId)
            {
                await DeferAsync();
                ulong id = ulong.Parse(serverId);
                if (serversToLog.ContainsKey(id))
                {
                    serversToLog.Remove(id);
                    serverLogChannels.TryGetValue(id, out RestTextChannel channel);

                    await FollowupAsync(text: $"✅ Debug logging for the serverId `{serverId}` has been **stopped** and {channel.Mention} will be deleted in 5 seconds.");

                    await Task.Delay(5000);
                    await channel.DeleteAsync();
                    serverLogChannels.Remove(id);
                }
                else
                {
                    IGuild guild = Bot.Client.GetGuild(id);
                    if (guild != null)
                    {
                        RestTextChannel restChannel = await Context.Guild.CreateTextChannelAsync(name: $"{serverId}", tcp => tcp.CategoryId = 1181420597138427967);
                        await FollowupAsync(text: $"✅ Debug logging for the serverId `{serverId}` has **started** in {restChannel.Mention}.");
                        serverLogChannels.Add(key: id, value: restChannel);

                        serversToLog.Add(key: id, value: guild);
                    }
                    else
                    {
                        await FollowupAsync(text: $"❌ Debug logging for the serverId `{serverId}` has **not** started.\n- Bob is not in the provided server.");
                    }
                }
            }
        }
    }
}