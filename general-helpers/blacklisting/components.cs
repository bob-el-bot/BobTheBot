using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Moderation
{
    public class Components : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("banUser:*:*")]
        public async Task HelpOptionsHandler(string id, string reason)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            var user = await BlackList.GetUser(userId);

            var punishment = (BlackList.Punishment)int.Parse(component.Data.Values.FirstOrDefault());
            var expiration = BlackList.GetExpiration(punishment);

            if (user == null)
            {
                user = new()
                {
                    Id = userId,
                    Reason = reason,
                    Expiration = expiration
                };
            }
            else
            {
                user.Reason = $"{user.Reason}\n{reason}";
                user.Expiration = expiration;
            }

            await BlackList.UpdateUser(user);

            string newInfo = $"✅ **Banned:**\n {user}";
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

            await component.ModifyOriginalResponseAsync(x => { x.Content = response; });
        }

        [ComponentInteraction("reportUser:*:*")]
        public async Task ReportUserButtonHandler(string id, string message)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await BlackList.NotifyUserReport(userId, message);

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "✅ User has been reported and will be punished accordingly.";
                x.Components = null;
            });
        }

        [ComponentInteraction("reportMessage:*:*")]
        public async Task ReportMessageButtonHandler(string dmChannelId, string messageId)
        {
            await DeferAsync();

            var parsedMessageId = Convert.ToUInt64(messageId);
            var parsedChannelId = Convert.ToUInt64(dmChannelId);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await BlackList.NotifyMessageReport(parsedChannelId, parsedMessageId);

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "✅ Message has been reported and will be reviewed.\n- If the message contains content which violates our terms of service our blacklist will be updated accordingly.\n- If you are hoping for the offending user to be punished, the infrastructure needed is currently in the works. We apologize for the inconvenience.";
                x.Components = null;
            });
        }

        [ComponentInteraction("disableConfessions:*")]
        public async Task DisableConfessionsButtonHandler(string id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            using var context = new BobEntities();
            var dbUser = await context.GetUser(userId); 
            
            if (dbUser.ConfessionsOff == false)
            {
                dbUser.ConfessionsOff = true;
                await context.UpdateUser(dbUser);
            }

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "✅ Your DMs will now appear closed to people using `/confess`.";
                x.Components = null;
            });
        }

        [ComponentInteraction("listBanDetails:*")]
        public async Task ListBanDetailsButtonHandler(string id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            var user = await BlackList.GetUser(userId);

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
        public async Task RemoveBanButtonHandler(string id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            await BlackList.RemoveUser(userId);

            string newInfo = "✅ This user has been unbanned.";

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

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = response;
                x.Components = null;
            });
        }

        [ComponentInteraction("permanentlyBan:*")]
        public async Task PermanentlyBanButtonHandler(string id)
        {
            await DeferAsync();

            var userId = Convert.ToUInt64(id);
            SocketMessageComponent component = (SocketMessageComponent)Context.Interaction;

            var user = await BlackList.GetUser(userId);

            string newInfo;
            if (user != null)
            {
                user.Expiration = DateTime.MaxValue;
                await BlackList.UpdateUser(user);

                newInfo = "✅ This user has been **permanently banned**.";
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

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = response;
                x.Components = null;
            });
        }
    }
}
