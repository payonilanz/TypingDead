using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TypingDead.Effects;
using TypingDead.Entities;
using TypingDead.Game;
using TypingDead.Systems;

namespace TypingDead.Tests
{
    /// <summary>
    /// Headless verification harness. It exercises the real rules through
    /// GameManager, Enemy, WordManager, SpawnManager and ProgressionManager,
    /// then writes a plain-text report. Run with --selftest.
    /// </summary>
    public static class SelfTest
    {
        private static readonly StringBuilder Log = new StringBuilder();
        private static int _passed;
        private static int _failed;

        public static string Run()
        {
            Log.Length = 0;
            _passed = 0;
            _failed = 0;

            Log.AppendLine("TYPING DEAD // HEADLESS SELF TEST");
            Log.AppendLine("Build: " + GameConfig.Version);
            Log.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Log.AppendLine();

            GameSettings settings = GameSettings.CreateDefault();
            settings.SoundEnabled = false;
            settings.MusicEnabled = false;
            settings.ReducedMotion = true;
            AudioManager audio = new AudioManager(settings);

            try
            {
                TestConfiguration();
                TestWords();
                TestEnemyMechanics();
                TestInputAndEnterAttack(audio, settings);
                TestSpaceTyping(audio, settings);
                TestEmptyAndWrongSubmission(audio, settings);
                TestTankZombie(audio, settings);
                TestRunnerDurability(audio, settings);
                TestTargetLock(audio, settings);
                TestLaneChangesPreserveProgress(audio, settings);
                TestPauseFreeze(audio, settings);
                TestScoreCapAndCompletion(audio, settings);
                TestProgression();
                TestSpawning();
                TestScoreManager();
                TestSpecialZombie();
            }
            finally
            {
                audio.Dispose();
            }

            Log.AppendLine();
            Log.AppendLine("CHECKS PASSED: " + _passed.ToString());
            Log.AppendLine("CHECKS FAILED: " + _failed.ToString());
            Log.AppendLine("RESULT: " + (_failed == 0 ? "PASS" : "FAIL"));
            return Log.ToString();
        }

        private static void TestConfiguration()
        {
            Log.AppendLine("[CONFIGURATION]");
            Check("Exactly three lanes are configured", GameConfig.LaneCount == 3);
            Check("Mode score target is 100", GameConfig.ModeScoreTarget == 100);
            Check("Starting lives are three", GameConfig.StartingLives == 3);
            Check("Standard kill is worth ten points", GameConfig.ScorePerKill == 10);
            Check("Group label is exact", GameConfig.GroupLabel ==
                "J. Francia // K. Galay // L. Garcia // C. Hernandez");
            Check("Lane 0 center is inside the battlefield",
                GameConfig.LaneCenter(0) > GameConfig.BattlefieldTop &&
                GameConfig.LaneCenter(0) < GameConfig.BattlefieldBottom);
            Check("Lane 2 center is inside the battlefield",
                GameConfig.LaneCenter(2) > GameConfig.BattlefieldTop &&
                GameConfig.LaneCenter(2) < GameConfig.BattlefieldBottom);
            Check("Lane 2 is the last valid lane",
                Math.Abs(GameConfig.LaneCenter(3) - GameConfig.LaneCenter(2)) < 0.001f);
            Check("Enter maps to the Submit action",
                InputHandler.MapGameKey(Keys.Enter).Action == GameAction.Submit);
            Check("Up maps to lane movement",
                InputHandler.MapGameKey(Keys.Up).Action == GameAction.MoveUp);
            Check("A letter maps to Letter input",
                InputHandler.MapGameKey(Keys.A).Action == GameAction.Letter &&
                InputHandler.MapGameKey(Keys.A).Letter == 'A');
            Check("Space maps to Letter input",
                InputHandler.MapGameKey(Keys.Space).Action == GameAction.Letter &&
                InputHandler.MapGameKey(Keys.Space).Letter == ' ');
            Log.AppendLine();
        }

