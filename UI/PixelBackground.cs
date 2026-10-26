using System;
using System.Drawing;
using TypingDead.Game;

namespace TypingDead.UI
{
    /// <summary>
    /// Hand-drawn pixel-art apocalypse backdrop. Everything is built from
    /// integer-aligned rectangles so it stays crisp at every window size.
    /// </summary>
    public static class PixelBackground
    {
        public static void Draw(Graphics g, float time, float intensity)
        {
            if (intensity < 0f) intensity = 0f;
            if (intensity > 1f) intensity = 1f;

            g.FillRectangle(DrawingBrush(Palette.Background), 0f, 0f,
                GameConfig.VirtualWidth, GameConfig.VirtualHeight);

            // Banded night sky.
            g.FillRectangle(DrawingBrush(Palette.NightSky), 0f, 0f, GameConfig.VirtualWidth, 220f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(44, 50, 65)), 0f, 70f, GameConfig.VirtualWidth, 90f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(31, 36, 49)), 0f, 160f, GameConfig.VirtualWidth, 100f);

            DrawMoon(g);
            DrawClouds(g, time);
            DrawSkyline(g, time);
            DrawRoad(g);
            DrawRuinedBuildings(g, time);
            DrawFenceAndBarricades(g);
            DrawAsh(g, time);

