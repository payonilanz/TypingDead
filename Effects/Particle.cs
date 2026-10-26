using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using TypingDead.Rendering;

namespace TypingDead.Effects
{
    public enum ParticleKind
    {
        Circle,
        Streak,
        Square,
        Spark
    }

    /// <summary>
    /// Lightweight visual particle. Particles are pooled by the EffectManager
    /// and expire automatically, so combat effects cannot accumulate forever.
    /// </summary>
    public sealed class Particle
    {
        public PointF Position;
        public PointF Velocity;
        public Color Colour;
        public float Size;
        public float Life;
        public float MaxLife;
        public float Drag;
        public float Gravity;
        public float Rotation;
        public float Spin;
        public float StreakLength;
        public ParticleKind Kind;

        public bool IsAlive
        {
            get { return Life > 0f; }
        }

        public float LifeRatio
        {
            get { return MaxLife <= 0f ? 0f : Math.Max(0f, Math.Min(1f, Life / MaxLife)); }
        }

        public Particle(PointF position, PointF velocity, Color colour, float size, float life, ParticleKind kind)
        {
            Position = position;
            Velocity = velocity;
            Colour = colour;
            Size = size;
            Life = life;
            MaxLife = life;
            Drag = 0.99f;
            Gravity = 0f;
            Kind = kind;
            Rotation = 0f;
            Spin = 0f;
            StreakLength = 10f;
        }

        public void Update(float deltaTime)
        {
            if (Life <= 0f) return;
            Life -= deltaTime;
            Position = new PointF(
                Position.X + Velocity.X * deltaTime,
                Position.Y + Velocity.Y * deltaTime);
            Velocity = new PointF(
                Velocity.X * (float)Math.Pow(Drag, deltaTime * 60f),
                Velocity.Y * (float)Math.Pow(Drag, deltaTime * 60f) + Gravity * deltaTime);
            Rotation += Spin * deltaTime;
        }

        public void Draw(Graphics g)
        {
            if (Life <= 0f) return;
            float ratio = LifeRatio;
            float size = Math.Max(0.7f, Size * (0.35f + 0.65f * ratio));
            int alpha = (int)(255f * Math.Min(1f, ratio * 1.6f));
            Color colour = Color.FromArgb((alpha / 16) * 16, Colour.R, Colour.G, Colour.B);

            switch (Kind)
            {
                case ParticleKind.Streak:
                {
                    float length = Math.Max(2f, StreakLength * ratio);
                    float magnitude = (float)Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
                    if (magnitude < 0.001f) magnitude = 1f;
                    int steps = Math.Max(1, (int)(length / 4f));
                    for (int i = 0; i < steps; i++)
                    {
                        float px = Position.X - (Velocity.X / magnitude) * (i * 4f);
                        float py = Position.Y - (Velocity.Y / magnitude) * (i * 4f);
                        g.FillRectangle(DrawingHelper.Brush(i == 0 ? Color.FromArgb(alpha, 255, 255, 255) : colour),
                            (float)Math.Round(px), (float)Math.Round(py),
                            (float)Math.Max(1f, Math.Round(size * 0.7f)),
                            (float)Math.Max(1f, Math.Round(size * 0.7f)));
                    }
                    break;
                }
                case ParticleKind.Square:
                {
                    float half = size * 0.5f;
                    GraphicsState state = g.Save();
                    g.TranslateTransform(Position.X, Position.Y);
                    g.RotateTransform(Rotation);
                    g.FillRectangle(DrawingHelper.Brush(colour), -half, -half, size, size);
                    g.Restore(state);
                    break;
                }
                case ParticleKind.Spark:
                {
                    g.DrawLine(DrawingHelper.Pen(colour, Math.Max(1f, size * 0.45f)),
                        Position.X - size, Position.Y, Position.X + size, Position.Y);
                    g.DrawLine(DrawingHelper.Pen(colour, Math.Max(1f, size * 0.45f)),
                        Position.X, Position.Y - size, Position.X, Position.Y + size);
                    break;
                }
                default:
                {
                    g.FillRectangle(DrawingHelper.Brush(colour),
                        (float)Math.Round(Position.X - size * 0.5f),
                        (float)Math.Round(Position.Y - size * 0.5f),
                        (float)Math.Max(1f, Math.Round(size)),
                        (float)Math.Max(1f, Math.Round(size)));
                    break;
                }
            }
        }
    }
}
