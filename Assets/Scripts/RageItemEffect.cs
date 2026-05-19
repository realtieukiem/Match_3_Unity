using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Item Effects/Rage")]
public class RageItemEffect : ShapeItemEffect
{
    public override bool CanHandle(ShapeEffectType effectType)
    {
        return effectType == ShapeEffectType.Rage;
    }

    public override void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount)
    {
        owner.AddRage(GetAmount(itemCount));
    }
}
