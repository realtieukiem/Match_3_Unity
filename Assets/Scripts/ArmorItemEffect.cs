using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Item Effects/Armor")]
public class ArmorItemEffect : ShapeItemEffect
{
    public override bool CanHandle(ShapeEffectType effectType)
    {
        return effectType == ShapeEffectType.Armor;
    }

    public override void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount)
    {
        owner.AddArmor(GetAmount(itemCount));
    }
}
