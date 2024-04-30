using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Discord;
using TimeStamps;
using static ApiInteractions.Interface;

namespace Commands.Helpers
{
    /// <summary>
    /// A helper class for reading pull requests from GitHub API.
    /// </summary>
    public static class PullRequestReader
    {
        /// <summary>
        /// Gets a preview of the pull request.
        /// </summary>
        /// <param name="pullRequestInfo">Information about the pull request.</param>
        /// <returns>An Embed representing the preview of the pull request.</returns>
        public static async Task<Embed> GetPreview(PullRequestInfo pullRequestInfo)
        {
            // Send Request
            string content = await GetFromAPI(pullRequestInfo.GetApiUrl(), AcceptTypes.application_json);

            // Parse Content
            JsonObject jsonData = JsonNode.Parse(content).AsObject();
            JsonObject userData = JsonNode.Parse(jsonData["user"].ToString()).AsObject();
            JsonObject baseData = JsonNode.Parse(jsonData["base"].ToString()).AsObject();
            JsonObject orgData = JsonNode.Parse(baseData["user"].ToString()).AsObject();

            pullRequestInfo.Title = jsonData["title"].ToString();
            pullRequestInfo.Author = userData["login"].ToString();
            pullRequestInfo.Url = jsonData["html_url"].ToString();
            pullRequestInfo.CreatedAt = jsonData["created_at"].ToString();
            pullRequestInfo.UpdatedAt = jsonData["updated_at"]?.ToString();
            pullRequestInfo.ClosedAt = jsonData["closed_at"]?.ToString();
            pullRequestInfo.MergedAt = jsonData["merged_at"]?.ToString();
            pullRequestInfo.State = jsonData["state"].ToString();
            pullRequestInfo.Locked = (bool)jsonData["locked"];
            pullRequestInfo.Additions = (int)jsonData["additions"];
            pullRequestInfo.Deletions = (int)jsonData["deletions"];
            pullRequestInfo.Comments = (int)jsonData["comments"];
            pullRequestInfo.Commits = (int)jsonData["commits"];
            pullRequestInfo.Merged = (bool)jsonData["merged"];
            pullRequestInfo.ChangedFiles = (int)jsonData["changed_files"];
            pullRequestInfo.ReviewComments = (int)jsonData["review_comments"];
            pullRequestInfo.Description = jsonData["body"]?.ToString();
            var labels = jsonData["labels"].AsArray();
            pullRequestInfo.Labels = new();
            foreach (var label in labels)
            {
                JsonObject labelData = JsonNode.Parse(label.ToString()).AsObject();
                pullRequestInfo.Labels.Add(labelData["name"].ToString());
            }

            var embed = new EmbedBuilder
            {
                Color = GetColor(pullRequestInfo.State),
                Author = new EmbedAuthorBuilder().WithName($"{pullRequestInfo.Organization}/{pullRequestInfo.Repository}").WithIconUrl(orgData["avatar_url"].ToString()),
                Description = $"### [{pullRequestInfo.Title}]({pullRequestInfo.Url})\n{pullRequestInfo.Description ?? ""}",
                Timestamp = DateTimeOffset.Parse(pullRequestInfo.CreatedAt),
                Footer = new EmbedFooterBuilder().WithIconUrl(userData["avatar_url"].ToString()).WithText($"Author: {pullRequestInfo.Author}")
            };

            if (embed.Description.Length > 4096)
            {
                // Calculate the maximum length for the description
                int overMaxLengthBy = embed.Description.Length + "...".Length - 4096;
                int maxDescriptionLength = pullRequestInfo.Description.Length - overMaxLengthBy;

                embed.Description = embed.Description[..maxDescriptionLength];
            }

            embed.AddField(name: $"{(!pullRequestInfo.Merged ? "<:pull_request_git:1234992648280866836>" : "<:merge_git:1234992673618919507>")} State", value: $"`{pullRequestInfo.State}`", inline: true);
            if (pullRequestInfo.MergedAt != null)
            {
                embed.AddField(name: "Merged At", value: TimeStamp.FromString(pullRequestInfo.MergedAt, TimeStamp.Formats.Detailed), inline: true);
            }
            else
            {
                if (pullRequestInfo.State == "closed")
                {
                    embed.AddField(name: "Closed At", value: TimeStamp.FromString(pullRequestInfo.ClosedAt, TimeStamp.Formats.Detailed), inline: true);
                }

                embed.AddField(name: "Merged", value: $"`{pullRequestInfo.Merged}`", inline: true);
            }

            embed.AddField(name: "<:lock_git:1234998466904854648> Locked", value: $"`{pullRequestInfo.Locked}`", inline: true)
            .AddField(name: "Comments", value: $"`{pullRequestInfo.Comments}`", inline: true)
            .AddField(name: "ReviewComments", value: $"`{pullRequestInfo.ReviewComments}`", inline: true)
            .AddField(name: "Commits", value: $"`{pullRequestInfo.Commits}`", inline: true)
            .AddField(name: "Last Updated", value: TimeStamp.FromString(pullRequestInfo.UpdatedAt, TimeStamp.Formats.Detailed), inline: true)
            .AddField(name: "Labels", value: pullRequestInfo.Labels.Count > 0 ? FormatLabels(pullRequestInfo.Labels) : "`none`")
            .AddField(name: "Diff", value: FormatDiff(pullRequestInfo.ChangedFiles, pullRequestInfo.Additions, pullRequestInfo.Deletions));

            return embed.Build();
        }

