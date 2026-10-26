using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using TypingDead.Effects;
using TypingDead.Entities;
using TypingDead.Systems;
using TypingDead.UI;

namespace TypingDead.Game
{
    /// <summary>
    /// The gameplay authority. It owns the player, enemies, spawning, scoring,
    /// lives, typing validation and the single transition into a finished mode.
    /// Rendering only reads this object; it never changes game state.
    /// </summary>
    public sealed class GameManager
    {
        private readonly AudioManager _audio;
        private readonly EffectManager _effects;
        private readonly Random _random;
        private readonly GameDifficulty _difficultyLevel;
        private readonly DifficultySettings _difficulty;
        private readonly WordManager _wordManager;
        private readonly SpawnManager _spawnManager;
        private readonly ScoreManager _score;

        private readonly List<Enemy> _enemies;
        private readonly ReadOnlyCollection<Enemy> _enemyView;
        private readonly List<Projectile> _projectiles;
        private readonly ReadOnlyCollection<Projectile> _projectileView;
        private readonly Player _player;

        // OOP PILLAR 1: ENCAPSULATION
        // Lives, score, timing and completion flags are private. Other classes
        // can read them through properties, but only methods such as LoseLife,
        // ResolveProjectile and BeginModeComplete may change them. This is what
        // prevents a paint method or a repeated key event from editing the score.
        private GameState _state;
        private int _lives;
        private float _elapsed;
        private float _inputLock;
        private float _statusTimer;
        private float _statusDuration;
        private string _statusMessage;
        private Color _statusColour;
        private float _scoreFlash;
        private float _lifeFlash;
        private float _endDelay;
        private bool _endRaised;
        private bool _modeCompleted;
        private bool _wordHadMistake;
        private int _lastChargeMilestone;
        private int _perfectWords;
        private bool _spawnsEnabled = true;

        public event EventHandler<GameEndedEventArgs> GameEnded;
        public event EventHandler TargetChanged;

        public GameState State { get { return _state; } }
        public int Lives { get { return _lives; } }
        public int Score { get { return _score.Score; } }
        public int ScoreTarget { get { return GameConfig.ModeScoreTarget; } }
        public float ScoreProgress
        {
            get { return Math.Max(0f, Math.Min(1f, (float)Score / ScoreTarget)); }
        }
        public int Kills { get { return _score.Kills; } }
        public int Streak { get { return _score.Streak; } }
        public int BestStreak { get { return _score.BestStreak; } }
        public int PerfectWords { get { return _perfectWords; } }
        public float Accuracy { get { return _score.Accuracy; } }
        public GameDifficulty Difficulty { get { return _difficultyLevel; } }
        public DifficultySettings DifficultySettings { get { return _difficulty; } }
        public Player Player { get { return _player; } }
        public IList<Enemy> Enemies { get { return _enemyView; } }
        public IList<Projectile> Projectiles { get { return _projectileView; } }
        public float ElapsedTime { get { return _elapsed; } }
        public float ScoreFlash { get { return _scoreFlash; } }
        public float LifeFlash { get { return _lifeFlash; } }
        public float InputLock { get { return Math.Max(0f, _inputLock); } }
        public bool ModeCompleted { get { return _modeCompleted; } }
        public string StatusMessage { get { return _statusMessage; } }
        public Color StatusColour { get { return _statusColour; } }
        public float StatusAlpha
        {
            get
            {
                if (_statusTimer <= 0f || _statusDuration <= 0f) return 0f;
                float age = _statusDuration - _statusTimer;
                float fadeIn = Math.Min(1f, age / 0.12f);
                float fadeOut = Math.Min(1f, _statusTimer / 0.35f);
                return Math.Max(0f, Math.Min(1f, Math.Min(fadeIn, fadeOut)));
            }
        }
        public float EndDelayRemaining { get { return Math.Max(0f, _endDelay); } }

        public GameManager(GameDifficulty difficulty, GameSettings settings,
            AudioManager audio, EffectManager effects)
            : this(difficulty, settings, audio, effects, Environment.TickCount)
        {
        }

