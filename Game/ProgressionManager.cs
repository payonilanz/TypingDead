using System;

namespace TypingDead.Game
{
    /// <summary>
    /// Storage contract for permanent mode unlocks. The game uses a JSON store;
    /// tests can use an in-memory manager with no store.
    /// </summary>
    public interface IProgressionStore
    {
        void Load(bool[] unlocked, bool[] completed);
        void Save(bool[] unlocked, bool[] completed);
    }

    /// <summary>
    /// Owns the Easy -> Medium -> Hard unlock sequence. Unlocks are permanent:
    /// losing never removes them, and every change is written through the
    /// optional store so progress survives an application restart.
    /// </summary>
    public sealed class ProgressionManager
    {
        private readonly bool[] _completed;
        private readonly bool[] _unlocked;
        private readonly IProgressionStore _store;

        public event EventHandler Changed;

        public ProgressionManager() : this(null)
        {
        }

        public ProgressionManager(IProgressionStore store)
        {
            _completed = new bool[3];
            _unlocked = new bool[3];
            _store = store;
            ResetState();
            Load();
        }

        /// <summary>Explicitly returns to a fresh Easy-only progression.</summary>
        public void Reset()
        {
            ResetState();
            Save();
            RaiseChanged();
        }

        private void ResetState()
        {
            for (int i = 0; i < _completed.Length; i++)
            {
                _completed[i] = false;
                _unlocked[i] = false;
            }
            _unlocked[(int)GameDifficulty.Easy] = true;
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

            Normalize();
            Save();
            RaiseChanged();
            return next;
        }

        /// <summary>
        /// A failed run never removes unlocks or completion records. The
        /// parameter identifies the mode that ended so callers can report it.
        /// </summary>
        public void Fail(GameDifficulty difficulty)
        {
            RaiseChanged();
        }

        public string UnlockNotice(GameDifficulty? unlocked)
        {
            if (!unlocked.HasValue) return String.Empty;
            return DifficultySettings.GetDisplayName(unlocked.Value) + " MODE UNLOCKED";
        }

        private void Load()
        {
            if (_store == null) return;
            try
            {
                _store.Load(_unlocked, _completed);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            Normalize();
        }

        private void Save()
        {
            if (_store == null) return;
            try
            {
                _store.Save(_unlocked, _completed);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Keeps saved data consistent: Easy is always open, and a completed
        /// mode always leaves its successor unlocked.
        /// </summary>
        private void Normalize()
        {
            _unlocked[(int)GameDifficulty.Easy] = true;
            if (_completed[(int)GameDifficulty.Easy]) _unlocked[(int)GameDifficulty.Medium] = true;
            if (_completed[(int)GameDifficulty.Medium]) _unlocked[(int)GameDifficulty.Hard] = true;
            if (_unlocked[(int)GameDifficulty.Hard]) _unlocked[(int)GameDifficulty.Medium] = true;
            if (_unlocked[(int)GameDifficulty.Medium]) _unlocked[(int)GameDifficulty.Easy] = true;
        }

        private void RaiseChanged()
        {
            if (Changed != null) Changed(this, EventArgs.Empty);
        }
    }
}
