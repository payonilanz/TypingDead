using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    // OOP PILLAR 4: ABSTRACTION
    // Enemy is the abstract contract for every hostile. It defines movement,
    // health, targeting and damage, while each zombie supplies its own body
    // drawing and speed behaviour. GameManager can treat all zombies the same
    // without knowing how a Walker or Tank is built internally.
    // OOP PILLAR 2: INHERITANCE
    // WalkerEnemy, RunnerEnemy, TankEnemy and SpecialZombie inherit the shared
    // lane movement, word sequence, health, hit flash and death timer from this
    // base class. They only add the parts that make each zombie different.
    public abstract class Enemy
    {
        private static int _nextId = 1;

        private readonly int _lane;
        private readonly List<string> _words;
        private int _wordIndex;
        private int _health;
        private float _x;
        private EnemyState _state;
        private float _age;
        private float _spawnTimer;
        private float _deathTimer;
        private float _hitFlash;
        private float _knockback;
        private float _verticalWobble;

        public int Id { get; private set; }
        public int MaxHealth { get; private set; }

        public string Word
        {
            get { return _words[_wordIndex]; }
        }

        public int WordIndex
        {
            get { return _wordIndex; }
        }

        public int WordCount
        {
            get { return _words.Count; }
        }

        public int Lane
        {
            get { return _lane; }
        }

        public int Health
        {
            get { return _health; }
        }

        public EnemyState State
        {
            get { return _state; }
        }

        public float X
        {
            get { return _x; }
        }

        public float VisualX
        {
            get { return _x + _knockback; }
        }

        public float Y
        {
            get { return GameConfig.LaneCenter(_lane) + _verticalWobble; }
        }

        public float Age
        {
            get { return _age; }
        }

        public float HitFlash
        {
            get { return Math.Max(0f, Math.Min(1f, _hitFlash)); }
        }

        public float DeathProgress
        {
            get { return Math.Max(0f, Math.Min(1f, _deathTimer / DeathDuration)); }
        }

        public float SpawnProgress
        {
            get { return _state == EnemyState.Spawning ? 1f - Math.Max(0f, _spawnTimer / 0.48f) : 1f; }
        }

        public bool HasIncomingAttack { get; private set; }

        public bool IsTargetable
        {
            get { return _state == EnemyState.Active && _health > 0 && !HasIncomingAttack; }
        }

        public bool IsDangerous
        {
            get { return _state == EnemyState.Active && _health > 0; }
        }

        public bool IsReadyForRemoval
        {
            get { return _state == EnemyState.Dead || _state == EnemyState.Escaped; }
        }

        public abstract EnemyType Type { get; }
        public abstract Color AccentColor { get; }
        public abstract string Codename { get; }

        protected virtual float BodyScale
        {
            get { return 1f; }
        }

        protected virtual float DeathDuration
        {
            get { return 0.64f; }
        }

        public float ProgressToPlayer
        {
            get
            {
                float range = GameConfig.SpawnX - GameConfig.CollisionX;
                float travelled = GameConfig.SpawnX - _x;
                return Math.Max(0f, Math.Min(1f, travelled / range));
            }
        }

        protected Enemy(int lane, IList<string> words)
        {
            if (lane < 0 || lane >= GameConfig.LaneCount)
            {
                throw new ArgumentOutOfRangeException("lane");
            }
            if (words == null || words.Count == 0)
            {
                throw new ArgumentException("Enemy words cannot be empty.", "words");
            }

            _words = new List<string>();
            for (int i = 0; i < words.Count; i++)
            {
                string word = (words[i] ?? String.Empty).Trim().ToUpperInvariant();
                if (word.Length == 0)
                {
                    throw new ArgumentException("Enemy words cannot be empty.", "words");
                }
                if (_words.Contains(word))
                {
                    throw new ArgumentException("Enemy words must be distinct.", "words");
                }
                _words.Add(word);
            }

            Id = _nextId++;
            _lane = lane;
            MaxHealth = _words.Count;
            _health = MaxHealth;
            _wordIndex = 0;
            _x = GameConfig.SpawnX;
            _state = EnemyState.Spawning;
            _spawnTimer = 0.48f;
            _verticalWobble = 0f;
        }

        protected Enemy(int lane, string word)
            : this(lane, new string[] { word })
        {
        }

        /// <summary>Returns true only on the blow that defeats this zombie.</summary>
        public bool ApplyHit()
        {
            if (_state != EnemyState.Active || _health <= 0)
            {
                return false;
            }

            _health--;
            _hitFlash = 1f;
            _knockback = Math.Max(_knockback, Type == EnemyType.Tank ? 10f : 20f);
            HasIncomingAttack = false;

            if (_health <= 0)
            {
                _state = EnemyState.Dying;
                _deathTimer = 0f;
                return true;
            }

            _wordIndex = Math.Min(_words.Count - 1, MaxHealth - _health);
            return false;
        }

        public bool MarkEscaped()
        {
            if (_state != EnemyState.Active) return false;
            _state = EnemyState.Escaped;
            HasIncomingAttack = false;
            return true;
        }

        public void ReserveForAttack()
        {
            if (_state == EnemyState.Active)
            {
                HasIncomingAttack = true;
            }
        }

        public void CancelIncomingAttack()
        {
            HasIncomingAttack = false;
        }

        public void Update(float deltaTime, float terminalSpeed)
        {
            _age += deltaTime;
            _hitFlash = Math.Max(0f, _hitFlash - deltaTime * 5.2f);
            _knockback *= (float)Math.Pow(0.0005f, deltaTime);
            if (_knockback < 0.2f) _knockback = 0f;

            switch (_state)
            {
                case EnemyState.Spawning:
                    _spawnTimer -= deltaTime;
                    _verticalWobble = PixelBob(1.5f, 3.1f, Id);
                    if (_spawnTimer <= 0f)
                    {
                        _spawnTimer = 0f;
                        _state = EnemyState.Active;
                    }
                    break;

                case EnemyState.Active:
                    float speed = GetCurrentSpeed(terminalSpeed);
                    _x -= speed * deltaTime;
                    _verticalWobble = PixelBob(Type == EnemyType.Tank ? 1.2f : 2f, 3.2f, Id);
                    if (_x < GameConfig.CollisionX - 48f)
                    {
                        _x = GameConfig.CollisionX - 48f;
                    }
                    break;

                case EnemyState.Dying:
                    _deathTimer += deltaTime;
                    if (_deathTimer >= DeathDuration)
                    {
                        _state = EnemyState.Dead;
                    }
                    break;
            }
        }

        protected virtual float GetCurrentSpeed(float terminalSpeed)
        {
            return terminalSpeed;
        }

        private float PixelBob(float amplitude, float speed, int phase)
        {
            float wave = (float)Math.Sin(_age * speed + phase);
            return wave > 0.35f ? amplitude : (wave < -0.35f ? -amplitude : 0f);
        }

        // OOP PILLAR 3: POLYMORPHISM
        // The renderer and GameManager hold Enemy references, yet each call
        // reaches the correct subclass body and speed override. Walker, Runner,
        // Tank and Special therefore share one contract with different results.
        public void Draw(Graphics g, float time)
        {
            if (_state == EnemyState.Dead) return;

            float spawn = SpawnProgress;
            float alpha = _state == EnemyState.Spawning ? spawn : 1f;
            if (_state == EnemyState.Dying)
            {
                alpha *= Math.Max(0f, 1f - DeathProgress);
            }
            if (alpha <= 0.01f) return;

            float scale = BodyScale * (0.88f + 0.12f * spawn);
            if (_state == EnemyState.Dying)
            {
                scale *= 1f - DeathProgress * 0.18f;
            }

            GraphicsState state = g.Save();
            g.TranslateTransform((float)Math.Round(VisualX), (float)Math.Round(Y));
            g.ScaleTransform(scale, scale);

            // Blocky ground shadow instead of a smooth oval.
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb((int)(95 * alpha), 8, 11, 18)),
                -30f, 32f, 60f, 8f);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb((int)(55 * alpha), 0, 0, 0)),
                -22f, 38f, 44f, 4f);

            DrawBody(g, time, alpha);

            if (_hitFlash > 0.01f)
            {
                int flashAlpha = (int)(_hitFlash * 150f);
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(flashAlpha, 255, 255, 255)),
                    -30f, -34f, 60f, 68f);
            }

            if (_state == EnemyState.Spawning)
            {
                for (int i = 0; i < 4; i++)
                {
                    float y = -28f + i * 18f;
                    g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(AccentColor, (int)(120 * (1f - spawn)))),
                        -28f, y, 56f, 2f);
                }
            }

            g.Restore(state);
        }

        /// <summary>Draws the subclass body in local coordinates.</summary>
        protected abstract void DrawBody(Graphics g, float time, float alpha);

        protected Color Fade(Color colour, float alpha)
        {
            return Palette.Alpha(colour, (int)(Math.Max(0f, Math.Min(1f, alpha)) * 255f));
        }

        protected static int AnimFrame(float time, float speed, int phase, int frames)
        {
            if (frames < 1) frames = 1;
            int value = (int)(time * speed + phase);
            if (value < 0) value = -value;
            return value % frames;
        }

        protected static RectangleF Px(float x, float y, float width, float height)
        {
            return new RectangleF((float)Math.Round(x), (float)Math.Round(y),
                (float)Math.Round(width), (float)Math.Round(height));
        }
    }
}
