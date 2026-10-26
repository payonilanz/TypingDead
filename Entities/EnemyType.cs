namespace TypingDead.Entities
{
    public enum EnemyType
    {
        Walker,
        Runner,
        Tank,
        Special
    }

    public enum EnemyState
    {
        Spawning,
        Active,
        Dying,
        Dead,
        Escaped
    }
}
