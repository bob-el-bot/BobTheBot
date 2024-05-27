using System;
using System.Threading.Tasks;
using Challenges;
using Discord;
using Discord.WebSocket;
using Games;
using TimeStamps;

namespace Commands.Helpers
{
    public class Connect4 : Games.Game
    {
        public override string Title { get; } = "Connect 4";
        public const bool onePerChannel = false;

        public int[,] Grid = new int[7, 6];
        public int LastMoveColumn;
        public int LastMoveRow;
        public bool IsPlayer1Turn;
        public int Turns;

        public Connect4(IUser player1, IUser player2) : base(GameType.Connect4, onePerChannel, TimeSpan.FromMinutes(5), player1, player2)
        {

        }

        public override async Task StartBotGame(SocketInteraction interaction)
        {
            var msg = await interaction.FollowupAsync(text: "⚔️ *Creating Challenge...*");

            // Prepare Game
            Message = msg;
            Id = msg.Id;
            State = GameState.Active;

            // Pick Turn
            IsPlayer1Turn = Connect4Methods.DetermineFirstTurn();

            // Add to Games List
            Challenge.AddToSpecificGameList(this);

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(5));

            Expired += Challenge.ExpireGame;

            await Message.ModifyAsync(x => { x.Content = null; x.Embed = Connect4Methods.CreateEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)})"); x.Components = Connect4Methods.GetButtons(this).Build(); });

            if (!IsPlayer1Turn)
            {
                //await TTTMethods.BotPlay(this);
            }
        }

        public override async Task StartGame(SocketMessageComponent interaction)
        {
            // Set State
            State = GameState.Active;

            // Pick Turn
            IsPlayer1Turn = Connect4Methods.DetermineFirstTurn();

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));

            await interaction.ModifyOriginalResponseAsync(x => { x.Content = null; x.Embed = Connect4Methods.CreateEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn ( Forfeit {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)}).\n{Connect4Methods.GetGrid(Grid)}"); x.Components = Connect4Methods.GetButtons(this).Build(); });
        }

        public override async Task EndGameOnTime()
        {
            // Set State
            State = GameState.Ended;

            try
            {
                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, Connect4Methods.GetWinner(this, true));
                }

                await Message.ModifyAsync(x => { x.Embed = Connect4Methods.CreateEmbed(IsPlayer1Turn, $"{Connect4Methods.GetFinalTitle(this, true)}\n{Connect4Methods.GetGrid(Grid)}"); x.Components = Connect4Methods.GetButtons(this, true).Build(); });
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }
        }

        public async Task EndBotTurn(SocketMessageComponent component = null)
        {
            Turns++;

            Action<MessageProperties> properties;

            // // Check if there is a winner or the game is over
            // int winner = TTTMethods.GetWinner(grid, turns);
            // if (winner > 0 || turns >= 9)
            // {
            //     properties = (x) =>
            //     {
            //         x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, GetFinalTitle(winner)).Build();
            //         x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
            //     };

            //     await FinishGame(component, properties);
            // }
            // else // not over
            // {
            //     if (isPlayer1Turn)
            //     {
            //         isPlayer1Turn = false;
            //     }
            //     else
            //     {
            //         isPlayer1Turn = true;
            //     }

            //     var dateTime = new DateTimeOffset(ExpirationTime).ToUnixTimeSeconds();

            //     properties = (x) =>
            //     {
            //         x.Embed = TTTMethods.CreateEmbed(isPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(isPlayer1Turn ? Player1.Mention : Player2.Mention)} turn.\n(Ends in <t:{dateTime}:R>)").Build();
            //         x.Components = TTTMethods.GetButtons(grid, turns, Id).Build();
            //     };

            //     if (component != null)
            //     {
            //         await component.ModifyOriginalResponseAsync(properties);
            //     }
            //     else
            //     {
            //         await Message.ModifyAsync(properties);
            //     }

            //     if (!isPlayer1Turn && winner == 0)
            //     {
            //         await TTTMethods.BotPlay(this);
            //     }
            // }
        }

        private async Task FinishGame(SocketMessageComponent interaction, Action<MessageProperties> properties)
        {
            try
            {
                if (interaction != null)
                {
                    await interaction.ModifyOriginalResponseAsync(properties);
                }
                else
                {
                    await Message.ModifyAsync(properties);
                }

                // If not a bot match update stats.
                if (!Player2.IsBot)
                {
                    Challenge.DecrementUserChallenges(Player1.Id);
                    Challenge.DecrementUserChallenges(Player2.Id);

                    await Challenge.UpdateUserStats(this, Connect4Methods.GetWinner(this));
                }
            }
            catch (Exception)
            {
                // Do nothing Because the game will already be deleted.
            }

            // End Game
            _ = EndGame();
        }

        public async Task EndTurn(SocketMessageComponent component)
        {
            Turns++;

            // Reset Expiration Time.
            UpdateExpirationTime(TimeSpan.FromMinutes(1));

            Action<MessageProperties> properties;

            // Check if there is a winner or the game is over
            int winner = Connect4Methods.GetWinnerOutcome(Grid, Turns, LastMoveColumn, LastMoveRow);
            if (winner > 0 || Turns >= 42)
            {
                properties = (x) =>
                {
                    x.Embed = Connect4Methods.CreateEmbed(IsPlayer1Turn, $"{Connect4Methods.GetFinalTitle(this)}\n{Connect4Methods.GetGrid(Grid)}");
                    x.Components = Connect4Methods.GetButtons(this).Build();
                };

                await FinishGame(component, properties);
            }
            else // not over
            {
                if (IsPlayer1Turn)
                {
                    IsPlayer1Turn = false;
                }
                else
                {
                    IsPlayer1Turn = true;
                }

                properties = (x) =>
                {
                    x.Embed = Connect4Methods.CreateEmbed(IsPlayer1Turn, $"### ⚔️ {Player1.Mention} Challenges {Player2.Mention} to {Title}.\n{(IsPlayer1Turn ? Player1.Mention : Player2.Mention)} turn (Forfeit {TimeStamp.FromDateTime(ExpirationTime, TimeStamp.Formats.Relative)}).\n{Connect4Methods.GetGrid(Grid)}");
                    x.Components = Connect4Methods.GetButtons(this).Build();
                };

                await component.ModifyOriginalResponseAsync(properties);
            }
        }
    }
}