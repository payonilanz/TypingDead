using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.UI;

namespace TypingDead.Entities
{
    public enum TypingResult
    {
        NoTarget,
        Accepted,
        WrongKey,
        Blocked
    }

    /// <summary>
    /// The human survivor. Encapsulates lane movement, the selected target, the
    /// typed buffer and visual state. Individual keys can be wrong without
    /// costing a life; only GameManager can resolve an Enter submission.
    /// </summary>
    public sealed class Player
    {
        private int _lane;
        private float _visualY;
        private float _moveBob;
        private float _attackPulse;
        private float _damagePulse;
        private string _typedInput;
        private Enemy _target;
        private char _lastWrongCharacter;
        private float _wrongFlash;

        public int Lane
        {
            get { return _lane; }
        }

        public Enemy Target
        {
            get { return _target; }
        }

        public string TypedInput
        {
            get { return _typedInput; }
        }

        public char LastWrongCharacter
        {
            get { return _lastWrongCharacter; }
        }

        public float WrongFlash
        {
            get { return _wrongFlash; }
        }

        public float AttackPulse
        {
            get { return _attackPulse; }
        }

        public float DamagePulse
        {
            get { return _damagePulse; }
        }

        public float VisualY
        {
            get { return _visualY; }
        }

        public int CorrectPrefixLength
        {
            get
            {
                if (_target == null) return 0;
                int count = Math.Min(_typedInput.Length, _target.Word.Length);
                int matched = 0;
                for (int i = 0; i < count; i++)
                {
                    if (_typedInput[i] != _target.Word[i]) break;
                    matched++;
                }
                return matched;
            }
        }

        public bool IsInputMatch
        {
            get
            {
                return _target != null &&
                       _typedInput.Length > 0 &&
                       String.Equals(_typedInput, _target.Word, StringComparison.Ordinal);
            }
        }

        public float ChargeProgress
        {
            get
            {
                if (_target == null || _target.Word.Length == 0) return 0f;
                return Math.Max(0f, Math.Min(1f, (float)CorrectPrefixLength / _target.Word.Length));
            }
        }

        public bool HasTarget
        {
            get { return _target != null && _target.IsTargetable; }
        }

        public PointF MuzzlePosition
        {
            get { return new PointF(GameConfig.PlayerX + 58f, _visualY - 9f + _moveBob); }
        }

        public Player()
        {
            _typedInput = String.Empty;
            Reset();
        }

        public void Reset()
        {
            _lane = 1;
            _visualY = GameConfig.LaneCenter(_lane);
            _moveBob = 0f;
            _attackPulse = 0f;
            _damagePulse = 0f;
            _wrongFlash = 0f;
            _lastWrongCharacter = '\0';
            _typedInput = String.Empty;
            _target = null;
        }

        /// <summary>
        /// Moves to another lane. This method never submits a word, attacks or
        /// changes any zombie. Target selection is handled by GameManager.
        /// </summary>
        public bool SetLane(int lane)
        {
            if (lane < 0) lane = 0;
            if (lane >= GameConfig.LaneCount) lane = GameConfig.LaneCount - 1;
            if (lane == _lane) return false;

            _lane = lane;
            return true;
        }

        public void Update(float deltaTime)
        {
            float targetY = GameConfig.LaneCenter(_lane);
            float smoothing = 1f - (float)Math.Pow(0.00005f, deltaTime);
            _visualY += (targetY - _visualY) * smoothing;
            _moveBob = (float)Math.Sin(Environment.TickCount * 0.0045) * 1.2f;
            _attackPulse = Math.Max(0f, _attackPulse - deltaTime * 4.2f);
            _damagePulse = Math.Max(0f, _damagePulse - deltaTime * 2.6f);
            _wrongFlash = Math.Max(0f, _wrongFlash - deltaTime * 3.8f);
        }

