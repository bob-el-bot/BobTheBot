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

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers,
        AlwaysDownloadUsers = true,
    });

    public static readonly BobEntities DB = new();

    private static InteractionService Service;

    private static readonly string Token = Config.GetToken();

    // Purple (normal) Theme: 9261821 | Orange (halloween) Theme: 16760153
    public static readonly Color theme = new(9261821);

    public const ulong supportServerId = 1058077635692994651;

    public const ulong systemLogChannelId = 1160105468082004029;

    private static Timer timer;

    public static async Task Main()
    {
        if (Token is null)
        {
            throw new Exception("Discord bot token not set properly.");
        }

        // Ensure Database exists and is up to date
        await DB.Database.EnsureCreatedAsync();
        var migrations = DB.Database.GetPendingMigrations();

        Client.Ready += Ready;
        Client.Log += Log;
        Client.GuildAvailable += GuildAvailable;
        Client.JoinedGuild += JoinedGuild;
        Client.LeftGuild += LeftGuild;
        Client.UserJoined += UserJoined;

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        while (Console.ReadKey().Key != ConsoleKey.Q) 
        { 
            // Keep program from closing.
        };
    }

    public static int totalUsers;

    private static async Task Ready()
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
                var topGGResult = await PostToAPI("https://top.gg/api/bots/705680059809398804/stats", Config.GetTopGGToken(), new StringContent("{\"server_count\":" + Client.Guilds.Count.ToString() + "}", System.Text.Encoding.UTF8, "application/json"));
                Console.WriteLine($"TopGG POST status: {topGGResult}");

                // Discord Bots GG
                var discordBotsResult = await PostToAPI("https://discord.bots.gg/api/v1/bots/705680059809398804/stats", Config.GetDiscordBotsToken(), new StringContent("{\"guildCount\":" + Client.Guilds.Count.ToString() + "}", System.Text.Encoding.UTF8, "application/json"));
                Console.WriteLine($"Discord Bots GG POST status: {discordBotsResult}");
            }
            else
            {
                Console.WriteLine("Third party stats NOT updated because test bot is in use.");
            }
        });

        // _ = Task.Run(() =>
        // {
        //     // Status
        //     string[] statuses = { "/help | Try /quote!", $"/help | {totalUsers:n0} users!", "/help | Fonts!", "/help | RNG!", "/help | Quotes!" };
        //     int index = 0;
        //     timer = new(async x =>
        //     {
        //     if (Client.ConnectionState == ConnectionState.Connected)
        //     {
        //         await Client.SetCustomStatusAsync(statuses[index]);
        //         index = index + 1 == statuses.Length ? 0 : index + 1;
        //     }
        //     }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16));
        // });

        var cpuUsage = await GetCpuUsageForProcess();
        Console.WriteLine("CPU at Ready: " + cpuUsage.ToString() + "%");
        var ramUsage = GetRamUsageForProcess();
        Console.WriteLine("RAM at Ready: " + ramUsage.ToString() + "%");

        Client.Ready -= Ready;
    }

    private static Task GuildAvailable(SocketGuild guild)
    {
        // Download all of the users SEPARATELY from the Gateway Connection to keep WebSocket Connection Alive
        // (This is opposed to the standard: AlwaysDownloadUsers = true; flag) 
        _ = Task.Run(async () =>
        {
            await guild.DownloadUsersAsync();
        });

        return Task.CompletedTask;
    }

    private static async Task UserJoined(SocketGuildUser user)
    {
        Server server = await DB.GetServer(user.Guild.Id);
        if (server.Welcome == true)
        {
            if (user.Guild.SystemChannel != null && user.Guild.GetUser(Client.CurrentUser.Id).GetPermissions(user.Guild.SystemChannel).SendMessages && user.Guild.GetUser(Client.CurrentUser.Id).GetPermissions(user.Guild.SystemChannel).ViewChannel)
            {
                // Get random greeting
                Random random = new();
                string[] greetings = { $"Welcome {user.Mention}!", $"Who invited this guy? Just kidding, welcome {user.Mention}!", $"Happy to have you here {user.Mention}!", $"Looking good {user.Mention}!", $"{user.Mention} is here, everybody play cool.", $"{user.Mention} has entered the building.", $"Never fear, {user.Mention} is here.", $"A wild {user.Mention} appeared.", $"Everybody get loud because {user.Mention} is here!", $"{user.Mention} has graced us with their presence.", $"{user.Mention} is not the droid we're looking for. Also... their here!", $"Stand down, it's just {user.Mention}.", $"Make way for {user.Mention}!", $"{user.Mention} is here, in the flesh!", $"Open the gate for {user.Mention}!", $"Prepare yourselves, {user.Mention} has joined.", $"Look what the cat dragged in, {user.Mention} is here.", $"Speak of the devil, {user.Mention} joined.", $"Better late than never, {user.Mention} joined.", $"{user.Mention} has revealed themselves from the shadows." };
                await user.Guild.SystemChannel.SendMessageAsync(text: greetings[random.Next(0, greetings.Length)]);
            }
        }
    }

    private static async Task JoinedGuild(SocketGuild guild)
    {
        // Update user count
        totalUsers += guild.MemberCount;

        // Add server to DB
        await DB.AddServer(new Server { Id = guild.Id });

        // Welcome Message
        Random random = new();
        string[] greetings = { "G'day, I am Bob!", "Hello there, I'm Bob!", "Thanks for the invite, my name is Bob!" };

        string instructions = "I can do a lot of things now, but I also receive updates often. If you want to see my newest features use `/new`. If you want to learn about all of my commands use `/help` to get sent a list via DM. With that, I look forward to serving you all 🥳!";

        var embed = new EmbedBuilder
        {
            Title = "👋 " + greetings[random.Next(0, greetings.Length)],
            Description = instructions,
            Color = new Color(theme)
        };

        try
        {
            var TextChannels = guild.Channels.OfType<SocketTextChannel>().ToArray();
            SocketTextChannel DefaultChannel = TextChannels.Where(c => guild.CurrentUser.GetPermissions(c).SendMessages && guild.CurrentUser.GetPermissions(c).ViewChannel && (c.Name == "chat" || c.Name == "talk" || c.Name == "general")).First();

            if (DefaultChannel == null)
            {
                DefaultChannel = TextChannels
                        .Where(c => guild.CurrentUser.GetPermissions(c).SendMessages && guild.CurrentUser.GetPermissions(c).ViewChannel)
                        .OrderBy(c => c.Position)
                        .First();
            }

            await DefaultChannel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static Task LeftGuild(SocketGuild guild)
    {
        // Update user count
        totalUsers -= guild.MemberCount;

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
                    await ctx.Interaction.FollowupAsync($"❌ Something went wrong...\n- Try again later.\n- Join Bob's support server: https://discord.gg/HvGMRZD8jQ");
                    Console.WriteLine($"Error: {res.ErrorReason}");

                    IMessageChannel systemLogChannel = (IMessageChannel)Client.GetGuild(supportServerId).GetChannel(systemLogChannelId);

                    await LogToDiscord((RestTextChannel)systemLogChannel, ctx, info, res.ErrorReason);

                    // Live Debugging
                    // Server Logging
                    if (DebugGroup.LogGroup.serversToLog.ContainsKey(ctx.Guild.Id))
                    {
                        DebugGroup.LogGroup.serverLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel logChannel);
                        await LogToDiscord(logChannel, ctx, info, res.ErrorReason);
                    }
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
            var location = ctx.Interaction.GuildId == null ? "a DM" : Client.GetGuild(ulong.Parse(ctx.Interaction.GuildId.ToString())).ToString();
            var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
            Console.WriteLine($"{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {location} | Command: {commandName}");

            // Live Debugging
            // Server Logging
            if (DebugGroup.LogGroup.serversToLog.ContainsKey(ctx.Guild.Id))
            {
                DebugGroup.LogGroup.serverLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel logChannel);
                await LogToDiscord(logChannel, ctx, info);
            }
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