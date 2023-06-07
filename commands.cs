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

public class Commands : InteractionModuleBase<SocketInteractionContext>
{
    // GENERAL Stuff
    public Random random = new Random();

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

    [EnabledInDm(true)]
    [SlashCommand("rock-paper-scissors", "Play a game of Rock Paper Scissors with Bob.")]
    public async Task RPS()
    {
        var builder = new ComponentBuilder().WithSelectMenu(RockPaperScissors.RPSOptions);
        Bot.Client.SelectMenuExecuted += RockPaperScissors.RPSSelectMenuHandler;
        await RespondAsync(text: "*Game On!* What do you choose?", components: builder.Build());
    }

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
    [SlashCommand("8ball", "Bob will shake a magic 8 ball in repsonse to a question.")]
    public async Task Magic8Ball(string prompt)
    {
        // Possible Answers
        string[] results = { "'no'", "'yes'", "'maybe'", "'ask again'", "'probably not'", "'affirmative'", "'it is certain'", "'very doubtful'" };

        // Get Random Result
        string result = results[random.Next(0, results.Length)];

        // Respond
        await RespondAsync(text: $"🎱 The magic 8 ball says **{result}** in response to {prompt}");
    }

    [EnabledInDm(true)]
    [SlashCommand("review", "Leave a review for Bob on Top.gg")]
    public async Task Review()
    {
        // Respond
        await RespondAsync(text: "📝 If you're enjoying BobTheBot, please consider leaving a review on Top.gg!\nhttps://top.gg/bot/705680059809398804#reviews");
    }

    [EnabledInDm(true)]
    [SlashCommand("vote", "Vote for Bob on Top.gg")]
    public async Task Vote()
    {
        // Respond
        await RespondAsync(text: "**Top.gg is not associated with BobTheBot and so ads cannot be removed by Bob's creators.**\n\nVote for Bob!\nhttps://top.gg/bot/705680059809398804/vote");
    }

    [EnabledInDm(false)]
    [SlashCommand("random-fact", "Bob will provide you with an outrageous fact.")]
    public async Task RandomFacts()
    {
        // Formulate Request
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://uselessfacts.jsph.pl/api/v2/facts/random?language=en");

        var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/Quantam-Studios/BobTheBot)");
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

