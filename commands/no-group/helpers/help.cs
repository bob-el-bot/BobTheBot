using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Discord;

namespace Commands.Helpers
{
    public class CommandInfoGroup
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Emoji { get; set; }
        public string Url { get; set; }
        public CommandInfo[] Commands { get; set; }
    }

    public class CommandInfo
    {
        public string Name { get; set; }
        public bool InheritGroupName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public ParameterInfo[] Parameters { get; set; }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class Help
    {
        public static Embed GetCategoryEmbed(int index)
        {
            StringBuilder description = new();

            description.AppendLine($"[Docs]({CommandGroups[index].Url}) {CommandGroups[index].Description}");

            foreach (var command in CommandGroups[index].Commands)
            {
                var name = command.InheritGroupName ? $"{CommandGroups[index].Name} {command.Name}" : command.Name;
                description.AppendLine($"- [Docs]({command.Url}) `/{name}` {command.Description}");
				
                if (command.Parameters != null)
                {
                    foreach (var parameter in command.Parameters)
                    {
                        description.AppendLine($"  - `{parameter.Name}` {parameter.Description}");
                    }
                }
            }

            var embed = new EmbedBuilder
            {
                Title = $"{CommandGroups[index].Emoji} {CommandGroups[index].Title} Commands.",
                Description = description.ToString(),
                Color = Bot.theme
            };

            return embed.Build();
        }

        public static MessageComponent GetComponents()
        {
            var components = new ComponentBuilder();

            var selectMenu = new SelectMenuBuilder
            {
                MinValues = 1,
                MaxValues = 1,
                CustomId = "help",
                Placeholder = "Select Category...",
            };

            int i = 0;
            foreach (var category in Help.CommandGroups)
            {
                selectMenu.AddOption(label: category.Title, value: $"{i}", description: category.Description, emote: new Emoji(category.Emoji));
                i++;
            }

            components.WithSelectMenu(selectMenu);
            components.WithButton(Help.SupportServerButton)
            .WithButton(Help.DocsButton);

            return components.Build();
        }

        private static ButtonBuilder SupportServerButton = new ButtonBuilder
        {
            Label = "Support Server",
            Style = ButtonStyle.Link,
            Url = "https://discord.com/invite/HvGMRZD8jQ"
        };

        private static ButtonBuilder DocsButton = new ButtonBuilder
        {
            Label = "Web Docs",
            Style = ButtonStyle.Link,
            Url = "https://docs.bobthebot.net"
        };

        public static CommandInfoGroup[] CommandGroups =
        {
            new CommandInfoGroup
            {
                Title = "Randomly Generated (RNG)",
                Description = "An assortment of commands which respond with random results.",
                Name = "random",
                Emoji = "🎲",
                Url = "https://docs.bobthebot.net/#rng",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "color",
                        InheritGroupName = true,
                        Description = "Get a color with Hex, CMYK, HSL, HSV and RGB codes.",
                        Url = "https://docs.bobthebot.net/#random-color",
                    },
                    new CommandInfo
                    {
                        Name = "dice-roll",
                        InheritGroupName = true,
                        Description = "Roll a die with a specified # of sides.",
                        Url = "https://docs.bobthebot.net/#random-dice-roll",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "sides",
                                Description = "The number of sides you want the dice to have (atleast 0)"
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "coin-toss",
                        InheritGroupName = true,
                        Description = "Flip a coin.",
                        Url = "https://docs.bobthebot.net/#random-coin-toss",
                    },
                    new CommandInfo
                    {
                        Name = "quote",
                        InheritGroupName = true,
                        Description = "Get a random quote.",
                        Url = "https://docs.bobthebot.net/#random-quote",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "prompt",
                                Description =
                                    "This is optional, use `/quote-prompts` to view all valid prompts."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "dad-joke",
                        InheritGroupName = true,
                        Description = "Get a random dad joke.",
                        Url = "https://docs.bobthebot.net/#random-dad-joke",
                    },
                    new CommandInfo
                    {
                        Name = "fact",
                        InheritGroupName = true,
                        Description = "Get an outrageous fact.",
                        Url = "https://docs.bobthebot.net/#random-fact",
                    },
                    new CommandInfo
                    {
                        Name = "8ball",
                        InheritGroupName = true,
                        Description = "Get an 8 ball response to a prompt.",
                        Url = "https://docs.bobthebot.net/#random-8ball",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "prompt",
                                Description = "The prompt for the magic 8ball."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "dog",
                        InheritGroupName = true,
                        Description = "Get a random picture of a dog.",
                        Url = "https://docs.bobthebot.net/#random-dog",
                    },
                    new CommandInfo
                    {
                        Name = "date",
                        InheritGroupName = true,
                        Description = "Get a random date between the inputted years.",
                        Url = "https://docs.bobthebot.net/#random-date",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "earliestYear",
                                Description =
                                    "The earliest year you want the date to occur in (atleast 0)"
                            },
                            new ParameterInfo
                            {
                                Name = "latestYear",
                                Description =
                                    "The latest year you want the date to occur in (must be bigger than earliestYear)"
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "advice",
                        InheritGroupName = true,
                        Description = "Get a random piece of advice.",
                        Url = "https://docs.bobthebot.net/#random-advice",
                    },
                }
            },
            new CommandInfoGroup
            {
                Title = "Games",
                Description = "An assortment of games to play with or without friends.",
                Emoji = "🎮",
                Url = "https://docs.bobthebot.net/#games",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "trivia",
                        InheritGroupName = false,
                        Description = "Play a game of trivia with or without someone.",
                        Url = "https://docs.bobthebot.net/#trivia",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "opponent",
                                Description =
                                    "The user you wish to play. Leave empty to play alone."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "tic-tac-toe",
                        InheritGroupName = false,
                        Description = "Play Bob or a user in a game of Tic Tac Toe.",
                        Url = "https://docs.bobthebot.net/#tic-tac-toe",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "opponent",
                                Description =
                                    "The user you wish to play, leave empty to verse a bot."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "rock-paper-scissors",
                        InheritGroupName = false,
                        Description = "Play Bob or a user in a game of Rock Paper Scissors.",
                        Url = "https://docs.bobthebot.net/#rock-paper-scissors",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "opponent",
                                Description =
                                    "The user you wish to play, leave empty to verse a bot."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "master-mind new-game",
                        InheritGroupName = false,
                        Description =
                            "Play a game of Master Mind, the rules will shared upon usage.",
                        Url = "https://docs.bobthebot.net/#master-mind-new",
                    },
                    new CommandInfo
                    {
                        Name = "master-mind guess",
                        InheritGroupName = false,
                        Description =
                            "Make a guess in a game of Master Mind. You may only have one game per channel.",
                        Url = "https://docs.bobthebot.net/#master-mind-guess",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "guess",
                                Description = "Your 4 digit guess for the code."
                            }
                        }
                    },
                }
            },
            new CommandInfoGroup
            {
                Title = "Profiles",
                Name = "profile",
                Description = "Commands related to user profiles.",
                Emoji = "👤",
                Url = "https://docs.bobthebot.net#profile",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "display",
                        InheritGroupName = true,
                        Description = "Displays the specified user's profile.",
                        Url = "https://docs.bobthebot.net#profile-display",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "user",
                                Description = "User whose profile to display."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-color",
                        InheritGroupName = true,
                        Description = "Sets your profile color.",
                        Url = "https://docs.bobthebot.net#profile-set-color",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "color",
                                Description = "Color to set for your profile."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "badge-info",
                        InheritGroupName = true,
                        Description = "Shows how to unlock the given badge.",
                        Url = "https://docs.bobthebot.net#profile-badge-info",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "badge",
                                Description = "Badge to get information about."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Quoting",
                Name = "quote",
                Description = "Commands related to quoting.",
                Emoji = "🖊️",
                Url = "https://docs.bobthebot.net#quoting",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "new",
                        InheritGroupName = true,
                        Description = "Formats and shares the quote in designated channel.",
                        Url = "https://docs.bobthebot.net#quote-new",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "quote",
                                Description = "The quote to share."
                            },
                            new ParameterInfo
                            {
                                Name = "user",
                                Description = "User to attribute the quote to."
                            },
                            new ParameterInfo
                            {
                                Name = "tag * 3",
                                Description = "Up to three tags for the quote."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "channel",
                        InheritGroupName = true,
                        Description = "Sets the quote channel for the server.",
                        Url = "https://docs.bobthebot.net#quote-channel",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "channel",
                                Description = "Channel to set for quotes."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-max-length",
                        InheritGroupName = true,
                        Description = "Sets the maximum length of quotes for the server.",
                        Url = "https://docs.bobthebot.net#quote-set-max-length",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "length",
                                Description = "Maximum length for quotes."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-min-length",
                        InheritGroupName = true,
                        Description = "Sets the minimum length of quotes for the server.",
                        Url = "https://docs.bobthebot.net#quote-set-min-length",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "length",
                                Description = "Minimum length for quotes."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Welcoming",
                Name = "welcome",
                Description = "Commands related to welcoming new users.",
                Emoji = "👋",
                Url = "https://docs.bobthebot.net#welcome",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "toggle",
                        InheritGroupName = true,
                        Description = "Bob will send welcome messages to new server members.",
                        Url = "https://docs.bobthebot.net#welcome-toggle",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "welcome",
                                Description = "Enable or disable welcome messages."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-message",
                        InheritGroupName = true,
                        Description = "Set the welcome message to the given string.",
                        Url = "https://docs.bobthebot.net#welcome-set-message",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "Message to set for welcoming new members."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-channel",
                        InheritGroupName = true,
                        Description = "Set the welcome message channel to the given channel.",
                        Url = "https://docs.bobthebot.net#welcome-set-channel",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "channel",
                                Description = "Channel to set for welcome messages."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Auto Commands",
                Name = "auto",
                Description = "Commands related to automatic actions.",
                Emoji = "🖨️",
                Url = "https://docs.bobthebot.net#auto",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "publish-announcements",
                        InheritGroupName = true,
                        Description = "Bob will publish all messages sent in the given channel.",
                        Url = "https://docs.bobthebot.net#auto-publish-announcements",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "publish",
                                Description = "Enable or disable publishing."
                            },
                            new ParameterInfo
                            {
                                Name = "channel",
                                Description = "The channel to publish announcements in."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "preview-github",
                        InheritGroupName = true,
                        Description = "Bob will preview all valid GitHub links in the server.",
                        Url = "https://docs.bobthebot.net#auto-preview-github",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "preview",
                                Description = "Enable or disable GitHub link previews."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "preview-messages",
                        InheritGroupName = true,
                        Description =
                            "Bob will preview all valid Discord message links in the server.",
                        Url = "https://docs.bobthebot.net#auto-preview-messages",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "preview",
                                Description = "Enable or disable message link previews."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Encryption Commands",
                Name = "encrypt",
                Description = "Commands related to encrypting messages.",
                Emoji = "🔒",
                Url = "https://docs.bobthebot.net#encrypt",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "a1z26",
                        InheritGroupName = true,
                        Description =
                            "Encrypts your message by swapping letters to their corresponding number.",
                        Url = "https://docs.bobthebot.net#encrypt-a1z26",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to encrypt."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "atbash",
                        InheritGroupName = true,
                        Description =
                            "Encrypts your message by swapping letters to their opposite position.",
                        Url = "https://docs.bobthebot.net#encrypt-atbash",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to encrypt."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "caesar",
                        InheritGroupName = true,
                        Description =
                            "Encrypts your message by shifting the letters the specified amount.",
                        Url = "https://docs.bobthebot.net#encrypt-caesar",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to encrypt."
                            },
                            new ParameterInfo
                            {
                                Name = "shift",
                                Description = "The number of positions to shift each letter."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "morse",
                        InheritGroupName = true,
                        Description = "Encrypts your message using Morse code.",
                        Url = "https://docs.bobthebot.net#encrypt-morse",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to encrypt."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "vigenere",
                        InheritGroupName = true,
                        Description = "Encrypts your message using a specified key.",
                        Url = "https://docs.bobthebot.net#encrypt-vigenere",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to encrypt."
                            },
                            new ParameterInfo
                            {
                                Name = "key",
                                Description = "The key to use for encryption."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Decryption Commands",
                Name = "decrypt",
                Description = "Commands related to decrypting messages.",
                Emoji = "🔓",
                Url = "https://docs.bobthebot.net#decrypt",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "a1z26",
                        InheritGroupName = true,
                        Description =
                            "Decrypts your message by swapping numbers to their corresponding letters.",
                        Url = "https://docs.bobthebot.net#decrypt-a1z26",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to decrypt."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "atbash",
                        InheritGroupName = true,
                        Description =
                            "Decrypts your message by swapping letters to their opposite position.",
                        Url = "https://docs.bobthebot.net#decrypt-atbash",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to decrypt."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "caesar",
                        InheritGroupName = true,
                        Description =
                            "Decrypts your message by shifting the letters the specified amount.",
                        Url = "https://docs.bobthebot.net#decrypt-caesar",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to decrypt."
                            },
                            new ParameterInfo
                            {
                                Name = "shift",
                                Description = "The number of positions to shift each letter."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "morse",
                        InheritGroupName = true,
                        Description = "Decrypts your message using Morse code.",
                        Url = "https://docs.bobthebot.net#decrypt-morse",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to decrypt."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "vigenere",
                        InheritGroupName = true,
                        Description = "Decrypts your message using a specified key.",
                        Url = "https://docs.bobthebot.net#decrypt-vigenere",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to decrypt."
                            },
                            new ParameterInfo
                            {
                                Name = "key",
                                Description = "The key to use for decryption."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Other",
                Description = "Miscellaneous commands.",
                Emoji = "✨",
                Url = "https://docs.bobthebot.net#other",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "fonts",
                        InheritGroupName = false,
                        Description = "Change your text to a different font.",
                        Url = "https://docs.bobthebot.net#fonts",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "text",
                                Description = "The text to change."
                            },
                            new ParameterInfo
                            {
                                Name = "font",
                                Description =
                                    "The font to use (choose from: 𝖒𝖊𝖉𝖎𝖊𝖛𝖆𝖑, 𝓯𝓪𝓷𝓬𝔂, 𝕠𝕦𝕥𝕝𝕚𝕟𝕖𝕕, s̷l̷̷a̷s̷h̷e̷d̷, ɟןıddǝp, and 🄱🄾🅇🄴🄳)."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "confess",
                        InheritGroupName = false,
                        Description = "Have Bob DM a user a message.",
                        Url = "https://docs.bobthebot.net#confess",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message to send."
                            },
                            new ParameterInfo
                            {
                                Name = "user",
                                Description = "The user to send the message to."
                            },
                            new ParameterInfo
                            {
                                Name = "signoff",
                                Description = "Optional signoff for the message."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "announce",
                        InheritGroupName = false,
                        Description = "Have a fancy embed message sent.",
                        Url = "https://docs.bobthebot.net#announce",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "title",
                                Description = "The title of the announcement."
                            },
                            new ParameterInfo
                            {
                                Name = "description",
                                Description = "The description of the announcement."
                            },
                            new ParameterInfo
                            {
                                Name = "color",
                                Description = "The color of the embed."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "poll",
                        InheritGroupName = false,
                        Description = "Create a poll.",
                        Url = "https://docs.bobthebot.net#poll",
                        Parameters = new[]
                        {
                            new ParameterInfo { Name = "prompt", Description = "The poll prompt." },
                            new ParameterInfo
                            {
                                Name = "option * 4",
                                Description = "2-4 options for the poll."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "ship",
                        InheritGroupName = false,
                        Description = "See how good of a match 2 users are.",
                        Url = "https://docs.bobthebot.net#ship",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "user * 2",
                                Description = "The users to check compatibility for."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "hug",
                        InheritGroupName = false,
                        Description = "Show your friends some love with a hug.",
                        Url = "https://docs.bobthebot.net#hug",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "user * 5",
                                Description = "The users to hug."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Preview Commands",
                Name = "preview",
                Description = "Commands to preview content from various sources.",
                Emoji = "🔎",
                Url = "https://docs.bobthebot.net#preview",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "code",
                        InheritGroupName = true,
                        Description = "Preview specific lines of code from a file on GitHub.",
                        Url = "https://docs.bobthebot.net#preview-code",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "link",
                                Description = "The link to the GitHub file."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "pull-request",
                        InheritGroupName = true,
                        Description = "Preview a pull request from GitHub right on Discord.",
                        Url = "https://docs.bobthebot.net#preview-pull-request",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "link",
                                Description = "The link to the GitHub pull request."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "issue",
                        InheritGroupName = true,
                        Description = "Preview an issue from GitHub right on Discord.",
                        Url = "https://docs.bobthebot.net#preview-issue",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "link",
                                Description = "The link to the GitHub issue."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "message",
                        InheritGroupName = true,
                        Description = "Preview a Discord message from any server Bob is in.",
                        Url = "https://docs.bobthebot.net#preview-message",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "link",
                                Description = "The link to the Discord message."
                            }
                        }
                    }
                }
            },
            new CommandInfoGroup
            {
                Title = "Informational / Help",
                Description = "Commands to get information and help about the bot.",
                Emoji = "🗄️",
                Url = "https://docs.bobthebot.net#info",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "premium",
                        InheritGroupName = false,
                        Description =
                            "Ensures Bob knows you have premium! If not you will be given a button to get it!",
                        Url = "https://docs.bobthebot.net#premium"
                    },
                    new CommandInfo
                    {
                        Name = "new",
                        InheritGroupName = false,
                        Description = "See the latest updates to Bob.",
                        Url = "https://docs.bobthebot.net#new"
                    },
                    new CommandInfo
                    {
                        Name = "quote-prompts",
                        InheritGroupName = false,
                        Description = "See all valid prompts for `/random quote`.",
                        Url = "https://docs.bobthebot.net#quote-prompts"
                    },
                    new CommandInfo
                    {
                        Name = "ping",
                        InheritGroupName = false,
                        Description = "Find the client's latency.",
                        Url = "https://docs.bobthebot.net#ping"
                    },
                    new CommandInfo
                    {
                        Name = "analyze-link",
                        InheritGroupName = false,
                        Description = "See where a link will take you, and check for rick rolls.",
                        Url = "https://docs.bobthebot.net#analyze-link"
                    },
                    new CommandInfo
                    {
                        Name = "info",
                        InheritGroupName = false,
                        Description = "Learn about Bob.",
                        Url = "https://docs.bobthebot.net#info"
                    },
                    new CommandInfo
                    {
                        Name = "support",
                        InheritGroupName = false,
                        Description = "Sends an invite to Bob's support Server.",
                        Url = "https://docs.bobthebot.net#support"
                    }
                }
            }
        };
    }
}
