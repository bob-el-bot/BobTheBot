using System;

namespace Bob.Database.Types.DataTransferObjects;

public class MemoryDTO
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserMessage { get; set; }
    public string BotResponse { get; set; }
    public bool Ephemeral { get; set; }
}