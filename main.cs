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
using Commands.Attributes;
using Commands.Helpers;
using BadgeInterface;
using System.Text.Json.Nodes;

public static class Bot
{
    public static readonly DiscordSocketClient Client = new(new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
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

        while (Console.ReadKey().Key != ConsoleKey.Q) ;
    }

    public static int TotalUsers { get; set; }

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

            // Register Debug Commands
            ModuleInfo[] debugCommands = Service.Modules
                .Where(module => module.Preconditions.Any(precondition => precondition is RequireGuildAttribute)
                              && module.SlashGroupName == "debug")
                .ToArray();
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
                    TotalUsers += guild.MemberCount;
                }

                TotalUsers -= (Token == Config.GetTestToken()) ? 0 : 72000;
                Console.WriteLine($"Total Users: {TotalUsers}");

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
        // Update user count
        TotalUsers += guild.MemberCount;

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
            GitHubLinkParse.GitHubLink gitHubLink = GitHubLinkParse.GetUrl(message.Content);

            if (gitHubLink != null && gitHubLink.Type != GitHubLinkParse.GitHubLinkType.Unknown)
            {
                IUserMessage userMessage = (IUserMessage)message;

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
                        await ctx.Interaction.FollowupAsync($"❌ Something went wrong: {res.ErrorReason}", ephemeral: true);
                    }
                    else
                    {
                        await ctx.Interaction.RespondAsync($"❌ Something went wrong: {res.ErrorReason}", ephemeral: true);
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
                    await ctx.Interaction.FollowupAsync($"❌ Something went wrong...\n- Ensure Bob has the **View Channel** and **Send Messages** permissions.\n- Try again later.\n- Or, join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and let us know about it.", ephemeral: true);

                    var executionResult = (ExecuteResult)res;
                    Console.WriteLine($"Error: {executionResult.Exception}");

                    SocketTextChannel logChannel = (SocketTextChannel)Client.GetGuild(supportServerId).GetChannel(Token != Config.GetTestToken() ? systemLogChannelId : devLogChannelId);

                    await LogErrorToDiscord(logChannel, ctx, info, $"{executionResult.ErrorReason}\n{executionResult.Exception}");

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
            if (ctx.Interaction.GuildId != null && DebugGroup.LogGroup.ServersToLog.ContainsKey(ctx.Guild.Id))
            {
                DebugGroup.LogGroup.ServerLogChannels.TryGetValue(ctx.Guild.Id, out RestTextChannel debugLogChannel);
                await LogServerUseToDiscord(debugLogChannel, ctx, info);
            }

            if (DebugGroup.LogGroup.LogEverything == true)
            {
                SocketTextChannel logChannel = (SocketTextChannel)Client.GetGuild(supportServerId).GetChannel(Token != Config.GetTestToken() ? systemLogChannelId : devLogChannelId);
                await LogErrorToDiscord(logChannel, ctx, info);
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