using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.Systems;
using TypingDead.UI;

namespace TypingDead.Forms
{
    /// <summary>
    /// Main menu. Play opens the separate difficulty screen; no mode selection
    /// is hidden on this menu. The required group footer is drawn exactly once.
    /// </summary>
    public sealed class MainMenuPanel : VirtualScreenPanel
    {
        private readonly AudioManager _audio;
        private readonly HighScoreManager _highScores;
        private readonly NeonAction[] _actions;
        private readonly NeonAction[] _confirmActions;
        private int _focus;
        private int _hover = -1;
        private int _confirmFocus;
        private bool _confirmExit;
        private float _time;

        public event EventHandler PlayRequested;
        public event EventHandler TutorialRequested;
        public event EventHandler SettingsRequested;
        public event EventHandler ExitRequested;

        public MainMenuPanel(AudioManager audio, HighScoreManager highScores)
        {
            _audio = audio;
            _highScores = highScores;
            _focus = 0;

            _actions = new NeonAction[]
            {
                new NeonAction("PLAY", new RectangleF(455f, 334f, 370f, 54f)),
                new NeonAction("HOW TO PLAY", new RectangleF(455f, 400f, 370f, 54f)),
                new NeonAction("SETTINGS", new RectangleF(455f, 466f, 370f, 54f)),
                new NeonAction("EXIT GAME", new RectangleF(455f, 532f, 370f, 54f))
            };
            _actions[0].Primary = true;
            _actions[0].Hint = "ENTER";
            _actions[1].Accent = Palette.Warning;
            _actions[2].Accent = Palette.ZombieSkin;
            _actions[3].Accent = Palette.Blood;

            _confirmActions = new NeonAction[]
            {
                new NeonAction("LEAVE", new RectangleF(420f, 382f, 210f, 48f)),
                new NeonAction("STAY", new RectangleF(650f, 382f, 210f, 48f))
            };
            _confirmActions[0].Accent = Palette.Blood;
            _confirmActions[1].Primary = true;

            Cursor = Cursors.Default;
        }

        public void RefreshScores()
        {
            Invalidate();
        }

        public void FocusMenu()
        {
            _confirmExit = false;
            _focus = 0;
            _hover = -1;
            Focus();
        }

        protected override void OnFrame(float deltaTime)
        {
            _time += deltaTime;
        }

