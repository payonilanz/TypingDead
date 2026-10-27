using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using TypingDead.Game;

namespace TypingDead.Systems
{
    public enum GameSound
    {
        MenuHover,
        MenuSelect,
        GameStart,
        TypeCorrect,
        TypeWrong,
        TargetAcquired,
        Charge,
        Fire,
        Impact,
        EnemyDeath,
        Damage,
        LevelUp,
        Pause,
        Resume,
        GameOver,
        Victory
    }

    /// <summary>
    /// Centralised audio. All WAV assets are synthesised at startup, so the
    /// project has no missing-file dependencies. Sound effects use SoundPlayer,
    /// which only supports one waveform at a time, so the looping menu music is
    /// played through a separate MCI channel. That lets music and menu sound
    /// effects mix instead of cutting each other off. Music is stopped only
    /// when gameplay begins.
    /// </summary>
    public sealed class AudioManager : IDisposable
    {
        private enum WaveKind
        {
            Sine,
            Square,
            Saw,
            Noise
        }

        private sealed class ToneSpec
        {
            public float StartFrequency;
            public float EndFrequency;
            public float Duration;
            public float Volume;
            public WaveKind Wave;

            public ToneSpec(float start, float end, float duration, float volume, WaveKind wave)
            {
                StartFrequency = start;
                EndFrequency = end;
                Duration = duration;
                Volume = volume;
                Wave = wave;
            }
        }

        private sealed class SoundSlot : IDisposable
        {
            public SoundPlayer Player;
            public MemoryStream Stream;
            public int MinimumIntervalMs;
            public long LastPlayedTicks;

            public void Dispose()
            {
                if (Player != null)
                {
                    try { Player.Stop(); } catch { }
                    try { Player.Dispose(); } catch { }
                }
                if (Stream != null)
                {
                    try { Stream.Dispose(); } catch { }
                }
            }
        }

        private readonly Dictionary<GameSound, SoundSlot> _sounds = new Dictionary<GameSound, SoundSlot>();
        private SoundPlayer _musicPlayer;
        private MemoryStream _musicStream;
        private readonly object _sync = new object();

        // Music runs on its own MCI channel so SoundPlayer effects cannot stop
        // it. MCI waveaudio has no reliable "repeat" flag, so a lightweight
        // timer restarts the track when it reaches the end.
        private readonly string _musicAlias = "tdmusic" + Guid.NewGuid().ToString("N").Substring(0, 8);
        private string _musicPath;
        private bool _musicOpen;
        private System.Windows.Forms.Timer _musicLoopTimer;

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder returnValue,
            int returnLength, IntPtr hwndCallback);

        public bool SoundEnabled { get; private set; }
        public bool MusicEnabled { get; private set; }
        public bool Available { get; private set; }
        public bool MusicPlaying { get; private set; }

        public AudioManager(GameSettings settings)
        {
            Available = true;
            try
            {
                BuildSounds();
                BuildMusic();
            }
            catch (Exception ex)
            {
                Available = false;
                System.Diagnostics.Debug.WriteLine("Audio initialisation failed: " + ex.Message);
            }

            ApplySettings(settings);
        }

