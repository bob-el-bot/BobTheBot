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
        public bool ConfessionsOff { get; set; }
        // Message Scheduling
        public uint TotalScheduledMessages { get; set; }
        public uint TotalScheduledAnnouncements { get; set; }
        // Badges
        public ulong EarnedBadges { get; set; }
        // Stats
        public uint WinStreak { get; set; }
        public float Connect4Wins { get; set; }
        public int TotalConnect4Games { get; set; }
        public float TriviaWins { get; set; }
        public int TotalTriviaGames { get; set; }
        public float TicTacToeWins { get; set; }
        public int TotalTicTacToeGames { get; set; }
        public float RockPaperScissorsWins { get; set; }
        public int TotalRockPaperScissorsGames { get; set; }
    }
}