using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Bob.Commands;

public class QuotePromptAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = RandomQuote.NormalizePrompt(autocompleteInteraction.Data.Current.Value?.ToString());

        var results = RandomQuote.Prompts
            .Where(p => p.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => !p.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(p => new AutocompleteResult(p, p))
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(results));
    }
}
