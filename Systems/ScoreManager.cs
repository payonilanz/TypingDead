using System;
using TypingDead.Game;

namespace TypingDead.Systems
{
    /// <summary>
    /// Tracks scoring and typing statistics. Score is derived from confirmed
    /// kills only. The score cap is applied here so a delayed effect or a
    /// duplicated event can never push the HUD beyond 100 points.
    /// </summary>
    public sealed class ScoreManager
    {
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int CorrectCharacters { get; private set; }
        public int Errors { get; private set; }

        public float Accuracy
        {
            get
            {
                int attempts = CorrectCharacters + Errors;
                if (attempts <= 0) return 100f;
                return (CorrectCharacters * 100f) / attempts;
            }
        }

        public int TotalAttempts
        {
            get { return CorrectCharacters + Errors; }
        }

        public void Reset()
        {
            Score = 0;
            Kills = 0;
            Streak = 0;
            BestStreak = 0;
            CorrectCharacters = 0;
            Errors = 0;
        }

        /// <summary>Adds a reward once and clamps it at the mode target.</summary>
        public int RegisterKill(int points, int scoreCap)
        {
            if (points < 0) points = 0;
            if (scoreCap < 0) scoreCap = 0;
            if (Score >= scoreCap) return Score;

            Kills++;
            Streak++;
            if (Streak > BestStreak) BestStreak = Streak;
            Score = Math.Min(scoreCap, Score + points);
            return Score;
        }

        public void RegisterCorrectCharacter()
        {
            CorrectCharacters++;
        }

        public void RegisterError()
        {
            Errors++;
            Streak = 0;
        }

        public void RegisterEnemyEscaped()
        {
            Streak = 0;
        }
    }
}
