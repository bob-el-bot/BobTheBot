using Discord.Interactions;
using System.Threading.Tasks;
using System;
using System.Text.Json.Nodes;
using Discord.WebSocket;
using Discord;
using System.Text;
using static ApiInteractions.Interface;
using Commands.Helpers;
using Database.Types;
using SimpleCiphers;
using System.Reflection.Emit;
using System.Collections.Generic;
using ColorHelper;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Database;
using System.Text.RegularExpressions;
using System.Linq;
using Games;
using Challenges;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.Design.Serialization;
using System.Security.Cryptography;
using PremiumInterface;
using System.Reactive;
using ColorMethods;

namespace Commands
{
    public class NoGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("ping", "Bob will share his ping.")]
        public async Task Ping()
        {
            await RespondAsync(text: $"🏓 Pong! The client latency is **{Bot.Client.Latency}** ms.");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("hi", "Say hi to Bob.")]
        public async Task Hi()
        {
            await RespondAsync(text: "👋 hi!");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("analyze-link", "Bob will check out a link, and see where it takes you.")]
        public async Task AnalyzeLink([Summary("link", "The link in question.")] string link)
        {
            if (link.Contains('.') && link.Length < 7 || (link.Length >= 7 && link[..7] != "http://" && link.Length >= 8 && link[..8] != "https://"))
            {
                link = $"https://{link}";
            }

            if (!Uri.IsWellFormedUriString(link, UriKind.Absolute))
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know:\n- Your link could look like this `http://bobthebot.net`, `https://bobthebot.net`, or `bobthebot.net`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                await DeferAsync();
                await FollowupAsync(embed: await Analyze.AnalyzeLink(link));
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [MessageCommand("Analyze Link")]
        public async Task AnalyzeMessageLink(IMessage message)
        {
            string pattern = @"(https?://\S+)|(www\.\S+)";

            // Create a Regex object with the pattern
            Regex regex = new(pattern);

            // Find all matches in the input string
            MatchCollection matches = regex.Matches(message.Content);

            if (matches.Count == 0)
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know:\n- Your link could look like this `http://bobthebot.net`, or `https://bobthebot.net`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                await DeferAsync();
                await FollowupAsync(embed: await Analyze.AnalyzeLink(matches[0].Value));
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("hug", "Hug your friends! (up to 5 people in a group hug!)")]
        public async Task Hug(SocketUser person1, SocketUser person2 = null, SocketUser person3 = null, SocketUser person4 = null, SocketUser person5 = null)
        {
            StringBuilder response = new();
            response.Append(Context.User.Mention + " *hugs* " + person1.Mention);

            SocketUser[] people = { person2, person3, person4, person5 };

            for (int i = 0; i < people.Length; i++)
            {
                if (people[i] != null)
                {
                    response.Append(", " + people[i].Mention);
                }
            }

            await RespondAsync(text: $"🤗🫂 {response}" + "!");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("tic-tac-toe", "Play a game of Tic Tac Toe.")]
        public async Task TicTacToe([Summary("opponent", "Leave empty to verse an AI.")] SocketUser opponent = null)
        {
            if (opponent == null || opponent.IsBot)
            {
                await DeferAsync();
                TicTacToe game = new(Context.User, opponent ?? Bot.Client.CurrentUser);
                await game.StartBotGame(Context.Interaction);
            }
            else
            {
                (bool, string) canChallenge = await Challenge.CanChallengeAsync(Context.User.Id, opponent.Id);
                if (!canChallenge.Item1)
                {
                    await RespondAsync(text: canChallenge.Item2, ephemeral: true);
                }
                else
                {
                    await DeferAsync();
                    await Challenge.SendMessage(Context.Interaction, new TicTacToe(Context.User, opponent));
                }
            }
        }

        [ComponentInteraction("ttt:*:*")]
        public async Task TTTButtonHandler(string coordinate, string Id)
        {
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            Challenge.TicTacToeGames.TryGetValue(Convert.ToUInt64(Id), out TicTacToe game);

            if (game == null)
            {
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use `/tic-tac-toe` to start a new game.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else if (game.isPlayer1Turn && !isPlayer1 || !game.isPlayer1Turn && isPlayer1)
                {
                    await component.RespondAsync(text: $"❌ It is **not** your turn.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else
                {
                    await DeferAsync();

                    // Prepare Position
                    string[] coords = coordinate.Split('-');
                    int[] position = { Convert.ToInt16(coords[0]), Convert.ToInt16(coords[1]) };

                    // Check if the chosen move is valid and within bounds
                    if (game.grid[position[0], position[1]] == 0)
                    {
                        game.grid[position[0], position[1]] = game.isPlayer1Turn ? 1 : 2;
                        if (game.Player2.IsBot)
                        {
                            await game.EndBotTurn(component);
                        }
                        else
                        {
                            await game.EndTurn(component);
                        }
                    }
                    else
                    {
                        // Handle the case where the chosen move is not valid or out of bounds
                        await component.RespondAsync(text: $"❌ Invalid move.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    }
                }
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("trivia", "Play a game of trivia.")]
        public async Task Trivia([Summary("opponent", "Leave empty to play alone.")] SocketUser opponent = null)
        {
            if (opponent == null || opponent.IsBot)
            {
                await DeferAsync();
                Trivia game = new(Context.User, opponent ?? Bot.Client.CurrentUser);
                await game.StartBotGame(Context.Interaction);
            }
            else
            {
                (bool, string) canChallenge = await Challenge.CanChallengeAsync(Context.User.Id, opponent.Id);
                if (!canChallenge.Item1)
                {
                    await RespondAsync(text: canChallenge.Item2, ephemeral: true);
                }
                else
                {
                    await DeferAsync();
                    await Challenge.SendMessage(Context.Interaction, new Trivia(Context.User, opponent));
                }
            }
        }

        [ComponentInteraction("trivia:*:*")]
        public async Task TriviaButtonHandler(string answer, string Id)
        {
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            Challenge.TriviaGames.TryGetValue(Convert.ToUInt64(Id), out Trivia game);

            if (game == null)
            {
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use `/trivia` to start a new game.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else if (game.player1Answer != null && isPlayer1 || game.player2Answer != null && isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You have already answered.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else
                {
                    await DeferAsync();

                    // Answer
                    if (game.Player2.IsBot)
                    {
                        await game.AloneAnswer(answer, component);
                    }
                    else
                    {
                        await game.Answer(isPlayer1, answer, component);
                    }
                }
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("rock-paper-scissors", "Play a game of Rock Paper Scissors.")]
        public async Task RPS([Summary("opponent", "Leave empty to verse an AI.")] SocketUser opponent = null)
        {
            if (opponent == null || opponent.IsBot)
            {
                await DeferAsync();
                RockPaperScissors game = new(Context.User, opponent ?? Bot.Client.CurrentUser);
                await game.StartBotGame(Context.Interaction);
            }
            else
            {
                (bool, string) canChallenge = await Challenge.CanChallengeAsync(Context.User.Id, opponent.Id);
                if (!canChallenge.Item1)
                {
                    await RespondAsync(text: canChallenge.Item2, ephemeral: true);
                }
                else
                {
                    await DeferAsync();
                    await Challenge.SendMessage(Context.Interaction, new RockPaperScissors(Context.User, opponent));
                }
            }
        }

        [ComponentInteraction("rps:*:*")]
        public async Task RPSButtonHandler(string rps, string Id)
        {
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            Challenge.RockPaperScissorsGames.TryGetValue(Convert.ToUInt64(Id), out RockPaperScissors game);

            if (game == null)
            {
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use `/rock-paper-scissors` to start a new game.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                int choice = Convert.ToInt16(rps);
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else if ((isPlayer1 && game.player1Choice != -1) || (isPlayer2 && game.player2Choice != -1))
                {
                    await component.RespondAsync(text: $"❌ You **cannot** change your choice.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else
                {
                    if (isPlayer1)
                    {
                        game.player1Choice = choice;
                    }
                    else if (isPlayer2)
                    {
                        game.player2Choice = choice;
                    }

                    if (game.player1Choice != -1 && game.player2Choice != -1)
                    {
                        await game.FinishGame(component);
                    }
                    else
                    {
                        string[] options = { "🪨", "📃", "✂️" };
                        await component.RespondAsync($"You picked {options[choice]}", ephemeral: true);
                    }
                }
            }
        }

        // [EnabledInDm(false)]
        // [SlashCommand("connect4", "Play a game of Connect 4.")]
        // public async Task Connect4([Summary("opponent", "Leave empty to verse an AI.")] SocketUser opponent = null)
        // {
        //     if (opponent == null || opponent.IsBot)
        //     {
        //         await DeferAsync();
        //         Connect4 game = new(Context.User, opponent ?? Bot.Client.CurrentUser);
        //         await game.StartBotGame(Context.Interaction);
        //     }
        //     else
        //     {
        //         if (!await Challenge.CanChallengeAsync(Context.User.Id, opponent.Id))
        //         {
        //             await RespondAsync(text: "❌ You can't challenge them.", ephemeral: true);
        //         }
        //         else
        //         {
        //             await DeferAsync();
        //             await Challenge.SendMessage(Context.Interaction, new Connect4(Context.User, opponent));
        //         }
        //     }
        // }

        // [ComponentInteraction("connect4:*:*")]
        // public async Task Connect4ButtonHandler(string column, string Id)
        // {
        //     SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

        //     Challenge.Connect4Games.TryGetValue(Convert.ToUInt64(Id), out Connect4 game);

        //     if (game == null)
        //     {
        //         await component.RespondAsync(text: $"❌ This game no longer exists\n- Use `/connect4` to start a new game.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        //     }
        //     else
        //     {
        //         bool isPlayer1 = component.User.Id == game.Player1.Id;
        //         bool isPlayer2 = component.User.Id == game.Player2.Id;

        //         if (!isPlayer1 && !isPlayer2)
        //         {
        //             await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        //         }
        //         else if (game.player1Answer != null && isPlayer1 || game.player2Answer != null && isPlayer2)
        //         {
        //             await component.RespondAsync(text: $"❌ You have already answered.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
        //         }
        //         else
        //         {
        //             await DeferAsync();

        //             // Answer
        //             if (game.Player2.IsBot)
        //             {
        //                 await game.AloneAnswer(answer, component);
        //             }
        //             else
        //             {
        //                 await game.Answer(isPlayer1, answer, component);
        //             }
        //         }
        //     }
        // }

        [ComponentInteraction("acceptChallenge:*")]
        public async Task AcceptChallengeButtonHandler(string Id)
        {
            Challenge.Games.TryGetValue(Convert.ToUInt64(Id), out Games.Game challenge);
            if (challenge == null)
            {
                await Context.Interaction.RespondAsync(text: $"❌ This challenge no longer exists.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                if (Context.Interaction.User.Id != challenge.Player2.Id)
                {
                    await Context.Interaction.RespondAsync(text: $"❌ **Only** {challenge.Player2.Mention} can **accept** this challenge.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else
                {
                    await DeferAsync();

                    // Update User Info
                    Challenge.IncrementUserChallenges(challenge.Player2.Id);

                    await challenge.StartGame((SocketMessageComponent)Context.Interaction);
                }
            }
        }

        [ComponentInteraction("declineChallenge:*")]
        public async Task DeclineChallengeButtonHandler(string Id)
        {
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;
            Challenge.Games.TryGetValue(Convert.ToUInt64(Id), out Games.Game challenge);

            if (challenge == null)
            {
                await Context.Interaction.RespondAsync(text: $"❌ This challenge no longer exists.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                if (Context.Interaction.User.Id != challenge.Player2.Id)
                {
                    await component.RespondAsync(text: $"❌ **Only** {challenge.Player2.Mention} can **decline** this challenge.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                }
                else
                {
                    await DeferAsync();

                    // Update User Info
                    Challenge.DecrementUserChallenges(challenge.Player1.Id);

                    // Format Message
                    var embed = new EmbedBuilder
                    {
                        Color = Challenge.DefaultColor,
                        Description = $"### ⚔️ {challenge.Player1.Mention} Challenges {challenge.Player2.Mention} to {challenge.Title}.\n{challenge.Player2.Mention} declined."
                    };

                    var components = new ComponentBuilder().WithButton(label: "⚔️ Accept", customId: $"acceptedChallenge", style: ButtonStyle.Success, disabled: true)
                    .WithButton(label: "🛡️ Decline", customId: $"declinedChallenge", style: ButtonStyle.Danger, disabled: true);

                    await component.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); x.Content = null; x.Components = components.Build(); });

                    Challenge.RemoveFromSpecificGameList(challenge);
                    challenge.Dispose();
                }
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("review", "Leave a review for Bob on Top.gg")]
        public async Task Review()
        {
            // Respond
            await RespondAsync(text: "📝 If you're enjoying BobTheBot, please consider leaving a review on Top.gg!\n[review here](https://top.gg/bot/705680059809398804#reviews)");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("vote", "Vote for Bob on Top.gg")]
        public async Task Vote()
        {
            // Respond
            await RespondAsync(text: "**Top.gg is not associated with BobTheBot and so ads cannot be removed by Bob's creators.**\n\nVote for Bob!\n[vote here](https://top.gg/bot/705680059809398804/vote)");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("quote-prompts", "Bob will give you all valid prompts for /random quote.")]
        public async Task QuotePrompts()
        {
            // Respond
            await RespondAsync(text: $"Here are all valid prompts for `/random quote`:\nage, athletics, business, change, character, competition, conservative, courage, education, ethics, failure, faith, family, famous-quotes, film, freedom, future, generosity, genius, gratitude, happiness, health, history, honor, humor, humorous, inspirational, knowledge, leadership, life, love, mathematics, motivational, nature, oppurtunity, pain, perseverance, philosphy, politics, power-quotes, proverb, religion, sadness, science, self, sports, stupidity, success, technology, time, tolerance, truth, virtue, war, weakness, wellness, wisdom, work");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("announce", "Bob will create a fancy embed announcement in the channel the command is used in.")]
        public async Task Announce([Summary("title", "The title of the announcement (the title of the embed).")] string title, [Summary("description", "The anouncement (the description of the embed).")] string description, [Summary("color", "A color name (purple), or valid hex code (#8D52FD).")] string color)
        {
            Color finalColor = Colors.TryGetColor(color);

            // Check if Bob has permission to send messages in given channel
            if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel).ViewChannel)
            {
                await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{Context.Channel.Id}>\n- Try giving Bob the following pemrissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (finalColor == 0)
            {
                await RespondAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (title.Length > 256) // 256 is max characters in an embed title.
            {
                await RespondAsync($"❌ The announcement *cannot* be made because it contains **{title.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **256** characters in embed titles.", ephemeral: true);
            }
            else if (description.Length > 4096) // 4096 is max characters in an embed description.
            {
                await RespondAsync($"❌ The announcement *cannot* be made because it contains **{description.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Title = title,
                    Color = finalColor,
                    Description = Announcement.FormatDescription(description),
                    Footer = new EmbedFooterBuilder
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Text = $"Announced by {Context.User.GlobalName}."
                    }
                };

                await RespondAsync(text: "✅ Your announcement has been made.", ephemeral: true);
                await Context.Channel.SendMessageAsync(embed: embed.Build());
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("info", "Learn about Bob.")]
        public async Task Info()
        {
            var createdAt = Bot.Client.CurrentUser.CreatedAt.ToUnixTimeSeconds();

            var embed = new EmbedBuilder
            {
                Title = $"Bob's Info",
                Color = Bot.theme
            };

            embed.AddField(name: "📛 Username", value: $"{Bot.Client.CurrentUser.Username}", inline: true).AddField(name: "🪪 ID", value: $"`{Bot.Client.CurrentUser.Id}`", inline: true).AddField(name: ":calendar_spiral: Date Created", value: $"<t:{createdAt}:f>", inline: false).AddField(name: "📈 Servers", value: $"`{Bot.Client.Guilds.Count:n0}`", inline: true).AddField(name: "🤗 Users", value: $"`{Bot.totalUsers:n0}`", inline: true).AddField(name: "🌐 Website", value: "[bobthebot.net](https://bobthebot.net)").AddField(name: "⚡ Github Repository", value: "[github.com/bob-el-bot/BobTheBot](https://github.com/bob-el-bot/BobTheBot)").AddField(name: "🏗️ Made With", value: "C#, .NET", inline: true).AddField(name: "📡 Hosted With", value: "Raspberry PI 4", inline: true);

            await RespondAsync(embed: embed.Build());
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("new", "See the newest changes to Bob, and find out what's next.")]
        public async Task New()
        {
            string content = await GetFromAPI("https://api.github.com/repos/bob-el-bot/BobTheBot/commits/main", AcceptTypes.application_json);

            // Parse Content
            var jsonData = JsonNode.Parse(content).AsObject();
            var commit = JsonNode.Parse(jsonData["commit"].ToString()).AsObject();
            var commitMessage = commit["message"].ToString();
            var commitAuthor = JsonNode.Parse(commit["author"].ToString()).AsObject();
            var commitDate = commitAuthor["date"].ToString();
            // 2023-07-29T03:50:42Z -> unix epoch time
            var commitDateID = DateTimeOffset.Parse(commitDate).ToUnixTimeSeconds();

            var embed = new EmbedBuilder
            {
                Title = $"What's New?",
                Color = Bot.theme
            };

            embed.AddField(name: "🗒️ Creator's Notes", value: "Premium has now been officially implented and will only get better!\n💜 **thanks so much to those of you who supported Bob before premium features even existed!**\n- Added `/profile display` for looking at 🪪 user's game stats.\n- Added `/premium` for buying *premium* (💜 another thanks to the og supporters!).\n- Added `/profile set-color` for *premium* users to change their profiles' 🌈 color.\n- Added **unlimited** challenges for *premium* users.\n- Added `/quote set-max-length` and `/quote set-min-length` for *premium* users to add 📏 length standards for their server's quotes.\n- Added `/welcome set-message` for *premium* users to create custom 👋 welcome messages on their servers.\n- Fixed bug where `/poll` and `/announce` would not check for Send Messages permission.\n- Stay 📺 tuned for some awesome updates!", inline: false).AddField(name: "✨ Latest Update", value: commitMessage, inline: true).AddField(name: ":calendar_spiral: Date", value: $"<t:{commitDateID}:f>", inline: true).AddField(name: "🔮 See What's In the Works", value: "[Road Map](https://github.com/orgs/bob-el-bot/projects/4)");

            await RespondAsync(embed: embed.Build());
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("help", "Bob will DM you all relevant information for every command.")]
        public async Task help()
        {
            await DeferAsync(ephemeral: true);

            try
            {
                var firstEmbed = new EmbedBuilder
                {
                    Title = $"📖 Here is a list of all of my commands.",
                    Color = Bot.theme,
                    Description = @"See the [Docs](https://docs.bobthebot.net) for more in depth info.
**🎲 Randomly Generated (RNG):** [RNG Docs](https://docs.bobthebot.net#rng)
- `/random color` Get a color with Hex, CMYK, HSL, HSV and RGB codes. [Docs](https://docs.bobthebot.net#random-color)
- `/random dice-roll [sides]` Roll a die with a specified # of sides. [Docs](https://docs.bobthebot.net#random-dice-roll)
- `/random coin-toss` Flip a coin. [Docs](https://docs.bobthebot.net#random-coin-toss)
- `/random quote [prompt]` Get a random quote. [Docs](https://docs.bobthebot.net#random-quote)
  - `[prompt]`choices: This is optional, use `/quote-prompts` to view all valid prompts.
- `/random dad-joke` Get a random dad joke. [Docs](https://docs.bobthebot.net#random-dad-joke)
- `/random fact` Get an outrageous fact. [Docs](https://docs.bobthebot.net#random-fact)
- `/random 8ball [prompt]` Get an 8 ball response to a prompt. [Docs](https://docs.bobthebot.net#random-8ball)
- `/random dog` Get a random picture of a dog. [Docs](https://docs.bobthebot.net#random-dog)
- `/random date [earliestYear] [latestYear]` Get a random date between the inputted years. [Docs](https://docs.bobthebot.net#random-date)
- `/random advice` Get a random piece of advice. [Docs](https://docs.bobthebot.net#random-advice)
**🎮 Games:** [Games Docs](https://docs.bobthebot.net#games)
- `trivia [opponent]` Play a game of trivia with or without someone. [Docs](https://docs.bobthebot.net#trivia)
- `/tic-tac-toe [opponent]` Play Bob or a user in a game of Tic Tac Toe. [Docs](https://docs.bobthebot.net#tic-tac-toe)
- `/rock-paper-scissors [opponent]` Play Bob or a user in a game of Rock Paper Scissors. [Docs](https://docs.bobthebot.net#rock-paper-scissors)
- `/master-mind new-game` Play a game of Master Mind, the rules will shared upon usage. [Docs](https://docs.bobthebot.net#master-mind-new)
- `/master-mind guess` Make a guess in a game of Master Mind. [Docs](https://docs.bobthebot.net#master-mind-guess)
**👤 Profiles:** [Profile Docs](https://docs.bobthebot.net#profile)
- `/profile display [user]` Displays the specified user's profile. [Docs](https://docs.bobthebot.net#profile-display)
- `/profile set-color [color]` Sets your profile color. [Docs](https://docs.bobthebot.net#profile-set-color)
- `/profile badge-info [badge]` Shows how to unlock the given badge.
**🖊️ Quoting:** [Quoting Docs](https://docs.bobthebot.net#quoting)
- `/quote new [quote] [user] [tag]*3` Formats and shares the quote in designated channel. [Docs](https://docs.bobthebot.net#quote-new)
- `/quote channel [channel]` Sets the quote channel for the server. [Docs](https://docs.bobthebot.net#quote-channel)
- `/quote set-max-length [length]` Sets the maximum length of quotes for the server. [Docs](https://docs.bobthebot.net#quote-set-max-length)
- `/quote set-min-length [length]` Sets the minimum length of quotes for the server. [Docs](https://docs.bobthebot.net#quote-set-min-length)
**👋 Welcoming:** [Welcome Docs](https://docs.bobthebot.net#welcome)
- `/welcome toggle [welcome]` Bob will send welcome messages to new server members. [Docs](https://docs.bobthebot.net#welcome-toggle)
- `/welcome set-message [message]` Set a custom message to welcome new users with. [Docs](https://docs.bobthebot.net#welcome-set-message)
- `/welcome remove-message` Bob will stop using the custom message to welcome users. [Docs](https://docs.bobthebot.net#welcome-remove-message)"
                };

                var secondEmbed = new EmbedBuilder
                {
                    Title = $"",
                    Color = Bot.theme,
                    Description = @"**🔒 Encryption commands:** [Encryption Docs](https://docs.bobthebot.net#encrypt)
- `/encrypt a1z26 [message]` Encrypts your message by swapping letters to their corresponding number. [Docs](https://docs.bobthebot.net#encrypt-a1z26)
- `/encrypt atbash [message]` Encrypts your message by swapping letters to their opposite position. [Docs](https://docs.bobthebot.net#encrypt-atbash)
- `/encrypt caesar [message] [shift]` Encrypts your message by shifting the letters the specified amount. [Docs](https://docs.bobthebot.net#encrypt-caesar)
- `/encrypt morse [message]` Encrypts your message using Morse code. [Docs](https://docs.bobthebot.net#encrypt-morse)
- `/encrypt vigenere [message] [key]` Encrypts your message using a specified key. [Docs](https://docs.bobthebot.net#encrypt-vigenere)
**🔓 Decryption commands:** [Decryption Docs](https://docs.bobthebot.net#decrypt)
- `/decrypt a1z26 [message]` Decrypts your message by swapping letters to their corresponding number [Docs](https://docs.bobthebot.net#decrypt-a1z26).
- `/decrypt atbash [message]` Decrypts your message by swapping letters to their opposite position [Docs](https://docs.bobthebot.net#decrypt-atbash).
- `/decrypt caesar [message] [shift]` Decrypts your message by shifting the letters the specified amount [Docs](https://docs.bobthebot.net#decrypt-caesar).
- `/decrypt morse [message]` Decrypts your message using Morse code [Docs](https://docs.bobthebot.net#decrypt-morse).
- `/decrypt vigenere [message] [key]` Decrypts your message using a specified key [Docs](https://docs.bobthebot.net#decrypt-vigenere).
**✨ Other:** [Other Docs](https://docs.bobthebot.net#other)
- `/code preview [link]` Preview specific lines of code from a file on GitHub. [Docs](https://docs.bobthebot.net#code-preview)
- `/fonts [text] [font]` Change your text to a different font. [Docs](https://docs.bobthebot.net#fonts)
  - `[font]` choices: 𝖒𝖊𝖉𝖎𝖊𝖛𝖆𝖑, 𝓯𝓪𝓷𝓬𝔂, 𝕠𝕦𝕥𝕝𝕚𝕟𝕖𝕕, ɟןıddǝp, s̷l̷̷a̷s̷h̷e̷d̷, and 🄱🄾🅇🄴🄳.
- `/confess [message] [user] [signoff]` Have Bob DM a user a message. [Docs](https://docs.bobthebot.net#confess)
- `/announce [title] [description] [color]` Have a fancy embed message sent. [Docs](https://docs.bobthebot.net#announce)
- `/poll [prompt] [option]*4` Create a poll. [Docs](https://docs.bobthebot.net#poll)
  - `[option]*4` usage: You must provide 2-4 options. These are essentially the poll's choices.
- `/ship [user]*2` See how good of a match 2 users are. [Docs](https://docs.bobthebot.net#ship)
- `/hug [user]*5` Show your friends some love with a hug. [Docs](https://docs.bobthebot.net#hug)
**🖨️ Auto commands:** [Auto Docs](https://docs.bobthebot.net#auto)
- `/auto publish-announcements [publish] [channel]` Bob will publish all messages sent in the given channel. [Docs](https://docs.bobthebot.net#auto-publish-announcements)
**🗄️ Informational / Help:** [Info Docs](https://docs.bobthebot.net#info)
- `/premium` Ensures Bob knows you have premium! If not you will be given a button to get it! [Docs](https://docs.bobthebot.net#premium)
- `/new` See the latest updates to Bob. [Docs](https://docs.bobthebot.net#new)
- `/quote-prompts` See all valid prompts for `/random quote`. [Docs](https://docs.bobthebot.net#quote-prompts)
- `/ping` Find the client's latency. [Docs](https://docs.bobthebot.net#ping)
- `/analyze-link` See where a link will take you, and check for rick rolls. [Docs](https://docs.bobthebot.net#analyze-link)
- `/info` Learn about Bob. [Docs](https://docs.bobthebot.net#info)
- `/support` Sends an invite to Bob's support Server. [Docs](https://docs.bobthebot.net#support)"
                };

                await Context.User.SendMessageAsync(embed: firstEmbed.Build());
                await Context.User.SendMessageAsync(embed: secondEmbed.Build());
                await FollowupAsync(text: $"📪 Check your DMs.", ephemeral: true);
            }
            catch
            {
                await FollowupAsync(text: $"❌ Bob could not share the command list via DMs.\n- Try opening your DMs (making them open).\n- Or you can simply view the commands list here: [docs.bobthebot.net](<https://bobthebot.net/commands.html>)", ephemeral: true);
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("confess", "Bob will send someone a message anonymously")]
        public async Task confess(string message, SocketUser user, string signoff)
        {
            try
            {
                if (user.IsBot)
                {
                    await RespondAsync(text: "❌ Sorry, but no sending messages to bots.", ephemeral: true);
                }
                else if (message.Length + 3 + signoff.Length > 2000) // 2000 is max characters in a message.
                {
                    await RespondAsync($"❌ The message *cannot* be delivered because it contains **{message.Length + 3 + signoff.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **2000** characters.", ephemeral: true);
                }
                else
                {
                    await user.SendMessageAsync($"{message} - {signoff}");
                    await RespondAsync(text: $"✉️ Your message has been sent!\nMessage: **{message} - {signoff}** was sent to **{user.Username}**", ephemeral: true);
                }
            }
            catch
            {
                await RespondAsync(text: $"❌ Bob could not DM {user.Mention}.\n- You could try again, but this probably means their DMs are closed which Bob cannot change.", ephemeral: true);
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("premium", "Update your premium status or buy it!")]
        public async Task PremiumStatus()
        {
            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            // If user has premium ensure DB is updated.
            bool isPremium = Premium.IsPremium(Context.Interaction.Entitlements);
            if (isPremium == false && Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await RespondWithPremiumRequiredAsync();
            }
            else
            {
                await DeferAsync();

                if (isPremium != false)
                {
                    // Get Expiration Date
                    var expirationDate = (DateTimeOffset)Context.Interaction.Entitlements.FirstOrDefault(x => x.SkuId == 1169107771673812992).EndsAt;

                    // Only write to DB if needed.
                    if (user.PremiumExpiration != expirationDate)
                    {
                        user.PremiumExpiration = expirationDate;
                        await context.UpdateUser(user);
                    }
                }

                // Respond
                await FollowupAsync(text: "✨ Your premium status has been updated!\n**💜 Thanks so much** for your support and have fun with your new features!");
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("support", "Sends an invite to Bob's support Server.")]
        public async Task Support()
        {
            // Respond
            await RespondAsync(text: "🏰 Having issues with Bob? [Join Here](https://discord.gg/HvGMRZD8jQ) for help.");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("poll", "Bob will create a poll.")]
        public async Task Poll([Summary("prompt", "The question you are asking.")] string prompt, [Summary("option1", "an answer / response to your question")] string option1, [Summary("option2", "an answer / response to your question")] string option2, [Summary("option3", "an answer / response to your question")] string option3 = "", [Summary("option4", "an answer / response to your question")] string option4 = "")
        {
            // Check if Bob has permission to send messages in given channel
            if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel).ViewChannel)
            {
                await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{Context.Channel.Id}>\n- Try giving Bob the following pemrissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel).AddReactions) // Check if Bob can add reactions
            {
                await RespondAsync("❌ Bob needs the **Add Reactions** permission to use `/poll`\n- Try asking an administrator.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                // Setup base data
                string footerText = Context.User.GlobalName + " created this poll.";
                string instructions = "React with the corresponding number to cast your vote.";

                // Embed
                var embed = new EmbedBuilder
                {
                    Title = "📊 " + prompt,
                    Description = instructions,
                    Color = Bot.theme,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = footerText,
                        IconUrl = Context.User.GetAvatarUrl()
                    }
                };

                string[] possibleOptions = { option1, option2, option3, option4 };
                string[] optionLabels = { "1️⃣", "2️⃣", "3️⃣", "4️⃣" };
                int index = 0;
                foreach (string option in possibleOptions)
                {
                    if (option != "")
                    {
                        embed.AddField(name: $"Option {optionLabels[index]}", value: option);
                        index++;
                    }
                }

                await RespondAsync("✅ Your poll has been made.", ephemeral: true);

                var response = await Context.Channel.SendMessageAsync(embed: embed.Build());

                int index2 = 0;
                foreach (string option in possibleOptions)
                {
                    if (option != "")
                    {
                        await response.AddReactionAsync(new Emoji(optionLabels[index2]));
                        index2++;
                    }
                }
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("fonts", "Bob will type your text in a font of your choice")]
        public async Task Fonts([Summary("text", "the text you want converted. NOTE: only the alphabet is converted.")] string text, FontConversion.FontTypes font)
        {
            if (text.Length + 6 > 2000) // 2000 is max characters in a message.
            {
                await RespondAsync($"❌ The inputted text *cannot* be converted to a different font because it contains **{text.Length + 6}** characters.\n- Try having fewer characters.\n- Discord has a limit of **2000** characters.", ephemeral: true);
            }
            else
            {
                string finalText = font switch
                {
                    FontConversion.FontTypes.fancy => FontConversion.Fancy(text),
                    FontConversion.FontTypes.slashed => FontConversion.Slashed(text),
                    FontConversion.FontTypes.outlined => FontConversion.Outlined(text),
                    FontConversion.FontTypes.flipped => FontConversion.Flipped(text),
                    FontConversion.FontTypes.boxed => FontConversion.Boxed(text),
                    FontConversion.FontTypes.medieval => FontConversion.Medieval(text),
                    _ => text,
                };
                await RespondAsync($"```{finalText}```");
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("ship", "Bob will determine how good of a couple two users would make")]
        public async Task Ship(SocketUser person1, SocketUser person2)
        {
            // Prepare for calculations
            string id1 = person1.Id.ToString();
            int[] id1MakeUp = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            string id2 = person2.Id.ToString();
            int[] id2MakeUp = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int longestIdLength = id1.Length >= id2.Length ? id1.Length : id2.Length;

            // determine amount of each digit
            for (int i = 0; i < longestIdLength; i++)
            {
                if (i < id1.Length)
                {
                    id1MakeUp[int.Parse($"{id1[i]}")] += 1;
                }
                else
                {
                    id1MakeUp[10] += 1;
                }

                if (i < id2.Length)
                {
                    id2MakeUp[int.Parse($"{id2[i]}")] += 1;
                }
                else
                {
                    id2MakeUp[10] += 1;
                }
            }

            // determine difference between digits
            float idDifference = 0;
            for (int i = 0; i < 10; i++)
            {
                idDifference += MathF.Abs(id1MakeUp[i] - id2MakeUp[i]);
            }

            // determine difference in name lengths
            float nameLengthDifference = MathF.Abs(person1.Username.Length - person2.Username.Length);

            // determine difference in names
            int nameDifference = 0;
            if (person1.Username[0] != person2.Username[0])
            {
                nameDifference += 10;
                if (person1.Username[1] != person2.Username[1])
                {
                    nameDifference += 20;
                }
            }
            if (person1.Username[^1] != person2.Username[^1])
            {
                nameDifference += 10;
            }

            // calculate perecentage of similarity.
            float matchPercent = (idDifference / longestIdLength + nameLengthDifference / 30 + nameDifference / 40) / 3 * 100;

            // Determine Heart Level
            string heartLevel = HeartLevels.CalculateHeartLevel(matchPercent);

            // Embed
            var embed = new EmbedBuilder
            {
                Title = $"{person1.Username} ❤️ {person2.Username}",
                Color = new Color(15548997),
            };

            embed.AddField(name: $"Match of:", value: $"`{matchPercent}%`", inline: true).AddField(name: "Heart Level", value: heartLevel, inline: true);

            await RespondAsync(embed: embed.Build());
        }
    }
}




