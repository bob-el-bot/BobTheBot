using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Bob.Commands.Helpers
{
    public static class MessageReader
    {
        private static readonly string[] sourceArray = [".png", ".jpg", ".jpeg", ".gif"];

        public static async Task<Embed> GetPreview(DiscordLinkInfo linkInfo)
        {
            try
            {
                ITextChannel channel = (ITextChannel)await Bot.Client.GetShardFor(linkInfo.GuildId).GetChannelAsync(linkInfo.ChannelId);
                IMessage message = await channel.GetMessageAsync(linkInfo.MessageId);

                var firstEmbed = message.Embeds.FirstOrDefault();
                var embedTitle = firstEmbed?.Title;
                var embedDescription = firstEmbed?.Description;

                EmbedBuilder embed = new();

                if (string.IsNullOrEmpty(message.Content))
                {
                    if (!string.IsNullOrEmpty(embedTitle))
                    {
                        embed.Description = embedTitle;
                    }
                    else if (!string.IsNullOrEmpty(embedDescription))
                    {
                        embed.Description = embedDescription;
                    }
                    else
                    {
                        embed.Description = null;
                    }
                }
                else
                {
                    embed.Description = message.Content.Length > 0 ? message.Content : null;
                }

                // Check if both message content and embeds are empty or null, return null in that case
                if (string.IsNullOrEmpty(embed.Description) && string.IsNullOrEmpty(embedTitle) && message.Attachments.Count == 0)
                {
                    return null;
                }

                embed.Color = Bot.theme;
                embed.Author = new EmbedAuthorBuilder().WithName($"{message.Author.Username}").WithIconUrl(message.Author.GetAvatarUrl());
                embed.Timestamp = message.Timestamp;
                embed.Footer = new EmbedFooterBuilder().WithIconUrl(channel.Guild.IconUrl).WithText($"Server: {channel.Guild.Name}");

                if (embed.Description != null && embed.Description.Length > 4096)
                {
                    // Calculate the maximum length for the description
                    int overMaxLengthBy = embed.Description.Length + "...".Length - 4096;
                    int maxDescriptionLength = embed.Description.Length - overMaxLengthBy;

                    embed.Description = embed.Description[..maxDescriptionLength] + "...";
                }

                if (message.Attachments.Count > 0)
                {
                    foreach(var attachment in message.Attachments)
                    {
                        bool isImage = sourceArray.Any(ext => attachment.Filename.Contains(ext));
                        
                        if (isImage)
                        {
                            embed.WithImageUrl(attachment.Url);
                            break;
                        }
                    }
                }

                if (message.Reactions.Count > 0)
                {
                    embed.AddField(name: "Reactions", value: FormatReactions(message.Reactions));
                }

                return embed.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        private static string FormatReactions(IReadOnlyDictionary<IEmote, ReactionMetadata> reactions)
        {
            StringBuilder stringBuilder = new();

            foreach (var reaction in reactions)
            {
                stringBuilder.Append($"{reaction.Key} `{reaction.Value.ReactionCount}` ");
            }

            return stringBuilder.ToString();
        }

        public static DiscordLinkInfo CreateMessageInfo(string link)
        {
            DiscordLinkInfo linkInfo = new();
            string[] parts = link.Split('/');

            linkInfo.GoToUrl = link;
            linkInfo.GuildId = ulong.Parse(parts[4]);
            linkInfo.ChannelId = ulong.Parse(parts[5]);
            linkInfo.MessageId = ulong.Parse(parts[6]);

            return linkInfo;
        }

        public class DiscordLinkInfo
        {
            public ulong GuildId { get; set; }
            public ulong ChannelId { get; set; }
            public ulong MessageId { get; set; }
            public string GoToUrl { get; set; }
            public string Content { get; set; }
        }
    }
}
