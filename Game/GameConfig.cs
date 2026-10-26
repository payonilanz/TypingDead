namespace TypingDead.Game
{
    /// <summary>
    /// Shared layout and balance values. The game is drawn on a fixed 1280x720
    /// canvas, which keeps pixel positions identical on every monitor.
    /// </summary>
    public static class GameConfig
    {
        public const int VirtualWidth = 1280;
        public const int VirtualHeight = 720;

        public const float HudHeight = 92f;
        public const float BattlefieldTop = 92f;
        public const float BattlefieldBottom = 564f;

        // Exactly three lanes. All movement, spawning and drawing use this one
        // value, so a fourth lane cannot accidentally appear.
        public const int LaneCount = 3;
        public const float LaneHeight = (BattlefieldBottom - BattlefieldTop) / LaneCount;

        public const float PlayerX = 150f;
        public const float CollisionX = 214f;
        public const float SpawnX = 1208f;

        // One mode is one 100-point run. Standard zombies award ten points.
        public const int ModeScoreTarget = 100;
        public const int ScorePerKill = 10;
        public const int TankScore = 20;
        public const int SpecialScore = 25;
        public const int StartingLives = 3;

        public const string GameTitle = "TYPING DEAD";
        public const string Tagline = "TYPE THE WORD. PRESS ENTER. SURVIVE THE HORDE.";
        public const string GroupLabel = "J. Francia // K. Galay // L. Garcia // C. Hernandez";
        public const string Version = "TYPING DEAD // PIXEL SURVIVAL BUILD 2.0";

        public static float LaneCenter(int lane)
        {
            if (lane < 0) lane = 0;
            if (lane >= LaneCount) lane = LaneCount - 1;
            return BattlefieldTop + LaneHeight * (lane + 0.5f);
        }

        public static float LaneTop(int lane)
        {
            if (lane < 0) lane = 0;
            if (lane >= LaneCount) lane = LaneCount - 1;
            return BattlefieldTop + LaneHeight * lane;
        }
    }
}
