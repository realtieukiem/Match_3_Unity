using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pokiwar.App
{
    [Serializable]
    public sealed class KeyedClip
    {
        public string Key;
        public AudioClip Clip;
        [Range(0f, 1.5f)] public float Volume = 1f;
    }

    /// <summary>Key-based sound service: pooled one-shot voices, music source, SFX/music buses in dB, per-key cooldown.</summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        public List<KeyedClip> Clips = new List<KeyedClip>();
        [Tooltip("Simultaneous one-shot voices.")]
        public int Voices = 12;
        [Tooltip("SFX bus level in dB.")]
        public float SfxBusDb = -2f;
        [Tooltip("Music bus level in dB.")]
        public float MusicBusDb = -9f;
        [Tooltip("Same key cannot restart faster than this (seconds).")]
        public float KeyCooldown = 0.04f;
        [Tooltip("Max copies of one key playing at once.")]
        public int MaxPerKey = 3;
        [Tooltip("Random pitch spread on jittered sounds (0.05 = +/-5%).")]
        public float PitchJitter = 0.05f;

        public bool MusicOn { get; private set; } = true;
        public bool SfxOn { get; private set; } = true;
        public int TotalPlays { get; private set; }
        public readonly HashSet<string> MissingKeys = new HashSet<string>();
        public readonly Dictionary<string, int> PlayCounts = new Dictionary<string, int>();

        private readonly Dictionary<string, KeyedClip> map = new Dictionary<string, KeyedClip>();
        private readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        private AudioSource[] voices;
        private string[] voiceKeys;
        private float[] voiceStarted;
        private AudioSource music;
        private string musicKey;
        private float duckDb;
        private float duckUntil;

        private void Awake()
        {
            Instance = this;
            foreach (var c in Clips) if (c != null && !string.IsNullOrEmpty(c.Key)) map[c.Key] = c;
            voices = new AudioSource[Mathf.Max(1, Voices)];
            voiceKeys = new string[voices.Length];
            voiceStarted = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static float DbToLinear(float db) => Mathf.Pow(10f, db / 20f);

        public void Apply(bool musicOn, bool sfxOn)
        {
            MusicOn = musicOn;
            SfxOn = sfxOn;
            UpdateMusicVolume();
        }

        public bool Has(string key) => map.ContainsKey(key);

        /// <summary>Plays a one-shot. pitch multiplies the clip pitch; jitter adds a small random spread.</summary>
        public void Play(string key, float pitch = 1f, float volume = 1f, bool jitter = true)
        {
            if (!map.TryGetValue(key, out var kc) || kc.Clip == null)
            {
                if (MissingKeys.Add(key)) Debug.LogWarning("[Pokiwar Audio] missing clip key: " + key);
                return;
            }
            float now = Time.unscaledTime;
            if (lastPlay.TryGetValue(key, out var last) && now - last < KeyCooldown) return;
            lastPlay[key] = now;
            TotalPlays++;
            PlayCounts[key] = PlayCounts.TryGetValue(key, out var n) ? n + 1 : 1;
            if (!SfxOn) return;

            int same = 0, free = -1, oldest = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                bool busy = voices[i].isPlaying;
                if (busy && voiceKeys[i] == key) same++;
                if (!busy && free < 0) free = i;
                if (voiceStarted[i] < voiceStarted[oldest]) oldest = i;
            }
            if (same >= MaxPerKey) return;
            int v = free >= 0 ? free : oldest;
            var src = voices[v];
            src.clip = kc.Clip;
            src.pitch = pitch * (jitter ? 1f + UnityEngine.Random.Range(-PitchJitter, PitchJitter) : 1f);
            src.volume = Mathf.Clamp01(DbToLinear(SfxBusDb) * kc.Volume * volume);
            src.Play();
            voiceKeys[v] = key;
            voiceStarted[v] = now;
        }

        public void PlayMusic(string key)
        {
            if (musicKey == key && music.isPlaying) return;
            musicKey = key;
            if (!map.TryGetValue(key, out var kc) || kc.Clip == null)
            {
                if (MissingKeys.Add(key)) Debug.LogWarning("[Pokiwar Audio] missing music key: " + key);
                music.Stop();
                return;
            }
            music.clip = kc.Clip;
            UpdateMusicVolume();
            music.Play();
        }

        public void StopMusic()
        {
            musicKey = null;
            music.Stop();
        }

        /// <summary>Dips the music bus for a big moment, then recovers.</summary>
        public void Duck(float db, float seconds)
        {
            duckDb = db;
            duckUntil = Time.unscaledTime + seconds;
        }

        private void Update()
        {
            if (duckDb != 0f && Time.unscaledTime > duckUntil)
            {
                duckDb = Mathf.MoveTowards(duckDb, 0f, Time.unscaledDeltaTime * 20f);
            }
            UpdateMusicVolume();
        }

        private void UpdateMusicVolume()
        {
            if (music == null) return;
            float vol = MusicOn && musicKey != null && map.TryGetValue(musicKey, out var kc) ? DbToLinear(MusicBusDb + duckDb) * kc.Volume : 0f;
            music.volume = Mathf.Clamp01(vol);
        }

        public static void Sfx(string key, float pitch = 1f, float volume = 1f, bool jitter = true)
        {
            if (Instance != null) Instance.Play(key, pitch, volume, jitter);
        }
    }
}
