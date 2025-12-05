using System;
using Pgvector;

namespace Bob.Database.Types;

public class Memory
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string UserMessage { get; set; }
    public string BotResponse { get; set; }
    public Vector Embedding { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Ephemeral { get; set; } = false;
}