using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Item Effects/Attack")]
public class AttackItemEffect : ShapeItemEffect
{
    public override bool CanHandle(ShapeEffectType effectType)
    {
        return effectType == ShapeEffectType.Attack;
    }

    public override void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount)
    {
        opponent.TakeDamage(GetAmount(itemCount));
    }
}
