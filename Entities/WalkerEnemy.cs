using System;
using System.Drawing;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    /// <summary>Slow standard infected. One correct submitted word defeats it.</summary>
    public sealed class WalkerEnemy : Enemy
    {
        public override EnemyType Type { get { return EnemyType.Walker; } }
        public override Color AccentColor { get { return Palette.ZombieSkin; } }
        public override string Codename { get { return "WALKER"; } }

        public WalkerEnemy(int lane, string word)
            : base(lane, word)
        {
        }

        protected override void DrawBody(Graphics g, float time, float alpha)
        {
            int frame = AnimFrame(time, 4.2f, Id, 2);
            float step = frame == 0 ? -3f : 3f;
            Color dark = Fade(Palette.Shadow, alpha);
            Color skin = Fade(Palette.ZombieSkin, alpha);
            Color skinDark = Fade(Palette.ZombieSkinDark, alpha);
            Color shirt = Fade(Palette.HumanBlue, alpha);
            Color shirtDark = Fade(Color.FromArgb(52, 78, 103), alpha);
            Color blood = Fade(Palette.Blood, alpha);

            // Legs and boots.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-10 + step, 10, 9, 24));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-8 + step, 22, 7, 9));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(4 - step, 10, 9, 24));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(5 - step, 24, 7, 9));

            // Torn shirt and exposed skin.
            g.FillRectangle(DrawingHelper.Brush(shirtDark), Px(-16, -14, 31, 25));
            g.FillRectangle(DrawingHelper.Brush(shirt), Px(-13, -12, 24, 20));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(-5, -2, 8, 4));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(9, 2, 7, 6));

            // Outstretched arms.
            g.FillRectangle(DrawingHelper.Brush(shirtDark), Px(-22, -9, 12, 7));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-34, -8, 15, 6));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-37, -5, 6, 5));
            g.FillRectangle(DrawingHelper.Brush(shirt), Px(14, -7, 8, 6));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(20, -4, 8, 5));

            // Blocky head with one bright eye.
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-13, -31, 22, 18));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-11, -29, 18, 14));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-8, -25, 5, 4));
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), Px(-7, -24, 3, 2));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(4, -24, 3, 4));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(-2, -16, 7, 2));

            // A few crisp pixels imply ragged clothes without visual noise.
            g.FillRectangle(DrawingHelper.Brush(shirtDark), Px(-17, 9, 7, 4));
            g.FillRectangle(DrawingHelper.Brush(shirtDark), Px(9, 7, 6, 5));
        }
    }
}
