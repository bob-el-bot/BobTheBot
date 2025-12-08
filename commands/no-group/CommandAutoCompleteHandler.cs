using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Discord;
using Discord.Interactions;

namespace Bob.Commands
{
    public class CommandAutocompleteHandler : AutocompleteHandler
    {
        // Cached list built once
        private static readonly IReadOnlyList<(string Name, string Description)> _allCommands =
            Help.CommandGroups
                .SelectMany(group => group.Commands.Select(cmd => (
                    Name: cmd.InheritGroupName ? $"{group.Name} {cmd.Name}" : cmd.Name,
                    Description: cmd.Description
                )))
                .ToList();

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            string userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(userInput))
            {
                return Task.FromResult(AutocompletionResult.FromSuccess([]));
            }

            var results = _allCommands
                .Where(cmd =>
                    cmd.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase) ||
                    (cmd.Description?.Contains(userInput, StringComparison.OrdinalIgnoreCase) ?? false)
                )
                .Take(25)
                .Select(cmd => new AutocompleteResult(
                    $"{cmd.Name} — {Truncate(cmd.Description, 60)}",
                    cmd.Name
                ))
                .ToList();

            return Task.FromResult(AutocompletionResult.FromSuccess(results));
        }

        private static string Truncate(string text, int limit)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            return text.Length > limit ? text[..limit] + "…" : text;
        }
    }
}