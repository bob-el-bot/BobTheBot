using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Commands.Helpers
{
    public class Encrypt
    {
        public static async Task SendEncryptMessage(SocketInteraction interaction, string finalText)
        {
            if (finalText.Length + 6 > 4096)
            {
                await interaction.FollowupAsync(text: $"❌ The message *cannot* be encrypted because the message contains **{finalText.Length + 6}** characters.\n- Try encrypting fewer characters.\n- Try breaking it up.\n- Discord has a limit of **4096** characters in embeds.", ephemeral: true);
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Title = "🔒 Encrypt",
                    Color = 2303786,
                    Description = $"```{finalText}```",
                };
                await interaction.FollowupAsync(embed: embed.Build(), ephemeral: true);
            }
        }
    }
}