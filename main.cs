using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Database;
using Commands;
using static Performance.Stats;
using static Debug.Logger;
using Database.Types;
using Discord.Rest;
using Commands.Attributes;
using Commands.Helpers;
using BadgeInterface;
using static ApiInteractions.Interface;
using static Commands.Helpers.MessageReader;
using System.Net;
using System.Net.Http;
using System.Text;
using DotNetEnv;
using SQLitePCL;

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.AutoModerationConfiguration,
    });

    private static InteractionService Service;

    public static string Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

    // Purple (normal) Theme: 9261821 | Orange (halloween) Theme: 16760153
    public static readonly Color theme = new(9261821);

    public const ulong supportServerId = 1058077635692994651;
    public const ulong systemLogChannelId = 1160105468082004029;
    public const ulong devLogChannelId = 1196575302143459388;

    public static async Task Main()
    {
        if (Token is null)
        {
            throw new ArgumentException("Discord bot token not set properly.");
        }

        Client.Ready += Ready;
        Client.Log += Log;
        Client.JoinedGuild += JoinedGuild;
        Client.LeftGuild += Feedback.Prompt.LeftGuild;
        Client.UserJoined += UserJoined;
        Client.EntitlementCreated += EntitlementCreated;
        Client.EntitlementDeleted += EntitlementDeleted;
        Client.EntitlementUpdated += EntitlementUpdated;
        Client.MessageReceived += MessageReceived;

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        StartHttpListener();

        await Task.Delay(Timeout.Infinite);
    }

    private static void StartHttpListener()
    {
        HttpListener listener = new();
        listener.Prefixes.Add($"http://*:{Environment.GetEnvironmentVariable("PORT")}/");
        listener.Start();
        Console.WriteLine($"Listening for HTTP requests on port {Environment.GetEnvironmentVariable("PORT")}...");

        Task.Run(async () =>
        {
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                ProcessRequest(context);
            }
        });
    }

    private static void ProcessRequest(HttpListenerContext context)
    {
        _ = context.Request;
        HttpListenerResponse response = context.Response;

        // Process the request here
        string responseString = "Bob is Alive!";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private static async Task Ready()
    {
        try
        {
            Service = new(Client, new InteractionServiceConfig
            {
                UseCompiledLambda = true,
                ThrowOnError = true
            });

            await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            await Service.RegisterCommandsGloballyAsync();

            // Register Debug Commands
            ModuleInfo[] debugCommands = Service.Modules
                .Where(module => module.Preconditions.Any(precondition => precondition is RequireGuildAttribute)
                              && module.SlashGroupName == "debug")
                .ToArray();
            IGuild supportServer = Client.GetGuild(supportServerId);
            await Service.AddModulesToGuildAsync(supportServer, true, debugCommands);

            Client.InteractionCreated += InteractionCreated;
            Service.SlashCommandExecuted += SlashCommandResulted;

            if (Token != Environment.GetEnvironmentVariable("TEST_DISCORD_TOKEN"))
            {
                _ = Task.Run(async () =>
                {
                    // Update third party stats
                    // Throwaway as to not block Gateway Tasks.
                    // Top GG
                    var topGGResult = await PostToAPI("https://top.gg/api/bots/705680059809398804/stats", Environment.GetEnvironmentVariable("TOP_GG_TOKEN"), new StringContent("{\"server_count\":" + Client.Guilds.Count + "}", Encoding.UTF8, "application/json"));
                    Console.WriteLine($"TopGG POST status: {topGGResult}");

                    // Discord Bots GG
                    var discordBotsResult = await PostToAPI("https://discord.bots.gg/api/v1/bots/705680059809398804/stats", Environment.GetEnvironmentVariable("DISCORD_BOTS_TOKEN"), new StringContent("{\"guildCount\":" + Client.Guilds.Count + "}", Encoding.UTF8, "application/json"));
                    Console.WriteLine($"Discord Bots GG POST status: {discordBotsResult}");
                });
            }

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

        string[] statuses = { "/help | Games!", "/help | Premium! ❤︎", "/help | Scheduling!", "/help | Automod!", "/help | bobthebot.net", "/help | RNG!", "/help | Quotes!", "/help | Confessions!" };
        int index = 0;

        _ = Task.Run(() =>
        {
            // Status
            _ = new Timer(async x =>
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

        _ = Task.Run(async () =>
        {
            await Schedule.LoadAndScheduleItemsAsync<ScheduledAnnouncement>();
        });

        _ = Task.Run(async () =>
        {
            await Schedule.LoadAndScheduleItemsAsync<ScheduledMessage>();
        });
    }

    private static async Task UserJoined(SocketGuildUser user)
    {
        try
        {
            Server server;
            using (var context = new BobEntities())
            {
                server = await context.GetServer(user.Guild.Id);
            }

            if (server.Welcome == true)
            {
                ChannelPermissions permissions = user.Guild.GetUser(Client.CurrentUser.Id).GetPermissions(user.Guild.SystemChannel);
                if (user.Guild.SystemChannel != null && permissions.SendMessages && permissions.ViewChannel)
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

            // If support server, then give the user the Friend badge
            if (user.Guild.Id == supportServerId)
            {
                User dbUser;
                using var context = new BobEntities();
                dbUser = await context.GetUser(user.Id);

                await Badge.GiveUserBadge(dbUser, Badges.Badges.Friend);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task JoinedGuild(SocketGuild guild)
    {
        // Add server to DB (if needed)
        using var context = new BobEntities();
        await context.GetServer(guild.Id);
    }

    private static async Task EntitlementCreated(SocketEntitlement ent)
    {
        using var context = new BobEntities();
        IUser entUser = await ent.User.Value.GetOrDownloadAsync();
        User user = await context.GetUser(entUser.Id);

        if (ent.EndsAt == null)
        {
            user.PremiumExpiration = DateTimeOffset.MaxValue;
        }
        else
        {
            user.PremiumExpiration = (DateTimeOffset)ent.EndsAt;
        }

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

    private static async Task MessageReceived(SocketMessage message)
    {
        var channel = message.Channel as SocketGuildChannel;

        IGuildUser fetchedBot = Client.GetGuild(channel.Guild.Id).GetUser(Client.CurrentUser.Id);
        var botPerms = fetchedBot.GetPermissions(channel);

        // Ensure Bob can send messages in the channel.
        if (botPerms.SendMessages != true)
        {
            return;
        }

        try
        {
            // Auto Publish if in a News Channel
            if (channel.GetChannelType() == ChannelType.News && message.Components == null)
            {
                NewsChannel newsChannel;
                using (var context = new BobEntities())
                {
                    newsChannel = await context.GetNewsChannel(channel.Id);
                }

                if (newsChannel != null)
                {
                    IUserMessage userMessage = (IUserMessage)message;
                    await userMessage.CrosspostAsync();

                    return;
                }
            }

            // Ensure message was not from a Bot.
            if (message.Author.IsBot)
            {
                return;
            }

            // Auto Embed if GitHub Link and Server has Auto Embeds for GitHub 
            Server server;
            using var dbContext = new BobEntities();
            server = await dbContext.GetServer(channel.Guild.Id);
            if (server.AutoEmbedGitHubLinks == true)
            {
                GitHubLinkParse.GitHubLink gitHubLink = GitHubLinkParse.GetUrl(message.Content);

                if (gitHubLink != null)
                {
                    switch (gitHubLink.Type)
                    {
                        case GitHubLinkParse.GitHubLinkType.CodeFile:
                            FileLinkInfo linkInfo = CodeReader.CreateFileLinkInfo(gitHubLink.Url, true);

                            string previewLines = await CodeReader.GetPreview(linkInfo);

                            // Format final response
                            string preview = $"🔎 Showing {CodeReader.GetFormattedLineNumbers(linkInfo.LineNumbers)} of [{linkInfo.Repository}/{linkInfo.Branch}/{linkInfo.File}](<{gitHubLink.Url}>)\n```{linkInfo.File[(linkInfo.File.IndexOf('.') + 1)..]}\n{previewLines}```";
                            await message.Channel.SendMessageAsync(text: preview);

                            break;
                        case GitHubLinkParse.GitHubLinkType.PullRequest:
                            PullRequestInfo pullRequestInfo = PullRequestReader.CreatePullRequestInfo(gitHubLink.Url);
                            await message.Channel.SendMessageAsync(embed: await PullRequestReader.GetPreview(pullRequestInfo));

                            break;
                        case GitHubLinkParse.GitHubLinkType.Issue:
                            IssueInfo issueInfo = IssueReader.CreateIssueInfo(gitHubLink.Url);
                            await message.Channel.SendMessageAsync(embed: await IssueReader.GetPreview(issueInfo));

                            break;
                    }

                    return;
                }
            }

            if (server.AutoEmbedMessageLinks)
            {
                DiscordMessageLinkParse.DiscordLink discordLink = DiscordMessageLinkParse.GetUrl(message.Content);

                if (discordLink != null)
                {
                    DiscordLinkInfo linkInfo = CreateMessageInfo(discordLink.Url);
                    Embed preview = await GetPreview(linkInfo);
                    if (preview != null)
                    {
                        await message.Channel.SendMessageAsync(embed: preview);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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
                    if (ctx.Interaction.HasResponded)
                    {
                        await ctx.Interaction.FollowupAsync($"❌ Something went wrong:\n- {res.ErrorReason}", ephemeral: true);
                    }
                    else
                    {
                        await ctx.Interaction.RespondAsync($"❌ Something went wrong:\n- {res.ErrorReason}", ephemeral: true);
                    }
                    break;
                case InteractionCommandError.UnknownCommand:
                    if (ctx.Interaction.HasResponded)
                    {
                        await ctx.Interaction.FollowupAsync("❌ Unknown command\n- Try refreshing your Discord client.", ephemeral: true);
                    }
                    else
                    {
                        await ctx.Interaction.RespondAsync("❌ Unknown command\n- Try refreshing your Discord client.", ephemeral: true);
                    }
                    break;
                case InteractionCommandError.BadArgs:
                    if (ctx.Interaction.HasResponded)
                    {
                        await ctx.Interaction.FollowupAsync("❌ Invalid number or arguments.", ephemeral: true);
                    }
                    else
                    {
                        await ctx.Interaction.RespondAsync("❌ Invalid number or arguments.", ephemeral: true);
                    }
                    break;
                case InteractionCommandError.Exception:
                    await ctx.Interaction.FollowupAsync($"❌ Something went wrong...\n- Ensure Bob has the **View Channel** and **Send Messages** permissions.\n- Try again later.\n- The developers have been notified.\n- Or, join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and let us know about it.", ephemeral: true);

                    var executionResult = (ExecuteResult)res;
                    Console.WriteLine($"Error: {executionResult.Exception}");

                    await LogErrorToDiscord(ctx, info, $"{executionResult.ErrorReason}\n{executionResult.Exception}");

                    // Live Debugging
                    // Server Logging
                    if (ctx.Interaction.GuildId != null && DebugGroup.LogGroup.ServersToLog.ContainsKey(ctx.Guild.Id))
                    {
                        DebugGroup.LogGroup.ServerLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel debugLogChannel);
                        await LogServerUseToDiscord(debugLogChannel, ctx, info, res.ErrorReason);
                    }
                    break;
                case InteractionCommandError.Unsuccessful:
                    await ctx.Interaction.FollowupAsync("❌ Command could not be executed. This is odd...\n- Try again later.\n- You can also join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and let us know about it.", ephemeral: true);
                    break;
                default:
                    await ctx.Interaction.FollowupAsync("❌ Command could not be executed, but it is not Bob's fault (it is most likely Discord's API failing). Please try again later while the developers work out what is wrong.\n- You can join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and to let us know anything and/or stay posted on updates.", ephemeral: true);
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

            // Live Debugging
            // Server Logging
            if (ctx.Interaction.GuildId != null && DebugGroup.LogGroup.ServersToLog.ContainsKey((ulong)ctx.Interaction.GuildId))
            {
                DebugGroup.LogGroup.ServerLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel debugLogChannel);
                await LogServerUseToDiscord(debugLogChannel, ctx, info);
            }

            if (DebugGroup.LogGroup.LogEverything == true)
            {
                await LogErrorToDiscord(ctx, info);
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