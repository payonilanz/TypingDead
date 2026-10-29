# TYPING DEAD

**Type the word. Press Enter. Survive the horde.**

TYPING DEAD is a single-player pixel-art typing survival game for Windows, written in C# with WinForms and GDI+. You defend **three lanes** against a zombie horde by typing the word attached to the nearest zombie in your lane. Correct words do **not** fire automatically: after the word matches, you press **Enter** to attack.

The word pools are made of **IT and Data Structures terms**. Every target is a complete, single-word term such as `ARRAY`, `STACK`, `QUEUE`, `BINARY`, `NETWORK`, `POINTER`, `ALGORITHM`, and `DATABASE`. No mode uses spaced terms, so the Space key is never required.

All artwork, effects, and audio are generated at runtime. There are no image files, font files, or WAV files to load, and the game never uses the network.

---

## Current build

- Exactly **three lanes** and three lives.
- **Enter-to-attack** combat with nonlethal typing mistakes.
- Automatic target lock on the nearest living zombie in the player's lane.
- IT / Data Structures word pools with single-token terms only.
- Three complete modes: **Easy**, **Medium**, and **Hard**.
- One 100-point run per mode. No hidden levels or level transitions.
- Three-word / three-segment **Tank Zombies**.
- Permanent unlock progression: Easy -> Medium -> Hard -> **Final Victory**.
- Pixel-art battlefield, HUD, effects, menus, and result screens.
- Compact, short-phrase interface text.
- Music across menus and non-gameplay screens, silenced only during a run.
- Built-in headless test suite: **183 checks, 0 failures**.

---

## Requirements

- Windows 7 SP1 or later (verified on Windows 11 64-bit).
- .NET Framework 4.8 runtime.
- A keyboard and mouse.
- No other runtime dependencies.

The project targets **.NET Framework 4.8** and keeps C# 5-compatible syntax so the legacy .NET Framework 4.x compiler can build it.

---

## Build

### Visual Studio

1. Open `TypingDead.csproj`.
2. Select **Release** and **Any CPU**.
3. Build and run.

### MSBuild command line