        await RespondAsync(text: $"🤓 {fact}\n\n (Source: *Trust Me Bro*.)");
    }

    [EnabledInDm(false)]
    [SlashCommand("dad-joke", "Bob will tell you a dad joke.")]
    public async Task DadJoke()
    {
        // Formulate Request
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://icanhazdadjoke.com");

        var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/Quantam-Studios/BobTheBot)");
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

    [EnabledInDm(false)]
    [SlashCommand("quote", "Bob will tell you a quote.")]
    public async Task Quote(string prompt = "")
    {
        // Formulate Request
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api.quotable.io/quotes/random?tags={prompt}");

        var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/Quantam-Studios/BobTheBot)");
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
        var embed = new Discord.EmbedBuilder
        {
            Title = $"Bob's Info",
            Color = new Discord.Color(6689298),
        };
        embed.AddField(name: "📛 Username", value: $"{Bot.Client.CurrentUser.Username}", inline: true).AddField(name: "🪪 ID", value: $"`{Bot.Client.CurrentUser.Id}`", inline: true).AddField(name: "📈 Server Count", value: $"`{Bot.Client.Guilds.Count}`").AddField(name: ":calendar_spiral: Date Created", value: $"`{Bot.Client.CurrentUser.CreatedAt}`", inline: true).AddField(name: "⚡ Github Repository", value: "https://github.com/Quantam-Studios/BobTheBot").AddField(name: "🏗️ Made With", value: "C#, .NET", inline: true).AddField(name: "📡 Hosted With", value: "Raspberry PI 4", inline: true);

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(false)]
    [SlashCommand("new", "See the newest changes to Bob, and find out what's next.")]
    public async Task New()
    {
        // Formulate Request
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.github.com/repos/Quantam-Studios/BobTheBot/commits/main");

        var productValue = new ProductInfoHeaderValue("BobTheBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+https://github.com/Quantam-Studios/BobTheBot)");
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

        var embed = new Discord.EmbedBuilder
        {
            Title = $"What's New?",
            Color = new Discord.Color(6689298),
        };

        embed.AddField(name: "✨ Latest Update", value: commitMessage, inline: true).AddField(name: ":calendar_spiral: Date", value: $"`{commitDate}`", inline: true).AddField(name: "🔮 See What's In the Works", value: "https://github.com/users/Quantam-Studios/projects/3/views/1");

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(true)]
    [SlashCommand("color", "Bob will choose a random color.")]
    public async Task Color()
    {
        var hex = String.Format("{0:X6}", random.Next(0x1000000));
        System.Drawing.Color rgb = System.Drawing.Color.FromArgb(int.Parse(hex, System.Globalization.NumberStyles.HexNumber));
        Discord.Color displayColor = new Discord.Color(Convert.ToUInt32(hex, 16));

        var embed = new Discord.EmbedBuilder { };
        embed.AddField("Hex Value", "`" + hex + "`").AddField("RGB Value", $"`R: {rgb.R}, G: {rgb.G}, B: {rgb.B}`")
        .WithColor(displayColor);

        await RespondAsync(embed: embed.Build());
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

    [EnabledInDm(false)]
    [SlashCommand("help", "Bob will DM you all relevant information for every command.")]
    public async Task help()
    {
        var embed = new Discord.EmbedBuilder
        {
            Title = $"📖 Here is a list of all of my commands.",
            Color = new Discord.Color(6689298),
        };
        embed.AddField(name: "🎲 Randomly Generated (RNG):", value: "- `/color` Get a color with hex, and RGB codes.\n\n- `/dice-roll [sides]` Roll a die with a specified # of sides.\n\n- `/coin-toss` Flip a coin.\n\n- `/quote [prompt]` Get a random quote.\n  - `[prompt]`choices: This is optional, use `/quote-prompts` to view all valid prompts.\n\n- `/dad-joke` Get a random dad joke.\n\n- `/random-fact` Get an outrageous fact.\n\n- `/8ball [prompt]` Get an 8 ball response to a prompt.")
        .AddField(name: "🎮 Games:", value: "- `/rock-paper-scissors` Play Bob in a game of rock paper scissors.")
        .AddField(name: "✨ Other:", value: "- `/fonts [text] [font]` Change your text to a different font.\n  - `[font]` choices: 𝖒𝖊𝖉𝖎𝖊𝖛𝖆𝖑, 𝓯𝓪𝓷𝓬𝔂, flip (this will make your text the opposite), s̷l̷̷a̷s̷h̷e̷d̷, and 🄱🄾🅇🄴🄳.\n\n- `/encrypt [message] [cipher]` Change text into a cipher.\n    - `[cipher]` choices: Caesar, A1Z26, Atbash\n\n- `/poll [prompt] [option]*4` Create a poll.\n  - `[option]*4` usage: You must provide 2-4 options. These are essentially the poll's choices. \n\n- `/ship [user]*2` See how good of a match 2 users are.")
        .AddField(name: "🗄️ Informational / Help:", value: "- `/new` See the latest updates to Bob.\n\n- `/quote-prompts` See all valid prompts for `/quote`.\n\n- `/ping` Find the client's latency.\n\n- `/info` Learn about Bob.\n\n- `/suggest` Join Bob's official server, and share you ideas!");

        await Context.User.SendMessageAsync(embed: embed.Build());
        await RespondAsync(text: $"📪 Check your DMs.", ephemeral: true);
    }

    [EnabledInDm(false)]
    [SlashCommand("confess", "Bob will send someone a message anonymously")]
    public async Task confess(string message, SocketUser user, Confession.SignOffs signOff)
    {
        string signed = "";
        switch (signOff)
        {
            case Confession.SignOffs.Anon:
                signed = "- Anon";
                break;
            case Confession.SignOffs.Secret_Admirer:
                signed = "- your *secret* admirer";
                break;
            case Confession.SignOffs.You_Know_Who:
                signed = "- *you know who*";
                break;
            case Confession.SignOffs.Guess:
                signed = "- guess who!";
                break;
            case Confession.SignOffs.FBI:
                signed = "- The FBI ... (not actually)";
                break;
            case Confession.SignOffs.Your_Dad:
                signed = "- your father ... you thought";
                break;
        }

        if (user.IsBot)
        {
            await RespondAsync(text: "❌ Sorry, but no sending messages to bots.", ephemeral: true);
        }
        else
        {
            await user.SendMessageAsync($"{message} {signed}");
            await RespondAsync(text: $"✉️ Your message has been sent!\nMessage: **{message}** was sent to **{user.Username}**", ephemeral: true);
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
    [SlashCommand("poll", "Bob will create a poll.")]
    public async Task Poll(string prompt, string option1, string option2, string option3 = "", string option4 = "")
    {
        // Setup base data
        string footerText = Context.User.Username + " created this poll.";
        string instructions = "React with the corresponding number to cast your vote.";

        // Prepare color
        Discord.Color displayColor = new Discord.Color(6689298);

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
    public async Task Fonts(string text, FontConversion.FontTypes font)
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
            case FontConversion.FontTypes.flip:
                finalText = FontConversion.Flip(text);
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
    public async Task Encrypt(string message, Encryption.CipherTypes cipher)
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
        }

        await RespondAsync($"{finalText}", ephemeral: true);
    }
}




