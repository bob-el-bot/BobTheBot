using System;
using System.Threading.Tasks;
using ColorMethods;
using Commands.Helpers;
using Database;
using Database.Types;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PremiumInterface;
using BadgeInterface;
using System.Runtime.InteropServices;

namespace Commands
{
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
    [Group("profile", "All profile commands.")]
    public class ProfileGroup : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("display", "View a user's profile.")]
        public async Task Display([Summary("user", "The user whose profile you would like displayed (leave empty to display your own).")] SocketUser user = null)
        {
            user ??= Context.User;

            if (user.IsBot)
            {
                await RespondAsync("❌ Bot users do not have profiles, or stats for that matter...", ephemeral: true);
            }
            else
            {
                await DeferAsync();

                // Get Stats
                User userToDisplay;
                using var context = new BobEntities();
                userToDisplay = await context.GetUser(user.Id);

                float rpsWinPercent = (float)Math.Round(userToDisplay.TotalRockPaperScissorsGames != 0 ? userToDisplay.RockPaperScissorsWins / userToDisplay.TotalRockPaperScissorsGames * 100 : 0, 2);
                float tttWinPercent = (float)Math.Round(userToDisplay.TotalTicTacToeGames != 0 ? userToDisplay.TicTacToeWins / userToDisplay.TotalTicTacToeGames * 100 : 0, 2);
                float triviaWinPercent = (float)Math.Round(userToDisplay.TotalTriviaGames != 0 ? userToDisplay.TriviaWins / userToDisplay.TotalTriviaGames * 100 : 0, 2);
                float connect4WinPercent = (float)Math.Round(userToDisplay.TotalConnect4Games != 0 ? userToDisplay.Connect4Wins / userToDisplay.TotalConnect4Games * 100 : 0, 2);

                // Check Premium
                bool hasPremium = Premium.IsValidPremium(userToDisplay.PremiumExpiration);

                Color color = hasPremium ? Colors.TryGetColor(userToDisplay.ProfileColor) : 0x2C2F33;

                // Format Description
                string premium = hasPremium ? "💜✨ *Premium*" : "";
                string description = $"{premium}\n**Badges:** {Badge.GetBadgesProfileString(userToDisplay.EarnedBadges)}";

                // Create Embed
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder().WithName($"{user.Username}'s Profile").WithIconUrl(user.GetAvatarUrl()),
                    Color = color,
                    Description = description
                };

                embed.AddField(name: $"✂️ Rock Paper Scissors", value: $"Total Wins: `{userToDisplay.RockPaperScissorsWins}`\nTotal Games: `{userToDisplay.TotalRockPaperScissorsGames}`\nWin %: `{rpsWinPercent}%`", inline: true)
                .AddField(name: $"⭕ Tic Tac Toe", value: $"Total Wins: `{userToDisplay.TicTacToeWins}`\nTotal Games: `{userToDisplay.TotalTicTacToeGames}`\nWin %: `{tttWinPercent}%`", inline: true)
                .AddField(name: $"❓ Trivia", value: $"Total Wins: `{userToDisplay.TriviaWins}`\nTotal Games: `{userToDisplay.TotalTriviaGames}`\nWin %: `{triviaWinPercent}%`", inline: true)
                .AddField(name: $"🔵 Connect 4", value: $"Total Wins: `{userToDisplay.Connect4Wins}`\nTotal Games: `{userToDisplay.TotalConnect4Games}`\nWin %: `{connect4WinPercent}%`", inline: true)
                .AddField(name: $"🏅 Win Streak", value: $"`{userToDisplay.WinStreak}`");

                // Respond
                await FollowupAsync(embed: embed.Build());
            }
        }

        [SlashCommand("confessions-toggle", "Enable or disable your DMs for /confess messages sent to you.")]
        public async Task ConfessionsToggle([Summary("open", "If checked (true), Bob will allow users to use send messages to you with /confess.")] bool open)
        {

            await DeferAsync(ephemeral: true);

            User user;
            using (var context = new BobEntities())
            {
                user = await context.GetUser(Context.User.Id);

                if (user.ConfessionsOff == open)
                {
                    user.ConfessionsOff = !open;
                    await context.UpdateUser(user);
                }
            }

            if (open)
            {
                await FollowupAsync(text: "✅ Your DMs are now open to people using `/confess`.", ephemeral: true);
            }
            else
            {
                await FollowupAsync(text: "✅ Your DMs will now appear closed to people using `/confess`.", ephemeral: true);
            }
        }

        [SlashCommand("set-color", "Set your profile's embed color.")]
        public async Task SetColor([Summary("color", "A color name (purple), or valid hex code (#8D52FD).")] string color)
        {
            await DeferAsync(ephemeral: true);

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(Context.User.Id);

            Color finalColor = Colors.TryGetColor(color);

            // Check if the user has premium.
            if (Premium.IsValidPremium(user.PremiumExpiration) == false)
            {
                await FollowupAsync(text: $"✨ This is a *premium* feature.\n-{Premium.HasPremiumMessage}", ephemeral: true);
            }
            else if (finalColor == 0)
            {
                await FollowupAsync(text: $"❌ `{color}` is an invalid color. Here is a list of valid colors:\n- {Colors.GetSupportedColorsString()}.\n- Valid hex codes are also accepted.\n- If you think this is a mistake, let us know here: [Bob's Official Server](https://discord.gg/HvGMRZD8jQ)", ephemeral: true);
            }
            else
            {
                // Only write to DB if needed.
                if (user.ProfileColor != color)
                {
                    user.ProfileColor = color;
                    await context.UpdateUser(user);
                }

                // Create Embed
                var embed = new EmbedBuilder
                {
                    Title = "⬅️ Your new profile color.",
                    Color = finalColor,
                };

                // Respond
                await FollowupAsync(embed: embed.Build());
            }
        }

        [SlashCommand("badge-info", "Get info about profile badges.")]
        public async Task BadgeInfo([Summary("badge", "The badge you want to learn about (leave empty to show all).")] Badges.Badges? badge = null)
        {
            var embed = new EmbedBuilder
            {
                Color = Bot.theme,
                Footer = new EmbedFooterBuilder
                {
                    Text = "Have a badge idea? Let us know in Bob's official server."
                }
            };

            if (badge != null)
            {
                embed.Title = $"{Badge.GetBadgeEmoji((Badges.Badges)badge)} {Badge.GetBadgeDisplayName((Badges.Badges)badge)} Info";
                embed.Description = Badge.GetBadgeInfoString((Badges.Badges)badge);

                await RespondAsync(embed: embed.Build());
            }
            else
            {
                embed.Title = "All Profile Badges Info";
                embed.Description = Badge.GetBadgesInfoString();

                await RespondAsync(embed: embed.Build());
            }
        }
    }
}
