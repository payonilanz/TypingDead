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
                "RAM", "CPU", "API", "BUS", "KEY", "MAP", "SET", "CACHE", "QUERY",
                "TABLE", "TOKEN", "PIXEL", "LOGIC", "CLASS", "FIELD", "CLOUD",
                "CABLE", "FLASH", "DRIVE", "MODEM", "PATCH", "DEBUG", "VIRUS",
                "LOGIN", "ADMIN", "MOUNT", "PARSE", "PRINT", "STORE", "FRAME",
                "SLICE", "ROUTE"
            };

            // Medium terms: 5-6 characters. Every entry is a single IT token,
            // so no mode ever requires the Space key.
            _words[GameDifficulty.Medium] = new string[]
            {
                "CACHE", "QUERY", "TABLE", "TUPLE", "TOKEN", "CLASS", "FIELD",
                "PIXEL", "LOGIC", "CLOUD", "LINUX", "BATCH", "FLASH", "DRIVE",
                "MODEM", "PATCH", "DEBUG", "VIRUS", "LOGIN", "ADMIN", "MOUNT",
                "PARSE", "PRINT", "STORE", "FRAME", "SLICE", "INDEX", "MERGE",
                "ARRAY", "STACK", "QUEUE", "GRAPH", "CABLE", "ROUTE", "BINARY",
                "SEARCH", "INSERT", "DELETE", "MEMORY", "LINEAR", "SORTED",
                "ARRAYS", "QUEUES", "STACKS", "TABLES", "HASHES", "ENCODE",
                "DECODE", "CIPHER", "PACKET", "SOCKET", "SERVER", "CLIENT",
                "ROUTER", "SWITCH", "DOMAIN", "KERNEL", "THREAD", "VECTOR",
                "MATRIX", "SCHEMA", "BUFFER", "STREAM", "STRING", "OBJECT",
                "METHOD", "SYNTAX", "COOKIE", "MODULE", "ACCESS", "UPDATE",
                "RECORD", "COLUMN", "VALUES", "SOURCE", "TARGET", "SIGNAL",
                "LINKED", "BITMAP", "ONLINE", "BACKUP", "SUBNET", "HEADER",
                "FOOTER"
            };

            // Hard terms: 7-9 characters. Single tokens only, so complete
            // data-structure vocabulary never needs a Space.
            _words[GameDifficulty.Hard] = new string[]
            {
                "POINTER", "NETWORK", "STORAGE", "PROCESS", "PROGRAM", "COMPILE",
                "BOOLEAN", "INTEGER", "DECIMAL", "GRAPHIC", "MONITOR", "ENCRYPT",
                "DECRYPT", "HASHING", "SORTING", "LINKING", "MAPPING", "PARSING",
                "TESTING", "MALWARE", "ROUTING", "PACKETS", "SOCKETS", "THREADS",
                "KERNELS", "BUFFERS", "STREAMS", "VECTORS", "SCHEMAS", "QUERIES",
                "COLUMNS", "RECORDS", "OBJECTS", "METHODS", "CLASSES", "MODULES",
                "RUNTIME", "COMPUTE", "MACHINE", "DIGITAL", "VIRTUAL", "HEAPIFY",
                "SUBTREE", "SIBLING", "BREADTH", "WEBSITE", "DESKTOP", "NETBOOK",
                "BITWISE", "BROWSER", "COOKIES", "SERVERS", "CLIENTS", "ROUTERS",
                "DOMAINS", "COMMAND", "LOGICAL", "INHERIT", "MUTABLE", "FUNCTION",
                "VARIABLE", "KEYBOARD", "DATABASE", "SECURITY", "DEBUGGER",
                "COMPILER", "TERMINAL", "INTERNET", "ETHERNET", "FIRMWARE",
                "HARDWARE", "SOFTWARE", "PROTOCOL", "PASSWORD", "FIREWALL",
                "MATRICES", "ALLOCATE", "ASSEMBLY", "ENCODING", "DECODING",
                "POINTERS", "INTEGERS", "DECIMALS", "QUERYING", "INDEXING",
                "DELETION", "ANCESTOR", "CHILDREN", "CHECKSUM", "TRANSFER",
                "DOWNLOAD", "WIRELESS", "COMPUTER", "MESSAGES", "DATASETS",
                "BOOLEANS", "OVERRIDE", "INSTANCE", "TEMPLATE", "GENERICS",
                "DEADLOCK", "REGISTER", "PHYSICAL", "ABSTRACT", "ENCODERS",
                "DECODERS", "ALGORITHM", "RECURSION", "ITERATION", "TRAVERSAL",
                "OPERATION", "ALLOCATOR", "AGGREGATE", "COMPILING", "DEBUGGING",
                "PROCESSOR", "INTERFACE", "BANDWIDTH", "FREQUENCY", "PROTOCOLS",
                "ENCRYPTED", "BACKTRACK", "STRUCTURE", "PARAMETER", "STATEMENT",
                "CONDITION", "FUNCTIONS", "VARIABLES", "CONSTANTS", "DATABASES",
                "SEARCHING", "INSERTION", "NAMESPACE", "REFERENCE", "IMMUTABLE",
                "SERIALIZE", "SCHEDULER", "MULTITASK", "SEMAPHORE", "PROCESSES"
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

            for (int attempt = 0; attempt < 160; attempt++)
            {
                string candidate = pool[_random.Next(pool.Length)];
                if (!ValidateWord(difficulty, candidate)) continue;
                if (recent.Contains(candidate) && recent.Count < pool.Length - 1) continue;
                chosen = candidate;
                break;
            }

            if (chosen == null)
            {
                // Deterministic fallback that still respects the length rule.
                for (int i = 0; i < pool.Length; i++)
                {
                    if (ValidateWord(difficulty, pool[i]))
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

        /// <summary>Returns a copy of a mode pool for verification and tooling.</summary>
        public string[] GetPool(GameDifficulty difficulty)
        {
            return (string[])_words[difficulty].Clone();
        }

        /// <summary>
        /// Returns distinct words for multi-hit enemies such as the Tank and
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

            // Every assigned term is a single A-Z token. Spaces and
            // punctuation are rejected because no mode uses multi-word terms.
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];
                if (c < 'A' || c > 'Z') return false;
            }

            if (difficulty == GameDifficulty.Easy)
            {
                return word.Length >= 3 && word.Length <= 5;
            }
            if (difficulty == GameDifficulty.Medium)
            {
                return word.Length >= 5 && word.Length <= 6;
            }
            return word.Length >= 7 && word.Length <= 9;
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
