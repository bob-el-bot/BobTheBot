using System.Linq;
using System.Threading.Tasks;
using Bob.Database;
using Bob.Database.Types;
using Discord;
using Discord.Interactions;

namespace Bob.Commands;

[CommandContextType(InteractionContextType.Guild)]
[IntegrationType(ApplicationIntegrationType.GuildInstall)]
[Group("tags", "All tag commands.")]
public class TagGroup(BobEntities dbContext) : InteractionModuleBase<ShardedInteractionContext>
{
    [SlashCommand("create", "Create a new tag.")]
    public async Task create([Summary("name", "The name of the tag (1-50 characters).")][MinLength(1)][MaxLength(50)] string name, [Summary("content", "The content of the tag (5-2000 characters).")][MinLength(5)][MaxLength(2000)] string content)
    {
        await DeferAsync(ephemeral: true);

        var existingTag = await dbContext.GetTag(Context.Guild.Id, name);
        if (existingTag != null)
        {
            await FollowupAsync($"❌ Tag `{name}` already exists in this server!\n- Tags must have unique names.\n- Tags are case-insensitive.");
            return;
        }

        var tag = new Tag
        {
            Name = name.Trim().ToLowerInvariant(),
            Content = content,
            GuildId = Context.Guild.Id,
            AuthorId = Context.User.Id
        };

        await dbContext.AddTag(tag);
        TagAutocompleteHandler.AddGuildTagToCache(Context.Guild.Id, (tag.Name, tag.Id));

        await FollowupAsync($"🏷️ Tag `{name}` created successfully!");
    }

    [SlashCommand("list", "List all tags in the server.")]
    public async Task list()
    {
        await DeferAsync();

        var tags = await dbContext.GetTagsByGuildId(Context.Guild.Id);
        if (tags.Count == 0)
        {
            await FollowupAsync("❌ No tags found in this server.");
            return;
        }

        var tagList = string.Join("\n", tags.Select(t => $"- `{t.Name}`"));
        await FollowupAsync($"🏷️ Tags in this server:\n{tagList}");
    }

    [SlashCommand("delete", "Delete a tag by name.")]
    public async Task delete([Summary("tag", "The tag to delete.")][Autocomplete(typeof(TagAutocompleteHandler))] string tagId)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(tagId, out int id))
        {
            await FollowupAsync("❌ Invalid tag selection.");
            return;
        }

        var tag = await dbContext.GetTag(id);
        if (tag == null || tag.GuildId != Context.Guild.Id)
        {
            await FollowupAsync($"❌ Tag not found in this server.");
            return;
        }

        if (tag.AuthorId != Context.User.Id && !((IGuildUser)Context.User).GuildPermissions.Administrator)
        {
            await FollowupAsync("❌ You do not have permission to delete this tag.\n- Only the author or an administrator can delete tags.");
            return;
        }

        await dbContext.RemoveTag(tag);
        TagAutocompleteHandler.RemoveGuildTagFromCache(Context.Guild.Id, tag.Id);

        await FollowupAsync($"🗑️ Tag `{tag.Name}` deleted successfully!");
    }

    [SlashCommand("remove-all", "Delete all tags in the server.")]
    public async Task RemoveAll()
    {
        await DeferAsync(ephemeral: true);

        var user = Context.User as IGuildUser;
        var permissions = user.GuildPermissions;

        if (!permissions.Administrator && !permissions.ManageGuild)
        {
            await FollowupAsync("❌ You do not have permission to delete all tags in this server.\n- Permission(s) needed: `Manage Server`.");
            return;
        }

        var tags = await dbContext.GetTagsByGuildId(Context.Guild.Id);
        if (tags.Count == 0)
        {
            await FollowupAsync("❌ No tags found in this server.");
            return;
        }

        await dbContext.RemoveTags(tags);
        TagAutocompleteHandler.RemoveGuildTagsFromCache(Context.Guild.Id);

        await FollowupAsync($"🗑️ Deleted all tags in this server successfully!");
    }

    [SlashCommand("info", "Get information about a specific tag.")]
    public async Task Info([Summary("tag", "The tag to get information about.")][Autocomplete(typeof(TagAutocompleteHandler))] string tagId)
    {
        await DeferAsync();

        if (!int.TryParse(tagId, out int id))
        {
            await FollowupAsync("❌ Invalid tag selection.");
            return;
        }

        var tag = await dbContext.GetTag(id);
        if (tag == null || tag.GuildId != Context.Guild.Id)
        {
            await FollowupAsync($"❌ Tag not found in this server.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"🏷️ Tag Information: {tag.Name}")
            .WithDescription($"```{tag.Content}```")
            .AddField("Author", $"<@{tag.AuthorId}>")
            .WithFooter($"Tag ID: {tag.Id}")
            .WithColor(Bot.theme)
            .Build();

        await FollowupAsync(embed: embed);
    }

    [SlashCommand("edit", "Edit an existing tag.")]
    public async Task Edit([Summary("tag", "The tag to edit.")][Autocomplete(typeof(TagAutocompleteHandler))] string tagId, [Summary("content", "The new content for the tag.")][MinLength(5)][MaxLength(2000)] string content)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(tagId, out int id))
        {
            await FollowupAsync("❌ Invalid tag selection.");
            return;
        }

        var tag = await dbContext.GetTag(id);
        if (tag == null || tag.GuildId != Context.Guild.Id)
        {
            await FollowupAsync($"❌ Tag with ID `{id}` not found in this server.");
            return;
        }

        if (tag.AuthorId != Context.User.Id && !((IGuildUser)Context.User).GuildPermissions.Administrator)
        {
            await FollowupAsync("❌ You do not have permission to edit this tag.\n- Only the author or an administrator can edit tags.");
            return;
        }

        tag.Content = content;
        await dbContext.UpdateTag(tag);

        TagAutocompleteHandler.UpdateGuildTagInCache(Context.Guild.Id, (tag.Name, tag.Id));

        await FollowupAsync($"✅ Tag `{tag.Name}` updated successfully!");
    }
}
