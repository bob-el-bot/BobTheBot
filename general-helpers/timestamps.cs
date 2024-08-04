using System;
using System.Collections.Generic;
using Discord.Interactions;

namespace TimeStamps
{
    /// <summary>
    /// Represents a utility class for generating timestamps in various formats.
    /// </summary>
    public static class TimeStamp
    {
        public enum Timezone
        {
            [ChoiceDisplay("(DST) Dateline Standard Time")]
            DatelineStandardTime,  // UTC-12:00

            [ChoiceDisplay("(SST) Samoa Standard Time")]
            SamoaStandardTime,     // UTC-11:00

            [ChoiceDisplay("(HST) Hawaiian Standard Time")]
            HawaiianStandardTime,  // UTC-10:00

            [ChoiceDisplay("(AKST) Alaskan Standard Time")]
            AlaskanStandardTime,   // UTC-09:00

            [ChoiceDisplay("(PST) Pacific Standard Time")]
            PacificStandardTime,   // UTC-08:00

            [ChoiceDisplay("(MST) Mountain Standard Time")]
            MountainStandardTime,  // UTC-07:00

            [ChoiceDisplay("(CST) Central Standard Time")]
            CentralStandardTime,   // UTC-06:00

            [ChoiceDisplay("(EST) Eastern Standard Time")]
            EasternStandardTime,   // UTC-05:00

            [ChoiceDisplay("(AST) Atlantic Standard Time")]
            AtlanticStandardTime,  // UTC-04:00

            [ChoiceDisplay("(ART) Argentina Standard Time")]
            ArgentinaStandardTime, // UTC-03:00

            [ChoiceDisplay("(MST) Mid-Atlantic Standard Time")]
            MidAtlanticStandardTime, // UTC-02:00

            [ChoiceDisplay("(AZOT) Azores Standard Time")]
            AzoresStandardTime,    // UTC-01:00

            [ChoiceDisplay("(GMT) Greenwich Mean Time")]
            GreenwichMeanTime,     // UTC±00:00

            [ChoiceDisplay("(CET) Central European Time")]
            CentralEuropeanTime,   // UTC+01:00

            [ChoiceDisplay("(EET) Eastern European Time")]
            EasternEuropeanTime,   // UTC+02:00

            [ChoiceDisplay("(MSK) Moscow Standard Time")]
            MoscowStandardTime,    // UTC+03:00

            [ChoiceDisplay("(GST) Arabian Standard Time")]
            ArabianStandardTime,   // UTC+04:00

            [ChoiceDisplay("(PKT) Pakistan Standard Time")]
            PakistanStandardTime,  // UTC+05:00

            [ChoiceDisplay("(BST) Bangladesh Standard Time")]
            BangladeshStandardTime, // UTC+06:00

            [ChoiceDisplay("(ICT) Indochina Time")]
            IndochinaTime,         // UTC+07:00

            [ChoiceDisplay("(CST) China Standard Time")]
            ChinaStandardTime,     // UTC+08:00

            [ChoiceDisplay("(JST) Japan Standard Time")]
            JapanStandardTime,     // UTC+09:00

            [ChoiceDisplay("(AEST) Australian Eastern Time")]
            AustralianEasternTime, // UTC+10:00

            [ChoiceDisplay("(SBT) Solomon Islands Time")]
            SolomonIslandsTime,    // UTC+11:00

            [ChoiceDisplay("(NZST) New Zealand Standard Time")]
            NewZealandStandardTime // UTC+12:00
        }

        public static readonly Dictionary<Timezone, string> TimezoneMappings = new()
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
        /// <returns>The generated timestamp string.</returns>
        public static string FromDateTime(DateTime dateTime, Formats format)
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

            // Create a DateTimeOffset with the validated dateTime
            var dateTimeOffset = new DateTimeOffset(dateTime);

            // Return the formatted timestamp string
            return $"<t:{dateTimeOffset.ToUniversalTime().ToUnixTimeSeconds()}:{(char)format}>";
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
