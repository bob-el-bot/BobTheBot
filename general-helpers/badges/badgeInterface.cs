using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Badges;
using Database;
using Database.Types;

namespace BadgeInterface
{
    /// <summary>
    /// Provides methods for managing user badges.
    /// </summary>
    public static class Badge
    {
        /// <summary>
        /// Retrieves information about all available badges.
        /// </summary>
        /// <returns>A string containing information about each badge.</returns>
        public static string GetBadgesInfoString()
        {
            StringBuilder stringBuilder = new();

            foreach (Badges.Badges badge in Enum.GetValues(typeof(Badges.Badges)))
            {
                stringBuilder.AppendLine($"**{GetBadgeEmoji(badge)} {GetBadgeDisplayName(badge)}** - {GetBadgeInfoString(badge)}\n");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves detailed information about a specific badge.
        /// </summary>
        /// <param name="badge">The badge to retrieve information for.</param>
        /// <returns>A string containing the badge's description and how to obtain it.</returns>
        public static string GetBadgeInfoString(Badges.Badges badge)
        {
            if (BadgeDescriptions.Descriptions.TryGetValue(badge, out var badgeInfo))
            {
                return $"\"{badgeInfo.Description}\"\n- {badgeInfo.HowToGet}";
            }

            return "";
        }

        /// <summary>
        /// Retrieves the emoji associated with a specific badge.
        /// </summary>
        /// <param name="badge">The badge to retrieve the emoji for.</param>
        /// <returns>The emoji associated with the badge.</returns>
        public static string GetBadgeEmoji(Badges.Badges badge)
        {
            if (BadgeDescriptions.Descriptions.TryGetValue(badge, out var badgeInfo))
            {
                return badgeInfo.Emoji;
            }

            return "";
        }

        /// <summary>
        /// Retrieves the display name of a specific badge.
        /// </summary>
        /// <param name="badge">The badge to retrieve the display name for.</param>
        /// <returns>The display name of the badge.</returns>
        public static string GetBadgeDisplayName(Badges.Badges badge)
        {
            if (BadgeDescriptions.Descriptions.TryGetValue(badge, out var badgeInfo))
            {
                return badgeInfo.DisplayName;
            }

            return "";
        }

        /// <summary>
        /// Retrieves a string representation of a user's earned badges.
        /// </summary>
        /// <param name="userBadges">An integer representing the user's earned badges.</param>
        /// <returns>A string containing the display names and emojis of the user's earned badges.</returns>
        public static string GetBadgesProfileString(ulong userBadges)
        {
            List<Badges.Badges> userBadgeList = GetUserBadges(userBadges);

            StringBuilder stringBuilder = new();
            foreach (Badges.Badges badge in userBadgeList)
            {
                stringBuilder.Append($"{GetBadgeEmoji(badge)} {GetBadgeDisplayName(badge)} ");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves a list of badges that a user has earned.
        /// </summary>
        /// <param name="userBadges">An integer representing the user's earned badges.</param>
        /// <returns>A list of badges that the user has earned.</returns>
        public static List<Badges.Badges> GetUserBadges(ulong userBadges)
        {
            List<Badges.Badges> userBadgesList = new();

            foreach (Badges.Badges badge in Enum.GetValues(typeof(Badges.Badges)))
            {
                if ((userBadges & (ulong)badge) == (ulong)badge)
                {
                    userBadgesList.Add(badge);
                }
            }

            return userBadgesList;
        }

        /// <summary>
        /// Awards a badge to a user if they have not already earned it.
        /// </summary>
        /// <param name="user">The user to award the badge to.</param>
        /// <param name="badge">The badge to award.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task GiveUserBadge(User user, Badges.Badges badge)
        {
            if (GetUserBadges(user.EarnedBadges).Contains(badge) == false)
            {
                ulong currentBadges = user.EarnedBadges;
                ulong earnedBadgeBit = (ulong)badge;

                int endingNumber = GetBadgeTier(badge);
                if (endingNumber != -1 && endingNumber > 1)
                {
                    Badges.Badges? badgeToRemove = GetBadgeWithLesserTier(badge);
                    if (badgeToRemove != null)
                    {
                        ulong mask = ~(ulong)badgeToRemove;
                        currentBadges &= mask;
                    }
                }

                currentBadges |= earnedBadgeBit;

                user.EarnedBadges = currentBadges;

                using var context = new BobEntities();
                await context.UpdateUser(user);
            }
        }

        /// <summary>
        /// Removes a specified badge from a user's earned badges.
        /// </summary>
        /// <param name="user">The user from whom to remove the badge.</param>
        /// <param name="badge">The badge to remove from the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RemoveUserBadge(User user, Badges.Badges badge)
        {
            if (GetUserBadges(user.EarnedBadges).Contains(badge))
            {
                ulong currentBadges = user.EarnedBadges;

                ulong badgeToRemoveBit = (ulong)badge;

                ulong mask = ~badgeToRemoveBit;
                currentBadges &= mask;

                user.EarnedBadges = currentBadges;

                using var context = new BobEntities();
                await context.UpdateUser(user);
            }
        }

        /// <summary>
        /// Extracts the ending number from the string representation of the given badge.
        /// </summary>
        /// <param name="badge">The badge whose ending number will be extracted.</param>
        /// <returns>
        /// The ending number of the given badge, or -1 if no ending number is found in the string representation.
        /// </returns>
        private static int GetBadgeTier(Badges.Badges badge)
        {
            string badgeName = badge.ToString();
            Match match = Regex.Match(badgeName, @"\d$");
            if (match.Success)
            {
                return Convert.ToInt32(match.Value);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Extracts the base name from the string representation of the given badge.
        /// </summary>
        /// <param name="badge">The badge whose base name will be extracted.</param>
        /// <returns>
        /// The base name of the given badge, which excludes the ending number.
        /// </returns>
        private static string GetBadgeBaseName(Badges.Badges badge)
        {
            return badge.ToString()[..^1];
        }

        /// <summary>
        /// Finds the badge in the <see cref="Badges.Badges"/> enum with an ending number that is one less than the given badge.
        /// </summary>
        /// <param name="badge">The badge whose ending number is used to find the previous tier badge.</param>
        /// <returns>
        /// The badge with an ending number that is one less than the ending number of the given badge, 
        /// or <see langword="null"/> if no matching badge is found.
        /// </returns>
        public static Badges.Badges? GetBadgeWithLesserTier(Badges.Badges badge)
        {
            string baseBadgeName = GetBadgeBaseName(badge);
            string targetBadgeName = $"{baseBadgeName}{GetBadgeTier(badge) - 1}";

            if (Enum.TryParse(targetBadgeName, out Badges.Badges targetBadge))
            {
                return targetBadge;
            }
            else
            {
                return null;
            }
        }
    }
}