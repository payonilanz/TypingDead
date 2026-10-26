using System;
using System.Drawing;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    /// <summary>
    /// Fast, lean infected. A genuine speed threat in every mode, but always a
    /// single-hit enemy: one word and one health segment, including in Hard.
    /// </summary>
    public sealed class RunnerEnemy : Enemy
    {
        public override EnemyType Type { get { return EnemyType.Runner; } }
        public override Color AccentColor { get { return Palette.Warning; } }
        public override string Codename { get { return "RUNNER"; } }

        protected override float BodyScale
        {
            get { return 0.94f; }
        }

        public RunnerEnemy(int lane, string word)
            : base(lane, word)
        {
        }

        protected override float GetCurrentSpeed(float terminalSpeed)
        {
            // Runners are deliberately a little quicker than the mode's base
            // speed. This override is one of the real polymorphism examples.
            return terminalSpeed * 1.05f;
        }

        protected override void DrawBody(Graphics g, float time, float alpha)
        {
            int frame = AnimFrame(time, 10f, Id, 2);
            float step = frame == 0 ? -7f : 7f;
            Color dark = Fade(Palette.Shadow, alpha);
            Color skin = Fade(Palette.ZombieSkin, alpha);
            Color skinDark = Fade(Palette.ZombieSkinDark, alpha);
            Color shirt = Fade(Palette.Blood, alpha);
            Color shirtDark = Fade(Color.FromArgb(120, 42, 48), alpha);
            Color blood = Fade(Palette.Blood, alpha);

            // Long, stretched running legs.
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-7 + step, 8, 7, 28));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-10 + step, 29, 10, 6));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(6 - step, 10, 7, 25));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(8 - step, 28, 10, 6));

            // Forward-leaning torso.
            g.FillRectangle(DrawingHelper.Brush(shirtDark), Px(-15, -17, 29, 28));
            g.FillRectangle(DrawingHelper.Brush(shirt), Px(-12, -15, 22, 20));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-4, -4, 12, 3));

            // Arms swept back; the long claws are just a few bright pixels.
            g.FillRectangle(DrawingHelper.Brush(skin), Px(10, -10, 17, 6));
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(23, -7, 7, 5));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-24, -8, 13, 6));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(26, -8, 4, 2));

            // Narrow head, one eye, open jaw.
            g.FillRectangle(DrawingHelper.Brush(skinDark), Px(-18, -35, 24, 19));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-16, -33, 20, 15));
            g.FillRectangle(DrawingHelper.Brush(dark), Px(-13, -29, 6, 4));
            g.FillRectangle(DrawingHelper.Brush(Palette.Warning), Px(-12, -28, 4, 2));
            g.FillRectangle(DrawingHelper.Brush(blood), Px(-7, -20, 9, 3));

            // Motion dust pixels.
            if (!SettingsReduced(time))
            {
                for (int i = 0; i < 3; i++)
                {
                    g.FillRectangle(DrawingHelper.Brush(Fade(Palette.SecondaryText, alpha * 0.35f)),
                        Px(26 + i * 8, 4 + (i % 2) * 5, 5, 3));
                }
            }
        }

        private static bool SettingsReduced(float time)
        {
            return ((int)(time * 12f)) % 7 == 0;
        }
    }
}
