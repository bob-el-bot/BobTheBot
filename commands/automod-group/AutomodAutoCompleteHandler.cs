using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;
using Microsoft.VisualBasic;

namespace Bob.Commands;


public class AutomodAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? string.Empty;

        var rules = await context.Guild.GetAutoModRulesAsync();

        var suggestions = rules
            .Where(rule => rule.Name != null && rule.Name.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(rule =>
            {
                
                return new AutocompleteResult(rule.Name, rule.Id.ToString());
            })
            .ToList();

        // Return suggestions
        return AutocompletionResult.FromSuccess(suggestions);
    }
}