From this folder:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe TypingDead.csproj /t:Rebuild /p:Configuration=Release
```

Or run:

```bat
build.bat
```

`build.bat` rebuilds `bin\Release\TypingDead.exe` and copies the executable and config to the project root.

> **Build note:** A clean, warning-free build needs the .NET Framework 4.8 Developer Pack. Without the targeting pack, legacy MSBuild falls back to GAC assemblies and reports `MSB3644` / `MSB3270` warnings, but still produces a working executable. That is how the included executable was verified in this environment.

### Self-test

```bat
TypingDead.exe --selftest
```

The test harness runs headlessly, writes `selftest-results.txt` beside the executable, and returns exit code `0` on success.

Current verified result:

```text
CHECKS PASSED: 183
CHECKS FAILED: 0
RESULT: PASS
```

---

## Controls

| Input | Action |
|---|---|
| **Up / Down** or **W / S** | Move the survivor between the three lanes. |
| **A-Z** | Type the current target word. Wrong characters are highlighted but do not cost a life. |
| **Enter** | Submit the typed word. A matching word launches the attack. An empty Enter is safe. |
| **Backspace** | Delete the last typed character. |
| **Esc** | Pause during gameplay; back out of menus, Settings, or How to Play. |
| **F1** | Open How to Play. An active run is paused while it is open. |
| **F11** | Toggle fullscreen. |
| Mouse | Every menu button and mode card can be clicked. |

`P` is a normal letter while a target is selected. Pause is always `Esc`. No mode assigns a spaced term; every target is a single A-Z token.

---

## Core rules

### Three lanes

- The battlefield always has exactly three lanes.
- Up/Down or W/S clamp at lane 1 and lane 3; there is no fourth lane.
- The nearest **living** zombie in the player's lane is targeted automatically. If a closer zombie enters that lane, target lock moves to it.
- Moving lanes never submits a word, fires a projectile, damages the player, or resets enemy health/word progress.

### Enter-to-attack

1. Type the displayed word with A-Z. Every target is a single IT term.
2. Wrong characters are appended and shown in red, but typing them costs **no life**.
3. When the full word matches, the console changes to **WORD READY: PRESS ENTER**.
4. No attack happens automatically.
5. Press **Enter once** to launch the attack.

Submission rules:

| Input | Result |
|---|---|
| Empty Enter | Shows `TYPE A WORD FIRST`; costs nothing. |
| Non-empty wrong word + Enter | Costs exactly **1 life**; no attack. |
| Correct word + Enter | Launches one projectile at the target. |
| Repeated Enter / duplicate submission | Cannot duplicate the attack, duplicate points, or deduct another life. |

### Lives and breaches

- Every mode attempt begins with **3 lives**.
- A zombie crossing the escape line costs **1 life**.
- A failed run ends at 0 lives.
- Completing a mode keeps the three-life rule for the next attempt: every new mode starts fresh with 3 lives.
- Pause freezes enemy movement, projectiles, effects, and elapsed gameplay time.

### Scoring

| Zombie | Health | Words | Points |
|---|---:|---:|---:|
| **Walker** | 1 segment | 1 word | **10** |
| **Runner** | 1 segment | 1 word | **10** |
| **Tank** | 3 segments | **3 distinct words** | **20**, only on the third hit |
| **Special** | 2 segments | 2 distinct terms | **25** |

- Standard kills award 10 points.
- Runners are single-hit enemies in every mode, including Hard: one correct word submission defeats them.
- Medium mode contains **no Tanks and no Specials**. It mixes fast Runners with steady Walkers.
- Hard mode contains **no Tanks and no Specials**. It replaces them with multiple Runners, so every Hard kill is a single-word, single-hit Runner or Walker.
- The score is clamped at **100**.
- Each program launch starts the score display at **0**; personal bests and best times are session-only and reset when the game is restarted.
- When the player reaches 100 points, the mode completes immediately for scoring purposes. No more rewards or completion events are accepted after success.
- All three modes are configured for **100 points**, not 500 and not multiple levels.

### Tank Zombie

The Tank is the signature durability enemy:

- It has exactly **three health segments**.
- It requires exactly **three distinct words**, assigned when it spawns.
- Each correctly submitted word removes one segment.
- Its current word and remaining health are preserved when the player changes lanes.
- It awards **no points after the first or second hit**.
- It awards its configured Tank points only when the third hit kills it.

### Modes and unlock progression

| Mode | Zombie mix | Word length | Unlock rule |
|---|---|---|---|
| **Easy** | Mostly Walkers; a few slow Tanks later | 3-5 characters | Available from a fresh game |
| **Medium** | Fast Runners and steady Walkers; no Tanks or Specials | 5-6 characters | Complete Easy |
| **Hard** | Runner-heavy waves with Walkers; no Tanks or Specials | **every word is 7-9 characters** | Complete Medium |

### Word content

All assigned terms come from Information Technology and Data Structures vocabulary. Every entry is a complete, single-word term; no pool contains a space.

- **Easy (3-5):** `ARRAY`, `STACK`, `QUEUE`, `GRAPH`, `TREE`, `NODE`, `DATA`, `LIST`, `HEAP`, `CACHE`, `QUERY`, `PIXEL`.
- **Medium (5-6):** `BINARY`, `SEARCH`, `INSERT`, `DELETE`, `MEMORY`, `SERVER`, `ROUTER`, `PACKET`, `SOCKET`, `MATRIX`, `SCHEMA`, `STREAM`.
- **Hard (7-9):** `POINTER`, `NETWORK`, `STORAGE`, `PROGRAM`, `DATABASE`, `SECURITY`, `PROTOCOL`, `FUNCTION`, `VARIABLE`, `ALGORITHM`, `RECURSION`, `TRAVERSAL`.

Hard terms are measured in characters with no spaces, and every entry is between 7 and 9 characters.

Progression rules:

- A fresh game unlocks **Easy only**.
- Complete Easy -> Medium unlocks.
- Complete Medium -> Hard unlocks.
- Complete Hard -> **Final Victory** screen.
- **Unlocks are permanent.** A failed run, a replay, or restarting the application never relocks a mode or clears a completion badge.
- Unlock and completion state is saved to `progress.json` and loaded on the next launch.
- Easy always remains available.
- Completed modes remain replayable.
- Each completed run records its duration. The fastest completion per mode appears as `BEST TIME` on the mode card, formatted as `MM:SS`; unset times show `--:--`.
- Personal bests and best times remain **session-only**. Settings and unlock progress persist on disk; scores and times begin at zero on each launch.

### Hard mode guarantee

Hard-mode word assignment does not merely offer long words. **Every word assigned to every enemy is validated at 7-9 characters and must be a single A-Z token.** The word manager is also checked constantly by the self-test.

### Music and audio

- The procedural background loop plays on the Main Menu, Difficulty Selection, How to Play, Settings, Pause, and result screens.
- Music runs on its own audio channel, so menu and UI sound effects do not interrupt or restart it.
- Music stops only when Easy, Medium, or Hard gameplay starts.
- Sound effects and music can each be toggled independently in Settings.
- Graphics quality controls particle density; the former Reduced Motion, Screen Shake, and preview controls are no longer shown.

---

## Screens

- **Main Menu** — Play, How to Play, Settings, Exit Game, and the exact group footer `J. Francia // K. Galay // L. Garcia // C. Hernandez`.
- **Difficulty Selection** — three mode cards with lock state, completion state, best score, best completion time, tuning summary, and working Play/Replay buttons. `BEST` reads `COMPLETED` after a cleared mode; `BEST TIME` shows the fastest completed run or `--:--`.
- **Gameplay** — pixel HUD, three lanes, targeted word tags, Tank segments, typing console, word-ready state, and status feedback.
- **Pause** — Resume, Restart, Settings, Main Menu. The run freezes underneath.
- **Settings** — large full-width rows for sound effects, music, and graphics quality. Changes save immediately.
- **How to Play** — a full-screen reference for controls, Enter-to-attack rules, zombie types, Tank mechanics, modes, permanent unlocks, and word lengths.
- **Mode Completion** — success report, `COMPLETED` in place of `100/100`, stats, and a replay option.
- **Game Over** — failure report and Retry/Main Menu. Unlocks and completion badges are kept.
- **Final Victory** — the all-modes-cleared ending; Play Again returns to mode selection without clearing progress.

