using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.UI
{
    /// <summary>What a card shows: its face under "face." + card id, the big number of what it gives, and the gem of what it costs.</summary>
    public static class CardFaces
    {
        public const string PaintedPrefix = "face.";
        public const string SkillFace = "face.skill";

        public static Sprite Sprite(SpriteLibrary lib, CardDef card) =>
            lib.Has(PaintedPrefix + card.Id) ? lib.Get(PaintedPrefix + card.Id) : lib.Get(card.IconKey);

        /// <summary>Every reusable skill card wears the same face.</summary>
        public static Sprite Sprite(SpriteLibrary lib, SkillDef skill) =>
            lib.Has(SkillFace) ? lib.Get(SkillFace) : lib.Get(skill.IconKey);

        public static string Value(CardDef card)
        {
            if (card.Effects.Count == 0) return "";
            var fx = card.Effects[0];
            int pct = Mathf.RoundToInt(fx.Value * 100f);
            switch (fx.Kind)
            {
                case EffectKind.AddMana:
                case EffectKind.AddRage:
                case EffectKind.HealFlat:
                    return "+" + Mathf.RoundToInt(fx.Value);
                case EffectKind.HealPctMax:
                case EffectKind.AddShieldPctMax:
                case EffectKind.BuffAtk:
                    return "+" + pct + "%";
                case EffectKind.DrainManaPctOfCurrent:
                case EffectKind.DebuffAtk:
                    return "-" + pct + "%";
                case EffectKind.DrainRage:
                    return "-" + Mathf.RoundToInt(fx.Value);
                case EffectKind.FlatDamage:
                    return Mathf.RoundToInt(fx.Value).ToString();
                case EffectKind.AtkDamage:
                    return pct + "%";
                case EffectKind.Summon:
                    return pct + "%x" + fx.Turns;
                default:
                    return "";
            }
        }

        public static string Cost(CardDef card) => card.ManaCost > 0 ? card.ManaCost.ToString() : card.RageCost > 0 ? card.RageCost.ToString() : null;

        public static GemType CostGem(CardDef card) => card.ManaCost > 0 || card.RageCost <= 0 ? GemType.Lightning : GemType.Fire;
    }
}
