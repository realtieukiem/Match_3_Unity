using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GameAudio
{
    public enum SfxWave { Square = 0, Saw = 1, Sine = 2, Noise = 3, Triangle = 4 }

    /// <summary>sfxr parameter set, normalised 0..1 (signed fields -1..1), same meaning as jsfxr.</summary>
    [Serializable]
    public class SfxParams
    {
        public SfxWave Wave = SfxWave.Square;
        public float BaseFreq = 0.3f;
        public float FreqLimit;
        public float FreqRamp;
        public float FreqDRamp;
        public float Duty;
        public float DutyRamp;
        public float VibStrength;
        public float VibSpeed;
        public float Attack;
        public float Sustain = 0.3f;
        public float Punch;
        public float Decay = 0.4f;
        public float LpfFreq = 1f;
        public float LpfRamp;
        public float LpfResonance;
        public float HpfFreq;
        public float HpfRamp;
        public float PhaOffset;
        public float PhaRamp;
        public float RepeatSpeed;
        public float ArpSpeed;
        public float ArpMod;
        public float Volume = 0.5f;

        public SfxParams Clone() => (SfxParams)MemberwiseClone();
    }

    /// <summary>Deterministic sfxr port plus a small tone/mix toolkit for jingles and loops. No engine dependency.</summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;
        private const int Oversampling = 8;
        private const int MaxSamples = SampleRate * 12;

        private sealed class Rng
        {
            private uint s;
            public Rng(uint seed) { s = seed * 2654435761u + 0x9E3779B9u; if (s == 0) s = 1; }
            public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }
            public float F(float range) => Next() * range;
            public bool Coin() => Next() < 0.5f;
            public int I(int maxInclusive) => (int)(Next() * (maxInclusive + 1));
        }

        public static float[] Render(SfxParams p, uint seed = 1)
        {
            var rng = new Rng(seed ^ 0xA5A5u);
            var output = new List<float>(SampleRate);

            double period = 0, periodMax = 0, periodMult = 0, periodMultSlide = 0, dutyCycle = 0, dutyCycleSlide = 0, arpMult = 0;
            int arpTime = 0, elapsedSinceRepeat = 0;
            bool freqCutoff = false;
            void InitForRepeat()
            {
                elapsedSinceRepeat = 0;
                period = 100.0 / (p.BaseFreq * p.BaseFreq + 0.001);
                periodMax = 100.0 / (p.FreqLimit * p.FreqLimit + 0.001);
                freqCutoff = p.FreqLimit > 0;
                periodMult = 1 - Math.Pow(p.FreqRamp, 3) * 0.01;
                periodMultSlide = -Math.Pow(p.FreqDRamp, 3) * 0.000001;
                dutyCycle = 0.5 - p.Duty * 0.5;
                dutyCycleSlide = -p.DutyRamp * 0.00005;
                arpMult = p.ArpMod >= 0 ? 1 - Math.Pow(p.ArpMod, 2) * 0.9 : 1 + Math.Pow(p.ArpMod, 2) * 10;
                arpTime = (int)Math.Floor(Math.Pow(1 - p.ArpSpeed, 2) * 20000 + 32);
                if (p.ArpSpeed >= 1f) arpTime = 0;
            }
            InitForRepeat();

            double fltw = Math.Pow(p.LpfFreq, 3) * 0.1;
            bool lpf = p.LpfFreq < 1f;
            double fltwD = 1 + p.LpfRamp * 0.0001;
            double fltdmp = Math.Min(0.8, 5 / (1 + Math.Pow(p.LpfResonance, 2) * 20) * (0.01 + fltw));
            double flthp = Math.Pow(p.HpfFreq, 2) * 0.1;
            double flthpD = 1 + p.HpfRamp * 0.0003;
            double vibSpeed = Math.Pow(p.VibSpeed, 2) * 0.01;
            double vibAmp = p.VibStrength * 0.5;
            int[] envLen =
            {
                (int)Math.Floor(p.Attack * p.Attack * 100000.0),
                (int)Math.Floor(p.Sustain * p.Sustain * 100000.0),
                (int)Math.Floor(p.Decay * p.Decay * 100000.0)
            };
            double flangerOffset = Math.Pow(p.PhaOffset, 2) * 1020 * Math.Sign(p.PhaOffset);
            double flangerSlide = Math.Pow(p.PhaRamp, 2) * Math.Sign(p.PhaRamp);
            int repeatTime = p.RepeatSpeed <= 0 ? 0 : (int)Math.Floor(Math.Pow(1 - p.RepeatSpeed, 2) * 20000 + 32);
            double gain = Math.Exp(p.Volume) - 1;

            var noise = new double[32];
            for (int i = 0; i < 32; i++) noise[i] = rng.Next() * 2 - 1;
            var flanger = new double[1024];
            double fltp = 0, fltdp = 0, fltphp = 0, vibPhase = 0;
            int envStage = 0, envElapsed = 0, phase = 0, ipp = 0;

            for (int t = 0; output.Count < MaxSamples; t++)
            {
                if (repeatTime != 0 && ++elapsedSinceRepeat >= repeatTime) InitForRepeat();
                if (arpTime != 0 && t >= arpTime)
                {
                    arpTime = 0;
                    period *= arpMult;
                }
                periodMult += periodMultSlide;
                period *= periodMult;
                if (period > periodMax)
                {
                    period = periodMax;
                    if (freqCutoff) break;
                }
                double rfperiod = period;
                if (vibAmp > 0)
                {
                    vibPhase += vibSpeed;
                    rfperiod = period * (1 + Math.Sin(vibPhase) * vibAmp);
                }
                int iperiod = Math.Max(Oversampling, (int)Math.Floor(rfperiod));
                dutyCycle = Math.Max(0, Math.Min(0.5, dutyCycle + dutyCycleSlide));

                if (++envElapsed > envLen[envStage])
                {
                    envElapsed = 0;
                    if (++envStage > 2) break;
                }
                double envf = envLen[envStage] == 0 ? 1 : (double)envElapsed / envLen[envStage];
                double env = envStage == 0 ? envf : envStage == 1 ? 1 + (1 - envf) * 2 * p.Punch : 1 - envf;

                flangerOffset += flangerSlide;
                int iphase = Math.Min(1023, Math.Abs((int)Math.Floor(flangerOffset)));
                if (flthpD != 1)
                {
                    flthp *= flthpD;
                    flthp = Math.Max(0.00001, Math.Min(0.1, flthp));
                }

                double sample = 0;
                for (int si = 0; si < Oversampling; si++)
                {
                    double sub;
                    phase++;
                    if (phase >= iperiod)
                    {
                        phase %= iperiod;
                        if (p.Wave == SfxWave.Noise)
                            for (int i = 0; i < 32; i++) noise[i] = rng.Next() * 2 - 1;
                    }
                    double fp = (double)phase / iperiod;
                    switch (p.Wave)
                    {
                        case SfxWave.Square: sub = fp < dutyCycle ? 0.5 : -0.5; break;
                        case SfxWave.Saw:
                            sub = dutyCycle <= 0 ? 1 - 2 * fp : fp < dutyCycle ? -1 + 2 * fp / dutyCycle : 1 - 2 * (fp - dutyCycle) / (1 - dutyCycle);
                            break;
                        case SfxWave.Sine: sub = Math.Sin(fp * 2 * Math.PI); break;
                        case SfxWave.Triangle: sub = 1 - 4 * Math.Abs(fp - 0.5); break;
                        default: sub = noise[(int)Math.Floor(phase * 32.0 / iperiod) & 31]; break;
                    }
                    double pp = fltp;
                    fltw = Math.Max(0, Math.Min(0.1, fltw * fltwD));
                    if (lpf)
                    {
                        fltdp += (sub - fltp) * fltw;
                        fltdp -= fltdp * fltdmp;
                    }
                    else
                    {
                        fltp = sub;
                        fltdp = 0;
                    }
                    fltp += fltdp;
                    fltphp += fltp - pp;
                    fltphp -= fltphp * flthp;
                    sub = fltphp;
                    flanger[ipp & 1023] = sub;
                    sub += flanger[(ipp - iphase + 1024) & 1023];
                    ipp = (ipp + 1) & 1023;
                    sample += sub * env;
                }
                output.Add((float)(sample / Oversampling * gain));
            }
            return output.ToArray();
        }

        public static SfxParams PickupCoin(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams { Wave = SfxWave.Saw, BaseFreq = 0.4f + r.F(0.5f), Sustain = r.F(0.1f), Decay = 0.1f + r.F(0.4f), Punch = 0.3f + r.F(0.3f) };
            if (r.Coin()) { p.ArpSpeed = 0.5f + r.F(0.2f); p.ArpMod = 0.2f + r.F(0.4f); }
            return p;
        }

        public static SfxParams Laser(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams { Wave = (SfxWave)r.I(2) };
            if (r.I(2) == 0) { p.BaseFreq = 0.3f + r.F(0.6f); p.FreqLimit = r.F(0.1f); p.FreqRamp = -0.35f - r.F(0.3f); }
            else { p.BaseFreq = 0.5f + r.F(0.5f); p.FreqLimit = Math.Max(0.2f, p.BaseFreq - 0.2f - r.F(0.6f)); p.FreqRamp = -0.15f - r.F(0.2f); }
            if (r.Coin()) { p.Duty = r.F(0.5f); p.DutyRamp = r.F(0.2f); } else { p.Duty = 0.4f + r.F(0.5f); p.DutyRamp = -r.F(0.7f); }
            p.Sustain = 0.1f + r.F(0.2f);
            p.Decay = r.F(0.4f);
            if (r.Coin()) p.Punch = r.F(0.3f);
            if (r.I(2) == 0) { p.PhaOffset = r.F(0.2f); p.PhaRamp = -r.F(0.2f); }
            p.HpfFreq = r.F(0.3f);
            return p;
        }

        public static SfxParams Explosion(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams { Wave = SfxWave.Noise };
            if (r.Coin()) { float b = 0.1f + r.F(0.4f); p.BaseFreq = b * b; p.FreqRamp = -0.1f + r.F(0.4f); }
            else { float b = 0.2f + r.F(0.7f); p.BaseFreq = b * b; p.FreqRamp = -0.2f - r.F(0.2f); }
            if (r.I(4) == 0) p.FreqRamp = 0;
            if (r.I(2) == 0) p.RepeatSpeed = 0.3f + r.F(0.5f);
            p.Sustain = 0.1f + r.F(0.3f);
            p.Decay = r.F(0.5f);
            if (r.Coin()) { p.PhaOffset = -0.3f + r.F(0.9f); p.PhaRamp = -r.F(0.3f); }
            p.Punch = 0.2f + r.F(0.6f);
            if (r.Coin()) { p.VibStrength = r.F(0.7f); p.VibSpeed = r.F(0.6f); }
            if (r.I(2) == 0) { p.ArpSpeed = 0.6f + r.F(0.3f); p.ArpMod = 0.8f - r.F(1.6f); }
            return p;
        }

        public static SfxParams PowerUp(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams();
            if (r.Coin()) { p.Wave = SfxWave.Saw; p.Duty = 1; } else p.Duty = r.F(0.6f);
            p.BaseFreq = 0.2f + r.F(0.3f);
            if (r.Coin()) { p.FreqRamp = 0.1f + r.F(0.4f); p.RepeatSpeed = 0.4f + r.F(0.4f); }
            else { p.FreqRamp = 0.05f + r.F(0.2f); if (r.Coin()) { p.VibStrength = r.F(0.7f); p.VibSpeed = r.F(0.6f); } }
            p.Sustain = r.F(0.4f);
            p.Decay = 0.1f + r.F(0.4f);
            return p;
        }

        public static SfxParams HitHurt(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams { Wave = (SfxWave)r.I(2) };
            if (p.Wave == SfxWave.Sine) p.Wave = SfxWave.Noise;
            if (p.Wave == SfxWave.Square) p.Duty = r.F(0.6f);
            if (p.Wave == SfxWave.Saw) p.Duty = 1;
            p.BaseFreq = 0.2f + r.F(0.6f);
            p.FreqRamp = -0.3f - r.F(0.4f);
            p.Sustain = r.F(0.1f);
            p.Decay = 0.1f + r.F(0.2f);
            if (r.Coin()) p.HpfFreq = r.F(0.3f);
            return p;
        }

        public static SfxParams Jump(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams { Wave = SfxWave.Square, Duty = r.F(0.6f), BaseFreq = 0.3f + r.F(0.3f), FreqRamp = 0.1f + r.F(0.2f), Sustain = 0.1f + r.F(0.3f), Decay = 0.1f + r.F(0.2f) };
            if (r.Coin()) p.HpfFreq = r.F(0.3f);
            if (r.Coin()) p.LpfFreq = 1 - r.F(0.6f);
            return p;
        }

        public static SfxParams Blip(uint seed)
        {
            var r = new Rng(seed);
            var p = new SfxParams { Wave = (SfxWave)r.I(1), BaseFreq = 0.2f + r.F(0.4f), Sustain = 0.1f + r.F(0.1f), Decay = r.F(0.2f), HpfFreq = 0.1f };
            if (p.Wave == SfxWave.Square) p.Duty = r.F(0.6f); else p.Duty = 1;
            return p;
        }

        /// <summary>Plain oscillator note with an attack/release envelope, for jingles and music.</summary>
        public static float[] Tone(double hz, double seconds, SfxWave wave, float volume = 0.3f, double attack = 0.005, double release = 0.08, float duty = 0.5f, uint seed = 1)
        {
            int n = Math.Max(1, (int)(seconds * SampleRate));
            var s = new float[n];
            var rng = new Rng(seed);
            double ph = 0, inc = hz / SampleRate;
            float held = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double env = Math.Min(1, t / Math.Max(1e-4, attack)) * Math.Min(1, (seconds - t) / Math.Max(1e-4, release));
                double v;
                switch (wave)
                {
                    case SfxWave.Square: v = ph < duty ? 1 : -1; break;
                    case SfxWave.Saw: v = 2 * ph - 1; break;
                    case SfxWave.Sine: v = Math.Sin(ph * 2 * Math.PI); break;
                    case SfxWave.Triangle: v = 1 - 4 * Math.Abs(ph - 0.5); break;
                    default:
                        if (ph + inc >= 1) held = rng.Next() * 2 - 1;
                        v = held;
                        break;
                }
                s[i] = (float)(v * env * volume);
                ph += inc;
                if (ph >= 1) ph -= 1;
            }
            return s;
        }

        public static double NoteHz(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);

        public static float[] Silence(double seconds) => new float[Math.Max(0, (int)(seconds * SampleRate))];

        /// <summary>Adds src into dst starting at offsetSeconds, growing dst when needed.</summary>
        public static float[] MixInto(float[] dst, float[] src, double offsetSeconds, float gain = 1f)
        {
            int off = (int)(offsetSeconds * SampleRate);
            if (off + src.Length > dst.Length) Array.Resize(ref dst, off + src.Length);
            for (int i = 0; i < src.Length; i++) dst[off + i] += src[i] * gain;
            return dst;
        }

        public static float[] Concat(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var r = new float[n];
            int o = 0;
            foreach (var p in parts) { Array.Copy(p, 0, r, o, p.Length); o += p.Length; }
            return r;
        }

        /// <summary>Scales so the loudest sample sits at peakDb dBFS (default -3 dB headroom) and fades the last ms to avoid clicks.</summary>
        public static float[] Normalize(float[] s, float peakDb = -3f, double fadeOutMs = 6)
        {
            float peak = 0;
            foreach (var v in s) peak = Math.Max(peak, Math.Abs(v));
            if (peak > 0)
            {
                float k = (float)Math.Pow(10, peakDb / 20.0) / peak;
                for (int i = 0; i < s.Length; i++) s[i] *= k;
            }
            int fade = Math.Min(s.Length, (int)(fadeOutMs / 1000.0 * SampleRate));
            for (int i = 0; i < fade; i++) s[s.Length - 1 - i] *= (float)i / fade;
            return s;
        }

        public static byte[] ToWav(float[] samples, int sampleRate = SampleRate)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int dataLen = samples.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataLen);
                w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(sampleRate);
                w.Write(sampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataLen);
                foreach (var v in samples) w.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, v)) * 32767));
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
