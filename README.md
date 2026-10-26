# TYPING DEAD

**Type the word. Press Enter. Survive the horde.**

TYPING DEAD is a single-player pixel-art typing survival game for Windows, written in C# with WinForms and GDI+. You defend **three lanes** against a zombie horde by typing the word attached to the nearest zombie in your lane. Correct words do **not** fire automatically: after the word matches, you press **Enter** to attack.

The word pools are made of **Information Technology and Data Structures terms**, from short basics such as `ARRAY`, `STACK`, and `QUEUE` up to multi-word names such as `LINKED LIST`, `BINARY SEARCH TREE`, and `DOUBLY LINKED LIST`. Multi-word terms are typed with a real **Space** between words.

All artwork, effects, and audio are generated at runtime. There are no image files, font files, or WAV files to load, and the game never uses the network.

---

## Current build

- Exactly **three lanes** and three lives.
- **Enter-to-attack** combat with nonlethal typing mistakes.
- IT / Data Structures word pools, including multi-word terms typed with Space.
- Three complete modes: **Easy**, **Medium**, and **Hard**.
- One 100-point run per mode. No hidden levels or level transitions.
- Three-word / three-segment **Tank Zombies**.
- Unlock progression: Easy -> Medium -> Hard -> **Final Victory**.
- Pixel-art battlefield, HUD, effects, menus, and result screens.
- Compact, short-phrase interface text.
- Built-in headless test suite: **159 checks, 0 failures**.

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
CHECKS PASSED: 159
CHECKS FAILED: 0
RESULT: PASS
```

---

## Controls

| Input | Action |
|---|---|
| **Up / Down** or **W / S** | Move the survivor between the three lanes. |
| **A-Z** | Type the current target word. Wrong characters are highlighted but do not cost a life. |
| **Space** | Type a single space between words in terms such as `LINKED LIST`. Leading, trailing, and doubled spaces are rejected by validation. |
| **Enter** | Submit the typed word. A matching word launches the attack. An empty Enter is safe. |
| **Backspace** | Delete the last typed character. |
| **Esc** | Pause during gameplay; back out of menus, Settings, or How to Play. |
| **F1** | Open How to Play. An active run is paused while it is open. |
| **F11** | Toggle fullscreen. |
| Mouse | Every menu button and mode card can be clicked. |

`P` is a normal letter while a target is selected. Pause is always `Esc`.

---

## Core rules

### Three lanes

- The battlefield always has exactly three lanes.
- Up/Down or W/S clamp at lane 1 and lane 3; there is no fourth lane.
- The nearest **living** zombie in the player's lane becomes the current target.
- Moving lanes never submits a word, fires a projectile, damages the player, or resets enemy health/word progress.

### Enter-to-attack

1. Type the displayed word with A-Z. Use **Space** between words for multi-word terms.
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
| **Special** | 2 segments | 2 long words | **25** |

- Standard kills award 10 points.
- Runners are single-hit enemies in every mode, including Hard: one correct word submission defeats them.
- The score is clamped at **100**.
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
| **Medium** | Walkers, fast Runners, three-word Tanks | 5-9 characters | Complete Easy |
| **Hard** | Very fast Runners, fast Tanks, rare Specials | **every word is 10-18 characters** | Complete Medium |

### Word content

All assigned terms come from Information Technology and Data Structures vocabulary:

- **Easy (3-5):** `ARRAY`, `STACK`, `QUEUE`, `GRAPH`, `TREE`, `NODE`, `DATA`, `LIST`, `HEAP`, `LINK`, `LOOP`.
- **Medium (5-9):** `ALGORITHM`, `POINTER`, `BINARY`, `SEARCH`, `LINKED`, `SORTING`, `NETWORK`, `CIPHER`.
- **Hard (10-18):** `LINKED LIST`, `BINARY TREE`, `HASH TABLE`, `SINGLY LINKED LIST`, `DOUBLY LINKED LIST`, `BINARY SEARCH TREE`, `PRIORITY QUEUE`, `DATA STRUCTURE`, `TIME COMPLEXITY`.

Hard terms are measured in characters including spaces, and the longest entries are exactly 18 characters.

Progression rules:

- A fresh game unlocks **Easy only**.
- Complete Easy -> Medium unlocks.
- Complete Medium -> Hard unlocks.
- Complete Hard -> **Final Victory** screen.
- Any failed run resets progression to **Easy only**.
- Easy always remains available.
- Completed modes remain replayable.
- Progression is intentionally **session-only**. Starting the application creates a fresh Easy-only progression, while settings and personal bests are saved on disk.

### Hard mode guarantee

Hard-mode word assignment does not merely offer long words. **Every word assigned to every enemy — including every Tank word — is validated at 10 characters or more.** Multi-word terms are counted with their spaces. The word manager is also checked constantly by the self-test.

---

## Screens

- **Main Menu** — Play, How to Play, Settings, Exit Game, personal bests, and the exact group footer `J. Francia // K. Galay // L. Garcia // C. Hernandez`.
- **Difficulty Selection** — three mode cards with lock state, completion state, best score, tuning summary, and working Play/Replay buttons.
- **Gameplay** — pixel HUD, three lanes, targeted word tags, Tank segments, typing console, word-ready state, and status feedback.
- **Pause** — Resume, Restart, Settings, Main Menu. The run freezes underneath.
- **Settings** — sound, music, effects quality, screen shake, typing progress, reduced motion, and a live pixel preview. Changes save immediately.
- **How to Play** — controls, Enter-to-attack rules, zombie types, Tank mechanics, modes, unlocks, and reset rules.
- **Mode Completion** — success report, final score, personal best, stats, Continue.
- **Game Over** — failure report and Retry/Main Menu.
- **Final Victory** — the all-modes-cleared ending and a fresh-start option.

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
    ProgressionManager.cs    Unlock/reset authority

  Systems/
    AudioManager.cs          Procedural sound and music synthesis
    HighScoreManager.cs      Persistent per-mode personal bests
    InputHandler.cs          Keyboard-to-command mapping
    ScoreManager.cs          Score, streak, accuracy statistics and 100-point clamp
    SettingsManager.cs       JSON settings persistence
    SpawnManager.cs          Three-lane spawn fairness and mode-specific mixes
    WordManager.cs           Validated word pools, distinct Tank words

  Entities/
    Enemy.cs                 Abstract enemy base and shared pipeline
    WalkerEnemy.cs           Standard zombie
    RunnerEnemy.cs           Fast zombie
    TankEnemy.cs             Three-word / three-segment Tank
    SpecialZombie.cs         Two-word Hard-mode mutant
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
    SelfTest.cs              159-check headless rules verification
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
  -> ProgressionManager       unlocks/reset
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
- **WordManager** — owns mode word pools, validates lengths and characters, avoids immediate repeats, and returns three distinct Tank words.
- **SpawnManager** — chooses eligible lanes only, respects per-mode enemy caps, and applies the mode-specific zombie mix.
- **ProgressionManager** — owns unlock and reset rules; UI panels ask it for state instead of recalculating progression.
- **GameRenderer** — draws the HUD, lanes, enemies, word tags, typed console, pause overlay, and end banners from read-only game state.
- **ResultPanel** — displays the report for a completed mode, a game over, or final victory.

