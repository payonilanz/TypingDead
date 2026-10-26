using System;
using System.Runtime.Serialization;

namespace TypingDead.Game
{
    public enum EffectsQuality
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// User preferences persisted as JSON. Defaults are used if the file is
    /// missing or invalid, so a bad configuration never blocks the game.
    /// </summary>
    [DataContract]
    public sealed class GameSettings
    {
        [DataMember(Name = "soundEnabled")]
        public bool SoundEnabled { get; set; }

        [DataMember(Name = "musicEnabled")]
        public bool MusicEnabled { get; set; }

        [DataMember(Name = "effectsQuality")]
        public EffectsQuality EffectsQuality { get; set; }

        [DataMember(Name = "screenShake")]
        public bool ScreenShake { get; set; }

        [DataMember(Name = "showTypingProgress")]
        public bool ShowTypingProgress { get; set; }

        [DataMember(Name = "reducedMotion")]
        public bool ReducedMotion { get; set; }

        public static GameSettings CreateDefault()
        {
            GameSettings settings = new GameSettings();
            settings.SoundEnabled = true;
            settings.MusicEnabled = true;
            settings.EffectsQuality = EffectsQuality.High;
            settings.ScreenShake = true;
            settings.ShowTypingProgress = true;
            settings.ReducedMotion = false;
            return settings;
        }

        public GameSettings Clone()
        {
            GameSettings copy = new GameSettings();
            copy.SoundEnabled = SoundEnabled;
            copy.MusicEnabled = MusicEnabled;
            copy.EffectsQuality = EffectsQuality;
            copy.ScreenShake = ScreenShake;
            copy.ShowTypingProgress = ShowTypingProgress;
            copy.ReducedMotion = ReducedMotion;
            return copy;
        }

        public void Normalize()
        {
            if (EffectsQuality < EffectsQuality.Low || EffectsQuality > EffectsQuality.High)
            {
                EffectsQuality = EffectsQuality.High;
            }
        }

        public void ResetToDefaults()
        {
            GameSettings defaults = CreateDefault();
            SoundEnabled = defaults.SoundEnabled;
            MusicEnabled = defaults.MusicEnabled;
            EffectsQuality = defaults.EffectsQuality;
            ScreenShake = defaults.ScreenShake;
            ShowTypingProgress = defaults.ShowTypingProgress;
            ReducedMotion = defaults.ReducedMotion;
        }

        public float ParticleMultiplier()
        {
            if (ReducedMotion) return 0.35f;
            switch (EffectsQuality)
            {
                case EffectsQuality.Low: return 0.45f;
                case EffectsQuality.Medium: return 0.75f;
                default: return 1f;
            }
        }

        public bool ShakeAllowed()
        {
            return ScreenShake && !ReducedMotion;
        }
    }
}
