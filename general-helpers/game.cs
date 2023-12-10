using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace Games
{
    public enum GameType
    {
        [ChoiceDisplay("Rock Paper Scissors")]
        rps,
    }

    public class Game
    {
        public virtual string Title { get; }
        public virtual GameType Type { get; }
        public virtual ulong Id { get; set; }
        public virtual bool OnePerChannel { get; }
        public virtual bool IsStarted { get; set; }
        public virtual IUser Player1 { get; }
        public virtual IUser Player2 { get; }
        public virtual RestUserMessage Message { get; set; }

        public Game(GameType type, bool onePerChannel, IUser player1, IUser player2)
        {
            Type = type;
            OnePerChannel = onePerChannel;
            Player1 = player1;
            Player2 = player2;
        }

        public virtual Task StartBotGame(SocketInteraction interaction)
        {
            return Task.CompletedTask;
        }

        public virtual Task StartGame(SocketMessageComponent interaction)
        {
            return Task.CompletedTask;
        }

        public virtual Task EndGame()
        {
            return Task.CompletedTask;
        }
    }
}