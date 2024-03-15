using Discord;
using Discord.WebSocket;

namespace Commands.Helpers {
    public class MasterMindGame
    {
        public string Key { get; set; }
        public ulong Id { get; set; }
        public bool IsStarted { get; set; }
        public int GuessesLeft { get; set; } = 8;
        public IUser StartUser { get; set; }
        public SocketUserMessage Message { get; set; }
    }
}
