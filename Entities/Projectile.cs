using System;
using System.Drawing;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    /// <summary>
    /// A pixel flare released only after a correct Enter submission. The
    /// projectile remembers which word launched it so a delayed impact cannot
    /// damage a Tank Zombie's next word or award a second kill.
    /// </summary>
    public sealed class Projectile
    {
        private readonly Enemy _target;
        private readonly float _speed;
        private float _trailCooldown;
        private float _age;

        public PointF Position { get; private set; }
        public PointF Direction { get; private set; }
        public Enemy Target { get { return _target; } }
        public bool HasArrived { get; private set; }
        public bool Perfect { get; private set; }
        public float Strength { get; private set; }
        public bool ShouldEmitTrail { get; private set; }
        public int LaunchedWordIndex { get; private set; }
        public string LaunchedWord { get; private set; }

        public Projectile(PointF origin, Enemy target, bool perfect, float strength)
        {
            Position = origin;
            _target = target;
            Perfect = perfect;
            Strength = Math.Max(0.75f, Math.Min(1.6f, strength));
            _speed = 1680f + (perfect ? 260f : 0f);
            Direction = new PointF(1f, 0f);
            _trailCooldown = 0f;
            LaunchedWordIndex = target == null ? -1 : target.WordIndex;
            LaunchedWord = target == null ? String.Empty : target.Word;
        }

        public void Update(float deltaTime)
        {
            if (HasArrived) return;
            _age += deltaTime;
            ShouldEmitTrail = false;

            if (_target == null || _age > 1.2f)
            {
                HasArrived = true;
                return;
            }

            PointF targetPoint = new PointF(_target.VisualX - 4f, _target.Y - 8f);
            float dx = targetPoint.X - Position.X;
            float dy = targetPoint.Y - Position.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance <= 1f)
            {
                HasArrived = true;
                Direction = new PointF(1f, 0f);
                return;
            }

            Direction = new PointF(dx / distance, dy / distance);
            float step = _speed * deltaTime;
            if (distance <= step + 18f)
            {
                Position = targetPoint;
                HasArrived = true;
                return;
            }

            Position = new PointF(Position.X + Direction.X * step, Position.Y + Direction.Y * step);

            _trailCooldown -= deltaTime;
            if (_trailCooldown <= 0f)
            {
                _trailCooldown = 0.018f;
                ShouldEmitTrail = true;
            }
        }

        public void Draw(Graphics g, float time)
        {
            if (HasArrived) return;

            float pulse = 0.82f + 0.18f * (float)Math.Sin(time * 22f);
            float size = (8f + 4f * Strength) * pulse;
            Color core = Perfect ? Palette.Warning : Palette.Success;

            // Square afterimages instead of a smooth glow.
            for (int i = 1; i <= 4; i++)
            {
                float offset = i * 8f;
                int alpha = 90 - i * 18;
                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(core, alpha)),
                    (float)Math.Round(Position.X - Direction.X * offset - size * 0.35f),
                    (float)Math.Round(Position.Y - Direction.Y * offset - size * 0.35f),
                    (float)Math.Round(size * 0.7f), (float)Math.Round(size * 0.7f));
            }

            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(core, 90)),
                (float)Math.Round(Position.X - size), (float)Math.Round(Position.Y - size),
                (float)Math.Round(size * 2f), (float)Math.Round(size * 2f));
            g.FillRectangle(DrawingHelper.Brush(core),
                (float)Math.Round(Position.X - size * 0.62f), (float)Math.Round(Position.Y - size * 0.62f),
                (float)Math.Round(size * 1.24f), (float)Math.Round(size * 1.24f));
            g.FillRectangle(DrawingHelper.Brush(Palette.PrimaryText),
                (float)Math.Round(Position.X - 3f), (float)Math.Round(Position.Y - 3f), 6f, 6f);
        }
    }
}
