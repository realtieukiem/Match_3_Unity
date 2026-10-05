using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public enum GemType { None = -1, Sword = 0, Lightning = 1, Fire = 2, Heart = 3, Shield = 4, YinYang = 5 }

    public enum Element { Neutral = 0, Metal = 1, Wood = 2, Water = 3, Fire = 4, Earth = 5 }

    public enum Side { Player = 0, Enemy = 1 }

    public enum Confidence { Video, UserConfirmed, Inferred, Provisional }

    public enum RoundingMode { Floor, Round, Ceil }

    public enum ResourceKind { None, Hp, Mana, Rage, Shield }

    public static class Gems
    {
        public const int Count = 6;

        public static readonly GemType[] ResolveOrder =
        {
            GemType.Heart, GemType.Lightning, GemType.Fire, GemType.YinYang, GemType.Shield, GemType.Sword
        };

        public static readonly GemType[] All =
        {
            GemType.Sword, GemType.Lightning, GemType.Fire, GemType.Heart, GemType.Shield, GemType.YinYang
        };
    }

    public static class SideExt
    {
        public static Side Other(this Side s) => s == Side.Player ? Side.Enemy : Side.Player;
    }

    public static class MathUtil
    {
        public static int Round(double v, RoundingMode mode)
        {
            switch (mode)
            {
                case RoundingMode.Ceil: return (int)Math.Ceiling(v - 1e-4);
                case RoundingMode.Round: return (int)Math.Round(v, MidpointRounding.AwayFromZero);
                default: return (int)Math.Floor(v + 1e-4);
            }
        }

        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
    }

    [Serializable]
    public sealed class SeededRng
    {
        public uint State;

        public SeededRng(uint seed)
        {
            uint h = seed + 0x9E3779B9u;
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
            State = h == 0 ? 0x9E3779B9u : h;
        }

        public uint NextUInt()
        {
            uint x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return x;
        }

        public int Next(int maxExclusive) => maxExclusive <= 1 ? 0 : (int)(NextUInt() % (uint)maxExclusive);

        public int Range(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

        public double NextDouble() => NextUInt() / 4294967296.0;

        public bool Chance(double p) => p > 0 && NextDouble() < p;

        public SeededRng Fork(uint salt) => new SeededRng(NextUInt() ^ (salt * 2654435761u));

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        public int PickWeighted(IReadOnlyList<float> weights)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0, weights[i]);
            if (total <= 0) return Next(weights.Count);
            double r = NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                r -= Math.Max(0, weights[i]);
                if (r < 0) return i;
            }
            return weights.Count - 1;
        }
    }

    [Serializable]
    public sealed class Resource
    {
        public int Current;
        public int Max;

        public Resource() { }

        public Resource(int current, int max)
        {
            Max = Math.Max(0, max);
            Current = MathUtil.Clamp(current, 0, Max);
        }

        public int Room => Max - Current;
        public bool IsFull => Current >= Max;
        public float Ratio => Max <= 0 ? 0f : (float)Current / Max;

        public int Add(int amount)
        {
            int before = Current;
            Current = MathUtil.Clamp(Current + amount, 0, Max);
            return Current - before;
        }

        public void Set(int value) => Current = MathUtil.Clamp(value, 0, Max);

        public Resource Clone() => new Resource(Current, Max);
    }
}
