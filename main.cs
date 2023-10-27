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
using Database.Types;
using SQLitePCL;

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.Guilds,
        AlwaysDownloadUsers = true,
    });

    public static readonly BobEntities DB = new();

    private static InteractionService Service;

    private static readonly string Token = Config.GetToken();

    // Purple (normal) Theme: 9261821 |  Orange (halloween) Theme: 16760153
    public static readonly Color theme = new(16760153);

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
        Client.LeftGuild += LeftGuild;

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        while (Console.ReadKey().Key != ConsoleKey.Q) { };
    }

    public static int totalUsers;
    private static Timer timer;

    private static async Task Ready()
    {
        Service = new(Client, new InteractionServiceConfig()
        {
            UseCompiledLambda = true,
            ThrowOnError = true
        });

        await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        await Service.RegisterCommandsGloballyAsync();

        Client.InteractionCreated += InteractionCreated;
        Service.SlashCommandExecuted += SlashCommandResulted;

        // Determine the user count
        // Throwaway as to not block Gateway Tasks.
        _ = Task.Run(() =>
        {
            foreach (var guild in Client.Guilds)
            {
                totalUsers += guild.MemberCount;
            }

            totalUsers -= (Token == Config.GetTestToken()) ? 0 : 72000;
            Console.WriteLine($"Total Users: {totalUsers}");
        });

        // Update third party stats
        // Throwaway as to not block Gateway Tasks.
        _ = Task.Run(async () =>
        {
            if (Token != Config.GetTestToken())
            {
                // Top GG
                var topGGResult = await PostToAPI("https://top.gg/api/bots/705680059809398804/stats", Config.GetTopGGToken(), new StringContent("{\"server_count\":" + 294.ToString() + "}", System.Text.Encoding.UTF8, "application/json"));
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

        var cpuUsage = await GetCpuUsageForProcess();
        Console.WriteLine("CPU at Ready: " + cpuUsage.ToString() + "%");
        var ramUsage = GetRamUsageForProcess();
        Console.WriteLine("RAM at Ready: " + ramUsage.ToString() + "%");

        string[] statuses = { "/help | 🎃👻🍂", "/help | Try /quote!", $"/help | {totalUsers:n0} users!", "/help | Fonts!", "/help | RNG!", "/help | Quotes!" };
        int index = 0;

        timer = new(async x =>
        {
            if (Client.ConnectionState == ConnectionState.Connected)
            {
                await Client.SetGameAsync(statuses[index], null, ActivityType.Playing);
                index = index + 1 == statuses.Length ? 0 : index + 1;
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16));

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
            Color = new Color(9261821)
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

    private static readonly ulong ownerID = Config.GetOwnerID();

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
                    IMessageChannel logChannel = (IMessageChannel)Client.GetGuild(1058077635692994651).GetChannel(1160105468082004029);
                    var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
                    await logChannel.SendMessageAsync($"**Error:** ```cs\n{res.ErrorReason}```Guild: **{ctx.Guild}** | Command: **{commandName}**");
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
            var Location = ctx.Interaction.GuildId == null ? "a DM" : Client.GetGuild(ulong.Parse(ctx.Interaction.GuildId.ToString())).ToString();
            var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
            Console.WriteLine($"{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Location: {Location} | Command: {commandName}");
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