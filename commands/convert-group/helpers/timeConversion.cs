using System;
using System.Collections.Generic;
using TimeZoneConverter;

namespace Bob.Commands.Helpers
{
    public static class TimeConversion
    {
        private static readonly Dictionary<TimeSpan, string> TimeEmojis = new()
        {
            { new TimeSpan(0, 0, 0), "🕛" },  // 12:00 AM
            { new TimeSpan(0, 30, 0), "🕧" }, // 12:30 AM
            { new TimeSpan(1, 0, 0), "🕐" },  // 1:00 AM
            { new TimeSpan(1, 30, 0), "🕜" }, // 1:30 AM
            { new TimeSpan(2, 0, 0), "🕑" },  // 2:00 AM
            { new TimeSpan(2, 30, 0), "🕝" }, // 2:30 AM
            { new TimeSpan(3, 0, 0), "🕒" },  // 3:00 AM
            { new TimeSpan(3, 30, 0), "🕞" }, // 3:30 AM
            { new TimeSpan(4, 0, 0), "🕓" },  // 4:00 AM
            { new TimeSpan(4, 30, 0), "🕟" }, // 4:30 AM
            { new TimeSpan(5, 0, 0), "🕔" },  // 5:00 AM
            { new TimeSpan(5, 30, 0), "🕠" }, // 5:30 AM
            { new TimeSpan(6, 0, 0), "🕕" },  // 6:00 AM
            { new TimeSpan(6, 30, 0), "🕡" }, // 6:30 AM
            { new TimeSpan(7, 0, 0), "🕖" },  // 7:00 AM
            { new TimeSpan(7, 30, 0), "🕢" }, // 7:30 AM
            { new TimeSpan(8, 0, 0), "🕗" },  // 8:00 AM
            { new TimeSpan(8, 30, 0), "🕣" }, // 8:30 AM
            { new TimeSpan(9, 0, 0), "🕘" },  // 9:00 AM
            { new TimeSpan(9, 30, 0), "🕤" }, // 9:30 AM
            { new TimeSpan(10, 0, 0), "🕙" }, // 10:00 AM
            { new TimeSpan(10, 30, 0), "🕥" },// 10:30 AM
            { new TimeSpan(11, 0, 0), "🕚" }, // 11:00 AM
            { new TimeSpan(11, 30, 0), "🕦" },// 11:30 AM
            { new TimeSpan(12, 0, 0), "🕛" }, // 12:00 PM
            { new TimeSpan(12, 30, 0), "🕧" },// 12:30 PM
            { new TimeSpan(13, 0, 0), "🕐" }, // 1:00 PM
            { new TimeSpan(13, 30, 0), "🕜" },// 1:30 PM
            { new TimeSpan(14, 0, 0), "🕑" }, // 2:00 PM
            { new TimeSpan(14, 30, 0), "🕝" },// 2:30 PM
            { new TimeSpan(15, 0, 0), "🕒" }, // 3:00 PM
            { new TimeSpan(15, 30, 0), "🕞" },// 3:30 PM
            { new TimeSpan(16, 0, 0), "🕓" }, // 4:00 PM
            { new TimeSpan(16, 30, 0), "🕟" },// 4:30 PM
            { new TimeSpan(17, 0, 0), "🕔" }, // 5:00 PM
            { new TimeSpan(17, 30, 0), "🕠" },// 5:30 PM
            { new TimeSpan(18, 0, 0), "🕕" }, // 6:00 PM
            { new TimeSpan(18, 30, 0), "🕡" },// 6:30 PM
            { new TimeSpan(19, 0, 0), "🕖" }, // 7:00 PM
            { new TimeSpan(19, 30, 0), "🕢" },// 7:30 PM
            { new TimeSpan(20, 0, 0), "🕗" }, // 8:00 PM
            { new TimeSpan(20, 30, 0), "🕣" },// 8:30 PM
            { new TimeSpan(21, 0, 0), "🕘" }, // 9:00 PM
            { new TimeSpan(21, 30, 0), "🕤" },// 9:30 PM
            { new TimeSpan(22, 0, 0), "🕙" }, // 10:00 PM
            { new TimeSpan(22, 30, 0), "🕥" },// 10:30 PM
            { new TimeSpan(23, 0, 0), "🕚" }, // 11:00 PM
            { new TimeSpan(23, 30, 0), "🕦" } // 11:30 PM
        };

        /// <summary>
        /// Returns the closest time emoji for the given time.
        /// </summary>
        /// <param name="time">The time for which to find the closest emoji.</param>
        /// <returns>The emoji corresponding to the closest half-hour interval.</returns>
        public static string GetClosestTimeEmoji(DateTime time)
        {
            var inputTime = time.TimeOfDay;
            TimeSpan closestTime = TimeSpan.Zero;
            double smallestDifference = double.MaxValue;

            foreach (var entry in TimeEmojis)
            {
                double diff = Math.Abs((inputTime - entry.Key).TotalMinutes);
                double difference = Math.Min(diff, 1440 - diff);
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    closestTime = entry.Key;
                }
            }

            return TimeEmojis[closestTime];
        }
    }
}