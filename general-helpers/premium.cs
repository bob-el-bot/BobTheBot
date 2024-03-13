using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Challenges;
using Discord.Rest;

namespace PremiumInterface
{
    public static class Premium
    {
        // Limits
        public static readonly int ChallengeLimit = 1;
       
        // Premium Message
        public static readonly string HasPremiumMessage = "If you already have premium (💜 **thanks so much!**) simply use `/premium` to unlock all of the features.";

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
            return entitlements.Count > 0 && entitlements.FirstOrDefault(x => x.SkuId == 1169107771673812992) is not null;
        }
    }
}