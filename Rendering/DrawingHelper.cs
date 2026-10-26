using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using TypingDead.UI;

namespace TypingDead.Rendering
{
    /// <summary>
    /// Small drawing toolkit shared by all custom-painted screens. Brushes and
    /// pens are cached to avoid churn during 60 fps combat rendering.
    /// </summary>
    public static class DrawingHelper
    {
        private static readonly Dictionary<int, SolidBrush> BrushCache = new Dictionary<int, SolidBrush>();
        private static readonly Dictionary<string, Pen> PenCache = new Dictionary<string, Pen>();

        private static readonly StringFormat LeftTop;
        private static readonly StringFormat LeftMiddle;
        private static readonly StringFormat CenterTop;
        private static readonly StringFormat CenterMiddle;
        private static readonly StringFormat RightMiddle;
        private static readonly StringFormat CenterBottom;

        static DrawingHelper()
        {
            LeftTop = CreateFormat(StringAlignment.Near, StringAlignment.Near, false);
            LeftMiddle = CreateFormat(StringAlignment.Near, StringAlignment.Center, false);
            CenterTop = CreateFormat(StringAlignment.Center, StringAlignment.Near, false);
            CenterMiddle = CreateFormat(StringAlignment.Center, StringAlignment.Center, false);
            RightMiddle = CreateFormat(StringAlignment.Far, StringAlignment.Center, false);
            CenterBottom = CreateFormat(StringAlignment.Center, StringAlignment.Far, false);
        }

        private static StringFormat CreateFormat(StringAlignment horizontal, StringAlignment vertical, bool noWrap)
        {
            StringFormat format = new StringFormat(StringFormat.GenericTypographic);
            format.Alignment = horizontal;
            format.LineAlignment = vertical;
            format.Trimming = StringTrimming.EllipsisCharacter;
            if (noWrap) format.FormatFlags |= StringFormatFlags.NoWrap;
            return format;
        }

        public static SolidBrush Brush(Color colour)
        {
            int key = colour.ToArgb();
            SolidBrush brush;
            if (!BrushCache.TryGetValue(key, out brush))
            {
                brush = new SolidBrush(colour);
                BrushCache[key] = brush;
            }
            return brush;
        }

        public static Pen Pen(Color colour, float width)
        {
            int roundedWidth = (int)Math.Max(1, Math.Round(width * 2f));
            string key = colour.ToArgb().ToString() + "|" + roundedWidth.ToString();
            Pen pen;
            if (!PenCache.TryGetValue(key, out pen))
            {
                pen = new Pen(colour, roundedWidth / 2f);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                PenCache[key] = pen;
            }
            return pen;
        }

