namespace TypingDead.Game
{
    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Immutable tuning for one complete mode. There are no internal levels:
    /// a mode begins at zero points and ends once the player reaches 100.
    /// </summary>
    public sealed class DifficultySettings
    {
        public GameDifficulty Difficulty { get; private set; }
        public string Name { get; private set; }
        public string ShortImpact { get; private set; }
        public string Description { get; private set; }
        public string ZombieSummary { get; private set; }

        public float WalkerSpeed { get; private set; }
        public float RunnerSpeed { get; private set; }
        public float TankSpeed { get; private set; }
        public float SpecialSpeed { get; private set; }
        public float SpawnInterval { get; private set; }
        public int MaxEnemies { get; private set; }
        public int MinimumWordLength { get; private set; }
        public int MaximumWordLength { get; private set; }

        private DifficultySettings()
        {
        }

        public static DifficultySettings Create(GameDifficulty difficulty)
        {
            DifficultySettings settings = new DifficultySettings();
            settings.Difficulty = difficulty;
            switch (difficulty)
            {
                case GameDifficulty.Easy:
                    settings.Name = "EASY";
                    settings.ShortImpact = "START HERE";
                    settings.Description = "Short terms and slow walkers.";
                    settings.ZombieSummary = "WALKERS";
                    settings.WalkerSpeed = 45f;
                    settings.RunnerSpeed = 64f;
                    settings.TankSpeed = 26f;
                    settings.SpecialSpeed = 58f;
                    settings.SpawnInterval = 2.25f;
                    settings.MaxEnemies = 3;
                    settings.MinimumWordLength = 3;
                    settings.MaximumWordLength = 5;
                    break;

                case GameDifficulty.Medium:
                    settings.Name = "MEDIUM";
                    settings.ShortImpact = "FAST RUSH";
                    settings.Description = "More words, runners and Tanks.";
                    settings.ZombieSummary = "RUNNERS + TANKS";
                    settings.WalkerSpeed = 58f;
                    settings.RunnerSpeed = 88f;
                    settings.TankSpeed = 34f;
                    settings.SpecialSpeed = 80f;
                    settings.SpawnInterval = 1.72f;
                    settings.MaxEnemies = 5;
                    settings.MinimumWordLength = 5;
                    settings.MaximumWordLength = 9;
                    break;

                default:
                    settings.Name = "HARD";
                    settings.ShortImpact = "FINAL MODE";
                    settings.Description = "Long terms and relentless Runners.";
                    settings.ZombieSummary = "RUNNERS + WALKERS";
                    settings.WalkerSpeed = 70f;
                    settings.RunnerSpeed = 122f;
                    settings.TankSpeed = 50f;
                    settings.SpecialSpeed = 108f;
                    settings.SpawnInterval = 1.35f;
                    settings.MaxEnemies = 6;
                    settings.MinimumWordLength = 10;
                    settings.MaximumWordLength = 18;
                    break;
            }
            return settings;
        }

        public float SpeedFor(Entities.EnemyType type)
        {
            switch (type)
            {
                case Entities.EnemyType.Runner: return RunnerSpeed;
                case Entities.EnemyType.Tank: return TankSpeed;
                case Entities.EnemyType.Special: return SpecialSpeed;
                default: return WalkerSpeed;
            }
        }

        public static string GetDisplayName(GameDifficulty difficulty)
        {
            if (difficulty == GameDifficulty.Easy) return "EASY";
            if (difficulty == GameDifficulty.Medium) return "MEDIUM";
            return "HARD";
        }
    }
}
