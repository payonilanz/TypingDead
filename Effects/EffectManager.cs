using System;
using System.Collections.Generic;
using System.Drawing;
using TypingDead.Entities;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Effects
{
    /// <summary>
    /// Owns every transient visual. Gameplay code emits clear events (burst,
    /// trail, damage) and the manager handles lifetimes, quality scaling,
    /// screen shake and rendering order.
    /// </summary>
    public sealed class EffectManager
    {
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<VisualEffect> _worldEffects = new List<VisualEffect>();
        private readonly List<VisualEffect> _screenEffects = new List<VisualEffect>();
        private readonly Random _random = new Random(90210);
        private GameSettings _settings = GameSettings.CreateDefault();

        private float _chargeEmitTimer;
        private float _lastCharge = -1f;

        private float _shakeRemaining;
        private float _shakeDuration;
        private float _shakeAmplitude;
        private float _shakeClock;

        private float _flashRemaining;
        private float _flashDuration;
        private float _flashStrength;
        private Color _flashColour;

        public ChargeEffect Charge { get; private set; }

        public int ParticleCount
        {
            get { return _particles.Count; }
        }

        public EffectManager()
        {
            Charge = new ChargeEffect();
        }

        public void Reset()
        {
            _particles.Clear();
            _worldEffects.Clear();
            _screenEffects.Clear();
            Charge.Reset();
            _chargeEmitTimer = 0f;
            _lastCharge = -1f;
            _shakeRemaining = 0f;
            _flashRemaining = 0f;
        }

        public void ApplySettings(GameSettings settings)
        {
            if (settings != null)
            {
                _settings = settings;
            }
        }

        public void Update(float deltaTime, GameSettings settings)
        {
            ApplySettings(settings);
            if (deltaTime <= 0f) return;

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle particle = _particles[i];
                particle.Update(deltaTime);
                if (!particle.IsAlive)
                {
                    _particles.RemoveAt(i);
                }
            }

            for (int i = _worldEffects.Count - 1; i >= 0; i--)
            {
                VisualEffect effect = _worldEffects[i];
                effect.Update(deltaTime);
                if (effect.IsFinished) _worldEffects.RemoveAt(i);
            }

            for (int i = _screenEffects.Count - 1; i >= 0; i--)
            {
                VisualEffect effect = _screenEffects[i];
                effect.Update(deltaTime);
                if (effect.IsFinished) _screenEffects.RemoveAt(i);
            }

            if (_shakeRemaining > 0f)
            {
                _shakeRemaining = Math.Max(0f, _shakeRemaining - deltaTime);
                _shakeClock += deltaTime * 58f;
            }

            if (_flashRemaining > 0f)
            {
                _flashRemaining = Math.Max(0f, _flashRemaining - deltaTime);
            }
        }

        private int MaxParticles()
        {
            switch (_settings.EffectsQuality)
            {
                case EffectsQuality.Low: return _settings.ReducedMotion ? 110 : 190;
                case EffectsQuality.Medium: return _settings.ReducedMotion ? 190 : 430;
                default: return _settings.ReducedMotion ? 260 : 700;
            }
        }

        private void AddParticle(Particle particle)
        {
            if (_particles.Count >= MaxParticles()) return;
            _particles.Add(particle);
        }

        // ------------------------------------------------------------------
        // Charge / core / trail
        // ------------------------------------------------------------------

        public void SetCharge(PointF origin, float progress, float deltaTime)
        {
            progress = Math.Max(0f, Math.Min(1f, progress));
            Charge.Update(deltaTime, origin, progress);

            if (progress <= 0.001f)
            {
                _lastCharge = 0f;
                return;
            }

            _chargeEmitTimer -= deltaTime;
            bool milestone = _lastCharge < 0.5f && progress >= 0.5f;
            if (_chargeEmitTimer <= 0f || milestone)
            {
                _chargeEmitTimer = 0.11f - progress * 0.055f;
                float quality = _settings.ParticleMultiplier();
                int count = Math.Max(1, (int)((1f + progress * 3f) * quality));
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                    float radius = 13f + (float)_random.NextDouble() * (18f + progress * 12f);
                    PointF position = new PointF(
                        origin.X + (float)Math.Cos(angle) * radius,
                        origin.Y + (float)Math.Sin(angle) * radius);
                    PointF velocity = new PointF(
                        -(float)Math.Cos(angle) * (16f + progress * 35f) + Jitter(8f),
                        -(float)Math.Sin(angle) * (16f + progress * 35f) + Jitter(8f));
                    Color colour = progress > 0.65f
                        ? Palette.Blend(Palette.NeonCyan, Palette.ElectricPurple, progress - 0.4f)
                        : Palette.NeonCyan;
                    Particle p = new Particle(position, velocity, colour,
                        1.2f + (float)_random.NextDouble() * 2.2f, 0.24f + (float)_random.NextDouble() * 0.25f,
                        _random.NextDouble() > 0.78 ? ParticleKind.Streak : ParticleKind.Circle);
                    p.StreakLength = 5f + progress * 12f;
                    AddParticle(p);
                }
            }
            _lastCharge = progress;
        }

        public void SpawnMuzzle(PointF origin, float power)
        {
            for (int i = 0; i < scaled(12f * power); i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float speed = 50f + (float)_random.NextDouble() * 190f;
                PointF velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                Particle p = new Particle(origin, velocity,
                    _random.NextDouble() > 0.55 ? Palette.NeonCyan : Palette.ElectricPurple,
                    1.5f + (float)_random.NextDouble() * 2f,
                    0.20f + (float)_random.NextDouble() * 0.25f,
                    ParticleKind.Streak);
                p.StreakLength = 8f + (float)_random.NextDouble() * 9f;
                p.Drag = 0.93f;
                AddParticle(p);
            }
            _worldEffects.Add(new RingEffect(origin, Palette.NeonCyan, 4f, 34f + power * 18f, 0.26f, 2f));
        }

        public void SpawnTrail(PointF position, PointF direction, Color colour, float strength)
        {
            int count = scaled(1.2f + strength * 1.8f);
            for (int i = 0; i < count; i++)
            {
                PointF jitter = new PointF(Jitter(7f), Jitter(7f));
                PointF velocity = new PointF(
                    -direction.X * (40f + (float)_random.NextDouble() * 100f) + jitter.X,
                    -direction.Y * (40f + (float)_random.NextDouble() * 100f) + jitter.Y);
                Particle p = new Particle(new PointF(position.X + Jitter(4f), position.Y + Jitter(4f)),
                    velocity,
                    _random.NextDouble() > 0.28 ? colour : Color.White,
                    1.3f + (float)_random.NextDouble() * 2.2f,
                    0.18f + (float)_random.NextDouble() * 0.24f,
                    _random.NextDouble() > 0.35 ? ParticleKind.Streak : ParticleKind.Circle);
                p.StreakLength = 10f + strength * 12f;
                p.Drag = 0.94f;
                AddParticle(p);
            }
        }

        // ------------------------------------------------------------------
        // Impacts and reactions
        // ------------------------------------------------------------------

        public void SpawnBurst(PointF position, Color accent, float scale, bool perfect)
        {
            float quality = _settings.ParticleMultiplier();
            int count = scaled((perfect ? 30f : 20f) * scale);
            Color[] colours = new Color[]
            {
                Palette.NeonCyan,
                Palette.ElectricPurple,
                Color.White,
                accent
            };

            for (int i = 0; i < count; i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float speed = (55f + (float)_random.NextDouble() * 250f) * scale;
                PointF velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                Color colour = colours[_random.Next(colours.Length)];
                ParticleKind kind = _random.NextDouble() > 0.55 ? ParticleKind.Streak : ParticleKind.Circle;
                Particle p = new Particle(position, velocity, colour,
                    1.2f + (float)_random.NextDouble() * 3.2f,
                    0.25f + (float)_random.NextDouble() * 0.42f, kind);
                p.Drag = 0.92f;
                p.Gravity = 45f;
                p.StreakLength = 6f + (float)_random.NextDouble() * 14f;
                AddParticle(p);
            }

            _worldEffects.Add(new RingEffect(position, perfect ? Palette.ToxicGreen : Palette.NeonCyan,
                5f, (36f + 34f * scale), 0.34f, perfect ? 2.8f : 2.2f));
            if (perfect)
            {
                _worldEffects.Add(new RingEffect(position, Palette.ElectricPurple, 8f, 62f * scale, 0.45f, 1.5f));
            }
            Flash(Palette.NeonCyan, 0.09f * scale, 0.10f);
        }

        public void SpawnReaction(PointF position, Color accent)
        {
            for (int i = 0; i < scaled(7f); i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float speed = 35f + (float)_random.NextDouble() * 120f;
                Particle p = new Particle(position,
                    new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    _random.NextDouble() > 0.5 ? Color.White : accent,
                    1f + (float)_random.NextDouble() * 1.7f,
                    0.15f + (float)_random.NextDouble() * 0.2f, ParticleKind.Streak);
                p.StreakLength = 5f + (float)_random.NextDouble() * 8f;
                AddParticle(p);
            }
        }

        public void SpawnAftermath(PointF position, Color accent, EnemyType type, bool large)
        {
            float quality = _settings.ParticleMultiplier();
            int count = scaled((large ? 40f : 26f) * quality);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float speed = (25f + (float)_random.NextDouble() * (large ? 240f : 160f));
                PointF velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed - 25f);
                ParticleKind kind;
                double roll = _random.NextDouble();
                if (roll < 0.32) kind = ParticleKind.Square;
                else if (roll < 0.7) kind = ParticleKind.Streak;
                else kind = ParticleKind.Circle;
                Particle p = new Particle(position, velocity,
                    _random.NextDouble() > 0.5 ? accent : Palette.Alpha(accent, 210),
                    1.2f + (float)_random.NextDouble() * (large ? 4.2f : 2.8f),
                    0.38f + (float)_random.NextDouble() * 0.45f, kind);
                p.Gravity = 36f;
                p.Drag = 0.95f;
                p.Spin = Jitter(90f);
                p.StreakLength = 7f + (float)_random.NextDouble() * 12f;
                AddParticle(p);
            }

            _worldEffects.Add(new RingEffect(position, accent, 8f, large ? 90f : 58f, large ? 0.62f : 0.44f, 2.5f));
        }

        public void SpawnSpawnEffect(PointF position, Color accent)
        {
            for (int i = 0; i < scaled(8f); i++)
            {
                Particle p = new Particle(
                    new PointF(position.X + Jitter(22f), position.Y + Jitter(30f)),
                    new PointF(Jitter(40f), Jitter(40f)),
                    accent, 1f + (float)_random.NextDouble() * 2f,
                    0.25f + (float)_random.NextDouble() * 0.30f,
                    _random.NextDouble() > 0.6 ? ParticleKind.Square : ParticleKind.Circle);
                p.Drag = 0.92f;
                AddParticle(p);
            }
            _worldEffects.Add(new RingEffect(position, accent, 4f, 45f, 0.38f, 1.4f));
        }

        // ------------------------------------------------------------------
        // Damage, level transition, score text
        // ------------------------------------------------------------------

        public void PlayerDamaged(PointF playerPosition, bool enemyContact)
        {
            AddScreenEffect(new DamageEffect(enemyContact ? 1.3f : 0.9f));
            if (_settings.ShakeAllowed())
            {
                Shake(enemyContact ? 9f : 5f, 0.35f);
            }

            int sparks = scaled(18f);
            for (int i = 0; i < sparks; i++)
            {
                float angle = (float)(-Math.PI * 0.8 + _random.NextDouble() * Math.PI * 1.6);
                float speed = 40f + (float)_random.NextDouble() * 150f;
                Particle p = new Particle(playerPosition,
                    new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    _random.NextDouble() > 0.4 ? Palette.DangerRed : Color.White,
                    1.5f + (float)_random.NextDouble() * 2.4f,
                    0.28f + (float)_random.NextDouble() * 0.35f, ParticleKind.Streak);
                p.StreakLength = 8f + (float)_random.NextDouble() * 12f;
                AddParticle(p);
            }
        }

        public void WrongInput(PointF playerPosition)
        {
            AddScreenEffect(new DamageEffect(0.55f));
            if (_settings.ShakeAllowed())
            {
                Shake(3.5f, 0.20f);
            }
            for (int i = 0; i < scaled(10f); i++)
            {
                Particle p = new Particle(
                    new PointF(playerPosition.X + Jitter(24f), playerPosition.Y + Jitter(20f)),
                    new PointF(Jitter(110f), Jitter(80f)),
                    Palette.DangerRed, 1.2f + (float)_random.NextDouble() * 2f,
                    0.18f + (float)_random.NextDouble() * 0.25f, ParticleKind.Streak);
                p.StreakLength = 7f;
                AddParticle(p);
            }
        }

        public void SpawnLevelUp()
        {
            PointF center = new PointF(GameConfig.VirtualWidth * 0.5f, GameConfig.VirtualHeight * 0.42f);
            for (int i = 0; i < scaled(40f); i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float speed = 60f + (float)_random.NextDouble() * 300f;
                Particle p = new Particle(center,
                    new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    _random.NextDouble() > 0.5 ? Palette.NeonCyan : Palette.ElectricPurple,
                    1.2f + (float)_random.NextDouble() * 3f,
                    0.55f + (float)_random.NextDouble() * 0.65f,
                    _random.NextDouble() > 0.4 ? ParticleKind.Streak : ParticleKind.Spark);
                p.Drag = 0.94f;
                p.StreakLength = 12f;
                AddParticle(p);
            }
            _worldEffects.Add(new RingEffect(center, Palette.ElectricPurple, 20f, 280f, 0.8f, 3f));
            Flash(Palette.ElectricPurple, 0.22f, 0.45f);
        }

        public void AddScorePopup(string text, PointF position, Color colour)
        {
            _worldEffects.Add(new ScorePopup(text, position, colour));
        }

        public void Flash(Color colour, float strength, float duration)
        {
            _flashColour = colour;
            _flashStrength = Math.Max(0f, Math.Min(1f, strength));
            _flashDuration = Math.Max(0.05f, duration);
            _flashRemaining = _flashDuration;
        }

        public void Shake(float amplitude, float duration)
        {
            if (!_settings.ShakeAllowed()) return;
            _shakeAmplitude = Math.Max(_shakeAmplitude, amplitude);
            _shakeDuration = Math.Max(0.01f, duration);
            _shakeRemaining = Math.Max(_shakeRemaining, duration);
            _shakeClock = 0f;
        }

        private void AddScreenEffect(VisualEffect effect)
        {
            _screenEffects.Add(effect);
        }

        private int scaled(float count)
        {
            int value = (int)Math.Round(count * _settings.ParticleMultiplier());
            return Math.Max(1, value);
        }

        private float Jitter(float amount)
        {
            return (float)(_random.NextDouble() * 2.0 - 1.0) * amount;
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        public PointF GetShakeOffset()
        {
            if (_shakeRemaining <= 0f || !_settings.ShakeAllowed()) return PointF.Empty;
            float ratio = _shakeRemaining / _shakeDuration;
            float amplitude = _shakeAmplitude * ratio;
            return new PointF(
                (float)Math.Sin(_shakeClock * 1.7) * amplitude,
                (float)Math.Cos(_shakeClock * 2.3) * amplitude * 0.62f);
        }

        public void DrawCharge(Graphics g)
        {
            Charge.Draw(g);
        }

        public void DrawParticles(Graphics g)
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                _particles[i].Draw(g);
            }
        }

        public void DrawWorldEffects(Graphics g)
        {
            for (int i = 0; i < _worldEffects.Count; i++)
            {
                _worldEffects[i].Draw(g);
            }
        }

        public void DrawScreenEffects(Graphics g)
        {
            for (int i = 0; i < _screenEffects.Count; i++)
            {
                _screenEffects[i].Draw(g);
            }

            if (_flashRemaining > 0f && _flashDuration > 0f)
            {
                float ratio = _flashRemaining / _flashDuration;
                int alpha = (int)(_flashStrength * 255f * ratio * ratio);
                if (alpha > 0)
                {
                    g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(_flashColour, alpha)),
                        0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
                }
            }
        }
    }
}
