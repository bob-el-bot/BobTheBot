using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using System.Threading;

namespace Games
{
    public enum GameType
    {
        [ChoiceDisplay("Rock Paper Scissors")]
        RockPaperScissors,
        [ChoiceDisplay("Tic Tac Toe")]
        TicTacToe,
        [ChoiceDisplay("Trivia")]
        Trivia,
    }

    public class Game
    {
        public virtual string Title { get; }
        public virtual GameType Type { get; }
        public virtual ulong Id { get; set; }
        public virtual bool OnePerChannel { get; }
        public virtual IUser Player1 { get; }
        public virtual IUser Player2 { get; }
        public virtual RestUserMessage Message { get; set; }

        // Expiration Stuff
        public DateTime ExpirationTime { get; private set; }
        private readonly Timer expirationTimer;
        private readonly TimeSpan expirationDuration;
        private readonly object lockObject = new();
        public event Action<Game> Expired;

        public Game(GameType type, bool onePerChannel, TimeSpan expirationTime, IUser player1, IUser player2)
        {
            Type = type;
            OnePerChannel = onePerChannel;
            expirationDuration = expirationTime;
            ExpirationTime = DateTime.Now.Add(expirationTime);
            expirationTimer = new Timer(OnExpiration, null, expirationTime, Timeout.InfiniteTimeSpan);
            Player1 = player1;
            Player2 = player2;
        }

        public void UpdateExpirationTime()
        {
            lock (lockObject)
            {
                var remainingTime = ExpirationTime - DateTime.Now;

                ExpirationTime = DateTime.Now.Add(expirationDuration);

                expirationTimer.Change(remainingTime, Timeout.InfiniteTimeSpan);
            }
        }

        private void OnExpiration(object state)
        {
            // The game has expired
            Expired?.Invoke(this);
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                expirationTimer.Dispose();
                Expired = null;  // Detach event handlers
            }
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