        private static void TestWords()
        {
            Log.AppendLine("[WORD MANAGER]");
            WordManager words = new WordManager(new Random(2026));
            bool easyValid = true;
            bool mediumValid = true;
            bool hardValid = true;

            for (int i = 0; i < 300; i++)
            {
                string easy = words.GetWord(GameDifficulty.Easy, EnemyType.Walker);
                string medium = words.GetWord(GameDifficulty.Medium, EnemyType.Walker);
                string hard = words.GetWord(GameDifficulty.Hard, EnemyType.Special);
                if (!words.ValidateWord(GameDifficulty.Easy, easy)) easyValid = false;
                if (!words.ValidateWord(GameDifficulty.Medium, medium)) mediumValid = false;
                if (!words.ValidateWord(GameDifficulty.Hard, hard)) hardValid = false;
            }

            Check("Easy words are 3-5 letters", easyValid);
            Check("Medium words are 5-9 letters", mediumValid);
            Check("Hard words are 10-18 letters", hardValid);

            bool mediumSpaced = false;
            for (int i = 0; i < 1200; i++)
            {
                string candidate = words.GetWord(GameDifficulty.Medium, EnemyType.Walker);
                if (candidate.IndexOf(' ') >= 0)
                {
                    mediumSpaced = true;
                    break;
                }
            }
            Check("Medium pool includes spaced terms", mediumSpaced);
            Check("Spaced Medium term is accepted",
                words.ValidateWord(GameDifficulty.Medium, "HASH MAP"));

            bool hardRunnerLong = true;
            for (int i = 0; i < 240; i++)
            {
                string runnerWord = words.GetWord(GameDifficulty.Hard, EnemyType.Runner);
                if (runnerWord.Length < 13) hardRunnerLong = false;
            }
            Check("Hard Runner words are longer terms", hardRunnerLong);

            string[] tankWords = words.GetWords(GameDifficulty.Medium, EnemyType.Tank, 3);
            Check("Tank receives exactly three words", tankWords.Length == 3);
            Check("Tank words are distinct",
                tankWords[0] != tankWords[1] && tankWords[1] != tankWords[2] &&
                tankWords[0] != tankWords[2]);
            bool tankLengths = true;
            for (int i = 0; i < tankWords.Length; i++)
            {
                if (tankWords[i].Length < 5 || tankWords[i].Length > 9) tankLengths = false;
            }
            Check("Medium Tank words obey 5-9 length", tankLengths);

            string[] hardTank = words.GetWords(GameDifficulty.Hard, EnemyType.Tank, 3);
            bool hardTankLengths = true;
            for (int i = 0; i < hardTank.Length; i++)
            {
                if (hardTank[i].Length < 10) hardTankLengths = false;
            }
            Check("Every Hard Tank word is at least ten characters", hardTankLengths);

            Check("Lowercase words are rejected by validator",
                !words.ValidateWord(GameDifficulty.Easy, "array"));
            Check("Punctuation is rejected by validator",
                !words.ValidateWord(GameDifficulty.Hard, "LINKED-LIST"));
            Check("Too-short Hard word is rejected",
                !words.ValidateWord(GameDifficulty.Hard, "SHORT"));
            Check("Single space between words is accepted",
                words.ValidateWord(GameDifficulty.Hard, "LINKED LIST"));
            Check("Leading space is rejected",
                !words.ValidateWord(GameDifficulty.Hard, " LINKED LIST"));
            Check("Trailing space is rejected",
                !words.ValidateWord(GameDifficulty.Hard, "LINKED LIST "));
            Check("Double space is rejected",
                !words.ValidateWord(GameDifficulty.Hard, "LINKED  LIST"));
            Check("Word above the Medium limit is rejected",
                !words.ValidateWord(GameDifficulty.Medium, "DATASTRUCTURE"));
            Log.AppendLine();
        }

