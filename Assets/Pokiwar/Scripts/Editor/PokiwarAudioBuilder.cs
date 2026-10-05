using System;
using System.Collections.Generic;
using System.IO;
using GameAudio;
using Pokiwar.App;
using UnityEditor;
using UnityEngine;

namespace Pokiwar.EditorTools
{
    /// <summary>Generates every Pokiwar sound and music loop from SfxSynth recipes. Existing files are kept unless OverwriteExisting.</summary>
    public static class PokiwarAudioBuilder
    {
        public const string Folder = "Assets/Pokiwar/Audio";
        public static bool OverwriteExisting;

        private const int SR = SfxSynth.SampleRate;

        private sealed class Entry
        {
            public string Key;
            public string Path;
            public bool Music;
            public float Volume;
        }

        [MenuItem("Pokiwar/Regenerate Audio (overwrites)")]
        public static void RegenerateMenu()
        {
            OverwriteExisting = true;
            try { BuildAll(); }
            finally { OverwriteExisting = false; }
        }

        public static List<KeyedClip> BuildAll()
        {
            Directory.CreateDirectory(Folder);
            var list = new List<Entry>();

            void Sfx(string key, float volume, Func<float[]> make) => list.Add(Write(key, false, volume, make));
            void Music(string key, float volume, Func<float[]> make) => list.Add(Write(key, true, volume, make));

            Sfx("ui.click", 0.55f, () => R(new SfxParams { Wave = SfxWave.Square, Duty = 0.5f, BaseFreq = 0.55f, Sustain = 0.02f, Decay = 0.09f, HpfFreq = 0.1f }));
            Sfx("swap", 0.6f, () => R(new SfxParams { Wave = SfxWave.Sine, BaseFreq = 0.32f, FreqRamp = 0.22f, Sustain = 0.04f, Decay = 0.13f }));
            Sfx("invalid", 0.4f, () => Seq(SfxWave.Square, 0.25f, new[] { 50, 46 }, 0.09, 0.11));
            Sfx("match", 0.75f, () => R(new SfxParams { Wave = SfxWave.Square, Duty = 0.35f, BaseFreq = 0.42f, Sustain = 0.03f, Decay = 0.2f, Punch = 0.45f, ArpSpeed = 0.62f, ArpMod = 0.32f }));
            Sfx("shuffle", 0.6f, () => R(new SfxParams { Wave = SfxWave.Noise, BaseFreq = 0.45f, FreqRamp = 0.18f, Attack = 0.12f, Sustain = 0.25f, Decay = 0.3f, LpfFreq = 0.55f, LpfRamp = 0.2f }, 7));
            Sfx("heal", 0.7f, () => Arp(SfxWave.Sine, new[] { 84, 88, 91 }, 0.06, 0.18, 0.3f));
            Sfx("mana", 0.65f, () => R(new SfxParams { Wave = SfxWave.Sine, BaseFreq = 0.38f, FreqRamp = 0.14f, VibStrength = 0.25f, VibSpeed = 0.5f, Sustain = 0.1f, Decay = 0.2f }));
            Sfx("rage", 0.7f, () => R(new SfxParams { Wave = SfxWave.Saw, Duty = 1f, BaseFreq = 0.2f, FreqRamp = 0.12f, Sustain = 0.12f, Decay = 0.2f, Punch = 0.5f, LpfFreq = 0.6f }));
            Sfx("shield", 0.7f, () => Layer(Tone(81, 0.35, SfxWave.Triangle, 0.4f, 0.004, 0.3), Tone(93, 0.25, SfxWave.Square, 0.08f, 0.004, 0.22)));
            Sfx("block", 0.85f, () => Layer(R(new SfxParams { Wave = SfxWave.Noise, BaseFreq = 0.6f, Sustain = 0.02f, Decay = 0.16f, HpfFreq = 0.35f }, 3), Tone(86, 0.12, SfxWave.Triangle, 0.5f, 0.001, 0.1)));
            Sfx("steal", 0.7f, () => R(new SfxParams { Wave = SfxWave.Sine, BaseFreq = 0.6f, FreqRamp = -0.25f, Sustain = 0.1f, Decay = 0.22f, VibStrength = 0.15f, VibSpeed = 0.6f }));
            Sfx("swing", 0.6f, () => R(new SfxParams { Wave = SfxWave.Noise, BaseFreq = 0.3f, FreqRamp = 0.25f, Attack = 0.14f, Sustain = 0.05f, Decay = 0.18f, LpfFreq = 0.45f, LpfRamp = 0.3f }, 11));
            Sfx("hit", 0.85f, () => R(new SfxParams { Wave = SfxWave.Noise, BaseFreq = 0.35f, FreqRamp = -0.4f, Sustain = 0.05f, Decay = 0.2f, Punch = 0.4f }, 5));
            Sfx("hit.strong", 1f, () => Layer(R(new SfxParams { Wave = SfxWave.Noise, BaseFreq = 0.14f, FreqRamp = -0.1f, Sustain = 0.2f, Decay = 0.42f, Punch = 0.6f }, 9), Slide(90, 40, 0.3, 0.6f)));
            Sfx("card", 0.7f, () => Concat(R(new SfxParams { Wave = SfxWave.Square, Duty = 0.3f, BaseFreq = 0.48f, FreqRamp = 0.2f, Sustain = 0.03f, Decay = 0.08f }), R(new SfxParams { Wave = SfxWave.Square, Duty = 0.3f, BaseFreq = 0.6f, FreqRamp = 0.2f, Sustain = 0.03f, Decay = 0.1f })));
            Sfx("skill", 0.85f, () => R(new SfxParams { Wave = SfxWave.Saw, Duty = 1f, BaseFreq = 0.3f, FreqRamp = 0.25f, RepeatSpeed = 0.55f, Sustain = 0.25f, Decay = 0.3f }));
            Sfx("buff", 0.7f, () => Arp(SfxWave.Triangle, new[] { 72, 76, 79, 84 }, 0.055, 0.16, 0.35f));
            Sfx("summon", 0.75f, () => Layer(R(new SfxParams { Wave = SfxWave.Square, Duty = 0.2f, BaseFreq = 0.35f, FreqRamp = 0.15f, VibStrength = 0.35f, VibSpeed = 0.7f, Sustain = 0.2f, Decay = 0.3f }), Arp(SfxWave.Sine, new[] { 88, 91, 96 }, 0.07, 0.2, 0.2f)));
            Sfx("transform", 1f, () => MixAt(R(new SfxParams { Wave = SfxWave.Saw, Duty = 1f, BaseFreq = 0.15f, FreqRamp = 0.12f, VibStrength = 0.2f, VibSpeed = 0.4f, Sustain = 0.6f, Decay = 0.4f, LpfFreq = 0.7f, LpfRamp = 0.15f }),
                R(new SfxParams { Wave = SfxWave.Noise, BaseFreq = 0.12f, FreqRamp = -0.05f, Sustain = 0.25f, Decay = 0.5f, Punch = 0.7f }, 13), 0.75));
            Sfx("death", 0.85f, () => R(new SfxParams { Wave = SfxWave.Square, Duty = 0.4f, BaseFreq = 0.4f, FreqRamp = -0.2f, Sustain = 0.3f, Decay = 0.45f, Punch = 0.3f, VibStrength = 0.2f, VibSpeed = 0.5f }));
            Sfx("fight", 0.9f, () => MixAt(Chord(SfxWave.Saw, new[] { 55, 59, 62 }, 0.14, 0.2f), Chord(SfxWave.Saw, new[] { 60, 64, 67 }, 0.5, 0.2f), 0.18));
            Sfx("turn", 0.55f, () => Layer(Tone(88, 0.3, SfxWave.Sine, 0.4f, 0.003, 0.28), Tone(95, 0.22, SfxWave.Sine, 0.12f, 0.003, 0.2)));
            Sfx("tick", 0.3f, () => Tone(84, 0.035, SfxWave.Square, 0.3f, 0.001, 0.025));
            Sfx("timeout", 0.45f, () => Seq(SfxWave.Square, 0.25f, new[] { 64, 57 }, 0.1, 0.16));
            Sfx("victory", 0.7f, () => MixAt(Arp(SfxWave.Square, new[] { 72, 76, 79 }, 0.11, 0.12, 0.18f), Chord(SfxWave.Square, new[] { 84, 88, 91 }, 0.65, 0.12f, 0.25), 0.33));
            Sfx("defeat", 0.6f, () => Seq(SfxWave.Triangle, 0.4f, new[] { 67, 66, 65, 64 }, 0.22, 0.5));
            Sfx("qte.ok", 0.6f, () => Tone(84, 0.07, SfxWave.Sine, 0.4f, 0.002, 0.06));
            Sfx("qte.bad", 0.42f, () => Tone(45, 0.14, SfxWave.Square, 0.3f, 0.002, 0.08, 0.3f));
            Sfx("qte.perfect", 1f, () => Layer(Arp(SfxWave.Square, new[] { 79, 84, 88, 91, 96 }, 0.045, 0.2, 0.15f), R(new SfxParams { Wave = SfxWave.Saw, Duty = 1f, BaseFreq = 0.5f, Sustain = 0.05f, Decay = 0.3f, Punch = 0.5f, ArpSpeed = 0.55f, ArpMod = 0.3f })));
            Sfx("qte.good", 0.8f, () => Arp(SfxWave.Square, new[] { 76, 83 }, 0.07, 0.16, 0.2f));
            Sfx("qte.miss", 0.75f, () => Slide(300, 120, 0.3, 0.35f, SfxWave.Triangle));
            Sfx("upgrade.ok", 0.9f, () => MixAt(R(new SfxParams { Wave = SfxWave.Square, Duty = 0.3f, BaseFreq = 0.3f, FreqRamp = 0.2f, RepeatSpeed = 0.6f, Sustain = 0.2f, Decay = 0.2f }), Chord(SfxWave.Triangle, new[] { 72, 76, 79, 84 }, 0.6, 0.25f, 0.3), 0.3));
            Sfx("upgrade.fail", 0.85f, () => Layer(Slide(110, 50, 0.4, 0.6f), Seq(SfxWave.Square, 0.15f, new[] { 55, 54 }, 0.14, 0.2)));

            Music("music.menu", 0.8f, () => Song(100, new[] { 48, 45, 41, 43 }, false, false));
            Music("music.battle", 0.8f, () => Song(140, new[] { 45, 41, 48, 43 }, true, false));
            Music("music.boss", 0.85f, () => Song(150, new[] { 38, 46, 48, 45 }, true, true));

            AssetDatabase.Refresh();
            var result = new List<KeyedClip>();
            foreach (var e in list)
            {
                Configure(e.Path, e.Music);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(e.Path);
                result.Add(new KeyedClip { Key = e.Key, Clip = clip, Volume = e.Volume });
            }
            return result;
        }

