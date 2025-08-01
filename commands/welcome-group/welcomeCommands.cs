using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Interactions;
using Bob.PremiumInterface;
using static Bob.ApiInteractions.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bob.Commands
{
    [CommandContextType(InteractionContextType.Guild)]
    [IntegrationType(ApplicationIntegrationType.GuildInstall)]
    [Group("welcome", "All welcome commands.")]
    public class WelcomeGroup(BobEntities dbContext) : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("toggle", "Enable or disable Bob welcoming users to your server!")]
        public async Task WelcomeToggle([Summary("welcome", "If checked (true), Bob will send welcome messages.")] bool welcome)
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if user is an administrator
            if (!discordUser.GuildPermissions.Administrator)
            {
                // Ensure system channel is set
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You **need** to set a **System Messages** channel in settings for Bob to greet users.", ephemeral: true);
                    return;
                }

                // Check user permissions for managing the system channel
                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}>.\n- Ask a user with `Manage Channel` permission.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
                    return;
                }
            }

            // If the system channel is null
            if (welcome && systemChannel != null)
            {
                // Check if Bob has permission to send messages in the system channel
                var bobPermissions = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(systemChannel);
                if (!bobPermissions.SendMessages || !bobPermissions.ViewChannel)
                {
                    await FollowupAsync($"❌ Bob cannot view or send messages in <#{systemChannel.Id}>.\n- Give Bob `View Channel` and `Send Messages` permissions.", ephemeral: true);
                    return;
                }
            }

            // Update server welcome information
            var server = await dbContext.GetServer(Context.Guild.Id);

            if (server.Welcome != welcome)
            {
                server.Welcome = welcome;
                await dbContext.SaveChangesAsync();
            }

            // Send response based on welcome setting
            if (welcome)
            {
                await FollowupAsync(systemChannel == null
                    ? "❌ Bob knows to welcome users now, but you **need** to set a **System Messages** channel in settings for this to take effect."
                    : $"✅ Bob will now greet people in <#{systemChannel.Id}>", ephemeral: true);
            }
            else
            {
                await FollowupAsync("✅ Bob will not greet people anymore.", ephemeral: true);
            }
        }

        [SlashCommand("set-message", "Create a custom welcome message for your server!")]
        public async Task SetCustomWelcomeMessage([Summary("message", "Whatever you want said to your users. Type @ where you want the ping.")] string message)
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if the user has manage channels permissions.
            if (!discordUser.GuildPermissions.Administrator)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You do not have a **System Messages** channel set in your server.\n- You can change this in the **Overview** tab of your server's settings.", ephemeral: true);
                    return;
                }

                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission `Manage Channel`.", ephemeral: true);
                    return;
                }
            }

            // Check if the user has premium.
            if (await Premium.IsPremiumAsync(Context.Interaction.Entitlements, Context.User.Id) == false)
            {
                await FollowupAsync($"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}", components: Premium.GetComponents(), ephemeral: true);
                return;
            }

            // Check if the message is within Discord's length requirements.
            if (message.Length + (34 * message.Count(c => c == '@')) > 2000)
            {
                await FollowupAsync($"❌ The message length either exceeds Discord's **2000** character limit or could when mentions are inserted.\n- Try shortening your message.\n- Every mention is assumed to be a length of **32** characters plus **3** formatting characters.", ephemeral: true);
                return;
            }

            // Update server welcome information.
            var server = await dbContext.GetServer(Context.Guild.Id);

            // Only write to DB if needed.
            if (server.CustomWelcomeMessage != message)
            {
                server.CustomWelcomeMessage = message;
                await dbContext.SaveChangesAsync();
            }

            if (server.Welcome)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync($"❌ Bob knows to welcome users now, and what to say, but you **need** to set a **System Messages** channel in settings for this to take effect.\nYour welcome message will look like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                }
                else
                {
                    await FollowupAsync($"✅ Bob will now greet people in <#{systemChannel.Id}> like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
                }
            }
            else
            {
                await FollowupAsync($"✅ Bob knows what to say, but you **need** to enable welcome messages with {Help.GetCommandMention("welcome toggle")} for it to take effect.\nYour welcome message will look like so:\n\n{Welcome.FormatCustomMessage(message, Context.User.Mention)}", ephemeral: true);
            }
        }

        // Make sure you have this using statement for your new helper class
        // using YourProject.Helpers; // Or wherever you placed ImageProcessor.cs

        [SlashCommand("set-image", "Set a custom welcome image for your server!")]
        public async Task SetCustomWelcomeImage(
            [Summary("image", "The image you would like to use (PNG, JPG, JPEG, WEBP, GIF, BMP).")]
    Attachment attachment
        )
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if the user has manage channels permissions.
            if (!discordUser.GuildPermissions.Administrator)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync(
                        "❌ You do not have a **System Messages** channel set in your server.\n" +
                        "- You can change this in the **Overview** tab of your server's settings.",
                        ephemeral: true
                    );
                    return;
                }

                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync(
                        $"❌ You do not have permissions to manage <#{systemChannel.Id}> " +
                        "(The system channel where welcome messages are sent)\n" +
                        "- Try asking a user with the permission `Manage Channel`.",
                        ephemeral: true
                    );
                    return;
                }
            }

            // Check if the user has premium.
            if (await Premium.IsPremiumAsync(Context.Interaction.Entitlements, Context.User.Id) == false)
            {
                await FollowupAsync(
                    $"✨ This is a *premium* feature.\n- {Premium.HasPremiumMessage}",
                    components: Premium.GetComponents(),
                    ephemeral: true
                );
                return;
            }

            string contentType = attachment.ContentType?.ToLower();
            if (string.IsNullOrEmpty(contentType) || (contentType != "image/png" && contentType != "image/jpeg" && contentType != "image/jpg" && contentType != "image/webp" && contentType != "image/gif" && contentType != "image/bmp"))
            {
                await FollowupAsync(
                    "❌ The image must be in either **PNG**, **JPG**, **JPEG**, **WEBP**, **GIF**, or **BMP** format.",
                    ephemeral: true
                );
                return;
            }

            var image = await GetFromAPI(attachment.Url);

            if (image == null || image.Length == 0)
            {
                await FollowupAsync("❌ Failed to retrieve the image. Please try again later.", ephemeral: true);
                return;
            }

            byte[] compressedImage;
            try
            {
                if (contentType == "image/webp")
                {
                    compressedImage = image;
                }
                else
                {
                    compressedImage = ImageProcessor.ConvertToAnimatedWebP(image);
                }
            }
            catch (Exception e)
            {
                await FollowupAsync(
                    "❌ The image could not be processed. It might be corrupted or in an unsupported variation of the format. Please try again.",
                    ephemeral: true
                );
                Console.WriteLine($"ImageSharp conversion failed: {e}");
                return;
            }

            // Check if the image is within Discord's size requirements.
            if (compressedImage.Length > 8000000)
            {
                await FollowupAsync(
                    "❌ The image size exceeds Discord's **8MB** limit.\n" +
                    "- Try compressing the image or using a smaller one.\n" +
                    "- This is after compressing it with WEBP.",
                    ephemeral: true
                );
                return;
            }

            Console.WriteLine($"Image size: {compressedImage.Length}");

            // Update server welcome information.
            var server = await dbContext.GetServer(Context.Guild.Id);

            // Only write to DB if needed.
            if (server.HasWelcomeImage != true)
            {
                server.HasWelcomeImage = true;
                await dbContext.SaveChangesAsync();
            }

            var welcomeImage = await dbContext.GetWelcomeImage(Context.Guild.Id);

            if (welcomeImage != null)
            {
                welcomeImage.Image = compressedImage;
                await dbContext.SaveChangesAsync();
            }
            else
            {
                // Add image to database.
                WelcomeImage newImage = new()
                {
                    Id = Context.Guild.Id,
                    Image = compressedImage
                };
                await dbContext.AddWelcomeImage(newImage);
            }

            if (server.Welcome)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync(
                        "❌ Bob knows to welcome users now, and what image to use, but you **need** to set a **System Messages** channel in settings for this to take effect.",
                        ephemeral: true
                    );
                }
                else
                {
                    await FollowupAsync(
                        $"✅ Bob will now greet people in <#{systemChannel.Id}> with the given image.",
                        ephemeral: true
                    );
                }
            }
            else
            {
                await FollowupAsync(
                    $"✅ Bob knows what image to use, but you **need** to enable welcome messages with {Help.GetCommandMention("welcome toggle")} for it to take effect.",
                    ephemeral: true
                );
            }
        }

        [SlashCommand("remove-image", "Removes the custom welcome image from your server. Does not disable general welcome messages.")]
        public async Task RemoveCustomWelcomeImage()
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if the user has manage channels permissions.
            if (!discordUser.GuildPermissions.Administrator)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You do not have a **System Messages** channel set in your server.\n- You can change this in the **Overview** tab of your server's settings.", ephemeral: true);
                    return;
                }

                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission `Manage Channel`.", ephemeral: true);
                    return;
                }
            }
            // Update server welcome information.
            else
            {
                Server server = await dbContext.GetServer(Context.Guild.Id);

                // Only write to DB if needed.
                if (server.HasWelcomeImage == true)
                {
                    server.HasWelcomeImage = false;
                    await dbContext.SaveChangesAsync();

                    var welcomeImage = await dbContext.GetWelcomeImage(Context.Guild.Id);
                    if (welcomeImage != null)
                    {
                        await dbContext.RemoveWelcomeImage(welcomeImage);
                    }
                }

                await FollowupAsync(text: $"✅ Bob will no longer greet people with the custom image.", ephemeral: true);
            }
        }

        [SlashCommand("remove-message", "Removes the custom welcome message from your server. Does not disable general welcome messages.")]
        public async Task RemoveCustomWelcomeMessage()
        {
            await DeferAsync(ephemeral: true);
            var discordUser = Context.Guild.GetUser(Context.User.Id);
            var systemChannel = Context.Guild.SystemChannel;

            // Check if the user has manage channels permissions.
            if (!discordUser.GuildPermissions.Administrator)
            {
                if (systemChannel == null)
                {
                    await FollowupAsync("❌ You do not have a **System Messages** channel set in your server.\n- You can change this in the **Overview** tab of your server's settings.", ephemeral: true);
                    return;
                }

                if (!discordUser.GetPermissions(systemChannel).ManageChannel)
                {
                    await FollowupAsync($"❌ You do not have permissions to manage <#{systemChannel.Id}> (The system channel where welcome messages are sent)\n- Try asking a user with the permission `Manage Channel`.", ephemeral: true);
                    return;
                }
            }
            // Update server welcome information.
            else
            {
                Server server = await dbContext.GetServer(Context.Guild.Id);

                // Only write to DB if needed.
                if (!string.IsNullOrEmpty(server.CustomWelcomeMessage))
                {
                    server.CustomWelcomeMessage = "";
                    await dbContext.SaveChangesAsync();
                }

                await FollowupAsync(text: $"✅ Bob will no longer greet people with the custom message.", ephemeral: true);
            }
        }
    }
}
