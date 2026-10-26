using System;
using System.Drawing;
using TypingDead.Rendering;

namespace TypingDead.UI
{
    /// <summary>
    /// Shared pixel button model. Screens own the button list and handle mouse
    /// and keyboard focus; the drawing stays consistent everywhere.
    /// </summary>
    public sealed class NeonAction
    {
        public string Text;
        public RectangleF Bounds;
        public bool Primary;
        public bool Enabled;
        public string Hint;
        public Color Accent;

        public NeonAction(string text, RectangleF bounds)
        {
            Text = text;
            Bounds = bounds;
            Primary = false;
            Enabled = true;
            Hint = String.Empty;
            Accent = Palette.Success;
        }

        public bool HitTest(PointF point)
        {
            return Enabled && Bounds.Contains(point);
        }

        public void Draw(Graphics g, bool focused, bool hovered, bool pressed, float time)
        {
            bool active = focused || hovered;
            RectangleF rect = Bounds;
            if (pressed && Enabled)
            {
                rect = new RectangleF(rect.X + 2f, rect.Y + 3f, rect.Width, rect.Height);
            }

            Color accent = Accent;
            if (!Enabled) accent = Palette.SecondaryText;

            // Hard offset shadow is a cheap, readable pixel-art convention.
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow),
                rect.X + 4f, rect.Y + 4f, rect.Width, rect.Height);

            Color fill = Primary ? Color.FromArgb(47, 63, 54) : Color.FromArgb(27, 31, 41);
            if (active && Enabled)
            {
                fill = Primary ? Color.FromArgb(65, 91, 65) : Color.FromArgb(44, 50, 60);
            }
            if (!Enabled)
            {
                fill = Color.FromArgb(24, 26, 33);
            }

            g.FillRectangle(DrawingHelper.Brush(fill), rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawRectangle(DrawingHelper.Pen(active && Enabled ? accent : Palette.Alpha(accent, 120),
                active && Enabled ? 2f : 1f), rect.X, rect.Y, rect.Width, rect.Height);

            // Corner pixels give the button a blocky arcade outline.
            int corner = active && Enabled ? 4 : 2;
            g.FillRectangle(DrawingHelper.Brush(accent), rect.X - 1f, rect.Y + 8f, corner, rect.Height - 16f);
            g.FillRectangle(DrawingHelper.Brush(accent), rect.Right - corner + 1f, rect.Y + 8f, corner, rect.Height - 16f);

            if (active && Enabled)
            {
                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(accent, 45)),
                    rect.X + 5f, rect.Y + 5f, rect.Width - 10f, 2f);
            }

            Font font = UiFonts.Display(Primary ? 15f : 13f, FontStyle.Bold);
            Color textColour = Enabled
                ? (active ? Palette.PrimaryText : Palette.Alpha(Palette.PrimaryText, 220))
                : Palette.SecondaryText;
            DrawingHelper.DrawText(g, Text, font, textColour,
                new RectangleF(rect.X + 25f, rect.Y, rect.Width - 52f, rect.Height),
                StringAlignment.Near, StringAlignment.Center);

            if (!String.IsNullOrEmpty(Hint) && Enabled)
            {
                DrawingHelper.DrawText(g, Hint, UiFonts.Mono(8f, FontStyle.Regular),
                    Palette.Alpha(accent, active ? (byte)230 : (byte)150),
                    new RectangleF(rect.X + 24f, rect.Y, rect.Width - 38f, rect.Height),
                    StringAlignment.Far, StringAlignment.Center);
            }

            g.FillRectangle(DrawingHelper.Brush(accent), rect.X + 10f, rect.Y + rect.Height * 0.5f - 6f, 3f, 12f);
        }
    }
}
