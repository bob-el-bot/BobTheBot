using System.Collections.Generic;
using System.Threading.Tasks;
using BadgeInterface;
using Commands.Attributes;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [DontAutoRegister]
    [RequireGuild(Bot.supportServerId)]
    [RequireTeam]
    [Group("debug", "All commands relevant to debugging.")]
    public class DebugGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [Group("log", "All debug commands for logging.")]
        public class LogGroup : InteractionModuleBase<SocketInteractionContext>
        {
            public const ulong DebugServerCategoryId = 1181420597138427967;
            public static Dictionary<ulong, IGuild> ServersToLog { get; set; } = new();
            public static Dictionary<ulong, RestTextChannel> ServerLogChannels { get; set; } = new();
            public static bool LogEverything { get; set; } = false;

            [SlashCommand("server", "Log all usage of Bob from a specific server (toggleable).")]
            public async Task ServerLogToggle(string serverId)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(serverId, out ulong id);
                if (conversionResult == false)
                {
                    await FollowupAsync(text: $"❌ The server ID `{serverId}` is invalid.");
                    return;
                }

                if (ServersToLog.ContainsKey(id))
                {
                    ServersToLog.Remove(id);
                    ServerLogChannels.TryGetValue(id, out RestTextChannel channel);

                    await FollowupAsync(text: $"✅ Debug logging for the server ID `{serverId}` has been **stopped** and {channel.Mention} will be deleted in 5 seconds.");

                    await Task.Delay(5000);
                    await channel.DeleteAsync();
                    ServerLogChannels.Remove(id);
                }
                else
                {
                    IGuild guild = Bot.Client.GetGuild(id);
                    if (guild != null)
                    {
                        RestTextChannel restChannel = await Context.Guild.CreateTextChannelAsync(name: $"{serverId}", tcp => tcp.CategoryId = DebugServerCategoryId);
                        await FollowupAsync(text: $"✅ Debug logging for the server ID `{serverId}` has **started** in {restChannel.Mention}.");
                        ServerLogChannels.Add(key: id, value: restChannel);

                        ServersToLog.Add(key: id, value: guild);
                    }
                    else
                    {
                        await FollowupAsync(text: $"❌ Debug logging for the server ID `{serverId}` has **not** started.\n- Bob is not in the provided server.");
                    }
                }
            }

            [SlashCommand("delete-all-server-logs", "Stop logging and remove log channels for all currently logged channels")]
            public async Task DeleteAllServerLogs()
            {
                await DeferAsync();

                foreach (var guild in ServersToLog)
                {
                    ServersToLog.Remove(guild.Key);
                }

                foreach (var channel in ServerLogChannels)
                {
                    ServersToLog.Remove(channel.Key);
                }

                SocketCategoryChannel category = Context.Guild.GetCategoryChannel(DebugServerCategoryId);

                foreach (var channel in category.Channels)
                {
                    await channel.DeleteAsync();
                }

                await FollowupAsync(text: "✅ All data and channels relevant to server logging have been deleted.");
            }

            [SlashCommand("everything", "Log all usage of Bob.")]
            public async Task LogEverythingToggle()
            {
                await DeferAsync();

                if (LogEverything == true)
                {
                    LogEverything = false;

                    await FollowupAsync(text: $"✅ Debug logging for **everything** has been stopped.");
                }
                else
                {
                    LogEverything = true;

                    SocketTextChannel logChannel = (SocketTextChannel)Bot.Client.GetGuild(Bot.supportServerId).GetChannel(Bot.systemLogChannelId);

                    await FollowupAsync(text: $"✅ Debug logging for **everything** has **started** in {logChannel.Mention}.");
                }
            }
        }

        [Group("stat", "All debug commands for stats")]
        public class StatsGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("all-tables", "Shows all stats relevant to the all tables.")]
            public async Task AllTableStats()
            {
                await DeferAsync();

                using var context = new BobEntities();
                ulong entryCount = await context.GetTotalEntries();
                int userEntriesCount = await context.User.CountAsync();
                int serverEntriesCount = await context.Server.CountAsync();
                int newsChannelEntriesCount = await context.NewsChannel.CountAsync();
                double size = await context.GetDatabaseSizeBytes();

                var embed = new EmbedBuilder
                {
                    Color = Bot.theme,
                    Title = "✅ Showing stats for all tables",
                };

                embed.AddField(name: "Total Entries", value: $"`{entryCount}`", inline: true)
                    .AddField(name: "User Entries", value: $"`{userEntriesCount}`", inline: true)
                    .AddField(name: "Server Entries", value: $"`{serverEntriesCount}`", inline: true)
                    .AddField(name: "NewsChannel Entries", value: $"`{newsChannelEntriesCount}`", inline: true)
                    .AddField(name: "Size (Bytes)", value: $"`{size}`", inline: true)
                    .AddField(name: "Size (MegaBytes)", value: $"`{size / 1024 / 1024}`", inline: true)
                    .AddField(name: "Size (GigaBytes)", value: $"`{size / 1024 / 1024 / 1024}`", inline: true);

                await FollowupAsync(embed: embed.Build());
            }

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

        [Group("database", "All debug commands for the database")]
        public class DatabaseGroup : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("get-user", "Gets the user object of a given user.")]
            public async Task GetUser(IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);
                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Bot.Client.GetUserAsync(parsedId);

                if (discordUser.IsBot)
                {
                    await FollowupAsync(text: $"❌ You **cannot** perform this action on bots.");
                }
                else if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else
                {
                    User dbUser;
                    using var context = new BobEntities();
                    dbUser = await context.GetUser(user == null ? parsedId : user.Id);

                    await FollowupAsync(text: $"✅ `Showing User: {discordUser.GlobalName}, {discordUser.Id}`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                }
            }

            [SlashCommand("give-user-badge", "Gives the given user the given badge.")]
            public async Task GiveUserBadge(Badges.Badges badge, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);

                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Bot.Client.GetUserAsync(parsedId);

                if (discordUser.IsBot)
                {
                    await FollowupAsync(text: $"❌ You **cannot** perform this action on bots.");
                }
                else if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else
                {
                    User dbUser;
                    using var context = new BobEntities();
                    dbUser = await context.GetUser(user == null ? parsedId : user.Id);

                    await Badge.GiveUserBadge(dbUser, badge);

                    await FollowupAsync(text: $"✅ `Badge given to User: {discordUser.GlobalName}, {discordUser.Id}`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                }
            }

            [SlashCommand("remove-user-badge", "Removes the given badge from the given user.")]
            public async Task RemoveUserBadge(Badges.Badges badge, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);

                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Bot.Client.GetUserAsync(parsedId);

                if (discordUser.IsBot)
                {
                    await FollowupAsync(text: $"❌ You **cannot** perform this action on bots.");
                }
                else if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else
                {
                    User dbUser;
                    using var context = new BobEntities();
                    dbUser = await context.GetUser(user == null ? parsedId : user.Id);

                    await Badge.RemoveUserBadge(dbUser, badge);

                    await FollowupAsync(text: $"✅ `Badge removed from User: {discordUser.GlobalName}, {discordUser.Id}`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                }
            }

            [SlashCommand("get-server", "Gets the server object with the given ID.")]
            public async Task GetServer(string serverId)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(serverId, out ulong parsedId);
                if (conversionResult == false)
                {
                    await FollowupAsync(text: $"❌ The given server ID: `{serverId}` is invalid.");
                    return;
                }

                IGuild discordServer = Bot.Client.GetGuild(parsedId);

                if (discordServer == null)
                {
                    await FollowupAsync(text: $"❌ Bob could not find the server with the id: `{serverId}`.\n- This probably means Bob is not in the server.");
                }
                else
                {
                    Server dbServer;
                    using var context = new BobEntities();
                    dbServer = await context.GetServer(parsedId);

                    await FollowupAsync(text: $"✅ `Showing Server: {discordServer.Name}, {discordServer.Id}`\n```cs\nCustom Welcome Message: {dbServer.CustomWelcomeMessage}\nWelcome: {dbServer.Welcome}\nQuote Channel ID: {dbServer.QuoteChannelId}\nMax Quote Length: {dbServer.MaxQuoteLength}\nMin Quote Length: {dbServer.MinQuoteLength}```");
                }
            }
        }
    }
}