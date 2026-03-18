using System.Collections.Generic;
using System.Linq;
using Discord.Interactions;

namespace Bob.Time.Timezones
{
#nullable enable
    public enum Timezone
    {
        DatelineStandardTime,  // UTC-12:00
        SamoaStandardTime,     // UTC-11:00
        HawaiianStandardTime,  // UTC-10:00
        AlaskanStandardTime,   // UTC-09:00
        PacificStandardTime,   // UTC-08:00
        MountainStandardTime,  // UTC-07:00
        CentralStandardTime,   // UTC-06:00
        EasternStandardTime,   // UTC-05:00
        AtlanticStandardTime,  // UTC-04:00
        ArgentinaStandardTime, // UTC-03:00
        MidAtlanticStandardTime, // UTC-02:00
        AzoresStandardTime,    // UTC-01:00
        GreenwichMeanTime,     // UTC±00:00
        CentralEuropeanTime,   // UTC+01:00
        EasternEuropeanTime,   // UTC+02:00
        MoscowStandardTime,    // UTC+03:00
        ArabianStandardTime,   // UTC+04:00
        PakistanStandardTime,  // UTC+05:00
        BangladeshStandardTime, // UTC+06:00
        IndochinaTime,         // UTC+07:00
        ChinaStandardTime,     // UTC+08:00
        JapanStandardTime,     // UTC+09:00
        AustralianEasternTime, // UTC+10:00
        SolomonIslandsTime,    // UTC+11:00
        NewZealandStandardTime // UTC+12:00
    }

    public static class TimezoneExtensions
    {
        public static readonly Dictionary<string, Timezone> FromChoiceDisplay = new()
    {
        { "(DST) Dateline Standard Time", Timezone.DatelineStandardTime },
        { "(SST) Samoa Standard Time", Timezone.SamoaStandardTime },
        { "(HST) Hawaiian Standard Time", Timezone.HawaiianStandardTime },
        { "(AKST) Alaskan Standard Time", Timezone.AlaskanStandardTime },
        { "(PST) Pacific Standard Time", Timezone.PacificStandardTime },
        { "(MST) Mountain Standard Time", Timezone.MountainStandardTime },
        { "(CST) Central Standard Time", Timezone.CentralStandardTime },
        { "(EST) Eastern Standard Time", Timezone.EasternStandardTime },
        { "(AST) Atlantic Standard Time", Timezone.AtlanticStandardTime },
        { "(ART) Argentina Standard Time", Timezone.ArgentinaStandardTime },
        { "(MST) Mid-Atlantic Standard Time", Timezone.MidAtlanticStandardTime },
        { "(AZOT) Azores Standard Time", Timezone.AzoresStandardTime },
        { "(GMT) Greenwich Mean Time", Timezone.GreenwichMeanTime },
        { "(CET) Central European Time", Timezone.CentralEuropeanTime },
        { "(EET) Eastern European Time", Timezone.EasternEuropeanTime },
        { "(MSK) Moscow Standard Time", Timezone.MoscowStandardTime },
        { "(GST) Arabian Standard Time", Timezone.ArabianStandardTime },
        { "(PKT) Pakistan Standard Time", Timezone.PakistanStandardTime },
        { "(BST) Bangladesh Standard Time", Timezone.BangladeshStandardTime },
        { "(ICT) Indochina Time", Timezone.IndochinaTime },
        { "(CST) China Standard Time", Timezone.ChinaStandardTime },
        { "(JST) Japan Standard Time", Timezone.JapanStandardTime },
        { "(AEST) Australian Eastern Time", Timezone.AustralianEasternTime },
        { "(SBT) Solomon Islands Time", Timezone.SolomonIslandsTime },
        { "(NZST) New Zealand Standard Time", Timezone.NewZealandStandardTime },
    };

        public static string ToDisplayName(this Timezone abbreviation)
        {
            return abbreviation switch
            {
                Timezone.DatelineStandardTime => "Dateline Standard Time",
                Timezone.SamoaStandardTime => "Samoa Standard Time",
                Timezone.HawaiianStandardTime => "Hawaiian Standard Time",
                Timezone.AlaskanStandardTime => "Alaskan Standard Time",
                Timezone.PacificStandardTime => "Pacific Standard Time",
                Timezone.MountainStandardTime => "Mountain Standard Time",
                Timezone.CentralStandardTime => "Central Standard Time",
                Timezone.EasternStandardTime => "Eastern Standard Time",
                Timezone.AtlanticStandardTime => "Atlantic Standard Time",
                Timezone.ArgentinaStandardTime => "Argentina Standard Time",
                Timezone.MidAtlanticStandardTime => "Mid-Atlantic Standard Time",
                Timezone.AzoresStandardTime => "Azores Standard Time",
                Timezone.GreenwichMeanTime => "GMT Standard Time",
                Timezone.CentralEuropeanTime => "Central Europe Standard Time",
                Timezone.EasternEuropeanTime => "E. Europe Standard Time",
                Timezone.MoscowStandardTime => "Russian Standard Time",
                Timezone.ArabianStandardTime => "Arabian Standard Time",
                Timezone.PakistanStandardTime => "Pakistan Standard Time",
                Timezone.BangladeshStandardTime => "Bangladesh Standard Time",
                Timezone.IndochinaTime => "Indochina Time",
                Timezone.ChinaStandardTime => "China Standard Time",
                Timezone.JapanStandardTime => "Japan Standard Time",
                Timezone.AustralianEasternTime => "AUS Eastern Standard Time",
                Timezone.SolomonIslandsTime => "Central Pacific Standard Time",
                Timezone.NewZealandStandardTime => "New Zealand Standard Time",
                _ => "Unknown"
            };
        }
    }
}