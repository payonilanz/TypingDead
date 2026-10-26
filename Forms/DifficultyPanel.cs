using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.Systems;
using TypingDead.UI;

namespace TypingDead.Forms
{
    public sealed class DifficultyPlayEventArgs : EventArgs
    {
        public GameDifficulty Difficulty { get; private set; }

        public DifficultyPlayEventArgs(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
        }
    }

    /// <summary>
    /// Difficulty selection screen. Cards read their locked/completed state from
    /// ProgressionManager and expose a real Play button on every unlocked mode.
    /// </summary>
    public sealed class DifficultyPanel : VirtualScreenPanel
    {
        private readonly AudioManager _audio;
        private readonly HighScoreManager _highScores;
        private readonly ProgressionManager _progression;
        private readonly RectangleF[] _cards;
        private readonly NeonAction[] _playActions;
        private readonly NeonAction _back;
        private int _focus;
        private int _hoverCard = -1;
        private string _notice;
        private float _noticeTimer;

        public event EventHandler BackRequested;
        public event EventHandler<DifficultyPlayEventArgs> PlayRequested;

        public DifficultyPanel(AudioManager audio, HighScoreManager highScores,
            ProgressionManager progression)
        {
            _audio = audio;
            _highScores = highScores;
            _progression = progression;
            _focus = 0;

            float width = 386f;
            _cards = new RectangleF[]
            {
                new RectangleF(40f, 174f, width, 402f),
                new RectangleF(447f, 174f, width, 402f),
                new RectangleF(854f, 174f, width, 402f)
            };

            _playActions = new NeonAction[]
            {
                new NeonAction("PLAY EASY", new RectangleF(64f, 508f, 338f, 48f)),
                new NeonAction("PLAY MEDIUM", new RectangleF(471f, 508f, 338f, 48f)),
                new NeonAction("PLAY HARD", new RectangleF(878f, 508f, 338f, 48f))
            };
            for (int i = 0; i < _playActions.Length; i++)
            {
                _playActions[i].Primary = true;
                _playActions[i].Hint = "ENTER";
            }
            _playActions[0].Accent = Palette.Success;
            _playActions[1].Accent = Palette.Warning;
            _playActions[2].Accent = Palette.Blood;

            _back = new NeonAction("BACK", new RectangleF(40f, 626f, 178f, 44f));
            _back.Accent = Palette.SecondaryText;
            _back.Hint = "ESC";

            Cursor = Cursors.Default;
        }

        public void SetProgression(ProgressionManager progression, string notice)
        {
            _notice = notice ?? String.Empty;
            _noticeTimer = String.IsNullOrEmpty(_notice) ? 0f : 5.5f;
            Cursor = Cursors.Default;
            Focus();
            Invalidate();
        }

        public void SelectDifficulty(GameDifficulty difficulty)
        {
            _focus = (int)difficulty;
        }

        protected override void OnFrame(float deltaTime)
        {
            if (_noticeTimer > 0f)
            {
                _noticeTimer = Math.Max(0f, _noticeTimer - deltaTime);
                if (_noticeTimer <= 0f) _notice = String.Empty;
            }
        }

        protected override void Render(Graphics g, float time)
        {
            PixelBackground.Draw(g, time, 0.9f);

            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(150, 8, 10, 15)),
                0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);