        public GameManager(GameDifficulty difficulty, GameSettings settings,
            AudioManager audio, EffectManager effects, int seed)
        {
            _difficultyLevel = difficulty;
            _difficulty = DifficultySettings.Create(difficulty);
            _audio = audio;
            _effects = effects;
            _random = new Random(seed);
            _wordManager = new WordManager(_random);
            _spawnManager = new SpawnManager(_random, _wordManager);
            _score = new ScoreManager();
            _enemies = new List<Enemy>();
            _enemyView = new ReadOnlyCollection<Enemy>(_enemies);
            _projectiles = new List<Projectile>();
            _projectileView = new ReadOnlyCollection<Projectile>(_projectiles);
            _player = new Player();
            _state = GameState.Playing;
            ResetSession(settings);
        }

        private void ResetSession(GameSettings settings)
        {
            _player.Reset();
            _enemies.Clear();
            _projectiles.Clear();
            _score.Reset();
            _lives = GameConfig.StartingLives;
            _elapsed = 0f;
            _inputLock = 0f;
            _statusMessage = "REACH 100 POINTS";
            _statusColour = Palette.PrimaryText;
            _statusTimer = 2.3f;
            _statusDuration = 2.3f;
            _scoreFlash = 0f;
            _lifeFlash = 0f;
            _endDelay = 0f;
            _endRaised = false;
            _modeCompleted = false;
            _wordHadMistake = false;
            _lastChargeMilestone = 0;
            _perfectWords = 0;
            _state = GameState.Playing;
            _wordManager.ResetRecent();
            _spawnManager.Reset(0.85f);
            _effects.Reset();
            if (settings != null) _effects.ApplySettings(settings);
            _audio.Play(GameSound.GameStart);
        }

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            // Paused gameplay is completely frozen: no movement, spawn timer,
            // projectile, damage or survival-time update runs.
            if (_state == GameState.Paused) return;

            _player.Update(deltaTime);

            if (_state == GameState.ModeComplete || _state == GameState.GameOver ||
                _state == GameState.Victory)
            {
                if (_endDelay > 0f) _endDelay = Math.Max(0f, _endDelay - deltaTime);
                _effects.SetCharge(_player.MuzzlePosition, 0f, deltaTime);
                if (_endDelay <= 0f && !_endRaised)
                {
                    _endRaised = true;
                    RaiseGameEnded();
                }
                return;
            }

            if (_state != GameState.Playing) return;

            _elapsed += deltaTime;
            if (_scoreFlash > 0f) _scoreFlash = Math.Max(0f, _scoreFlash - deltaTime * 2.4f);
            if (_lifeFlash > 0f) _lifeFlash = Math.Max(0f, _lifeFlash - deltaTime * 1.8f);
            if (_statusTimer > 0f) _statusTimer = Math.Max(0f, _statusTimer - deltaTime);
            if (_inputLock > 0f) _inputLock = Math.Max(0f, _inputLock - deltaTime);

            UpdateEnemies(deltaTime);
            if (_state != GameState.Playing) return;

            UpdateProjectiles(deltaTime);
            if (_state != GameState.Playing) return;

            if (_spawnsEnabled && !_modeCompleted && Score < ScoreTarget)
            {
                Enemy spawned = _spawnManager.TrySpawn(deltaTime, _difficulty, _enemies, _elapsed);
                if (spawned != null)
                {
                    _enemies.Add(spawned);
                    _effects.SpawnSpawnEffect(new PointF(spawned.VisualX, spawned.Y), spawned.AccentColor);
                }
            }

            UpdateTargetSelection(false);
            UpdateChargeEffect(deltaTime);

            if (Score >= ScoreTarget)
            {
                BeginModeComplete();
            }
        }

