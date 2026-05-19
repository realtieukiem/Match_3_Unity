using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Item Effects/Absorb")]
public class AbsorbItemEffect : ShapeItemEffect
{
    public override bool CanHandle(ShapeEffectType effectType)
    {
        return effectType == ShapeEffectType.Absorb;
    }

    public override void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount)
    {
        int amount = GetAmount(itemCount);

        if (Random.value < 0.5f)
            owner.AddMana(opponent.DrainMana(amount));
        else
            owner.AddRage(opponent.DrainRage(amount));
    }
}
