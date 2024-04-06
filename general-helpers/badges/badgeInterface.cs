using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using Badges;
using Database;
using Database.Types;

namespace BadgeInterface
{
    public static class Badge
    {
        public static string GetBadgesInfoString()
        {
            StringBuilder stringBuilder = new();

            foreach (Badges.Badges badge in Enum.GetValues(typeof(Badges.Badges)))
            {
                if (badge != Badges.Badges.None)
                {
                    stringBuilder.AppendLine($"**{GetBadgeEmoji(badge)} {badge}** - {GetBadgeInfoString(badge)}\n");
                }
            }

            return stringBuilder.ToString();
        }

        public static string GetBadgeInfoString(Badges.Badges badge)
        {
            if (badge != Badges.Badges.None)
            {
                if (BadgeDescriptions.Descriptions.TryGetValue(badge, out var badgeInfo))
                {
                    return $"\"{badgeInfo.Description}\"\n- {badgeInfo.HowToGet}";
                }
            }

            return "";
        }

        public static string GetBadgeEmoji(Badges.Badges badge)
        {
            if (badge != Badges.Badges.None)
            {
                if (BadgeDescriptions.Descriptions.TryGetValue(badge, out var badgeInfo))
                {
                    return badgeInfo.Emoji;
                }
            }

            return "";
        }

        public static string GetBadgesProfileString(ulong userBadges)
        {
            List<Badges.Badges> userBadgeList = GetUserBadges(userBadges);

            StringBuilder stringBuilder = new();
            foreach (Badges.Badges badge in userBadgeList)
            {
                stringBuilder.Append($"{badge} ");
            }

            return stringBuilder.ToString();
        }

        public static List<Badges.Badges> GetUserBadges(ulong userBadges)
        {
            List<Badges.Badges> userBadgesList = new();

            foreach (Badges.Badges badge in Enum.GetValues(typeof(Badges.Badges)))
            {
                if (badge != Badges.Badges.None && ((userBadges & (ulong)badge) == (ulong)badge))
                {
                    userBadgesList.Add(badge);
                }
            }

            return userBadgesList;
        }

        public static async Task GiveUserBadge(User user, Badges.Badges badge)
        {
            if (GetUserBadges(user.EarnedBadges).Contains(badge) == false)
            {
                // Retrieve the user's current badges bitmask
                ulong currentBadges = user.EarnedBadges;

                // Calculate the bitmask for the badge the user has earned
                ulong earnedBadgeBit = (ulong)badge;

                // Set the corresponding bit in the current badges bitmask
                currentBadges |= earnedBadgeBit;

                // Update the user's EarnedBadges property
                user.EarnedBadges = currentBadges;

                using var context = new BobEntities();
                await context.UpdateUser(user);
            }
        }
    }
}