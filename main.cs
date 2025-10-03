using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Bob.Database;
using Bob.Commands;
using static Bob.Monitoring.PerformanceStats;
using static Bob.Debug.Logger;
using Bob.Database.Types;
using Discord.Rest;
using Bob.Commands.Attributes;
using Bob.Commands.Helpers;
using Bob.BadgeInterface;
using static Bob.ApiInteractions.Interface;
using static Bob.Commands.Helpers.MessageReader;
using System.Net.Http;
using System.Text;
using DotNetEnv;
using System.IO;
using Bob.Monitoring;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BobTheBot.Chat;

namespace Bob
{
    public static class Bot
    {
        public static readonly DiscordShardedClient Client = new(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.MessageContent | GatewayIntents.AutoModerationConfiguration,
        });

        public static string Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        // Purple (normal) Theme: 9261821 | Orange (halloween) Theme: 16760153
        public static readonly Color theme = new(9261821);

        public const ulong supportServerId = 1058077635692994651;
        public static readonly ulong systemLogChannelId = 1160105468082004029;

        private static readonly string[] statuses = ["/help | Games!", "/help | Premium! ❤︎", "/help | Scheduling!", "/help | Automod!", "/help | bobthebot.net", "/help | RNG!", "/help | Quotes!", "/help | Confessions!"];

        private static int _shardsReady = 0;
        private static TaskCompletionSource<bool> _allShardsReady = new();

        public static IServiceProvider Services;

        public static async Task Main()
        {
            Env.Load();
            if (Token is null)
            {
                throw new ArgumentException("Discord bot token not set properly.");
            }

            var services = new ServiceCollection();

            var npgsqlConnectionString = DatabaseUtils.GetNpgsqlConnectionString();

            services.AddDbContext<BobEntities>(options =>
            {
                options.UseNpgsql(
                    npgsqlConnectionString,
                    npgsqlOptions => npgsqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 1,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null
                        )
                        .UseVector()
                );
            });

            services.AddSingleton(new InteractionService(Client, new InteractionServiceConfig
            {
                UseCompiledLambda = true,
                ThrowOnError = true,
            }));

            Services = services.BuildServiceProvider();

            Client.ShardReady += ShardReady;
            Client.Log += Log;
            Client.JoinedGuild += JoinedGuild;
            Client.LeftGuild += Feedback.Prompt.LeftGuild;
            Client.UserJoined += UserJoined;
            Client.EntitlementCreated += EntitlementCreated;
            Client.EntitlementDeleted += EntitlementDeleted;
            Client.EntitlementUpdated += EntitlementUpdated;
            Client.MessageReceived += MessageReceived;
            Client.ReactionAdded += HandleReactionAddedAsync;
            Client.ReactionRemoved += HandleReactionRemovedAsync;
            Client.ReactionRemoved += HandleReactionClearedAsync;

            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            Uptime.StartHttpListener();

            // Wait for all shards to be ready before proceeding
            await _allShardsReady.Task;

            await RegisterSlashCommands();

            var cpuUsage = await GetCpuUsageForProcess();
            Console.WriteLine("CPU at Ready: " + cpuUsage.ToString() + "%");
            var ramUsage = GetRamUsageForProcess();
            Console.WriteLine("RAM at Ready: " + ramUsage.ToString() + "%");

            UpdateSiteStats();

            // Restart / reset scheduled messages and announcements
            _ = Task.Run(Schedule.LoadAndScheduleItemsAsync<ScheduledAnnouncement>);
            _ = Task.Run(Schedule.LoadAndScheduleItemsAsync<ScheduledMessage>);