        private static Entry Write(string key, bool music, float volume, Func<float[]> make)
        {
            string path = Folder + "/" + (music ? "" : "sfx_") + key.Replace('.', '_') + ".wav";
            if (OverwriteExisting || !File.Exists(path))
            {
                var s = make();
                s = music ? SfxSynth.Normalize(s, -6f, 0) : SfxSynth.Normalize(TrimHead(s), -3f, 6);
                File.WriteAllBytes(path, SfxSynth.ToWav(s));
            }
            return new Entry { Key = key, Path = path, Music = music, Volume = volume };
        }

        private static void Configure(string path, bool music)
        {
            var ai = (AudioImporter)AssetImporter.GetAtPath(path);
            if (ai == null) return;
            var s = ai.defaultSampleSettings;
            if (music)
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                ai.loadInBackground = true;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
                ai.loadInBackground = false;
            }
            ai.forceToMono = true;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }

        private static float[] R(SfxParams p, uint seed = 1) => SfxSynth.Render(p, seed);

        private static float[] Tone(int midi, double seconds, SfxWave wave, float vol, double attack, double release, float duty = 0.5f) =>
            SfxSynth.Tone(SfxSynth.NoteHz(midi), seconds, wave, vol, attack, release, duty);

        private static float[] Concat(params float[][] parts) => SfxSynth.Concat(parts);

