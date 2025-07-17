using System;
using System.Collections.Generic;
using Discord;

namespace Bob.Commands.Helpers;

/// <summary>
/// Represents a cached Discord message along with a count of reactions per emoji.
/// Provides methods to access and update reaction counts efficiently.
/// </summary>
public class CachedMessageInfo
{
    /// <summary>
    /// Gets or sets the cached Discord message.
    /// </summary>
    public IUserMessage Message { get; set; }

    private readonly Dictionary<string, uint> _reactionCounts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedMessageInfo"/> class,
    /// populating the reaction counts from the provided message.
    /// </summary>
    /// <param name="message">The Discord message to cache and track reactions for.</param>
    public CachedMessageInfo(IUserMessage message)
    {
        Message = message;
        foreach (var reaction in message.Reactions)
        {
            var key = reaction.Key is Emote emote ? emote.Id.ToString() : reaction.Key.Name;
            _reactionCounts[key] = (uint)reaction.Value.ReactionCount;
        }
    }

    /// <summary>
    /// Gets the current reaction count for the specified emoji key.
    /// </summary>
    /// <param name="key">The emoji key (custom emoji ID as string or unicode emoji name).</param>
    /// <returns>The number of reactions for the specified emoji; returns 0 if not present.</returns>
    public uint GetReactionCount(string key) =>
        _reactionCounts.TryGetValue(key, out var count) ? count : 0;

    /// <summary>
    /// Sets the reaction count for the specified emoji key.
    /// If the count is zero, the key is removed from the dictionary; otherwise, the count is updated.
    /// </summary>
    /// <param name="key">The emoji key (custom emoji ID as string or unicode emoji name).</param>
    /// <param name="count">The new reaction count to set.</param>
    public void SetReactionCount(string key, uint count)
    {
        if (count == 0)
        {
            _reactionCounts.Remove(key);
        }
        else
        {
            _reactionCounts[key] = count;
        }
    }

    /// <summary>
    /// Increases the reaction count for the specified emoji key by one.
    /// If the key does not exist, it is added with a count of 1.
    /// </summary>
    /// <param name="key">The emoji key (custom emoji ID as string or unicode emoji name).</param>
    public void IncreaseReactionCount(string key)
    {
        if (_reactionCounts.ContainsKey(key))
        {
            _reactionCounts[key]++;
        }
        else
        {
            _reactionCounts[key] = 1;
        }
    }

    /// <summary>
    /// Decreases the reaction count for the specified emoji key by one,
    /// if the key exists and the count is greater than zero.
    /// </summary>
    /// <param name="key">The emoji key (custom emoji ID as string or unicode emoji name).</param>
    public void DecrementReactionCount(string key)
    {
        if (_reactionCounts.ContainsKey(key) && _reactionCounts[key] > 0)
        {
            _reactionCounts[key]--;
        }
    }
}