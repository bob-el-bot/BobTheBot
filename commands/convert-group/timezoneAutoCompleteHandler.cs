using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Time.Timezones;
using Discord;
using Discord.Interactions;

namespace Bob.Commands;

public class TimezoneAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var userInput =
            autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

        var results = TimezoneExtensions.FromChoiceDisplay.Keys
            .Where(label =>
                label.Contains(userInput, StringComparison.OrdinalIgnoreCase)
            )
            .Take(25)
            .Select(label => new AutocompleteResult(label, label))
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(results));
    }
}