        private static float[] Layer(params float[][] parts)
        {
            var dst = new float[0];
            foreach (var p in parts) dst = SfxSynth.MixInto(dst, p, 0);
            return dst;
        }

        private static float[] MixAt(float[] a, float[] b, double offset) => SfxSynth.MixInto((float[])a.Clone(), b, offset);

        private static float[] Seq(SfxWave wave, float vol, int[] notes, double step, double last)
        {
            var dst = new float[0];
            for (int i = 0; i < notes.Length; i++)
            {
                double len = i == notes.Length - 1 ? last : step;
                dst = SfxSynth.MixInto(dst, Tone(notes[i], len, wave, vol, 0.003, Math.Min(0.06, len * 0.5)), i * step);
            }
            return dst;
        }

        private static float[] Arp(SfxWave wave, int[] notes, double step, double tail, float vol)
        {
            var dst = new float[0];
            for (int i = 0; i < notes.Length; i++)
            {
                double len = i == notes.Length - 1 ? tail : step * 1.6;
                dst = SfxSynth.MixInto(dst, Tone(notes[i], len, wave, vol, 0.002, len * 0.7), i * step);
            }
            return dst;
        }

        private static float[] Chord(SfxWave wave, int[] notes, double seconds, float vol, double release = 0.12)
        {
            var dst = new float[0];
            foreach (var n in notes) dst = SfxSynth.MixInto(dst, Tone(n, seconds, wave, vol, 0.004, release, 0.25f), 0);
            return dst;
        }