        /// <summary>Assigns a new target and clears stale typing when it changes.</summary>
        public bool AssignTarget(Enemy enemy)
        {
            if (enemy != null && (!enemy.IsTargetable || enemy.Lane != _lane))
            {
                enemy = null;
            }

            if (_target == enemy)
            {
                return false;
            }

            _target = enemy;
            ClearTyping();
            _lastWrongCharacter = '\0';
            return true;
        }

        public void ClearTarget()
        {
            _target = null;
            ClearTyping();
        }

        public void ValidateTarget()
        {
            if (_target != null && (!_target.IsTargetable || _target.Lane != _lane))
            {
                _target = null;
                ClearTyping();
            }
        }

        public TypingResult SubmitCharacter(char character)
        {
            if (_target == null || !_target.IsTargetable)
            {
                return TypingResult.NoTarget;
            }

            char typed = Char.ToUpperInvariant(character);
            if (typed != ' ' && (typed < 'A' || typed > 'Z'))
            {
                return TypingResult.Blocked;
            }

            // A small overflow allowance lets the HUD show a visibly wrong
            // input instead of silently swallowing extra keys.
            if (_typedInput.Length >= _target.Word.Length + 4)
            {
                return TypingResult.Blocked;
            }

            char expected = _typedInput.Length < _target.Word.Length
                ? _target.Word[_typedInput.Length]
                : '\0';
            _typedInput += typed;

            if (typed == expected)
            {
                return TypingResult.Accepted;
            }

            _lastWrongCharacter = typed;
            _wrongFlash = 1f;
            return TypingResult.WrongKey;
        }

        public bool Backspace()
        {
            if (_typedInput.Length <= 0) return false;
            _typedInput = _typedInput.Substring(0, _typedInput.Length - 1);
            return true;
        }

        public void ClearTyping()
        {
            _typedInput = String.Empty;
        }

        public void TriggerAttack()
        {
            _attackPulse = 1f;
            ClearTyping();
        }

        public void TriggerDamage()
        {
            _damagePulse = 1f;
        }

        public void FlashWrongKey()
        {
            _wrongFlash = 1f;
        }

