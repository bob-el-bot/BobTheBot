using Discord.WebSocket;

public class MasterMindGame
{
    public string key;
    public ulong id;
    public bool isStarted = false;
    public int guessesLeft = 8;
    public string startUser;
    public SocketUserMessage message;
}
