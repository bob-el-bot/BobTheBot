using Bob.Database.Types;
using Discord;

namespace Bob.Commands.Helpers;

public static class AdminUtils
{
    public static Embed BuildSettingsEmbed(Server server, IGuild guild)
    {
        var eb = new EmbedBuilder()
            .WithTitle($"⚙️ {guild.Name} Settings for Bob.")
            .WithColor(Bot.theme);

        eb.AddField("🖊️ Quotes",
            $"Quote Channel: {(server.QuoteChannelId.HasValue ? $"<#{server.QuoteChannelId}>" : "None")} " +
            $"({Help.GetCommandMention("quote channel")})\n" +
            $"Max Quote Length: {server.MaxQuoteLength} " +
            $"({Help.GetCommandMention("quote set-max-length")})\n" +
            $"Min Quote Length: {server.MinQuoteLength} " +
            $"({Help.GetCommandMention("quote set-min-length")})"
        );

        eb.AddField("✉️ Confessions",
            $"Filtering Off: {server.ConfessFilteringOff} " +
            $"({Help.GetCommandMention("admin confess filter-toggle")})"
        );

        eb.AddField("👋 Welcoming",
            $"Enabled: {server.Welcome} " +
            $"({Help.GetCommandMention("welcome toggle")})\n" +
            $"Message: {(string.IsNullOrWhiteSpace(server.CustomWelcomeMessage) ? "Default" : server.CustomWelcomeMessage)} " +
            $"({Help.GetCommandMention("welcome set-message")}, {Help.GetCommandMention("welcome remove-message")})\n" +
            $"Has Image: {server.HasWelcomeImage} " +
            $"({Help.GetCommandMention("welcome set-image")}, {Help.GetCommandMention("welcome remove-image")})"
        );

        eb.AddField("🖨️ Auto Embeds",
            $"GitHub: {server.AutoEmbedGitHubLinks} " +
            $"({Help.GetCommandMention("auto preview-github")})\n" +
            $"Message Links: {server.AutoEmbedMessageLinks} " +
            $"({Help.GetCommandMention("auto preview-messages")})"
        );

        eb.AddField("📌 React Board",
            $"Enabled: {server.ReactBoardOn} " +
            $"({Help.GetCommandMention("react-board toggle")})\n" +
            $"Channel: {(server.ReactBoardChannelId.HasValue ? $"<#{server.ReactBoardChannelId}>" : "None")} " +
            $"({Help.GetCommandMention("react-board channel")})\n" +
            $"Emoji: {server.ReactBoardEmoji ?? "None"} " +
            $"({Help.GetCommandMention("react-board emoji")})\n" +
            $"Min Reactions: {server.ReactBoardMinimumReactions} " +
            $"({Help.GetCommandMention("react-board minimum-reactions")})"
        );

        return eb.Build();
    }
}