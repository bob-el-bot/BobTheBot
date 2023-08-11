using Discord.Interactions;
using System.Threading.Tasks;
using System;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;
using System.Net.Http;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Linq;
using ColorHelper;
using System.Diagnostics;

public class Commands : InteractionModuleBase<SocketInteractionContext>
{
    // GENERAL Stuff

    [EnabledInDm(false)]
    [SlashCommand("ping", "Bob will share his ping.")]
    public async Task Ping()
    {
        await RespondAsync(text: $"🏓 Pong! The client latency is **{Bot.Client.Latency}** ms.");
    }

    [EnabledInDm(true)]
    [SlashCommand("hi", "Say hi to Bob.")]
    public async Task Hi()
    {
        await RespondAsync(text: "👋 hi!");
    }

    [EnabledInDm(true)]
    [SlashCommand("hug", "Hug your friends! (up to 5 people in a group hug!)")]
    public async Task Hug(SocketUser person1, SocketUser person2 = null, SocketUser person3 = null, SocketUser person4 = null, SocketUser person5 = null)
    {
        string response = "";
        response += Context.User.Mention + " *hugs* " + person1.Mention;

        SocketUser[] people = { person2, person3, person4, person5 };

        for (int i = 0; i < people.Length; i++)
        {
            if (people[i] != null)
                response += ", " + people[i].Mention;
        }

        await RespondAsync(text: $"🤗🫂 {response}" + "!");
    }

    [EnabledInDm(true)]
    [SlashCommand("rock-paper-scissors", "Play a game of Rock Paper Scissors with Bob.")]
    public async Task RPS()
    {
        SelectMenuBuilder RPSOptions = new SelectMenuBuilder().WithPlaceholder("Select an option").WithCustomId("RPSOptions").WithMaxValues(1).WithMinValues(1).AddOption("🪨 Rock", "0").AddOption("📃 Paper", "1").AddOption("✂️ Scissors", "2");
        var builder = new ComponentBuilder().WithSelectMenu(RPSOptions);
        await RespondAsync(text: "*Game On!* What do you choose?", components: builder.Build());
    }

    [ComponentInteraction("RPSOptions")]
    public async Task RPSOptionsHandler()
    {
        SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;
        string result = RockPaperScissors.PlayRPS(string.Join("", component.Data.Values));
        await component.UpdateAsync(x => { x.Content = result + component.User.Mention; x.Components = null; });
    }

