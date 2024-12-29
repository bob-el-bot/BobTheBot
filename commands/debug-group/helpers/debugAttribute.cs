using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Commands.Attributes
{
    public class RequireGuildAttribute(ulong gId) : PreconditionAttribute
    {
        public ulong? GuildId { get; } = gId;

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.Guild.Id == GuildId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("❌ This command can't be used on this server."));
            }
        }
    }
}