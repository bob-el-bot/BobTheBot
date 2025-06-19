using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Challenges;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Rest;
using Bob.Moderation;
using Bob.Commands.Helpers;

namespace Bob.PremiumInterface
{
    public static class Premium
    {
        // Limits
        public static readonly int ChallengeLimit = 1;
        public static readonly int MaxScheduledAnnouncements = 0;
        public static readonly int MaxScheduledMessages = 1;

        // Premium Message
        public static readonly string HasPremiumMessage = $"If you already have premium (💜 **thanks so much!**) simply use {Help.GetCommandMention("premium")} to unlock all of the features.";

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

            if (entitlements == null) return false;

            Console.WriteLine("Entitlements:");

            foreach (var e in entitlements)
            {
                Console.WriteLine($"Entitlement SKU: {e.SkuId}, EndsAt: {e.EndsAt}");
            }

            return entitlements.Any(x => x.SkuId == 1169107771673812992 || x.SkuId == 1282452500913328180);
        }

        /// <summary>
        /// Determines whether a user is entitled to premium access based on their entitlements. This variant of the method is for when the user DB object is available.
        /// </summary>
        /// <param name="entitlements">The collection of entitlements for the user.</param>
        /// <param name="user">The user to check for premium access.</param>
        /// <returns>True if the user is entitled to premium access, otherwise false.</returns>
        public static bool IsPremium(IReadOnlyCollection<RestEntitlement> entitlements, User user)
        {
            // Check if entitlements is null before calling Any()
            if (entitlements?.Any(x => x.SkuId == 1169107771673812992 || x.SkuId == 1282452500913328180) == true)
            {
                return true;
            }

            return IsValidPremium(user.PremiumExpiration);
        }

        /// <summary>
        /// Determines whether a user is entitled to premium access based on their entitlements. This variant of the method is for when the user DB object is not available.
        /// </summary>
        /// <param name="entitlements">The collection of entitlements for the user.</param>
        /// <param name="userId">The ID of the user to check for premium access.</param>
        /// <returns>True if the user is entitled to premium access, otherwise false.</returns>
        public static async Task<bool> IsPremiumAsync(IReadOnlyCollection<RestEntitlement> entitlements, ulong userId)
        {
            // Check if entitlements is null before calling Any()
            if (entitlements?.Any(x => x.SkuId == 1169107771673812992 || x.SkuId == 1282452500913328180) == true)
            {
                return true;
            }

            User user;
            using var context = new BobEntities();
            user = await context.GetUser(userId);

            return IsValidPremium(user.PremiumExpiration);
        }
    }
}