        private void BuildSounds()
        {
            Add(GameSound.MenuHover, new ToneSpec[]
            {
                new ToneSpec(880f, 920f, 0.035f, 0.12f, WaveKind.Sine)
            }, 45);

            Add(GameSound.MenuSelect, new ToneSpec[]
            {
                new ToneSpec(620f, 980f, 0.075f, 0.20f, WaveKind.Sine)
            }, 80);

            Add(GameSound.GameStart, new ToneSpec[]
            {
                new ToneSpec(180f, 360f, 0.18f, 0.20f, WaveKind.Saw),
                new ToneSpec(360f, 720f, 0.20f, 0.18f, WaveKind.Sine),
                new ToneSpec(720f, 1180f, 0.28f, 0.16f, WaveKind.Sine)
            }, 250);

            Add(GameSound.TypeCorrect, new ToneSpec[]
            {
                new ToneSpec(740f, 860f, 0.045f, 0.13f, WaveKind.Sine)
            }, 32);

            Add(GameSound.TypeWrong, new ToneSpec[]
            {
                new ToneSpec(190f, 105f, 0.17f, 0.24f, WaveKind.Square),
                new ToneSpec(90f, 60f, 0.16f, 0.16f, WaveKind.Saw)
            }, 220);

            Add(GameSound.TargetAcquired, new ToneSpec[]
            {
                new ToneSpec(520f, 760f, 0.075f, 0.12f, WaveKind.Sine)
            }, 130);

            Add(GameSound.Charge, new ToneSpec[]
            {
                new ToneSpec(280f, 560f, 0.12f, 0.09f, WaveKind.Sine)
            }, 300);

            Add(GameSound.Fire, new ToneSpec[]
            {
                new ToneSpec(1050f, 180f, 0.20f, 0.20f, WaveKind.Saw),
                new ToneSpec(1400f, 400f, 0.10f, 0.10f, WaveKind.Sine)
            }, 80);

            Add(GameSound.Impact, new ToneSpec[]
            {
                new ToneSpec(160f, 70f, 0.16f, 0.28f, WaveKind.Noise),
                new ToneSpec(320f, 150f, 0.11f, 0.16f, WaveKind.Square)
            }, 55);

            Add(GameSound.EnemyDeath, new ToneSpec[]
            {
                new ToneSpec(320f, 70f, 0.24f, 0.24f, WaveKind.Saw),
                new ToneSpec(900f, 200f, 0.10f, 0.10f, WaveKind.Noise)
            }, 50);

            Add(GameSound.Damage, new ToneSpec[]
            {
                new ToneSpec(120f, 45f, 0.34f, 0.30f, WaveKind.Square),
                new ToneSpec(240f, 80f, 0.20f, 0.18f, WaveKind.Noise)
            }, 260);

            Add(GameSound.LevelUp, new ToneSpec[]
            {
                new ToneSpec(523f, 523f, 0.10f, 0.16f, WaveKind.Sine),
                new ToneSpec(659f, 659f, 0.10f, 0.16f, WaveKind.Sine),
                new ToneSpec(784f, 784f, 0.10f, 0.18f, WaveKind.Sine),
                new ToneSpec(1046f, 1046f, 0.28f, 0.20f, WaveKind.Sine)
            }, 500);

            Add(GameSound.Pause, new ToneSpec[]
            {
                new ToneSpec(440f, 220f, 0.12f, 0.13f, WaveKind.Sine)
            }, 100);

            Add(GameSound.Resume, new ToneSpec[]
            {
                new ToneSpec(220f, 440f, 0.12f, 0.13f, WaveKind.Sine)
            }, 100);

            Add(GameSound.GameOver, new ToneSpec[]
            {
                new ToneSpec(330f, 330f, 0.22f, 0.20f, WaveKind.Saw),
                new ToneSpec(262f, 262f, 0.22f, 0.20f, WaveKind.Saw),
                new ToneSpec(196f, 196f, 0.25f, 0.20f, WaveKind.Saw),
                new ToneSpec(130f, 64f, 0.65f, 0.22f, WaveKind.Sine)
            }, 700);

            Add(GameSound.Victory, new ToneSpec[]
            {
                new ToneSpec(523f, 523f, 0.12f, 0.17f, WaveKind.Sine),
                new ToneSpec(659f, 659f, 0.12f, 0.17f, WaveKind.Sine),
                new ToneSpec(784f, 784f, 0.12f, 0.17f, WaveKind.Sine),
                new ToneSpec(1046f, 1046f, 0.14f, 0.18f, WaveKind.Sine),
                new ToneSpec(1318f, 1318f, 0.50f, 0.20f, WaveKind.Sine)
            }, 700);
        }

