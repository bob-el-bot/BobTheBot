using System;
using System.Threading.Tasks;
using Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Moderation
{
    public class Buttons : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("listBanDetails:*")]
        public async Task ListBanDetailsButtonHandler(string Id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(Id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(userId);

            string newInfo = user != null ? $"✅ {user.FormatAsString()}" : "❌ This user is no longer on the blacklist.";
            string response = $"{component.Message.Content}\n{newInfo}";

            if (response.Length > 2000) // Discord Message Max Length is 2000
            {
                var newMessage = await component.Message.ReplyAsync(text: newInfo);
                response = $"{component.Message.Content}\n{newMessage.GetJumpUrl()}";

                if (response.Length > 2000) // Check again after adding jump URL
                {
                    response = component.Message.Content;
                }
            }

            await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = response);
        }

        [ComponentInteraction("removeBan:*")]
        public async Task removeBanButtonHandler(string Id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(Id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(userId);

            string newInfo;
            if (user != null)
            {
                await context.RemoveUserFromBlackList(user);

                newInfo = "✅ This user has been unbanned.";
            }
            else
            {
                newInfo = "✅ This user was not on the blacklist.";
            }

            string response = $"{component.Message.Content}\n{newInfo}";

            if (response.Length > 2000) // Discord Message Max Length is 2000
            {
                var newMessage = await component.Message.ReplyAsync(text: newInfo);
                response = $"{component.Message.Content}\n{newMessage.GetJumpUrl()}";

                if (response.Length > 2000) // Check again after adding jump URL
                {
                    response = component.Message.Content;
                }
            }

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = response; x.Components = null; });
        }

        [ComponentInteraction("permanentlyBan:*")]
        public async Task PermanentlyBanButtonHandler(string Id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(Id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            using var context = new BobEntities();
            var user = await context.GetUserFromBlackList(userId);

            string newInfo;
            if (user != null)
            {
                user.Expiration = DateTime.MaxValue;
                await context.UpdateUserFromBlackList(user);

                newInfo = "✅ This user has been **permanantly banned**.";
            }
            else
            {
                newInfo = $"❌ This user was **not** on the blacklist. Try running:\n```/debug database add-user-to-black-list punishment: Permanent reason: Unknown (this was a manual decision so you must have really messed up). user-id: {userId}```";
            }

            string response = $"{component.Message.Content}\n{newInfo}";

            if (response.Length > 2000) // Discord Message Max Length is 2000
            {
                var newMessage = await component.Message.ReplyAsync(text: newInfo);
                response = $"{component.Message.Content}\n{newMessage.GetJumpUrl()}";

                if (response.Length > 2000) // Check again after adding jump URL
                {
                    response = component.Message.Content;
                }
            }

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = response; x.Components = null; });
        }
    }
}