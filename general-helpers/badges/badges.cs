using System;
using System.Collections.Generic;
using Discord.Interactions;

namespace Badges
{
    [Flags]
    public enum Badges : ulong
    {
        [ChoiceDisplay("Thinker")]
        Thinker = 1 << 0,
        [ChoiceDisplay("Helper")]
        Helper = 1 << 1,
        [ChoiceDisplay("Friend")]
        Friend = 1 << 2,
        [ChoiceDisplay("Decrypter")]
        Decrypter = 1 << 3,
        [ChoiceDisplay("Bug Reporter")]
        BugReporter = 1 << 4,
        [ChoiceDisplay("Winner")]
        Winner1 = 1 << 5,
        [ChoiceDisplay("Winner 2")]
        Winner2 = 1 << 6,
        [ChoiceDisplay("Winner 3")]
        Winner3 = 1 << 7,
        [ChoiceDisplay("Artist")]
        Artist = 1 << 8,
    }

    public class BadgeInformation
    {
        public string DisplayName { get; set; }
        public string Emoji { get; set; }
        public string Description { get; set; }
        public string HowToGet { get; set; }
    }

    public class BadgeDescriptions
    {
        public static Dictionary<Badges, BadgeInformation> Descriptions { get; } = new Dictionary<Badges, BadgeInformation>
        {
            {
                Badges.Thinker,
                new BadgeInformation
                {
                    DisplayName = "Thinker",
                    Emoji = "💡",
                    Description = "That was... a really good idea!",
                    HowToGet = "Have an idea that is added to Bob."
                }
            },
            {
                Badges.Helper,
                new BadgeInformation
                {
                    DisplayName = "Helper",
                    Emoji = "🫡",
                    Description = "Thank you for your kindness!",
                    HowToGet = "Helped users on the official BobTheBot server."
                }
            },
            {
                Badges.Friend,
                new BadgeInformation
                {
                    DisplayName = "Friend",
                    Emoji = "🤗",
                    Description = "Happy to have you here!",
                    HowToGet = "Join Bob's official server."
                }
            },
            {
                Badges.Decrypter,
                new BadgeInformation
                {
                    DisplayName = "Decrypter",
                    Emoji = "🧐",
                    Description = "You really pay attention.",
                    HowToGet = "???"
                }
            },
            {
                Badges.BugReporter,
                new BadgeInformation
                {
                    DisplayName = "Bug Reporter",
                    Emoji = "🪲",
                    Description = "Great catch!",
                    HowToGet = "Report and help fix a bug on Bob's official server."
                }
            },
            {
                Badges.Winner1,
                new BadgeInformation
                {
                    DisplayName = "Winner",
                    Emoji = "🏆",
                    Description = "In it to win it.",
                    HowToGet = "Win 10 challenges in a row."
                }
            },
            {
                Badges.Winner2,
                new BadgeInformation
                {
                    DisplayName = "Winner 2",
                    Emoji = "🏆",
                    Description = "In it to win it.",
                    HowToGet = "Win 20 challenges in a row."
                }
            },
            {
                Badges.Winner3,
                new BadgeInformation
                {
                    DisplayName = "Winner 3",
                    Emoji = "🏆",
                    Description = "In it to win it.",
                    HowToGet = "Win 30 challenges in a row."
                }
            },
            {
                Badges.Artist,
                new BadgeInformation
                {
                    DisplayName = "Artist",
                    Emoji = "🖼️",
                    Description = "It's... magnificent.",
                    HowToGet = "Make fanart and share it on Bob's official server!"
                }
            },
        };
    }
}