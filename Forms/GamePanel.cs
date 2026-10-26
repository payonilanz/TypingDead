using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Effects;
using TypingDead.Game;
using TypingDead.Rendering;
using TypingDead.Systems;
using TypingDead.UI;

namespace TypingDead.Forms
{
    /// <summary>
    /// Gameplay screen/controller. It owns the one gameplay loop, routes
    /// keyboard commands and asks the renderer to paint. Game rules remain in
    /// GameManager; this panel never edits lives or score directly.
    /// </summary>
    public sealed class GamePanel : VirtualScreenPanel
    {
        private readonly GameRenderer _renderer;
        private readonly EffectManager _effects;
        private readonly AudioManager _audio;
        private GameManager _game;
        private GameSettings _settings;
        private GameDifficulty _lastDifficulty;
        private bool _sessionActive;
        private bool _endRaised;
        private int _pauseSelection;
        private int _pauseHover = -1;

        public event EventHandler<GameEndedEventArgs> GameEnded;
        public event EventHandler SettingsRequested;
        public event EventHandler MainMenuRequested;
        public event EventHandler TutorialRequested;

        public GameManager Game
        {
            get { return _game; }
        }

        public GamePanel(AudioManager audio)
        {
            _audio = audio;
            _renderer = new GameRenderer();
            _effects = new EffectManager();
            _settings = GameSettings.CreateDefault();
            _pauseSelection = 0;
            BackColor = Palette.Background;
            Cursor = Cursors.Default;
        }

        public void StartGame(GameDifficulty difficulty, GameSettings settings)
        {
            _settings = settings ?? GameSettings.CreateDefault();
            _lastDifficulty = difficulty;
            if (_game != null)
            {
                _game.GameEnded -= GameEndedHandler;
            }
            _effects.Reset();
            _effects.ApplySettings(_settings);
            _game = new GameManager(difficulty, _settings, _audio, _effects);
            _game.GameEnded += GameEndedHandler;
            _sessionActive = true;
            _endRaised = false;
            _pauseSelection = 0;
            _pauseHover = -1;
            Focus();
        }

        public void ApplySettings(GameSettings settings)
        {
            _settings = settings ?? GameSettings.CreateDefault();
            _effects.ApplySettings(_settings);
        }

        public void RestartGame()
        {
            StartGame(_lastDifficulty, _settings);
        }

        public void StopSession()
        {
            _sessionActive = false;
        }

        public bool IsSessionActive
        {
            get { return _sessionActive; }
        }

        public bool IsPaused
        {
            get { return _game != null && _game.State == GameState.Paused; }
        }

        public bool PauseForOverlay()
        {
            if (_game == null) return false;
            if (_game.State == GameState.Paused) return true;
            return _game.Pause();
        }

        public Bitmap CaptureSnapshot()
        {
            if (_game == null) return null;
            return _renderer.CaptureFrame(_game, _effects, _settings, AnimationTime);
        }

        protected override void OnFrame(float deltaTime)
        {
            if (_game == null || !_sessionActive) return;
            if (_game.State != GameState.Paused)
            {
                _game.Update(deltaTime);
                _effects.Update(deltaTime, _settings);
            }
        }

        protected override void Render(Graphics g, float time)
        {
            if (_game == null)
            {
                DrawingHelper.DrawCentered(g, "LOADING THE STREETS...",
                    UiFonts.Mono(16f, FontStyle.Bold), Palette.SecondaryText,
                    new RectangleF(0f, 0f, GameConfig.VirtualWidth, GameConfig.VirtualHeight));
                return;
            }
            _renderer.Render(g, _game, _effects, _settings, time, _pauseSelection, _pauseHover, false);
        }

        private void GameEndedHandler(object sender, GameEndedEventArgs e)
        {
            if (_endRaised) return;
            _endRaised = true;
            _sessionActive = false;
            if (GameEnded != null)
            {
                GameEnded(this, e);
            }
        }

