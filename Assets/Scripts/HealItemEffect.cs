using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Item Effects/Heal")]
public class HealItemEffect : ShapeItemEffect
{
    public override bool CanHandle(ShapeEffectType effectType)
    {
        return effectType == ShapeEffectType.Heal;
    }

    public override void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount)
    {
        owner.Heal(GetAmount(itemCount));
    }
}
