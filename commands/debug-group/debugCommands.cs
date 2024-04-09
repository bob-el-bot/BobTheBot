using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadgeInterface;
using Commands.Attributes;
using Database;
using Database.Types;
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
            public static bool LogEverything = false;

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

                if (serversToLog.ContainsKey(id))
                {
                    serversToLog.Remove(id);
                    serverLogChannels.TryGetValue(id, out RestTextChannel channel);

                    await FollowupAsync(text: $"✅ Debug logging for the server ID `{serverId}` has been **stopped** and {channel.Mention} will be deleted in 5 seconds.");

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
                        await FollowupAsync(text: $"✅ Debug logging for the server ID `{serverId}` has **started** in {restChannel.Mention}.");
                        serverLogChannels.Add(key: id, value: restChannel);

                        serversToLog.Add(key: id, value: guild);
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

                    await FollowupAsync(text: $"✅ `Showing User: {discordUser.GlobalName}, {discordUser.Id}`\n```cs\nPremium Expiration: {dbUser.PremiumExpiration}\nProfile Color: {dbUser.ProfileColor}\nRock Paper Scissors Wins: {dbUser.RockPaperScissorsWins}\nTotal Rock Paper Scissor Games: {dbUser.TotalRockPaperScissorsGames}\nTic-Tac-Toe Wins: {dbUser.TicTacToeWins}\nTotal Tic-Tac-Toe Games: {dbUser.TotalTicTacToeGames}\nTrivia Wins: {dbUser.TriviaWins}\nTotal Trivia Games: {dbUser.TotalTriviaGames}\nBadges: {Badge.GetBadgesProfileString(dbUser.EarnedBadges)}```");
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

                    await FollowupAsync(text: $"✅ `Badge given to User: {discordUser.GlobalName}, {discordUser.Id}`\n```cs\nPremium Expiration: {dbUser.PremiumExpiration}\nProfile Color: {dbUser.ProfileColor}\nRock Paper Scissors Wins: {dbUser.RockPaperScissorsWins}\nTotal Rock Paper Scissor Games: {dbUser.TotalRockPaperScissorsGames}\nTic-Tac-Toe Wins: {dbUser.TicTacToeWins}\nTotal Tic-Tac-Toe Games: {dbUser.TotalTicTacToeGames}\nTrivia Wins: {dbUser.TriviaWins}\nTotal Trivia Games: {dbUser.TotalTriviaGames}\nBadges: {Badge.GetBadgesProfileString(dbUser.EarnedBadges)}```");
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

                    await FollowupAsync(text: $"✅ `Badge removed from User: {discordUser.GlobalName}, {discordUser.Id}`\n```cs\nPremium Expiration: {dbUser.PremiumExpiration}\nProfile Color: {dbUser.ProfileColor}\nRock Paper Scissors Wins: {dbUser.RockPaperScissorsWins}\nTotal Rock Paper Scissor Games: {dbUser.TotalRockPaperScissorsGames}\nTic-Tac-Toe Wins: {dbUser.TicTacToeWins}\nTotal Tic-Tac-Toe Games: {dbUser.TotalTicTacToeGames}\nTrivia Wins: {dbUser.TriviaWins}\nTotal Trivia Games: {dbUser.TotalTriviaGames}\nBadges: {Badge.GetBadgesProfileString(dbUser.EarnedBadges)}```");
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