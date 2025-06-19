using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Bob.Commands;

public class UnitAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

        var unitTypeOption = autocompleteInteraction.Data.Options
            .FirstOrDefault(opt => opt.Name == "unit-type");

        if (unitTypeOption?.Value is not string unitTypeString || !Enum.TryParse<UnitConversion.UnitType>(unitTypeString, true, out var unitType))
        {
            return Task.FromResult(AutocompletionResult.FromSuccess());
        }

        var unitEnumType = UnitConversion.GetUnitEnumType(unitType);

        if (unitEnumType == null)
        {
            return Task.FromResult(AutocompletionResult.FromSuccess());
        }

        var results = Enum.GetNames(unitEnumType)
            .Where(n => n.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(n => new AutocompleteResult(n, n))
            .ToList();

        return Task.FromResult(AutocompletionResult.FromSuccess(results));
    }
}