        private void Add(GameSound sound, ToneSpec[] tones, int minimumIntervalMs)
        {
            byte[] data = Synthesize(tones);
            SoundSlot slot = new SoundSlot();
            slot.Stream = new MemoryStream(data);
            slot.Player = new SoundPlayer(slot.Stream);
            slot.Player.Load();
            slot.MinimumIntervalMs = minimumIntervalMs;
            slot.LastPlayedTicks = 0;
            _sounds[sound] = slot;
        }

        private void BuildMusic()
        {
            byte[] data = SynthesizeMusic();
            _musicStream = new MemoryStream(data);
            _musicPlayer = new SoundPlayer(_musicStream);
            _musicPlayer.Load();

            // MCI needs a file path. The temporary WAV is removed on dispose.
            try
            {
                string folder = Path.Combine(Path.GetTempPath(), "TypingDead");
                Directory.CreateDirectory(folder);
                _musicPath = Path.Combine(folder, "menu-music-" + _musicAlias + ".wav");
                File.WriteAllBytes(_musicPath, data);
            }
            catch
            {
                _musicPath = null;
            }
        }

        public void ApplySettings(GameSettings settings)
        {
            if (settings == null) return;
            SoundEnabled = settings.SoundEnabled;
            MusicEnabled = settings.MusicEnabled;
        }

        public void Play(GameSound sound)
        {
            if (!Available || !SoundEnabled) return;
            SoundSlot slot;
            if (!_sounds.TryGetValue(sound, out slot)) return;

            long now = StopwatchTicks();
            long minimum = slot.MinimumIntervalMs * TimeSpan.TicksPerMillisecond;
            if (now - slot.LastPlayedTicks < minimum) return;
            slot.LastPlayedTicks = now;

            lock (_sync)
            {
                try
                {
                    slot.Player.Play();
                }
                catch
                {
                    // Audio is optional; gameplay continues if the device fails.
                }
            }
        }

        private static long StopwatchTicks()
        {
            return DateTime.UtcNow.Ticks;
        }

        public void StartMusic()
        {
            if (!Available || !MusicEnabled) return;

            lock (_sync)
            {
                if (MusicPlaying) return;

                if (TryStartMciMusic())
                {
                    MusicPlaying = true;
                    EnsureMusicLoop();
                    return;
                }

                // Fallback for systems where MCI is unavailable. SoundPlayer
                // music can be interrupted by effects, but it is better than
                // silence.
                if (_musicPlayer != null)
                {
                    try
                    {
                        _musicPlayer.PlayLooping();
                        MusicPlaying = true;
                    }
                    catch
                    {
                        MusicPlaying = false;
                    }
                }
            }
        }

        public void StopMusic()
        {
            lock (_sync)
            {
                MusicPlaying = false;

                if (_musicOpen)
                {
                    MciSend("stop " + _musicAlias);
                    MciSend("seek " + _musicAlias + " to start");
                }

                if (_musicPlayer != null)
                {
                    try { _musicPlayer.Stop(); } catch { }
                }
            }
        }

        private bool TryStartMciMusic()
        {
            if (String.IsNullOrEmpty(_musicPath) || !File.Exists(_musicPath)) return false;

            if (!_musicOpen)
            {
                int open = MciSend("open \"" + _musicPath + "\" type waveaudio alias " + _musicAlias);
                if (open != 0) return false;
                _musicOpen = true;
            }

            MciSend("seek " + _musicAlias + " to start");
            return MciSend("play " + _musicAlias) == 0;
        }

        private void EnsureMusicLoop()
        {
            if (_musicLoopTimer == null)
            {
                // MCI device aliases are thread-affine, so the loop check must
                // run on the UI thread that opened the device.
                _musicLoopTimer = new System.Windows.Forms.Timer();
                _musicLoopTimer.Interval = 200;
                _musicLoopTimer.Tick += MusicLoopTick;
                _musicLoopTimer.Start();
            }
        }

