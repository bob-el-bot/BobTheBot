using System;
using System.Collections.Generic;
using Commands.Helpers;
using Discord.Interactions;
using Time.Timezones;

namespace Time.Timestamps
{
    /// <summary>
    /// Represents a utility class for generating timestamps in various formats.
    /// </summary>
    public static class Timestamp
    {
        /// <summary>
        /// Enumeration of timestamp formats.
        /// </summary>
        public enum Formats
        {
            /// <summary>
            /// Tuesday, April 30, 2024 11:14 AM
            /// </summary>
            Exact = 'F',
            /// <summary>
            /// April 30, 2024 11:14 AM
            /// </summary>
            Detailed = 'f',
            /// <summary>
            /// April 30, 2024
            /// </summary>
            MM_DD_YY_InWords = 'D',
            /// <summary>
            /// 04/30/2024
            /// </summary>
            MM_DD_YY = 'd',
            /// <summary>
            /// 11:14 AM
            /// </summary>
            Time = 't',
            /// <summary>
            /// 11:14:00 AM
            /// </summary>
            ExactTime = 'T',
            /// <summary>
            /// 30 seconds ago
            /// </summary>
            Relative = 'R'
        }

        /// <summary>
        /// Generates a timestamp from a DateTime object in the specified format.
        /// </summary>
        /// <param name="dateTime">The DateTime object to generate the timestamp from.</param>
        /// <param name="format">The format of the timestamp.</param>
        /// <param name="timeZone">The timezone to use for the timestamp. Null by default</param>
        /// <returns>The generated timestamp string.</returns>
        public static string FromDateTime(DateTime dateTime, Formats format, Timezone? timeZone = null)
        {
            // Ensure the dateTime is within the range supported by DateTimeOffset
            if (dateTime < DateTimeOffset.MinValue.UtcDateTime)
            {
                dateTime = DateTimeOffset.MinValue.UtcDateTime;
            }
            else if (dateTime > DateTimeOffset.MaxValue.UtcDateTime)
            {
                dateTime = DateTimeOffset.MaxValue.UtcDateTime;
            }

            // Log the time before conversion
            Console.WriteLine("FromDateTime() before goes to DateTimeOffset: " + dateTime);

            // Assign the correct timezone offset
            var dateTimeOffset = new DateTimeOffset(dateTime);

            // Log the DateTimeOffset after conversion
            Console.WriteLine("FromDateTime() after gone to DateTimeOffset: " + dateTimeOffset);
            
            long unixTime = timeZone.HasValue ? dateTimeOffset.ToUnixTimeSeconds() : dateTimeOffset.ToUniversalTime().ToUnixTimeSeconds();

            // Return the formatted timestamp string
            return $"<t:{unixTime}:{(char)format}>";
        }

        /// <summary>
        /// Generates a timestamp from a DateTimeOffset object in the specified format.
        /// </summary>
        /// <param name="dateTime">The DateTimeOffset object to generate the timestamp from.</param>
        /// <param name="format">The format of the timestamp.</param>
        /// <returns>The generated timestamp string.</returns>
        public static string FromDateTimeOffset(DateTimeOffset dateTime, Formats format)
        {
            return $"<t:{dateTime.ToUnixTimeSeconds()}:{(char)format}>";
        }

        /// <summary>
        /// Generates a timestamp from a string representation of a DateTime in the specified format.
        /// </summary>
        /// <param name="dateTime">The string representation of the DateTime to generate the timestamp from.</param>
        /// <param name="format">The format of the timestamp.</param>
        /// <returns>The generated timestamp string.</returns>
        public static string FromString(string dateTime, Formats format)
        {
            var parsedDateTime = DateTime.Parse(dateTime);

            // Convert to UTC if necessary
            if (parsedDateTime.Kind != DateTimeKind.Utc)
            {
                parsedDateTime = parsedDateTime.ToUniversalTime();
            }

            // Ensure the parsedDateTime is within the range supported by DateTimeOffset
            if (parsedDateTime < DateTimeOffset.MinValue.UtcDateTime)
            {
                parsedDateTime = DateTimeOffset.MinValue.UtcDateTime;
            }
            else if (parsedDateTime > DateTimeOffset.MaxValue.UtcDateTime)
            {
                parsedDateTime = DateTimeOffset.MaxValue.UtcDateTime;
            }

            // Create a DateTimeOffset with the validated parsedDateTime
            var dateTimeOffset = new DateTimeOffset(parsedDateTime, TimeSpan.Zero);

            return $"<t:{dateTimeOffset.ToUnixTimeSeconds()}:{(char)format}>";
        }
    }
}
