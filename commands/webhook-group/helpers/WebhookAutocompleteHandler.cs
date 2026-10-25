using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Bob.Commands;

/// <summary>Suggests this server's active webhooks by name. The submitted value is the webhook's ID.</summary>
public class WebhookAutocompleteHandler(BobEntities dbService) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString()?.Trim() ?? "";
        var guild = context.Guild as SocketGuild;

        var hooks = await dbService.WebhookInfo
            .Where(w => w.GuildId == context.Guild.Id && w.IsActive)
            .OrderBy(w => w.DisplayName)
            .ToListAsync();

        var suggestions = hooks
            .Where(w => w.DisplayName.Contains(userInput, StringComparison.InvariantCultureIgnoreCase))
            .Take(25)
            .Select(w =>
            {
                var channelName = guild?.GetChannel(w.ChannelId)?.Name;
                var label = channelName != null ? $"{w.DisplayName} ({w.Provider} in #{channelName})" : $"{w.DisplayName} ({w.Provider})";
                return new AutocompleteResult(name: label.Length > 100 ? label[..99] + "…" : label, value: w.Id.ToString());
            })
            .ToArray();

        return AutocompletionResult.FromSuccess(suggestions);
    }
}
