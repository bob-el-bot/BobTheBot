using System;
using System.ComponentModel.DataAnnotations;

namespace Database.Types
{
    public class User
    {
        [Key]
        public ulong Id { get; set; }
        // Premium
        public DateTimeOffset PremiumExpiration { get; set; }
        // Profile
        public string ProfileColor { get; set; }
        // Stats
        public float TriviaWins { get; set; } 
        public int TotalTriviaGames { get; set; }
        public float TicTacToeWins { get; set; }
        public int TotalTicTacToeGames { get; set; }
        public float RockPaperScissorsWins { get; set; }
        public int TotalRockPaperScissorsGames { get; set; }
    }
}