The game also auto-pauses when the window loses focus.

---

## Pixel art direction

- Custom `PixelBackground` city skyline, dead trees, rain, and pixel survivors.
- Blocky sprites and hard-edged rectangles instead of neon cyberpunk machinery.
- Three-lane battlefield with lane markers and a dashed escape line.
- Charged shots, trails, impact bursts, reaction marks, score popups, damage feedback, and aftermath debris retained as pixel effects.
- `InterpolationMode.NearestNeighbor` and `SmoothingMode.None` keep the art crisp when the 1280x720 virtual canvas scales.

---

## Project structure

```text
TypingDead/
  Program.cs                 Entry point and --selftest switch
  TypingDead.csproj          .NET Framework 4.8 WinForms project
  app.manifest               DPI awareness and supported OS declarations
  App.config                 Runtime configuration
  build.bat                  Release build helper
  README.md

  Forms/
    MainForm.cs              Screen graph, navigation, progression integration
    MainMenuPanel.cs         Required main menu and exact group footer
    DifficultyPanel.cs       Easy / Medium / Hard selection cards
    GamePanel.cs             Gameplay controller and session lifecycle
    TutorialPanel.cs         How to Play screen
    SettingsPanel.cs         Functional settings editor
    ResultPanel.cs           Mode Complete / Game Over / Final Victory variants

  Game/
    GameConfig.cs            Three-lane layout and balance constants
    GameManager.cs           Rules authority: typing, Enter, lives, Tank hits, completion
    GameState.cs             Explicit game states
    GameResult.cs            End-of-run result data
    GameSettings.cs          User preference model
    DifficultySettings.cs    Mode tuning and word-length rules
    ProgressionManager.cs    Permanent unlock and completion authority

  Systems/
    AudioManager.cs          Procedural sound and music synthesis
    HighScoreManager.cs      Session-only per-mode bests and times
    InputHandler.cs          Keyboard-to-command mapping
    ProgressionStore.cs      JSON persistence for permanent unlocks
    ScoreManager.cs          Score, streak, accuracy statistics and 100-point clamp
    SettingsManager.cs       JSON settings persistence
    SpawnManager.cs          Three-lane spawn fairness and mode-specific mixes
    WordManager.cs           Validated single-token word pools, distinct Tank words

  Entities/
    Enemy.cs                 Abstract enemy base and shared pipeline
    WalkerEnemy.cs           Standard zombie
    RunnerEnemy.cs           Fast zombie
    TankEnemy.cs             Three-word / three-segment Tank
    SpecialZombie.cs         Two-word mutant retained for tests; not spawned in Hard
    Player.cs                Lane, target, typing, and charge state
    Projectile.cs            Attack that resolves against one target
    EnemyType.cs

  Effects/
    VisualEffect.cs          Abstract effect contract
    CombatEffects.cs         Impact, reaction, damage, and aftermath effects
    ChargeEffect.cs          Pixel charge feedback
    Particle.cs              Blocky particle primitive
    EffectManager.cs         Effect lifetime and screen shake

  Rendering/
    GameRenderer.cs          Pixel HUD, battlefield, tags, console, pause overlay
    DrawingHelper.cs         Cached GDI+ drawing primitives

  UI/
    Theme.cs                 Pixel palette and fonts
    VirtualScreenPanel.cs    1280x720 scaling, timer, double buffering
    NeonAction.cs            Reusable menu button
    PixelBackground.cs       Shared pixel-art background

  Tests/
    SelfTest.cs              183-check headless rules verification
```

