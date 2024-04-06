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
        };
    }
}