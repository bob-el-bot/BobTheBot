using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Discord;
using Discord.Interactions;

namespace Bob.Commands;

public class TagAutocompleteHandler : AutocompleteHandler
{
    private readonly BobEntities _dbService;
    private static readonly Dictionary<ulong, List<(string Name, int Id)>> _guildTagCache = new();

    public TagAutocompleteHandler(BobEntities dbService)
    {
        _dbService = dbService;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";
        var guildId = context.Guild.Id;

        if (!_guildTagCache.ContainsKey(guildId))
        {
            var tags = await _dbService.GetTagsByGuildId(guildId);
            _guildTagCache[guildId] = tags.Select(t => (Name: t.Name, Id: t.Id)).ToList();
        }

        var suggestions = _guildTagCache[guildId]
            .Where(tag => tag.Name.Contains(userInput.Trim().ToLowerInvariant()))
            .Take(25)
            .Select(tag => new AutocompleteResult(
                name: tag.Name,
                value: tag.Id.ToString()
            ))
            .ToArray();

        return AutocompletionResult.FromSuccess(suggestions);
    }

    /// <summary>
    /// Removes all tag entries from the cache for the specified guild.
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild.</param>
    public static void RemoveGuildTagsFromCache(ulong guildId)
    {
        if (_guildTagCache.ContainsKey(guildId))
        {
            _guildTagCache.Remove(guildId);
        }
    }

    /// <summary>
    /// Adds a tag to the cache for the specified guild, if the cache exists.
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild.</param>
    /// <param name="tag">The tag (name and ID) to add to the cache.</param>
    public static void AddGuildTagToCache(ulong guildId, (string Name, int Id) tag)
    {
        if (_guildTagCache.ContainsKey(guildId))
        {
            _guildTagCache[guildId].Add(tag);
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

