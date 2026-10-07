using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.UI
{
    /// <summary>What a single-use card shows: the board gem of the thing it gives, or painted art under "face." + card id when the library has it.</summary>
    public static class CardFaces
    {
        public const string PaintedPrefix = "face.";

        public static GemType Symbol(CardDef card)
        {
            foreach (var fx in card.Effects)
            {
                switch (fx.Kind)
                {
                    case EffectKind.HealFlat:
                    case EffectKind.HealPctMax:
                    case EffectKind.SetHpPctOfMax:
                        return GemType.Heart;
                    case EffectKind.AddMana:
                        return GemType.Lightning;
                    case EffectKind.AddRage:
                        return GemType.Fire;
                    case EffectKind.AddShieldPctMax:
                        return GemType.Shield;
                    case EffectKind.DrainManaPctOfCurrent:
                    case EffectKind.DrainRage:
                        return GemType.YinYang;
                    default:
                        return GemType.Sword;
                }
            }
            return GemType.Sword;
        }

        public static Sprite Sprite(SpriteLibrary lib, CardDef card) =>
            lib.Has(PaintedPrefix + card.Id) ? lib.Get(PaintedPrefix + card.Id) : lib.Gem(Symbol(card));

        public static Color Plate(CardDef card) => Color.Lerp(SpriteLibrary.GemColor(Symbol(card)), Color.black, 0.5f);

        public static GemType CostGem(CardDef card) => card.ManaCost > 0 || card.RageCost <= 0 ? GemType.Lightning : GemType.Fire;
    }
}
