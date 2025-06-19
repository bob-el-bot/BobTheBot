using System.Threading.Tasks;
using Bob.Database;
using Bob.Debug;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Bob.Feedback
{
    public class Prompt : InteractionModuleBase<ShardedInteractionContext>
    {
        public static async Task LeftGuild(SocketGuild guild)
        {
            try
            {
                // Check if the guild still exists
                IUser owner = guild.Owner ?? await Bot.Client.GetShardFor(guild.Id).GetUserAsync(guild.OwnerId);
                var components = new ComponentBuilder();

                var selectMenu = new SelectMenuBuilder
                {
                    MinValues = 1,
                    CustomId = $"leftGuild:{guild.Name}",
                    Placeholder = "Select why...",
                };

                selectMenu.AddOption("Too complicated", "1 Too complicated");
                selectMenu.AddOption("Found a better bot", "2 Found a better bot");
                selectMenu.AddOption("Missing feature(s)", "3 Missing features");
                selectMenu.AddOption("Invited on accident / wrong server", "4 Invited on accident / wrong server");
                selectMenu.AddOption("Bot was not responding", "5 Bot was not responding");
                selectMenu.AddOption("Other", "6 Other");

                selectMenu.MaxValues = selectMenu.Options.Count;

                components.WithSelectMenu(selectMenu);

                // Send the message to the guild owner
                await owner.SendMessageAsync(text: "### 😢 Sorry to see you go...\nMind telling us why?", components: components.Build());
            }
            catch
            {
                // User's direct messages are closed, no action needed
            }

            using var context = new BobEntities();

            var server = await context.GetServer(guild.Id);
            
            if (server == null)
            {
                return;
            }

            await context.RemoveServer(server);
        }

        [ComponentInteraction("leftGuild:*")]
        public async Task LeftGuildOptionsHandler(string guildName)
        {
            await DeferAsync();

            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await component.ModifyOriginalResponseAsync(x => { x.Content = "💜 Thanks for the feedback!"; x.Components = null; });

            await Logger.LogFeedbackToDiscord(guildName, (string[])component.Data.Values);
        }
    }
}