        private void UpdateEnemies(float deltaTime)
        {
            float terminalSpeed = _difficulty.WalkerSpeed;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _enemies[i];
                enemy.Update(deltaTime, _difficulty.SpeedFor(enemy.Type));

                if (enemy.State == EnemyState.Active && enemy.X <= GameConfig.CollisionX)
                {
                    if (enemy.MarkEscaped())
                    {
                        _enemies.RemoveAt(i);
                        _score.RegisterEnemyEscaped();
                        _effects.PlayerDamaged(new PointF(GameConfig.PlayerX + 20f, _player.VisualY), true);
                        _audio.Play(GameSound.Damage);
                        SetStatus("BREACH // -1 LIFE", Palette.Blood, 1.6f);
                        LoseLife();
                        if (_state != GameState.Playing) return;
                    }
                }
                else if (enemy.IsReadyForRemoval)
                {
                    _enemies.RemoveAt(i);
                }
            }
        }

        private void UpdateProjectiles(float deltaTime)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = _projectiles[i];
                projectile.Update(deltaTime);
                if (projectile.ShouldEmitTrail)
                {
                    _effects.SpawnTrail(projectile.Position, projectile.Direction,
                        projectile.Perfect ? Palette.Warning : Palette.Success, projectile.Strength);
                }

                if (!projectile.HasArrived) continue;
                _projectiles.RemoveAt(i);
                ResolveProjectile(projectile);
                if (_state != GameState.Playing) return;
            }
        }

        private void ResolveProjectile(Projectile projectile)
        {
            Enemy target = projectile.Target;
            if (target == null) return;

            if (target.State != EnemyState.Active || target.Health <= 0 ||
                target.WordIndex != projectile.LaunchedWordIndex ||
                !String.Equals(target.Word, projectile.LaunchedWord, StringComparison.Ordinal))
            {
                if (target != null) target.CancelIncomingAttack();
                return;
            }

            PointF impact = new PointF(target.VisualX, target.Y - 8f);
            float burstScale = target.Type == EnemyType.Tank ? 1.45f : 1f;
            _effects.SpawnBurst(impact, target.AccentColor, burstScale, projectile.Perfect);
            _effects.SpawnReaction(impact, target.AccentColor);
            _audio.Play(GameSound.Impact);

            bool killed = target.ApplyHit();
            if (!killed)
            {
                string progress = target.MaxHealth > 1
                    ? " // " + target.Health.ToString() + "/" + target.MaxHealth.ToString()
                    : String.Empty;
                SetStatus("HIT" + progress, Palette.Warning, 1.4f);
                _effects.AddScorePopup(target.MaxHealth > 1 ? "HIT" : "HIT",
                    new PointF(target.VisualX, target.Y - 42f), Palette.Warning);
                return;
            }

            int before = Score;
            int reward = RewardFor(target.Type);
            _score.RegisterKill(reward, ScoreTarget);
            int gained = Score - before;
            _scoreFlash = 1f;
            if (projectile.Perfect) _perfectWords++;

            _effects.SpawnAftermath(impact, target.AccentColor, target.Type,
                target.Type == EnemyType.Tank || target.Type == EnemyType.Special);
            if (gained > 0)
            {
                _effects.AddScorePopup("+" + gained.ToString(),
                    new PointF(target.VisualX, target.Y - 42f), Palette.Success);
            }
            _audio.Play(GameSound.EnemyDeath);
            SetStatus(projectile.Perfect ? "PERFECT // +" + gained.ToString() :
                "KILL // +" + gained.ToString(), Palette.Success, 1.3f);

            // The score threshold is checked once at the end of Update, after
            // every projectile and reward has been resolved.
        }

        private int RewardFor(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Tank: return GameConfig.TankScore;
                case EnemyType.Special: return GameConfig.SpecialScore;
                default: return GameConfig.ScorePerKill;
            }
        }

        /// <summary>
        /// Moves lanes. It deliberately does not submit typing, attack anything,
        /// or reset any zombie's health or current word.
        /// </summary>
        public void MoveLane(int direction)
        {
            if (_state != GameState.Playing) return;
            int newLane = _player.Lane + (direction < 0 ? -1 : 1);
            if (newLane < 0 || newLane >= GameConfig.LaneCount) return;
            if (!_player.SetLane(newLane)) return;

            _wordHadMistake = false;
            _lastChargeMilestone = 0;
            bool changed = UpdateTargetSelection(true);
            if (!changed || _player.Target == null)
            {
                SetStatus("NO TARGET // MOVE UP/DOWN", Palette.SecondaryText, 1.1f);
            }
        }

        /// <summary>
        /// Adds one typed character. Wrong characters are shown in red, but only
        /// an incorrect Enter submission can cost a life.
        /// </summary>
        public void SubmitCharacter(char character)
        {
            if (_state != GameState.Playing || _inputLock > 0f) return;

            TypingResult result = _player.SubmitCharacter(character);
            switch (result)
            {
                case TypingResult.NoTarget:
                    SetStatus("NO TARGET // MOVE UP/DOWN", Palette.Warning, 1.1f);
                    break;

                case TypingResult.Blocked:
                    break;

                case TypingResult.Accepted:
                    _score.RegisterCorrectCharacter();
                    _audio.Play(GameSound.TypeCorrect);
                    TryChargeMilestoneSound();
                    break;

                case TypingResult.WrongKey:
                    _wordHadMistake = true;
                    _score.RegisterError();
                    _audio.Play(GameSound.TypeWrong);
                    SetStatus("WRONG KEY // FIX IT", Palette.Warning, 0.9f);
                    break;
            }
        }

        public void Backspace()
        {
            if (_state != GameState.Playing || _inputLock > 0f) return;
            _player.Backspace();
        }

        /// <summary>
        /// The only attack entry point. Enter calls this once. It rejects empty
        /// input safely and deducts exactly one life for a wrong non-empty word.
        /// </summary>
        public void SubmitWord()
        {
            if (_state != GameState.Playing || _inputLock > 0f) return;

            string typed = _player.TypedInput;
            if (typed.Length == 0)
            {
                SetStatus("TYPE A WORD FIRST", Palette.Warning, 1.3f);
                _audio.Play(GameSound.MenuHover);
                return;
            }

            Enemy target = _player.Target;
            if (target == null || !target.IsTargetable || target.Lane != _player.Lane)
            {
                _player.ValidateTarget();
                UpdateTargetSelection(false);
                SetStatus("NO TARGET // MOVE UP/DOWN", Palette.SecondaryText, 1.2f);
                return;
            }

            if (!String.Equals(typed, target.Word, StringComparison.Ordinal))
            {
                _player.ClearTyping();
                _score.RegisterError();
                _inputLock = 0.55f;
                _wordHadMistake = true;
                _lifeFlash = 1f;
                _player.TriggerDamage();
                _effects.WrongInput(new PointF(GameConfig.PlayerX + 20f, _player.VisualY));
                _audio.Play(GameSound.TypeWrong);
                SetStatus("WRONG WORD // -1 LIFE", Palette.Blood, 1.8f);
                LoseLife();
                return;
            }

            LaunchAttack(target);
        }

        private void LaunchAttack(Enemy target)
        {
            // ReserveForAttack is checked inside the enemy, so a delayed or
            // repeated Enter cannot launch two projectiles at the same word.
            if (target == null || target.HasIncomingAttack || !target.IsTargetable) return;

            bool perfect = !_wordHadMistake;
            target.ReserveForAttack();
            PointF origin = _player.MuzzlePosition;
            Projectile projectile = new Projectile(origin, target, perfect, 1f + _player.ChargeProgress * 0.35f);
            _projectiles.Add(projectile);
            _player.TriggerAttack();
            _player.ClearTarget();
            _effects.SpawnMuzzle(origin, perfect ? 1.35f : 1f);
            _audio.Play(GameSound.Fire);
            SetStatus("ATTACK LAUNCHED", Palette.Success, 0.9f);

            _wordHadMistake = false;
            _lastChargeMilestone = 0;
            UpdateTargetSelection(false);
        }

        private void TryChargeMilestoneSound()
        {
            float progress = _player.ChargeProgress;
            int milestone = progress >= 0.99f ? 3 : (progress >= 0.66f ? 2 : (progress >= 0.33f ? 1 : 0));
            if (milestone > _lastChargeMilestone)
            {
                _lastChargeMilestone = milestone;
                _audio.Play(GameSound.Charge);
            }
        }

        private void UpdateChargeEffect(float deltaTime)
        {
            if (_player.Target != null && _state == GameState.Playing && _inputLock <= 0f)
            {
                _effects.SetCharge(_player.MuzzlePosition, _player.ChargeProgress, deltaTime);
            }
            else
            {
                _effects.SetCharge(_player.MuzzlePosition, 0f, deltaTime);
            }
        }

        private bool UpdateTargetSelection(bool force)
        {
            Enemy previous = _player.Target;
            _player.ValidateTarget();

            if (_player.Target == null)
            {
                Enemy nearest = FindNearestTarget(_player.Lane);
                if (nearest != null)
                {
                    if (_player.AssignTarget(nearest))
                    {
                        _wordHadMistake = false;
                        _lastChargeMilestone = 0;
                        _audio.Play(GameSound.TargetAcquired);
                        SetStatus("TARGET // LANE " + (nearest.Lane + 1).ToString("00"),
                            Palette.Success, 1.0f);
                        if (TargetChanged != null) TargetChanged(this, EventArgs.Empty);
                        return true;
                    }
                }
            }

            if (previous != _player.Target && TargetChanged != null)
            {
                TargetChanged(this, EventArgs.Empty);
            }
            return false;
        }

        private Enemy FindNearestTarget(int lane)
        {
            Enemy best = null;
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                if (enemy.Lane != lane || !enemy.IsTargetable) continue;
                if (best == null || enemy.X < best.X)
                {
                    best = enemy;
                }
            }
            return best;
        }

        /// <summary>Deducts exactly one life from one resolved event.</summary>
        private void LoseLife()
        {
            if (_state != GameState.Playing) return;
            _lives = Math.Max(0, _lives - 1);
            _lifeFlash = 1f;
            _player.TriggerDamage();
            if (_lives <= 0)
            {
                BeginDefeat();
            }
        }

        private void BeginModeComplete()
        {
            if (_state != GameState.Playing || _modeCompleted) return;
            _modeCompleted = true;
            _state = _difficultyLevel == GameDifficulty.Hard ? GameState.Victory : GameState.ModeComplete;
            _endDelay = 1.25f;
            _endRaised = false;
            _player.ClearTarget();
            _player.ClearTyping();
            _effects.Flash(Palette.Success, 0.22f, 0.65f);
            _effects.SpawnLevelUp();
            _audio.Play(_difficultyLevel == GameDifficulty.Hard ? GameSound.Victory : GameSound.LevelUp);
            SetStatus("MODE CLEARED", Palette.Success, 2.2f);
        }

        private void BeginDefeat()
        {
            if (_state == GameState.GameOver || _state == GameState.Victory) return;
            _state = GameState.GameOver;
            _endDelay = 0.95f;
            _endRaised = false;
            _player.ClearTarget();
            _player.ClearTyping();
            _effects.Flash(Palette.Blood, 0.2f, 0.55f);
            _audio.Play(GameSound.GameOver);
            SetStatus("MODE FAILED // NO LIVES", Palette.Blood, 2.0f);
        }

        public bool Pause()
        {
            if (_state != GameState.Playing) return false;
            _state = GameState.Paused;
            _audio.Play(GameSound.Pause);
            return true;
        }

        public bool Resume()
        {
            if (_state != GameState.Paused) return false;
            _state = GameState.Playing;
            _audio.Play(GameSound.Resume);
            return true;
        }

        public bool TogglePause()
        {
            if (_state == GameState.Playing) return Pause();
            if (_state == GameState.Paused) return Resume();
            return false;
        }

        private void SetStatus(string message, Color colour, float duration)
        {
            _statusMessage = message;
            _statusColour = colour;
            _statusDuration = Math.Max(0.2f, duration);
            _statusTimer = _statusDuration;
        }

        private void RaiseGameEnded()
        {
            if (GameEnded == null) return;
            GameResult result = new GameResult();
            result.Victory = _modeCompleted;
            result.ModeCompleted = _modeCompleted;
            result.FinalVictory = _modeCompleted && _difficultyLevel == GameDifficulty.Hard;
            result.Score = Score;
            result.ScoreTarget = ScoreTarget;
            result.Kills = Kills;
            result.Difficulty = _difficultyLevel;
            result.Accuracy = Accuracy;
            result.BestStreak = BestStreak;
            result.PerfectWords = _perfectWords;
            result.LivesRemaining = _lives;
            result.Duration = TimeSpan.FromSeconds(_elapsed);
            GameEnded(this, new GameEndedEventArgs(result));
        }

        /// <summary>
        /// Adds a deterministic enemy for automated tests. This is internal so
        /// normal keyboard input cannot inject an enemy into a run.
        /// </summary>
        internal void AddEnemyForTest(Enemy enemy)
        {
            if (enemy == null) return;
            _enemies.Add(enemy);
        }

        internal void ForceInputLock(float seconds)
        {
            _inputLock = Math.Max(_inputLock, seconds);
        }

        /// <summary>
        /// Adds points without a projectile. Used only by the deterministic
        /// self-test to exercise the 100-point completion boundary quickly.
        /// </summary>
        internal void RegisterKillForTest()
        {
            _score.RegisterKill(GameConfig.ScorePerKill, ScoreTarget);
            _scoreFlash = 1f;
        }

        internal void RegisterKillForTest(int points)
        {
            _score.RegisterKill(points, ScoreTarget);
            _scoreFlash = 1f;
        }

        internal void ClearEnemiesForTest()
        {
            _enemies.Clear();
            _projectiles.Clear();
            _player.ClearTarget();
        }

        internal void DisableSpawningForTest()
        {
            _spawnsEnabled = false;
        }
    }
}