There are no `.Designer.cs` files and no `TextBox` controls. Every screen is custom-painted on a `VirtualScreenPanel`.

---

## Architecture and OOP design

The central design rule is that **`MainForm` is not the game**. It owns navigation and application lifecycle only.

```text
MainForm
  -> panels / screens
  -> GamePanel
       -> GameManager          rules authority
            -> Player
            -> Enemy subclasses
            -> Projectile
            -> ScoreManager
            -> SpawnManager
            -> WordManager
       -> GameRenderer        presentation only
       -> EffectManager       visual effects
  -> ProgressionManager       permanent unlocks/completions
  -> ProgressionStore         saved unlock progress
  -> AudioManager / SettingsManager / HighScoreManager
```

Rendering reads state; it does not change score, lives, health, or progression. The result screens receive a `GameResult` snapshot and an optional captured bitmap instead of a live game reference.

### Required OOP pillar labels

The four exact comments are present in the source:

```csharp
// OOP PILLAR 1: ENCAPSULATION
```

`Game/GameManager.cs`, above the private rules state. `GameManager` hides lives, score, enemies, projectiles, input lock, target selection, Tank hit sequencing, and completion. Callers use operations such as `MoveLane`, `SubmitCharacter`, `SubmitWord`, `Pause`, and `Resume`.

```csharp
// OOP PILLAR 2: INHERITANCE
```

`Entities/Enemy.cs`, above the `Enemy` class. `WalkerEnemy`, `RunnerEnemy`, `TankEnemy`, and `SpecialZombie` inherit shared position, health, word sequencing, hit feedback, update, and draw behaviour.

```csharp
// OOP PILLAR 3: POLYMORPHISM
```

`Entities/Enemy.cs`, above `Enemy.Draw`. The renderer stores `List<Enemy>` and calls one shared contract, while each subclass supplies a different pixel body and speed override. `VisualEffect` also uses polymorphic update/draw calls.

```csharp
// OOP PILLAR 4: ABSTRACTION
```

`Entities/Enemy.cs`, above the abstract base class. The rest of the game depends on the abstract `Enemy` contract (`Word`, `Health`, `Lane`, `State`, `IsTargetable`, `ApplyHit`, `Draw`) rather than concrete zombie types. `VirtualScreenPanel` and `VisualEffect` provide the same kind of simplified contract for screens and effects.

### Class responsibilities

- **GameManager** — the only rules authority. It validates target selection, matches typed input, gates Enter submission, applies life penalties, advances Tank words, launches one projectile, clamps score, and raises the end event once.
- **Enemy / TankEnemy** — own health segments, current word index, incoming-attack reservation, movement, and pixel body drawing.
- **WordManager** — owns mode word pools, validates single-token lengths and characters, avoids immediate repeats, and returns three distinct Tank words.
- **SpawnManager** — chooses eligible lanes only, respects per-mode enemy caps, and applies the mode-specific zombie mix. Medium and Hard exclude Tanks.
- **ProgressionManager** — owns permanent unlock and completion state; UI panels ask it for state instead of recalculating progression.
- **ProgressionStore** — loads and saves unlock progress so a restart never relocks a mode.
- **GameRenderer** — draws the HUD, lanes, enemies, word tags, typed console, pause overlay, and end banners from read-only game state.
- **ResultPanel** — displays the report for a completed mode, a game over, or final victory.

---

## Persistence

The game writes these files under:

```text
%AppData%\TypingDead\settings.json
%AppData%\TypingDead\highscores.json
%AppData%\TypingDead\progress.json
```

