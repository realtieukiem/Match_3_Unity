using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public sealed class CardSlot
    {
        public CardDef Def;
        public int UsesLeft;

        public CardSlot(CardDef def)
        {
            Def = def;
            UsesLeft = 1;
        }
    }

    public sealed class Buff
    {
        public string Label;
        public float AtkMultiplier = 1f;
        public int TurnsLeft;
    }

    public sealed class Summon
    {
        public string Name;
        public int DamagePerTurn;
        public int TurnsLeft;
    }

    public sealed class CombatantSetup
    {
        public CreatureDef Creature;
        public string SpriteKey;
        public bool NoPhases;
        public int Level = 1;
        public StatBlock Stats;
        public int ElementBonus;
        public float StartHpPct = 1f;
        public int StartMana;
        public int StartRage;
        public GemEffectProfile Gems;
        public List<CardDef> Cards = new List<CardDef>();
        public List<SkillDef> Skills = new List<SkillDef>();
        public List<SkillDef> LockedSkills = new List<SkillDef>();
        public Dictionary<string, int> SkillLevels = new Dictionary<string, int>();
        public AIPolicy Ai;
    }

    public sealed class Combatant
    {
        public Side Side;
        public string CreatureId;
        public string Name;
        public string FormName;
        public string SpriteKey;
        public Element Element;
        public int ElementBonus;
        public int Level;
        public int BaseAtk;
        public int Def;
        public float FormAtkMultiplier = 1f;
        public Resource Hp;
        public Resource Mana;
        public Resource Rage;
        public Resource Shield;
        public GemEffectProfile Gems;
        public RageProfile RageRules;
        public readonly List<CardSlot> Cards = new List<CardSlot>();
        public readonly List<SkillDef> Skills = new List<SkillDef>();
        public readonly List<SkillDef> LockedSkills = new List<SkillDef>();
        public readonly Dictionary<string, int> SkillLevels = new Dictionary<string, int>();

        public int SkillLevel(SkillDef s) => SkillLevels.TryGetValue(s.Id, out int l) ? Math.Max(1, l) : 1;
        public readonly List<Buff> Buffs = new List<Buff>();
        public readonly List<Summon> Summons = new List<Summon>();
        public readonly List<BossPhaseDef> Phases = new List<BossPhaseDef>();
        public readonly HashSet<string> FiredPhases = new HashSet<string>();
        public AIPolicy Ai;

        public bool IsDead => Hp.Current <= 0;

        public int EffectiveAtk
        {
            get
            {
                double m = FormAtkMultiplier;
                foreach (var b in Buffs) m *= b.AtkMultiplier;
                return Math.Max(1, (int)Math.Floor(BaseAtk * m));
            }
        }

        public static Combatant Create(Side side, CombatantSetup s)
        {
            var c = s.Creature;
            var st = s.Stats ?? c.StatsAt(s.Level);
            var gems = s.Gems ?? new GemEffectProfile();
            int shieldCap = Math.Max(1, (int)(st.MaxHp * gems.ShieldCapPctMaxHp));
            var cb = new Combatant
            {
                Side = side,
                CreatureId = c.Id,
                Name = c.Name,
                FormName = c.Name,
                SpriteKey = s.SpriteKey ?? c.SpriteKey,
                Element = c.Element,
                ElementBonus = c.ElementBonus + s.ElementBonus,
                Level = s.Level,
                BaseAtk = st.Atk,
                Def = st.Def,
                Hp = new Resource(Math.Max(1, (int)Math.Round(st.MaxHp * s.StartHpPct)), st.MaxHp),
                Mana = new Resource(s.StartMana, st.MaxMana),
                Rage = new Resource(s.StartRage, st.MaxRage),
                Shield = new Resource(0, shieldCap),
                Gems = gems,
                RageRules = c.Rage ?? new RageProfile(),
                Ai = s.Ai
            };
            foreach (var card in s.Cards) if (card != null) cb.Cards.Add(new CardSlot(card));
            foreach (var sk in s.Skills) if (sk != null) cb.Skills.Add(sk);
            foreach (var kv in s.SkillLevels) cb.SkillLevels[kv.Key] = kv.Value;
            foreach (var sk in s.LockedSkills) if (sk != null) cb.LockedSkills.Add(sk);
            if (!s.NoPhases) cb.Phases.AddRange(c.Phases);
            return cb;
        }

        public Resource Get(ResourceKind k)
        {
            switch (k)
            {
                case ResourceKind.Hp: return Hp;
                case ResourceKind.Mana: return Mana;
                case ResourceKind.Rage: return Rage;
                case ResourceKind.Shield: return Shield;
                default: return null;
            }
        }

        public CombatantSnapshot Snapshot() => new CombatantSnapshot
        {
            Side = Side,
            Name = FormName,
            SpriteKey = SpriteKey,
            Hp = Hp.Current, MaxHp = Hp.Max,
            Mana = Mana.Current, MaxMana = Mana.Max,
            Rage = Rage.Current, MaxRage = Rage.Max,
            Shield = Shield.Current, MaxShield = Shield.Max,
            Atk = EffectiveAtk
        };
    }

    [Serializable]
    public struct CombatantSnapshot
    {
        public Side Side;
        public string Name;
        public string SpriteKey;
        public int Hp, MaxHp, Mana, MaxMana, Rage, MaxRage, Shield, MaxShield, Atk;

        public override string ToString() =>
            Name + " HP " + Hp + "/" + MaxHp + " MP " + Mana + "/" + MaxMana + " RG " + Rage + "/" + MaxRage + " SH " + Shield + "/" + MaxShield;
    }
}
