using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using TypingDead.Game;

namespace TypingDead.Systems
{
    /// <summary>
    /// Loads and saves settings in %AppData%\TypingDead\settings.json.
    /// Failures are non-fatal and surfaced through LastError.
    /// </summary>
    public sealed class SettingsManager
    {
        private readonly string _path;

        public GameSettings Settings { get; private set; }
        public string LastError { get; private set; }

        public SettingsManager()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TypingDead");
            _path = Path.Combine(folder, "settings.json");
            Settings = GameSettings.CreateDefault();
            Load();
        }

        public void Load()
        {
            LastError = null;
            try
            {
                if (!File.Exists(_path))
                {
                    return;
                }

                string json = File.ReadAllText(_path, Encoding.UTF8);
                if (String.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GameSettings));
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    GameSettings loaded = serializer.ReadObject(stream) as GameSettings;
                    if (loaded != null)
                    {
                        loaded.Normalize();
                        Settings = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = "Settings could not be loaded. Defaults restored.";
                Settings = GameSettings.CreateDefault();
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public bool Save()
        {
            LastError = null;
            try
            {
                string folder = Path.GetDirectoryName(_path);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GameSettings));
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, Settings);
                    string json = Encoding.UTF8.GetString(stream.ToArray());
                    File.WriteAllText(_path, json, Encoding.UTF8);
                }
                return true;
            }
            catch (Exception ex)
            {
                LastError = "Settings could not be saved. This session still works.";
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public void ResetToDefaults()
        {
            Settings.ResetToDefaults();
            Save();
        }
    }
}
