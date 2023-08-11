using Discord.Interactions;
using System.Threading.Tasks;
using System;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;
using System.Net.Http;
using Discord.WebSocket;
using Discord;
using System.Linq;

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
        .AddField(name: "🗄️ Informational / Help:", value: "- `/new` See the latest updates to Bob.\n\n- `/quote-prompts` See all valid prompts for `/random quote`.\n\n- `/ping` Find the client's latency.\n\n- `/info` Learn about Bob.\n\n- `/suggest` Join Bob's official server, and share you ideas!");

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
    [SlashCommand("poll", "Bob will create a poll.")]
    public async Task Poll([Summary("prompt", "The question you are asking.")] string prompt, [Summary("option1", "an answer / response to your question")] string option1, [Summary("option2", "an answer / response to your question")] string option2, [Summary("option3", "an answer / response to your question")] string option3 = "", [Summary("option4", "an answer / response to your question")] string option4 = "")
    {
        // Check for permissions
        if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.AddReactions)
        {
            await RespondAsync("❌ Bob needs the **Add Reactions** permission to use `/poll`\n- Try asking an administrator.", ephemeral: true);
        }
        else
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




