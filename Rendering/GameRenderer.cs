using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using TypingDead.Effects;
using TypingDead.Entities;
using TypingDead.Game;
using TypingDead.UI;

namespace TypingDead.Rendering
{
    /// <summary>
    /// Draws the pixel battlefield, HUD, typing console and pause overlay.
    /// It reads GameManager only. No score, life, attack or timer is changed
    /// inside a paint method.
    /// </summary>
    public sealed class GameRenderer : IDisposable
    {
        public static readonly string[] PauseButtonLabels = new string[]
        {
            "RESUME", "RESTART", "SETTINGS", "MAIN MENU"
        };

        public static readonly string[] PauseButtonHints = new string[]
        {
            "ESC", "R", "S", "M"
        };

        public static readonly RectangleF[] PauseButtonBounds = new RectangleF[]
        {
            new RectangleF(430f, 324f, 420f, 48f),
            new RectangleF(430f, 382f, 420f, 48f),
            new RectangleF(430f, 440f, 420f, 48f),
            new RectangleF(430f, 498f, 420f, 48f)
        };

        public void Dispose()
        {
        }

        public Bitmap CaptureFrame(GameManager game, EffectManager effects,
            GameSettings settings, float time)
        {
            Bitmap bitmap = new Bitmap(GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                Render(g, game, effects, settings, time, 0, -1, true);
            }
            return bitmap;
        }

        public void Render(Graphics g, GameManager game, EffectManager effects,
            GameSettings settings, float time, int pauseSelection, int pauseHover, bool capture)
        {
            PixelBackground.Draw(g, time * 0.7f, 0.75f);
            DrawBattlefieldBackdrop(g, game, time);

            PointF shake = effects == null ? PointF.Empty : effects.GetShakeOffset();
            GraphicsState world = g.Save();
            g.TranslateTransform((float)Math.Round(shake.X), (float)Math.Round(shake.Y));
            DrawLaneGuides(g, game, time);
            DrawEnemies(g, game, time);
            game.Player.Draw(g, time);

            if (effects != null)
            {
                effects.DrawCharge(g);
                for (int i = 0; i < game.Projectiles.Count; i++)
                {
                    game.Projectiles[i].Draw(g, time);
                }
                effects.DrawParticles(g);
                effects.DrawWorldEffects(g);
            }

            DrawEnemyTags(g, game, time);

            if (game.Player.Target != null)
            {
                DrawTargetBrackets(g, game.Player.Target, time);
            }
            g.Restore(world);

            DrawHud(g, game, time);
            DrawTypingConsole(g, game, time);

            if (game.State == GameState.Paused)
            {
                DrawPauseOverlay(g, game, time, pauseSelection, pauseHover);
            }
            else if (game.State == GameState.ModeComplete || game.State == GameState.Victory)
            {
                DrawEndBanner(g, game, time, true);
            }
            else if (game.State == GameState.GameOver)
            {
                DrawEndBanner(g, game, time, false);
            }

            if (effects != null)
            {
                effects.DrawScreenEffects(g);
            }
        }