        public void Draw(Graphics g, float time)
        {
            int frame = ((int)(time * 3.4f)) % 2;
            float bob = frame == 0 ? 0f : -1f;
            float attackOffset = _attackPulse * 9f;
            float damageShake = _damagePulse > 0f ? (float)Math.Sin(time * 70f) * _damagePulse * 3f : 0f;

            // Blocky shadow and lane platform.
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(120, 8, 11, 18)),
                GameConfig.PlayerX - 38f, _visualY + 34f + bob, 82f, 8f);
            g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Success, 55)),
                GameConfig.PlayerX - 44f, _visualY + 43f + bob, 94f, 2f);

            GraphicsState state = g.Save();
            g.TranslateTransform((float)Math.Round(GameConfig.PlayerX + damageShake),
                (float)Math.Round(_visualY + bob));

            Color outline = _damagePulse > 0.1f ? Palette.Blood : Palette.Shadow;
            Color jacket = _damagePulse > 0.1f ? Palette.Blood : Palette.HumanBlue;
            Color jacketDark = _damagePulse > 0.1f ? Color.FromArgb(132, 43, 52) : Color.FromArgb(51, 77, 104);
            Color skin = Palette.PrimaryText;
            Color pants = Color.FromArgb(56, 63, 76);
            Color cap = Color.FromArgb(42, 46, 55);

            // Back leg, front leg and boots.
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-16, 8, 12, 28));
            g.FillRectangle(DrawingHelper.Brush(pants), Px(-14, 9, 8, 22));
            g.FillRectangle(DrawingHelper.Brush(outline), Px(8, 8, 12, 28));
            g.FillRectangle(DrawingHelper.Brush(pants), Px(10, 9, 8, 22));
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-18, 30, 15, 6));
            g.FillRectangle(DrawingHelper.Brush(outline), Px(7, 30, 15, 6));

            // Backpack.
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-26, -14, 14, 26));
            g.FillRectangle(DrawingHelper.Brush(Palette.Grime), Px(-24, -12, 10, 20));
            g.FillRectangle(DrawingHelper.Brush(Palette.Warning), Px(-23, -8, 3, 9));

            // Torso and jacket panels.
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-16, -17, 34, 29));
            g.FillRectangle(DrawingHelper.Brush(jacket), Px(-14, -15, 30, 25));
            g.FillRectangle(DrawingHelper.Brush(jacketDark), Px(-12, -14, 9, 22));
            g.FillRectangle(DrawingHelper.Brush(Palette.Success), Px(2, -10, 3, 8));
            g.FillRectangle(DrawingHelper.Brush(Palette.Blood), Px(6, 4, 6, 3));

            // Arms and the survivor's improvised blaster.
            g.FillRectangle(DrawingHelper.Brush(outline), Px(12, -8, 20 + attackOffset, 10));
            g.FillRectangle(DrawingHelper.Brush(jacket), Px(14, -6, 15 + attackOffset, 6));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(27 + attackOffset, -5, 7, 5));
            g.FillRectangle(DrawingHelper.Brush(outline), Px(30 + attackOffset, -11, 27, 8));
            g.FillRectangle(DrawingHelper.Brush(Palette.Concrete), Px(32 + attackOffset, -9, 22, 4));
            g.FillRectangle(DrawingHelper.Brush(Palette.Warning), Px(52 + attackOffset, -10, 7, 5));
            g.FillRectangle(DrawingHelper.Brush(Palette.PrimaryText), Px(54 + attackOffset, -9, 3, 3));
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-20, -6, 10, 18));
            g.FillRectangle(DrawingHelper.Brush(jacketDark), Px(-18, -4, 6, 13));

            // Head, ear and cap.
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-11, -35, 23, 20));
            g.FillRectangle(DrawingHelper.Brush(skin), Px(-9, -33, 19, 16));
            g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(217, 190, 162)), Px(-5, -31, 4, 7));
            g.FillRectangle(DrawingHelper.Brush(cap), Px(-13, -41, 26, 9));
            g.FillRectangle(DrawingHelper.Brush(outline), Px(-15, -38, 10, 4));
            g.FillRectangle(DrawingHelper.Brush(Palette.Shadow), Px(3, -28, 6, 4));
            g.FillRectangle(DrawingHelper.Brush(Palette.PrimaryText), Px(7, -28, 3, 2));

            // Damage flash overlay, still built from rectangles.
            if (_damagePulse > 0.02f)
            {
                int alpha = (int)(80 * _damagePulse);
                g.FillRectangle(DrawingHelper.Brush(Color.FromArgb(alpha, Palette.Blood)),
                    -30, -45, 92, 82);
            }

            g.Restore(state);

            // Wrong-keystroke marker near the survivor.
            if (_wrongFlash > 0.05f && _lastWrongCharacter != '\0')
            {
                RectangleF badge = new RectangleF(GameConfig.PlayerX + 16f, _visualY - 62f, 30f, 24f);
                g.FillRectangle(DrawingHelper.Brush(Palette.Alpha(Palette.Blood, (int)(180 * _wrongFlash))),
                    badge.X, badge.Y, badge.Width, badge.Height);
                g.DrawRectangle(DrawingHelper.Pen(Palette.PrimaryText, 1f),
                    badge.X, badge.Y, badge.Width, badge.Height);
                DrawingHelper.DrawCentered(g, "X", UiFonts.Mono(13f, FontStyle.Bold), Palette.PrimaryText, badge);
            }
        }

        private static RectangleF Px(float x, float y, float width, float height)
        {
            return new RectangleF((float)Math.Round(x), (float)Math.Round(y),
                (float)Math.Round(width), (float)Math.Round(height));
        }
    }
}
