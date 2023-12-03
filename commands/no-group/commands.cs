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

namespace Commands
{
    public class NoGroup : InteractionModuleBase<SocketInteractionContext>
    {
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
        [SlashCommand("analyze-link", "Bob will check out a link, and see where it takes you.")]
        public async Task AnalyzeLink([Summary("link", "The link in question.")] string link)
        {
            if (link.Contains('.') && link.Length < 7 || (link.Length >= 7 && link[..7] != "http://" && link.Length >= 8 && link[..8] != "https://"))
            {
                link = $"https://{link}";
            }

            if (!Uri.IsWellFormedUriString(link, UriKind.Absolute))
            {
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know:\n- Your link could look like this `http://bobthebot.net`, `https://bobthebot.net`, or `bobthebot.net`.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                await DeferAsync();
                await FollowupAsync(embed: await Analyze.AnalyzeLink(link));
            }
        }

        [EnabledInDm(true)]
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
                await RespondAsync(text: "❌ Your link is not valid. Here are some things to know:\n- Your link could look like this `http://bobthebot.net`, or `https://bobthebot.net`.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                await DeferAsync();
                await FollowupAsync(embed: await Analyze.AnalyzeLink(matches[0].Value));
            }
        }

        [EnabledInDm(true)]
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
        [SlashCommand("quote-prompts", "Bob will give you all valid prompts for /random quote.")]
        public async Task QuotePrompts()
        {
            // Respond
            await RespondAsync(text: $"Here are all valid prompts for `/random quote`:\nage, athletics, business, change, character, competition, conservative, courage, education, ethics, failure, faith, family, famous-quotes, film, freedom, future, generosity, genius, gratitude, happiness, health, history, honor, humor, humorous, inspirational, knowledge, leadership, life, love, mathematics, motivational, nature, oppurtunity, pain, perseverance, philosphy, politics, power-quotes, proverb, religion, sadness, science, self, sports, stupidity, success, technology, time, tolerance, truth, virtue, war, weakness, wellness, wisdom, work");
        }

