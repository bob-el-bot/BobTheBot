using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Discord;
using Discord.Interactions;

namespace Bob.Commands;

public class TagAutocompleteHandler(BobEntities dbService) : AutocompleteHandler
{
    private static readonly ConcurrentDictionary<ulong, List<(string Name, int Id)>> _guildTagCache = [];

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";
        var guildId = context.Guild.Id;

        if (!_guildTagCache.TryGetValue(guildId, out List<(string Name, int Id)> value))
        {
            var tags = await dbService.GetTagsByGuildId(guildId);
            value = tags.Select(t => (t.Name, t.Id)).ToList();
            _guildTagCache[guildId] = value;
        }

        var suggestions = value.Where(tag => tag.Name.Contains(userInput.Trim(), StringComparison.InvariantCultureIgnoreCase))
            .Take(25)
            .Select(tag => new AutocompleteResult(
                name: tag.Name,
                value: tag.Id.ToString()
            ))
            .ToArray();

        return AutocompletionResult.FromSuccess(suggestions);
    }

    public static void RemoveGuildTagsFromCache(ulong guildId)
    {
        _guildTagCache.TryRemove(guildId, out _);
    }

    /// <summary>
    /// Adds a tag to the cache for the specified guild, if the cache exists.
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild.</param>
    /// <param name="tag">The tag (name and ID) to add to the cache.</param>
    public static void AddGuildTagToCache(ulong guildId, (string Name, int Id) tag)
    {
        if (_guildTagCache.TryGetValue(guildId, out List<(string Name, int Id)> value))
        {
            value.Add(tag);
        }
    }

    /// <summary>
    /// Updates an existing tag in the cache for the specified guild, if present.
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild.</param>
    /// <param name="updatedTag">The updated tag (name and ID).</param>
    public static void UpdateGuildTagInCache(ulong guildId, (string Name, int Id) updatedTag)
    {
        if (_guildTagCache.TryGetValue(guildId, out var tags))
        {
            var idx = tags.FindIndex(t => t.Id == updatedTag.Id);
            if (idx != -1)
            {
                tags[idx] = updatedTag;
            }
        }
    }

    /// <summary>
    /// Removes a tag with the specified ID from the cache for the given guild, if present.
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild.</param>
    /// <param name="tagId">The unique identifier of the tag to remove.</param>
    public static void RemoveGuildTagFromCache(ulong guildId, int tagId)
    {
        if (_guildTagCache.TryGetValue(guildId, out var tags))
        {
            tags.RemoveAll(t => t.Id == tagId);
        }
    }
}