        private void MusicLoopTick(object sender, EventArgs e)
        {
            lock (_sync)
            {
                if (!MusicPlaying || !_musicOpen) return;

                StringBuilder mode = new StringBuilder(32);
                int result = mciSendString("status " + _musicAlias + " mode", mode,
                    mode.Capacity, IntPtr.Zero);
                if (result != 0) return;

                if (String.Equals(mode.ToString().Trim(), "stopped", StringComparison.OrdinalIgnoreCase))
                {
                    MciSend("seek " + _musicAlias + " to start");
                    MciSend("play " + _musicAlias);
                }
            }
        }

        private static int MciSend(string command)
        {
            return mciSendString(command, null, 0, IntPtr.Zero);
        }

        public void RefreshMusic(GameSettings settings)
        {
            if (settings.MusicEnabled)
            {
                StartMusic();
            }
            else
            {
                StopMusic();
            }
        }

        public void Dispose()
        {
            foreach (SoundSlot slot in _sounds.Values)
            {
                slot.Dispose();
            }
            _sounds.Clear();

            if (_musicLoopTimer != null)
            {
                try { _musicLoopTimer.Stop(); } catch { }
                try { _musicLoopTimer.Dispose(); } catch { }
                _musicLoopTimer = null;
            }

            lock (_sync)
            {
                MusicPlaying = false;
                if (_musicOpen)
                {
                    MciSend("stop " + _musicAlias);
                    MciSend("close " + _musicAlias);
                    _musicOpen = false;
                }
            }

            if (_musicPlayer != null)
            {
                try { _musicPlayer.Stop(); } catch { }
                try { _musicPlayer.Dispose(); } catch { }
                _musicPlayer = null;
            }
            if (_musicStream != null)
            {
                try { _musicStream.Dispose(); } catch { }
                _musicStream = null;
            }
            if (!String.IsNullOrEmpty(_musicPath))
            {
                try { File.Delete(_musicPath); } catch { }
                _musicPath = null;
            }
        }

        // ------------------------------------------------------------------
        // Tiny PCM synthesiser
        // ------------------------------------------------------------------

        private static byte[] Synthesize(ToneSpec[] tones)
        {
            const int sampleRate = 22050;
            int totalSamples = 0;
            for (int i = 0; i < tones.Length; i++)
            {
                totalSamples += (int)(tones[i].Duration * sampleRate);
            }
            totalSamples = Math.Max(1, totalSamples);

            short[] samples = new short[totalSamples];
            Random noise = new Random(7717 + tones.Length * 19);
            int cursor = 0;

            for (int n = 0; n < tones.Length; n++)
            {
                ToneSpec tone = tones[n];
                int count = Math.Max(1, (int)(tone.Duration * sampleRate));
                float phase = 0f;
                float lastNoise = 0f;

                for (int i = 0; i < count; i++)
                {
                    float progress = (float)i / count;
                    float frequency = tone.StartFrequency + (tone.EndFrequency - tone.StartFrequency) * progress;
                    phase += (float)(Math.PI * 2.0 * frequency / sampleRate);
                    if (phase > Math.PI * 2f) phase -= (float)(Math.PI * 2.0);

                    float wave;
                    switch (tone.Wave)
                    {
                        case WaveKind.Square:
                            wave = Math.Sin(phase) >= 0 ? 1f : -1f;
                            break;
                        case WaveKind.Saw:
                            wave = (float)((phase / (Math.PI * 2.0)) * 2.0 - 1.0);
                            break;
                        case WaveKind.Noise:
                            float white = (float)(noise.NextDouble() * 2.0 - 1.0);
                            lastNoise = lastNoise * 0.55f + white * 0.45f;
                            wave = lastNoise;
                            break;
                        default:
                            wave = (float)Math.Sin(phase);
                            break;
                    }

                    float attack = Math.Min(1f, i / (sampleRate * 0.006f));
                    float release = Math.Min(1f, (count - i) / (sampleRate * 0.035f));
                    float envelope = attack * release;

                    samples[cursor + i] = (short)(wave * envelope * tone.Volume * 32760f);
                }

                cursor += count;
            }

            return BuildWav(samples, sampleRate);
        }