        private static void TestEnemyMechanics()
        {
            Log.AppendLine("[ENEMY AND TANK CONTRACT]");
            WalkerEnemy walker = new WalkerEnemy(1, "ARRAY");
            Check("Walker has one health segment", walker.MaxHealth == 1);
            Check("Walker is spawning before activation", walker.State == EnemyState.Spawning);
            walker.Update(0.5f, 45f);
            Check("Walker becomes active after spawn time", walker.State == EnemyState.Active);
            Check("Walker is targetable when active", walker.IsTargetable);

            TankEnemy tank = new TankEnemy(0, new string[] { "STACK", "QUEUE", "ARRAY" });
            Check("Tank has exactly three health segments", tank.MaxHealth == 3);
            Check("Tank exposes three words", tank.WordCount == 3);
            Check("Tank starts on its first word", tank.Word == "STACK");
            tank.Update(0.5f, 30f);
            tank.ReserveForAttack();
            Check("Reserved Tank is not targetable", !tank.IsTargetable);
            bool first = tank.ApplyHit();
            Check("First Tank hit does not kill", !first);
            Check("First Tank hit removes one segment", tank.Health == 2);
            Check("First Tank hit advances the word", tank.Word == "QUEUE");
            Check("Reservation clears after impact", !tank.HasIncomingAttack);
            tank.ReserveForAttack();
            bool second = tank.ApplyHit();
            Check("Second Tank hit does not kill", !second && tank.Health == 1);
            Check("Second Tank hit advances the word", tank.Word == "ARRAY");
            tank.ReserveForAttack();
            bool third = tank.ApplyHit();
            Check("Third Tank hit kills", third);
            Check("Dead Tank has zero health", tank.Health == 0);
            Check("Dead Tank enters dying state", tank.State == EnemyState.Dying);

            bool tankGuard = false;
            try
            {
                TankEnemy badTank = new TankEnemy(0, new string[] { "STACK", "QUEUE" });
            }
            catch (ArgumentException)
            {
                tankGuard = true;
            }
            Check("Tank constructor rejects fewer than three words", tankGuard);

            Log.AppendLine();
        }

        private static void TestInputAndEnterAttack(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[ENTER-TO-ATTACK]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Easy, settings, audio, effects, 100);
            game.DisableSpawningForTest();
            WalkerEnemy walker = new WalkerEnemy(1, "ARRAY");
            game.AddEnemyForTest(walker);
            Advance(game, 0.55f);

            Check("Target acquired in the player lane", game.Player.Target == walker);
            game.SubmitCharacter('X');
            Check("Wrong typed character does not cost a life", game.Lives == 3);
            Check("Wrong typed character does not attack", game.Projectiles.Count == 0);
            game.Backspace();
            Check("Backspace removes the wrong character", game.Player.TypedInput.Length == 0);

            TypeWord(game, "ARRAY");
            Check("Completed word does not attack automatically", game.Projectiles.Count == 0);
            Check("Completed word does not cost a life", game.Lives == 3);
            Check("Completed word shows ready state", game.Player.IsInputMatch);

            game.SubmitWord();
            Check("Enter launches exactly one projectile", game.Projectiles.Count == 1);
            game.SubmitWord();
            Check("Repeated Enter does not duplicate the attack", game.Projectiles.Count == 1);
            Check("Repeated Enter does not deduct a life", game.Lives == 3);

            Advance(game, 0.6f);
            Check("Projectile resolves and is removed", game.Projectiles.Count == 0);
            Check("Kill awards ten points", game.Score == 10);
            Check("Kill counter increments once", game.Kills == 1);
            Check("Walker is dying or removed after hit",
                walker.State == EnemyState.Dying || walker.State == EnemyState.Dead);
            Log.AppendLine();
        }

        private static void TestSpaceTyping(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[SPACE TYPING]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Hard, settings, audio, effects, 107);
            game.DisableSpawningForTest();
            WalkerEnemy walker = new WalkerEnemy(1, "LINKED LIST");
            game.AddEnemyForTest(walker);
            Advance(game, 0.55f);

            Check("Spaced term becomes the target", game.Player.Target == walker);
            TypeWord(game, "LINKED");
            game.SubmitCharacter(' ');
            Check("Space enters the typed buffer", game.Player.TypedInput == "LINKED ");
            TypeWord(game, "LIST");
            Check("Spaced term completes without auto-attack",
                game.Player.IsInputMatch && game.Projectiles.Count == 0);
            Check("Spaced term costs no life", game.Lives == 3);