        private static float[] Slide(double fromHz, double toHz, double seconds, float vol, SfxWave wave = SfxWave.Sine)
        {
            int n = (int)(seconds * SR);
            var s = new float[n];
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / n;
                double hz = fromHz * Math.Pow(toHz / fromHz, t);
                double v = wave == SfxWave.Triangle ? 1 - 4 * Math.Abs(ph - 0.5) : Math.Sin(ph * 2 * Math.PI);
                s[i] = (float)(v * vol * Math.Min(1, i / (0.003 * SR)) * (1 - t));
                ph += hz / SR;
                if (ph >= 1) ph -= 1;
            }
            return s;
        }

        private static float[] TrimHead(float[] s)
        {
            int i = 0;
            while (i < s.Length && Math.Abs(s[i]) < 0.0005f) i++;
            if (i == 0) return s;
            var r = new float[s.Length - i];
            Array.Copy(s, i, r, 0, r.Length);
            return r;
        }

        private static void Wrap(float[] dst, float[] src, int offset, float gain)
        {
            for (int i = 0; i < src.Length; i++) dst[(offset + i) % dst.Length] += src[i] * gain;
        }

        /// <summary>Two passes over a four-chord progression, one bar per chord. Length is exactly bars * beat, so it loops on the sample.</summary>
        private static float[] Song(int bpm, int[] roots, bool driving, bool dark)
        {
            int eighth = (int)Math.Round(SR * 60.0 / bpm / 2);
            int bars = roots.Length * 2;
            var s = new float[bars * 8 * eighth];
            double e = (double)eighth / SR;
            int[] arpMajor = { 0, 4, 7, 12, 7, 4, 0, 7 };
            int[] arpMinor = { 0, 3, 7, 12, 7, 3, 0, 7 };
            for (int bar = 0; bar < bars; bar++)
            {
                int root = roots[bar % roots.Length];
                bool minor = dark ? bar % roots.Length != 2 : (root % 12 == 9 || root % 12 == 2);
                var arp = minor ? arpMinor : arpMajor;
                int b0 = bar * 8 * eighth;
                for (int k = 0; k < 8; k++)
                {
                    int at = b0 + k * eighth;
                    if (driving || k % 2 == 0)
                    {
                        int bassNote = root - 12 + (driving && k % 4 == 3 ? 12 : 0);
                        Wrap(s, Tone(bassNote, e * (driving ? 0.9 : 1.8), dark ? SfxWave.Saw : SfxWave.Triangle, dark ? 0.16f : 0.32f, 0.004, 0.04), at, 1f);
                    }
                    Wrap(s, Tone(root + 24 + arp[k], e * 0.85, SfxWave.Square, 0.07f, 0.003, e * 0.5, 0.25f), at, 1f);
                    if (driving)
                    {
                        Wrap(s, Tone(100, 0.03, SfxWave.Noise, k % 2 == 1 ? 0.12f : 0.06f, 0.001, 0.025), at, 1f);
                        if (k == 0 || k == 4) Wrap(s, Slide(140, 45, 0.12, 0.5f), at, 1f);
                        if (k == 2 || k == 6) Wrap(s, Tone(70, 0.08, SfxWave.Noise, 0.18f, 0.001, 0.07), at, 1f);
                    }
                    else if (k == 0)
                    {
                        Wrap(s, Tone(root + 12, e * 7.5, SfxWave.Triangle, 0.1f, 0.05, e * 3), at, 1f);
                    }
                }
            }
            return s;
        }
    }
}
