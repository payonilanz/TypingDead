using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.Systems;
using TypingDead.UI;

namespace TypingDead.Forms
{
    public enum ResultKind
    {
        ModeComplete,
        GameOver,
        FinalVictory
    }

    /// <summary>
    /// End screen for a mode completion, a game over, or the final victory.
    /// The three variants use different titles, accents and primary actions, but
    /// share one consistent report layout.
    /// </summary>
    public sealed class ResultPanel : VirtualScreenPanel
    {
        private readonly AudioManager _audio;
        private readonly NeonAction _primary;
        private readonly NeonAction _menu;
        private readonly NeonAction[] _actions;
        private int _focus;
        private int _hover = -1;
        private GameResult _result;
        private Bitmap _snapshot;
        private ResultKind _kind;
        private bool _newBest;
        private string _notice;

        public event EventHandler PrimaryRequested;
        public event EventHandler MainMenuRequested;

        public ResultKind Kind
        {
            get { return _kind; }
        }

        public ResultPanel(AudioManager audio)
        {
            _audio = audio;
            _primary = new NeonAction("CONTINUE", new RectangleF(374f, 612f, 248f, 50f));
            _primary.Primary = true;
            _primary.Hint = "ENTER";
            _menu = new NeonAction("MAIN MENU", new RectangleF(658f, 612f, 248f, 50f));
            _menu.Accent = Palette.SecondaryText;
            _menu.Hint = "ESC";
            _actions = new NeonAction[] { _primary, _menu };
            Cursor = Cursors.Default;
        }

        public void ShowResult(GameResult result, Bitmap snapshot, ResultKind kind,
            bool newBest, string notice)
        {
            _result = result;
            _kind = kind;
            _newBest = newBest;
            _notice = notice ?? String.Empty;
            _focus = 0;
            _hover = -1;

            if (_snapshot != null)
            {
                _snapshot.Dispose();
                _snapshot = null;
            }
            _snapshot = snapshot;

            if (kind == ResultKind.ModeComplete)
            {
                _primary.Text = "CONTINUE";
                _primary.Accent = Palette.Success;
            }
            else if (kind == ResultKind.GameOver)
            {
                _primary.Text = "RETRY";
                _primary.Accent = Palette.Blood;
            }
            else
            {
                _primary.Text = "PLAY AGAIN";
                _primary.Accent = Palette.Warning;
            }

            Cursor = Cursors.Default;
            Focus();
            Invalidate();
        }

        protected override void Render(Graphics g, float time)
        {
            if (_snapshot != null)
            {
                g.DrawImage(_snapshot, new RectangleF(0f, 0f,
                    GameConfig.VirtualWidth, GameConfig.VirtualHeight));
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(178, 8, 10, 15)),
                    0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            }
            else
            {
                PixelBackground.Draw(g, time, 1f);
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(186, 8, 10, 15)),
                    0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            }