        public static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
        {
            float diameter = Math.Min(radius * 2f, Math.Min(rect.Width, rect.Height));
            GraphicsPath path = new GraphicsPath();
            if (diameter <= 1f)
            {
                path.AddRectangle(rect);
                return path;
            }

            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
            path.AddArc(arc, 180f, 90f);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270f, 90f);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0f, 90f);
            arc.X = rect.X;
            path.AddArc(arc, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, RectangleF rect, Color colour, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(rect, radius))
            {
                g.FillPath(Brush(colour), path);
            }
        }

        public static void DrawRounded(Graphics g, RectangleF rect, Color colour, float width, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(rect, radius))
            {
                g.DrawPath(Pen(colour, width), path);
            }
        }

        public static void FillGlowRounded(Graphics g, RectangleF rect, Color colour, float radius)
        {
            using (GraphicsPath outer = RoundedRectangle(rect, radius))
            {
                g.FillPath(Brush(Palette.Alpha(colour, 20)), outer);
            }
            RectangleF inner = new RectangleF(rect.X + 2f, rect.Y + 2f, rect.Width - 4f, rect.Height - 4f);
            using (GraphicsPath innerPath = RoundedRectangle(inner, Math.Max(1f, radius - 2f)))
            {
                g.FillPath(Brush(Palette.Alpha(colour, 26)), innerPath);
            }
        }

        public static void DrawPanel(Graphics g, RectangleF rect, Color fill, Color border, float radius)
        {
            FillRounded(g, rect, fill, radius);
            DrawRounded(g, rect, border, 1.2f, radius);
        }

        public static void DrawText(Graphics g, string text, Font font, Color colour, RectangleF rect,
            StringAlignment horizontal, StringAlignment vertical)
        {
            StringFormat format = FormatFor(horizontal, vertical);
            g.DrawString(text, font, Brush(colour), rect, format);
        }

        public static void DrawText(Graphics g, string text, Font font, Color colour, float x, float y)
        {
            g.DrawString(text, font, Brush(colour), x, y);
        }

        public static void DrawCentered(Graphics g, string text, Font font, Color colour, RectangleF rect)
        {
            DrawText(g, text, font, colour, rect, StringAlignment.Center, StringAlignment.Center);
        }

        private static StringFormat FormatFor(StringAlignment horizontal, StringAlignment vertical)
        {
            if (horizontal == StringAlignment.Near && vertical == StringAlignment.Near) return LeftTop;
            if (horizontal == StringAlignment.Near && vertical == StringAlignment.Center) return LeftMiddle;
            if (horizontal == StringAlignment.Center && vertical == StringAlignment.Near) return CenterTop;
            if (horizontal == StringAlignment.Center && vertical == StringAlignment.Center) return CenterMiddle;
            if (horizontal == StringAlignment.Far && vertical == StringAlignment.Center) return RightMiddle;
            return CenterBottom;
        }

        public static SizeF Measure(Graphics g, string text, Font font)
        {
            return g.MeasureString(text, font, 2000, StringFormat.GenericTypographic);
        }

        public static void DrawGlowText(Graphics g, string text, Font font, Color colour, RectangleF rect,
            StringAlignment horizontal, StringAlignment vertical, float glowStrength)
        {
            StringFormat format = FormatFor(horizontal, vertical);
            int alpha = (int)(38 * glowStrength);
            if (alpha > 0)
            {
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        if (i == 0 && j == 0) continue;
                        RectangleF offset = new RectangleF(rect.X + i, rect.Y + j, rect.Width, rect.Height);
                        g.DrawString(text, font, Brush(Palette.Alpha(colour, alpha)), offset, format);
                    }
                }
            }
            g.DrawString(text, font, Brush(colour), rect, format);
        }

        public static void DrawCornerBrackets(Graphics g, RectangleF rect, Color colour, float length, float width)
        {
            Pen pen = Pen(colour, width);
            g.DrawLine(pen, rect.Left, rect.Top, rect.Left + length, rect.Top);
            g.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Top + length);
            g.DrawLine(pen, rect.Right, rect.Top, rect.Right - length, rect.Top);
            g.DrawLine(pen, rect.Right, rect.Top, rect.Right, rect.Top + length);
            g.DrawLine(pen, rect.Left, rect.Bottom, rect.Left + length, rect.Bottom);
            g.DrawLine(pen, rect.Left, rect.Bottom, rect.Left, rect.Bottom - length);
            g.DrawLine(pen, rect.Right, rect.Bottom, rect.Right - length, rect.Bottom);
            g.DrawLine(pen, rect.Right, rect.Bottom, rect.Right, rect.Bottom - length);
        }

        public static void DrawGrid(Graphics g, RectangleF rect, Color colour, float spacing, float offset)
        {
            Pen pen = Pen(colour, 1f);
            float startX = rect.Left + (offset % spacing);
            for (float x = startX; x < rect.Right; x += spacing)
            {
                g.DrawLine(pen, x, rect.Top, x, rect.Bottom);
            }
            float startY = rect.Top + (offset % spacing);
            for (float y = startY; y < rect.Bottom; y += spacing)
            {
                g.DrawLine(pen, rect.Left, y, rect.Right, y);
            }
        }

        public static void ClearCache()
        {
            foreach (SolidBrush brush in BrushCache.Values)
            {
                try { brush.Dispose(); } catch { }
            }
            BrushCache.Clear();
            foreach (Pen pen in PenCache.Values)
            {
                try { pen.Dispose(); } catch { }
            }
            PenCache.Clear();
        }
    }
}
