using System;
using System.Collections.Generic;
using TypingDead.Entities;
using TypingDead.Game;

namespace TypingDead.Systems
{
    /// <summary>
    /// Controls spawn cadence, lane fairness and mode-specific zombie mixes.
    /// No panel contains spawn rules; this class is the single source of truth.
    /// </summary>
    public sealed class SpawnManager
    {
        private readonly Random _random;
        private readonly WordManager _words;
        private float _spawnTimer;

        public SpawnManager(Random random, WordManager words)
        {
            _random = random;
            _words = words;
            _spawnTimer = 0.8f;
        }

        public void Reset(float initialDelay)
        {
            _spawnTimer = Math.Max(0.25f, initialDelay);
        }

        public Enemy TrySpawn(float deltaTime, DifficultySettings difficulty,
            IList<Enemy> activeEnemies, float gameTime)
        {
            if (activeEnemies == null || difficulty == null) return null;

            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0f) return null;

            if (activeEnemies.Count >= difficulty.MaxEnemies)
            {
                _spawnTimer = 0.3f;
                return null;
            }

            EnemyType type = ChooseType(difficulty.Difficulty, gameTime);
            int wordCount = type == EnemyType.Tank ? 3 : (type == EnemyType.Special ? 2 : 1);
            string[] words = _words.GetWords(difficulty.Difficulty, type, wordCount);
            int lane = ChooseLane(activeEnemies);

            Enemy enemy = CreateEnemy(type, lane, words);
            _spawnTimer = difficulty.SpawnInterval *
                (0.82f + (float)_random.NextDouble() * 0.36f);
            return enemy;
        }

        private Enemy CreateEnemy(EnemyType type, int lane, string[] words)
        {
            switch (type)
            {
                case EnemyType.Runner:
                    // Runners are single-word, single-hit enemies in every mode.
                    return new RunnerEnemy(lane, words[0]);
                case EnemyType.Tank:
                    return new TankEnemy(lane, words);
                case EnemyType.Special:
                    return new SpecialZombie(lane, words);
                default:
                    return new WalkerEnemy(lane, words[0]);
            }
        }

        private EnemyType ChooseType(GameDifficulty difficulty, float gameTime)
        {
            double roll = _random.NextDouble();
            if (difficulty == GameDifficulty.Easy)
            {
                // Easy is a learning mode: Walkers dominate. A slow Tank may
                // appear after the player has had time to learn the controls.
                if (gameTime > 12f && roll > 0.90) return EnemyType.Tank;
                return EnemyType.Walker;
            }

            if (difficulty == GameDifficulty.Medium)
            {
                if (roll < 0.30) return EnemyType.Runner;
                if (roll < 0.46) return EnemyType.Tank;
                return EnemyType.Walker;
            }

            // Hard mode replaces the old Tank and Special threats with a heavy
            // Runner presence. Every Runner is a single-word, single-hit enemy.
            if (roll < 0.70) return EnemyType.Runner;
            return EnemyType.Walker;
        }

        private int ChooseLane(IList<Enemy> activeEnemies)
        {
            int[] start = new int[] { _random.Next(GameConfig.LaneCount),
                _random.Next(GameConfig.LaneCount), _random.Next(GameConfig.LaneCount) };
            int bestLane = start[0];
            float bestGap = -1f;

            for (int i = 0; i < start.Length; i++)
            {
                int lane = start[i];
                float gap = 9999f;
                for (int e = 0; e < activeEnemies.Count; e++)
                {
                    Enemy enemy = activeEnemies[e];
                    if (enemy.Lane != lane || !enemy.IsDangerous) continue;
                    gap = Math.Min(gap, GameConfig.SpawnX - enemy.X);
                }
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestLane = lane;
                }
            }
            return bestLane;
        }
    }
}
