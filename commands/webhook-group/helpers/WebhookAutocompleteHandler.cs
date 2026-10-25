using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bob.Commands;

/// <summary>Suggests this server's active webhooks by name. The submitted value is the webhook's ID.</summary>
public class WebhookAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString()?.Trim() ?? "";
        var guild = context.Guild as SocketGuild;

        // Discord.Net creates one handler and reuses it for every request, so a DbContext injected into the
        // constructor would be shared by all of them: concurrent keystrokes would collide on it, and its
        // change tracker would keep returning old names after /webhook edit. Use a fresh one per request.
        using var scope = Bob.Bot.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BobEntities>();

        var hooks = await db.WebhookInfo
            .AsNoTracking()
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
