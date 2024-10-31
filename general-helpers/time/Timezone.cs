using Discord.Interactions;

namespace Time.Timezones
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

    public static class TimezoneExtensions
    {
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