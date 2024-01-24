using Discord.WebSocket;

namespace Commands.Helpers {
    public class MasterMindGame
    {
        public string key;
        public ulong id;
        public bool isStarted;
        public int guessesLeft = 8;
        public string startUser;
        public SocketUserMessage message;
    }
}
