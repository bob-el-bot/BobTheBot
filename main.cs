// For Discord bot
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Database;
using Commands; // DO NOT REMOVE
using System.Net.Http;
using static Performance.Stats;
using static ApiInteractions.Interface;
using static Debug.Logger;
using Database.Types;
using SQLitePCL;
using Microsoft.EntityFrameworkCore;
using Discord.Rest;
using System.Text;
using Microsoft.VisualBasic;
using System.Diagnostics;
using Commands.Attributes;
using Commands.Helpers;

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers,
        AlwaysDownloadUsers = true,
    });

    private static InteractionService Service;

    private static readonly string Token = Config.GetToken();

    // Purple (normal) Theme: 9261821 | Orange (halloween) Theme: 16760153
    public static readonly Color theme = new(9261821);

    public const ulong supportServerId = 1058077635692994651;
    public const ulong systemLogChannelId = 1160105468082004029;
    public const ulong devLogChannelId = 1196575302143459388;

    private static Timer timer;

    public static async Task Main()
    {
        if (Token is null)
        {
            throw new Exception("Discord bot token not set properly.");
        }

        Client.Ready += Ready;
        Client.Log += Log;
        Client.GuildAvailable += GuildAvailable;
        Client.JoinedGuild += JoinedGuild;
        Client.LeftGuild += Feedback.Prompt.LeftGuild;
        Client.UserJoined += UserJoined;
        Client.EntitlementCreated += EntitlementCreated;
        Client.EntitlementDeleted += EntitlementDeleted;
        Client.EntitlementUpdated += EntitlementUpdated;

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        while (Console.ReadKey().Key != ConsoleKey.Q) ;
    }

    public static int totalUsers;

    private static async Task Ready()
    {
        try
        {
            Service = new(Client, new InteractionServiceConfig()
            {
                UseCompiledLambda = true,
                ThrowOnError = true
            });

            await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            await Service.RegisterCommandsGloballyAsync();

            ModuleInfo[] debugCommands = Service.Modules.Where((x) => x.Preconditions.Any(x => x is RequireGuildAttribute)).ToArray();
            IGuild supportServer = Client.GetGuild(supportServerId);
            await Service.AddModulesToGuildAsync(supportServer, true, debugCommands);

            Client.InteractionCreated += InteractionCreated;
            Service.SlashCommandExecuted += SlashCommandResulted;

            _ = Task.Run(async () =>
            {
                // Determine the user count
                // Throwaway as to not block Gateway Tasks.
                foreach (var guild in Client.Guilds)
                {
                    totalUsers += guild.MemberCount;
                }

                totalUsers -= (Token == Config.GetTestToken()) ? 0 : 72000;
                Console.WriteLine($"Total Users: {totalUsers}");

                // Update third party stats
                // Throwaway as to not block Gateway Tasks.
                if (Token != Config.GetTestToken())
                {
                    // Top GG
                    var topGGResult = await PostToAPI("https://top.gg/api/bots/705680059809398804/stats", Config.GetTopGGToken(), new StringContent("{\"server_count\":" + Client.Guilds.Count.ToString() + "}", Encoding.UTF8, "application/json"));
                    Console.WriteLine($"TopGG POST status: {topGGResult}");

                    // Discord Bots GG
                    var discordBotsResult = await PostToAPI("https://discord.bots.gg/api/v1/bots/705680059809398804/stats", Config.GetDiscordBotsToken(), new StringContent("{\"guildCount\":" + Client.Guilds.Count.ToString() + "}", Encoding.UTF8, "application/json"));
                    Console.WriteLine($"Discord Bots GG POST status: {discordBotsResult}");
                }
                else
                {
                    Console.WriteLine("Third party stats NOT updated because test bot is in use.");
                }
            });

            var cpuUsage = await GetCpuUsageForProcess();
            Console.WriteLine("CPU at Ready: " + cpuUsage.ToString() + "%");
            var ramUsage = GetRamUsageForProcess();
            Console.WriteLine("RAM at Ready: " + ramUsage.ToString() + "%");

            Client.Ready -= Ready;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        string[] statuses = { "/help | Games!", "/help | Premium! ❤︎", "/help | bobthebot.net", "/help | RNG!", "/help | Quotes!", "/help | Confessions!" };
        int index = 0;

        _ = Task.Run(() =>
        {
            // Status
            timer = new Timer(async x =>
            {
                try
                {
                    if (Client.ConnectionState == ConnectionState.Connected)
                    {
                        await Client.SetCustomStatusAsync(statuses[index]);
                        index = (index + 1) % statuses.Length;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting status: {ex.Message} | {statuses[index]}");
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16));
        });
    }

    private static Task GuildAvailable(SocketGuild guild)
    {
        // // Download all of the users SEPARATELY from the Gateway Connection to keep WebSocket Connection Alive
        // // (This is opposed to the standard: AlwaysDownloadUsers = true; flag) 
        // _ = Task.Run(async () =>
        // {
        //     await guild.DownloadUsersAsync();
        // });

        return Task.CompletedTask;
    }

    private static async Task UserJoined(SocketGuildUser user)
    {
        Server server;
        using (var context = new BobEntities())
        {
            server = await context.GetServer(user.Guild.Id);
        }

        if (server.Welcome == true)
        {
            if (user.Guild.SystemChannel != null && user.Guild.GetUser(Client.CurrentUser.Id).GetPermissions(user.Guild.SystemChannel).SendMessages && user.Guild.GetUser(Client.CurrentUser.Id).GetPermissions(user.Guild.SystemChannel).ViewChannel)
            {
                if (server.CustomWelcomeMessage != null && server.CustomWelcomeMessage != "")
                {
                    await user.Guild.SystemChannel.SendMessageAsync(text: Welcome.FormatCustomMessage(server.CustomWelcomeMessage, user.Mention));
                }
                else
                {
                    // Get random greeting
                    await user.Guild.SystemChannel.SendMessageAsync(text: Welcome.GetRandomMessage(user.Mention));
                }
            }
        }
    }

    private static async Task JoinedGuild(SocketGuild guild)
    {
        // Update user count
        totalUsers += guild.MemberCount;

        // Add server to DB (if needed)
        using var context = new BobEntities();
        await context.GetServer(guild.Id);
    }

    private static async Task EntitlementCreated(SocketEntitlement ent)
    {
        using var context = new BobEntities();
        IUser entUser = await ent.User.Value.GetOrDownloadAsync();
        User user = await context.GetUser(entUser.Id);

        user.PremiumExpiration = (DateTimeOffset)ent.EndsAt;
        await context.UpdateUser(user);
    }

    private static async Task EntitlementUpdated(Cacheable<SocketEntitlement, ulong> before, SocketEntitlement after)
    {
        using var context = new BobEntities();
        IUser entUser = await before.Value.User.Value.GetOrDownloadAsync();
        User user = await context.GetUser(entUser.Id);

        user.PremiumExpiration = (DateTimeOffset)after.EndsAt;
        await context.UpdateUser(user);
    }

    private static Task EntitlementDeleted(Cacheable<SocketEntitlement, ulong> ent)
    {
        return Task.CompletedTask;
    }

    private static async Task InteractionCreated(SocketInteraction interaction)
    {
        try
        {
            SocketInteractionContext ctx = new(Client, interaction);
            await Service.ExecuteCommandAsync(ctx, null);
        }
        catch
        {
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }

    private static async Task SlashCommandResulted(SlashCommandInfo info, IInteractionContext ctx, IResult res)
    {
        if (!res.IsSuccess)
        {
            switch (res.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    await ctx.Interaction.FollowupAsync($"❌ Unmet Precondition: {res.ErrorReason}");
                    break;
                case InteractionCommandError.UnknownCommand:
                    await ctx.Interaction.FollowupAsync("❌ Unknown command");
                    break;
                case InteractionCommandError.BadArgs:
                    await ctx.Interaction.FollowupAsync("❌ Invalid number or arguments");
                    break;
                case InteractionCommandError.Exception:
                    await ctx.Interaction.FollowupAsync($"❌ Something went wrong...\n- Ensure Bob has the **View Channel** and **Send Messages** permissions.\n- Try again later.\n- Join Bob's support server, let us know here: https://discord.gg/HvGMRZD8jQ");

                    var executionResult = (ExecuteResult)res;
                    Console.WriteLine($"Error: {executionResult.Exception}");

                    SocketTextChannel logChannel = (SocketTextChannel)Client.GetGuild(supportServerId).GetChannel(Token != Config.GetTestToken() ? systemLogChannelId : devLogChannelId);

                    await LogErrorToDiscord(logChannel, ctx, info, $"{executionResult.ErrorReason}\n{executionResult.Exception}");

                    // // Live Debugging
                    // // Server Logging
                    // if (ctx.Interaction.GuildId != null && DebugGroup.LogGroup.serversToLog.ContainsKey(ctx.Guild.Id))
                    // {
                    //     DebugGroup.LogGroup.serverLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel logChannel);
                    //     await LogToDiscord(logChannel, ctx, info, res.ErrorReason);
                    // }
                    break;
                case InteractionCommandError.Unsuccessful:
                    await ctx.Interaction.FollowupAsync("❌ Command could not be executed");
                    break;
                default:
                    await ctx.Interaction.FollowupAsync("❌ Command could not be executed, but it is not Bob's fualt. Please try again later while the developers work out what is wrong.");
                    break;
            }
        }
        else
        {
            var cpuUsage = await GetCpuUsageForProcess();
            var ramUsage = GetRamUsageForProcess();
            string location = (ctx.Interaction.GuildId == null) ? "a DM" : (Client.GetGuild((ulong)ctx.Interaction.GuildId) == null ? "User Install" : Client.GetGuild((ulong)ctx.Interaction.GuildId).ToString());
            var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
            Console.WriteLine($"{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | Command: {commandName}");

            // // Live Debugging
            // // Server Logging
            // if (ctx.Interaction.GuildId != null && DebugGroup.LogGroup.serversToLog.ContainsKey(ctx.Guild.Id))
            // {
            //     DebugGroup.LogGroup.serverLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel logChannel);
            //     await LogToDiscord(logChannel, ctx, info);
            // }
        }
    }

    private static Task Log(LogMessage logMessage)
    {
        Console.ForegroundColor = SeverityToConsoleColor(logMessage.Severity);
        Console.WriteLine($"{DateTime.Now:dd/MM. H:mm:ss} [{logMessage.Source}] {logMessage.Message}");
        Console.ResetColor();

        return Task.CompletedTask;
    }

    private static ConsoleColor SeverityToConsoleColor(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Critical => ConsoleColor.Red,
            LogSeverity.Debug => ConsoleColor.Blue,
            LogSeverity.Error => ConsoleColor.Yellow,
            LogSeverity.Info => ConsoleColor.Cyan,
            LogSeverity.Verbose => ConsoleColor.Green,
            LogSeverity.Warning => ConsoleColor.Magenta,
            _ => ConsoleColor.White,
        };
    }
}