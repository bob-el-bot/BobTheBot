using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Collections.Generic;

public class MasterMindGame
{
    public string key;
    public ulong id;
    public bool isStarted = false;
    public int guessesLeft = 8;
    public string startUser;
    public SocketUserMessage message;
}
