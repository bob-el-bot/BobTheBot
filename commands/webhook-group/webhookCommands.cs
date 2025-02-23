using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("webhook", "All webhook commands.")]
    public class WebhookGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("create", "Creates a webhook for the specified channel.")]
        public async Task CreateWebhook(ITextChannel channel, string webhookName)
        {
            try
            {
                // Ensure the bot has permission to manage webhooks
                var botUser = Context.Client.CurrentUser as SocketSelfUser;
                var guild = (channel as IGuildChannel).Guild;
                var currentUser = await guild.GetCurrentUserAsync();
                var botPermissions = currentUser.GetPermissions(channel);

                if (!botPermissions.ManageWebhooks)
                {
                    await RespondAsync("I don't have permission to manage webhooks in that channel.", ephemeral: true);
                    return;
                }

                // Create the webhook
                var webhook = await channel.CreateWebhookAsync(webhookName);
                var webhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}";

                await RespondAsync($"Webhook created successfully! \nURL: ||{webhookUrl}||", ephemeral: true);
            }
            catch (Exception ex)
            {
                await RespondAsync($"Failed to create webhook: {ex.Message}", ephemeral: true);
            }
        }
    }
}