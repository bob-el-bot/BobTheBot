using System;
using System.Threading.Tasks;
using Challenges;

namespace PremiumInterface
{
    public static class Premium
    {
        public static readonly int ChallengeLimit = 1;

        /// <summary>
        /// Checks if the premium subscription is still valid based on the expiration date.
        /// </summary>
        /// <param name="premiumExpiration">The expiration date and time of the premium subscription.</param>
        /// <returns>True if the premium subscription is still valid; otherwise, false.</returns>
        public static bool HasValidPremium(DateTimeOffset premiumExpiration)
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
    }
}