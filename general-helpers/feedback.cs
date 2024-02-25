using System.Threading.Tasks;
using Debug;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Feedback
{
    public class Prompt : InteractionModuleBase<SocketInteractionContext>
    {
        public static async Task LeftGuild(SocketGuild guild)
        {
            // Check if the guild still exists
            IUser owner = guild.Owner ?? await Bot.Client.GetUserAsync(guild.OwnerId);
            var components = new ComponentBuilder();

            var selectMenu = new SelectMenuBuilder
            {
                MinValues = 1,
                CustomId = $"leftGuild:{guild.Name}:{guild.Users.Count}",
                Placeholder = "Select why...",
            };

            selectMenu.AddOption("Too complicated", "1 Too complicated");
            selectMenu.AddOption("Found a better bot", "2 Found a better bot");
            selectMenu.AddOption("Missing feature(s)", "3 Missing features");
            selectMenu.AddOption("Invited on accident / wrong server", "4 Invited on accident / wrong server");
            selectMenu.AddOption("Bot was not responding", "5 Bot was not responding");

            selectMenu.MaxValues = selectMenu.Options.Count;

            components.WithSelectMenu(selectMenu);

            // Send the message to the guild owner
            await owner.SendMessageAsync(text: "### 😢 Sorry to see you go...\nMind telling us why?", components: components.Build());
        }

        [ComponentInteraction("leftGuild:*:*")]
        public async Task LeftGuildOptionsHandler(string guildName, string userCount)
        {   
            await DeferAsync();

            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await component.ModifyOriginalResponseAsync(x => { x.Content = "💜 Thanks for the feedback!"; x.Components = null; });

            SocketTextChannel logChannel = (SocketTextChannel)Bot.Client.GetGuild(Bot.supportServerId).GetChannel(Bot.systemLogChannelId);
            await Logger.LogFeedbackToDiscord(logChannel, guildName, userCount, (string[])component.Data.Values);
        }
    }
}