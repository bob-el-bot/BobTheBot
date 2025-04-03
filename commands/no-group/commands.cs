using Discord.Interactions;
using System.Threading.Tasks;
using System;
using System.Text.Json.Nodes;
using Discord.WebSocket;
using Discord;
using System.Text;
using static Bob.ApiInteractions.Interface;
using Bob.Commands.Helpers;
using Bob.Database.Types;
using Bob.Database;
using System.Text.RegularExpressions;
using System.Linq;
using static Bob.Bot;
using Bob.Challenges;
using Bob.PremiumInterface;
using Bob.ColorMethods;
using Bob.Moderation;
using Bob.Time.Timestamps;

namespace Bob.Commands
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

            SocketUser[] people = [person2, person3, person4, person5];

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
        [RequireBotPermission(ChannelPermission.ViewChannel)]
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
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use {Help.GetCommandMention("tic-tac-toe")} to start a new game.", ephemeral: true);
            }
            else
            {
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.", ephemeral: true);
                }
                else if (game.IsPlayer1Turn && !isPlayer1 || !game.IsPlayer1Turn && isPlayer1)
                {
                    await component.RespondAsync(text: $"❌ It is **not** your turn.", ephemeral: true);
                }
                else
                {
                    await DeferAsync();

                    // Prepare Position
                    string[] coords = coordinate.Split('-');
                    int[] position = [Convert.ToInt16(coords[0]), Convert.ToInt16(coords[1])];

                    // Check if the chosen move is valid and within bounds
                    if (game.Grid[position[0], position[1]] == 0)
                    {
                        game.Grid[position[0], position[1]] = game.IsPlayer1Turn ? 1 : 2;
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
        [RequireBotPermission(ChannelPermission.ViewChannel)]
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
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use {Help.GetCommandMention("trivia")} to start a new game.", ephemeral: true);
            }
            else
            {
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.", ephemeral: true);
                }
                else if (game.Player1Answer != null && isPlayer1 || game.Player2Answer != null && isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You have already answered.", ephemeral: true);
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
        [RequireBotPermission(ChannelPermission.ViewChannel)]
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
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use {Help.GetCommandMention("rock-paper-scissors")} to start a new game.", ephemeral: true);
            }
            else
            {
                int choice = Convert.ToInt16(rps);
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.", ephemeral: true);
                }
                else if ((isPlayer1 && game.Player1Choice != -1) || (isPlayer2 && game.Player2Choice != -1))
                {
                    await component.RespondAsync(text: $"❌ You **cannot** change your choice.", ephemeral: true);
                }
                else
                {
                    if (isPlayer1)
                    {
                        game.Player1Choice = choice;
                    }
                    else if (isPlayer2)
                    {
                        game.Player2Choice = choice;
                    }

                    if (game.Player1Choice != -1 && game.Player2Choice != -1)
                    {
                        await game.FinishGame(component);
                    }
                    else
                    {
                        string[] options = ["🪨", "📃", "✂️"];
                        await component.RespondAsync($"You picked {options[choice]}", ephemeral: true);
                    }
                }
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [RequireBotPermission(ChannelPermission.ViewChannel)]
        [SlashCommand("connect4", "Play a game of Connect 4.")]
        public async Task Connect4([Summary("opponent", "Leave empty to verse an AI.")] SocketUser opponent = null)
        {
            if (opponent == null || opponent.IsBot)
            {
                await DeferAsync();
                Connect4 game = new(Context.User, opponent ?? Bot.Client.CurrentUser);
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
                    await Challenge.SendMessage(Context.Interaction, new Connect4(Context.User, opponent));
                }
            }
        }

        [ComponentInteraction("connect4:*:*")]
        public async Task Connect4ButtonHandler(string column, string Id)
        {
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            Challenge.Connect4Games.TryGetValue(Convert.ToUInt64(Id), out Connect4 game);

            if (game == null)
            {
                await component.RespondAsync(text: $"❌ This game no longer exists\n- Use {Help.GetCommandMention("connect4")} to start a new game.", ephemeral: true);
            }
            else
            {
                bool isPlayer1 = component.User.Id == game.Player1.Id;
                bool isPlayer2 = component.User.Id == game.Player2.Id;

                if (!isPlayer1 && !isPlayer2)
                {
                    await component.RespondAsync(text: $"❌ You **cannot** play this game because you are not a participant.", ephemeral: true);
                }
                else if (game.IsPlayer1Turn && !isPlayer1 || !game.IsPlayer1Turn && isPlayer1)
                {
                    await component.RespondAsync(text: $"❌ It is **not** your turn.", ephemeral: true);
                }
                else
                {
                    await DeferAsync();

                    if (int.TryParse(column, out int col))
                    {
                        // Find the lowest empty row in the selected column
                        for (int row = 5; row >= 0; row--)
                        {
                            if (game.Grid[col - 1, row] == 0)
                            {
                                game.Grid[col - 1, row] = isPlayer1 ? 1 : 2;
                                game.LastMoveColumn = col - 1;
                                game.LastMoveRow = row;

                                if (game.Player2.IsBot)
                                {
                                    await game.EndBotTurn(component);
                                }
                                else
                                {
                                    await game.EndTurn(component);
                                }

                                return;
                            }
                        }

                        // If no empty cells in the selected column
                        await component.RespondAsync(text: "❌ This column is full.", ephemeral: true);
                    }
                    else
                    {
                        // Handle the case where the chosen move is not valid or out of bounds
                        await component.RespondAsync(text: $"❌ Invalid move.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    }
                }
            }
        }

        [ComponentInteraction("acceptChallenge:*")]
        public async Task AcceptChallengeButtonHandler(string Id)
        {
            Challenge.Games.TryGetValue(Convert.ToUInt64(Id), out Games.Game challenge);
            if (challenge == null)
            {
                await Context.Interaction.RespondAsync(text: $"❌ This challenge no longer exists.", ephemeral: true);
            }
            else
            {
                if (Context.Interaction.User.Id != challenge.Player2.Id)
                {
                    await Context.Interaction.RespondAsync(text: $"❌ **Only** {challenge.Player2.Mention} can **accept** this challenge.", ephemeral: true);
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
                await Context.Interaction.RespondAsync(text: $"❌ This challenge no longer exists.", ephemeral: true);
            }
            else
            {
                if (Context.Interaction.User.Id != challenge.Player2.Id)
                {
                    await component.RespondAsync(text: $"❌ **Only** {challenge.Player2.Mention} can **decline** this challenge.", ephemeral: true);
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
                        Description = $"### ⚔️ {challenge.Player1.Mention} Challenged {challenge.Player2.Mention} to {challenge.Title}.\n{challenge.Player2.Mention} declined."
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
            var embed = new EmbedBuilder
            {
                Title = "<:bob:1161511472791293992> Review Bob on Top.GG",
                Color = Bot.theme,
                Footer = new EmbedFooterBuilder
                {
                    Text = "Top.gg is not associated with BobTheBot."
                }
            };

            var components = new ComponentBuilder().WithButton(label: "Review Bob!", style: ButtonStyle.Link, emote: new Emoji("✍️"), url: "https://top.gg/bot/705680059809398804#reviews");

            // Respond
            await RespondAsync(embed: embed.Build(), components: components.Build());
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("vote", "Vote for Bob on Top.gg")]
        public async Task Vote()
        {
            var embed = new EmbedBuilder
            {
                Title = "<:bob:1161511472791293992> Upvote Bob on Top.GG",
                Color = Bot.theme,
                Footer = new EmbedFooterBuilder
                {
                    Text = "Top.gg is not associated with BobTheBot."
                }
            };

            var components = new ComponentBuilder().WithButton(label: "Vote for Bob!", style: ButtonStyle.Link, emote: new Emoji("🗳️"), url: "https://top.gg/bot/705680059809398804/vote");

            // Respond
            await RespondAsync(embed: embed.Build(), components: components.Build());
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("quote-prompts", "Bob will give you all valid prompts for /random quote.")]
        public async Task QuotePrompts()
        {
            // Respond
            await RespondAsync(text: @$"
**Here are all valid prompts for {Help.GetCommandMention("random quote")}:**

**Categories:**

• **Life & Philosophy:**  
  `age`, `change`, `character`, `courage`, `failure`, `faith`, `gratitude`, `happiness`, `honor`, `inspirational`,  
  `life`, `love`, `pain`, `perseverance`, `philosophy`, `self`, `truth`, `virtue`, `wisdom`

• **Work & Success:**  
  `business`, `competition`, `ethics`, `genius`, `leadership`, `motivational`, `opportunity`, `success`,  
  `work`

• **Society & Politics:**  
  `conservative`, `education`, `freedom`, `politics`, `power-quotes`, `religion`, `tolerance`, `war`

• **Health & Wellness:**  
  `athletics`, `health`, `sports`, `wellness`

• **Science & Technology:**  
  `mathematics`, `science`, `technology`

• **Famous & Historical:**  
  `famous-quotes`, `history`, `proverb`

• **Emotions & Humor:**  
  `humor`, `humorous`, `sadness`, `stupidity`, `weakness`

• **Miscellaneous:**  
  `family`, `film`, `future`, `generosity`, `knowledge`, `nature`, `time`
");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("announce", "Bob will create a fancy embed announcement in the channel the command is used in.")]
        public async Task Announce([Summary("title", "The title of the announcement (the title of the embed).")][MinLength(1)][MaxLength(256)] string title, [Summary("description", "The anouncement (the description of the embed).")][MinLength(1)][MaxLength(4096)] string description, [Summary("color", "A color name (purple), or a valid hex code (#8D52FD) or valid RGB code (141, 82, 253).")] string color)
        {
            Color? finalColor = Colors.TryGetColor(color);

            // Check if Bob has permission to send messages in given channel
            ChannelPermissions permissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel);
            if (!Context.Interaction.IsDMInteraction && (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks))
            {
                await RespondAsync(text: $"❌ Bob is either missing permissions to view, send messages, *or* embed links in the channel <#{Context.Channel.Id}>\n- Try giving Bob the following pemrissions: `View Channel`, `Embed Links`, and `Send Messages`.", ephemeral: true);
            }
            else if (finalColor == null)
            {
                await RespondAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex and RGB codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
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
            var embed = new EmbedBuilder
            {
                Title = $"Bob's Info",
                Color = Bot.theme
            };

            embed.AddField(name: "📛 Username", value: $"{Bot.Client.CurrentUser.Username}", inline: true)
            .AddField(name: "🪪 ID", value: $"`{Bot.Client.CurrentUser.Id}`", inline: true)
            .AddField(name: ":calendar_spiral: Date Created", value: Timestamp.FromDateTimeOffset(Bot.Client.CurrentUser.CreatedAt, Timestamp.Formats.Detailed), inline: false)
            .AddField(name: "📈 Servers", value: $"`{Bot.Client.Guilds.Count:n0}`", inline: true)
            .AddField(name: "🏗️ Made With", value: "C#, .NET, PostgreSQL, Docker", inline: true)
            .AddField(name: "📡 Hosted With", value: "Railway", inline: true);

            var components = new ComponentBuilder();

            components.WithButton(label: "Website", emote: new Emoji("🌐"), style: ButtonStyle.Link, url: "https://bobthebot.net")
            .WithButton(label: "GitHub", emote: Emote.Parse("<:github:1236245156798402685>"), style: ButtonStyle.Link, url: "https://github.com/bob-el-bot/BobTheBot");

            await RespondAsync(embed: embed.Build(), components: components.Build());
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

            var embed = new EmbedBuilder
            {
                Title = $"What's New?",
                Description = @"### 🗒️ Creator's Notes
- We realize that subscription models suck, and now that Discord has an option for one-time purchases, we do too! Lifetime ✨ premium goes for ***only $4.99!*** (💜 we hope this makes your life better!)
- Added `/convert qr-code` to convert text or URLs to a QR code here in Discord!
- Added random selection of users if left unspecified in `/ship`. Shoutout x_star. for the idea!
- Revamped `/mastermind` to be more accurate to the original board game. It is now color-based, has three difficulty options, and two game modes. Shoutout intelligenceunknownsh for some of these ideas!
- Improved the `/connect4` AI to be more challenging by increasing search depth and adding in some randomness to the beginning of games. Shoutout intelligenceunknownsh for catching this!
- Improved `/ship` calculation to make the distribution more even and less biased towards 60-70%. Shoutout intelligenceunknownsh for catching this!
- Made `/preview color` and `/random color` faster and more memory efficient by using the WEBP format instead of PNG.
- Fixed bug where user's game stats were updated on the test bot.
- Fixed bug where `/quote new` and the message command `Quote` incorrectly handled quotes of 4096 characters which led to uncaught exceptions.
- Stay 📺 tuned for more awesome updates!",
                Color = Bot.theme
            };

            embed.AddField(name: "✨ Latest Update", value: commitMessage, inline: true)
            .AddField(name: ":calendar_spiral: Date", value: Timestamp.FromString(commitDate, Timestamp.Formats.Detailed), inline: true);

            var components = new ComponentBuilder();

            components.WithButton(label: "Future Plans", emote: new Emoji("🔮"), style: ButtonStyle.Link, url: "https://github.com/orgs/bob-el-bot/projects/4")
            .WithButton(label: "Blog", emote: new Emoji("📰"), style: ButtonStyle.Link, url: "https://bobthebot.net/blog.html");

            await RespondAsync(embed: embed.Build(), components: components.Build());
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("help", "Bob will share info about every command sorted by category.")]
        public async Task HelpCommand()
        {
            await DeferAsync();

            var embed = new EmbedBuilder
            {
                Title = "📖 All of My Commands.",
                Description = "Select a category to see info about relevant commands.",
                Color = Bot.theme
            };

            embed.AddField(name: "Categories", value: $"`{Help.CommandGroups.Length}`", inline: true)
            .AddField(name: "Commands", value: $"`{Help.GetCommandCount()}`", inline: true);

            await FollowupAsync(embed: embed.Build(), components: Help.GetComponents());
        }

        [ComponentInteraction("help")]
        public async Task HelpOptionsHandler()
        {
            await DeferAsync();

            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await component.ModifyOriginalResponseAsync(x => { x.Embed = Help.GetCategoryEmbed(int.Parse(component.Data.Values.FirstOrDefault())); });
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("confess", "Bob will send someone a message anonymously (saying inappropriate things will result in punishment)")]
        public async Task Confess(string message, SocketUser user, string signoff)
        {
            if (user.IsBot)
            {
                await RespondAsync("❌ Sorry, but you can't send messages to bots.", ephemeral: true);
                return;
            }

            if (ConfessFiltering.IsBlank(message))
            {
                await RespondAsync("❌ Sorry, but you can't send blank, or effectively blank, messages.\n- Try adding characters that are not blank or zero-width.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            try
            {
                using var context = new BobEntities();

                var dbUser = await context.GetUser(user.Id);

                FilterResult filterResult = new();

                // If user has confess filtering on, check for blacklisted words
                if (dbUser.ConfessFilteringOff == false)
                {
                    // Check for blacklisted words
                    filterResult = ConfessFiltering.ContainsBannedWords($"{message} {signoff}");
                    bool isUserBlacklisted = await BlackList.IsBlacklisted(Context.User.Id);

                    if (filterResult.BlacklistMatches.Count > 0)
                    {
                        Server dbServer = Context.Guild?.Id != null ? await context.GetServer(Context.Guild.Id) : null;

                        // If a Guild Install and confess filtering is on for the server, punish.
                        if (dbServer != null && dbServer.ConfessFilteringOff == false)
                        {
                            var bannedUser = await context.GetUserFromBlackList(Context.User.Id);
                            string reason = $"Sending a message with {Help.GetCommandMention("confess")} that contained: {ConfessFiltering.FormatBannedWords(filterResult.BlacklistMatches)}";

                            if (isUserBlacklisted)
                            {
                                bannedUser = await BlackList.StepBanUser(Context.User.Id, reason);
                                await FollowupAsync($"❌ This server has confess message filtering turned on and your message contains blacklisted words. You are **already banned** and so your punishment has **increased**.\n- You will be able to use {Help.GetCommandMention("confess")} again {Timestamp.FromDateTime((DateTime)bannedUser.Expiration, Timestamp.Formats.Relative)}.\n**Reason(s):**\n{bannedUser.Reason}\n- Use {Help.GetCommandMention("profile punishments")} to check your punishment status.\n- **Do not try to use this command with an offending word or your punishment will be increased.**", ephemeral: true);
                            }
                            else
                            {
                                bannedUser = await BlackList.BlackListUser(bannedUser, Context.User.Id, reason, BlackList.Punishment.FiveMinutes);
                                await FollowupAsync($"❌ This server has confess message filtering turned on and your message contains blacklisted words. You have been temporarily banned.\n- You will be able to use {Help.GetCommandMention("confess")} again {Timestamp.FromDateTime((DateTime)bannedUser.Expiration, Timestamp.Formats.Relative)}.\n**Reason(s):**\n{bannedUser.Reason}\n- Use {Help.GetCommandMention("profile punishments")} to check your punishment status.\n- **Do not try to use this command with an offending word or your punishment will be increased.**", ephemeral: true);
                            }

                            return;
                        }
                    }

                    if (isUserBlacklisted)
                    {
                        var bannedUser = await context.GetUserFromBlackList(Context.User.Id);
                        await FollowupAsync($"❌ This server has confess message filtering turned on. You are currently banned from using {Help.GetCommandMention("confess")}\n{bannedUser.FormatAsString()}\n- **Do not try to use this command with an offending word or your punishment will be increased.**", ephemeral: true);
                        return;
                    }
                }

                // If user has confessions off or filtering on and there are blacklisted words, do not send the message.
                if (dbUser.ConfessionsOff || filterResult.BlacklistMatches.Count > 0)
                {
                    await FollowupAsync($"❌ Bob could **not** DM {user.Mention}.\n- You could try again, but this *probably* means their DMs are closed which Bob cannot change.", ephemeral: true);
                    return;
                }

                string formattedMessage = $"{ConfessFiltering.notificationMessage}\n{message} - {signoff}";

                if (filterResult.WordsToCensor.Count > 0)
                {
                    formattedMessage = ConfessFiltering.MarkSpoilers(formattedMessage, filterResult.WordsToCensor);
                }

                if (formattedMessage.Length > 2000)
                {
                    await FollowupAsync($"❌ The message *cannot* be delivered because it contains **{formattedMessage.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **2000** characters.", ephemeral: true);
                    return;
                }

                var components = new ComponentBuilder()
                    .WithButton(label: "Disable Confessions", customId: $"disableConfessions:{user.Id}", style: ButtonStyle.Secondary, emote: Emoji.Parse("🚫"));
                ButtonBuilder reportMessageButton = new()
                {
                    Label = "Report Message",
                    Style = ButtonStyle.Secondary,
                    Emote = Emoji.Parse("⚠️")
                };

                var finalMessage = formattedMessage;

                if (ConfessFiltering.ContainsLink(formattedMessage))
                {
                    if (formattedMessage.Length + ConfessFiltering.linkWarningMessage.Length < 2000)
                    {
                        finalMessage += "\n" + ConfessFiltering.linkWarningMessage;
                    }
                }

                var sentMessage = await user.SendMessageAsync(text: finalMessage);
                reportMessageButton.CustomId = $"reportMessage:{sentMessage.Channel.Id}:{sentMessage.Id}";
                components.WithButton(reportMessageButton);

                await sentMessage.ModifyAsync(x => x.Components = components.Build());
                await FollowupAsync($"✉️ Sent!\n**Message:** {message} - {signoff}\n**To:** **{user.Mention}**", ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await FollowupAsync($"❌ Bob could **not** DM {user.Mention}.\n- You could try again, but this *probably* means their DMs are closed which Bob cannot change.", ephemeral: true);
            }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("premium", "Update your premium status or buy it!")]
        public async Task PremiumStatus()
        {
            await DeferAsync();

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            // If user has premium ensure DB is updated.
            bool isPremium = Premium.IsPremium(Context.Interaction.Entitlements);
            if (isPremium == false && Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await FollowupAsync(text: "✨ You can get premium below!\n💜 *Thanks so much!*", components: Premium.GetComponents());
            }
            else if (isPremium == false && Premium.IsValidPremium(user.PremiumExpiration))
            {
                await FollowupAsync(text: "✨ You already have premium!\n💜 *Thanks so much!*");
            }
            else
            {
                await foreach (var entitlementCollection in Client.GetEntitlementsAsync(userId: Context.User.Id))
                {
                    if (entitlementCollection.Any(x => x.SkuId == 1282452500913328180))
                    {
                        user.PremiumExpiration = DateTimeOffset.MaxValue;
                        await context.UpdateUser(user);
                    }
                }

                await FollowupAsync(text: "✨ Your premium status has been updated!\n**💜 Thanks so much** for your support and have fun with your new features!");
            }

            // Temporarily commented out due to Discord not properly handling entitlements on their end.

            // else
            // {
            //     await DeferAsync();

            //     if (isPremium)
            //     {


            //         // Get Expiration Date
            //         var entitlement = Context.Interaction.Entitlements.FirstOrDefault(x => x.SkuId == 1169107771673812992);
            //         var expirationDate = entitlement?.EndsAt;

            //         if (expirationDate == null && Context.Interaction.Entitlements.First(x => x.SkuId == 1282452500913328180) != null)
            //         {
            //             expirationDate = DateTimeOffset.MaxValue;
            //         }

            //         // Only write to DB if needed.
            //         if (user.PremiumExpiration != expirationDate && expirationDate != null)
            //         {
            //             Console.WriteLine($"User {Context.User.Id} has updated their premium expiration to {expirationDate} in the DB");
            //             user.PremiumExpiration = (DateTimeOffset) expirationDate;
            //             await context.UpdateUser(user);
            //         }
            //         else
            //         {
            //             Console.WriteLine($"User {Context.User.Id} has not updated their premium expiration in the DB (it is already {expirationDate})");
            //         }
            //     }

            //     // Respond
            //     await FollowupAsync(text: "✨ Your premium status has been updated!\n**💜 Thanks so much** for your support and have fun with your new features!");
            // }
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("support", "Sends an invite to Bob's support Server.")]
        public async Task Support()
        {
            var components = new ComponentBuilder();
            components.WithButton(label: "Join Bob's Server!", emote: new Emoji("🏰"), style: ButtonStyle.Link, url: "https://discord.gg/HvGMRZD8jQ");

            // Respond
            await RespondAsync(text: "Whether you're having issues with Bob, wanting to suggest a new idea, or maybe just wanna hang...", components: components.Build());
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("poll", "Bob will create a poll.")]
        public async Task Poll([Summary("prompt", "The question you are asking.")] string prompt, [Summary("option1", "an answer / response to your question")] string option1, [Summary("option2", "an answer / response to your question")] string option2, [Summary("option3", "an answer / response to your question")] string option3 = "", [Summary("option4", "an answer / response to your question")] string option4 = "")
        {
            var permissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel);

            // Check if Bob has permission to send messages in given channel
            if (!permissions.SendMessages || !permissions.ViewChannel || !permissions.EmbedLinks || !permissions.AddReactions)
            {
                await RespondAsync(text: $"❌ Bob is either missing permissions to view, send messages, add reactions, *or* embed links in the channel <#{Context.Channel.Id}>.\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`, `Add Reactions`, and `Embed Links`.", ephemeral: true);
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

                string[] possibleOptions = [option1, option2, option3, option4];
                string[] optionLabels = ["1️⃣", "2️⃣", "3️⃣", "4️⃣"];
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

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("ship", "Bob will determine how good of a couple two users would make")]
        public async Task Ship(SocketUser person1 = null, SocketUser person2 = null)
        {
            await DeferAsync();

            if (person1 == null)
            {
                await CachedUsers.AddGuildUsersAsync(Context); // Ensure the guild is cached
                person1 = CachedUsers.GetRandomMember(Context);
            }

            if (person2 == null)
            {
                await CachedUsers.AddGuildUsersAsync(Context); // Ensure the guild is cached
                person2 = CachedUsers.GetRandomMember(Context);
            }

            float matchPercent = 0;

            if (person1.Id != person2.Id)
            {
                // Ensure order consistency
                if (person1.Id > person2.Id)
                {
                    (person1, person2) = (person2, person1);
                }

                // Combine the snowflakes and usernames into a single string
                string combinedString = person1.Id.ToString() + person2.Id.ToString() + person1.Username + person2.Username;

                // 1. Hash the combined string (both IDs and usernames)
                int hash = combinedString.GetHashCode(); // Combined hash of both user IDs and usernames

                // 2. Normalize hash to 0-100 range
                float hashBasedVariance = Math.Abs(hash % 100);  // Use modulo to restrict hash within 0-99
                matchPercent = hashBasedVariance;  // Directly use the hash as the match percentage

                // 3. Adjust match percentage if needed (for example, ensure the match is always between 0 and 100)
                matchPercent = Math.Clamp(matchPercent, 0, 100);  // Ensure final result is within 0-100
            }

            // Determine Heart Level
            string heartLevel = HeartLevels.CalculateHeartLevel(matchPercent);

            // Embed
            var embed = new EmbedBuilder
            {
                Title = $"{person1.Username} ❤️ {person2.Username}",
                Color = new Color(15548997),
            };

            embed.AddField(name: $"Match of:", value: $"`{matchPercent}%`", inline: true)
                 .AddField(name: "Heart Level", value: heartLevel, inline: true);

            await FollowupAsync(embed: embed.Build());
        }
    }
}