    [EnabledInDm(false)]
    [Group("master-mind", "All commands relevant to game Master Mind.")]
    public class MasterMind : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(false)]
        [SlashCommand("new-game", "Start a game of Master Mind (rules will be sent upon use of this command).")]
        public async Task NewGame()
        {
            if (MasterMindGeneral.currentGames != null && MasterMindGeneral.currentGames.Find(game => game.id == Context.Channel.Id) != null)
            {
                await RespondAsync(text: "❌ Only one game of Master Mind can be played per channel at a time.", ephemeral: true);
            }
            else // Display Rules / Initial Embed
            {
                MasterMindGame game = new MasterMindGame();
                MasterMindGeneral.currentGames.Add(game);
                game.id = Context.Channel.Id;

                var embed = new Discord.EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new Discord.Color(9261821),
                };
                embed.AddField(name: "How to Play.", value: "The goal of the game is to guess the correct randomly generated code. **Each code is 4 digits** long where each digit is an integer from **1-9**. Use the command `/master-mind guess` to make your guess. Be warned you only have **8 tries**!");

                // Begin Button
                var button = new Discord.ButtonBuilder
                {
                    Label = "Begin Game!",
                    Style = Discord.ButtonStyle.Success,
                    CustomId = "begin"
                };

                var builder = new ComponentBuilder().WithButton(button);

                await RespondAsync(embed: embed.Build(), components: builder.Build());
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("guess", "make a guess in an existing game of Master Mind")]
        public async Task Guess([Summary("guess", "Type a 4 digit guess as to what the answer is.")] string guess)
        {
            var game = MasterMindGeneral.currentGames.Find(game => game.id == Context.Channel.Id);
            if (game == null)
                await RespondAsync(text: "❌ There is currently not a game of Master Mind in this channel. To make one use `/master-mind new-game`", ephemeral: true);
            else if (MasterMindGeneral.currentGames.Count > 0 && game.isStarted == false)
                await RespondAsync(text: "❌ Press \"Begin Game!\" to start guessing.", ephemeral: true);
            else if (guess.Length != 4)
                await RespondAsync(text: "❌ Your should have **exactly 4 digits** (No guesses were expended).", ephemeral: true);
            else
            {
                // Set Values
                game.guessesLeft -= 1;

                // Get Result
                string result = MasterMindGeneral.GetResult(guess, game.key);

                // Ready Embed
                var embed = new Discord.EmbedBuilder
                {
                    Title = "🧠 Master Mind",
                    Color = new Discord.Color(9261821),
                };

                if (result == "🟩🟩🟩🟩") // it is solved
                {
                    embed.Title += " (solved)";
                    embed.Description = MasterMindGeneral.GetCongrats();
                    embed.AddField(name: "Answer:", value: $"`{game.key}`", inline: true).AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true);
                    await game.message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
                    MasterMindGeneral.currentGames.Remove(game);
                }
                else if (game.guessesLeft <= 0) // lose game
                {
                    embed.Title += " (lost)";
                    embed.Description = "You have lost, but don't be sad you can just start a new game with `/master-mind new-game`";
                    embed.AddField(name: "Answer:", value: $"`{game.key}`");
                    await game.message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
                    MasterMindGeneral.currentGames.Remove(game);
                }
                else
                {
                    embed.AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true).AddField(name: "Last Guess:", value: $"`{guess}`", inline: true).AddField(name: "Result:", value: $"{result}");
                    await game.message.ModifyAsync(x => { x.Embed = embed.Build(); });
                }

                // Respond
                await RespondAsync(text: "🎯 Guess Made.", ephemeral: true);
            }
        }
    }

    [ComponentInteraction("begin")]
    public async Task MasterMindBeginButtonHandler()
    {
        await DeferAsync();
        // Get Game
        var game = MasterMindGeneral.currentGames.Find(game => game.id == Context.Interaction.ChannelId);

        // Set message
        var component = (SocketMessageComponent)Context.Interaction;
        game.message = component.Message;

        // Set startUser
        game.startUser = component.Message.Author.Username;

        // Initialize Key
        game.key = MasterMindGeneral.CreateKey();

        // Initialize Embed  
        var embed = new Discord.EmbedBuilder
        {
            Title = "🧠 Master Mind",
            Color = new Discord.Color(9261821),
        };
        embed.AddField(name: "Guesses Left:", value: $"`{game.guessesLeft}`", inline: true).AddField(name: "Last Guess:", value: "use `/master-mind guess`", inline: true).AddField(name: "Result:", value: "use `/master-mind guess`");

        // Forfeit Button
        var button = new Discord.ButtonBuilder
        {
            Label = "Forfeit",
            Style = Discord.ButtonStyle.Danger,
            CustomId = "quit"
        };
        var builder = new ComponentBuilder().WithButton(button);

        // Begin Game
        game.isStarted = true;

        // Edit Message For Beginning of Game.
        await component.Message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = builder.Build(); });
    }

    [ComponentInteraction("quit")]
    public async Task MasterMindQuitButtonHandler()
    {
        await DeferAsync();
        // Get Game
        var game = MasterMindGeneral.currentGames.Find(game => game.id == Context.Interaction.Channel.Id);

        var embed = new Discord.EmbedBuilder
        {
            Title = "🧠 Master Mind",
            Color = new Discord.Color(9261821),
            Description = "This was certainly difficult, try again with `/master-mind new-game`",
        };

        embed.Title += " (forfeited)";
        embed.AddField(name: "Answer:", value: $"`{game.key}`");
        await game.message.ModifyAsync(x => { x.Embed = embed.Build(); x.Components = null; });
        MasterMindGeneral.currentGames.Remove(game);
    }

    [EnabledInDm(true)]
    [SlashCommand("review", "Leave a review for Bob on Top.gg")]
    public async Task Review()
    {
        // Respond
        await RespondAsync(text: "📝 If you're enjoying BobTheBot, please consider leaving a review on Top.gg!\n[review here](https://top.gg/bot/705680059809398804#reviews)");
    }

    [EnabledInDm(true)]
    [SlashCommand("vote", "Vote for Bob on Top.gg")]
    public async Task Vote()
    {
        // Respond
        await RespondAsync(text: "**Top.gg is not associated with BobTheBot and so ads cannot be removed by Bob's creators.**\n\nVote for Bob!\n[vote here](https://top.gg/bot/705680059809398804/vote)");
    }

    [EnabledInDm(false)]
    [SlashCommand("quote-prompts", "Bob will give you all valid prompts for /quote.")]
    public async Task QuotePrompts()
    {
        // Respond
        await RespondAsync(text: $"Here are all valid prompts for `/quote`:\nage, athletics, business, change, character, competition, conservative, courage, education, ethics, failure, faith, family, famous-quotes, film, freedom, future, generosity, genius, gratitude, happiness, health, history, honor, humor, humorous, inspirational, knowledge, leadership, life, love, mathematics, motivational, nature, oppurtunity, pain, perseverance, philosphy, politics, power-quotes, proverb, religion, sadness, science, self, sports, stupidity, success, technology, time, tolerance, truth, virtue, war, weakness, wellness, wisdom, work");
    }

    [EnabledInDm(true)]
    [SlashCommand("info", "Learn about Bob.")]
    public async Task Info()
    {
        var createdAt = Bot.Client.CurrentUser.CreatedAt.ToUnixTimeSeconds();

        var embed = new Discord.EmbedBuilder
        {
            Title = $"Bob's Info",
            Color = new Discord.Color(9261821),
        };

        embed.AddField(name: "📛 Username", value: $"{Bot.Client.CurrentUser.Username}", inline: true).AddField(name: "🪪 ID", value: $"`{Bot.Client.CurrentUser.Id}`", inline: true).AddField(name: "📈 Server Count", value: $"`{Bot.Client.Guilds.Count:n0}`").AddField(name: ":calendar_spiral: Date Created", value: $"<t:{createdAt}:f>", inline: true).AddField(name: "🌐 Website", value: "[bobthebot.net](https://bobthebot.net)").AddField(name: "⚡ Github Repository", value: "[github.com/bob-el-bot/BobTheBot](https://github.com/bob-el-bot/BobTheBot)").AddField(name: "🏗️ Made With", value: "C#, .NET", inline: true).AddField(name: "📡 Hosted With", value: "Raspberry PI 4", inline: true);

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(false)]
    [SlashCommand("new", "See the newest changes to Bob, and find out what's next.")]
    public async Task New()
    {
        // Formulate Request
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.github.com/repos/bob-el-bot/BobTheBot/commits/main");

        var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/bob-el-bot/BobTheBot)");
        var acceptValue = new MediaTypeWithQualityHeaderValue("application/json");
        request.Headers.UserAgent.Add(productValue);
        request.Headers.UserAgent.Add(commentValue);
        request.Headers.Accept.Add(acceptValue);

        // Send Request (Get the Commit Data)
        var resp = await httpClient.SendAsync(request);
        // Read In Content
        var content = await resp.Content.ReadAsStringAsync();
        // Parse Content
        var jsonData = JsonNode.Parse(content).AsObject();
        var commit = JsonNode.Parse(jsonData["commit"].ToString()).AsObject();
        var commitMessage = commit["message"].ToString();
        var commitAuthor = JsonNode.Parse(commit["author"].ToString()).AsObject();
        var commitDate = commitAuthor["date"].ToString();
        // 2023-07-29T03:50:42Z -> unix epoch time
        var commitDateID = DateTimeOffset.Parse(commitDate).ToUnixTimeSeconds();

        var embed = new Discord.EmbedBuilder
        {
            Title = $"What's New?",
            Color = new Discord.Color(9261821),
        };

        embed.AddField(name: "🗒️ Creators Notes", value: "- Bob has a place on the 🌐 web! [bobthebot.net](https://bobthebot.net)\n- Stay 📺 tuned for some awesome updates!\n- Bob has a new 😎 style, specifically `#8D52FD`", inline: false).AddField(name: "✨ Latest Update", value: commitMessage, inline: true).AddField(name: ":calendar_spiral: Date", value: $"<t:{commitDateID}:f>", inline: true).AddField(name: "🔮 See What's In the Works", value: "[Road Map](https://github.com/users/Quantam-Studios/projects/3/views/1)");

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(true)]
    [Group("random", "All random (RNG) commands.")]
    public class RandomCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public Random random = new Random();

        [EnabledInDm(true)]
        [SlashCommand("dice-roll", "Bob will roll a die with the side amount specified.")]
        public async Task DiceRoll(int sides)
        {
            if (sides <= 0)
            {
                await RespondAsync(text: $"🌌 The die formed a *rift* in our dimension! **Maybe** try using a number **greater than 0**");
            }
            else
            {
                int randInt = random.Next(1, (sides + 1));
                await RespondAsync(text: $"🎲 The {sides} sided die landed on **{randInt}**");
            }
        }

        [EnabledInDm(true)]
        [SlashCommand("date", "Bob will will pick a random date.")]
        public async Task RandomDate([Summary("earliestYear", "A year that is as early as you want the date to occur in.")] int earliestYear, [Summary("latestYear", "A year that is as late as you want the date to occur in.")] int latestYear)
        {
            if (latestYear < 0 || earliestYear < 0)
            {
                await RespondAsync(text: $"🌌 *Whoa!* a *rift* in our dimension! **Maybe** try a year that is **atleast 0**.");
            }
            else if (latestYear.ToString().Length > 8 || earliestYear.ToString().Length > 8)
            {
                await RespondAsync(text: $"❌ Please, choose a year that is **8 digits or less**.");
            }
            else if (earliestYear > latestYear)
            {
                await RespondAsync(text: $"❌ Please, make the *earliest year* **smaller** than the *latest year*.");
            }
            else
            {
                // Pick Year
                int year = random.Next(earliestYear, latestYear + 1);

                // Pick Month
                (string name, int days)[] months = { ("January", 31), ("February", 28), ("March", 31), ("April", 30), ("May", 31), ("June", 30), ("July", 31), ("August", 31), ("September", 30), ("October", 31), ("November", 30), ("December", 31) };
                (string name, int days) month = months[random.Next(0, months.Length)];

                // Pick Day
                int day = random.Next(1, month.days + 1);

                await RespondAsync(text: $":calendar_spiral: {month.name} {day}, {year}");
            }
        }

        [EnabledInDm(true)]
        [SlashCommand("8ball", "Bob will shake a magic 8 ball in response to a question.")]
        public async Task Magic8Ball(string prompt)
        {
            // Possible Answers
            string[] results = { "'no'", "'yes'", "'maybe'", "'ask again'", "'probably not'", "'affirmative'", "'it is certain'", "'very doubtful'", "'regretfully.. yes'", "'try again later...'" };

            // Get Random Result
            string result = results[random.Next(0, results.Length)];

            // Respond
            await RespondAsync(text: $"🎱 **{result}** in response to {prompt}");
        }

        [EnabledInDm(true)]
        [SlashCommand("choose", "Can't make up your mind? Bob can for you!")]
        public async Task Pick(string option1, string option2, string option3 = "", string option4 = "", string option5 = "")
        {
            List<string> choices = new List<string>();
            choices.Add(option1);
            choices.Add(option2);
            Choose.TestAdd(option3, choices);
            Choose.TestAdd(option4, choices);
            Choose.TestAdd(option5, choices);

            string choice = choices[random.Next(0, choices.Count)];

            await RespondAsync(text: "🤔 " + Choose.GetRandomDecisionText() + $"**{choice}**");
        }

        [EnabledInDm(true)]
        [SlashCommand("color", "Bob will choose a random color.")]
        public async Task Color()
        {
            var hex = String.Format("{0:X6}", random.Next(0x1000000));
            CMYK cmyk = ColorConverter.HexToCmyk(new HEX(hex));
            HSL hsl = ColorConverter.HexToHsl(new HEX(hex));
            HSV hsv = ColorConverter.HexToHsv(new HEX(hex));
            RGB rgb = ColorConverter.HexToRgb(new HEX(hex));
            Discord.Color displayColor = new Discord.Color(Convert.ToUInt32(hex, 16));

            var embed = new Discord.EmbedBuilder { };
            embed.AddField("Hex", "`" + hex + "`")
            .AddField("RGB", $"`R: {rgb.R}, G: {rgb.G}, B: {rgb.B}`")
            .AddField("CMYK", $"`C: {cmyk.C}, M: {cmyk.M}, Y: {cmyk.Y}, K: {cmyk.K}`")
            .AddField("HSL", $"`H: {hsl.H}, S: {hsl.S}, L: {hsl.L}`")
            .AddField("HSV", $"`H: {hsv.H}, S: {hsv.S}, V: {hsv.V}`")
            .WithColor(displayColor);

            await RespondAsync(embed: embed.Build());
        }

        [EnabledInDm(false)]
        [SlashCommand("quote", "Bob will tell you a quote.")]
        public async Task Quote([Summary("prompt", "use /quote-prompts to see all valid prompts")] string prompt = "")
        {
            // Formulate Request
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api.quotable.io/quotes/random?tags={prompt}");

            var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/bob-el-bot/BobTheBot)");
            var acceptValue = new MediaTypeWithQualityHeaderValue("application/json");
            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // Send Request (Get The Quote)
            var resp = await httpClient.SendAsync(request);
            // Read In Content
            var content = await resp.Content.ReadAsStringAsync();

            if (content != "[]") // no quotes match the prompt
            {
                // Parse Content
                var formattedContent = content.Substring(1, content.Length - 2);
                var jsonData = JsonNode.Parse(formattedContent).AsObject();
                var quote = jsonData["content"].ToString();
                var author = jsonData["author"].ToString();
                // Respond
                await RespondAsync(text: $"✍️ {quote} - *{author}*");
            }
            else
            {
                // Respond
                await RespondAsync(text: $"Sorry, but no quotes could be found for the prompt: {prompt} \nTry a different prompt, and make sure you spelled everything correctly.\nYou can also use `/quoteprompts` to see all valid prompts.");
            }
        }

        [EnabledInDm(true)]
        [SlashCommand("coin-toss", "Bob will flip a coin")]
        public async Task CoinToss()
        {
            var random = new Random();
            int randInt = random.Next(0, 2);
            string result = "";
            if (randInt == 1)
                result = "heads";
            else
                result = "tails";
            await RespondAsync(text: $"🪙 The coin landed **" + result + "**!");
        }

        [EnabledInDm(false)]
        [SlashCommand("fact", "Bob will provide you with an outrageous fact.")]
        public async Task RandomFacts()
        {
            // Formulate Request
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://uselessfacts.jsph.pl/api/v2/facts/random?language=en");

            var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/bob-el-bot/BobTheBot)");
            var acceptValue = new MediaTypeWithQualityHeaderValue("application/json");
            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // Send Request (Get the fact)
            var resp = await httpClient.SendAsync(request);

            // Read In Content
            var content = await resp.Content.ReadAsStringAsync();
            // Parse Content
            var jsonData = JsonNode.Parse(content).AsObject();
            var fact = jsonData["text"].ToString();

            string factFormatted = "";

            for (int i = 0; i < fact.Length; i++)
            {
                if (fact[i] == '`')
                    factFormatted += "\\";
                factFormatted += fact[i];
            }

            await RespondAsync(text: $"🤓 {factFormatted}");
        }

        [EnabledInDm(false)]
        [SlashCommand("dog", "Bob will find you a cute doggo image!")]
        public async Task RandomDog()
        {
            // Formulate Request
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://random.dog/woof.json");

            var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/bob-el-bot/BobTheBot)");
            var acceptValue = new MediaTypeWithQualityHeaderValue("application/json");
            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // Send Request (Get the dog image)
            var resp = await httpClient.SendAsync(request);

            // Read In Content
            var content = await resp.Content.ReadAsStringAsync();
            // Parse Content
            var jsonData = JsonNode.Parse(content).AsObject();
            var image = jsonData["url"].ToString();

            string[] dogEmojis = { "🐕", "🐶", "🐕‍🦺", "🐩" };

            await RespondAsync(text: $"{dogEmojis[random.Next(0, dogEmojis.Length)]}[dog]({image})");
        }

        [EnabledInDm(false)]
        [SlashCommand("advice", "Bob will provide you with random advice.")]
        public async Task RandomAdvice()
        {
            // Formulate Request
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.adviceslip.com/advice");

            var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/bob-el-bot/BobTheBot)");
            var acceptValue = new MediaTypeWithQualityHeaderValue("application/json");
            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // Send Request (Get the advice)
            var resp = await httpClient.SendAsync(request);

            // Read In Content
            var content = await resp.Content.ReadAsStringAsync();
            // Parse Content
            var jsonData = JsonNode.Parse(content).AsObject();
            var slip = JsonNode.Parse(jsonData["slip"].ToString()).AsObject();
            var advice = slip["advice"].ToString();

            // Respond
            await RespondAsync(text: $"🦉 *{advice}*");
        }

        [EnabledInDm(false)]
        [SlashCommand("dad-joke", "Bob will tell you a dad joke.")]
        public async Task DadJoke()
        {
            // Formulate Request
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://icanhazdadjoke.com");

            var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/bob-el-bot/BobTheBot)");
            var acceptValue = new MediaTypeWithQualityHeaderValue("text/plain");
            request.Headers.UserAgent.Add(productValue);
            request.Headers.UserAgent.Add(commentValue);
            request.Headers.Accept.Add(acceptValue);

            // Send Request (Get the joke)
            var resp = await httpClient.SendAsync(request);
            // Parse Content
            var content = await resp.Content.ReadAsStringAsync();

            // Respond
            await RespondAsync(text: $"😉  *{content}*");
        }
    }

    [EnabledInDm(false)]
    [SlashCommand("help", "Bob will DM you all relevant information for every command.")]
    public async Task help()
    {
        var embed = new Discord.EmbedBuilder
        {
            Title = $"📖 Here is a list of all of my commands.",
            Color = new Discord.Color(9261821),
        };
        embed.AddField(name: "🎲 Randomly Generated (RNG):", value: "- `/random color` Get a color with Hex, CMYK, HSL, HSV and RGB codes.\n\n- `/random dice-roll [sides]` Roll a die with a specified # of sides.\n\n- `/random coin-toss` Flip a coin.\n\n- `/random quote [prompt]` Get a random quote.\n  - `[prompt]`choices: This is optional, use `/quote-prompts` to view all valid prompts.\n\n- `/random dad-joke` Get a random dad joke.\n\n- `/random fact` Get an outrageous fact.\n\n- `/random 8ball [prompt]` Get an 8 ball response to a prompt.\n\n- `/random dog` Get a random picture of a dog.\n\n- `/random date [earliestYear] [latestYear]` Get a random date between the inputed years.\n\n- `/random advice` Get a random piece of advice.\n\n- `/random choose [option]*5` Bob will pick from the options provided.")
        .AddField(name: "🎮 Games:", value: "- `/rock-paper-scissors` Play Bob in a game of rock paper scissors.\n\n- `/master-mind new-game` Play a game of Master Mind, the rules will shared upon usage.\n\n- `/master-mind guess` Make a guess in a game of Master Mind.")
        .AddField(name: "✨ Other:", value: "- `/fonts [text] [font]` Change your text to a different font.\n  - `[font]` choices: 𝖒𝖊𝖉𝖎𝖊𝖛𝖆𝖑, 𝓯𝓪𝓷𝓬𝔂, 𝕠𝕦𝕥𝕝𝕚𝕟𝕖𝕕, ɟןıddǝp, s̷l̷̷a̷s̷h̷e̷d̷, and 🄱🄾🅇🄴🄳.\n\n- `/encrypt [message] [cipher]` Change text into a cipher.\n    - `[cipher]` choices: Caesar, A1Z26, Atbash, Morse Code\n\n- `/confess [message] [user] [signoff]` Have Bob DM a user a message.\n\n- `/poll [prompt] [option]*4` Create a poll.\n  - `[option]*4` usage: You must provide 2-4 options. These are essentially the poll's choices.\n\n- `/ship [user]*2` See how good of a match 2 users are.\n\n- `/hug [user]*5` Show your friends some love with a hug.")
        .AddField(name: "🗄️ Informational / Help:", value: "- `/new` See the latest updates to Bob.\n\n- `/quote-prompts` See all valid prompts for `/quote`.\n\n- `/ping` Find the client's latency.\n\n- `/info` Learn about Bob.\n\n- `/suggest` Join Bob's official server, and share you ideas!");

        await Context.User.SendMessageAsync(embed: embed.Build());
        await RespondAsync(text: $"📪 Check your DMs.", ephemeral: true);
    }

    [EnabledInDm(false)]
    [SlashCommand("confess", "Bob will send someone a message anonymously")]
    public async Task confess(string message, SocketUser user, string signoff)
    {
        if (user.IsBot)
        {
            await RespondAsync(text: "❌ Sorry, but no sending messages to bots.", ephemeral: true);
        }
        else
        {
            await user.SendMessageAsync($"{message} - {signoff}");
            await RespondAsync(text: $"✉️ Your message has been sent!\nMessage: **{message} - {signoff}** was sent to **{user.Username}**", ephemeral: true);
        }
    }

    [EnabledInDm(true)]
    [SlashCommand("suggest", "Invites to Bob's Official Discord server where you can suggest ideas.")]
    public async Task Suggest()
    {
        // Respond
        await RespondAsync(text: "🏰 Have an idea for a command? Share it on the official server for Bob The Bot.\nhttps://discord.gg/HvGMRZD8jQ");
    }

    [EnabledInDm(false)]
    [Group("quote", "All quoting commands.")]
    public class QuoteCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [EnabledInDm(false)]
        [SlashCommand("new", "Create a quote.")]
        public async Task New([Summary("quote", "The text you want quoted. Quotation marks (\") will be added.")] string quote, [Summary("user", "The user who the quote belongs to.")] SocketUser user, [Summary("tag1", "A tag for sorting quotes later on.")] string tag1 = "", [Summary("tag2", "A tag for sorting quotes later on.")] string tag2 = "", [Summary("tag3", "A tag for sorting quotes later on.")] string tag3 = "")
        {
            // Date
            var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Fomrat Quote
            string formattedQuote = quote;
            if (quote[0] != '"' && quote[quote.Length - 1] != '"')
            {
                formattedQuote = "\"" + quote + "\"";
            }

            // Create embed
            var embed = new Discord.EmbedBuilder
            {
                Title = $"{formattedQuote}",
                Color = new Discord.Color(2895667),
                Description = $"-{user.Mention}, <t:{dateTime}:R>"
            };

            // Footer
            string footerText = "";
            if (tag1 != "" || tag2 != "" || tag3 != "")
            {
                footerText += "Tag(s): ";
                string[] tags = { tag1, tag2, tag3 };
                for (int index = 0; index < tags.Length; index++)
                {
                    if (tags[index] != "")
                    {
                        footerText += tags[index];
                        if (index < tags.Length - 1)
                            footerText += ", ";
                    }
                }
                footerText += " | ";
            }
            footerText += $"Quoted by {Context.User.GlobalName}";
            embed.WithFooter(footer => footer.Text = footerText);

            // Respond
            await RespondAsync(text: $"🖊️ The quote: **{formattedQuote}**\n-{user.Mention}", ephemeral: true);

            // Send quote in quotes channel of server
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [EnabledInDm(false)]
        [SlashCommand("channel", "Configure /quote channel.")]
        public async Task Settings([Summary("channel", "The quotes channel for the server.")] SocketChannel channel)
        {
            // Check permissions
            if (!Context.Guild.GetUser(Context.User.Id).GuildPermissions.ManageChannels)
                await RespondAsync(text: "❌ Ask an admin or mod to configure this for you.\n- Permission(s) needed: **Manage Channels**\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            // Check if Bob has permission to send messages in given channel
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.ViewChannel) 
                await RespondAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{channel.Id}>\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            else
            {
                // Check if Bob has a record for this server in DB.
                await RespondAsync(text: $"✅ <#{channel.Id}> is now the quote channel for the server.", ephemeral: true);
            }
        }
    }

    [EnabledInDm(false)]
    [SlashCommand("poll", "Bob will create a poll.")]
    public async Task Poll([Summary("prompt", "The question you are asking.")] string prompt, [Summary("option1", "an answer / response to your question")] string option1, [Summary("option2", "an answer / response to your question")] string option2, [Summary("option3", "an answer / response to your question")] string option3 = "", [Summary("option4", "an answer / response to your question")] string option4 = "")
    {
        // Setup base data
        string footerText = Context.User.Username + " created this poll.";
        string instructions = "React with the corresponding number to cast your vote.";

        // Prepare color
        Discord.Color displayColor = new Discord.Color(9261821);

        // Embed
        var embed = new Discord.EmbedBuilder
        {
            Title = "📊 " + prompt,
            Description = instructions,
            Color = displayColor,
        };

        // Embed Setup
        embed.WithFooter(footer => footer.Text = footerText);

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

        await RespondAsync("Your poll has been made.", ephemeral: true);

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

    [EnabledInDm(true)]
    [SlashCommand("fonts", "Bob will type your text in a font of your choice")]
    public async Task Fonts([Summary("text", "the text you want converted. NOTE: only the alphabet is converted.")] string text, FontConversion.FontTypes font)
    {
        string finalText = "";

        switch (font)
        {
            case FontConversion.FontTypes.fancy:
                finalText = FontConversion.Fancy(text);
                break;
            case FontConversion.FontTypes.slashed:
                finalText = FontConversion.Slashed(text);
                break;
            case FontConversion.FontTypes.outlined:
                finalText = FontConversion.Outlined(text);
                break;
            case FontConversion.FontTypes.flipped:
                finalText = FontConversion.Flipped(text);
                break;
            case FontConversion.FontTypes.boxed:
                finalText = FontConversion.Boxed(text);
                break;
            case FontConversion.FontTypes.medieval:
                finalText = FontConversion.Medieval(text);
                break;
        }

        await RespondAsync(finalText);
    }

    [EnabledInDm(false)]
    [SlashCommand("ship", "Bob will determine how good of a couple two users would make")]
    public async Task Ship(SocketUser person1, SocketUser person2)
    {
        // Prepare for calculations
        string id1 = person1.Id.ToString();
        string name1 = person1.Username;
        int[] id1MakeUp = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        string id2 = person2.Id.ToString();
        string name2 = person2.Username;
        int[] id2MakeUp = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // determine amount of each digit
        for (int i = 0; i < 18; i++)
        {
            id1MakeUp[Int32.Parse($"{id1[i]}")] += 1;
            id2MakeUp[Int32.Parse($"{id2[i]}")] += 1;
        }

        // determine difference in name length
        float nameLengthDifference = Math.Abs(name1.Length - name2.Length);
        float[] specialDifference = { 1f, 2f, 3f, 5f, 8f, 13f, 21f };
        if (specialDifference.ToList().Contains(nameLengthDifference))
            nameLengthDifference += 20;
        nameLengthDifference *= 0.6f;

        // determine difference in names
        int nameDifference = 0;
        if (name1[0] != name2[0])
        {
            nameDifference += 10;
            if (name1[1] != name2[1])
                nameDifference += 20;
        }

        if (name1[name1.Length - 1] != name2[name2.Length - 1])
            nameDifference += 10;

        // determine difference between digits
        float matchDifference = 0;
        for (int i = 0; i < 9; i++)
        {
            matchDifference += MathF.Abs(id1MakeUp[i] - id2MakeUp[i]);
        }

        // calculate perecentage of similarity.
        float matchPercent = ((matchDifference + nameLengthDifference + nameDifference) / 135) * 100;
        if (matchPercent > 100)
            matchPercent = 100;

        // Determine Heart Level
        string heartLevel = HeartLevels.CalculateHeartLevel(matchPercent);

        // Embed
        var embed = new Discord.EmbedBuilder
        {
            Title = $"{person1.Username} ❤️ {person2.Username}",
            Color = new Discord.Color(15548997),
        };

        embed.AddField(name: $"Match of:", value: $"`{matchPercent}%`", inline: true).AddField(name: "Heart Level", value: heartLevel, inline: true);

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(true)]
    [SlashCommand("encrypt", "Bob will encrypt your message with a cipher of your choice.")]
    public async Task Encrypt([Summary("message", "the text you want to encrypt")] string message, Encryption.CipherTypes cipher)
    {
        string finalText = "";

        switch (cipher)
        {
            case Encryption.CipherTypes.Atbash:
                finalText = Encryption.Atbash(message);
                break;
            case Encryption.CipherTypes.Caesar:
                finalText = Encryption.Caesar(message);
                break;
            case Encryption.CipherTypes.A1Z26:
                finalText = Encryption.A1Z26(message);
                break;
            case Encryption.CipherTypes.Morse:
                finalText = Encryption.Morse(message);
                break;
        }

        await RespondAsync($"{finalText}", ephemeral: true);
    }
}




