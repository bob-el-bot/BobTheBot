using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Discord;

namespace Commands.Helpers
{
    /// <summary>
    /// Represents a group of related commands.
    /// </summary>
    public class CommandInfoGroup
    {
        /// <summary>
        /// Gets or sets the title of the command group.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the name of the command group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the command group.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the emoji representing the command group.
        /// </summary>
        public string Emoji { get; set; }

        /// <summary>
        /// Gets or sets the URL linking to the documentation of the command group.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the array of commands in this group.
        /// </summary>
        public CommandInfo[] Commands { get; set; }
    }

    /// <summary>
    /// Represents information about a command.
    /// </summary>
    public class CommandInfo
    {
        /// <summary>
        /// Gets or sets the name of the command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the command should inherit the group name as a prefix.
        /// </summary>
        public bool InheritGroupName { get; set; }

        /// <summary>
        /// Gets or sets the description of the command.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL linking to the documentation of the command.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the array of parameters for the command.
        /// </summary>
        public ParameterInfo[] Parameters { get; set; }
    }

    /// <summary>
    /// Represents information about a command parameter.
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Provides helper methods for generating help information and components.
    /// </summary>
    public static class Help
    {
        /// <summary>
        /// Generates an embed containing information about a command group and its commands.
        /// </summary>
        /// <param name="index">The index of the command group in the CommandGroups array.</param>
        /// <returns>An embed containing the details of the specified command group.</returns>
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
                Title = $"{CommandGroups[index].Emoji} {CommandGroups[index].Title} | {CommandGroups[index].Commands.Length} Commands.",
                Description = description.ToString(),
                Color = Bot.theme
            };

