using System;
using System.Drawing;
using System.Windows.Forms;
using TypingDead.Game;
using TypingDead.Systems;
using TypingDead.UI;

namespace TypingDead.Forms
{
    /// <summary>
    /// Application shell. It owns the screens, shared services, progression
    /// state and every navigation decision. Panels never create each other.
    /// </summary>
    public sealed class MainForm : Form
    {
        private enum ScreenKind
        {
            Menu,
            Difficulty,
            Game,
            Tutorial,
            Settings,
            Result
        }

        private readonly SettingsManager _settingsManager;
        private readonly HighScoreManager _highScores;
        private readonly AudioManager _audio;
        private readonly ProgressionManager _progression;
        private readonly MainMenuPanel _menu;
        private readonly DifficultyPanel _difficultyPanel;
        private readonly GamePanel _game;
        private readonly TutorialPanel _tutorial;
        private readonly SettingsPanel _settings;
        private readonly ResultPanel _result;

        private ScreenKind _screen;
        private ScreenKind _overlayReturn;
        private GameDifficulty _lastDifficulty;
        private GameResult _lastResult;
        private string _difficultyNotice;
        private bool _fullscreen;
        private FormBorderStyle _savedBorder;
        private FormWindowState _savedWindowState;
        private Rectangle _savedBounds;

        public MainForm()
        {
            _settingsManager = new SettingsManager();
            _highScores = new HighScoreManager();
            _audio = new AudioManager(_settingsManager.Settings);
            _progression = new ProgressionManager();

            Text = "TYPING DEAD // Pixel Typing Survival";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(GameConfig.VirtualWidth, GameConfig.VirtualHeight);
            MinimumSize = new Size(940, 570);
            BackColor = Palette.NearBlack;
            DoubleBuffered = true;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            _lastDifficulty = GameDifficulty.Easy;

            _menu = new MainMenuPanel(_audio, _highScores);
            _difficultyPanel = new DifficultyPanel(_audio, _highScores, _progression);
            _game = new GamePanel(_audio);
            _tutorial = new TutorialPanel();
            _settings = new SettingsPanel(_settingsManager);
            _result = new ResultPanel(_audio);

            SuspendLayout();
            Panel[] screens = new Panel[]
            {
                _menu, _difficultyPanel, _game, _tutorial, _settings, _result
            };
            for (int i = 0; i < screens.Length; i++)
            {
                Panel panel = screens[i];
                panel.Dock = DockStyle.Fill;
                panel.Visible = false;
                Controls.Add(panel);
            }
            ResumeLayout(false);

            WireEvents();
        }

        private void WireEvents()
        {
            _menu.PlayRequested += MenuPlayRequested;
            _menu.TutorialRequested += MenuTutorialRequested;
            _menu.SettingsRequested += MenuSettingsRequested;
            _menu.ExitRequested += MenuExitRequested;

            _difficultyPanel.BackRequested += DifficultyBackRequested;
            _difficultyPanel.PlayRequested += DifficultyPlayRequested;

            _game.GameEnded += GameEndedHandler;
            _game.SettingsRequested += GameSettingsRequested;
            _game.MainMenuRequested += GameMainMenuRequested;
            _game.TutorialRequested += GameTutorialRequested;

            _tutorial.Closed += OverlayClosed;
            _settings.Closed += OverlayClosed;
            _settings.SettingsChanged += SettingsChangedHandler;

            _result.PrimaryRequested += ResultPrimaryRequested;
            _result.MainMenuRequested += ResultMainMenuRequested;
        }

        private void MenuPlayRequested(object sender, EventArgs e)
        {
            ShowDifficulty(String.Empty);
        }

        private void MenuTutorialRequested(object sender, EventArgs e)
        {
            ShowOverlay(ScreenKind.Tutorial, ScreenKind.Menu);
        }

        private void MenuSettingsRequested(object sender, EventArgs e)
        {
            ShowOverlay(ScreenKind.Settings, ScreenKind.Menu);
        }

        private void MenuExitRequested(object sender, EventArgs e)
        {
            Close();
        }

