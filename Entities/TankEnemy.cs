using System;
using System.Collections.Generic;
using System.Drawing;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    /// <summary>
    /// Heavy tank zombie. It requires exactly three different correctly
    /// submitted words. Every hit removes one health segment and advances to
    /// the next word; the kill reward is only possible after the third hit.
    /// </summary>
    public sealed class TankEnemy : Enemy
    {
        public override EnemyType Type { get { return EnemyType.Tank; } }
        public override Color AccentColor { get { return Palette.Warning; } }
        public override string Codename { get { return "TANK"; } }

        protected override float BodyScale
        {
            get { return 1.18f; }
        }

        protected override float DeathDuration
        {
            get { return 0.92f; }
        }

        public TankEnemy(int lane, IList<string> words)
            : base(lane, words)
        {
            if (words == null || words.Count != 3)
            {
                throw new ArgumentException("A Tank Zombie needs exactly three words.", "words");
            }
        }

        protected override float GetCurrentSpeed(float terminalSpeed)
        {
            return terminalSpeed * 0.94f;
        }

        protected override void DrawBody(Graphics g, float time, float alpha)
        {
            int frame = AnimFrame(time, 2.8f, Id, 2);
            float step = frame == 0 ? -2f : 2f;
            Color dark = Fade(Palette.Shadow, alpha);
            Color skin = Fade(Palette.ZombieSkinDark, alpha);
            Color skinLight = Fade(Palette.ZombieSkin, alpha);
            Color pants = Fade(Color.FromArgb(63, 71, 55), alpha);
            Color blood = Fade(Palette.Blood, alpha);

            // Massive feet and legs.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-24, 12, 20, 24));
            g.FillRectangle(DrawingHelper.Brush(pants), Px(-22 + step, 22, 19, 15));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(7, 12, 20, 24));
            g.FillRectangle(DrawingHelper.Brush(pants), Px(6 - step, 24, 19, 14));

            // Huge torso and shoulder blocks.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-31, -20, 61, 38));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-28, -18, 54, 33));
            g.FillRectangle(DrawingHelper.Brush(skinLight), Px(-24, -15, 19, 12));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(-4, -2, 14, 6));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(-20, 8, 8, 3));

            // Thick arms reaching for the survivor.
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-43, -9, 17, 12));
            g.FillRectangle(DrawingHelper.Brush(skinLight), Px(-51, -5, 10, 8));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(25, -8, 15, 11));
            g.FillRectangle(DrawingHelper.Brush(skinLight), Px(35, -4, 8, 7));

            // Small head sunk into the shoulders.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-14, -37, 28, 19));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-11, -35, 22, 15));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-8, -31, 6, 4));
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), Px(-7, -30, 4, 2));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(3, -30, 5, 4));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(-4, -23, 11, 3));

            // Three health pips are part of the sprite as a second visual cue.
            for (int i = 0; i < 3; i++)
            {
                Color pip = i < Health ? Palette.Warning : Palette.Shadow;
                g.FillRectangle(DrawingHelper.Brush(Fade(pip, alpha)), Px(-16 + i * 12, -43, 9, 4));
            }

            // Heavy stitches: short pixel lines, never smooth curves.
            g.FillRectangle(DrawingHelper.Brush(skinLight), Px(-25, -11, 4, 13));
            g.FillRectangle(DrawingHelper.Brush(skinLight), Px(20, -14, 3, 15));
        }
    }
}
