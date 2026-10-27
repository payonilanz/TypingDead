using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Forms
{
    /// <summary>
    /// Illustrated rules screen. It explains the Enter-to-attack model, the
    /// three modes, the three-word Tank Zombie and the progression reset rule.
    /// </summary>
    public sealed class TutorialPanel : VirtualScreenPanel
    {
        private readonly NeonAction _back;

        public event EventHandler Closed;

        public TutorialPanel()
        {
            _back = new NeonAction("BACK", new RectangleF(44f, 632f, 200f, 48f));
            _back.Primary = true;
            _back.Hint = "ESC";
            Cursor = Cursors.Default;
        }

        protected override void Render(Graphics g, float time)
        {
            PixelBackground.Draw(g, time, 1f);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(184, 8, 10, 15)),
                0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);

            DrawingHelper.DrawText(g, "HOW TO PLAY", UiFonts.Display(34f, FontStyle.Bold),
                Palette.PrimaryText, new RectangleF(0f, 26f, GameConfig.VirtualWidth, 46f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "THREE LANES  //  THREE LIVES  //  100 POINTS PER MODE",
                UiFonts.Mono(8f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(0f, 72f, GameConfig.VirtualWidth, 20f),
                StringAlignment.Center, StringAlignment.Center);

            DrawSection(g, new RectangleF(44f, 112f, 568f, 250f),
                "MOVE AND SURVIVE", Palette.Success, new string[]
                {
                    "Three lanes. UP/DOWN or W/S moves.",
                    "",
                    "The nearest zombie in your lane is",
                    "targeted automatically.",
                    "",
                    "A breach or wrong word costs 1 life.",
                    "Empty Enter is always safe.",
                    "",
                    "Reach 100 points to clear the mode."
                });

            DrawSection(g, new RectangleF(668f, 112f, 568f, 250f),
                "TYPE. PRESS ENTER.", Palette.Warning, new string[]
                {
                    "Type with A-Z. Use SPACE between words.",
                    "Wrong keys flash red but cost no life.",
                    "",
                    "Complete word: WORD READY: PRESS ENTER.",
                    "Press ENTER once to attack.",
                    "",
                    "Empty ENTER: TYPE A WORD FIRST (safe).",
                    "Wrong word: -1 life.",
                    "",
                    "Repeated ENTER cannot duplicate an attack."
                });

            DrawSection(g, new RectangleF(44f, 370f, 568f, 250f),
                "ENEMIES", Palette.Blood, new string[]
                {
                    "WALKER  -  slow, one word, 10 points.",
                    "RUNNER  -  fast, one word, 10 points.",
                    "TANK    -  3 words, 3 health segments.",
                    "           Points only after hit 3.",
                    "",
                    "HARD    -  Runner waves with long terms.",
                    "No Tanks or Specials in Hard mode.",
                    "",
                    "Lane changes keep word and health progress."
                });

            DrawSection(g, new RectangleF(668f, 370f, 568f, 250f),
                "MODES", Palette.ZombieSkin, new string[]
                {
                    "EASY    -  3-5 characters.",
                    "MEDIUM  -  5-9 characters.",
                    "HARD    -  10-18 characters.",
                    "",
                    "Start with Easy only. Clear a mode to unlock",
                    "the next. Failure resets unlocks to Easy.",
                    "Completed modes can be replayed.",
                    "",
                    "Best time is tracked for each mode."
                });

            DrawControlStrip(g, 262f, 632f, 974f, 48f);
            _back.Draw(g, false, false, false, time);
        }

        private static void DrawSection(Graphics g, RectangleF rect, string title,
            Color accent, string[] lines)
        {
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow),
                rect.X + 6f, rect.Y + 6f, rect.Width, rect.Height);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(240, 21, 24, 32)),
                rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(accent, 135), 1f),
                rect.X, rect.Y, rect.Width, rect.Height);
            g.FillRectangle(DrawingHelper.Brush(accent), rect.X, rect.Y, 5f, rect.Height);

            DrawingHelper.DrawText(g, title, UiFonts.Display(15f, FontStyle.Bold), accent,
                new RectangleF(rect.X + 20f, rect.Y + 14f, rect.Width - 44f, 24f),
                StringAlignment.Near, StringAlignment.Center);
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(accent, 70)),
                rect.X + 20f, rect.Y + 43f, rect.Width - 40f, 1f);

            Font body = UiFonts.Mono(10.5f, FontStyle.Regular);
            float bodyTop = rect.Y + 54f;
            float bodyBottom = rect.Bottom - 16f;
            float available = Math.Max(1f, bodyBottom - bodyTop);
            float spacing = Math.Min(24f, available / Math.Max(1, lines.Length));
            float blockHeight = spacing * lines.Length;
            float startY = bodyTop + Math.Max(0f, (available - blockHeight) * 0.5f);
            for (int i = 0; i < lines.Length; i++)
            {
                DrawingHelper.DrawText(g, lines[i], body, Palette.PrimaryText,
                    new RectangleF(rect.X + 22f, startY + i * spacing, rect.Width - 44f, 18f),
                    StringAlignment.Near, StringAlignment.Center);
            }
        }

        private static void DrawControlStrip(Graphics g, float x, float y, float width, float height)
        {
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(232, 16, 19, 26)),
                x, y, width, height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Success, 120), 1f),
                x, y, width, height);
            DrawingHelper.DrawText(g, "CONTROLS", UiFonts.Mono(6.5f, FontStyle.Bold),
                Palette.Success, new RectangleF(x + 14f, y + 4f, 100f, 13f),
                StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g,
                "UP/DOWN  MOVE     A-Z + SPACE  TYPE     ENTER  ATTACK     BACKSPACE  EDIT     ESC  PAUSE     F1  HELP",
                UiFonts.Mono(7f, FontStyle.Bold), Palette.PrimaryText,
                new RectangleF(x + 112f, y + 4f, width - 126f, 18f),
                StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g,
                "TYPE A WORD, THEN PRESS ENTER. WRONG KEYS ARE SAFE. EMPTY ENTER IS SAFE.",
                UiFonts.Mono(6.5f, FontStyle.Regular), Palette.SecondaryText,
                new RectangleF(x + 14f, y + 24f, width - 28f, 16f),
                StringAlignment.Near, StringAlignment.Center);
        }

        public bool HandleKey(Keys key)
        {
            if (key == Keys.Escape || key == Keys.Back || key == Keys.Enter ||
                key == Keys.Space || key == Keys.F1)
            {
                if (Closed != null) Closed(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (_back.HitTest(ToVirtual(e.Location)) && Closed != null)
            {
                Closed(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF point = ToVirtual(e.Location);
            Cursor = _back.HitTest(point) ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }
}
