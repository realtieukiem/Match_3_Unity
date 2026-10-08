using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public static class SpriteKeys
    {
        public const string EvolvedSuffix = "_evolved";
    }

    [Serializable]
    public class StatBlock
    {
        public int MaxHp;
        public int Atk;
        public int Def;
        public int MaxMana;
        public int MaxRage;

        public StatBlock() { }

        public StatBlock(int hp, int atk, int def, int mana, int rage)
        {
            MaxHp = hp;
            Atk = atk;
            Def = def;
            MaxMana = mana;
            MaxRage = rage;
        }

        public StatBlock Clone() => new StatBlock(MaxHp, Atk, Def, MaxMana, MaxRage);
    }

    /// <summary>Per-actor gem yields.</summary>
    [Serializable]
    public class GemEffectProfile
    {
        public string Id = "gem.default";
        public Confidence Confidence = Confidence.Provisional;

        public float HeartPctMaxHpPerGem = 0.03f;
        public int HeartFlatPerGem;

        public float LightningPctMaxManaPerGem = 0.03f;
        public int LightningFlatPerGem;

        public int FireRagePerGem = 6;

        public float ShieldPctMaxHpPerGem = 0.012f;
        public int ShieldFlatPerGem;
        public float ShieldCapPctMaxHp = 0.35f;

        public float YinYangManaPctOfTargetMaxPerGem = 0.02f;
        public int YinYangRagePerGem = 5;
        public float YinYangManaChance = 0.5f;

        public float SwordAtkPerGem = 0.6f;

        public float Variation;
        public RoundingMode Rounding = RoundingMode.Floor;
    }

    [Serializable]
    public class RageProfile
    {
        public int AttackThreshold = 100;
        public int AttackCost = 100;
        public float StrongMultiplier = 1.70f;
        public float RageFromHpDamageScale = 100f;
        public int RageOnOwnTurnStart = 3;

        public Confidence StrongMultiplierConfidence = Confidence.Inferred;
        public Confidence HpDamageRageConfidence = Confidence.Inferred;
        public Confidence TurnStartRageConfidence = Confidence.Provisional;
    }

    public enum DefenseMode { Barrier, FlatDef, PercentReduction }

    [Serializable]
    public class DefenseRules
    {
        public DefenseMode Mode = DefenseMode.Barrier;
        public float PercentPerShieldPoint = 0.0005f;
        public float MaxPercentReduction = 0.6f;
        public bool ApplyDefStat;
        public Confidence Confidence = Confidence.Provisional;
    }

    [Serializable]
    public class ElementChart
    {
        public float Advantage = 1.25f;
        public float Disadvantage = 0.80f;
        public float Neutral = 1.00f;
        public float BonusPerPoint = 0.01f;
        public Confidence Confidence = Confidence.Provisional;

        public static bool Beats(Element a, Element b)
        {
            switch (a)
            {
                case Element.Metal: return b == Element.Wood;
                case Element.Wood: return b == Element.Earth;
                case Element.Earth: return b == Element.Water;
                case Element.Water: return b == Element.Fire;
                case Element.Fire: return b == Element.Metal;
                default: return false;
            }
        }

        public int Relation(Element attacker, Element defender)
        {
            if (Beats(attacker, defender)) return 1;
            if (Beats(defender, attacker)) return -1;
            return 0;
        }

        /// <summary>Attacker bonus points sharpen an advantage, defender bonus points soften a disadvantage. The +N shown next to an element icon is fed in here; it is not assumed to be N%.</summary>
        public float Modifier(Element attacker, int attackerBonus, Element defender, int defenderBonus)
        {
            int rel = Relation(attacker, defender);
            if (rel > 0) return Advantage + attackerBonus * BonusPerPoint;
            if (rel < 0) return Math.Max(0.1f, Disadvantage - defenderBonus * BonusPerPoint);
            return Neutral;
        }
    }

    [Serializable]
    public class BattleRules
    {
        public ElementChart Elements = new ElementChart();
        public DefenseRules Defense = new DefenseRules();
        public float AiThinkSeconds = 2f;
        public int MinimumHpDamageOnHit = 1;
        public bool ShieldExpiresOnOwnTurnStart = true;
        public float StartHpPct = 0.75f;
    }

    public enum TargetKind { Self, Opponent }

    public enum EffectKind
    {
        FlatDamage,
        AtkDamage,
        HealFlat,
        HealPctMax,
        AddMana,
        AddRage,
        AddShieldPctMax,
        DrainManaPctOfCurrent,
        DrainRage,
        BuffAtk,
        DebuffAtk,
        Summon,
        SetHpPctOfMax
    }

    [Serializable]
    public class EffectSpec
    {
        public EffectKind Kind;
        public TargetKind Target;
        public float Value;
        public int Turns;
        public string Label;

        public EffectSpec() { }

        public EffectSpec(EffectKind kind, TargetKind target, float value, int turns = 0, string label = null)
        {
            Kind = kind;
            Target = target;
            Value = value;
            Turns = turns;
            Label = label;
        }
    }

    [Serializable]
    public class CardDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string IconKey;
        public int ManaCost;
        public int RageCost;
        public bool CanUseBeforeMatch = true;
        public bool CanUseAfterMatch;
        public bool EndTurnAfterUse;
        public List<EffectSpec> Effects = new List<EffectSpec>();
    }

    [Serializable]
    public class QteProfile
    {
        public int ArrowCount = 5;
        public float PerCorrectBonus = 0.20f;
        public float MissMultiplier = 1.00f;
        public float GoodMultiplier = 1.24f;
        public float PerfectMultiplier = 1.25f;
        public float ArrowSeconds = 4.0f;
        public float BarSeconds = 3.0f;
        public float BarCyclesPerSecond = 0.9f;
        public float GoodHalfWidth = 0.16f;
        public float PerfectHalfWidth = 0.05f;
        public Confidence Confidence = Confidence.Video;
    }

    [Serializable]
    public class SkillDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string IconKey;
        public int ManaCost;
        public int RageCost;
        public float AtkMultiplier;
        public int FlatBase;
        public int FlatPerLevel;
        public bool UseQte = true;
        public QteProfile Qte = new QteProfile();
        public bool EndTurnAfterUse = true;
        public List<EffectSpec> PostEffects = new List<EffectSpec>();
    }

    public enum PhaseTriggerKind { HpAtOrBelowPct }

    [Serializable]
    public class BossPhaseDef
    {
        public string Id;
        public string FormName;
        public string SpriteKey;
        public PhaseTriggerKind Trigger = PhaseTriggerKind.HpAtOrBelowPct;
        public float TriggerValue = 0.32f;
        public bool OneShot = true;
        public bool TriggerOnLethal = true;
        public float SetHpPctOfMax = 0.5f;
        public float AtkMultiplier = 1.15f;
        public List<string> UnlockSkillIds = new List<string>();
        public bool ContinueTurn = true;
        public Confidence TriggerConfidence = Confidence.Provisional;
        public Confidence SetHpConfidence = Confidence.Video;
    }

    [Serializable]
    public class CreatureDef
    {
        public string Id;
        public string Name;
        public string SpriteKey;
        public Element Element;
        public int ElementBonus;
        public StatBlock BaseStats = new StatBlock();
        public StatBlock PerLevel = new StatBlock();
        public string GemProfileId;
        public RageProfile Rage = new RageProfile();
        public List<string> SkillIds = new List<string>();
        public List<string> CardIds = new List<string>();
        public List<BossPhaseDef> Phases = new List<BossPhaseDef>();

        public StatBlock StatsAt(int level)
        {
            int l = Math.Max(1, level) - 1;
            return new StatBlock(
                BaseStats.MaxHp + PerLevel.MaxHp * l,
                BaseStats.Atk + PerLevel.Atk * l,
                BaseStats.Def + PerLevel.Def * l,
                BaseStats.MaxMana + PerLevel.MaxMana * l,
                BaseStats.MaxRage + PerLevel.MaxRage * l);
        }
    }

    [Serializable]
    public class AIPolicy
    {
        public string Id = "ai.normal";
        public float Temperature = 0.35f;
        public int TopK = 3;
        public float MistakeChance = 0.08f;
        public float SwordWeight = 1.0f;
        public float HeartWeight = 1.0f;
        public float LightningWeight = 0.8f;
        public float FireWeight = 0.8f;
        public float ShieldWeight = 0.6f;
        public float YinYangWeight = 0.7f;
        public float CascadeWeight = 0.4f;
        public float OpponentOpportunityWeight = 0.35f;
        public bool UseCards = true;
        public bool UseSkills = true;
        public float QtePerfectChance = 0.25f;
        public float QteGoodChance = 0.5f;
        public float QteAvgCorrect = 3.5f;
    }

    public enum RewardKind { Stone, Card, LuckyCharm, ProtectionCharm, Pet, CardStone, SkillCard }

    [Serializable]
    public class RewardDrop
    {
        public RewardKind Kind;
        public string ItemId;
        public Element Element;
        public int Tier = 1;
        public int Count = 1;
        public float Chance = 1f;
        public bool FirstClearOnly;
    }

    [Serializable]
    public class RewardTable
    {
        public string Id;
        public int Gold;
        public List<RewardDrop> Drops = new List<RewardDrop>();
    }

    [Serializable]
    public class EncounterDef
    {
        public string Id;
        public string Name;
        public string CreatureId;
        public int Level = 1;
        public float StartHpPct = 1f;
        public string AiPolicyId;
        public string RewardTableId;
        public bool IsBoss;
        public int EnergyCost = 1;
        public int Difficulty = 1;
        public Side FirstTurn = Side.Player;
        public bool CaptureOnFirstWin = true;
        public float ManaVsPlayer;
        public int SkillLevel = 1;
        public int RankPoints;
    }

    [Serializable]
    public class RegionDef
    {
        public string Id;
        public string Name;
    }

    [Serializable]
    public class MapNodeDef
    {
        public string Id;
        public string Name;
        public string RegionId;
        public string EncounterId;
        public string RequiresNodeId;
        public int WinsRequired = 1;
        public float X;
        public float Y;
    }

    [Serializable]
    public class MapDef
    {
        public List<RegionDef> Regions = new List<RegionDef>();
        public List<MapNodeDef> Nodes = new List<MapNodeDef>();
    }

    /// <summary>Upgrade numbers here are proposed tuning, not original Pokiwar tables.</summary>
    [Serializable]
    public class UpgradeConfig
    {
        public int MaxStoneTier = 7;
        public float[] MergeChanceByTier = { 0.90f, 0.75f, 0.60f, 0.45f, 0.32f, 0.20f, 0.12f };
        public int[] MergeGoldByTier = { 100, 250, 600, 1500, 3500, 8000, 15000 };
        public float LuckyCharmBonus = 0.15f;

        public float[] EnhanceChanceByLevel = { 0.95f, 0.85f, 0.75f, 0.62f, 0.50f, 0.40f, 0.30f, 0.22f, 0.15f, 0.10f };
        public int[] EnhanceGoldByLevel = { 200, 400, 700, 1100, 1600, 2300, 3200, 4500, 6000, 8000 };
        public float EnhanceTierBonusPerTierAbove = 0.05f;
        public float EnhanceFailAccumulate = 0.04f;
        public float EnhanceMinChance = 0.02f;
        public bool FailDropsLevelWithoutProtection = true;
        public float EnhanceStatPctPerLevel = 0.04f;

        public int SocketCount = 3;
        public int SocketAtkPerTier = 6;
        public int SocketHpPerTier = 60;
        public int SocketElementBonusPerTier = 1;

        public int MaxCardLevel = 12;
        public int MaxCardStoneTier = 6;
        public float[] CardUpgradeChanceByLevel = { 0.95f, 0.88f, 0.80f, 0.70f, 0.60f, 0.50f, 0.40f, 0.32f, 0.25f, 0.18f, 0.12f };
        public int[] CardUpgradeGoldByLevel = { 150, 300, 500, 800, 1200, 1700, 2400, 3300, 4500, 6000, 8000 };
        public float CardStoneTierBonusPerTierAbove = 0.06f;
        public bool CardFailDropsLevelWithoutProtection;

        public Confidence Confidence = Confidence.Provisional;
    }

    [Serializable]
    public class ProgressionConfig
    {
        public int StartGold = 500;
        public int EvolveAtLevel = 5;
        public int TrainerHpPerLevel = 5;
        public int TrainerManaPerLevel = 5;
        public int MaxEnergy = 30;
        public int EnergyRegenSeconds = 300;
        public int ExpWinBase = 19;
        public int ExpWinPerHuntLevel = 3;
        public int ExpWinLostPerTrainerLevel = 5;
        public int ExpLoss = 1;
        public int ExpToNextPerLevel = 40;
        public int PetMaxLevel = 14;
        public List<string> StarterPetIds = new List<string>();
        public List<int> StarterPetLevels = new List<int>();
        public List<string> StarterCardIds = new List<string>();
        public List<string> StarterSkillIds = new List<string>();
        public List<RewardDrop> StarterItems = new List<RewardDrop>();
        public string DefaultPlayerName = "Trainer";
        public List<string> StarterAvatarIds = new List<string>();
        public List<ShopItemDef> Shop = new List<ShopItemDef>();
    }

    [Serializable]
    public class ShopItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string IconKey;
        public int Price;
        public RewardDrop Grant = new RewardDrop();
    }
}
