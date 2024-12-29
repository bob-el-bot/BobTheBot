using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Discord;
using Time.Timestamps;
using static ApiInteractions.Interface;

namespace Commands.Helpers
{
    public static class IssueReader
    {
        /// <summary>
        /// Gets a preview of the issue.
        /// </summary>
        /// <param name="issueInfo">Information about the issue.</param>
        /// <returns>An Embed representing the preview of the issue.</returns>
        public static async Task<Embed> GetPreview(IssueInfo issueInfo)
        {
            // Send Request
            string content = await GetFromAPI(issueInfo.GetApiUrl(), AcceptTypes.application_json, Environment.GetEnvironmentVariable("GITHUB_TOKEN"));

            // Parse Content
            JsonObject jsonData = JsonNode.Parse(content).AsObject();
            JsonObject userData = JsonNode.Parse(jsonData["user"].ToString()).AsObject();
            JsonObject reactionData = JsonNode.Parse(jsonData["reactions"].ToString()).AsObject();

            issueInfo.Title = jsonData["title"].ToString();
            issueInfo.Author = userData["login"].ToString();
            issueInfo.Url = jsonData["html_url"].ToString();
            issueInfo.CreatedAt = jsonData["created_at"].ToString();
            issueInfo.UpdatedAt = jsonData["updated_at"]?.ToString();
            issueInfo.ClosedAt = jsonData["closed_at"]?.ToString();
            issueInfo.State = jsonData["state"].ToString();
            issueInfo.Locked = (bool)jsonData["locked"];
            issueInfo.Comments = (int)jsonData["comments"];
            issueInfo.Description = jsonData["body"]?.ToString();
            var labels = jsonData["labels"].AsArray();
            issueInfo.Labels = [];
            foreach (var label in labels)
            {
                JsonObject labelData = JsonNode.Parse(label.ToString()).AsObject();
                issueInfo.Labels.Add(labelData["name"].ToString());
            }
            issueInfo.ThumbsUps = (int)reactionData["+1"];
            issueInfo.ThumbsDowns = (int)reactionData["-1"];
            issueInfo.Laughs = (int)reactionData["laugh"];
            issueInfo.Hoorays = (int)reactionData["hooray"];
            issueInfo.Confuseds = (int)reactionData["confused"];
            issueInfo.Hearts = (int)reactionData["heart"];
            issueInfo.Rockets = (int)reactionData["rocket"];
            issueInfo.Eyes = (int)reactionData["eyes"];

            var embed = new EmbedBuilder
            {
                Color = GetColor(issueInfo.State),
                Author = new EmbedAuthorBuilder().WithName($"{issueInfo.Organization}/{issueInfo.Repository}"),
                Description = $"### [#{issueInfo.IssueNumber} {issueInfo.Title}]({issueInfo.Url})\n{issueInfo.Description ?? ""}",
                Timestamp = DateTimeOffset.Parse(issueInfo.CreatedAt),
                Footer = new EmbedFooterBuilder().WithIconUrl(userData["avatar_url"].ToString()).WithText($"Author: {issueInfo.Author}")
            };

            if (embed.Description.Length > 4096)
            {
                // Calculate the maximum length for the description
                int overMaxLengthBy = embed.Description.Length + "...".Length - 4096;
                int maxDescriptionLength = issueInfo.Description.Length - overMaxLengthBy;

                embed.Description = embed.Description[..maxDescriptionLength] + "...";
            }

            embed.AddField(name: "Reactions", value: FormatReactions(issueInfo));

            if (issueInfo.State == "closed")
            {
                embed.AddField(name: "<:issue_closed_git:1234993540119920711> Closed At", value: Timestamp.FromString(issueInfo.ClosedAt, Timestamp.Formats.Detailed), inline: true);
            }
            else
            {
                embed.AddField(name: "<:issue_opened_git:1234993539134259290> State", value: $"`{issueInfo.State}`", inline: true);
            }

            embed.AddField(name: "<:lock_git:1234998466904854648> Locked", value: $"`{issueInfo.Locked}`", inline: true)
            .AddField(name: "Comments", value: $"`{issueInfo.Comments}`", inline: true)
            .AddField(name: "Last Updated", value: Timestamp.FromString(issueInfo.UpdatedAt, Timestamp.Formats.Detailed), inline: true)
            .AddField(name: "Labels", value: issueInfo.Labels.Count > 0 ? FormatLabels(issueInfo.Labels) : "`none`");

            return embed.Build();
        }