        /// <summary>
        /// Gets the color for the embed based on the pull request status.
        /// </summary>
        /// <param name="status">The status of the pull request.</param>
        /// <returns>The color corresponding to the pull request status.</returns>
        private static Color GetColor(string status)
        {
            if (status == "open")
            {
                return new Color(0x57F287);
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
        /// Formats the diff information into a string.
        /// </summary>
        /// <param name="changedFiles">The number of changed files.</param>
        /// <param name="additions">The number of additions.</param>
        /// <param name="deletions">The number of deletions.</param>
        /// <returns>A formatted string representing the diff information.</returns>
        private static string FormatDiff(int changedFiles, int additions, int deletions)
        {
            return $"Changes across `{changedFiles}` file(s).\n```diff\n+{additions}\n-{deletions}```";
        }

        /// <summary>
        /// Creates a PullRequestInfo object from the provided URL.
        /// </summary>
        /// <param name="url">The URL of the pull request.</param>
        /// <returns>The PullRequestInfo object.</returns>
        public static PullRequestInfo CreatePullRequestInfo(string url)
        {
            PullRequestInfo pullRequestInfo = new();
            Uri uri = new(url);

            pullRequestInfo.Organization = uri.Segments[1].Trim('/');
            pullRequestInfo.Repository = uri.Segments[2].Trim('/');
            pullRequestInfo.PullNumber = uri.Segments[4].Trim('/');

            return pullRequestInfo;
        }
    }

    /// <summary>
    /// Represents information about a pull request.
    /// </summary>
    public class PullRequestInfo
    {
        /// <summary>
        /// The title of the pull request.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The URL of the pull request.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The state of the pull request.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The author of the pull request.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The creation date of the pull request.
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// The last update date of the pull request.
        /// </summary>
        public string UpdatedAt { get; set; }

        /// <summary>
        /// The organization of the repository.
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// The repository of the pull request.
        /// </summary>
        public string Repository { get; set; }

        /// <summary>
        /// The pull request number.
        /// </summary>
        public string PullNumber { get; set; }

        /// <summary>
        /// The number of comments on the pull request.
        /// </summary>
        public int Comments { get; set; }

        /// <summary>
        /// The list of labels on the pull request.
        /// </summary>
        public List<string> Labels { get; set; }

        /// <summary>
        /// The description of the pull request.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the pull request is locked.
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// The number of additions in the pull request.
        /// </summary>
        public int Additions { get; set; }

        /// <summary>
        /// The number of deletions in the pull request.
        /// </summary>
        public int Deletions { get; set; }

        /// <summary>
        /// The number of commits in the pull request.
        /// </summary>
        public int Commits { get; set; }

        /// <summary>
        /// Indicates whether the pull request is merged.
        /// </summary>
        public bool Merged { get; set; }

        /// <summary>
        /// The number of changed files in the pull request.
        /// </summary>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// The date the pull request was closed.
        /// </summary>
        public string ClosedAt { get; set; }

        /// <summary>
        /// The date the pull request was merged.
        /// </summary>
        public string MergedAt { get; set; }

        /// <summary>
        /// The number of review comments on the pull request.
        /// </summary>
        public int ReviewComments { get; set; }

        /// <summary>
        /// Generates the API URL based on the organization, repository, and pull number.
        /// </summary>
        /// <returns>The generated API URL.</returns>
        public string GetApiUrl()
        {
            return $"https://api.github.com/repos/{this.Organization}/{this.Repository}/pulls/{this.PullNumber}";
        }
    }
}