            Color accent = KindColour();
            RectangleF panel = new RectangleF(250f, 60f, 780f, 532f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow),
                panel.X + 9f, panel.Y + 9f, panel.Width, panel.Height);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(244, 22, 26, 35)),
                panel.X, panel.Y, panel.Width, panel.Height);
            g.DrawRectangle(DrawingHelper.Pen(accent, 3f),
                panel.X, panel.Y, panel.Width, panel.Height);
            g.FillRectangle(DrawingHelper.Brush(accent),
                panel.X, panel.Y, panel.Width, 7f);

            // Small pixel corner decorations.
            g.FillRectangle(DrawingHelper.Brush(accent), panel.X + 14f, panel.Y + 17f, 28f, 4f);
            g.FillRectangle(DrawingHelper.Brush(accent), panel.X + 14f, panel.Y + 17f, 4f, 22f);
            g.FillRectangle(DrawingHelper.Brush(accent), panel.Right - 42f, panel.Bottom - 21f, 28f, 4f);
            g.FillRectangle(DrawingHelper.Brush(accent), panel.Right - 18f, panel.Bottom - 39f, 4f, 22f);

            DrawingHelper.DrawText(g, TitleText(), UiFonts.Display(38f, FontStyle.Bold),
                accent, new RectangleF(panel.X + 24f, panel.Y + 28f, panel.Width - 48f, 50f),
                StringAlignment.Center, StringAlignment.Center);

            DrawingHelper.DrawText(g, SubtitleText(), UiFonts.Mono(8.5f, FontStyle.Bold),
                Palette.SecondaryText, new RectangleF(panel.X + 24f, panel.Y + 81f, panel.Width - 48f, 18f),
                StringAlignment.Center, StringAlignment.Center);

            if (_newBest)
            {
                RectangleF badge = new RectangleF(panel.X + panel.Width - 190f, panel.Y + 33f, 154f, 24f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Warning), badge.X, badge.Y, badge.Width, badge.Height);
                DrawingHelper.DrawText(g, "NEW BEST", UiFonts.Mono(7f, FontStyle.Bold),
                    Palette.Shadow, badge, StringAlignment.Center, StringAlignment.Center);
            }

            DrawLargeScore(g, panel, accent);
            DrawStatGrid(g, panel, accent);

            if (!String.IsNullOrEmpty(_notice))
            {
                RectangleF notice = new RectangleF(panel.X + 54f, panel.Y + 418f, panel.Width - 108f, 34f);
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(232, 17, 20, 27)),
                    notice.X, notice.Y, notice.Width, notice.Height);
                g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Warning, 165), 1f),
                    notice.X, notice.Y, notice.Width, notice.Height);
                DrawingHelper.DrawText(g, _notice, UiFonts.Mono(8f, FontStyle.Bold),
                    Palette.Warning, notice, StringAlignment.Center, StringAlignment.Center);
            }

            DrawingHelper.DrawText(g, PrimaryHint(), UiFonts.Mono(7f, FontStyle.Regular),
                Palette.SecondaryText, new RectangleF(panel.X + 40f, panel.Y + 466f,
                    panel.Width - 80f, 30f),
                StringAlignment.Center, StringAlignment.Center);

            for (int i = 0; i < _actions.Length; i++)
            {
                _actions[i].Draw(g, _focus == i, _hover == i, false, time);
            }

            DrawingHelper.DrawText(g, GameConfig.Version, UiFonts.Mono(6.5f, FontStyle.Regular),
                Palette.Alpha(Palette.SecondaryText, 150),
                new RectangleF(0f, 694f, GameConfig.VirtualWidth, 14f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawLargeScore(Graphics g, RectangleF panel, Color accent)
        {
            RectangleF scoreBox = new RectangleF(panel.X + 54f, panel.Y + 117f, panel.Width - 108f, 84f);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(225, 14, 17, 23)),
                scoreBox.X, scoreBox.Y, scoreBox.Width, scoreBox.Height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(accent, 135), 1f),
                scoreBox.X, scoreBox.Y, scoreBox.Width, scoreBox.Height);

            DrawingHelper.DrawText(g, "FINAL SCORE", UiFonts.Mono(7f, FontStyle.Bold),
                Palette.SecondaryText,
                new RectangleF(scoreBox.X + 24f, scoreBox.Y + 14f, 220f, 14f),
                StringAlignment.Near, StringAlignment.Center);
            int score = _result == null ? 0 : _result.Score;
            string scoreText = _result != null && _result.ModeCompleted
                ? "COMPLETED"
                : score.ToString("000") + " / 100";
            DrawingHelper.DrawText(g, scoreText, UiFonts.Display(38f, FontStyle.Bold),
                accent, new RectangleF(scoreBox.X + 24f, scoreBox.Y + 29f, 360f, 48f),
                StringAlignment.Near, StringAlignment.Center);

            string mode = _result == null ? "--" : DifficultySettings.GetDisplayName(_result.Difficulty);
            DrawingHelper.DrawText(g, "MODE", UiFonts.Mono(7f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(scoreBox.Right - 278f, scoreBox.Y + 14f, 110f, 14f),
                StringAlignment.Far, StringAlignment.Center);
            DrawingHelper.DrawText(g, mode, UiFonts.Mono(19f, FontStyle.Bold), Palette.PrimaryText,
                new RectangleF(scoreBox.Right - 284f, scoreBox.Y + 31f, 250f, 36f),
                StringAlignment.Far, StringAlignment.Center);

            // Segmented score bar.
            float progress = _result == null ? 0f : Math.Max(0f, Math.Min(1f, _result.Score / 100f));
            RectangleF bar = new RectangleF(panel.X + 54f, panel.Y + 211f, panel.Width - 108f, 8f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), bar.X, bar.Y, bar.Width, bar.Height);
            for (int i = 0; i < 20; i++)
            {
                float x = bar.X + i * (bar.Width / 20f);
                g.FillRectangle(DrawingHelper.Brush(i < (int)Math.Round(progress * 20f)
                    ? accent : Color.FromArgb(57, 60, 69)), x, bar.Y, bar.Width / 20f - 3f, bar.Height);
            }
        }

        private void DrawStatGrid(Graphics g, RectangleF panel, Color accent)
        {
            float x = panel.X + 54f;
            float y = panel.Y + 244f;
            float width = (panel.Width - 128f) / 3f;
            float height = 74f;

            DrawStat(g, new RectangleF(x, y, width, height), "KILLS",
                _result == null ? "0" : _result.Kills.ToString("00"), Palette.Success);
            DrawStat(g, new RectangleF(x + width + 20f, y, width, height), "ACCURACY",
                _result == null ? "0%" : _result.Accuracy.ToString("0") + "%", Palette.Warning);
            DrawStat(g, new RectangleF(x + (width + 20f) * 2f, y, width, height), "STREAK",
                _result == null ? "0" : _result.BestStreak.ToString("00"), Palette.PrimaryText);

            DrawStat(g, new RectangleF(x, y + 90f, width, height), "LIVES",
                _result == null ? "0" : _result.LivesRemaining.ToString() + " / 3",
                _result != null && _result.LivesRemaining > 0 ? Palette.Success : Palette.Blood);
            DrawStat(g, new RectangleF(x + width + 20f, y + 90f, width, height), "PERFECT",
                _result == null ? "0" : _result.PerfectWords.ToString("00"), Palette.Warning);
            DrawStat(g, new RectangleF(x + (width + 20f) * 2f, y + 90f, width, height), "TIME",
                _result == null ? "00:00" : FormatDuration(_result.Duration), accent);
        }

        private static void DrawStat(Graphics g, RectangleF rect, string label, string value, Color accent)
        {
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(224, 14, 17, 23)),
                rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(accent, 110), 1f),
                rect.X, rect.Y, rect.Width, rect.Height);
            g.FillRectangle(DrawingHelper.Brush(accent), rect.X, rect.Y, 4f, rect.Height);
            DrawingHelper.DrawText(g, label, UiFonts.Mono(6.5f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(rect.X + 16f, rect.Y + 10f, rect.Width - 28f, 14f),
                StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, value, UiFonts.Mono(16f, FontStyle.Bold), accent,
                new RectangleF(rect.X + 15f, rect.Y + 27f, rect.Width - 26f, 34f),
                StringAlignment.Near, StringAlignment.Center);
        }

        private Color KindColour()
        {
            if (_kind == ResultKind.ModeComplete) return Palette.Success;
            if (_kind == ResultKind.GameOver) return Palette.Blood;
            return Palette.Warning;
        }

        private string TitleText()
        {
            if (_kind == ResultKind.GameOver) return "GAME OVER";
            if (_kind == ResultKind.FinalVictory) return "FINAL VICTORY";
            return "MODE COMPLETE";
        }

        private string SubtitleText()
        {
            if (_result == null) return String.Empty;
            if (_kind == ResultKind.GameOver)
            {
                return DifficultySettings.GetDisplayName(_result.Difficulty) +
                    " FAILED // UNLOCKS KEPT";
            }
            if (_kind == ResultKind.FinalVictory)
            {
                return "ALL MODES CLEARED";
            }
            return DifficultySettings.GetDisplayName(_result.Difficulty) + " CLEARED";
        }

        private string PrimaryHint()
        {
            if (_kind == ResultKind.GameOver)
            {
                return "RETRY RESTARTS THE MODE.";
            }
            if (_kind == ResultKind.FinalVictory)
            {
                return "PLAY AGAIN RETURNS TO MODE SELECT.";
            }
            return "CONTINUE OPENS THE NEXT MODE.";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            int totalMinutes = (int)duration.TotalMinutes;
            return totalMinutes.ToString("00") + ":" + duration.Seconds.ToString("00");
        }

        public bool HandleKey(Keys key)
        {
            if (key == Keys.Left || key == Keys.Right || key == Keys.Tab ||
                key == Keys.Up || key == Keys.Down)
            {
                _focus = 1 - _focus;
                _audio.Play(GameSound.MenuHover);
                Invalidate();
                return true;
            }
            if (key == Keys.Enter || key == Keys.Space)
            {
                Activate(_focus);
                return true;
            }
            if (key == Keys.Escape)
            {
                _audio.Play(GameSound.MenuSelect);
                if (MainMenuRequested != null) MainMenuRequested(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void Activate(int index)
        {
            _audio.Play(GameSound.MenuSelect);
            if (index == 0)
            {
                if (PrimaryRequested != null) PrimaryRequested(this, EventArgs.Empty);
            }
            else
            {
                if (MainMenuRequested != null) MainMenuRequested(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF point = ToVirtual(e.Location);
            int old = _hover;
            _hover = -1;
            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i].HitTest(point)) _hover = i;
            }
            if (_hover >= 0) _focus = _hover;
            if (old != _hover)
            {
                Cursor = _hover >= 0 ? Cursors.Hand : Cursors.Default;
                if (_hover >= 0) _audio.Play(GameSound.MenuHover);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            PointF point = ToVirtual(e.Location);
            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i].HitTest(point))
                {
                    _focus = i;
                    Activate(i);
                    return;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _snapshot != null)
            {
                _snapshot.Dispose();
                _snapshot = null;
            }
            base.Dispose(disposing);
        }
    }
}
