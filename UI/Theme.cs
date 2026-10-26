using System;
using System.Collections.Generic;
using System.Drawing;

namespace TypingDead.UI
{
    /// <summary>
    /// Central palette for the pixel-art apocalypse. The old field names are
    /// kept as aliases so existing screens share the same limited colour set.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Background = Color.FromArgb(23, 25, 35);      // #171923
        public static readonly Color Shadow = Color.FromArgb(11, 13, 18);          // #0B0D12
        public static readonly Color NightSky = Color.FromArgb(37, 43, 58);        // #252B3A
        public static readonly Color Grime = Color.FromArgb(52, 75, 54);           // #344B36
        public static readonly Color ZombieSkin = Color.FromArgb(113, 155, 98);    // #719B62
        public static readonly Color ZombieSkinDark = Color.FromArgb(66, 96, 68);  // #426044
        public static readonly Color HumanBlue = Color.FromArgb(73, 106, 141);      // #496A8D
        public static readonly Color Blood = Color.FromArgb(201, 62, 79);          // #C93E4F
        public static readonly Color Warning = Color.FromArgb(229, 180, 84);       // #E5B454
        public static readonly Color Success = Color.FromArgb(156, 203, 114);      // #9CCB72
        public static readonly Color PrimaryText = Color.FromArgb(242, 235, 221);  // #F2EBDD
        public static readonly Color SecondaryText = Color.FromArgb(169, 172, 182); // #A9ACB6
        public static readonly Color Mutation = Color.FromArgb(137, 94, 159);
        public static readonly Color Concrete = Color.FromArgb(65, 67, 71);

        // Compatibility aliases used by menus, effects and the HUD.
        public static readonly Color VoidBlack = Shadow;
        public static readonly Color NearBlack = Color.FromArgb(8, 9, 13);
        public static readonly Color DeepNavy = Background;
        public static readonly Color DarkSlate = NightSky;
        public static readonly Color SteelBlue = Grime;
        public static readonly Color NeonCyan = Success;
        public static readonly Color ElectricPurple = ZombieSkinDark;
        public static readonly Color ToxicGreen = Success;
        public static readonly Color Amber = Warning;
        public static readonly Color DangerRed = Blood;
        public static readonly Color FrostWhite = PrimaryText;
        public static readonly Color MutedBlue = SecondaryText;
        public static readonly Color DimCyan = Grime;
        public static readonly Color Magenta = Mutation;

        public static Color Alpha(Color colour, int alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            return Color.FromArgb(alpha, colour.R, colour.G, colour.B);
        }

        public static Color Blend(Color a, Color b, float amount)
        {
            if (amount < 0f) amount = 0f;
            if (amount > 1f) amount = 1f;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }
    }

    /// <summary>
    /// Creates and caches the fonts used by the interface. Monospace fonts are
    /// preferred for the typing console because character boxes must line up.
    /// </summary>
    public static class UiFonts
    {
        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();
        private static readonly string DisplayFamily;
        private static readonly string BodyFamily;
        private static readonly string MonoFamily;

        static UiFonts()
        {
            DisplayFamily = FindFamily(new string[] { "Consolas", "Cascadia Mono", "Lucida Console", "Courier New" });
            BodyFamily = FindFamily(new string[] { "Segoe UI", "Tahoma", "Arial" });
            MonoFamily = FindFamily(new string[] { "Consolas", "Cascadia Mono", "Courier New" });
        }

        private static string FindFamily(string[] candidates)
        {
            HashSet<string> installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (FontFamily family in FontFamily.Families)
                {
                    installed.Add(family.Name);
                }
            }
            catch
            {
                // Font enumeration can fail on locked-down machines. Defaults follow.
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (installed.Contains(candidates[i]))
                {
                    return candidates[i];
                }
            }
            return "Arial";
        }

        public static Font Display(float size, FontStyle style)
        {
            return Get(DisplayFamily, size, style);
        }

        public static Font Body(float size, FontStyle style)
        {
            return Get(BodyFamily, size, style);
        }

        public static Font Mono(float size, FontStyle style)
        {
            return Get(MonoFamily, size, style);
        }

        private static Font Get(string family, float size, FontStyle style)
        {
            float rounded = (float)Math.Round(size * 2f) / 2f;
            string key = family + "|" + rounded.ToString("0.0") + "|" + (int)style;
            Font font;
            if (!Cache.TryGetValue(key, out font))
            {
                float pixels = rounded * (96f / 72f);
                font = new Font(family, pixels, style, GraphicsUnit.Pixel);
                Cache[key] = font;
            }
            return font;
        }

        public static void DisposeCache()
        {
            foreach (Font font in Cache.Values)
            {
                try { font.Dispose(); }
                catch { }
            }
            Cache.Clear();
        }
    }
}