        protected override void Render(Graphics g, float time)
        {
            PixelBackground.Draw(g, time, 0.95f);

            // A simple pixel survivor and two distant zombies frame the title.
            DrawMenuSurvivor(g, 170f, 560f, time);
            DrawMenuZombie(g, 1118f, 552f, time, false);
            DrawMenuZombie(g, 1025f, 585f, time, true);

            // Title plates and hard offset shadows.
            DrawPixelTextShadow(g, "TYPING", UiFonts.Display(76f, FontStyle.Bold),
                new RectangleF(0f, 94f, 1280f, 90f), Palette.PrimaryText, Palette.Blood, 5f);
            DrawPixelTextShadow(g, "DEAD", UiFonts.Display(76f, FontStyle.Bold),
                new RectangleF(0f, 162f, 1280f, 90f), Palette.Success, Palette.Shadow, 5f);

            DrawingHelper.DrawText(g, "TYPE TO SURVIVE.",
                UiFonts.Mono(10f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(0f, 254f, 1280f, 20f),
                StringAlignment.Center, StringAlignment.Center);

            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), 434f, 292f, 412f, 2f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Success), 502f, 291f, 276f, 4f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), 834f, 292f, 12f, 2f);

            for (int i = 0; i < _actions.Length; i++)
            {
                _actions[i].Draw(g, _focus == i, _hover == i, false, time);
            }

            // Best score strip.
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Shadow, 220)),
                388f, 606f, 504f, 30f);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Success, 105), 1f),
                388f, 606f, 504f, 30f);
            string best = "BEST  EASY " + _highScores.GetBest(GameDifficulty.Easy).ToString("000") +
                "   MED " + _highScores.GetBest(GameDifficulty.Medium).ToString("000") +
                "   HARD " + _highScores.GetBest(GameDifficulty.Hard).ToString("000");
            DrawingHelper.DrawText(g, best, UiFonts.Mono(8f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(396f, 606f, 488f, 30f),
                StringAlignment.Center, StringAlignment.Center);

            // Exact required footer label, centered.
            DrawingHelper.DrawText(g, GameConfig.GroupLabel,
                UiFonts.Mono(9f, FontStyle.Bold), Palette.Alpha(Palette.PrimaryText, 220),
                new RectangleF(0f, 673f, GameConfig.VirtualWidth, 22f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, GameConfig.Version, UiFonts.Mono(6.5f, FontStyle.Regular),
                Palette.Alpha(Palette.SecondaryText, 150),
                new RectangleF(0f, 699f, GameConfig.VirtualWidth, 14f),
                StringAlignment.Center, StringAlignment.Center);

            if (_confirmExit)
            {
                DrawExitConfirmation(g, time);
            }
        }

        private void DrawExitConfirmation(Graphics g, float time)
        {
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(180, 8, 10, 15)),
                0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            RectangleF panel = new RectangleF(338f, 258f, 604f, 208f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), panel.X + 7f, panel.Y + 7f, panel.Width, panel.Height);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(244, 24, 27, 36)), panel.X, panel.Y, panel.Width, panel.Height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Blood, 2f), panel.X, panel.Y, panel.Width, panel.Height);

            DrawingHelper.DrawText(g, "LEAVE THE STREET?", UiFonts.Display(25f, FontStyle.Bold),
                Palette.PrimaryText, new RectangleF(panel.X, panel.Y + 26f, panel.Width, 38f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "Progress will be lost.",
                UiFonts.Body(10f, FontStyle.Regular), Palette.SecondaryText,
                new RectangleF(panel.X + 28f, panel.Y + 78f, panel.Width - 56f, 24f),
                StringAlignment.Center, StringAlignment.Center);

            for (int i = 0; i < _confirmActions.Length; i++)
            {
                _confirmActions[i].Draw(g, i == _confirmFocus, false, false, time);
            }
        }

        private void MoveFocus(int delta)
        {
            _focus = (_focus + delta + _actions.Length) % _actions.Length;
            _audio.Play(GameSound.MenuHover);
        }

        private void Activate(int index)
        {
            _audio.Play(GameSound.MenuSelect);
            switch (index)
            {
                case 0:
                    if (PlayRequested != null) PlayRequested(this, EventArgs.Empty);
                    break;
                case 1:
                    if (TutorialRequested != null) TutorialRequested(this, EventArgs.Empty);
                    break;
                case 2:
                    if (SettingsRequested != null) SettingsRequested(this, EventArgs.Empty);
                    break;
                case 3:
                    _confirmExit = true;
                    _confirmFocus = 0;
                    break;
            }
        }

        public bool HandleKey(Keys key)
        {
            if (_confirmExit)
            {
                if (key == Keys.Left || key == Keys.Right || key == Keys.Tab)
                {
                    _confirmFocus = 1 - _confirmFocus;
                    _audio.Play(GameSound.MenuHover);
                    return true;
                }
                if (key == Keys.Enter || key == Keys.Space)
                {
                    if (_confirmFocus == 0)
                    {
                        _audio.Play(GameSound.MenuSelect);
                        if (ExitRequested != null) ExitRequested(this, EventArgs.Empty);
                    }
                    else
                    {
                        _audio.Play(GameSound.MenuSelect);
                        _confirmExit = false;
                    }
                    return true;
                }
                if (key == Keys.Escape)
                {
                    _audio.Play(GameSound.MenuSelect);
                    _confirmExit = false;
                    return true;
                }
                return false;
            }

            if (key == Keys.Up || key == Keys.W)
            {
                MoveFocus(-1);
                return true;
            }
            if (key == Keys.Down || key == Keys.S)
            {
                MoveFocus(1);
                return true;
            }
            if (key == Keys.Enter || key == Keys.Space)
            {
                Activate(_focus);
                return true;
            }
            if (key == Keys.F1)
            {
                _audio.Play(GameSound.MenuSelect);
                if (TutorialRequested != null) TutorialRequested(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF point = ToVirtual(e.Location);

            if (_confirmExit)
            {
                int confirmHover = -1;
                for (int i = 0; i < _confirmActions.Length; i++)
                {
                    if (_confirmActions[i].HitTest(point)) confirmHover = i;
                }
                if (confirmHover >= 0) _confirmFocus = confirmHover;
                Cursor = confirmHover >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
                return;
            }

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

            if (_confirmExit)
            {
                for (int i = 0; i < _confirmActions.Length; i++)
                {
                    if (_confirmActions[i].HitTest(point))
                    {
                        _confirmFocus = i;
                        if (i == 0)
                        {
                            _audio.Play(GameSound.MenuSelect);
                            if (ExitRequested != null) ExitRequested(this, EventArgs.Empty);
                        }
                        else
                        {
                            _audio.Play(GameSound.MenuSelect);
                            _confirmExit = false;
                        }
                        return;
                    }
                }
                return;
            }

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

        private static void DrawPixelTextShadow(Graphics g, string text, Font font,
            RectangleF rect, Color colour, Color shadow, float offset)
        {
            RectangleF down = new RectangleF(rect.X + offset, rect.Y + offset, rect.Width, rect.Height);
            DrawingHelper.DrawText(g, text, font, shadow, down,
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, text, font, colour, rect,
                StringAlignment.Center, StringAlignment.Center);
        }

        private static void DrawMenuSurvivor(Graphics g, float x, float y, float time)
        {
            // Scaled-up pixel survivor silhouette, intentionally blocky.
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 28f, y + 64f, 90f, 9f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 20f, y + 16f, 16f, 51f);
            g.FillRectangle(DrawingHelper.Brush(Palette.HumanBlue), x - 17f, y + 18f, 11f, 38f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x + 8f, y + 16f, 17f, 51f);
            g.FillRectangle(DrawingHelper.Brush(Palette.HumanBlue), x + 11f, y + 18f, 11f, 38f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 25f, y - 34f, 53f, 56f);
            g.FillRectangle(DrawingHelper.Brush(Palette.HumanBlue), x - 21f, y - 30f, 45f, 47f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Success), x - 3f, y - 18f, 5f, 17f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 18f, y - 62f, 34f, 30f);
            g.FillRectangle(DrawingHelper.Brush(Palette.PrimaryText), x - 14f, y - 58f, 26f, 23f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 22f, y - 69f, 42f, 12f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Warning), x + 7f, y - 51f, 4f, 4f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x + 24f, y - 30f, 45f, 9f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Concrete), x + 38f, y - 34f, 32f, 7f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Warning), x + 67f, y - 33f, 7f, 5f);
        }

        private static void DrawMenuZombie(Graphics g, float x, float y, float time, bool alternate)
        {
            int frame = ((int)(time * 3f + (alternate ? 1 : 0))) % 2;
            float step = frame == 0 ? -3f : 3f;
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 22f, y + 42f, 62f, 7f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 13f + step, y + 4f, 10f, 39f);
            g.FillRectangle(DrawingHelper.Brush(Palette.ZombieSkinDark), x - 11f + step, y + 5f, 6f, 31f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x + 3f - step, y + 4f, 10f, 39f);
            g.FillRectangle(DrawingHelper.Brush(Palette.ZombieSkinDark), x + 5f - step, y + 5f, 6f, 31f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 18f, y - 28f, 40f, 39f);
            g.FillRectangle(DrawingHelper.Brush(Palette.ZombieSkin), x - 13f, y - 23f, 29f, 29f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), x - 15f, y - 48f, 32f, 23f);
            g.FillRectangle(DrawingHelper.Brush(Palette.ZombieSkin), x - 12f, y - 45f, 26f, 18f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), x - 7f, y - 38f, 5f, 3f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), x + 3f, y - 37f, 3f, 3f);
            g.FillRectangle(DrawingHelper.Brush(Palette.ZombieSkin), x - 42f, y - 15f, 26f, 7f);
            g.FillRectangle(DrawingHelper.Brush(Palette.ZombieSkinDark), x - 45f, y - 12f, 8f, 5f);
        }
    }
}
