namespace TypingDead.Game
{
    /// <summary>
    /// Explicit application/game states prevent contradictory conditions such
    /// as continuing enemy movement while the pause menu is open.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        DifficultySelection,
        Playing,
        Paused,
        ModeComplete,
        GameOver,
        Victory,
        Settings,
        Tutorial
    }
}