        public bool HandleKey(Keys key)
        {
            if (_game == null || !_sessionActive) return false;

            if (_game.State == GameState.Paused)
            {
                if (key == Keys.Escape)
                {
                    ResumeFromPause();
                    return true;
                }
                if (key == Keys.Up)
                {
                    _pauseSelection = (_pauseSelection + PauseItems - 1) % PauseItems;
                    _audio.Play(GameSound.MenuHover);
                    return true;
                }
                if (key == Keys.Down)
                {
                    _pauseSelection = (_pauseSelection + 1) % PauseItems;
                    _audio.Play(GameSound.MenuHover);
                    return true;
                }
                if (key == Keys.Enter || key == Keys.Space)
                {
                    ActivatePauseSelection();
                    return true;
                }
                if (key == Keys.F1)
                {
                    RaiseTutorial();
                    return true;
                }
                if (key == Keys.R)
                {
                    _audio.Play(GameSound.MenuSelect);
                    RestartGame();
                    return true;
                }
                if (key == Keys.S)
                {
                    if (SettingsRequested != null) SettingsRequested(this, EventArgs.Empty);
                    return true;
                }
                if (key == Keys.M)
                {
                    if (MainMenuRequested != null) MainMenuRequested(this, EventArgs.Empty);
                    return true;
                }
                return false;
            }

            if (_game.State != GameState.Playing)
            {
                return false;
            }

            // P can be typed while a target exists. Used in a safe lane it opens
            // the pause menu, which keeps words such as PLAGUE possible.
            if (key == Keys.P && _game.Player.Target == null)
            {
                _game.Pause();
                _pauseSelection = 0;
                return true;
            }

            InputCommand command = InputHandler.MapGameKey(key);
            switch (command.Action)
            {
                case GameAction.MoveUp:
                    _game.MoveLane(-1);
                    return true;
                case GameAction.MoveDown:
                    _game.MoveLane(1);
                    return true;
                case GameAction.Backspace:
                    _game.Backspace();
                    return true;
                case GameAction.Pause:
                    if (_game.TogglePause())
                    {
                        _pauseSelection = 0;
                        return true;
                    }
                    return false;
                case GameAction.Tutorial:
                    RaiseTutorial();
                    return true;
                case GameAction.Letter:
                    _game.SubmitCharacter(command.Letter);
                    return true;
                case GameAction.Submit:
                    _game.SubmitWord();
                    return true;
                default:
                    return false;
            }
        }

        private void RaiseTutorial()
        {
            if (_game.State != GameState.Playing && _game.State != GameState.Paused) return;
            if (_game.State == GameState.Playing) _game.Pause();
            if (TutorialRequested != null) TutorialRequested(this, EventArgs.Empty);
        }

        private void ResumeFromPause()
        {
            if (_game.Resume())
            {
                _pauseSelection = 0;
            }
        }

        private const int PauseItems = 4;

        private void ActivatePauseSelection()
        {
            _audio.Play(GameSound.MenuSelect);
            switch (_pauseSelection)
            {
                case 0:
                    ResumeFromPause();
                    break;
                case 1:
                    RestartGame();
                    break;
                case 2:
                    if (SettingsRequested != null) SettingsRequested(this, EventArgs.Empty);
                    break;
                case 3:
                    if (MainMenuRequested != null) MainMenuRequested(this, EventArgs.Empty);
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_game == null || _game.State != GameState.Paused) return;
            PointF point = ToVirtual(e.Location);
            int hover = HitPauseButton(point);
            if (hover != _pauseHover)
            {
                _pauseHover = hover;
                Cursor = hover >= 0 ? Cursors.Hand : Cursors.Default;
                if (hover >= 0) _audio.Play(GameSound.MenuHover);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _pauseHover = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_game == null || _game.State != GameState.Paused || e.Button != MouseButtons.Left) return;
            PointF point = ToVirtual(e.Location);
            int hit = HitPauseButton(point);
            if (hit >= 0)
            {
                _pauseSelection = hit;
                ActivatePauseSelection();
            }
        }

        private int HitPauseButton(PointF point)
        {
            for (int i = 0; i < GameRenderer.PauseButtonBounds.Length; i++)
            {
                if (GameRenderer.PauseButtonBounds[i].Contains(point)) return i;
            }
            return -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
