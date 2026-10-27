using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using TypingDead.Game;

namespace TypingDead.Systems
{
    [DataContract]
    public sealed class HighScoreData
    {
        [DataMember(Name = "easy")] public int Easy { get; set; }
        [DataMember(Name = "medium")] public int Medium { get; set; }
        [DataMember(Name = "hard")] public int Hard { get; set; }
        [DataMember(Name = "easyTimeMs")] public int EasyTimeMs { get; set; }
        [DataMember(Name = "mediumTimeMs")] public int MediumTimeMs { get; set; }
        [DataMember(Name = "hardTimeMs")] public int HardTimeMs { get; set; }

        public int Get(GameDifficulty difficulty)
        {
            switch (difficulty)
            {
                case GameDifficulty.Easy: return Easy;
                case GameDifficulty.Medium: return Medium;
                default: return Hard;
            }
        }

        public void Set(GameDifficulty difficulty, int value)
        {
            if (value < 0) value = 0;
            if (value > GameConfig.ModeScoreTarget) value = GameConfig.ModeScoreTarget;
            switch (difficulty)
            {
                case GameDifficulty.Easy: Easy = value; break;
                case GameDifficulty.Medium: Medium = value; break;
                default: Hard = value; break;
            }
        }

        public int GetTimeMs(GameDifficulty difficulty)
        {
            switch (difficulty)
            {
                case GameDifficulty.Easy: return EasyTimeMs;
                case GameDifficulty.Medium: return MediumTimeMs;
                default: return HardTimeMs;
            }
        }

        public void SetTimeMs(GameDifficulty difficulty, int value)
        {
            if (value < 0) value = 0;
            switch (difficulty)
            {
                case GameDifficulty.Easy: EasyTimeMs = value; break;
                case GameDifficulty.Medium: MediumTimeMs = value; break;
                default: HardTimeMs = value; break;
            }
        }
    }

    /// <summary>
    /// Per-session best scores. Every program launch starts from zero, so the
    /// score display never carries over from a previous run. Best values are
    /// still tracked and saved while the current session is open.
    /// </summary>
    public sealed class HighScoreManager
    {
        private readonly string _path;
        public HighScoreData Data { get; private set; }

        public HighScoreManager()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TypingDead");
            _path = Path.Combine(folder, "highscores.json");

            // Deliberately do not load the previous session's file. The score
            // display must begin at 0 on every program start.
            Data = new HighScoreData();
        }

        public int GetBest(GameDifficulty difficulty)
        {
            int best = Data.Get(difficulty);
            if (best < 0) return 0;
            return Math.Min(best, GameConfig.ModeScoreTarget);
        }

        /// <summary>Returns true when the score is a new personal best.</summary>
        public bool Report(GameDifficulty difficulty, int score)
        {
            if (score <= Data.Get(difficulty)) return false;
            Data.Set(difficulty, score);
            Save();
            return true;
        }

        /// <summary>Fastest completed run for the mode, or zero when unset.</summary>
        public int GetBestTimeMs(GameDifficulty difficulty)
        {
            int best = Data.GetTimeMs(difficulty);
            return best > 0 ? best : 0;
        }

        /// <summary>Records a completed run when it is faster than the best.</summary>
        public bool ReportTime(GameDifficulty difficulty, TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return false;
            int candidate = (int)Math.Min((double)Int32.MaxValue,
                Math.Round(duration.TotalMilliseconds));
            if (candidate < 1) candidate = 1;

            int current = Data.GetTimeMs(difficulty);
            if (current > 0 && candidate >= current) return false;

            Data.SetTimeMs(difficulty, candidate);
            Save();
            return true;
        }

        public static string FormatTime(int milliseconds)
        {
            if (milliseconds <= 0) return "--:--";
            int totalSeconds = milliseconds / 1000;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void Save()
        {
            try
            {
                string folder = Path.GetDirectoryName(_path);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(HighScoreData));
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, Data);
                    File.WriteAllText(_path, Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8);
                }
            }
            catch
            {
                // High scores are optional; a read-only profile must not crash play.
            }
        }
    }
}
