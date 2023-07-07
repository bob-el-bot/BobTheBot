// For Discord bot
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.Guilds,
        AlwaysDownloadUsers = true,
    });

    private static InteractionService Service;

    private static readonly string Token = Config.GetToken();

    public static async Task Main()
    {
        if (Token is null) throw new Exception("Discord bot token not set properly.");

        Client.Ready += Ready;
        Client.Log += Log;
        //Client.JoinedGuild += JoinedGuild;

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        while (Console.ReadKey().Key != ConsoleKey.Q) { };
    }

    private static Timer timer;

    private static async Task Ready()
    {
        Service = new InteractionService(Client, new InteractionServiceConfig()
        {
            UseCompiledLambda = true,
            ThrowOnError = true
        });

        await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        await Service.RegisterCommandsGloballyAsync();

        Client.InteractionCreated += InteractionCreated;
        Service.SlashCommandExecuted += SlashCommandResulted;

        string[] statuses = { "/help | Fonts!", "/help | New Commands!", "/help | RNG!", "/help | New Games!", "/help | 4,000+ users" };
        int index = 0;

        timer = new Timer(async x =>
        {
            await Client.SetGameAsync(statuses[index], null, ActivityType.Playing);
            index = index + 1 == statuses.Length ? 0 : index + 1;
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16));

        // Update Top.GG stats.
        if (Token != Config.GetTestToken())
        {
            TopGG topGG = new TopGG();
            await topGG.PostStats();
            Console.WriteLine("Top.GG stats updated");
        }
        else
        {
            Console.WriteLine("Top.GG stats NOT updated because test bot is in use.");
        }

        // Print the servers bob is in.
        int totalUsers = 0;
        foreach (var guild in Bot.Client.Guilds)
        {
            Console.WriteLine($"{guild.Name}, {guild.MemberCount}");
            totalUsers += guild.MemberCount;
        }

        Console.WriteLine($"Total Users: {totalUsers}");

        var cpuUsage = await Performance.GetCpuUsageForProcess();
        Console.WriteLine("CPU at Ready: " + cpuUsage.ToString());
        var ramUsage = Performance.GetRamUsageForProcess();
        Console.WriteLine("RAM at Ready: " + ramUsage.ToString());
    }

    // private static async Task JoinedGuild(SocketGuild guild)
    // {
    //     Random random = new Random();
    //     string[] greetings = { "G'day, I am Bob!", "Hello there, I'm Bob!", "Thanks for the invite, my name is Bob!" };

    //     string instructions = "I can do a lot of things now, but I also receive updates almost daily. If you want to see my newest features use `/new`. If you want to learn about all of my commands use `/help` to get sent a list via DM. With that, I look forward to serving you all 🥳!";

    //     var embed = new Discord.EmbedBuilder
    //     {
    //         Title = "👋 " + greetings[random.Next(0, greetings.Length)],
    //         Description = instructions,
    //         Color = new Discord.Color(6689298)
    //     };

    //     try
    //     {
    //         var TextChannels = guild.Channels.OfType<SocketTextChannel>().ToArray();

    //         SocketTextChannel DefaultChannel = TextChannels
    //                 .Where(c => guild.CurrentUser.GetPermissions(c).SendMessages && guild.CurrentUser.GetPermissions(c).ViewChannel)
    //                 .OrderBy(c => c.Position)
    //                 .First();

    //         await DefaultChannel.SendMessageAsync(embed: embed.Build());
    //     } 
    //     catch(Exception e)
    //     {
    //         Console.WriteLine(e);
    //     }
    // }

    private static async Task InteractionCreated(SocketInteraction interaction)
    {
        try
        {
            SocketInteractionContext ctx = new(Client, interaction);
            IResult res = await Service.ExecuteCommandAsync(ctx, null);
        }
        catch
        {
            if (interaction.Type == InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
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
                    await ctx.Interaction.FollowupAsync($"❌ Command exception: {res.ErrorReason}");
                    await ctx.Interaction.FollowupAsync("This might be because the server IP needs to be changed.");
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
            var cpuUsage = await Performance.GetCpuUsageForProcess();
            var ramUsage = Performance.GetRamUsageForProcess();
            var Location = ctx.Interaction.GuildId == null ? "a DM" : Client.GetGuild(ulong.Parse(ctx.Interaction.GuildId.ToString())).ToString();
            Console.WriteLine($"{DateTime.Now:dd/MM. H:mm:ss} CPU: {cpuUsage.ToString()} RAM: {ramUsage.ToString()} Location: {Location} Command: /{info.Name}");
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