        [EnabledInDm(true)]
        [SlashCommand("announce", "Bob will create a fancy embed announcement in the channel the command is used in.")]
        public async Task Announce([Summary("title", "The title of the announcement (the title of the embed).")] string title, [Summary("description", "The anouncement (the description of the embed).")] string description, [Summary("color", "A color name (purple), or valid hex code (#8D52FD).")] string color)
        {
            Color finalColor = Convert.ToUInt32(Announcement.StringToHex(color), 16);

            if (finalColor == 0)
            {
                await RespondAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- red, pink, orange, yellow, blue, green, white, gray (grey), black. \n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else if (title.Length > 256) // 256 is max characters in an embed title.
            {
                await FollowupAsync($"❌ The announcement *cannot* be made because it contains **{title.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **256** characters in embed titles.", ephemeral: true);
            }
            else if (description.Length > 4096) // 4096 is max characters in an embed description.
            {
                await FollowupAsync($"❌ The announcement *cannot* be made because it contains **{description.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **4096** characters in embed descriptions.", ephemeral: true);
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
                        Text = $"Announced by {Context.User.GlobalName}."
                    }
                };

                await RespondAsync(text: "✅ Your announcement has been made.", ephemeral: true);
                await Context.Channel.SendMessageAsync(embed: embed.Build());
            }
        }

        [EnabledInDm(true)]
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

        [EnabledInDm(false)]
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

            embed.AddField(name: "🗒️ Creators Notes", value: "- 🖊️ *The best* quote system is out now :partying_face: :tada: .\n- Bob has a place on the 🌐 web! [bobthebot.net](https://bobthebot.net)\n- Stay 📺 tuned for some awesome updates!", inline: false).AddField(name: "✨ Latest Update", value: commitMessage, inline: true).AddField(name: ":calendar_spiral: Date", value: $"<t:{commitDateID}:f>", inline: true).AddField(name: "🔮 See What's In the Works", value: "[Road Map](https://github.com/orgs/bob-el-bot/projects/4)");

            await RespondAsync(embed: embed.Build());
        }

        [EnabledInDm(false)]
        [SlashCommand("help", "Bob will DM you all relevant information for every command.")]
        public async Task help()
        {
            var embed = new EmbedBuilder
            {
                Title = $"📖 Here is a list of all of my commands.",
                Color = Bot.theme
            };
            embed.AddField(name: "🎲 Randomly Generated (RNG):", value: "- `/random color` Get a color with Hex, CMYK, HSL, HSV and RGB codes.\n\n- `/random dice-roll [sides]` Roll a die with a specified # of sides.\n\n- `/random coin-toss` Flip a coin.\n\n- `/random quote [prompt]` Get a random quote.\n  - `[prompt]`choices: This is optional, use `/quote-prompts` to view all valid prompts.\n\n- `/random dad-joke` Get a random dad joke.\n\n- `/random fact` Get an outrageous fact.\n\n- `/random 8ball [prompt]` Get an 8 ball response to a prompt.\n\n- `/random dog` Get a random picture of a dog.\n\n- `/random date [earliestYear] [latestYear]` Get a random date between the inputed years.\n\n- `/random advice` Get a random piece of advice.\n\n- `/random choose [option]*5` Bob will pick from the options provided.")
            .AddField(name: "🎮 Games:", value: "- `/rock-paper-scissors` Play Bob in a game of rock paper scissors.\n\n- `/master-mind new-game` Play a game of Master Mind, the rules will shared upon usage.\n\n- `/master-mind guess` Make a guess in a game of Master Mind.")
            .AddField(name: "🖊️ Quoting:", value: "- `/quote new [quote] [user] [tag]*3` Formats and shares the quote in designated channel.\n\n- `/quote channel [channel]` Sets the quote channel for the server.")
            .AddField(name: "✨ Other:", value: "- `/code preview [link]` Preview specific lines of code from a file on GitHub. \n\n- `/fonts [text] [font]` Change your text to a different font.\n  - `[font]` choices: 𝖒𝖊𝖉𝖎𝖊𝖛𝖆𝖑, 𝓯𝓪𝓷𝓬𝔂, 𝕠𝕦𝕥𝕝𝕚𝕟𝕖𝕕, ɟןıddǝp, s̷l̷̷a̷s̷h̷e̷d̷, and 🄱🄾🅇🄴🄳.\n\n- `/encrypt [message] [cipher]` Change text into a cipher.\n    - `[cipher]` choices: Caesar, A1Z26, Atbash, Morse Code\n\n- `/decrypt [message] [cipher]` Change encrypted text to plain text.\n    - `[cipher]` choices: Caesar, A1Z26, Atbash, Morse Code\n\n- `/confess [message] [user] [signoff]` Have Bob DM a user a message.\n\n- `/announce [title] [description] [color]` Have a fancy embed message sent.\n\n- `/poll [prompt] [option]*4` Create a poll.\n  - `[option]*4` usage: You must provide 2-4 options. These are essentially the poll's choices.\n\n- `/ship [user]*2` See how good of a match 2 users are.\n\n- `/hug [user]*5` Show your friends some love with a hug.\n\n- `/welcome [welcome]` Bob will send welcome messages to new server members.")
            .AddField(name: "🗄️ Informational / Help:", value: "- `/new` See the latest updates to Bob.\n\n- `/quote-prompts` See all valid prompts for `/random quote`.\n\n- `/ping` Find the client's latency.\n\n- `/analyze-link` See where a link will take you, and check for rick rolls.\n\n- `/info` Learn about Bob.\n\n- `/support` Sends an invite to Bob's support Server.");

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

        [EnabledInDm(true)]
        [SlashCommand("support", "Sends an invite to Bob's support Server.")]
        public async Task Support()
        {
            // Respond
            await RespondAsync(text: "🏰 Having issues with Bob? [Join Here](https://discord.gg/HvGMRZD8jQ) for help.");
        }

        [EnabledInDm(false)]
        [SlashCommand("welcome", "Enable or disable Bob welcoming users to your server!")]
        public async Task Welcome([Summary("welcome", "If checked, Bob will send welcome messages.")] bool welcome)
        {
            await DeferAsync(ephemeral: true);

            if (Context.Guild.SystemChannel == null)
            {
                await FollowupAsync(text: $"❌ You **need** to set a *System Messages* channel in settings in order for Bob to greet people.", ephemeral: true);
            }
            // Check if the user has manage channels permissions
            else if (!Context.Guild.GetUser(Context.User.Id).GetPermissions(Context.Guild.SystemChannel).ManageChannel)
            {
                await FollowupAsync(text: $"❌ You do not have permissions to manage <#{Context.Guild.SystemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission **Manage Channel**.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Check if Bob has permission to send messages in given channel
            else if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.SystemChannel).SendMessages || !Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(Context.Guild.SystemChannel).ViewChannel)
            {
                await FollowupAsync(text: $"❌ Bob either does not have permission to view *or* send messages in the channel <#{Context.Guild.SystemChannel.Id}> (The system channel where welcome messages are sent)\n- Try giving Bob the following permissions: `View Channel`, `Send Messages`.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            // Update server welcome information.
            else
            {
                Server server = await Bot.DB.GetServer(Context.Guild.Id);
                server.Welcome = welcome;
                await Bot.DB.UpdateServer(server);

                if (welcome)
                {
                    if (Context.Guild.SystemChannel == null)
                    {
                        await FollowupAsync(text: $"❌ Bob knows to welcome users now, but you **need** to set a *System Messages* channel in settings for this to take effect.", ephemeral: true);
                    }
                    else
                    {
                        await FollowupAsync(text: $"✅ Bob will now greet people in <#{Context.Guild.SystemChannel.Id}>", ephemeral: true);
                    }
                }
                else
                {
                    await FollowupAsync(text: $"✅ Bob will not greet people anymore.", ephemeral: true);
                }
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("poll", "Bob will create a poll.")]
        public async Task Poll([Summary("prompt", "The question you are asking.")] string prompt, [Summary("option1", "an answer / response to your question")] string option1, [Summary("option2", "an answer / response to your question")] string option2, [Summary("option3", "an answer / response to your question")] string option3 = "", [Summary("option4", "an answer / response to your question")] string option4 = "")
        {
            // Check for permissions
            if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions((IGuildChannel)Context.Channel).AddReactions)
            {
                await RespondAsync("❌ Bob needs the **Add Reactions** permission to use `/poll`\n- Try asking an administrator.\n- If you think this is a mistake join [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                // Setup base data
                string footerText = Context.User.Username + " created this poll.";
                string instructions = "React with the corresponding number to cast your vote.";

                // Embed
                var embed = new EmbedBuilder
                {
                    Title = "📊 " + prompt,
                    Description = instructions,
                    Color = Bot.theme
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
            if (text.Length > 2000) // 2000 is max characters in a message.
            {
                await RespondAsync($"❌ The inputted text *cannot* be converted to a different font because it contains **{text.Length}** characters.\n- Try having fewer characters.\n- Discord has a limit of **2000** characters.", ephemeral: true);
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
                await RespondAsync(finalText);
            }
        }

        [EnabledInDm(false)]
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

        [EnabledInDm(true)]
        [SlashCommand("encrypt", "Bob will encrypt your message with a cipher of your choice.")]
        public async Task Encrypt([Summary("message", "the text you want to encrypt")] string message, Ciphers.CipherTypes cipher)
        {
            string finalText = "";

            switch (cipher)
            {
                case Ciphers.CipherTypes.Atbash:
                    finalText = Encryption.Atbash(message);
                    break;
                case Ciphers.CipherTypes.Caesar:
                    finalText = Encryption.Caesar(message, 3);
                    break;
                case Ciphers.CipherTypes.A1Z26:
                    finalText = Encryption.A1Z26(message);
                    break;
                case Ciphers.CipherTypes.Morse:
                    finalText = Encryption.Morse(message);
                    break;
                default:
                    break;
            }

            if (finalText.Length > 2000)
            {
                await RespondAsync(text: $"❌ The message *cannot* be encrypted because the encryption contains **{finalText.Length}** characters.\n- Try encrypting fewer lines.\n- Try breaking it up.\n- Discord has a limit of **2000** characters.", ephemeral: true);
            }
            else
            {
                await RespondAsync($"🔒 {finalText}", ephemeral: true);
            }
        }

        [EnabledInDm(true)]
        [SlashCommand("decrypt", "Bob will decrypt your message from a cipher of your choice.")]
        public async Task Decrypt([Summary("message", "the text you want to encrypt")] string message, Ciphers.CipherTypes cipher)
        {
            string finalText = "";

            switch (cipher)
            {
                case Ciphers.CipherTypes.Atbash:
                    finalText = Decryption.Atbash(message);
                    break;
                case Ciphers.CipherTypes.Caesar:
                    finalText = Decryption.Caesar(message, 3);
                    break;
                case Ciphers.CipherTypes.A1Z26:
                    finalText = Decryption.A1Z26(message);
                    break;
                case Ciphers.CipherTypes.Morse:
                    finalText = Decryption.Morse(message);
                    break;
                default:
                    break;
            }

            if (finalText.Length > 2000)
            {
                await RespondAsync(text: $"❌ The message *cannot* be encrypted because the encryption contains **{finalText.Length}** characters.\n- Try encrypting fewer lines.\n- Try breaking it up.\n- Discord has a limit of **2000** characters.", ephemeral: true);
            }
            else
            {
                await RespondAsync($"🔓 {finalText}", ephemeral: true);
            }
        }
    }
}




