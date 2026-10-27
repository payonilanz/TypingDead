using System;
using System.Collections.Generic;
using TypingDead.Entities;
using TypingDead.Game;

namespace TypingDead.Systems
{
    /// <summary>
    /// Keeps the three mode word pools and hands out words that obey the mode's
    /// length rules. A short recent-history queue prevents the same word from
    /// appearing over and over while alternatives remain.
    /// </summary>
    public sealed class WordManager
    {
        private readonly Random _random;
        private readonly Dictionary<GameDifficulty, string[]> _words;
        private readonly Dictionary<GameDifficulty, Queue<string>> _recent;

        public WordManager(Random random)
        {
            _random = random ?? new Random();
            _words = new Dictionary<GameDifficulty, string[]>();
            _recent = new Dictionary<GameDifficulty, Queue<string>>();

            // Easy terms: 3-5 letters. Single-word IT and data-structure basics.
            _words[GameDifficulty.Easy] = new string[]
            {
                "ARRAY", "STACK", "QUEUE", "GRAPH", "TREE", "NODE", "DATA", "LIST",
                "HEAP", "LINK", "SORT", "LOOP", "BYTE", "CODE", "FILE", "CHIP",
                "CORE", "DISK", "PATH", "PORT", "ROOT", "LEAF", "EDGE", "BIT",
                "RAM", "CPU", "API", "BUS", "KEY", "MAP", "SET"
            };

            // Medium terms: 5-9 characters. Includes spaced two-word terms,
            // which are typed with a real Space key.
            _words[GameDifficulty.Medium] = new string[]
            {
                "ALGORITHM", "POINTER", "BINARY", "SEARCH", "LINKED", "INSERT",
                "DELETE", "MEMORY", "LINEAR", "SORTING", "INDEX", "MERGE",
                "ARRAYS", "QUEUES", "STACKS", "TABLES", "RECURSE", "HASHING",
                "ENCRYPT", "NETWORK", "SERVER", "CLIENT", "ROUTER", "CIPHER",
                "QUEUED", "STACKED", "LINKING", "SORTED",
                "HASH MAP", "HASH SET", "BIT MASK", "DATA SET", "KEY PAIR",
                "TREE NODE", "LINK NODE", "BASE CASE", "STACK TOP", "TREE ROOT",
                "LEAF NODE", "MAIN LOOP", "SORT KEY", "HARD DISK", "CODE BASE",
                "TEST CASE", "USE CASE", "BIG DATA", "OPEN API", "BUG FIX",
                "LOW BIT"
            };

            // Hard terms: every entry is 10-18 characters. Spaces are typed
            // with the Space key, so full data-structure names stay readable.
            _words[GameDifficulty.Hard] = new string[]
            {
                "LINKED LIST", "BINARY TREE", "HASH TABLE", "BINARY HEAP",
                "SINGLY LINKED LIST", "DOUBLY LINKED LIST", "BINARY SEARCH TREE",
                "PRIORITY QUEUE", "CIRCULAR QUEUE", "HASH FUNCTION", "CALL STACK",
                "STACK TRACE", "DYNAMIC ARRAY", "DIRECTED GRAPH", "WEIGHTED GRAPH",
                "ADJACENCY LIST", "LINKED LIST NODE", "BINARY TREE NODE",
                "DATA STRUCTURE", "TIME COMPLEXITY", "SPACE COMPLEXITY",
                "DEPTH FIRST", "BREADTH FIRST", "HASH TABLES", "BINARY TREES",
                "LINKED LISTS", "SORTING ALGORITHM", "SEARCH ALGORITHM",
                "GRAPH TRAVERSAL", "TREE TRAVERSAL", "MUTUAL RECURSION",
                "TAIL RECURSION"
            };

            foreach (GameDifficulty difficulty in _words.Keys)
            {
                _recent[difficulty] = new Queue<string>();
            }
        }

        public void ResetRecent()
        {
            foreach (Queue<string> queue in _recent.Values)
            {
                queue.Clear();
            }
        }

        /// <summary>Returns one validated word for the mode and enemy type.</summary>
        public string GetWord(GameDifficulty difficulty, EnemyType type)
        {
            string[] pool = _words[difficulty];
            Queue<string> recent = _recent[difficulty];
            string chosen = null;

            // Hard-mode Runners are the replacement threat for Tanks and
            // Specials, so they draw from the longer end of the Hard pool.
            int minimumLength = difficulty == GameDifficulty.Hard && type == EnemyType.Runner
                ? 13 : 0;

            for (int attempt = 0; attempt < 160; attempt++)
            {
                string candidate = pool[_random.Next(pool.Length)];
                if (!ValidateWord(difficulty, candidate)) continue;
                if (candidate.Length < minimumLength) continue;
                if (recent.Contains(candidate) && recent.Count < pool.Length - 1) continue;
                chosen = candidate;
                break;
            }

            if (chosen == null)
            {
                // Deterministic fallback that still respects the length rule.
                for (int i = 0; i < pool.Length; i++)
                {
                    if (ValidateWord(difficulty, pool[i]) && pool[i].Length >= minimumLength)
                    {
                        chosen = pool[i];
                        break;
                    }
                }
            }

            if (chosen == null)
            {
                chosen = pool[_random.Next(pool.Length)];
            }

            Remember(difficulty, chosen);
            return chosen;
        }

        /// <summary>
        /// Returns distinct words for multi-word enemies such as the Tank and
        /// the mutated Special Zombie.
        /// </summary>
        public string[] GetWords(GameDifficulty difficulty, EnemyType type, int count)
        {
            if (count < 1) count = 1;
            List<string> picked = new List<string>();
            int guard = 0;
            while (picked.Count < count && guard < 600)
            {
                guard++;
                string word = GetWord(difficulty, type);
                if (!picked.Contains(word)) picked.Add(word);
            }

            // The pools are much larger than three words, but this fallback
            // keeps the class safe if a student trims a list in the future.
            string[] pool = _words[difficulty];
            int index = 0;
            while (picked.Count < count && index < pool.Length)
            {
                if (!picked.Contains(pool[index])) picked.Add(pool[index]);
                index++;
            }
            return picked.ToArray();
        }

        public bool ValidateWord(GameDifficulty difficulty, string word)
        {
            if (String.IsNullOrEmpty(word)) return false;
            if (word[0] == ' ' || word[word.Length - 1] == ' ') return false;

            bool previousWasSpace = false;
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];
                if (c == ' ')
                {
                    if (previousWasSpace) return false;
                    previousWasSpace = true;
                    continue;
                }
                if (c < 'A' || c > 'Z') return false;
                previousWasSpace = false;
            }

            if (difficulty == GameDifficulty.Easy)
            {
                return word.Length >= 3 && word.Length <= 5;
            }
            if (difficulty == GameDifficulty.Medium)
            {
                return word.Length >= 5 && word.Length <= 9;
            }
            return word.Length >= 10 && word.Length <= 18;
        }

        private void Remember(GameDifficulty difficulty, string word)
        {
            Queue<string> recent = _recent[difficulty];
            recent.Enqueue(word);
            int limit = difficulty == GameDifficulty.Hard ? 12 : 8;
            while (recent.Count > limit) recent.Dequeue();
        }
    }
}
