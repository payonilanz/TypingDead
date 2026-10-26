using System;
using System.Drawing;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Effects
{
    /// <summary>
    /// Blocky energy build-up around the survivor's flare launcher. Progress is
    /// driven by correct typed characters only; it never attacks by itself.
    /// </summary>
    public sealed class ChargeEffect
    {
        private float _time;
        private PointF _origin;
        private float _progress;

        public float Progress
        {
            get { return _progress; }
        }

        public PointF Origin
        {
            get { return _origin; }
        }

        public void Reset()
        {
            _progress = 0f;
        }

        public void Update(float deltaTime, PointF origin, float progress)
        {
            _time += deltaTime;
            _origin = origin;
            _progress = Math.Max(0f, Math.Min(1f, progress));
        }

        public void Draw(Graphics g)
        {
            if (_progress <= 0.01f) return;

            int frame = ((int)(_time * 10f)) % 2;
            float radius = 10f + 24f * _progress + frame * 2f;
            Color colour = _progress > 0.72f
                ? Palette.Blend(Palette.Success, Palette.Warning, (_progress - 0.72f) / 0.28f)
                : Palette.Success;

            int points = 8 + (int)(_progress * 6f);
            for (int i = 0; i < points; i++)
            {
                double angle = _time * (0.8 + i * 0.04) + i * (Math.PI * 2.0 / points);
                float orbit = radius + (i % 3) * 4f;
                float size = 2f + (i % 2) * 2f;
                float x = _origin.X + (float)Math.Cos(angle) * orbit;
                float y = _origin.Y + (float)Math.Sin(angle) * orbit;
                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(colour, 150)),
                    (float)Math.Round(x - size * 0.5f), (float)Math.Round(y - size * 0.5f), size, size);
            }

            float core = 3f + 7f * _progress + frame;
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(colour, 45)),
                (float)Math.Round(_origin.X - core * 2f), (float)Math.Round(_origin.Y - core * 2f),
                (float)Math.Round(core * 4f), (float)Math.Round(core * 4f));
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(colour, 170)),
                (float)Math.Round(_origin.X - core), (float)Math.Round(_origin.Y - core),
                (float)Math.Round(core * 2f), (float)Math.Round(core * 2f));
            g.FillRectangle(DrawingHelper.Brush(Palette.PrimaryText),
                (float)Math.Round(_origin.X - core * 0.35f), (float)Math.Round(_origin.Y - core * 0.35f),
                (float)Math.Round(core * 0.7f), (float)Math.Round(core * 0.7f));

            if (_progress > 0.78f)
            {
                // Four corner ticks announce that the word is ready.
                float tick = radius + 8f;
                g.FillRectangle(DrawingHelper.Brush(Palette.Warning),
                    _origin.X - tick, _origin.Y - 1f, 7f, 2f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Warning),
                    _origin.X + tick - 7f, _origin.Y - 1f, 7f, 2f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Warning),
                    _origin.X - 1f, _origin.Y - tick, 2f, 7f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Warning),
                    _origin.X - 1f, _origin.Y + tick - 7f, 2f, 7f);
            }
        }
    }
}
