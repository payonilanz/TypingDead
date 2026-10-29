using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using TypingDead.Game;

namespace TypingDead.Systems
{
    [DataContract]
    internal sealed class ProgressionData
    {
        [DataMember(Name = "unlocked")] public bool[] Unlocked { get; set; }
        [DataMember(Name = "completed")] public bool[] Completed { get; set; }
    }

    /// <summary>
    /// Saves permanent mode unlocks in %AppData%\TypingDead\progress.json.
    /// Failures are non-fatal; the current session keeps its in-memory state.
    /// </summary>
    public sealed class ProgressionStore : IProgressionStore
    {
        private readonly string _path;

        public string LastError { get; private set; }

        public ProgressionStore() : this(DefaultPath())
        {
        }

        public ProgressionStore(string path)
        {
            _path = path;
        }

        public void Load(bool[] unlocked, bool[] completed)
        {
            LastError = null;
            try
            {
                if (String.IsNullOrEmpty(_path) || !File.Exists(_path)) return;

                string json = File.ReadAllText(_path, Encoding.UTF8);
                if (String.IsNullOrWhiteSpace(json)) return;

                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(ProgressionData));
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    ProgressionData loaded = serializer.ReadObject(stream) as ProgressionData;
                    if (loaded == null) return;
                    CopyInto(loaded.Unlocked, unlocked);
                    CopyInto(loaded.Completed, completed);
                }
            }
            catch (Exception ex)
            {
                LastError = "Unlock progress could not be loaded.";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public void Save(bool[] unlocked, bool[] completed)
        {
            LastError = null;
            try
            {
                if (String.IsNullOrEmpty(_path)) return;

                string folder = Path.GetDirectoryName(_path);
                if (!String.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                ProgressionData data = new ProgressionData();
                data.Unlocked = CopyOf(unlocked);
                data.Completed = CopyOf(completed);

                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(ProgressionData));
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, data);
                    string json = Encoding.UTF8.GetString(stream.ToArray());
                    File.WriteAllText(_path, json, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                LastError = "Unlock progress could not be saved. This session still works.";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private static bool[] CopyOf(bool[] source)
        {
            if (source == null) return new bool[3];
            bool[] copy = new bool[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static void CopyInto(bool[] source, bool[] destination)
        {
            if (source == null || destination == null) return;
            int count = Math.Min(source.Length, destination.Length);
            Array.Copy(source, destination, count);
        }

        private static string DefaultPath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TypingDead");
            return Path.Combine(folder, "progress.json");
        }
    }
}