            DrawingHelper.DrawText(g, "SELECT MODE",
                UiFonts.Display(34f, FontStyle.Bold), Palette.PrimaryText,
                new RectangleF(0f, 34f, GameConfig.VirtualWidth, 48f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "REACH 100 POINTS IN ONE RUN",
                UiFonts.Mono(8f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(0f, 83f, GameConfig.VirtualWidth, 20f),
                StringAlignment.Center, StringAlignment.Center);

            if (!String.IsNullOrEmpty(_notice) && _noticeTimer > 0f)
            {
                float alpha = Math.Min(1f, _noticeTimer / 0.5f);
                RectangleF banner = new RectangleF(388f, 119f, 504f, 34f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Shadow, (int)(230 * alpha))),
                    banner.X, banner.Y, banner.Width, banner.Height);
                g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Warning, (int)(255 * alpha)), 2f),
                    banner.X, banner.Y, banner.Width, banner.Height);
                DrawingHelper.DrawText(g, _notice, UiFonts.Mono(9f, FontStyle.Bold),
                    Palette.Alpha(Palette.Warning, (int)(255 * alpha)), banner,
                    StringAlignment.Center, StringAlignment.Center);
            }

            DrawModeCard(g, 0, GameDifficulty.Easy, time);
            DrawModeCard(g, 1, GameDifficulty.Medium, time);
            DrawModeCard(g, 2, GameDifficulty.Hard, time);

            if (_progression.AllModesCompleted)
            {
                DrawingHelper.DrawText(g, "ALL MODES CLEARED",
                    UiFonts.Mono(8f, FontStyle.Bold), Palette.Warning,
                    new RectangleF(0f, 592f, GameConfig.VirtualWidth, 20f),
                    StringAlignment.Center, StringAlignment.Center);
            }
            else
            {
                DrawingHelper.DrawText(g, "A FAILED RUN RELOCKS LATER MODES",
                    UiFonts.Mono(7f, FontStyle.Regular), Palette.SecondaryText,
                    new RectangleF(0f, 592f, GameConfig.VirtualWidth, 18f),
                    StringAlignment.Center, StringAlignment.Center);
            }

            _back.Draw(g, false, false, false, time);
            DrawingHelper.DrawText(g, "LEFT/RIGHT SELECT     ENTER PLAY     ESC BACK",
                UiFonts.Mono(7f, FontStyle.Regular), Palette.SecondaryText,
                new RectangleF(244f, 637f, 792f, 22f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawModeCard(Graphics g, int index, GameDifficulty difficulty, float time)
        {
            RectangleF card = _cards[index];
            bool unlocked = _progression.IsUnlocked(difficulty);
            bool completed = _progression.IsCompleted(difficulty);
            bool focused = _focus == index;
            bool hovered = _hoverCard == index;
            Color accent = ModeColour(difficulty);

            Color fill = unlocked ? Color.FromArgb(238, 24, 28, 37) : Color.FromArgb(235, 17, 19, 25);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow),
                card.X + 7f, card.Y + 7f, card.Width, card.Height);
            g.FillRectangle(DrawingHelper.Brush(fill),
                card.X, card.Y, card.Width, card.Height);
            g.DrawRectangle(DrawingHelper.Pen(
                focused || hovered ? accent : Palette.Alpha(accent, 105),
                focused || hovered ? 3f : 1f), card.X, card.Y, card.Width, card.Height);

            // Mode number strip.
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(accent, unlocked ? 190 : 65)),
                card.X, card.Y, card.Width, 7f);
            DrawingHelper.DrawText(g, "MODE 0" + (index + 1).ToString(),
                UiFonts.Mono(7f, FontStyle.Bold), unlocked ? accent : Palette.SecondaryText,
                new RectangleF(card.X + 20f, card.Y + 16f, card.Width - 40f, 16f),
                StringAlignment.Near, StringAlignment.Center);

            if (completed)
            {
                RectangleF badge = new RectangleF(card.Right - 136f, card.Y + 14f, 116f, 22f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Success), badge.X, badge.Y, badge.Width, badge.Height);
                DrawingHelper.DrawText(g, "COMPLETED", UiFonts.Mono(7f, FontStyle.Bold),
                    Palette.Shadow, badge, StringAlignment.Center, StringAlignment.Center);
            }

            DrawingHelper.DrawText(g, DifficultySettings.GetDisplayName(difficulty),
                UiFonts.Display(27f, FontStyle.Bold),
                unlocked ? accent : Palette.SecondaryText,
                new RectangleF(card.X + 20f, card.Y + 40f, card.Width - 40f, 42f),
                StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, DifficultySettings.Create(difficulty).ShortImpact,
                UiFonts.Mono(7.5f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(card.X + 20f, card.Y + 82f, card.Width - 40f, 16f),
                StringAlignment.Near, StringAlignment.Center);

            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(accent, 85)),
                card.X + 20f, card.Y + 108f, card.Width - 40f, 2f);

            DifficultySettings tuning = DifficultySettings.Create(difficulty);
            DrawingHelper.DrawText(g, tuning.Description,
                UiFonts.Body(13f, FontStyle.Regular), Palette.PrimaryText,
                new RectangleF(card.X + 20f, card.Y + 122f, card.Width - 40f, 52f),
                StringAlignment.Near, StringAlignment.Near);

            DrawInfoRow(g, card, 184f, "ENEMIES", tuning.ZombieSummary, accent);
            DrawInfoRow(g, card, 232f, "CHARS", tuning.MinimumWordLength.ToString() + "-" +
                tuning.MaximumWordLength.ToString(), accent);
            DrawInfoRow(g, card, 280f, "BEST",
                _highScores.GetBest(difficulty).ToString("000") + " / 100", accent);

            string status;
            if (completed)
            {
                status = "COMPLETE // REPLAY";
            }
            else if (unlocked)
            {
                status = "READY";
            }
            else if (difficulty == GameDifficulty.Medium)
            {
                status = "LOCKED // CLEAR EASY";
            }
            else
            {
                status = "LOCKED // CLEAR MEDIUM";
            }
            DrawingHelper.DrawText(g, status, UiFonts.Mono(8.5f, FontStyle.Bold),
                unlocked ? Palette.Success : Palette.Blood,
                new RectangleF(card.X + 20f, card.Y + 318f, card.Width - 40f, 18f),
                StringAlignment.Near, StringAlignment.Center);

            if (!unlocked)
            {
                // Dim the lower half of the card and draw a pixel padlock.
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(150, 13, 15, 20)),
                    card.X + 1f, card.Y + 342f, card.Width - 2f, card.Height - 343f);
                DrawPadlock(g, card.X + card.Width * 0.5f, card.Y + 354f, accent);
            }
            else
            {
                _playActions[index].Enabled = true;
                _playActions[index].Text = completed ? "REPLAY " + DifficultySettings.GetDisplayName(difficulty)
                    : "PLAY " + DifficultySettings.GetDisplayName(difficulty);
                _playActions[index].Draw(g, focused, hovered, false, time);
            }
        }

        private static void DrawInfoRow(Graphics g, RectangleF card, float offset,
            string label, string value, Color accent)
        {
            DrawingHelper.DrawText(g, label, UiFonts.Mono(8f, FontStyle.Bold),
                Palette.SecondaryText,
                new RectangleF(card.X + 20f, card.Y + offset, 88f, 16f),
                StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, value, UiFonts.Mono(9f, FontStyle.Bold),
                Palette.Alpha(Palette.PrimaryText, 220),
                new RectangleF(card.X + 108f, card.Y + offset, card.Width - 128f, 16f),
                StringAlignment.Near, StringAlignment.Center);
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(accent, 65)),
                card.X + 20f, card.Y + offset + 19f, card.Width - 40f, 1f);
        }

        private static void DrawPadlock(Graphics g, float cx, float cy, Color accent)
        {
            g.FillRectangle(DrawingHelper.Brush(Palette.SecondaryText), cx - 18f, cy - 13f, 36f, 6f);
            g.FillRectangle(DrawingHelper.Brush(Palette.SecondaryText), cx - 18f, cy - 13f, 6f, 22f);
            g.FillRectangle(DrawingHelper.Brush(Palette.SecondaryText), cx + 12f, cy - 13f, 6f, 22f);
            g.FillRectangle(DrawingHelper.Brush(accent), cx - 24f, cy + 3f, 48f, 34f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), cx - 3f, cy + 13f, 6f, 15f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), cx - 7f, cy + 13f, 14f, 6f);
        }

        private static Color ModeColour(GameDifficulty difficulty)
        {
            if (difficulty == GameDifficulty.Easy) return Palette.Success;
            if (difficulty == GameDifficulty.Medium) return Palette.Warning;
            return Palette.Blood;
        }

        public bool HandleKey(Keys key)
        {
            if (key == Keys.Left || key == Keys.A || key == Keys.Up || key == Keys.W)
            {
                _focus = (_focus + 2) % 3;
                _audio.Play(GameSound.MenuHover);
                Invalidate();
                return true;
            }
            if (key == Keys.Right || key == Keys.D || key == Keys.Down || key == Keys.S)
            {
                _focus = (_focus + 1) % 3;
                _audio.Play(GameSound.MenuHover);
                Invalidate();
                return true;
            }
            if (key == Keys.Enter || key == Keys.Space)
            {
                TryPlay(_focus);
                return true;
            }
            if (key == Keys.Escape || key == Keys.Back)
            {
                _audio.Play(GameSound.MenuSelect);
                if (BackRequested != null) BackRequested(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void TryPlay(int index)
        {
            GameDifficulty difficulty = (GameDifficulty)index;
            if (!_progression.IsUnlocked(difficulty))
            {
                _audio.Play(GameSound.TypeWrong);
                _notice = difficulty == GameDifficulty.Medium
                    ? "LOCKED // CLEAR EASY FIRST"
                    : "LOCKED // CLEAR MEDIUM FIRST";
                _noticeTimer = 2.6f;
                Invalidate();
                return;
            }

            _audio.Play(GameSound.MenuSelect);
            if (PlayRequested != null)
            {
                PlayRequested(this, new DifficultyPlayEventArgs(difficulty));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF point = ToVirtual(e.Location);
            int oldCard = _hoverCard;
            _hoverCard = -1;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i].Contains(point)) _hoverCard = i;
            }
            if (_hoverCard >= 0)
            {
                _focus = _hoverCard;
            }
            if (oldCard != _hoverCard)
            {
                Cursor = _hoverCard >= 0 ? Cursors.Hand : Cursors.Default;
                if (_hoverCard >= 0) _audio.Play(GameSound.MenuHover);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverCard = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            PointF point = ToVirtual(e.Location);

            for (int i = 0; i < _playActions.Length; i++)
            {
                if (_progression.IsUnlocked((GameDifficulty)i) && _playActions[i].HitTest(point))
                {
                    _focus = i;
                    TryPlay(i);
                    return;
                }
            }

            if (_back.HitTest(point))
            {
                _audio.Play(GameSound.MenuSelect);
                if (BackRequested != null) BackRequested(this, EventArgs.Empty);
                return;
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                if (!_cards[i].Contains(point)) continue;
                _focus = i;
                _audio.Play(GameSound.MenuHover);
                Invalidate();
                return;
            }
        }
    }
}
