using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.UI;

namespace TypingDead.UI
{
    /// <summary>
    /// Base panel for all full-screen custom-painted screens. It provides one
    /// animation timer, double buffering and a fixed 1280x720 design canvas
    /// that scales with letterboxing as the window is resized.
    /// </summary>
    public abstract class VirtualScreenPanel : Panel
    {
        private readonly Timer _timer;
        private readonly Stopwatch _clock;
        private float _time;
        private float _lastSeconds;

        protected float AnimationTime
        {
            get { return _time; }
        }

        protected VirtualScreenPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Palette.VoidBlack;
            DoubleBuffered = true;
            TabStop = false;

            _clock = new Stopwatch();
            _timer = new Timer();
            _timer.Interval = 16;
            _timer.Tick += TimerTick;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                _time = 0f;
                _lastSeconds = 0f;
                _clock.Reset();
                _clock.Start();
                _timer.Start();
                Invalidate();
            }
            else
            {
                _timer.Stop();
                _clock.Stop();
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (!Visible) return;
            float seconds = (float)_clock.Elapsed.TotalSeconds;
            float delta = seconds - _lastSeconds;
            _lastSeconds = seconds;
            if (delta < 0f) delta = 0f;
            if (delta > 0.05f) delta = 0.05f;
            _time += delta;
            OnFrame(delta);
            Invalidate();
        }

        /// <summary>Called once per animation frame, before painting.</summary>
        protected virtual void OnFrame(float deltaTime)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Palette.NearBlack);

            // Pixel art must stay sharp. Smooth shapes and bilinear scaling
            // would blur every blocky sprite and border.
            g.SmoothingMode = SmoothingMode.None;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            float scale = Math.Min((float)ClientSize.Width / GameConfig.VirtualWidth,
                                   (float)ClientSize.Height / GameConfig.VirtualHeight);
            if (scale <= 0f) return;
            float offsetX = (ClientSize.Width - GameConfig.VirtualWidth * scale) * 0.5f;
            float offsetY = (ClientSize.Height - GameConfig.VirtualHeight * scale) * 0.5f;

            GraphicsState state = g.Save();
            g.TranslateTransform(offsetX, offsetY);
            g.ScaleTransform(scale, scale);
            g.SetClip(new RectangleF(0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight));
            Render(g, _time);
            g.Restore(state);
        }

        protected abstract void Render(Graphics g, float time);

        protected PointF ToVirtual(Point point)
        {
            float scale = Math.Min((float)ClientSize.Width / GameConfig.VirtualWidth,
                                   (float)ClientSize.Height / GameConfig.VirtualHeight);
            if (scale <= 0f) return PointF.Empty;
            float offsetX = (ClientSize.Width - GameConfig.VirtualWidth * scale) * 0.5f;
            float offsetY = (ClientSize.Height - GameConfig.VirtualHeight * scale) * 0.5f;
            return new PointF((point.X - offsetX) / scale, (point.Y - offsetY) / scale);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _clock.Stop();
            }
            base.Dispose(disposing);
        }
    }
}
