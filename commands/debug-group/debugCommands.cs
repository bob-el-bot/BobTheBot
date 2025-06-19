using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bob.BadgeInterface;
using Bob.Commands.Attributes;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Bob.Moderation;
using static Bob.ApiInteractions.Interface;
using static Bob.Bot;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [DontAutoRegister]
    [RequireGuild(Bot.supportServerId)]
    [RequireTeam]
    [Group("debug", "All commands relevant to debugging.")]
    public class DebugGroup : InteractionModuleBase<ShardedInteractionContext>
    {
        [Group("log", "All debug commands for logging.")]
        public class LogGroup : InteractionModuleBase<ShardedInteractionContext>
        {
            private static readonly ulong DebugServerCategoryId = 1181420597138427967;
            public static Dictionary<ulong, IGuild> ServersToLog { get; set; } = [];
            public static Dictionary<ulong, RestTextChannel> ServerLogChannels { get; set; } = [];
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
        public class StatsGroup : InteractionModuleBase<ShardedInteractionContext>
        {
            [SlashCommand("entitlements", "view all app entitlements.")]
            public async Task GetEntitlements()
            {
                StringBuilder response = new();

                await foreach (var entitlementCollection in Client.Rest.GetEntitlementsAsync())
                {
                    foreach (var entitlement in entitlementCollection)
                    {
                        response.AppendLine($"{entitlement.SkuId} - {entitlement.Type} - {entitlement.EndsAt} - {entitlement.UserId}");
                    }
                }

                await RespondAsync(text: response.ToString());
            }

            [SlashCommand("all-tables", "Shows all stats relevant to the all tables.")]
            public async Task AllTableStats()
            {
                await DeferAsync();

                using var context = new BobEntities();
                ulong entryCount = await context.GetTotalEntries();
                int userEntriesCount = await context.User.CountAsync();
                int blackListEntriesCount = await context.BlackListUser.CountAsync();
                int serverEntriesCount = await context.Server.CountAsync();
                int newsChannelEntriesCount = await context.NewsChannel.CountAsync();
                int scheduledMessageEntriesCount = await context.ScheduledMessage.CountAsync();
                int scheduledAnnouncementEntriesCount = await context.ScheduledAnnouncement.CountAsync();
                int welcomeImageEntriesCount = await context.WelcomeImage.CountAsync();
                double size = await context.GetDatabaseSizeBytes();

                var embed = new EmbedBuilder
                {
                    Color = Bot.theme,
                    Title = "✅ Showing stats for all tables",
                };

                embed.AddField(name: "Total Entries", value: $"`{entryCount}`", inline: true)
                    .AddField(name: "User Entries", value: $"`{userEntriesCount}`", inline: true)
                    .AddField(name: "BlackListUser Entries", value: $"`{blackListEntriesCount}`", inline: true)
                    .AddField(name: "Server Entries", value: $"`{serverEntriesCount}`", inline: true)
                    .AddField(name: "Welcome Image Entries", value: $"`{welcomeImageEntriesCount}`", inline: true)
                    .AddField(name: "NewsChannel Entries", value: $"`{newsChannelEntriesCount}`", inline: true)
                    .AddField(name: "Scheduled Message Entries", value: $"`{scheduledMessageEntriesCount}`", inline: true)
                    .AddField(name: "Scheduled Announcement Entries", value: $"`{scheduledAnnouncementEntriesCount}`", inline: true)
                    .AddField(name: "Size (Bytes)", value: $"`{size}`", inline: true)
                    .AddField(name: "Size (MegaBytes)", value: $"`{size / 1024 / 1024}`", inline: true)
                    .AddField(name: "Size (GigaBytes)", value: $"`{size / 1024 / 1024 / 1024}`", inline: true);

                await FollowupAsync(embed: embed.Build());
            }

            [SlashCommand("exact-user-count", "Calculates the EXACT user count excluding bots and shows top servers.")]
            public async Task ExactUserCount()
            {
                await DeferAsync();

                int totalUsers = 0;

                DiscordRestClient client = new();
                await client.LoginAsync(TokenType.Bot, Bot.Token);
                var guilds = await client.GetGuildsAsync(withCounts: true);

                // List to store guilds with their member count
                var guildUserCounts = new List<(string guildName, int userCount)>();

                foreach (IGuild guild in guilds)
                {
                    var userCount = guild.ApproximateMemberCount.GetValueOrDefault(0);
                    totalUsers += userCount;

                    // Add the guild and its user count to the list
                    guildUserCounts.Add((guild.Name, userCount));
                }

                // Sort the guilds by user count in descending order
                var topServers = guildUserCounts.OrderByDescending(g => g.userCount).Take(5).ToList();

                // Build the message with the top servers and their user counts
                var topServersMessage = new StringBuilder();
                topServersMessage.AppendLine("Top 5 biggest servers (by user count):");

                foreach (var (guildName, userCount) in topServers)
                {
                    topServersMessage.AppendLine($"**{guildName}**: `{userCount}` users");
                }

                // Send the response
                await FollowupAsync(text: $"✅ The exact amount of real users Bob has is: `{totalUsers}`\n\n{topServersMessage}");
            }

            [SlashCommand("update-bot-sites-server-count", "Updates the Top.GG and discord.bots server count.")]
            public async Task UpdateTopGGServerCount()
            {
                await DeferAsync();

                int totalServers = Bot.Client.Guilds.Count;
                var message = new StringBuilder();

                async Task<HttpStatusCode> UpdateServerCount(string url, string token, string jsonKey)
                {
                    var content = new StringContent($"{{\"{jsonKey}\":{totalServers}}}", Encoding.UTF8, "application/json");
                    return await PostToAPI(url, Environment.GetEnvironmentVariable(token), content);
                }

                var topGGResult = await UpdateServerCount("https://top.gg/api/bots/705680059809398804/stats", "TOP_GG_TOKEN", "server_count");
                var discordBotsResult = await UpdateServerCount("https://discord.bots.gg/api/v1/bots/705680059809398804/stats", "DISCORD_BOTS_TOKEN", "guildCount");

                void AppendResult(string siteName, string url, HttpStatusCode result)
                {
                    if (result == HttpStatusCode.OK)
                    {
                        message.AppendLine($"✅ The exact amount of servers has been updated to: `{totalServers}` on [{siteName}](<{url}>)");
                    }
                    else
                    {
                        message.AppendLine($"❌ There was an error updating the server count on {siteName}.\n- Status Code: `{result}`");
                    }
                }

                AppendResult("Top.GG", "https://top.gg/bot/705680059809398804", topGGResult);
                AppendResult("Discord.Bots", "https://discord.bots.gg/bots/705680059809398804", discordBotsResult);

                await FollowupAsync(text: message.ToString());
            }
        }

        [Group("database", "All debug commands for the database")]
        public class DatabaseGroup : InteractionModuleBase<ShardedInteractionContext>
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

                IUser discordUser = user ?? await Client.GetUserAsync(parsedId, CacheMode.AllowDownload, null);   

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

            [SlashCommand("set-user-announcement-total", "Edit the scheduled announcement total.")]
            public async Task SetUserAnnouncementTotal(int value, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);

                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

                if (discordUser.IsBot)
                {
                    await FollowupAsync(text: $"❌ You **cannot** perform this action on bots.");
                }
                else if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else if (value < 0)
                {
                    await FollowupAsync(text: $"❌ The given `value` must be greater than 0 (The field is a `uint`).");
                }
                else
                {
                    using var context = new BobEntities();
                    User dbUser = await context.GetUser(user == null ? parsedId : user.Id);
                    dbUser.TotalScheduledAnnouncements = (uint)value;
                    await context.UpdateUser(dbUser);

                    await FollowupAsync(text: $"✅ `Announcement Count of User: {discordUser.GlobalName}, {discordUser.Id} updated.`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                }
            }

            [SlashCommand("set-user-message-total", "Edit the scheduled message total.")]
            public async Task SetUserMessageTotal(int value, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);

                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

                if (discordUser.IsBot)
                {
                    await FollowupAsync(text: $"❌ You **cannot** perform this action on bots.");
                }
                else if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else if (value < 0)
                {
                    await FollowupAsync(text: $"❌ The given `value` must be greater than 0 (The field is a `uint`).");
                }
                else
                {
                    using var context = new BobEntities();
                    User dbUser = await context.GetUser(user == null ? parsedId : user.Id);
                    dbUser.TotalScheduledMessages = (uint)value;
                    await context.UpdateUser(dbUser);

                    await FollowupAsync(text: $"✅ `Announcement Count of User: {discordUser.GlobalName}, {discordUser.Id} updated.`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                }
            }

            [SlashCommand("give-user-badge", "Gives the given user the given badge.")]
            public async Task GiveUserBadge(Bob.Badges.Badges badge, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);

                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

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
            public async Task RemoveUserBadge(Bob.Badges.Badges badge, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);

                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

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

                SocketGuild discordServer = Bot.Client.GetGuild(parsedId);

                if (discordServer == null)
                {
                    await FollowupAsync(text: $"❌ Bob could not find the server with the id: `{serverId}`.\n- This probably means Bob is not in the server.");
                }
                else
                {
                    Server dbServer;
                    using var context = new BobEntities();
                    dbServer = await context.GetServer(parsedId);

                    await FollowupAsync(text: $"✅ `Showing Server: {discordServer.Name}, {discordServer.Id}`\n{ServerDebugging.GetServerPropertyString(dbServer)}");
                }
            }

            [SlashCommand("get-user-from-black-list", "Gets the UserBlackList object with the given ID.")]
            public async Task GetUserFromBlackList(IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);
                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

                if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else if (discordUser == null)
                {
                    await FollowupAsync(text: $"❌ The given `userId` is not valid.");
                }
                else
                {
                    BlackListUser dbUser = await BlackList.GetUser(discordUser.Id);

                    if (dbUser != null)
                    {
                        await FollowupAsync(text: $"✅ `Showing Blacklisted User: {discordUser.GlobalName}, {discordUser.Id}`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                    }
                    else
                    {
                        await FollowupAsync(text: $"❌ The given `user` could not be found in the Database.");
                    }
                }
            }

            [SlashCommand("remove-user-from-black-list", "Removes the UserBlackList object with the given ID from the database.")]
            public async Task RemoveUserFromBlackList(IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);
                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

                if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else if (discordUser == null)
                {
                    await FollowupAsync(text: $"❌ The given `userId` is not valid.");
                }
                else
                {
                    BlackListUser dbUser = await BlackList.GetUser(discordUser.Id);

                    if (dbUser != null)
                    {
                        await BlackList.RemoveUser(dbUser);
                        await FollowupAsync(text: $"✅ Deleted User {discordUser.GlobalName} `{discordUser.Id}`.");
                    }
                    else
                    {
                        await FollowupAsync(text: $"❌ The given `user` could not be found in the Database.");
                    }
                }
            }

            [SlashCommand("update-user-from-black-list", "Updates the UserBlackList object with the given ID.")]
            public async Task UpdateUserFromBlackList(BlackList.Punishment punishment, string reason = "", IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);
                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

                if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else if (discordUser == null)
                {
                    await FollowupAsync(text: $"❌ The given `userId` is not valid.");
                }
                else
                {
                    BlackListUser dbUser = await BlackList.GetUser(discordUser.Id);

                    if (dbUser != null)
                    {
                        var updatedExpiration = BlackList.GetExpiration(punishment);
                        if (dbUser.Expiration != updatedExpiration || (reason != "" && dbUser.Reason != reason))
                        {
                            dbUser.Expiration = updatedExpiration;
                            dbUser.Reason = reason;
                            await BlackList.UpdateUser(dbUser);
                        }

                        await FollowupAsync(text: $"✅ `Showing Blacklisted User: {discordUser.GlobalName}, {discordUser.Id}`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                    }
                    else
                    {
                        await FollowupAsync(text: $"❌ The given `user` could not be found in the Database.");
                    }
                }
            }

            [SlashCommand("add-user-to-black-list", "Blacklists the user with the given ID.")]
            public async Task AddUsertoBlackList(BlackList.Punishment punishment, string reason, IUser user = null, string userId = null)
            {
                await DeferAsync();

                bool conversionResult = ulong.TryParse(userId, out ulong parsedId);
                if (user == null && userId == null)
                {
                    await FollowupAsync(text: $"❌ You **must** specify a `user` **or** a `userId`.");
                    return;
                }

                IUser discordUser = user ?? await Client.GetShardFor(Context.Guild).GetUserAsync(parsedId);

                if (user != null && conversionResult != false && user.Id != parsedId)
                {
                    await FollowupAsync(text: $"❌ The given `user` **and** `userId` must have matching IDs.");
                }
                else if (discordUser == null)
                {
                    await FollowupAsync(text: $"❌ The given `userId` is not valid.");
                }
                else
                {
                    BlackListUser dbUser = new()
                    {
                        Id = discordUser.Id,
                        Expiration = BlackList.GetExpiration(punishment),
                        Reason = reason
                    };

                    await BlackList.UpdateUser(dbUser);

                    await FollowupAsync(text: $"✅ `Showing Blacklisted User: {discordUser.GlobalName}, {discordUser.Id}`\n{UserDebugging.GetUserPropertyString(dbUser)}");
                }
            }

            [SlashCommand("remove-scheduled-announcement", "Removes the ScheduledAnnouncement object with the given ID from the database.")]
            public async Task RemoveScheduledAnnouncement(string announcementId)
            {
                await DeferAsync();

                bool announcementConversionResult = ulong.TryParse(announcementId, out ulong parsedAnnouncementId);

                if (announcementConversionResult == false)
                {
                    await FollowupAsync(text: $"❌ The given `announcementId` is not valid.");
                }
                else
                {
                    using var context = new BobEntities();
                    await context.RemoveScheduledAnnouncement(parsedAnnouncementId);

                    await FollowupAsync(text: $"✅ Deleted Scheduled Announcement `{parsedAnnouncementId}`.");
                }
            }
        }
    }
}