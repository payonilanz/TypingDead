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
    }

    /// <summary>
    /// Local best scores per difficulty. Invalid or missing files are ignored.
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
            Data = new HighScoreData();
            Load();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                string json = File.ReadAllText(_path, Encoding.UTF8);
                if (String.IsNullOrWhiteSpace(json)) return;

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(HighScoreData));
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    HighScoreData data = serializer.ReadObject(stream) as HighScoreData;
                    if (data != null)
                    {
                        if (data.Easy < 0) data.Easy = 0;
                        if (data.Medium < 0) data.Medium = 0;
                        if (data.Hard < 0) data.Hard = 0;
                        if (data.Easy > GameConfig.ModeScoreTarget) data.Easy = GameConfig.ModeScoreTarget;
                        if (data.Medium > GameConfig.ModeScoreTarget) data.Medium = GameConfig.ModeScoreTarget;
                        if (data.Hard > GameConfig.ModeScoreTarget) data.Hard = GameConfig.ModeScoreTarget;
                        Data = data;
                    }
                }
            }
            catch
            {
                Data = new HighScoreData();
            }
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
