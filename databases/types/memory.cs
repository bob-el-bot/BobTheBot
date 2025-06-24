using System;
using Pgvector;

public class Memory
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public Vector Embedding { get; set; }
    public DateTime CreatedAt { get; set; }
}