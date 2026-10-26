using System;
using System.Collections.Generic;
using System.Drawing;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    /// <summary>
    /// Rare mutated zombie found in Hard mode. It is faster than a Runner for
    /// short bursts and survives two long words. Its glowing eyes make it easy
    /// to identify as the highest-priority threat.
    /// </summary>
    public sealed class SpecialZombie : Enemy
    {
        public override EnemyType Type { get { return EnemyType.Special; } }
        public override Color AccentColor { get { return Palette.Mutation; } }
        public override string Codename { get { return "SPECIAL"; } }

        protected override float BodyScale
        {
            get { return 1.08f; }
        }

        protected override float DeathDuration
        {
            get { return 0.86f; }
        }

        public SpecialZombie(int lane, IList<string> words)
            : base(lane, words)
        {
            if (words == null || words.Count != 2)
            {
                throw new ArgumentException("The Special Zombie needs exactly two words.", "words");
            }
        }

        protected override float GetCurrentSpeed(float terminalSpeed)
        {
            // Bounded pulsing movement is threatening but readable.
            float pulse = 1f + 0.12f * (float)Math.Sin(Age * 2.6f + Id);
            return terminalSpeed * pulse;
        }

        protected override void DrawBody(Graphics g, float time, float alpha)
        {
            int frame = AnimFrame(time, 8.5f, Id, 2);
            float step = frame == 0 ? -5f : 5f;
            Color dark = Fade(Palette.Shadow, alpha);
            Color skin = Fade(Palette.Mutation, alpha);
            Color skinDark = Fade(Color.FromArgb(69, 48, 86), alpha);
            Color eye = Fade(Palette.Warning, alpha);

            // Twisted legs.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-14 + step, 8, 8, 27));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-17 + step, 28, 11, 7));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(7 - step, 10, 8, 25));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(9 - step, 27, 11, 7));

            // Uneven mutated torso.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-19, -20, 39, 34));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-16, -18, 32, 28));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-11, -6, 12, 12));
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), Px(3, 0, 8, 4));

            // Long claws and shoulder spurs.
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-34, -10, 17, 8));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-43, -7, 11, 5));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(17, -13, 18, 8));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(31, -10, 9, 4));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-19, -30, 6, 12));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(14, -31, 6, 11));

            // Large split head with glowing pixel eyes.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-18, -43, 34, 24));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-15, -40, 28, 18));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-7, -36, 6, 5));
            g.FillRectangle(DrawingHelper.Brush(eye), Px(-6, -35, 4, 3));
            g.FillRectangle(DrawingHelper.Brush(eye), Px(7, -36, 4, 3));
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), Px(-5, -29, 13, 4));

            // Mutation vents.
            g.FillRectangle(DrawingHelper.Brush(Fade(Palette.Mutation, alpha * 0.55f)),
                Px(-11, -48, 4, 5));
            g.FillRectangle(DrawingHelper.Brush(Fade(Palette.Mutation, alpha * 0.35f)),
                Px(8, -46, 4, 6));
        }
    }
}