        private void DrawBattlefieldBackdrop(Graphics g, GameManager game, float time)
        {
            // Road and lane bands occupy exactly the three configured rows.
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Background, 215)),
                0f, GameConfig.BattlefieldTop, GameConfig.VirtualWidth,
                GameConfig.BattlefieldBottom - GameConfig.BattlefieldTop);

            for (int lane = 0; lane < GameConfig.LaneCount; lane++)
            {
                float y = GameConfig.LaneTop(lane);
                Color band = lane % 2 == 0
                    ? Color.FromArgb(32, 34, 43)
                    : Color.FromArgb(27, 29, 38);
                g.FillRectangle(DrawingHelper.Brush(band), 0f, y,
                    GameConfig.VirtualWidth, GameConfig.LaneHeight);

                bool selected = lane == game.Player.Lane;
                if (selected)
                {
                    g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(19, Palette.Success)),
                        0f, y, GameConfig.VirtualWidth, GameConfig.LaneHeight);
                }

                // Broken lane divider.
                float lineY = y + GameConfig.LaneHeight - 2f;
                for (int x = 22; x < GameConfig.VirtualWidth; x += 28)
                {
                    g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(55, 58, 67)),
                        x, lineY, 14f, 2f);
                }
            }

            // Sidewalk strips frame the road.
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(48, 50, 57)),
                0f, GameConfig.BattlefieldTop, 118f, GameConfig.BattlefieldBottom - GameConfig.BattlefieldTop);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(31, 33, 41)),
                112f, GameConfig.BattlefieldTop, 8f, GameConfig.BattlefieldBottom - GameConfig.BattlefieldTop);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(48, 50, 57)),
                GameConfig.VirtualWidth - 120f, GameConfig.BattlefieldTop, 120f,
                GameConfig.BattlefieldBottom - GameConfig.BattlefieldTop);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(31, 33, 41)),
                GameConfig.VirtualWidth - 120f, GameConfig.BattlefieldTop, 8f,
                GameConfig.BattlefieldBottom - GameConfig.BattlefieldTop);

            // Pixel cracks and scattered debris.
            for (int i = 0; i < 22; i++)
            {
                float x = 150f + (i * 97f) % 980f;
                float y = 130f + (i * 53f) % 390f;
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(55, 54, 58)), x, y, 20f, 2f);
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(55, 54, 58)), x + 16f, y + 2f, 2f, 8f);
            }

            // Danger boundary is a clear vertical stripe, not a smooth glow.
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Blood, 38)),
                GameConfig.CollisionX - 12f, GameConfig.BattlefieldTop, 18f,
                GameConfig.BattlefieldBottom - GameConfig.BattlefieldTop);
            for (int y = (int)GameConfig.BattlefieldTop; y < (int)GameConfig.BattlefieldBottom; y += 14)
            {
                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Blood, 150)),
                    GameConfig.CollisionX - 2f, y, 4f, 8f);
            }
        }

        private void DrawLaneGuides(Graphics g, GameManager game, float time)
        {
            for (int lane = 0; lane < GameConfig.LaneCount; lane++)
            {
                float y = GameConfig.LaneCenter(lane);
                bool selected = lane == game.Player.Lane;
                DrawingHelper.DrawText(g, "0" + (lane + 1).ToString(),
                    UiFonts.Mono(10f, FontStyle.Bold),
                    selected ? Palette.Success : Palette.SecondaryText,
                    new RectangleF(18f, y - 15f, 44f, 30f),
                    StringAlignment.Center, StringAlignment.Center);

                if (selected)
                {
                    for (int x = 75; x < 200; x += 18)
                    {
                        g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Success, 80)),
                            x, y - 1f, 10f, 2f);
                    }
                }
            }

            // Zone label.
            DrawingHelper.DrawText(g, "ESCAPE LINE", UiFonts.Mono(7f, FontStyle.Bold),
                Palette.Alpha(Palette.Blood, 200), new RectangleF(154f, GameConfig.BattlefieldTop + 4f, 90f, 12f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawEnemies(Graphics g, GameManager game, float time)
        {
            for (int i = 0; i < game.Enemies.Count; i++)
            {
                Enemy enemy = game.Enemies[i];
                if (enemy.State == EnemyState.Dead) continue;
                enemy.Draw(g, time);
            }
        }

        private void DrawEnemyTags(Graphics g, GameManager game, float time)
        {
            for (int i = 0; i < game.Enemies.Count; i++)
            {
                Enemy enemy = game.Enemies[i];
                if (enemy.State == EnemyState.Dead) continue;

                string word = enemy.Word;
                bool target = ReferenceEquals(game.Player.Target, enemy);
                Color accent = target ? Palette.Success : enemy.AccentColor;
                Font font = UiFonts.Mono(enemy.Type == EnemyType.Tank ? 14f : 13f, FontStyle.Bold);
                float width = Math.Max(70f, DrawingHelper.Measure(g, word, font).Width + 24f);
                float tagY = enemy.Y - (enemy.Type == EnemyType.Tank ? 78f : 58f);
                tagY -= (enemy.Id % 2) * 18f;
                RectangleF plate = new RectangleF(
                    (float)Math.Round(enemy.VisualX - width * 0.5f),
                    (float)Math.Round(tagY + 6f), width, 28f);

                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Shadow, 230)),
                    plate.X + 3f, plate.Y + 3f, plate.Width, plate.Height);
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(245, 19, 22, 30)),
                    plate.X, plate.Y, plate.Width, plate.Height);
                g.DrawRectangle(DrawingHelper.Pen(accent, target ? 2f : 1f),
                    plate.X, plate.Y, plate.Width, plate.Height);
                DrawingHelper.DrawCentered(g, word, font, target ? Palette.PrimaryText : Palette.Alpha(Palette.PrimaryText, 220), plate);

                if (enemy.MaxHealth > 1)
                {
                    DrawEnemySegments(g, enemy, plate, accent);
                }
            }
        }

        private void DrawEnemySegments(Graphics g, Enemy enemy, RectangleF plate, Color accent)
        {
            int segments = enemy.MaxHealth;
            float total = segments * 17f + (segments - 1) * 4f;
            float startX = plate.X + (plate.Width - total) * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                Color c = i < enemy.Health ? accent : Palette.Alpha(Palette.SecondaryText, 80);
                g.FillRectangle(DrawingHelper.Brush(c), startX + i * 21f, plate.Y - 9f, 17f, 5f);
            }
            string progress = "HIT " + (enemy.MaxHealth - enemy.Health).ToString() + "/" +
                enemy.MaxHealth.ToString();
            DrawingHelper.DrawText(g, progress, UiFonts.Mono(6.5f, FontStyle.Bold), accent,
                new RectangleF(plate.X, plate.Y - 25f, plate.Width, 12f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawTargetBrackets(Graphics g, Enemy target, float time)
        {
            float x = target.VisualX;
            float y = target.Y;
            float halfWidth = target.Type == EnemyType.Tank ? 52f : 38f;
            float top = y - (target.Type == EnemyType.Tank ? 60f : 48f);
            float bottom = y + 44f;
            int pulse = ((int)(time * 6f)) % 2;
            float length = 13f + pulse * 3f;
            Pen pen = DrawingHelper.Pen(Palette.Success, 2f);

            RectangleF box = new RectangleF(x - halfWidth, top, halfWidth * 2f, bottom - top);
            g.DrawLine(pen, box.Left, box.Top, box.Left + length, box.Top);
            g.DrawLine(pen, box.Left, box.Top, box.Left, box.Top + length);
            g.DrawLine(pen, box.Right, box.Top, box.Right - length, box.Top);
            g.DrawLine(pen, box.Right, box.Top, box.Right, box.Top + length);
            g.DrawLine(pen, box.Left, box.Bottom, box.Left + length, box.Bottom);
            g.DrawLine(pen, box.Left, box.Bottom, box.Left, box.Bottom - length);
            g.DrawLine(pen, box.Right, box.Bottom, box.Right - length, box.Bottom);
            g.DrawLine(pen, box.Right, box.Bottom, box.Right, box.Bottom - length);
        }

        private void DrawHud(Graphics g, GameManager game, float time)
        {
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), 0f, 0f, GameConfig.VirtualWidth, GameConfig.HudHeight);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(23, 27, 36)), 0f, 0f,
                GameConfig.VirtualWidth, GameConfig.HudHeight - 4f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Success), 0f, GameConfig.HudHeight - 4f,
                GameConfig.VirtualWidth, 4f);

            Color diff = DifficultyColour(game.Difficulty);

            // Mode block.
            g.FillRectangle(DrawingHelper.Brush(Palette.NightSky), 18f, 14f, 232f, 62f);
            g.DrawRectangle(DrawingHelper.Pen(diff, 2f), 18f, 14f, 232f, 62f);
            DrawingHelper.DrawText(g, "MODE", UiFonts.Mono(7f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(32f, 18f, 190f, 12f), StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, difficultyName(game), UiFonts.Display(20f, FontStyle.Bold), diff,
                new RectangleF(32f, 31f, 205f, 28f), StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, "ONE RUN", UiFonts.Mono(6.5f, FontStyle.Regular),
                Palette.SecondaryText, new RectangleF(32f, 59f, 205f, 12f),
                StringAlignment.Near, StringAlignment.Center);

            // Score block with segmented progress bar.
            g.FillRectangle(DrawingHelper.Brush(Palette.NightSky), 266f, 14f, 338f, 62f);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Success, 145), 1f), 266f, 14f, 338f, 62f);
            DrawingHelper.DrawText(g, "SCORE", UiFonts.Mono(7f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(280f, 18f, 90f, 12f), StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, game.Score.ToString("000") + " / 100", UiFonts.Mono(19f, FontStyle.Bold),
                game.ScoreFlash > 0.05f ? Palette.Warning : Palette.PrimaryText,
                new RectangleF(280f, 30f, 210f, 26f), StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, game.Kills.ToString("00") + " KILLS", UiFonts.Mono(7f, FontStyle.Regular),
                Palette.SecondaryText, new RectangleF(492f, 21f, 98f, 14f),
                StringAlignment.Far, StringAlignment.Center);

            RectangleF bar = new RectangleF(280f, 60f, 307f, 9f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), bar.X, bar.Y, bar.Width, bar.Height);
            for (int i = 0; i < 20; i++)
            {
                float segmentX = bar.X + i * 15.6f;
                if ((float)i / 20f < game.ScoreProgress)
                {
                    g.FillRectangle(DrawingHelper.Brush(Palette.Success), segmentX, bar.Y, 13f, 9f);
                }
                else
                {
                    g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(61, 65, 74)), segmentX, bar.Y, 13f, 9f);
                }
            }

            // Lives block.
            g.FillRectangle(DrawingHelper.Brush(Palette.NightSky), 620f, 14f, 206f, 62f);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Blood, 150), 1f), 620f, 14f, 206f, 62f);
            DrawingHelper.DrawText(g, "LIVES", UiFonts.Mono(7f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(634f, 18f, 90f, 12f), StringAlignment.Near, StringAlignment.Center);
            DrawingHelper.DrawText(g, game.Lives.ToString() + " / 3", UiFonts.Mono(20f, FontStyle.Bold),
                game.Lives <= 1 ? Palette.Blood : Palette.PrimaryText,
                new RectangleF(634f, 31f, 92f, 27f), StringAlignment.Near, StringAlignment.Center);
            for (int i = 0; i < 3; i++)
            {
                DrawLifeIcon(g, 742f + i * 24f, 37f, i < game.Lives, time);
            }
            DrawingHelper.DrawText(g, "BREACH OR WRONG WORD", UiFonts.Mono(6.2f, FontStyle.Regular),
                Palette.SecondaryText, new RectangleF(632f, 59f, 180f, 12f),
                StringAlignment.Near, StringAlignment.Center);

            // Target summary block.
            g.FillRectangle(DrawingHelper.Brush(Palette.NightSky), 842f, 14f, 420f, 62f);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Alpha(Palette.Success, 120), 1f), 842f, 14f, 420f, 62f);
            Enemy target = game.Player.Target;
            DrawingHelper.DrawText(g, "TARGET", UiFonts.Mono(7f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(856f, 18f, 190f, 12f), StringAlignment.Near, StringAlignment.Center);
            string targetName = target == null ? "NO TARGET" :
                (target.Codename + " // LANE 0" + (target.Lane + 1));
            string targetWord = target == null ? "---" : target.Word;
            DrawingHelper.DrawText(g, targetName, UiFonts.Mono(8f, FontStyle.Bold),
                target == null ? Palette.SecondaryText : target.AccentColor,
                new RectangleF(856f, 31f, 240f, 18f), StringAlignment.Near, StringAlignment.Center);
            float targetWordSize = targetWord.Length > 13 ? 10f :
                (targetWord.Length > 10 ? 12f : 14f);
            DrawingHelper.DrawText(g, targetWord, UiFonts.Mono(targetWordSize, FontStyle.Bold), Palette.PrimaryText,
                new RectangleF(1034f, 28f, 214f, 24f), StringAlignment.Far, StringAlignment.Center);
            if (target != null && target.MaxHealth > 1)
            {
                DrawingHelper.DrawText(g, "SEGMENTS " + target.Health.ToString() + "/" +
                    target.MaxHealth.ToString() + " // WORD " + (target.WordIndex + 1).ToString() + "/" +
                    target.WordCount.ToString(), UiFonts.Mono(6.5f, FontStyle.Bold), Palette.Warning,
                    new RectangleF(856f, 57f, 390f, 13f), StringAlignment.Near, StringAlignment.Center);
            }
            else
            {
                DrawingHelper.DrawText(g, "ENTER ATTACKS // UP/DOWN MOVES",
                    UiFonts.Mono(6.5f, FontStyle.Regular), Palette.SecondaryText,
                    new RectangleF(856f, 57f, 390f, 13f), StringAlignment.Near, StringAlignment.Center);
            }
        }

        private void DrawLifeIcon(Graphics g, float x, float y, bool active, float time)
        {
            Color c = active ? Palette.Blood : Color.FromArgb(67, 52, 58);
            g.FillRectangle(DrawingHelper.Brush(c), x, y, 9f, 9f);
            g.FillRectangle(DrawingHelper.Brush(c), x + 8f, y, 9f, 9f);
            g.FillRectangle(DrawingHelper.Brush(c), x + 3f, y + 8f, 12f, 5f);
            g.FillRectangle(DrawingHelper.Brush(c), x + 6f, y + 12f, 6f, 4f);
        }

        private void DrawTypingConsole(Graphics g, GameManager game, float time)
        {
            float top = GameConfig.BattlefieldBottom;
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), 0f, top,
                GameConfig.VirtualWidth, GameConfig.VirtualHeight - top);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(21, 24, 32)), 0f, top + 4f,
                GameConfig.VirtualWidth, GameConfig.VirtualHeight - top - 4f);
            Color border = game.LifeFlash > 0.05f ? Palette.Blood :
                (game.Player.IsInputMatch ? Palette.Warning : Palette.Success);
            g.FillRectangle(DrawingHelper.Brush(border), 0f, top, GameConfig.VirtualWidth, 4f);

            Enemy target = game.Player.Target;
            string targetLabel = target == null
                ? "TARGET // NONE"
                : "TARGET // " + target.Codename + " // LANE 0" + (target.Lane + 1);
            DrawingHelper.DrawText(g, targetLabel, UiFonts.Mono(8f, FontStyle.Bold),
                target == null ? Palette.SecondaryText : target.AccentColor,
                new RectangleF(30f, top + 13f, 460f, 15f), StringAlignment.Near, StringAlignment.Center);

            if (target != null && target.MaxHealth > 1)
            {
                DrawingHelper.DrawText(g, "WORD " + (target.WordIndex + 1).ToString() + "/" +
                    target.WordCount.ToString() + "  //  HITS " + (target.MaxHealth - target.Health).ToString() +
                    "/" + target.MaxHealth.ToString(), UiFonts.Mono(7f, FontStyle.Bold), Palette.Warning,
                    new RectangleF(460f, top + 13f, 340f, 15f), StringAlignment.Near, StringAlignment.Center);
            }

            string word = target == null ? "---" : target.Word;
            string typed = game.Player.TypedInput;
            int boxSize = target == null ? 30 : Math.Max(22, Math.Min(34, 420 / Math.Max(1, word.Length)));
            float wordX = 34f;
            float wordY = top + 36f;
            for (int i = 0; i < word.Length; i++)
            {
                char letter = word[i];
                bool typedHere = i < typed.Length;
                bool correct = typedHere && typed[i] == letter;
                bool wrong = typedHere && typed[i] != letter;
                Color fill = wrong ? Color.FromArgb(118, 40, 47) :
                    (correct ? Color.FromArgb(44, 74, 51) : Color.FromArgb(31, 35, 44));
                Color borderColor = wrong ? Palette.Blood :
                    (correct ? Palette.Success : Palette.Alpha(Palette.SecondaryText, 90));
                RectangleF box = new RectangleF(wordX + i * (boxSize + 4f), wordY, boxSize, boxSize);
                g.FillRectangle(DrawingHelper.Brush(fill), box.X, box.Y, box.Width, box.Height);
                g.DrawRectangle(DrawingHelper.Pen(borderColor, correct || wrong ? 2f : 1f),
                    box.X, box.Y, box.Width, box.Height);
                if (letter == ' ')
                {
                    float barWidth = box.Width * 0.44f;
                    g.FillRectangle(DrawingHelper.Brush(
                            wrong ? Palette.PrimaryText : (correct ? Palette.PrimaryText : Palette.Alpha(Palette.PrimaryText, 130))),
                        box.X + (box.Width - barWidth) * 0.5f, box.Y + box.Height * 0.66f,
                        barWidth, Math.Max(2f, box.Height * 0.1f));
                }
                else
                {
                    DrawingHelper.DrawCentered(g, letter.ToString(),
                        UiFonts.Mono(boxSize * 0.52f, FontStyle.Bold),
                        wrong ? Palette.PrimaryText : (correct ? Palette.PrimaryText : Palette.Alpha(Palette.PrimaryText, 160)), box);
                }
            }

            // Input preview and instruction occupy the right half.
            DrawingHelper.DrawText(g, "INPUT", UiFonts.Mono(7f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(690f, top + 35f, 58f, 15f), StringAlignment.Near, StringAlignment.Center);
            string typedDisplay = typed.Length == 0 ? "..." : typed.Replace(' ', '_');
            DrawingHelper.DrawText(g, typedDisplay, UiFonts.Mono(15f, FontStyle.Bold),
                typed.Length == 0 ? Palette.SecondaryText : Palette.PrimaryText,
                new RectangleF(748f, top + 31f, 360f, 24f), StringAlignment.Near, StringAlignment.Center);

            string instruction;
            Color instructionColour;
            if (target == null)
            {
                instruction = "NO TARGET // MOVE UP/DOWN";
                instructionColour = Palette.SecondaryText;
            }
            else if (game.Player.IsInputMatch)
            {
                instruction = "WORD READY: PRESS ENTER";
                instructionColour = Palette.Warning;
            }
            else
            {
                instruction = "TYPE THE WORD AND PRESS ENTER TO ATTACK";
                instructionColour = Palette.Success;
            }
            DrawingHelper.DrawText(g, instruction, UiFonts.Mono(8f, FontStyle.Bold), instructionColour,
                new RectangleF(690f, top + 60f, 560f, 17f), StringAlignment.Near, StringAlignment.Center);

            float statusAlpha = game.StatusAlpha;
            if (statusAlpha > 0.02f)
            {
                DrawingHelper.DrawText(g, game.StatusMessage, UiFonts.Mono(8f, FontStyle.Bold),
                    Palette.Alpha(game.StatusColour, (int)(255 * statusAlpha)),
                    new RectangleF(30f, top + 91f, 1220f, 17f),
                    StringAlignment.Center, StringAlignment.Center);
            }
            else
            {
                DrawingHelper.DrawText(g, "UP/DOWN MOVE     A-Z + SPACE TYPE     ENTER ATTACK     BACKSPACE EDIT     ESC PAUSE",
                    UiFonts.Mono(7f, FontStyle.Regular), Palette.SecondaryText,
                    new RectangleF(30f, top + 91f, 1220f, 17f),
                    StringAlignment.Center, StringAlignment.Center);
            }

            // Bottom hint strip.
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), 0f, 694f, GameConfig.VirtualWidth, 26f);
            DrawingHelper.DrawText(g, "WRONG KEY: SAFE   //   WRONG WORD: -1 LIFE   //   EMPTY ENTER: SAFE",
                UiFonts.Mono(7f, FontStyle.Bold), Palette.Alpha(Palette.SecondaryText, 185),
                new RectangleF(30f, 697f, 1220f, 18f), StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawPauseOverlay(Graphics g, GameManager game, float time, int selection, int hover)
        {
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(196, 8, 10, 15)),
                0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            RectangleF panel = new RectangleF(356f, 156f, 568f, 436f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), panel.X + 8f, panel.Y + 8f, panel.Width, panel.Height);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(240, 21, 25, 34)), panel.X, panel.Y, panel.Width, panel.Height);
            g.DrawRectangle(DrawingHelper.Pen(Palette.Success, 2f), panel.X, panel.Y, panel.Width, panel.Height);

            DrawingHelper.DrawText(g, "PAUSED", UiFonts.Display(34f, FontStyle.Bold), Palette.PrimaryText,
                new RectangleF(panel.X, panel.Y + 22f, panel.Width, 45f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, "TAKE A BREATH",
                UiFonts.Mono(8f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(panel.X, panel.Y + 68f, panel.Width, 16f),
                StringAlignment.Center, StringAlignment.Center);

            DrawingHelper.DrawText(g, "MODE " + DifficultySettings.GetDisplayName(game.Difficulty) +
                "     SCORE " + game.Score.ToString("000") + "/100     LIVES " + game.Lives.ToString() + "/3",
                UiFonts.Mono(8f, FontStyle.Bold), Palette.Warning,
                new RectangleF(panel.X, panel.Y + 92f, panel.Width, 18f),
                StringAlignment.Center, StringAlignment.Center);

            for (int i = 0; i < PauseButtonBounds.Length; i++)
            {
                NeonAction action = new NeonAction(PauseButtonLabels[i], PauseButtonBounds[i]);
                action.Hint = PauseButtonHints[i];
                action.Primary = i == 0;
                action.Accent = i == 3 ? Palette.Blood : (i == 2 ? Palette.Warning : Palette.Success);
                action.Draw(g, selection == i, hover == i, false, time);
            }

            DrawingHelper.DrawText(g, "UP/DOWN SELECT     ENTER CONFIRM     ESC RESUME",
                UiFonts.Mono(7f, FontStyle.Regular), Palette.SecondaryText,
                new RectangleF(panel.X, panel.Bottom - 27f, panel.Width, 16f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private void DrawEndBanner(Graphics g, GameManager game, float time, bool success)
        {
            int alpha = 100 + (int)(20 * Math.Sin(time * 4f));
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(alpha, 8, 10, 15)),
                0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            RectangleF panel = new RectangleF(380f, 292f, 520f, 118f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), panel.X + 6f, panel.Y + 6f, panel.Width, panel.Height);
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(240, 20, 24, 32)), panel.X, panel.Y, panel.Width, panel.Height);
            Color accent = success ? Palette.Success : Palette.Blood;
            g.DrawRectangle(DrawingHelper.Pen(accent, 2f), panel.X, panel.Y, panel.Width, panel.Height);
            string title = success
                ? (game.Difficulty == GameDifficulty.Hard ? "FINAL VICTORY" : "MODE COMPLETE")
                : "GAME OVER";
            DrawingHelper.DrawText(g, title, UiFonts.Display(25f, FontStyle.Bold), accent,
                new RectangleF(panel.X, panel.Y + 18f, panel.Width, 40f),
                StringAlignment.Center, StringAlignment.Center);
            DrawingHelper.DrawText(g, success ? "SCORE " + game.Score.ToString("000") + " / 100" :
                "OUT OF LIVES", UiFonts.Mono(8f, FontStyle.Bold), Palette.SecondaryText,
                new RectangleF(panel.X, panel.Y + 64f, panel.Width, 18f),
                StringAlignment.Center, StringAlignment.Center);
        }

        private static Color DifficultyColour(GameDifficulty difficulty)
        {
            if (difficulty == GameDifficulty.Easy) return Palette.Success;
            if (difficulty == GameDifficulty.Medium) return Palette.Warning;
            return Palette.Blood;
        }

        private static string difficultyName(GameManager game)
        {
            return DifficultySettings.GetDisplayName(game.Difficulty);
        }
    }
}