            return embed.Build();
        }

        /// <summary>
        /// Generates a message component containing a select menu for choosing a command group and buttons for support and documentation links.
        /// </summary>
        /// <returns>A message component with a select menu and buttons.</returns>
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
            foreach (var category in CommandGroups)
            {
                selectMenu.AddOption(label: category.Title, value: $"{i}", description: category.Description, emote: new Emoji(category.Emoji));
                i++;
            }

            components.WithSelectMenu(selectMenu);
            components.WithButton(SupportServerButton)
                      .WithButton(DocsButton);

            return components.Build();
        }

        /// <summary>
        /// Counts the total number of commands across all command groups.
        /// </summary>
        /// <returns>The total number of commands.</returns>
        public static int GetCommandCount()
        {
            int total = 0;

            foreach (var group in CommandGroups)
            {
                foreach (var command in group.Commands)
                {
                    total++;
                }
            }

            return total;
        }

        /// <summary>
        /// A button linking to the support server.
        /// </summary>
        private static readonly ButtonBuilder SupportServerButton = new()
        {
            Label = "Support Server",
            Style = ButtonStyle.Link,
            Emote = new Emoji("🏰"),
            Url = "https://discord.com/invite/HvGMRZD8jQ"
        };

        /// <summary>
        /// A button linking to the web documentation.
        /// </summary>
        private static readonly ButtonBuilder DocsButton = new()
        {
            Label = "Web Docs",
            Style = ButtonStyle.Link,
            Emote = new Emoji("🌐"),
            Url = "https://docs.bobthebot.net"
        };

        public static readonly CommandInfoGroup[] CommandGroups =
        {
            new() {
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
                                Description = "The number of sides you want the dice to have (atleast **0**)"
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
                                    "The earliest year you want the date to occur in (atleast **0**)"
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
            new() {
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
                        Name = "connect4",
                        InheritGroupName = false,
                        Description = "Play Bob or a user in a game of Connect 4.",
                        Url = "https://docs.bobthebot.net/#connect4",
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
            new() {
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
                                Description = "User whose profile to display. If left empty it display your own."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "confessions-toggle",
                        InheritGroupName = true,
                        Description = "Configure if you want to receive messages sent with `/confess`.",
                        Url = "https://docs.bobthebot.net#profile-confessions-toggle",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "open",
                                Description = "Enable or disable receiving messages sent via `/confess`. Choose from: True, False."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "punishments",
                        InheritGroupName = true,
                        Description = "See all active punishments on your account.",
                        Url = "https://docs.bobthebot.net#profile-punishments"
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
                                Description = "A color name (like \"purple\"), or a valid hex code (like \"#8D52FD\") or valid RGB code (like \"141, 82, 253\")."
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
                                Description = "The (optional) badge you want to learn about. If left empty, information for all badges will be shown."
                            }
                        }
                    }
                }
            },
            new() {
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
                                Description = "The text you want quoted. Quotation marks (\") are automatically added."
                            },
                            new ParameterInfo
                            {
                                Name = "user",
                                Description = "The user the quote belongs to."
                            },
                            new ParameterInfo
                            {
                                Name = "tag * 3",
                                Description = "You can optionally add a tag to make searching them easier."
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
                                Description = "The text channel you want to use as the quotes channel for the server."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-max-length",
                        InheritGroupName = true,
                        Description = "Sets the maximum quote length for the server (Discord has a limit of **4096**).",
                        Url = "https://docs.bobthebot.net#quote-set-max-length",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "length",
                                Description = "The amount of characters you want, at most, in a quote."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-min-length",
                        InheritGroupName = true,
                        Description = "Sets the minimum quote length for the server (must be atleast **0**).",
                        Url = "https://docs.bobthebot.net#quote-set-min-length",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "length",
                                Description = "The amount of characters you want, at least, in a quote."
                            }
                        }
                    }
                }
            },
            new() {
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
                                Description = "Enable or disable welcome messages. Choose from: True, False."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "set-message",
                        InheritGroupName = true,
                        Description = "Set a custom message to welcome new users with.",
                        Url = "https://docs.bobthebot.net#welcome-set-message",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message you want sent to welcome users to your server. Type @ where you want a ping and/or mention to be."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "remove-message",
                        InheritGroupName = true,
                        Description = "Bob will stop using the custom message to welcome users.",
                        Url = "https://docs.bobthebot.net#welcome-remove-message",
                    }
                }
            },
            new() {
                Title = "Scheduling",
                Description = "A collection of commands for scheduling messages and announcements.",
                Name = "schedule",
                Emoji = "🕖",
                Url = "https://docs.bobthebot.net/#schedule",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "message",
                        InheritGroupName = true,
                        Description = "Bob will send your message at a specified time.",
                        Url = "https://docs.bobthebot.net/#schedule-message",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "message",
                                Description = "The message you want to send. Markdown still works!"
                            },
                            new ParameterInfo
                            {
                                Name = "channel",
                                Description = "The channel for the message to be sent in."
                            },
                            new ParameterInfo
                            {
                                Name = "month",
                                Description = "The month you want your message sent."
                            },
                            new ParameterInfo
                            {
                                Name = "day",
                                Description = "The day you want your message sent."
                            },
                            new ParameterInfo
                            {
                                Name = "hour",
                                Description = "The hour you want your message sent, in military time (if PM, add 12)."
                            },
                            new ParameterInfo
                            {
                                Name = "minute",
                                Description = "The minute you want your message sent."
                            },
                            new ParameterInfo
                            {
                                Name = "timezone",
                                Description = "Your timezone."
                            }
                        },
                    },
                    new CommandInfo
                    {
                        Name = "announcement",
                        InheritGroupName = true,
                        Description = "Bob will send an embed at a specified time.",
                        Url = "https://docs.bobthebot.net/#schedule-announcement",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "title",
                                Description = "The title of the announcement (the title of the embed)."
                            },
                            new ParameterInfo
                            {
                                Name = "description",
                                Description = "The anouncement (the description of the embed)."
                            },
                            new ParameterInfo
                            {
                                Name = "color",
                                Description = "A color name (like \"purple\"), or a valid hex code (like \"#8D52FD\") or valid RGB code (like \"141, 82, 253\")."
                            },
                            new ParameterInfo
                            {
                                Name = "channel",
                                Description = "The channel for the message to be sent in."
                            },
                            new ParameterInfo
                            {
                                Name = "month",
                                Description = "The month you want your message sent."
                            },
                            new ParameterInfo
                            {
                                Name = "day",
                                Description = "The day you want your message sent."
                            },
                            new ParameterInfo
                            {
                                Name = "hour",
                                Description = "The hour you want your message sent, in military time (if PM, add 12)."
                            },
                            new ParameterInfo
                            {
                                Name = "minute",
                                Description = "The minute you want your message sent."
                            },
                            new ParameterInfo
                            {
                                Name = "timezone",
                                Description = "Your timezone."
                            }
                        },
                    },
                    new CommandInfo
                    {
                        Name = "edit",
                        InheritGroupName = true,
                        Description = "Bob will allow you to edit any messages or announcements you have scheduled.",
                        Url = "https://docs.bobthebot.net/#schedule-edit",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "id",
                                Description = "The ID of the scheduled message or announcement."
                            }
                        },
                    }
                }
            },
            new() {
                Title = "Auto",
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
                                Description = "Enable or disable publishing. Choose from: True, False."
                            },
                            new ParameterInfo
                            {
                                Name = "channel",
                                Description = "The announcement channel to publish announcements in."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "preview-github",
                        InheritGroupName = true,
                        Description = "Bob will preview all valid github links (code files, issues, and pull requests).",
                        Url = "https://docs.bobthebot.net#auto-preview-github",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "preview",
                                Description = "Enable or disable GitHub link previews. Choose from: True, False."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "preview-messages",
                        InheritGroupName = true,
                        Description =
                            "Bob will preview all valid Discord message links.",
                        Url = "https://docs.bobthebot.net#auto-preview-messages",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "preview",
                                Description = "Enable or disable message link previews. Choose from: True, False."
                            }
                        }
                    }
                }
            },
            new() {
                Title = "Encryption",
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
                        Name = "binary",
                        InheritGroupName = true,
                        Description =
                            "Encrypts your message by representing each character in binary.",
                        Url = "https://docs.bobthebot.net#encrypt-binary",
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
            new() {
                Title = "Decryption",
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
                        Name = "binary",
                        InheritGroupName = true,
                        Description =
                            "Decrypts your message byswapping binary representations to their corresponding characters.",
                        Url = "https://docs.bobthebot.net#decrypt-binary",
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
            new() {
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
                                Description = "The title of the announcement (the embed title)."
                            },
                            new ParameterInfo
                            {
                                Name = "description",
                                Description = "The in-depth announcement (the embed description). Use \"###\" for headings and \"-\" for lists."
                            },
                            new ParameterInfo
                            {
                                Name = "color",
                                Description = "A color name (like \"purple\"), or a valid hex code (like \"#8D52FD\") or valid RGB code (like \"141, 82, 253\")."
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
                    },
                    new CommandInfo
                    {
                        Name = "vote",
                        InheritGroupName = false,
                        Description = "Get a link to upvote Bob on Top.GG",
                        Url = "https://docs.bobthebot.net#vote",
                    },
                    new CommandInfo
                    {
                        Name = "review",
                        InheritGroupName = false,
                        Description = "Get a link to review Bob on Top.GG",
                        Url = "https://docs.bobthebot.net#review",
                    },
                }
            },
            new() {
                Title = "Preview",
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
                                Description = "The GitHub file link you want to share. Character indicators are ignored. Valid formats include: `https://github.com/bob-el-bot/website/blob/main/index.html#L15` and `https://github.com/bob-el-bot/website/blob/main/index.html#L15-L18`"
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
                                Description = "The link to the GitHub pull request. Valid formats include: `https://github.com/bob-el-bot/BobTheBot/pull/149`"
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
                                Description = "The link to the GitHub issue. `https://github.com/bob-el-bot/BobTheBot/issues/153`"
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
                                Description = "The link to the Discord message. Valid formats include: `https://discord.com/channels/1058077635692994651/1058081599222186074/1111715651476799619`"
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "color",
                        InheritGroupName = true,
                        Description = "Preview what a color looks like, and get more information.",
                        Url = "https://docs.bobthebot.net#preview-color",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "color",
                                Description = "A color name (like \"purple\"), or a valid hex code (like \"#8D52FD\") or valid RGB code (like \"141, 82, 253\")."
                            }
                        }
                    }
                }
            },
            new() {
                Title = "Convert",
                Name = "convert",
                Description = "Commands related to conversions.",
                Emoji = "↔️",
                Url = "https://docs.bobthebot.net#convert",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "units",
                        InheritGroupName = true,
                        Description = "Bob will convert one unit to another for you.",
                        Url = "https://docs.bobthebot.net#convert-units",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "unit-type",
                                Description = "The unit type of the conversion. Choose from: length, mass, temperature, volume, duration, speed, area, pressure, energy, information, angle, frequency."
                            },
                            new ParameterInfo
                            {
                                Name = "amount",
                                Description = "The amount of the unit you want to convert."
                            },
                            new ParameterInfo
                            {
                                Name = "from-unit",
                                Description = "The unit you want to convert from (to see specifics use the command with a random value for this field)."
                            },
                            new ParameterInfo
                            {
                                Name = "to-unit",
                                Description = "The unit you want to convert to (to see specifics use the command with a random value for this field)."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "timezones",
                        InheritGroupName = true,
                        Description = "Bob will convert a time from one timezone to another.",
                        Url = "https://docs.bobthebot.net#convert-timezones",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "month",
                                Description = "The month of the time you want to convert."
                            },
                            new ParameterInfo
                            {
                                Name = "day",
                                Description = "The day of the time you want to convert."
                            },
                            new ParameterInfo
                            {
                                Name = "hour",
                                Description = "The hour of the time you want to convert, in military time (if PM, add 12)."
                            },
                            new ParameterInfo
                            {
                                Name = "minute",
                                Description = "The minute of the time you want to convert."
                            },
                            new ParameterInfo
                            {
                                Name = "from-timezone",
                                Description = "The timezone of the time you want to convert from."
                            },
                            new ParameterInfo
                            {
                                Name = "to-timezone",
                                Description = "The timezone of the time you want to convert to."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "qr-code",
                        InheritGroupName = true,
                        Description = "Bob will convert a link or text to a QR code.",
                        Url = "https://docs.bobthebot.net#convert-qr-code",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "content",
                                Description = "The content you want to convert to a QR code (text or a link)."
                            },
                            new ParameterInfo
                            {
                                Name = "error-correction-level",
                                Description = "The error correction level of the QR code. Choose from: L, M, Q, H."
                            },
                        }
                    },
                }
            },
            new() {
                Title = "Auto Moderation",
                Name = "automod",
                Description = "Commands related to auto moderation.",
                Emoji = "⚖️",
                Url = "https://docs.bobthebot.net#automod",
                Commands = new[]
                {
                    new CommandInfo
                    {
                        Name = "phone-numbers",
                        InheritGroupName = true,
                        Description = "Add phone number auto moderation. Prevent phone numbers from being sent in this server.",
                        Url = "https://docs.bobthebot.net#automod-phone-numbers",
                        Parameters = new[]
                        {
                            new ParameterInfo
                            {
                                Name = "strict",
                                Description = "If checked (true) numbers like 1234567890 will be blocked. Otherwise only formatted phone numbers will be."
                            }
                        }
                    },
                    new CommandInfo
                    {
                        Name = "links",
                        InheritGroupName = true,
                        Description = "Add link auto moderation. Prevent links from being sent in this server.",
                        Url = "https://docs.bobthebot.net#automod-links",
                    },
                    new CommandInfo
                    {
                        Name = "zalgo-text",
                        InheritGroupName = true,
                        Description = "Add zalgo-text auto moderation. Prevent glitchy text from being sent in this server.",
                        Url = "https://docs.bobthebot.net#automod-zalgo-text",
                    },
                    new CommandInfo
                    {
                        Name = "bad-words",
                        InheritGroupName = true,
                        Description = "Add bad word auto moderation. Prevent bad words from being sent in this server.",
                        Url = "https://docs.bobthebot.net#automod-bad-words",
                    },
                    new CommandInfo
                    {
                        Name = "invite-links",
                        InheritGroupName = true,
                        Description = "Add invite link auto moderation. Prevent invites from being sent in this server.",
                        Url = "https://docs.bobthebot.net#automod-links-invite-links",
                    },
                }
            },
            new() {
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
                        Description = "See where a link will take you, and check for rick rolls. Valid formats include: `bobthebot.net` and `https://bobthebot.net`",
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