            // Slight darkness vignette assembled from four opaque bands.
            g.FillRectangle(DrawingBrush(Color.FromArgb((int)(45 * intensity), 5, 6, 9)),
                0f, 0f, 150f, GameConfig.VirtualHeight);
            g.FillRectangle(DrawingBrush(Color.FromArgb((int)(45 * intensity), 5, 6, 9)),
                GameConfig.VirtualWidth - 150f, 0f, 150f, GameConfig.VirtualHeight);
            g.FillRectangle(DrawingBrush(Color.FromArgb((int)(35 * intensity), 5, 6, 9)),
                0f, 0f, GameConfig.VirtualWidth, 46f);
            g.FillRectangle(DrawingBrush(Color.FromArgb((int)(48 * intensity), 5, 6, 9)),
                0f, GameConfig.VirtualHeight - 62f, GameConfig.VirtualWidth, 62f);
        }

        private static void DrawMoon(Graphics g)
        {
            float x = 1004f;
            float y = 78f;
            g.FillRectangle(DrawingBrush(Palette.Alpha(Palette.Warning, 28)), x - 16f, y - 16f, 88f, 88f);
            g.FillRectangle(DrawingBrush(Palette.Alpha(Palette.Warning, 48)), x - 8f, y - 8f, 72f, 72f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(226, 214, 178)), x, y, 56f, 56f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(210, 195, 158)), x + 8f, y + 8f, 12f, 12f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(207, 193, 155)), x + 34f, y + 28f, 14f, 10f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(198, 184, 150)), x + 18f, y + 40f, 10f, 8f);

            // Pixel clouds crossing the moon.
            g.FillRectangle(DrawingBrush(Color.FromArgb(65, 37, 43, 58)), 902f, 120f, 260f, 10f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(52, 37, 43, 58)), 946f, 130f, 200f, 8f);
        }

        private static void DrawClouds(Graphics g, float time)
        {
            float drift = (time * 7f) % 1400f;
            for (int i = 0; i < 5; i++)
            {
                float x = (i * 310f + drift) % 1500f - 160f;
                float y = 54f + (i % 3) * 39f;
                g.FillRectangle(DrawingBrush(Color.FromArgb(36, 22, 26, 38)), x, y, 190f, 14f);
                g.FillRectangle(DrawingBrush(Color.FromArgb(26, 22, 26, 38)), x + 30f, y + 14f, 120f, 8f);
            }
        }

        private static void DrawSkyline(Graphics g, float time)
        {
            // Two layers of broken city silhouettes.
            g.FillRectangle(DrawingBrush(Color.FromArgb(26, 29, 39)), 0f, 228f, 1280f, 44f);
            DrawBuilding(g, 30f, 150f, 150f, 110f, Color.FromArgb(20, 23, 31));
            DrawBuilding(g, 196f, 190f, 104f, 80f, Color.FromArgb(18, 21, 29));
            DrawBuilding(g, 318f, 156f, 138f, 124f, Color.FromArgb(23, 26, 35));
            DrawBuilding(g, 474f, 208f, 94f, 58f, Color.FromArgb(19, 22, 30));
            DrawBuilding(g, 586f, 176f, 126f, 96f, Color.FromArgb(24, 27, 36));
            DrawBuilding(g, 732f, 200f, 86f, 68f, Color.FromArgb(18, 21, 29));
            DrawBuilding(g, 836f, 146f, 150f, 120f, Color.FromArgb(22, 25, 34));
            DrawBuilding(g, 1006f, 190f, 104f, 76f, Color.FromArgb(18, 21, 29));
            DrawBuilding(g, 1124f, 162f, 132f, 112f, Color.FromArgb(22, 25, 34));

            // A single unstable street lamp, animated with a discrete flicker.
            float x = 904f;
            g.FillRectangle(DrawingBrush(Palette.Shadow), x, 158f, 5f, 84f);
            g.FillRectangle(DrawingBrush(Palette.Shadow), x - 22f, 156f, 27f, 5f);
            bool on = ((int)(time * 7f)) % 9 != 0;
            g.FillRectangle(DrawingBrush(on ? Palette.Alpha(Palette.Warning, 180) : Palette.Alpha(Palette.Warning, 45)),
                x - 30f, 158f, 16f, 7f);
            g.FillRectangle(DrawingBrush(on ? Palette.Alpha(Palette.Warning, 26) : Palette.Alpha(Palette.Warning, 8)),
                x - 49f, 165f, 55f, 20f);
        }

        private static void DrawBuilding(Graphics g, float x, float y, float width, float height, Color colour)
        {
            g.FillRectangle(DrawingBrush(colour), x, y, width, height);
            g.FillRectangle(DrawingBrush(Color.FromArgb(13, 15, 21)), x, y, width, 5f);

            // Jagged broken roofline.
            for (int i = 0; i < width; i += 18)
            {
                float step = ((int)(x + i) % 3) * 4f;
                g.FillRectangle(DrawingBrush(Palette.Alpha(Palette.Background, 220)),
                    x + i, y - step, 10f, step + 2f);
            }

            // Sparse windows; most are dark, a few are sickly amber.
            for (int wy = 18; wy < height - 10; wy += 24)
            {
                for (int wx = 14; wx < width - 12; wx += 26)
                {
                    bool lit = ((wx + wy + (int)x) % 7 == 0);
                    Color c = lit ? Color.FromArgb(58, 48, 31) : Color.FromArgb(13, 16, 22);
                    g.FillRectangle(DrawingBrush(c), x + wx, y + wy, 9f, 11f);
                }
            }
        }

        private static void DrawRoad(Graphics g)
        {
            g.FillRectangle(DrawingBrush(Color.FromArgb(44, 45, 51)), 0f, 300f, 1280f, 264f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(34, 35, 40)), 0f, 448f, 1280f, 116f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(21, 23, 29)), 0f, 560f, 1280f, 160f);

            // Broken sidewalk and road cracks.
            g.FillRectangle(DrawingBrush(Color.FromArgb(62, 62, 66)), 0f, 292f, 1280f, 12f);
            for (int i = 0; i < 18; i++)
            {
                float x = 40f + i * 72f;
                float y = 336f + (i % 4) * 54f;
                g.FillRectangle(DrawingBrush(Color.FromArgb(53, 53, 59)), x, y, 34f, 3f);
                g.FillRectangle(DrawingBrush(Color.FromArgb(53, 53, 59)), x + 22f, y + 3f, 3f, 11f);
            }

            // Faded road stripe.
            for (int i = 0; i < 13; i++)
            {
                g.FillRectangle(DrawingBrush(Color.FromArgb(61, 56, 43)), 40f + i * 100f, 424f, 54f, 5f);
            }
        }

        private static void DrawRuinedBuildings(Graphics g, float time)
        {
            // Large foreground ruins on both sides frame the battlefield.
            g.FillRectangle(DrawingBrush(Color.FromArgb(18, 20, 27)), 0f, 312f, 92f, 252f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(22, 24, 31)), 92f, 338f, 36f, 226f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(32, 35, 42)), 0f, 306f, 110f, 10f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(67, 49, 35)), 18f, 360f, 19f, 30f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(13, 15, 20)), 24f, 368f, 7f, 17f);

            g.FillRectangle(DrawingBrush(Color.FromArgb(18, 20, 27)), 1188f, 318f, 92f, 246f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(24, 26, 33)), 1152f, 344f, 36f, 220f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(33, 35, 42)), 1150f, 340f, 130f, 11f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(64, 46, 33)), 1222f, 374f, 20f, 32f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(14, 16, 21)), 1228f, 382f, 8f, 18f);

            // Flickering fire barrel in the right ruin.
            float flicker = ((int)(time * 9f) % 4) * 3f;
            g.FillRectangle(DrawingBrush(Color.FromArgb(70, 47, 34)), 1206f, 500f, 18f, 22f);
            g.FillRectangle(DrawingBrush(Palette.Blood), 1209f, 490f - flicker, 9f, 12f + flicker);
            g.FillRectangle(DrawingBrush(Palette.Warning), 1211f, 494f - flicker, 4f, 8f);
        }

        private static void DrawFenceAndBarricades(Graphics g)
        {
            // Broken chain fence across the back.
            for (int i = 0; i < 26; i++)
            {
                float x = 120f + i * 38f;
                g.FillRectangle(DrawingBrush(Color.FromArgb(50, 52, 57)), x, 274f, 2f, 24f);
            }
            g.FillRectangle(DrawingBrush(Color.FromArgb(50, 52, 57)), 120f, 283f, 906f, 2f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(50, 52, 57)), 120f, 292f, 906f, 2f);

            // Wooden barricades at the road edge.
            DrawBarricade(g, 300f, 526f);
            DrawBarricade(g, 700f, 541f);
            DrawBarricade(g, 1010f, 530f);
        }

        private static void DrawBarricade(Graphics g, float x, float y)
        {
            g.FillRectangle(DrawingBrush(Color.FromArgb(61, 45, 33)), x, y, 86f, 9f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(48, 35, 26)), x + 7f, y + 9f, 7f, 22f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(48, 35, 26)), x + 68f, y + 9f, 7f, 22f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(84, 64, 45)), x + 11f, y + 2f, 22f, 4f);
            g.FillRectangle(DrawingBrush(Color.FromArgb(84, 64, 45)), x + 50f, y + 2f, 16f, 4f);
        }

        private static void DrawAsh(Graphics g, float time)
        {
            // Deterministic drifting pixels, so no random state leaks into paint.
            for (int i = 0; i < 38; i++)
            {
                float x = (i * 131f + time * (5f + (i % 4) * 2f)) % 1280f;
                float y = 80f + ((i * 73f + time * (8f + i % 5)) % 520f);
                int alpha = 24 + (i % 3) * 13;
                g.FillRectangle(DrawingBrush(Color.FromArgb(alpha, Palette.SecondaryText)),
                    (float)Math.Round(x), (float)Math.Round(y), 2f, 2f);
            }
        }

        private static SolidBrush DrawingBrush(Color colour)
        {
            return Rendering.DrawingHelper.Brush(colour);
        }
    }
}
