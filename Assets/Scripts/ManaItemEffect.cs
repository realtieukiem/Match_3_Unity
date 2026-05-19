using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Item Effects/Mana")]
public class ManaItemEffect : ShapeItemEffect
{
    public override bool CanHandle(ShapeEffectType effectType)
    {
        return effectType == ShapeEffectType.Mana;
    }

    public override void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount)
    {
        owner.AddMana(GetAmount(itemCount));
    }
}
