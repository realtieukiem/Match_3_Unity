using UnityEngine;

public abstract class ShapeItemEffect : ScriptableObject
{
    public int AmountPerItem = 10;

    private void OnValidate()
    {
        AmountPerItem = Mathf.Max(0, AmountPerItem);
    }

    public abstract bool CanHandle(ShapeEffectType effectType);

    public abstract void Apply(PlayerControllerBase owner, PlayerControllerBase opponent, int itemCount);

    protected int GetAmount(int itemCount)
    {
        return AmountPerItem * Mathf.Max(1, itemCount);
    }

}
