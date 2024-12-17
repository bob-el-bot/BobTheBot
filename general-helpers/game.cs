using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using System.Threading;
using Challenges;
using Debug;

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
        [ChoiceDisplay("Connect 4")]
        Connect4,
        [ChoiceDisplay("Wordle")]
        Wordle,
    }

    public enum GameState
    {
        Challenge,
        SettingRules,
        Active,
        Ended,
    }

    public class Game : IDisposable
    {
        public virtual string Title { get; }
        public virtual GameState State { get; set; }
        public virtual GameType Type { get; }
        public virtual ulong Id { get; set; }
        public virtual bool OnePerChannel { get; }
        public virtual IUser Player1 { get; }
        public virtual IUser Player2 { get; }
        public virtual RestUserMessage Message { get; set; }

        // Expiration Stuff
        public DateTime ExpirationTime { get; private set; }
        private readonly object lockObject = new();
        private CancellationTokenSource expirationCancellationTokenSource;
        public event Action<Game> Expired;

        public Game(GameType type, bool onePerChannel, TimeSpan expirationTime, IUser player1, IUser player2)
        {
            Type = type;
            OnePerChannel = onePerChannel;
            ExpirationTime = DateTime.Now.Add(expirationTime);
            expirationCancellationTokenSource = new CancellationTokenSource();
            StartExpirationTimer(expirationTime);
            Player1 = player1;
            Player2 = player2;
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                Expired = null;  // Detach event handlers
                expirationCancellationTokenSource?.Dispose();
            }
            GC.SuppressFinalize(this); // Suppress the finalization of this object
        }


        public void UpdateExpirationTime(TimeSpan expirationTime)
        {
            lock (lockObject)
            {
                // Update the expiration time
                ExpirationTime = DateTime.Now.Add(expirationTime);

                // Cancel the existing cancellation token source
                expirationCancellationTokenSource.Cancel();

                // Create a new cancellation token source
                expirationCancellationTokenSource = new CancellationTokenSource();

                // Start the expiration timer with the updated delay
                StartExpirationTimer(expirationTime);
            }
        }

        private async Task DelayUntilExpiration(TimeSpan expirationDelay)
        {
            try
            {
                // Wait until the specified delay or until cancellation
                await Task.Delay(expirationDelay, expirationCancellationTokenSource.Token);

                // If not canceled, invoke the Expired event
                if (!expirationCancellationTokenSource.Token.IsCancellationRequested)
                {
                    Expired?.Invoke(this);
                }
            }
            catch (TaskCanceledException)
            {
                // If this happens it simply means UpdateExpirationTimer() was called.
            }
        }

        private void StartExpirationTimer(TimeSpan expirationTime)
        {
            // Start a new task for the expiration timer
            Task.Run(() => DelayUntilExpiration(expirationTime));
        }

        public virtual Task StartBotGame(SocketInteraction interaction)
        {
            return Task.CompletedTask;
        }

        public virtual Task StartGame(SocketMessageComponent interaction)
        {
            State = GameState.Active;
            return Task.CompletedTask;
        }

        public virtual Task EndGameOnTime()
        {
            State = GameState.Ended;
            return Task.CompletedTask;
        }

        public Task EndGame()
        {
            // Set State
            State = GameState.Ended;

            Challenge.RemoveFromSpecificGameList(this);
            Dispose();

            return Task.CompletedTask;
        }
    }
}