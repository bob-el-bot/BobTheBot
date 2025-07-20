using System.ComponentModel.DataAnnotations;

namespace Bob.Database.Types;

public class Tag
{
    [Key]
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public ulong AuthorId { get; set; }
}