        private void DifficultyBackRequested(object sender, EventArgs e)
        {
            ShowScreen(ScreenKind.Menu);
        }

        private void DifficultyPlayRequested(object sender, DifficultyPlayEventArgs e)
        {
            StartNewGame(e.Difficulty);
        }

        private void GameEndedHandler(object sender, GameEndedEventArgs e)
        {
            GameResult result = e.Result;
            _lastResult = result;
            Bitmap snapshot = _game.CaptureSnapshot();
            _game.StopSession();

            bool newBest = _highScores.Report(result.Difficulty, result.Score);

            string notice;
            ResultKind kind;
            if (result.ModeCompleted)
            {
                GameDifficulty? unlocked = _progression.Complete(result.Difficulty);
                _difficultyNotice = _progression.UnlockNotice(unlocked);
                if (unlocked.HasValue)
                {
                    _lastDifficulty = unlocked.Value;
                }
                if (result.FinalVictory)
                {
                    kind = ResultKind.FinalVictory;
                    notice = "ALL MODES CLEARED // FINAL VICTORY";
                    _difficultyNotice = "ALL MODES COMPLETE";
                }
                else
                {
                    kind = ResultKind.ModeComplete;
                    notice = String.IsNullOrEmpty(_difficultyNotice)
                        ? "MODE CLEARED"
                        : _difficultyNotice;
                }
            }
            else
            {
                _progression.Fail(result.Difficulty);
                _lastDifficulty = GameDifficulty.Easy;
                _difficultyNotice = "PROGRESSION RESET // EASY IS UNLOCKED";
                kind = ResultKind.GameOver;
                notice = _difficultyNotice;
            }

            _menu.RefreshScores();
            _result.ShowResult(result, snapshot, kind, newBest, notice);
            ShowScreen(ScreenKind.Result);
        }

        private void GameSettingsRequested(object sender, EventArgs e)
        {
            ShowOverlay(ScreenKind.Settings, ScreenKind.Game);
        }

        private void GameMainMenuRequested(object sender, EventArgs e)
        {
            _game.StopSession();
            _menu.RefreshScores();
            ShowScreen(ScreenKind.Menu);
        }

        private void GameTutorialRequested(object sender, EventArgs e)
        {
            ShowOverlay(ScreenKind.Tutorial, ScreenKind.Game);
        }

        private void OverlayClosed(object sender, EventArgs e)
        {
            ScreenKind target = _overlayReturn;
            if (target == ScreenKind.Game && (_game.Game == null || !_game.IsSessionActive))
            {
                target = ScreenKind.Menu;
            }
            if (target == ScreenKind.Difficulty)
            {
                ShowDifficulty(_difficultyNotice);
                return;
            }
            ShowScreen(target);
        }

        private void SettingsChangedHandler(object sender, EventArgs e)
        {
            ApplyCurrentSettings();
        }

        private void ResultPrimaryRequested(object sender, EventArgs e)
        {
            if (_result.Kind == ResultKind.ModeComplete)
            {
                ShowDifficulty(_difficultyNotice);
                return;
            }

            if (_result.Kind == ResultKind.FinalVictory)
            {
                _progression.Reset();
                _difficultyNotice = "FRESH START // EASY IS UNLOCKED";
                ShowDifficulty(_difficultyNotice);
                return;
            }

            // Game over. Retry the failed mode only when progression still
            // allows it; otherwise the player must earn the unlock again.
            GameDifficulty difficulty = _lastResult == null
                ? GameDifficulty.Easy
                : _lastResult.Difficulty;

            if (_progression.IsUnlocked(difficulty))
            {
                StartNewGame(difficulty);
            }
            else
            {
                ShowDifficulty(_difficultyNotice);
            }
        }

        private void ResultMainMenuRequested(object sender, EventArgs e)
        {
            ShowScreen(ScreenKind.Menu);
        }