---

## Persistence

Settings and personal bests are stored under:

```text
%AppData%\TypingDead\settings.json
%AppData%\TypingDead\highscores.json
```

Missing or invalid files fall back to defaults. High scores are kept separately for Easy, Medium, and Hard, and every personal best is capped at **100** to match the one-run score limit.

Progression is deliberately kept in memory for the current application session. Closing and reopening the game returns to a fresh **Easy-only** state.

---

## Testing

Run:

```bat
TypingDead.exe --selftest
```

The self-test uses the real rules API and deterministic seeds. It covers:

1. Exactly three lanes, 100-point target, and three starting lives.
2. Enter/arrow/letter/Space key mapping.
3. Easy, Medium, and Hard word-length validation, including the 5-9 Medium range.
4. Single-space acceptance and rejection of leading, trailing, or doubled spaces.
5. Space-key typing of multi-word Hard terms through the real rules API.
6. Hard mode requiring every word to be at least 10 characters.
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
20. Progression unlock/reset rules and replay state.
21. Spawn lanes, enemy caps, and Tank/Special word counts.
22. ScoreManager arithmetic and accuracy.
23. Special Zombie two-hit durability.

### Visual verification

All required screens were rendered to 1280x720 captures during the final pass:

- Main menu with the exact required footer.
- Difficulty screen in locked and unlocked states.
- Gameplay with a Tank target, three health segments, a completed word, and `WORD READY: PRESS ENTER`.
- Hard gameplay with a spaced term (`BINARY TREES`) showing visible Space markers in the word boxes.
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
- **Latin typing only.** Input is mapped from A-Z plus a single Space between words. IME composition, dead keys, and non-Latin scripts are not supported.
- **Fixed virtual resolution.** The 1280x720 canvas scales and letterboxes rather than reflowing.
- **Session-only progression.** Unlocks reset on a fresh launch; settings and personal bests persist.
- **No online services.** No account, cloud sync, or online leaderboard.
- **No mid-run save.** Pause freezes the current run; closing the application ends it.
- **Simple audio backend.** `SoundPlayer` is used instead of a full mixer.
- **Targeting-pack note.** The build machine lacks the .NET Framework 4.8 Developer Pack, so MSBuild 4.0 compiled against GAC assemblies and printed reference-assembly warnings. The executable was built, launched, exercised, and self-tested successfully.
- **Unsigned executable.** Windows SmartScreen may ask for confirmation.

---

## Reset and troubleshooting

- Delete `%AppData%\TypingDead\settings.json` to reset settings.
- Delete `%AppData%\TypingDead\highscores.json` to reset personal bests.
- Delete the whole `%AppData%\TypingDead` folder to reset both.
- If audio is silent, check the in-game Settings toggles and the Windows playback device.
- If the game pauses unexpectedly, another window took focus. This is intentional and prevents breaches while you are away.
- If a build cannot replace the root executable, close any running copy and run `build.bat` again.

---

*TYPING DEAD — Pixel Survival Build 2.0. Built with C#, WinForms, and GDI+.*
