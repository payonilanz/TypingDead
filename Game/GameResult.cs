using System;

namespace TypingDead.Game
{
    /// <summary>Snapshot of a finished mode attempt, used by the result screens.</summary>
    public sealed class GameResult
    {
        public bool Victory { get; set; }
        public bool ModeCompleted { get; set; }
        public bool FinalVictory { get; set; }
        public int Score { get; set; }
        public int ScoreTarget { get; set; }
        public int Kills { get; set; }
        public GameDifficulty Difficulty { get; set; }
        public float Accuracy { get; set; }
        public int BestStreak { get; set; }
        public int PerfectWords { get; set; }
        public int LivesRemaining { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public sealed class GameEndedEventArgs : EventArgs
    {
        public GameResult Result { get; private set; }

        public GameEndedEventArgs(GameResult result)
        {
            Result = result;
        }
    }
}
