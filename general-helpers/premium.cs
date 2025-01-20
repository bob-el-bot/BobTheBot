using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.Rest;
using Moderation;

namespace PremiumInterface
{
    public static class Premium
    {
        // Limits
        public static readonly int ChallengeLimit = 1;
        public static readonly int MaxScheduledAnnouncements = 0;
        public static readonly int MaxScheduledMessages = 1;

        // Premium Message
        public static readonly string HasPremiumMessage = "If you already have premium (💜 **thanks so much!**) simply use `/premium` to unlock all of the features.";

        public static MessageComponent GetComponents()
        {
            var components = new ComponentBuilder();

            var monthlyPremiumButton = new ButtonBuilder();
            monthlyPremiumButton.WithSkuId(1169107771673812992);
            monthlyPremiumButton.WithStyle(ButtonStyle.Premium);

            var lifetimePremiumButton = new ButtonBuilder();
            lifetimePremiumButton.WithSkuId(1282452500913328180);
            lifetimePremiumButton.WithStyle(ButtonStyle.Premium);

            return components.WithButton(monthlyPremiumButton).WithButton(lifetimePremiumButton).Build();
        }

        /// <summary>
        /// Checks if the premium subscription is still valid based on the expiration date.
        /// </summary>
        /// <param name="premiumExpiration">The expiration date and time of the premium subscription.</param>
        /// <returns>True if the premium subscription is still valid; otherwise, false.</returns>
        public static bool IsValidPremium(DateTimeOffset premiumExpiration)
        {
            if (premiumExpiration.CompareTo(DateTimeOffset.Now) <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Determines whether a user is entitled to premium access based on their entitlements.
        /// </summary>
        /// <param name="entitlements">The collection of entitlements for the user.</param>
        /// <returns>True if the user is entitled to premium access, otherwise false.</returns>
        public static bool IsPremium(IReadOnlyCollection<RestEntitlement> entitlements)
        {
            // Ensure entitlements is not null before iterating
            if (entitlements == null) return false;

            foreach (var entitlement in entitlements)
            {
                // Ensure SkuId has a value before comparing
                if (entitlement.SkuId != 0 && (entitlement.SkuId == 1169107771673812992 || entitlement.SkuId == 1282452500913328180))
                {
                    Console.WriteLine("User has premium.");
                    return true;
                }
            }

            return false;
        }
    }
}