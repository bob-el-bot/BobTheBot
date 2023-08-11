using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ColorHelper;
using Discord.Interactions;

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