        private void StartNewGame(GameDifficulty difficulty)
        {
            if (!_progression.IsUnlocked(difficulty))
            {
                ShowDifficulty(difficulty == GameDifficulty.Medium
                    ? "MEDIUM IS LOCKED // COMPLETE EASY FIRST"
                    : "HARD IS LOCKED // COMPLETE MEDIUM FIRST");
                return;
            }

            _lastDifficulty = difficulty;
            _game.StartGame(difficulty, _settingsManager.Settings);
            ShowScreen(ScreenKind.Game);
        }

        private void ShowDifficulty(string notice)
        {
            _difficultyNotice = notice ?? String.Empty;
            _difficultyPanel.SetProgression(_progression, _difficultyNotice);
            _difficultyPanel.SelectDifficulty(_lastDifficulty);
            ShowScreen(ScreenKind.Difficulty);
        }

        private void ShowOverlay(ScreenKind overlay, ScreenKind returnTo)
        {
            if (returnTo == ScreenKind.Game)
            {
                _game.PauseForOverlay();
            }
            _overlayReturn = returnTo;
            ShowScreen(overlay);
        }

        private void ShowScreen(ScreenKind kind)
        {
            Panel target = PanelFor(kind);
            if (target == null) return;

            Panel[] all = new Panel[]
            {
                _menu, _difficultyPanel, _game, _tutorial, _settings, _result
            };
            for (int i = 0; i < all.Length; i++)
            {
                if (!ReferenceEquals(all[i], target) && all[i].Visible)
                {
                    all[i].Visible = false;
                }
            }

            target.Visible = true;
            target.BringToFront();
            target.Focus();
            _screen = kind;
            Cursor = Cursors.Default;

            if (kind == ScreenKind.Menu)
            {
                _menu.FocusMenu();
            }
            Invalidate(true);
        }

        private Panel PanelFor(ScreenKind kind)
        {
            switch (kind)
            {
                case ScreenKind.Menu: return _menu;
                case ScreenKind.Difficulty: return _difficultyPanel;
                case ScreenKind.Game: return _game;
                case ScreenKind.Tutorial: return _tutorial;
                case ScreenKind.Settings: return _settings;
                case ScreenKind.Result: return _result;
                default: return null;
            }
        }

        private void ApplyCurrentSettings()
        {
            GameSettings settings = _settingsManager.Settings;
            _audio.ApplySettings(settings);
            _audio.RefreshMusic(settings);
            _game.ApplySettings(settings);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _audio.ApplySettings(_settingsManager.Settings);
            _audio.StartMusic();
            ShowScreen(ScreenKind.Menu);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (_screen == ScreenKind.Game && _game.IsSessionActive)
            {
                _game.PauseForOverlay();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            Keys modifiers = keyData & Keys.Modifiers;

            if (key == Keys.F11 && (modifiers == Keys.None || modifiers == Keys.Shift))
            {
                ToggleFullscreen();
                return true;
            }
            if (modifiers == Keys.Alt || modifiers == Keys.Control)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            switch (_screen)
            {
                case ScreenKind.Menu:
                    if (_menu.HandleKey(key)) return true;
                    break;
                case ScreenKind.Difficulty:
                    if (_difficultyPanel.HandleKey(key)) return true;
                    break;
                case ScreenKind.Game:
                    if (_game.HandleKey(key)) return true;
                    break;
                case ScreenKind.Tutorial:
                    if (_tutorial.HandleKey(key)) return true;
                    break;
                case ScreenKind.Settings:
                    if (_settings.HandleKey(key)) return true;
                    break;
                case ScreenKind.Result:
                    if (_result.HandleKey(key)) return true;
                    break;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ToggleFullscreen()
        {
            if (!_fullscreen)
            {
                _savedBorder = FormBorderStyle;
                _savedWindowState = WindowState;
                _savedBounds = Bounds;
                MinimumSize = Size.Empty;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromControl(this).Bounds;
                _fullscreen = true;
            }
            else
            {
                FormBorderStyle = _savedBorder;
                Bounds = _savedBounds;
                WindowState = _savedWindowState;
                MinimumSize = new Size(940, 570);
                _fullscreen = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _settingsManager.Save();
            _audio.StopMusic();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _audio.Dispose();
            base.OnFormClosed(e);
        }
    }
}
