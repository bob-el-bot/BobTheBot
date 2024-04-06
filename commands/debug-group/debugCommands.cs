using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Commands.Attributes;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [DontAutoRegister]
    [RequireGuild(Bot.supportServerId)]
    [RequireOwner]
    [Group("debug", "All commands relevant to debugging.")]
    public class DebugGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [Group("log", "All debug commands for logging.")]
        public class LogGroup : InteractionModuleBase<SocketInteractionContext>
        {
            public const ulong DebugServerCategoryId = 1181420597138427967;
            public static Dictionary<ulong, IGuild> serversToLog = new();
            public static Dictionary<ulong, RestTextChannel> serverLogChannels = new();

            [SlashCommand("server", "Log all usage of Bob from a specific server (toggleable).")]
            public async Task ServerLogToggle(string serverId)
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
                        RestTextChannel restChannel = await Context.Guild.CreateTextChannelAsync(name: $"{serverId}", tcp => tcp.CategoryId = DebugServerCategoryId);
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

            [SlashCommand("delete-all-server-logs", "Stop logging and remove log channels for all currently logged channels")]
            public async Task DeleteAllServerLogs()
            {
                await DeferAsync();

                foreach (var guild in serversToLog)
                {
                    serversToLog.Remove(guild.Key);
                }

                foreach (var channel in serverLogChannels)
                {
                    serversToLog.Remove(channel.Key);
                }

                SocketCategoryChannel category = Context.Guild.GetCategoryChannel(DebugServerCategoryId);

                foreach (var channel in category.Channels)
                {
                    await channel.DeleteAsync();
                }

                await FollowupAsync(text: "✅ All data and channels relevant to server logging have been deleted.");
            }
        }

        [Group("stat", "All debug commands for stats")]
        public class StatsGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("exact-user-count", "Calculates the EXACT user count excluding bots.")]
            public async Task ExactUserCount()
            {
                await DeferAsync();

                int totalUsers = 0;

                foreach (var guild in Bot.Client.Guilds)
                {
                    foreach (var user in guild.Users)
                    {
                        if (user.IsBot == false)
                        {
                            totalUsers++;
                        }
                    }
                }

                await FollowupAsync(text: $"✅ The exact amount of real users bob has: `{totalUsers}`");
            }
        }
    }
}