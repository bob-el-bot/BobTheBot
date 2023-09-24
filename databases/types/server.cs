using System;
using System.ComponentModel.DataAnnotations;

public class Server
{
    [Key]
    public ulong Id { get; set; }

    public ulong? QuoteChannelId { get; set; }
}