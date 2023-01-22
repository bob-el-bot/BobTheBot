// For Discord bot
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

// For c# server
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.Guilds
    });

    private static InteractionService Service;
    private static readonly string Token = Environment.GetEnvironmentVariable("token");

    public static async Task Main()
    {
        if (Token is null) throw new Exception("You didn't set your Discord bot token properly.");

        Client.Ready += Ready;
        Client.Log += Log;

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        while (Console.ReadKey().Key != ConsoleKey.Q) { };
    }

    private static async Task Ready()
    {
        Service = new InteractionService(Client, new InteractionServiceConfig()
        {
            UseCompiledLambda = true,
            ThrowOnError = true
        });

        await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        await Service.RegisterCommandsGloballyAsync();

        Client.SlashCommandExecuted += SlashCommandExecuted;
        Service.SlashCommandExecuted += SlashCommandResulted;

        await Client.SetGameAsync("with C#", null, ActivityType.Playing);
    }

    private static async Task SlashCommandExecuted(SocketSlashCommand command)
    {
        try
        {
            SocketInteractionContext ctx = new(Client, command);
            IResult res = await Service.ExecuteCommandAsync(ctx, null);
        }
        catch
        {
            if (command.Type == InteractionType.ApplicationCommand)
                await command.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
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
                    await ctx.Interaction.FollowupAsync("This might be because the server IP needs to changed.");
                    break;
                case InteractionCommandError.Unsuccessful:
                    await ctx.Interaction.FollowupAsync("❌ Command could not be executed");
                    break;
                default:
                    break;
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

public static class HttpServer
{
    static Encoding enc = Encoding.UTF8;

    public static void CreateServer(string[] args)
    {
        Console.WriteLine("start up http server...");

        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("request incoming...");

            NetworkStream stream = client.GetStream();
            string request = ToString(stream);

            Console.WriteLine("");
            Console.WriteLine(request);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(@"HTTP/1.1 200 OK");
            builder.AppendLine(@"Content-Type: text/html");
            builder.AppendLine(@"");
            builder.AppendLine(@"<html><head></head><body><p>Bob the bot is online!</p></body></html>");

            Console.WriteLine("");
            Console.WriteLine("response...");
            Console.WriteLine(builder.ToString());

            byte[] sendBytes = enc.GetBytes(builder.ToString());
            stream.Write(sendBytes, 0, sendBytes.Length);

            stream.Close();
            client.Close();
        }
    }

    public static string ToString(NetworkStream stream)
    {
        MemoryStream memoryStream = new MemoryStream();
        byte[] data = new byte[256];
        int size;
        do
        {
            size = stream.Read(data, 0, data.Length);
            if (size == 0)
            {
                Console.WriteLine("client disconnected...");
                Console.ReadLine();
                return null;
            }
            memoryStream.Write(data, 0, size);
        } while (stream.DataAvailable);
        return enc.GetString(memoryStream.ToArray());
    }
}