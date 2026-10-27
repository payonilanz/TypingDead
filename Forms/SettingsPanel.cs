using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Forms
{
    /// <summary>
    /// Functional settings screen. Changes are saved immediately and announced
    /// through SettingsChanged so audio and the active game can react at once.
    /// </summary>
    public sealed class SettingsPanel : VirtualScreenPanel
    {
        private const int RowCount = 3;
        private const int ItemCount = 5;

        private readonly Systems.SettingsManager _manager;
        private readonly NeonAction _reset;
        private readonly NeonAction _back;
        private readonly Font _heading;
        private readonly Font _rowTitle;
        private readonly Font _small;
        private readonly RectangleF[] _rows;
        private int _focusIndex;
        private int _hoverIndex;
        private float _feedbackTime;
        private string _feedback;
        private Color _feedbackColour;

        public event EventHandler Closed;
        public event EventHandler SettingsChanged;

        public SettingsPanel(Systems.SettingsManager manager)
        {
            _manager = manager;
            _heading = UiFonts.Display(36f, FontStyle.Bold);
            _rowTitle = UiFonts.Display(12.5f, FontStyle.Bold);
            _small = UiFonts.Mono(7.5f, FontStyle.Regular);

            _rows = new RectangleF[RowCount];
            for (int i = 0; i < RowCount; i++)
            {
                _rows[i] = new RectangleF(90f, 200f + i * 70f, 1100f, 50f);
            }

            _reset = new NeonAction("RESET DEFAULTS", new RectangleF(90f, 548f, 330f, 46f));
            _reset.Accent = Palette.Amber;
            _back = new NeonAction("BACK", new RectangleF(460f, 548f, 330f, 46f));
            _back.Primary = true;
            _back.Hint = "ESC";

            _focusIndex = 0;
            _hoverIndex = -1;
            _feedbackTime = 0f;
        }

        private GameSettings Settings
        {
            get { return _manager.Settings; }
        }

        protected override void OnFrame(float deltaTime)
        {
            if (_feedbackTime > 0f) _feedbackTime = Math.Max(0f, _feedbackTime - deltaTime);
        }

        protected override void Render(Graphics g, float time)
        {
            PixelBackground.Draw(g, time, 1f);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(150, 8, 10, 15)),
                0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);

            DrawingHelper.DrawText(g, "LOCAL PROFILE", UiFonts.Mono(8f, FontStyle.Regular),
                Palette.Alpha(Palette.Success, 190), new RectangleF(0f, 32f, GameConfig.VirtualWidth, 16f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "SETTINGS", _heading, Palette.Shadow,
                new RectangleF(4f, 56f, GameConfig.VirtualWidth, 55f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "SETTINGS", _heading, Palette.PrimaryText,
                new RectangleF(0f, 52f, GameConfig.VirtualWidth, 55f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "DISPLAY AND AUDIO",
                UiFonts.Mono(8.5f, FontStyle.Bold), Palette.Alpha(Palette.MutedBlue, 200),
                new RectangleF(0f, 108f, GameConfig.VirtualWidth, 18f),
                StringAlignment.Center, StringAlignment.Center);

            RectangleF content = new RectangleF(60f, 140f, 1160f, 480f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow),
                content.X + 6f, content.Y + 6f, content.Width, content.Height);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(236, 19, 22, 30)),
                content.X, content.Y, content.Width, content.Height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Success, 150), 2f),
                content.X, content.Y, content.Width, content.Height);
            DrawingHelper.DrawCornerBrackets(g, content, Palette.Alpha(Palette.Success, 180), 20f, 2f);

            DrawRows(g, time);
            _reset.Draw(g, _focusIndex == 3, _hoverIndex == 3, false, time);
            _back.Draw(g, _focusIndex == 4, _hoverIndex == 4, false, time);

            if (_feedbackTime > 0f && !String.IsNullOrEmpty(_feedback))
            {
                DrawingHelper.DrawText(g, _feedback, UiFonts.Mono(8f, FontStyle.Bold),
                    Palette.Alpha(_feedbackColour, (int)(230 * Math.Min(1f, _feedbackTime * 2f))),
                    new RectangleF(90f, 604f, 620f, 14f),
                    StringAlignment.Center, StringAlignment.Center);
            }
            else if (!String.IsNullOrEmpty(_manager.LastError))
            {
                DrawingHelper.DrawText(g, _manager.LastError, UiFonts.Mono(8f, FontStyle.Bold),
                    Palette.DangerRed, new RectangleF(90f, 604f, 620f, 14f),
                    StringAlignment.Center, StringAlignment.Center);
            }

            DrawingHelper.DrawText(g, "↑↓ NAVIGATE     ←→ ADJUST     ENTER TOGGLE     ESC BACK",
                UiFonts.Mono(7.5f, FontStyle.Regular), Palette.Alpha(Palette.MutedBlue, 165),
                new RectangleF(300f, 684f, 680f, 14f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawRows(Graphics g, float time)
        {
            DrawRow(g, 0, "SOUND EFFECTS", "Combat and menu sounds.",
                Settings.SoundEnabled, null, time);
            DrawRow(g, 1, "MUSIC", "Ambient loop.",
                Settings.MusicEnabled, null, time);
            DrawRow(g, 2, "GRAPHICS", "Particle density.",
                false, Settings.EffectsQuality.ToString().ToUpperInvariant(), time);
        }

        private void DrawRow(Graphics g, int index, string title, string description, bool toggleValue,
            string segmentedValue, float time)
        {
            RectangleF rect = _rows[index];
            bool focused = _focusIndex == index;
            bool hovered = _hoverIndex == index;
            Color accent = Palette.NeonCyan;
            if (index == 0 || index == 1) accent = Palette.ToxicGreen;
            if (index == 2) accent = Palette.Amber;

            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), rect.X + 4f, rect.Y + 4f, rect.Width, rect.Height);
            g.FillRectangle(DrawingHelper.Brush(focused
                    ? Color.FromArgb(238, 28, 34, 43) : Color.FromArgb(226, 20, 23, 31)),
                rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawRectangle(DrawingHelper.Pen(
                Palette.Alpha(accent, focused ? (byte)220 : (byte)(hovered ? 150 : 88)),
                focused ? 2f : 1f), rect.X, rect.Y, rect.Width, rect.Height);
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(accent, focused ? (byte)235 : (byte)105)),
                rect.X, rect.Y + 10f, 5f, rect.Height - 20f);

            DrawingHelper.DrawText(g, title, _rowTitle, focused ? Palette.FrostWhite : Color.FromArgb(220, 234, 245),
                new RectangleF(rect.X + 17f, rect.Y + 6f, 440f, 22f),
                StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, description, _small, Palette.Alpha(Palette.MutedBlue, 195),
                new RectangleF(rect.X + 17f, rect.Y + 25f, 500f, 16f),
                StringAlignment.Near, StringAlignment.Center);

            if (segmentedValue != null)
            {
                DrawQualitySelector(g, rect, accent);
            }
            else
            {
                DrawToggle(g, new RectangleF(rect.Right - 83f, rect.Y + 13f, 64f, 24f), toggleValue, accent);
                DrawingHelper.DrawText(g, toggleValue ? "ON" : "OFF",
                    UiFonts.Mono(7.5f, FontStyle.Bold),
                    toggleValue ? Palette.ToxicGreen : Palette.MutedBlue,
                    new RectangleF(rect.Right - 142f, rect.Y + 15f, 50f, 18f),
                    StringAlignment.Far, StringAlignment.Center);
            }
        }

        private void DrawToggle(Graphics g, RectangleF rect, bool value, Color accent)
        {
            Color track = value ? Color.FromArgb(105, 50, 70, 52) : Color.FromArgb(120, 45, 49, 58);
            g.FillRectangle(DrawingHelper.Brush(track), rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawRectangle(DrawingHelper.Pen(
                Palette.Alpha(value ? accent : Palette.MutedBlue, value ? (byte)220 : (byte)120),
                2f), rect.X, rect.Y, rect.Width, rect.Height);
            float knobX = value ? rect.Right - 21f : rect.X + 3f;
            Color knob = value ? accent : Palette.MutedBlue;
            g.FillRectangle(DrawingHelper.Brush(knob), knobX, rect.Y + 3f, 18f, 18f);
            g.FillRectangle(DrawingHelper.Brush(Palette.PrimaryText),
                knobX + 5f, rect.Y + 7f, 7f, 4f);
        }

        private void DrawQualitySelector(Graphics g, RectangleF row, Color accent)
        {
            RectangleF segmented = new RectangleF(row.Right - 275f, row.Y + 12f, 258f, 27f);
            DrawingHelper.FillRounded(g, segmented, Color.FromArgb(80, 6, 11, 18), 5f);
            string[] labels = new string[] { "LOW", "MED", "HIGH" };
            for (int i = 0; i < 3; i++)
            {
                RectangleF part = new RectangleF(segmented.X + i * 86f, segmented.Y, 86f, segmented.Height);
                bool active = (int)Settings.EffectsQuality == i;
                if (active)
                {
                    DrawingHelper.FillRounded(g, part, Palette.Alpha(accent, 55), 4f);
                    DrawingHelper.DrawRounded(g, part, Palette.Alpha(accent, 170), 1f, 4f);
                }
                DrawingHelper.DrawCentered(g, labels[i], UiFonts.Mono(7.5f, FontStyle.Bold),
                    active ? Palette.FrostWhite : Palette.MutedBlue, part);
            }
        }

        public bool HandleKey(Keys key)
        {
            if (key == Keys.Escape)
            {
                ClosePanel();
                return true;
            }
            if (key == Keys.Up)
            {
                _focusIndex = (_focusIndex + ItemCount - 1) % ItemCount;
                return true;
            }
            if (key == Keys.Down || key == Keys.Tab)
            {
                _focusIndex = (_focusIndex + 1) % ItemCount;
                return true;
            }
            if (key == Keys.Enter || key == Keys.Space)
            {
                ActivateItem(_focusIndex, 1);
                return true;
            }
            if (key == Keys.Left)
            {
                ActivateItem(_focusIndex, -1);
                return true;
            }
            if (key == Keys.Right)
            {
                ActivateItem(_focusIndex, 1);
                return true;
            }
            return false;
        }

        private void ActivateItem(int index, int direction)
        {
            bool changed = false;
            switch (index)
            {
                case 0:
                    Settings.SoundEnabled = direction < 0 ? false : (direction > 0 ? true : !Settings.SoundEnabled);
                    changed = true;
                    break;
                case 1:
                    Settings.MusicEnabled = direction < 0 ? false : (direction > 0 ? true : !Settings.MusicEnabled);
                    changed = true;
                    break;
                case 2:
                    int quality = (int)Settings.EffectsQuality;
                    if (direction > 0) quality = (quality + 1) % 3;
                    else quality = (quality + 2) % 3;
                    Settings.EffectsQuality = (EffectsQuality)quality;
                    changed = true;
                    break;
                case 3:
                    _manager.ResetToDefaults();
                    changed = true;
                    ShowFeedback("DEFAULTS RESTORED", Palette.ToxicGreen);
                    break;
                case 4:
                    ClosePanel();
                    break;
            }

            if (changed)
            {
                if (_manager.Save())
                {
                    ShowFeedback("SETTINGS SAVED // APPLIED", Palette.ToxicGreen);
                }
                else
                {
                    ShowFeedback("SETTINGS COULD NOT BE SAVED", Palette.DangerRed);
                }
                if (SettingsChanged != null) SettingsChanged(this, EventArgs.Empty);
            }
        }

        private void ShowFeedback(string text, Color colour)
        {
            _feedback = text;
            _feedbackColour = colour;
            _feedbackTime = 2.1f;
        }

        private void ClosePanel()
        {
            _manager.Save();
            if (Closed != null) Closed(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF point = ToVirtual(e.Location);
            int hover = -1;
            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i].Contains(point)) hover = i;
            }
            if (_reset.HitTest(point)) hover = 3;
            if (_back.HitTest(point)) hover = 4;
            if (hover != _hoverIndex)
            {
                _hoverIndex = hover;
                if (hover >= 0) _focusIndex = hover;
                Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            PointF point = ToVirtual(e.Location);
            if (_reset.HitTest(point))
            {
                ActivateItem(3, 1);
                return;
            }
            if (_back.HitTest(point))
            {
                ClosePanel();
                return;
            }
            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i].Contains(point))
                {
                    _focusIndex = i;
                    if (i == 2)
                    {
                        RectangleF segmented = new RectangleF(_rows[i].Right - 275f, _rows[i].Y + 12f, 258f, 27f);
                        int part = (int)((point.X - segmented.X) / 86f);
                        if (part < 0) part = 0;
                        if (part > 2) part = 2;
                        Settings.EffectsQuality = (EffectsQuality)part;
                        _manager.Save();
                        ShowFeedback("SETTINGS SAVED // APPLIED", Palette.ToxicGreen);
                        if (SettingsChanged != null) SettingsChanged(this, EventArgs.Empty);
                    }
                    else
                    {
                        ActivateItem(i, 0);
                    }
                    return;
                }
            }
        }
    }
}