`settings.json` and `progress.json` persist across launches. `highscores.json` is written while the current session runs but is intentionally **not loaded at startup**, so personal bests and best times begin at zero on every launch. Best scores are kept separately for Easy, Medium, and Hard, capped at **100**, and best times are stored in milliseconds for each mode.

`progress.json` stores the unlocked and completed flags for all three modes. Unlocks are permanent: a failed run, a replay, or a restart cannot relock a mode. Missing or invalid progress data falls back to a fresh **Easy-only** state.

---

## Testing

Run:

```bat
TypingDead.exe --selftest
```

The self-test uses the real rules API and deterministic seeds. It covers:

1. Exactly three lanes, 100-point target, and three starting lives.
2. Enter/arrow/letter key mapping.
3. Easy (3-5), Medium (5-6), and Hard (7-9) word-length validation.
4. Single-token pool validation with no spaces anywhere.
5. Every pool entry being an uppercase A-Z term inside its mode limit.
6. Hard mode requiring every word to be 7-9 characters.
7. Tank three-word distinctness and three-health-segment sequencing.
8. Hard Runners having one health segment and dying to one correct word.
9. A Hard Runner breach costing exactly one life.
10. Wrong typed characters not costing lives.
11. Empty Enter being safe and showing `TYPE A WORD FIRST`.
12. A wrong non-empty submitted word costing exactly one life.
13. Correct words not attacking automatically.
14. One Enter launching one projectile, with duplicate submissions blocked.
15. Lane changes preserving Tank health and word progress.
16. Pause freezing movement and elapsed time.
17. Score clamping at 100 and one-shot completion.
18. Easy completion not awarding Final Victory.
19. Hard completion awarding Final Victory.
20. Permanent progression: failures keep unlocks, and saved unlocks survive a restart.
21. Spawn lanes, enemy caps, Medium/Hard Tank exclusion, and word counts.
22. ScoreManager arithmetic and accuracy.
23. Special Zombie two-hit durability.

### Visual verification

All required screens were rendered to 1280x720 captures during the final pass:

- Main menu with the exact required footer.
- Difficulty screen in locked and unlocked states.
- Gameplay with a completed Medium word and `WORD READY: PRESS ENTER`.
- Hard gameplay with a single-token 7-9 character term and `WORD READY: PRESS ENTER`.
- Pause overlay.
- Settings.
- How to Play.
- Mode Completion.
- Game Over.
- Final Victory.

The final pass also fixed two layout issues found in those captures: a misplaced How-to-Play control strip and overlapping replay/completion text on the difficulty card.

---

## Limitations and known constraints

- **Windows only.** WinForms and GDI+.
- **Latin typing only.** Input is mapped from A-Z. No assigned word contains a space; IME composition, dead keys, and non-Latin scripts are not supported.
- **Fixed virtual resolution.** The 1280x720 canvas scales and letterboxes rather than reflowing.
- **Permanent progression, session-only records.** Unlocks and completion badges persist; personal bests and best times reset on a fresh launch.
- **No online services.** No account, cloud sync, or online leaderboard.
- **No mid-run save.** Pause freezes the current run; closing the application ends it.
- **Simple audio backend.** `SoundPlayer` handles sound effects while a separate MCI channel plays looping music.
- **Targeting-pack note.** The build machine lacks the .NET Framework 4.8 Developer Pack, so MSBuild 4.0 compiled against GAC assemblies and printed reference-assembly warnings. The executable was built, launched, exercised, and self-tested successfully.
- **Unsigned executable.** Windows SmartScreen may ask for confirmation.

---

## Reset and troubleshooting

- Delete `%AppData%\TypingDead\settings.json` to reset settings.
- Delete `%AppData%\TypingDead\progress.json` to reset unlocks and completion badges to **Easy only**.
- Best scores and best times are session-only; `highscores.json` is not loaded on startup.
- Delete the whole `%AppData%\TypingDead` folder to reset settings, unlocks, and the saved session file.
- If audio is silent, check the in-game Settings toggles and the Windows playback device.
- If the game pauses unexpectedly, another window took focus. This is intentional and prevents breaches while you are away.
- If a build cannot replace the root executable, close any running copy and run `build.bat` again.

---

*TYPING DEAD — Pixel Survival Build 2.0. Built with C#, WinForms, and GDI+.*