        /// <summary>
        /// Gets the color for the embed based on the issue status.
        /// </summary>
        /// <param name="status">The status of the issue.</param>
        /// <returns>The color corresponding to the issue status.</returns>
        private static Color GetColor(string status)
        {
            if (status == "open")
            {
                return new Color(0xE67E22);
            }

            return new Color(0x23272D);
        }

        /// <summary>
        /// Formats the labels into a string.
        /// </summary>
        /// <param name="labels">The list of labels.</param>
        /// <returns>A formatted string representing the labels.</returns>
        private static string FormatLabels(List<string> labels)
        {
            StringBuilder stringBuilder = new();

            foreach (var label in labels)
            {
                stringBuilder.Append($"`{label}` ");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats the reactions into a string.
        /// </summary>
        /// <param name="issueInfo">Information about the issue's reactions.</param>
        /// <returns>A formatted string representing the reactions.</returns>
        private static string FormatReactions(IssueInfo issueInfo)
        {
            return $"`👍 {issueInfo.ThumbsUps}` `👎 {issueInfo.ThumbsDowns}` `😆 {issueInfo.Laughs}` `🎉 {issueInfo.Hoorays}` `😕 {issueInfo.Confuseds}` `❤️ {issueInfo.Hearts}` `🚀 {issueInfo.Rockets}` `👀 {issueInfo.Eyes}`";
        }

        /// <summary>
        /// Creates a IssueInfo object from the provided URL.
        /// </summary>
        /// <param name="url">The URL of the issue.</param>
        /// <returns>The IssueInfo object.</returns>
        public static IssueInfo CreateIssueInfo(string url)
        {
            IssueInfo issueInfo = new();
            Uri uri = new(url);

            issueInfo.Organization = uri.Segments[1].Trim('/');
            issueInfo.Repository = uri.Segments[2].Trim('/');
            issueInfo.IssueNumber = uri.Segments[4].Trim('/');

            return issueInfo;
        }
    }

    /// <summary>
    /// Represents information about a issue.
    /// </summary>
    public class IssueInfo
    {
        /// <summary>
        /// The title of the issue.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The URL of the issue.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The state of the issue.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The author of the issue.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The creation date of the issue.
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// The last update date of the issue.
        /// </summary>
        public string UpdatedAt { get; set; }

        /// <summary>
        /// The organization of the issue.
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// The repository of the issue.
        /// </summary>
        public string Repository { get; set; }

        /// <summary>
        /// The issue number.
        /// </summary>
        public string IssueNumber { get; set; }

        /// <summary>
        /// The number of comments on the issue.
        /// </summary>
        public int Comments { get; set; }

        /// <summary>
        /// The list of labels on the issue.
        /// </summary>
        public List<string> Labels { get; set; }

        /// <summary>
        /// The number of thumbs ups on the issue.
        /// </summary>
        public int ThumbsUps { get; set; }

        /// <summary>
        /// The number of thumbs downs on the issue.
        /// </summary>
        public int ThumbsDowns { get; set; }

        /// <summary>
        /// The number of laughs on the issue.
        /// </summary>
        public int Laughs { get; set; }

        /// <summary>
        /// The number of hoorays on the issue.
        /// </summary>
        public int Hoorays { get; set; }

        /// <summary>
        /// The number of confused reactions on the issue.
        /// </summary>
        public int Confuseds { get; set; }

        /// <summary>
        /// The number of heart reactions on the issue.
        /// </summary>
        public int Hearts { get; set; }

        /// <summary>
        /// The number of rocket reactions on the issue.
        /// </summary>
        public int Rockets { get; set; }

        /// <summary>
        /// The number of eye reactions on the issue.
        /// </summary>
        public int Eyes { get; set; }

        /// <summary>
        /// The description of the issue.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the issue is locked.
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// The date the issue was closed.
        /// </summary>
        public string ClosedAt { get; set; }

        /// <summary>
        /// Generates the API URL based on the organization, repository, and pull number.
        /// </summary>
        /// <returns>The generated API URL.</returns>
        public string GetApiUrl()
        {
            return $"https://api.github.com/repos/{this.Organization}/{this.Repository}/issues/{this.IssueNumber}";
        }
    }
}