        private static byte[] SynthesizeMusic()
        {
            const int sampleRate = 22050;
            const float duration = 8f;
            int total = (int)(sampleRate * duration);
            short[] samples = new short[total];
            Random rng = new Random(2129);

            float[] padNotes = new float[] { 55f, 65.41f, 73.42f, 49f };
            float[] arpNotes = new float[] { 220f, 261.63f, 329.63f, 392f, 329.63f, 261.63f, 246.94f, 196f };

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                int chordIndex = ((int)(t / 2f)) % padNotes.Length;
                float padFrequency = padNotes[chordIndex];
                float pad = (float)(Math.Sin(2.0 * Math.PI * padFrequency * t) * 0.55 +
                                    Math.Sin(2.0 * Math.PI * padFrequency * 2.0 * t + 0.7) * 0.22 +
                                    Math.Sin(2.0 * Math.PI * padFrequency * 0.5 * t + 1.1) * 0.32);

                float arpStep = t * 4f;
                int arpIndex = ((int)arpStep) % arpNotes.Length;
                float arpProgress = arpStep - (float)Math.Floor(arpStep);
                float arp = (float)Math.Sin(2.0 * Math.PI * arpNotes[arpIndex] * t);
                arp *= (float)Math.Exp(-arpProgress * 4.4f) * 0.20f;

                float shimmer = (float)Math.Sin(2.0 * Math.PI * 1760f * t) * 0.018f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.004f;
                float value = pad * 0.075f + arp + shimmer + noise;

                // Gentle loop-edge crossfade prevents a click when looping.
                float edge = Math.Min(1f, Math.Min(t / 0.08f, (duration - t) / 0.08f));
                value *= edge;

                samples[i] = (short)(value * 32760f);
            }

            return BuildWav(samples, sampleRate);
        }

        private static byte[] BuildWav(short[] samples, int sampleRate)
        {
            int dataSize = samples.Length * 2;
            byte[] wav = new byte[44 + dataSize];
            int cursor = 0;

            WriteAscii(wav, ref cursor, "RIFF");
            WriteInt(wav, ref cursor, 36 + dataSize);
            WriteAscii(wav, ref cursor, "WAVE");
            WriteAscii(wav, ref cursor, "fmt ");
            WriteInt(wav, ref cursor, 16);
            WriteShort(wav, ref cursor, 1);          // PCM
            WriteShort(wav, ref cursor, 1);          // mono
            WriteInt(wav, ref cursor, sampleRate);
            WriteInt(wav, ref cursor, sampleRate * 2);
            WriteShort(wav, ref cursor, 2);
            WriteShort(wav, ref cursor, 16);
            WriteAscii(wav, ref cursor, "data");
            WriteInt(wav, ref cursor, dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                short value = samples[i];
                wav[cursor++] = (byte)(value & 0xFF);
                wav[cursor++] = (byte)((value >> 8) & 0xFF);
            }

            return wav;
        }

        private static void WriteAscii(byte[] buffer, ref int cursor, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                buffer[cursor++] = (byte)text[i];
            }
        }

        private static void WriteInt(byte[] buffer, ref int cursor, int value)
        {
            buffer[cursor++] = (byte)(value & 0xFF);
            buffer[cursor++] = (byte)((value >> 8) & 0xFF);
            buffer[cursor++] = (byte)((value >> 16) & 0xFF);
            buffer[cursor++] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteShort(byte[] buffer, ref int cursor, short value)
        {
            buffer[cursor++] = (byte)(value & 0xFF);
            buffer[cursor++] = (byte)((value >> 8) & 0xFF);
        }
    }
}
