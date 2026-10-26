using System;
using System.Drawing;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Effects
{
    /// <summary>Expanding energy ring used by impacts, deaths and level-ups.</summary>
    public sealed class RingEffect : VisualEffect
    {
        private readonly PointF _center;
        private readonly Color _colour;
        private readonly float _startRadius;
        private readonly float _endRadius;
        private readonly float _lineWidth;

        public RingEffect(PointF center, Color colour, float startRadius, float endRadius,
            float duration, float lineWidth)
            : base(duration)
        {
            _center = center;
            _colour = colour;
            _startRadius = startRadius;
            _endRadius = endRadius;
            _lineWidth = lineWidth;
        }

        public override void Draw(Graphics g)
        {
            float ratio = Math.Min(1f, Age / Duration);
            float ease = 1f - (float)Math.Pow(1f - ratio, 3.0);
            float radius = _startRadius + (_endRadius - _startRadius) * ease;
            int alpha = (int)(230f * (1f - ratio));
            if (alpha <= 0) return;

            // A stepped square ring reads as a pixel explosion instead of a
            // smooth science-fiction energy circle.
            float snapped = (float)Math.Round(radius / 4f) * 4f;
            float size = snapped * 2f;
            float x = (float)Math.Round(_center.X - snapped);
            float y = (float)Math.Round(_center.Y - snapped);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(_colour, alpha), _lineWidth), x, y, size, size);
            if (snapped > 16f)
            {
                float inner = (float)Math.Round(snapped * 0.72f);
                g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(_colour, alpha / 3), 1f),
                    (float)Math.Round(_center.X - inner), (float)Math.Round(_center.Y - inner),
                    inner * 2f, inner * 2f);
            }
        }
    }

    /// <summary>Floating combat text, primarily used for +10 scores.</summary>
    public sealed class ScorePopup : VisualEffect
    {
        private readonly PointF _origin;
        private readonly string _text;
        private readonly Color _colour;
        private readonly Font _font;

        public ScorePopup(string text, PointF origin, Color colour)
            : base(0.90f)
        {
            _text = text;
            _origin = origin;
            _colour = colour;
            _font = UiFonts.Mono(20f, FontStyle.Bold);
        }

        public override void Draw(Graphics g)
        {
            float ratio = Math.Min(1f, Age / Duration);
            float y = _origin.Y - ratio * 46f;
            int alpha = (int)(255f * (1f - ratio * ratio));
            RectangleF rect = new RectangleF(_origin.X - 80f, y - 16f, 160f, 32f);
            DrawingHelper.DrawGlowText(g, _text, _font, Palette.Alpha(_colour, alpha), rect,
                StringAlignment.Center, StringAlignment.Center, 1.2f);
        }
    }

    /// <summary>
    /// Red flashing border and vignette used when the player loses a life.
    /// The effect is brief and deliberately keeps the centre readable.
    /// </summary>
    public sealed class DamageEffect : VisualEffect
    {
        private readonly float _intensity;

        public DamageEffect(float intensity)
            : base(0.58f)
        {
            _intensity = Math.Max(0.2f, Math.Min(2f, intensity));
        }

        public override void Draw(Graphics g)
        {
            float ratio = Math.Min(1f, Age / Duration);
            float fade = 1f - ratio;
            int flash = (int)(fade * 30f * _intensity);
            if (flash > 0)
            {
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(flash, Palette.DangerRed)),
                    0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            }

            int borderAlpha = (int)(fade * 220f * _intensity);
            for (int i = 0; i < 5; i++)
            {
                float inset = i * 5f;
                int alpha = Math.Max(0, borderAlpha - i * 32);
                g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.DangerRed, alpha), 3f - i * 0.4f),
                    inset, inset, GameConfig.VirtualWidth - inset * 2f, GameConfig.VirtualHeight - inset * 2f);
            }

            int sideAlpha = (int)(fade * 75f * _intensity);
            for (int i = 0; i < 16; i++)
            {
                int a = Math.Max(0, sideAlpha - i * 4);
                g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.DangerRed, a / 3), 1f),
                    12f + i * 18f, 12f, 1f, GameConfig.VirtualHeight - 24f);
                g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.DangerRed, a / 3), 1f),
                    GameConfig.VirtualWidth - 12f - i * 18f, 12f, 1f, GameConfig.VirtualHeight - 24f);
            }
        }
    }
}
