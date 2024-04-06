using System;
using System.Collections.Generic;

namespace Badges
{
    [Flags]
    public enum Badges : ulong
    {
        None = 0,
        Thinker = 1 << 0,
        Helper = 1 << 1,
        Friend = 1 << 2,
        Decrypter = 1 << 3,
        BugReporter = 1 << 4,
    }

    public class BadgeInformation
    {
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
                    Emoji = "💡",
                    Description = "That was... a really good idea!",
                    HowToGet = "Have an idea that is added to Bob."
                }
            },
            {
                Badges.Helper,
                new BadgeInformation
                {
                    Emoji = "🫡",
                    Description = "Thank you for your kindness!",
                    HowToGet = "Helped users on the official BobTheBot server."
                }
            },
            {
                Badges.Friend,
                new BadgeInformation
                {
                    Emoji = "🤗",
                    Description = "Happy to have you here!",
                    HowToGet = "Join Bob's official server."
                }
            },
            {
                Badges.Decrypter,
                new BadgeInformation
                {
                    Emoji = "🧐",
                    Description = "You really pay attention.",
                    HowToGet = "???"
                }
            },
            {
                Badges.BugReporter,
                new BadgeInformation
                {
                    Emoji = "🪲",
                    Description = "Great catch!",
                    HowToGet = "Report and help fix a bug on Bob's official server."
                }
            },
        };
    }
}