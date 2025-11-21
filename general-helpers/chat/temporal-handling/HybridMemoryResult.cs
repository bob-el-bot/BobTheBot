using System.Collections.Generic;
using Bob.Database.Types;

namespace BobTheBot.Chat.MemoryHandling;

public record HybridMemoryResult(
    List<Memory> Memories,
    int SemanticCount,
    int TemporalCount
)
{
    public bool HasTemporalMatches => TemporalCount > 0;
    public bool HasSemanticMatches => SemanticCount > 0;
}