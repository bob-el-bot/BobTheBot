using System;
using System.Collections.Generic;

namespace Time.Timezones
{
    /// <summary>
    /// Utility class for timezone conversion.
    /// </summary>
    public static class TimeConverter
    {
        private static readonly Dictionary<Timezone, string> TimezoneMappings = new()
        {
            { Timezone.DatelineStandardTime, "Dateline Standard Time" },     // UTC-12:00
            { Timezone.SamoaStandardTime, "Samoa Standard Time" },           // UTC-11:00
            { Timezone.HawaiianStandardTime, "Hawaiian Standard Time" },     // UTC-10:00
            { Timezone.AlaskanStandardTime, "Alaskan Standard Time" },       // UTC-09:00
            { Timezone.PacificStandardTime, "Pacific Standard Time" },       // UTC-08:00
            { Timezone.MountainStandardTime, "Mountain Standard Time" },     // UTC-07:00
            { Timezone.CentralStandardTime, "Central Standard Time" },       // UTC-06:00
            { Timezone.EasternStandardTime, "Eastern Standard Time" },       // UTC-05:00
            { Timezone.AtlanticStandardTime, "Atlantic Standard Time" },     // UTC-04:00
            { Timezone.ArgentinaStandardTime, "Argentina Standard Time" },   // UTC-03:00
            { Timezone.MidAtlanticStandardTime, "Mid-Atlantic Standard Time" }, // UTC-02:00
            { Timezone.AzoresStandardTime, "Azores Standard Time" },         // UTC-01:00
            { Timezone.GreenwichMeanTime, "GMT Standard Time" },             // UTC±00:00
            { Timezone.CentralEuropeanTime, "Central Europe Standard Time" },// UTC+01:00
            { Timezone.EasternEuropeanTime, "E. Europe Standard Time" },     // UTC+02:00
            { Timezone.MoscowStandardTime, "Russian Standard Time" },        // UTC+03:00
            { Timezone.ArabianStandardTime, "Arabian Standard Time" },       // UTC+04:00
            { Timezone.PakistanStandardTime, "Pakistan Standard Time" },     // UTC+05:00
            { Timezone.BangladeshStandardTime, "Bangladesh Standard Time" }, // UTC+06:00
            { Timezone.IndochinaTime, "SE Asia Standard Time" },             // UTC+07:00
            { Timezone.ChinaStandardTime, "China Standard Time" },           // UTC+08:00
            { Timezone.JapanStandardTime, "Tokyo Standard Time" },           // UTC+09:00
            { Timezone.AustralianEasternTime, "AUS Eastern Standard Time" }, // UTC+10:00
            { Timezone.SolomonIslandsTime, "Central Pacific Standard Time" },// UTC+11:00
            { Timezone.NewZealandStandardTime, "New Zealand Standard Time" } // UTC+12:00
        };

        /// <summary>
        /// Gets the timezone ID for the specified timezone.
        /// </summary>
        /// <param name="timezone">The timezone to get the ID for.</param>
        /// <returns>The timezone ID.</returns>
        public static string GetTimezoneId(Timezone timezone)
        {
            return TimezoneMappings[timezone];
        }

        /// <summary>
        /// Converts a local time specified by month, day, hour, and minute into UTC time, based on the provided timezone.
        /// </summary>
        /// <param name="month">The month of the local time.</param>
        /// <param name="day">The day of the local time.</param>
        /// <param name="hour">The hour of the local time (24-hour format).</param>
        /// <param name="minute">The minute of the local time.</param>
        /// <param name="timezone">The timezone in which the local time is specified.</param>
        /// <returns>The equivalent UTC time of the specified local time.</returns>
        public static DateTime ConvertToUtcTime(int month, int day, int hour, int minute, Timezone timezone)
        {
            // Create a DateTime with DateTimeKind.Unspecified to indicate it's not tied to any particular time zone yet
            var localDateTime = new DateTime(DateTime.UtcNow.Year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            Console.WriteLine($"Local time (unspecified): {localDateTime}");

            // Get the corresponding time zone ID from the TimezoneMappings
            var timeZoneId = TimezoneMappings[timezone];
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            // Convert the local time to UTC, treating the input time as being in the specified time zone
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);
            Console.WriteLine($"Converted UTC time: {utcDateTime}");

            return utcDateTime;
        }


        /// <summary>
        /// Converts a given time to UTC time.
        /// </summary>
        /// <param name="month">The month of the time.</param>
        /// <param name="day">The day of the time.</param>
        /// <param name="hour">The hour of the time.</param>
        /// <param name="minute">The minute of the time.</param>
        /// <param name="sourceTimezone">The timezone of the time.</param>
        /// <param name="destinationTimezone">The timezeone to convert to.</param>
        /// <returns>The converted time in UTC.</returns>
        public static DateTime ConvertBetweenTimezones(int month, int day, int hour, int minute, Timezone sourceTimezone, Timezone destinationTimezone)
        {
            if (!TimezoneMappings.TryGetValue(sourceTimezone, out _) ||
                !TimezoneMappings.TryGetValue(destinationTimezone, out var destinationTimezoneId))
            {
                throw new ArgumentException("Unsupported timezone provided.");
            }

            var sourceTimeInUtc = ConvertToUtcTime(month, day, hour, minute, sourceTimezone);
            Console.WriteLine("Source time in UTC: " + sourceTimeInUtc);

            // Get the destination time zone
            var destinationTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(destinationTimezoneId);

            // Convert from UTC to the destination time zone.
            return TimeZoneInfo.ConvertTimeFromUtc(sourceTimeInUtc, destinationTimeZoneInfo);
        }
    }
}