            game.SubmitWord();
            Check("Enter launches one attack for a spaced term", game.Projectiles.Count == 1);
            game.SubmitWord();
            Check("Repeated Enter does not duplicate spaced attack", game.Projectiles.Count == 1);

            Advance(game, 0.6f);
            Check("Spaced term kill awards ten points", game.Score == 10);
            Log.AppendLine();
        }

        private static void TestEmptyAndWrongSubmission(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[EMPTY AND WRONG ENTER]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Easy, settings, audio, effects, 101);
            game.DisableSpawningForTest();
            WalkerEnemy walker = new WalkerEnemy(1, "ARRAY");
            game.AddEnemyForTest(walker);
            Advance(game, 0.55f);

            game.SubmitWord();
            Check("Empty Enter costs no life", game.Lives == 3);
            Check("Empty Enter attacks nothing", game.Projectiles.Count == 0);
            Check("Empty Enter shows TYPE A WORD FIRST",
                game.StatusMessage == "TYPE A WORD FIRST");

            game.SubmitCharacter('B');
            game.SubmitWord();
            Check("Wrong non-empty word costs exactly one life", game.Lives == 2);
            Check("Wrong non-empty word attacks nothing", game.Projectiles.Count == 0);
            Check("Typed buffer is cleared after wrong Enter", game.Player.TypedInput.Length == 0);

            game.SubmitWord();
            Check("Immediate repeated Enter cannot double-deduct", game.Lives == 2);

            Advance(game, 0.65f);
            game.SubmitCharacter('B');
            game.SubmitWord();
            Check("Second wrong submission costs one life", game.Lives == 1);

            Advance(game, 0.65f);
            game.SubmitCharacter('B');
            game.SubmitWord();
            Check("Third wrong submission reaches zero lives", game.Lives == 0);
            Check("Zero lives changes state to GameOver", game.State == GameState.GameOver);

            GameResult ended = null;
            game.GameEnded += delegate(object s, GameEndedEventArgs e) { ended = e.Result; };
            Advance(game, 1.1f);
            Check("Game over result is raised exactly once", ended != null);
            Check("Game over result is not a completion", ended != null && !ended.ModeCompleted);
            Log.AppendLine();
        }

        private static void TestTankZombie(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[THREE-WORD TANK]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Easy, settings, audio, effects, 102);
            game.DisableSpawningForTest();
            TankEnemy tank = new TankEnemy(1, new string[] { "STACK", "QUEUE", "ARRAY" });
            game.AddEnemyForTest(tank);
            Advance(game, 0.55f);

            Check("Tank becomes the target", game.Player.Target == tank);
            TypeWord(game, "STACK");
            game.SubmitWord();
            Advance(game, 0.58f);
            Check("First Tank word removes one segment", tank.Health == 2);
            Check("First Tank word awards no points", game.Score == 0);
            Check("First Tank word advances to the second word", tank.Word == "QUEUE");
            Check("Tank remains targetable after first impact", tank.IsTargetable);

            game.MoveLane(1);
            game.MoveLane(-1);
            Check("Lane change preserves Tank health", tank.Health == 2);
            Check("Lane change preserves Tank current word", tank.Word == "QUEUE");
            Check("Lane change does not attack", game.Projectiles.Count == 0);
            Check("Lane change does not damage the player", game.Lives == 3);

            TypeWord(game, "QUEUE");
            game.SubmitWord();
            Advance(game, 0.58f);
            Check("Second Tank word removes another segment", tank.Health == 1);
            Check("Second Tank word awards no points", game.Score == 0);
            Check("Second Tank word advances to the third word", tank.Word == "ARRAY");

            TypeWord(game, "ARRAY");
            game.SubmitWord();
            Advance(game, 0.58f);
            Check("Third Tank word removes the final segment", tank.Health == 0);
            Check("Tank kill awards configured Tank points",
                game.Score == GameConfig.TankScore);
            Check("Tank kill increments kills once", game.Kills == 1);
            Log.AppendLine();
        }

        private static void TestRunnerDurability(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[RUNNER DURABILITY]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Hard, settings, audio, effects, 108);
            game.DisableSpawningForTest();
            RunnerEnemy runner = new RunnerEnemy(1, "ALGORITHM");
            game.AddEnemyForTest(runner);
            Advance(game, 0.55f);

            Check("Hard Runner has one health segment", runner.MaxHealth == 1);
            Check("Hard Runner has one word", runner.WordCount == 1);
            Check("Hard Runner becomes the target", game.Player.Target == runner);
            TypeWord(game, "ALGORITHM");
            game.SubmitWord();
            Advance(game, 0.6f);
            Check("One correct word defeats a Hard Runner",
                runner.State == EnemyState.Dying || runner.State == EnemyState.Dead);
            Check("Hard Runner awards standard kill points", game.Score == 10);
            Check("Hard Runner kill increments kills once", game.Kills == 1);

            EffectManager breachEffects = new EffectManager();
            GameManager breach = new GameManager(GameDifficulty.Hard, settings, audio, breachEffects, 109);
            breach.DisableSpawningForTest();
            breach.AddEnemyForTest(new RunnerEnemy(1, "ALGORITHM"));
            Advance(breach, 12f);
            Check("Hard Runner breach costs exactly one life", breach.Lives == 2);
            Log.AppendLine();
        }

        private static void TestTargetLock(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[TARGET LOCK]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Easy, settings, audio, effects, 110);
            game.DisableSpawningForTest();
            WalkerEnemy farther = new WalkerEnemy(1, "ARRAY");
            game.AddEnemyForTest(farther);
            Advance(game, 0.55f);
            Check("Nearest lane enemy becomes the target", game.Player.Target == farther);

            WalkerEnemy nearer = new WalkerEnemy(1, "STACK");
            game.AddEnemyForTest(nearer);
            nearer.Update(0.5f, 0f);
            nearer.SetXForTest(500f);
            farther.SetXForTest(900f);
            Advance(game, 0.02f);
            Check("Target lock switches to a closer enemy", game.Player.Target == nearer);
            Check("Target lock keeps the player's lane",
                game.Player.Target != null && game.Player.Target.Lane == game.Player.Lane);

            game.MoveLane(-1);
            Check("Changing lanes releases a target from the old lane", game.Player.Target == null);
            Log.AppendLine();
        }

        private static void TestLaneChangesPreserveProgress(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[LANE SAFETY]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Medium, settings, audio, effects, 103);
            game.DisableSpawningForTest();
            TankEnemy tank = new TankEnemy(0, new string[] { "LINKED", "SORTING", "POINTER" });
            game.AddEnemyForTest(tank);
            Advance(game, 0.55f);
            game.MoveLane(-1);
            Check("Moved into lane 0", game.Player.Lane == 0);
            Check("Tank selected in lane 0", game.Player.Target == tank);

            int health = tank.Health;
            string word = tank.Word;
            game.MoveLane(1);
            Check("Moved out of lane 0", game.Player.Lane == 1);
            game.MoveLane(-1);
            Check("Returned to lane 0", game.Player.Lane == 0);
            Check("Enemy health preserved across returns", tank.Health == health);
            Check("Enemy word preserved across returns", tank.Word == word);
            Check("Lane movement did not submit or attack", game.Projectiles.Count == 0);
            Check("Lane movement did not deduct a life", game.Lives == 3);
            game.MoveLane(-1);
            Check("Moving above lane 0 is clamped safely", game.Player.Lane == 0);
            game.MoveLane(1);
            game.MoveLane(1);
            game.MoveLane(1);
            Check("Moving below lane 2 is clamped safely", game.Player.Lane == 2);
            Log.AppendLine();
        }

        private static void TestPauseFreeze(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[PAUSE]");
            EffectManager effects = new EffectManager();
            GameManager game = new GameManager(GameDifficulty.Easy, settings, audio, effects, 104);
            game.DisableSpawningForTest();
            WalkerEnemy walker = new WalkerEnemy(0, "ARRAY");
            game.AddEnemyForTest(walker);
            Advance(game, 0.55f);
            game.MoveLane(-1);
            float enemyX = walker.X;
            float elapsed = game.ElapsedTime;

            Check("Pause accepted during play", game.Pause());
            Advance(game, 1.2f);
            Check("Pause freezes enemy movement", Math.Abs(walker.X - enemyX) < 0.001f);
            Check("Pause freezes elapsed gameplay time", Math.Abs(game.ElapsedTime - elapsed) < 0.001f);
            Check("Pause does not alter lives", game.Lives == 3);
            Check("Pause state remains explicit", game.State == GameState.Paused);
            Check("Resume accepted from pause", game.Resume());
            Advance(game, 0.2f);
            Check("Resume restarts enemy movement", walker.X < enemyX);
            Log.AppendLine();
        }

        private static void TestScoreCapAndCompletion(AudioManager audio, GameSettings settings)
        {
            Log.AppendLine("[100-POINT COMPLETION]");
            EffectManager freshEffects = new EffectManager();
            GameManager fresh = new GameManager(GameDifficulty.Hard, settings, audio, freshEffects, 111);
            Check("A fresh run starts at zero score", fresh.Score == 0);
            Check("A fresh run starts with three lives", fresh.Lives == 3);

            EffectManager easyEffects = new EffectManager();
            GameManager easy = new GameManager(GameDifficulty.Easy, settings, audio, easyEffects, 105);
            easy.DisableSpawningForTest();
            for (int i = 0; i < 9; i++)
            {
                easy.RegisterKillForTest();
                easy.Update(0.01f);
            }
            Check("Nine standard kills award 90 points", easy.Score == 90);
            Check("90 points has not completed the mode", easy.State == GameState.Playing);
            easy.RegisterKillForTest(25);
            Check("Score is clamped to 100", easy.Score == 100);
            easy.Update(0.01f);
            Check("100 points enters ModeComplete", easy.State == GameState.ModeComplete);
            easy.RegisterKillForTest(50);
            Check("No extra reward is added after completion", easy.Score == 100);

            GameResult easyResult = null;
            easy.GameEnded += delegate(object s, GameEndedEventArgs e) { easyResult = e.Result; };
            Advance(easy, 1.4f);
            Check("Mode completion result raised", easyResult != null);
            Check("Easy result is a mode completion", easyResult != null && easyResult.ModeCompleted);
            Check("Easy result is not final victory", easyResult != null && !easyResult.FinalVictory);
            Check("Result score is 100", easyResult != null && easyResult.Score == 100);

            EffectManager hardEffects = new EffectManager();
            GameManager hard = new GameManager(GameDifficulty.Hard, settings, audio, hardEffects, 106);
            hard.DisableSpawningForTest();
            hard.RegisterKillForTest(100);
            hard.Update(0.01f);
            Check("Hard completion enters Victory", hard.State == GameState.Victory);
            GameResult hardResult = null;
            hard.GameEnded += delegate(object s, GameEndedEventArgs e) { hardResult = e.Result; };
            Advance(hard, 1.4f);
            Check("Final victory result raised", hardResult != null && hardResult.FinalVictory);
            Log.AppendLine();
        }

        private static void TestProgression()
        {
            Log.AppendLine("[PROGRESSION]");
            ProgressionManager progression = new ProgressionManager();
            Check("Fresh game unlocks Easy", progression.IsUnlocked(GameDifficulty.Easy));
            Check("Fresh game locks Medium", !progression.IsUnlocked(GameDifficulty.Medium));
            Check("Fresh game locks Hard", !progression.IsUnlocked(GameDifficulty.Hard));

            GameDifficulty? firstUnlock = progression.Complete(GameDifficulty.Easy);
            Check("Easy completion returns Medium unlock", firstUnlock == GameDifficulty.Medium);
            Check("Easy completion unlocks Medium", progression.IsUnlocked(GameDifficulty.Medium));
            Check("Easy completion keeps Hard locked", !progression.IsUnlocked(GameDifficulty.Hard));
            Check("Easy is marked completed", progression.IsCompleted(GameDifficulty.Easy));

            GameDifficulty? secondUnlock = progression.Complete(GameDifficulty.Medium);
            Check("Medium completion returns Hard unlock", secondUnlock == GameDifficulty.Hard);
            Check("Medium completion unlocks Hard", progression.IsUnlocked(GameDifficulty.Hard));

            progression.Complete(GameDifficulty.Hard);
            Check("Hard completion marks all modes complete", progression.AllModesCompleted);

            progression.Fail(GameDifficulty.Medium);
            Check("Failure reopens Easy", progression.IsUnlocked(GameDifficulty.Easy));
            Check("Failure locks Medium", !progression.IsUnlocked(GameDifficulty.Medium));
            Check("Failure locks Hard", !progression.IsUnlocked(GameDifficulty.Hard));
            Check("Failure clears completion records",
                !progression.IsCompleted(GameDifficulty.Easy) &&
                !progression.IsCompleted(GameDifficulty.Medium) &&
                !progression.IsCompleted(GameDifficulty.Hard));

            progression.Reset();
            Check("Reset restores fresh progression", progression.IsUnlocked(GameDifficulty.Easy) &&
                !progression.IsUnlocked(GameDifficulty.Medium) &&
                !progression.IsUnlocked(GameDifficulty.Hard));
            Log.AppendLine();
        }

        private static void TestSpawning()
        {
            Log.AppendLine("[SPAWNING]");
            WordManager words = new WordManager(new Random(12));
            SpawnManager spawner = new SpawnManager(new Random(13), words);
            bool lanesValid = true;
            bool wordsValid = true;
            bool hardLengths = true;
            bool hardNoTanksOrSpecials = true;
            bool hardRunnerLong = true;
            int hardRunners = 0;
            List<Enemy> active = new List<Enemy>();
            GameDifficulty[] modes = new GameDifficulty[]
            {
                GameDifficulty.Easy, GameDifficulty.Medium, GameDifficulty.Hard
            };

            for (int m = 0; m < modes.Length; m++)
            {
                DifficultySettings tuning = DifficultySettings.Create(modes[m]);
                for (int i = 0; i < 120; i++)
                {
                    Enemy enemy = spawner.TrySpawn(5f, tuning, active, 20f);
                    if (enemy == null) continue;
                    if (enemy.Lane < 0 || enemy.Lane >= 3) lanesValid = false;
                    if (enemy.Type == EnemyType.Tank && (enemy.WordCount != 3 || enemy.Health != 3))
                        wordsValid = false;
                    if (enemy.Type == EnemyType.Special && (enemy.WordCount != 2 || enemy.Health != 2))
                        wordsValid = false;
                    if (modes[m] == GameDifficulty.Hard && enemy.Word.Length < 10) hardLengths = false;
                    if (modes[m] == GameDifficulty.Hard)
                    {
                        if (enemy.Type == EnemyType.Tank || enemy.Type == EnemyType.Special)
                        {
                            hardNoTanksOrSpecials = false;
                        }
                        if (enemy.Type == EnemyType.Runner)
                        {
                            hardRunners++;
                            if (enemy.Word.Length < 13) hardRunnerLong = false;
                        }
                    }
                    enemy.Update(0.5f, 0f);
                    active.Clear();
                }
            }

            Check("Spawner only uses the three valid lanes", lanesValid);
            Check("Tank and Special spawn with correct word/health counts", wordsValid);
            Check("Hard spawns never assign a word below ten letters", hardLengths);
            Check("Hard spawns exclude Tanks and Specials", hardNoTanksOrSpecials);
            Check("Hard sends a Runner-heavy mix", hardRunners >= 20);
            Check("Hard Runner spawns use longer terms", hardRunnerLong);

            List<Enemy> crowded = new List<Enemy>();
            crowded.Add(new WalkerEnemy(0, "STACK"));
            crowded.Add(new WalkerEnemy(1, "QUEUE"));
            crowded.Add(new WalkerEnemy(2, "ARRAY"));
            Enemy blocked = spawner.TrySpawn(99f, DifficultySettings.Create(GameDifficulty.Easy),
                crowded, 30f);
            Check("Spawner respects per-mode enemy cap", blocked == null);
            Log.AppendLine();
        }

        private static void TestScoreManager()
        {
            Log.AppendLine("[SCORE MANAGER]");
            ScoreManager score = new ScoreManager();
            score.RegisterKill(10, 100);
            score.RegisterKill(10, 100);
            Check("ScoreManager accumulates kills", score.Score == 20 && score.Kills == 2);
            score.RegisterKill(200, 100);
            Check("ScoreManager clamps at the target", score.Score == 100);
            int killsBefore = score.Kills;
            score.RegisterKill(10, 100);
            Check("ScoreManager refuses rewards at the cap", score.Kills == killsBefore);
            score.RegisterCorrectCharacter();
            score.RegisterError();
            Check("Accuracy counts both correct and wrong characters",
                Math.Abs(score.Accuracy - 50f) < 0.01f);

            HighScoreData best = new HighScoreData();
            Check("A new score table starts at zero",
                best.Get(GameDifficulty.Easy) == 0 &&
                best.Get(GameDifficulty.Medium) == 0 &&
                best.Get(GameDifficulty.Hard) == 0);
            best.Set(GameDifficulty.Easy, 230);
            Check("Personal best is capped at 100", best.Get(GameDifficulty.Easy) == 100);
            best.Set(GameDifficulty.Easy, -5);
            Check("Personal best cannot go below zero", best.Get(GameDifficulty.Easy) == 0);

            Check("A new score table has no best time",
                best.GetTimeMs(GameDifficulty.Easy) == 0 &&
                best.GetTimeMs(GameDifficulty.Medium) == 0 &&
                best.GetTimeMs(GameDifficulty.Hard) == 0);
            best.SetTimeMs(GameDifficulty.Easy, 90500);
            Check("Best time stores milliseconds", best.GetTimeMs(GameDifficulty.Easy) == 90500);
            Check("Best time formats as minutes and seconds",
                HighScoreManager.FormatTime(90500) == "01:30");
            Check("Unset best time shows a placeholder",
                HighScoreManager.FormatTime(0) == "--:--");
            best.SetTimeMs(GameDifficulty.Easy, -50);
            Check("Best time cannot go below zero", best.GetTimeMs(GameDifficulty.Easy) == 0);
            Log.AppendLine();
        }

        private static void TestSpecialZombie()
        {
            Log.AppendLine("[SPECIAL ZOMBIE]");
            string[] words = new string[] { "BINARY SEARCH TREE", "SINGLY LINKED LIST" };
            SpecialZombie special = new SpecialZombie(2, words);
            Check("Special has two health segments", special.MaxHealth == 2);
            Check("Special has two long words", special.WordCount == 2);
            special.Update(0.5f, 100f);
            special.ReserveForAttack();
            bool killed = special.ApplyHit();
            Check("First Special hit is not lethal", !killed && special.Health == 1);
            Check("Special advances to its second word", special.Word == "SINGLY LINKED LIST");
            special.ReserveForAttack();
            Check("Second Special hit kills", special.ApplyHit());
            Check("Special is worth more than a standard Walker",
                GameConfig.SpecialScore > GameConfig.ScorePerKill);
            Log.AppendLine();
        }

        private static void TypeWord(GameManager game, string word)
        {
            for (int i = 0; i < word.Length; i++)
            {
                game.SubmitCharacter(word[i]);
            }
        }

        private static void Advance(GameManager game, float seconds)
        {
            if (seconds <= 0f) return;
            int steps = Math.Max(1, (int)Math.Ceiling(seconds / 0.02f));
            float slice = seconds / steps;
            for (int i = 0; i < steps; i++)
            {
                game.Update(slice);
            }
        }

        private static void Check(string name, bool condition)
        {
            if (condition)
            {
                _passed++;
                Log.AppendLine("  PASS  " + name);
            }
            else
            {
                _failed++;
                Log.AppendLine("  FAIL  " + name);
            }
        }
    }
}
