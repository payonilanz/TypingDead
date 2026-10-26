using System;

namespace TypingDead.Game
{
    /// <summary>
    /// Owns the Easy -> Medium -> Hard unlock sequence. UI screens ask this
    /// class for state and never calculate unlock rules themselves.
    /// </summary>
    public sealed class ProgressionManager
    {
        private readonly bool[] _completed;
        private readonly bool[] _unlocked;

        public event EventHandler Changed;

        public ProgressionManager()
        {
            _completed = new bool[3];
            _unlocked = new bool[3];
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < _completed.Length; i++)
            {
                _completed[i] = false;
                _unlocked[i] = false;
            }
            _unlocked[(int)GameDifficulty.Easy] = true;
            RaiseChanged();
        }

        public bool IsUnlocked(GameDifficulty difficulty)
        {
            return _unlocked[(int)difficulty];
        }

        public bool IsCompleted(GameDifficulty difficulty)
        {
            return _completed[(int)difficulty];
        }

        public bool AllModesCompleted
        {
            get
            {
                return _completed[(int)GameDifficulty.Easy] &&
                       _completed[(int)GameDifficulty.Medium] &&
                       _completed[(int)GameDifficulty.Hard];
            }
        }

        /// <summary>Returns the mode unlocked by this completion, or null.</summary>
        public GameDifficulty? Complete(GameDifficulty difficulty)
        {
            _completed[(int)difficulty] = true;
            _unlocked[(int)difficulty] = true;

            GameDifficulty? next = null;
            if (difficulty == GameDifficulty.Easy && !_unlocked[(int)GameDifficulty.Medium])
            {
                _unlocked[(int)GameDifficulty.Medium] = true;
                next = GameDifficulty.Medium;
            }
            else if (difficulty == GameDifficulty.Medium && !_unlocked[(int)GameDifficulty.Hard])
            {
                _unlocked[(int)GameDifficulty.Hard] = true;
                next = GameDifficulty.Hard;
            }
            RaiseChanged();
            return next;
        }

        /// <summary>
        /// Any failed run resets later modes. Easy always remains available, so
        /// Medium and Hard must be earned again in order.
        /// </summary>
        public void Fail(GameDifficulty difficulty)
        {
            _completed[(int)GameDifficulty.Easy] = false;
            _completed[(int)GameDifficulty.Medium] = false;
            _completed[(int)GameDifficulty.Hard] = false;
            _unlocked[(int)GameDifficulty.Easy] = true;
            _unlocked[(int)GameDifficulty.Medium] = false;
            _unlocked[(int)GameDifficulty.Hard] = false;
            RaiseChanged();
        }

        public string UnlockNotice(GameDifficulty? unlocked)
        {
            if (!unlocked.HasValue) return String.Empty;
            return DifficultySettings.GetDisplayName(unlocked.Value) + " MODE UNLOCKED";
        }

        private void RaiseChanged()
        {
            if (Changed != null) Changed(this, EventArgs.Empty);
        }
    }
}
