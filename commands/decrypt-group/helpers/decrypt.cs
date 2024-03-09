using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Commands.Helpers
{
    public static class Decrypt
    {
        public static async Task SendDecryptMessage(SocketInteraction interaction, string finalText)
        {
            if (finalText.Length + 6 > 4096)
            {
                await interaction.FollowupAsync(text: $"❌ The message *cannot* be decrypted because the encryption contains **{finalText.Length + 6}** characters.\n- Try decrypting fewer characters.\n- Try breaking it up.\n- Discord has a limit of **4096** characters in embeds.", ephemeral: true);
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Title = "🔓 Decrypt",
                    Color = 2303786,
                    Description = $"```{finalText}```",
                };
                await interaction.FollowupAsync(embed: embed.Build(), ephemeral: true);
            }
        }
    }
}