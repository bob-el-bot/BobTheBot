using Discord.Interactions;
using System.Threading.Tasks;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using Discord.WebSocket;
using Discord;

public class Commands : InteractionModuleBase<SocketInteractionContext>
{
    // GENERAL Stuff

    [EnabledInDm(false)]
    [SlashCommand("ping", "Bob will share his ping.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Ping()
    {
        await RespondAsync(text: $"🏓 Pong! The client latency is **{Bot.Client.Latency}** ms.");
    }

    [EnabledInDm(false)]
    [SlashCommand("hi", "Say hi to Bob.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Hi()
    {
        await RespondAsync(text: "👋 hi!");
    }

    [EnabledInDm(false)]
    [SlashCommand("cointoss", "Bob will flip a coin")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
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
    [SlashCommand("diceroll", "Bob will roll a die with the side amount specified.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task DiceRoll(int sides)
    {
        if (sides <= 0)
        {
            await RespondAsync(text: $"🌌 The die formed a *rift* in our dimension! **Maybe** try using a number **greater than 0**");
        }
        else
        {
            var random = new Random();
            int randInt = random.Next(1, (sides + 1));
            await RespondAsync(text: $"🎲 The {sides} sided die landed on **{randInt}**");
        }
    }

    [EnabledInDm(false)]
    [SlashCommand("8ball", "Bob will shake a magic 8 ball in repsonse to a question.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Magic8Ball(string prompt)
    {
        // Possible Answers
        string[] results = { "'no'", "'yes'", "'maybe'", "'ask again'", "'probably not'", "'affirmative'", "'it is certain'", "'very doubtful'" };

        // Get Random Result
        var random = new Random();
        string result = results[random.Next(0, results.Length)];

        // Respond
        await RespondAsync(text: $"🎱 The magic 8 ball says **{result}** in response to {prompt}");
    }

    [EnabledInDm(false)]
    [SlashCommand("dadjoke", "Bob will tell you a dad joke.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
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
    [SlashCommand("bye", "Say bye to Bob.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Bye()
    {
        await RespondAsync(text: "👋 bye!");
    }

    [EnabledInDm(false)]
    [SlashCommand("color", "Bob will choose a random color.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Color()
    {
        var random = new Random();
        var hex = String.Format("{0:X6}", random.Next(0x1000000));
        System.Drawing.Color rgb = System.Drawing.Color.FromArgb(int.Parse(hex, System.Globalization.NumberStyles.HexNumber));
        Discord.Color displayColor = new Discord.Color(Convert.ToUInt32(hex, 16));

        var embed = new Discord.EmbedBuilder { };
        embed.AddField("Hex Value", "`" + hex + "`").AddField("RGB Value", $"`R: {rgb.R}, G: {rgb.G}, B: {rgb.B}`")
        .WithColor(displayColor);

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(false)]
    [SlashCommand("servers", "How many servers is Bob serving?")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Servers()
    {
        await RespondAsync(text: $"📈 I am in **{Bot.Client.Guilds.Count}** servers!");
    }

    [EnabledInDm(false)]
    [SlashCommand("serverinfo", "Bob will tell you info about the current server.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task ServerInfo()
    {
        // Get all data
        string serverName = Context.Guild.Name;
        int memberCount = Context.Guild.MemberCount;
        DateTimeOffset dateCreated = Context.Guild.CreatedAt;
        string serverDescription = Context.Guild.Description ?? "";
        int textChannelCount = Context.Guild.TextChannels.Count;
        int voiceChannelCount = Context.Guild.VoiceChannels.Count;
        int roleCount = Context.Guild.Roles.Count;
        ulong serverId = Context.Guild.Id;
        string ownerDisplayName = Context.Guild.Owner.Username;
        ulong ownerId = Context.Guild.OwnerId;
        string iconUrl = Context.Guild.IconUrl;

        // Prepare color
        Discord.Color displayColor = new Discord.Color(5793266);

        // Embed
        var embed = new Discord.EmbedBuilder
        {
            Title = serverName,
            ThumbnailUrl = iconUrl,
            Description = serverDescription,
        };

        embed.AddField(name: "Id", value: "`" + serverId.ToString() + "`", inline: true).AddField(name: "Members", value: "`" + memberCount.ToString() + "`", inline: true).AddField(name: "Creation Date", value: "`" + dateCreated.ToString("yyyy/MM/dd") + "`", inline: true).AddField(name: "Roles", "`" + roleCount.ToString() + "`", inline: true).AddField(name: "Text Channels", value: "`" + textChannelCount.ToString() + "`", inline: true).AddField(name: "Voice Channels", value: "`" + voiceChannelCount + "`", inline: true).AddField(name: "Owner", value: ownerDisplayName, inline: true).AddField(name: "Owner ID", value: "`" + ownerId.ToString() + "`", inline: true).WithColor(displayColor);

        await RespondAsync(embed: embed.Build());
    }

    [EnabledInDm(false)]
    [SlashCommand("poll", "Bob will create a poll.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
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

 [EnabledInDm(false)]
    [SlashCommand("fancytext", "Bob will type your text in a fancy font.")]
    [RequireBotPermission(Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task FancyText(string text)
    {
        string[] fancyAlpha = { "𝖆", "𝖇", "𝖈", "𝖉", "𝖊", "𝖋", "𝖌", "𝖍", "𝖎", "𝖏", "𝖐", "𝖑", "𝖒", "𝖓", "𝖔", "𝖕", "𝖖", "𝖗", "𝖘", "𝖙", "𝖚", "𝖛", "𝖜", "𝖝", "𝖞", "𝖟" };
        string alpha = "abcdefghijklmnopqrstuvwxyz";
        string fancifiedText = "";
      
        foreach(char letter in text){
          if (alpha.Contains(letter)) 
          {
            int letterIndex = alpha.IndexOf(letter);
            fancifiedText += fancyAlpha[letterIndex]; 
          }
          else
            fancifiedText += letter;
        }

        await RespondAsync(fancifiedText);      
    }

    // MOD Stuff

    [EnabledInDm(false)]
    [SlashCommand("ban", "Bob will defend your server from a user forever by... banning them.")]
    [RequireBotPermission(Discord.GuildPermission.BanMembers | Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Ban(SocketGuildUser user, int days, String reason)
    {
        if (Context.Guild.GetUser(id: Context.User.Id).GuildPermissions.BanMembers)
        {
            await user.Guild.AddBanAsync(user: user, pruneDays: days, reason: reason);
            await RespondAsync(text: $"🚫 {user.Mention} has been **banned**.");
        }
        else
        {
            await RespondAsync(text: $"Hey, you do **not** have permission to do that.", ephemeral: true);
        }
    }

    [EnabledInDm(false)]
    [SlashCommand("unban", "Allow a user to come back to the server... maybe they learned their lesson.")]
    [RequireBotPermission(Discord.GuildPermission.BanMembers | Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task UnBan(SocketUser user)
    {

        if (Context.Guild.GetUser(id: Context.User.Id).GuildPermissions.BanMembers)
        {
            await Context.Guild.RemoveBanAsync(user.Id);
            await RespondAsync(text: $"✅ {user.Mention} has been **unbanned**.");
        }
        else
        {
            await RespondAsync(text: $"Hey, you do **not** have permission to do that.", ephemeral: true);
        }
    }

    [EnabledInDm(false)]
    [SlashCommand("kick", "Bob will put their foot down by kicking a user.")]
    [RequireBotPermission(Discord.GuildPermission.KickMembers | Discord.GuildPermission.ViewChannel | Discord.GuildPermission.SendMessages)]
    public async Task Kick(SocketGuildUser user, String reason)
    {
        if (Context.Guild.GetUser(id: Context.User.Id).GuildPermissions.KickMembers)
        {
            await user.KickAsync(reason: reason);
            await RespondAsync(text: $"🦶 {user.Mention} has been **kicked**.");
        }
        else
        {
            await RespondAsync(text: $"Hey, you do **not** have permission to do that.", ephemeral: true);
        }
    }
}