            await Task.Delay(Timeout.Infinite);
        }

        private static void UpdateSiteStats()
        {
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
        }

        private static async Task RegisterSlashCommands()
        {
            var interactionService = Services.GetRequiredService<InteractionService>();

            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), Services);
            var globalCommands = await interactionService.RegisterCommandsGloballyAsync();

            // Update command IDs...
            Dictionary<string, ulong> _commandIds = globalCommands.ToDictionary(cmd => cmd.Name, cmd => cmd.Id);

            foreach (var group in Help.CommandGroups)
            {
                foreach (var command in group.Commands)
                {
                    if (command.InheritGroupName)
                    {
                        if (_commandIds.TryGetValue(group.Name, out ulong commandId))
                        {
                            command.Id = commandId;
                        }
                    }
                    else
                    {
                        var commandNameParts = command.Name.Split(' ');
                        string lookupName = commandNameParts.Length > 1 ? commandNameParts[0] : command.Name;

                        if (_commandIds.TryGetValue(lookupName, out ulong commandId))
                        {
                            command.Id = commandId;
                        }
                    }
                }
            }

            // Optional: Register per-guild debug commands
            ModuleInfo[] debugCommands = interactionService.Modules
                .Where(module => module.Preconditions.Any(precondition => precondition is RequireGuildAttribute)
                              && module.SlashGroupName == "debug")
                .ToArray();
            IGuild supportServer = Client.GetGuild(supportServerId);
            await interactionService.AddModulesToGuildAsync(supportServer, true, debugCommands);

            Client.InteractionCreated += InteractionCreated;
            interactionService.SlashCommandExecuted += SlashCommandResulted;

            Console.WriteLine("Slash commands registered successfully.");
        }

        private static Task ShardReady(DiscordSocketClient shard)
        {
            _shardsReady++;
            if (_shardsReady == Client.Shards.Count)
            {
                _allShardsReady.TrySetResult(true);
            }

            if (_shardsReady <= Client.Shards.Count)
            {
                // Status rotation
                _ = Task.Run(async () =>
                {
                    int index = 0;

                    var timer = new PeriodicTimer(TimeSpan.FromSeconds(16));
                    while (await timer.WaitForNextTickAsync())
                    {
                        await shard.SetCustomStatusAsync(statuses[index]);
                        index = index + 1 == statuses.Length ? 0 : index + 1;
                    }
                });
            }

            return Task.CompletedTask;
        }

        private static async Task UserJoined(SocketGuildUser user)
        {
            try
            {
                Server server;
                using (var scope = Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                    server = await context.GetOrCreateServerAsync(user.Guild.Id);
                }

                if (server.Welcome == true)
                {
                    ChannelPermissions permissions = user.Guild.GetUser(Client.CurrentUser.Id).GetPermissions(user.Guild.SystemChannel);
                    if (user.Guild.SystemChannel != null && permissions.SendMessages && permissions.ViewChannel)
                    {
                        if (server.HasWelcomeImage)
                        {
                            WelcomeImage welcomeImage = null;
                            using (var scope = Services.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                                welcomeImage = await context.GetWelcomeImage(user.Guild.Id);
                            }

                            if (welcomeImage != null)
                            {
                                await user.Guild.SystemChannel.SendFileAsync(new MemoryStream(welcomeImage.Image), "welcome.webp", text: Welcome.FormatCustomMessage(server.CustomWelcomeMessage, user.Mention));
                            }
                        }
                        else if (server.CustomWelcomeMessage != null && server.CustomWelcomeMessage != "")
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
                    using (var scope = Services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                        dbUser = await context.GetOrCreateUserAsync(user.Id);
                    }

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
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            await context.GetOrCreateServerAsync(guild.Id);
        }

        private static async Task EntitlementCreated(SocketEntitlement ent)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            IUser entUser = await ent.User.Value.GetOrDownloadAsync();
            User user = await context.GetOrCreateUserAsync(entUser.Id);

            if (ent.EndsAt == null)
            {
                user.PremiumExpiration = DateTimeOffset.MaxValue;
            }
            else
            {
                user.PremiumExpiration = (DateTimeOffset)ent.EndsAt;
            }

            await context.SaveChangesAsync();
        }

        private static async Task EntitlementUpdated(Cacheable<SocketEntitlement, ulong> before, SocketEntitlement after)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
            IUser entUser = await before.Value.User.Value.GetOrDownloadAsync();
            User user = await context.GetOrCreateUserAsync(entUser.Id);

            user.PremiumExpiration = (DateTimeOffset)after.EndsAt;
            await context.SaveChangesAsync();
        }

        private static Task EntitlementDeleted(Cacheable<SocketEntitlement, ulong> ent)
        {
            return Task.CompletedTask;
        }

        private static Task MessageReceived(SocketMessage message)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Ensure channel is not null (guild messages only)
                    if (message.Channel is not SocketGuildChannel channel)
                        return;

                    SocketGuildUser fetchedBot = Client.GetGuild(channel.Guild.Id)
                        .GetUser(Client.CurrentUser.Id);
                    var botPerms = fetchedBot.GetPermissions(channel);

                    if (!botPerms.SendMessages)
                        return;

                    // Auto Publish if in a News Channel
                    if (channel.GetChannelType() == ChannelType.News &&
                        message.Components == null)
                    {
                        using var scope = Services.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<BobEntities>();
                        var newsChannel = await context.GetNewsChannel(channel.Id);

                        if (newsChannel != null)
                        {
                            if (message is IUserMessage userMessage)
                                await userMessage.CrosspostAsync();
                            return;
                        }
                    }

                    // Ignore bots
                    if (message.Author.IsBot)
                        return;

                    // Mention handling
                    if (channel.Guild.Id == supportServerId && message.Content.StartsWith("<@705680059809398804>"))
                    {
                        await ChatHandling.HandleMentionAsync(message);
                    }

                    // GitHub auto-embeds
                    Server server;
                    using (var scope = Services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<BobEntities>();
                        server = await dbContext.GetServer(channel.Guild.Id);
                    }

                    if (server.AutoEmbedGitHubLinks)
                    {
                        var gitHubLink = GitHubLinkParse.GetUrl(message.Content);
                        if (gitHubLink != null)
                        {
                            switch (gitHubLink.Type)
                            {
                                case GitHubLinkParse.GitHubLinkType.CodeFile:
                                    var linkInfo = CodeReader.CreateFileLinkInfo(gitHubLink.Url, true);
                                    string previewLines = await CodeReader.GetPreview(linkInfo);
                                    string preview =
                                        $"🔎 Showing {CodeReader.GetFormattedLineNumbers(linkInfo.LineNumbers)} of " +
                                        $"[{linkInfo.Repository}/{linkInfo.Branch}/{linkInfo.File}](<{gitHubLink.Url}>)\n" +
                                        $"```{linkInfo.File[(linkInfo.File.IndexOf('.') + 1)..]}\n{previewLines}```";
                                    await message.Channel.SendMessageAsync(preview);
                                    break;

                                case GitHubLinkParse.GitHubLinkType.PullRequest:
                                    var prInfo = PullRequestReader.CreatePullRequestInfo(gitHubLink.Url);
                                    await message.Channel.SendMessageAsync(
                                        embed: await PullRequestReader.GetPreview(prInfo)
                                    );
                                    break;

                                case GitHubLinkParse.GitHubLinkType.Issue:
                                    var issueInfo = IssueReader.CreateIssueInfo(gitHubLink.Url);
                                    await message.Channel.SendMessageAsync(
                                        embed: await IssueReader.GetPreview(issueInfo)
                                    );
                                    break;
                            }
                            return;
                        }
                    }

                    // Discord message link auto-embeds
                    if (server.AutoEmbedMessageLinks)
                    {
                        var discordLink = DiscordMessageLinkParse.GetUrl(message.Content);
                        if (discordLink != null)
                        {
                            var linkInfo = CreateMessageInfo(discordLink.Url);
                            var preview = await GetPreview(linkInfo);
                            if (preview != null)
                                await message.Channel.SendMessageAsync(embed: preview);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            return Task.CompletedTask;
        }

        private static Task HandleReactionAddedAsync(
            Cacheable<IUserMessage, ulong> cacheable,
            Cacheable<IMessageChannel, ulong> channelCache,
            SocketReaction reaction)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (await channelCache.GetOrDownloadAsync() is not SocketTextChannel textChannel)
                    {
                        return;
                    }

                    var botUser = textChannel.GetUser(Client.CurrentUser.Id);

                    if (botUser == null || !botUser.GetPermissions(textChannel).ReadMessageHistory)
                    {
                        return;
                    }

                    var userMessage = await CachedMessages.GetOrDownloadAsync(textChannel, cacheable.Id);
                    if (userMessage == null)
                    {
                        return;
                    }

                    string key = reaction.Emote is Emote emote ? emote.Id.ToString() : reaction.Emote.Name;

                    userMessage.IncreaseReactionCount(key);

                    using var scope = Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<BobEntities>();
                    var server = await dbContext.GetOrCreateServerAsync(textChannel.Guild.Id);

                    if (!ReactBoardMethods.IsSetup(server))
                    {
                        return;
                    }

                    var storedEmojiId = ReactBoardMethods.GetEmojiIdFromString(server.ReactBoardEmoji);

                    bool isMatchingEmoji = (storedEmojiId != null && key == storedEmojiId)
                        || reaction.Emote.Name.Equals(server.ReactBoardEmoji, StringComparison.OrdinalIgnoreCase);

                    if (!isMatchingEmoji || textChannel.Id == server.ReactBoardChannelId)
                    {
                        return;
                    }

                    var reactionCount = userMessage.GetReactionCount(key);

                    if (reactionCount == 0 || reactionCount < server.ReactBoardMinimumReactions)
                    {
                        return;
                    }

                    if (Client.GetChannel(server.ReactBoardChannelId.Value) is not SocketTextChannel reactBoardChannel)
                    {
                        return;
                    }

                    botUser = reactBoardChannel.GetUser(Client.CurrentUser.Id);
                    if (botUser == null || !botUser.GetPermissions(reactBoardChannel).SendMessages)
                    {
                        return;
                    }

                    if (await ReactBoardMethods.IsMessageOnBoardAsync(reactBoardChannel, userMessage.Message.Id))
                    {
                        return;
                    }

                    await reactBoardChannel.SendMessageAsync(
                        embeds: [.. ReactBoardMethods.GetReactBoardEmbeds(userMessage.Message)],
                        allowedMentions: AllowedMentions.None,
                        components: ReactBoardMethods.GetReactBoardComponents(userMessage.Message)
                    );

                    await ReactBoardMethods.AddToCacheAndDbAsync(reactBoardChannel, userMessage.Message.Id);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error handling reaction: {e.Message}");
                    Console.WriteLine(e.StackTrace);
                }
            });
            return Task.CompletedTask;
        }

        public static Task HandleReactionRemovedAsync(
            Cacheable<IUserMessage, ulong> cacheable,
            Cacheable<IMessageChannel, ulong> channelCache,
            SocketReaction reaction)
        {
            _ = Task.Run(async () =>
            {
                if (await channelCache.GetOrDownloadAsync() is not SocketTextChannel textChannel)
                {
                    return;
                }

                var userMessage = await CachedMessages.GetOrDownloadAsync(textChannel, cacheable.Id);
                if (userMessage == null)
                {
                    return;
                }

                var key = reaction.Emote is Emote emote ? emote.Id.ToString() : reaction.Emote.Name;

                userMessage.DecrementReactionCount(key);
            });
            return Task.CompletedTask;
        }

        public static Task HandleReactionClearedAsync(
            Cacheable<IUserMessage, ulong> cacheable,
            Cacheable<IMessageChannel, ulong> channelCache,
            SocketReaction reaction)
        {
            _ = Task.Run(async () =>
            {
                if (await channelCache.GetOrDownloadAsync() is not SocketTextChannel textChannel)
                {
                    return;
                }

                var userMessage = await CachedMessages.GetOrDownloadAsync(textChannel, cacheable.Id);
                if (userMessage == null)
                {
                    return;
                }

                var key = reaction.Emote is Emote emote ? emote.Id.ToString() : reaction.Emote.Name;

                userMessage.SetReactionCount(key, 0);
            });
            return Task.CompletedTask;
        }

        private static async Task InteractionCreated(SocketInteraction interaction)
        {
            try
            {
                var interactionService = Services.GetRequiredService<InteractionService>();
                var ctx = new ShardedInteractionContext(Client, interaction);
                await interactionService.ExecuteCommandAsync(ctx, Services);
            }
            catch
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(async (msg) => await msg.Result.DeleteAsync());
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
                        await ctx.Interaction.FollowupAsync($"❌ Something went wrong...\n- Ensure Bob has the `View Channel` and `Send Messages` permissions.\n- Try again later.\n- The developers have been notified.\n- Or, join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ) and let us know about it.", ephemeral: true);

                        var executionResult = (ExecuteResult)res;
                        Console.WriteLine($"Error: {executionResult.Exception}");

                        await LogErrorToDiscord(ctx, info, $"{executionResult.ErrorReason}\n{executionResult.Exception}");

                        // Live Debugging
                        // Server Logging
                        if (ctx.Interaction.GuildId.HasValue && DebugGroup.LogGroup.ServersToLog.ContainsKey(ctx.Interaction.GuildId.Value))
                        {
                            DebugGroup.LogGroup.ServerLogChannels.TryGetValue(ctx.Interaction.GuildId.Value, out RestTextChannel debugLogChannel);
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
                int? shardId = ctx.Interaction.GuildId == null ? null : (ctx as ShardedInteractionContext).Client.GetShardIdFor(ctx.Guild);
                var commandName = info.IsTopLevelCommand ? $"/{info.Name}" : $"/{info.Module.SlashGroupName} {info.Name}";
                Console.WriteLine($"{DateTime.Now:dd/MM. H:mm:ss} | {FormatPerformance(cpuUsage, ramUsage)} | Shard: {(shardId == null ? "N" : shardId)} | Location: {location} | Command: {commandName}");

                // Live Debugging
                // Server Logging
                if (ctx.Interaction.GuildId.HasValue && DebugGroup.LogGroup.ServersToLog.ContainsKey(ctx.Interaction.GuildId.Value))
                {
                    DebugGroup.LogGroup.ServerLogChannels.TryGetValue(ctx.Interaction.GuildId.Value, out RestTextChannel debugLogChannel);
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
}