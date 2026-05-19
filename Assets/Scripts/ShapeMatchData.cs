using UnityEngine;

public struct ShapeMatchData
{
    public readonly ShapeEffectType EffectType;
    public readonly int Count;
    public readonly Sprite Sprite;
    public readonly Color Color;

    public ShapeMatchData(ShapeEffectType effectType, int count, Sprite sprite, Color color)
    {
        EffectType = effectType;
        Count = count;
        Sprite = sprite;
        Color = color;
    }

    public ShapeMatchData WithAddedCount(int count)
    {
        return new ShapeMatchData(EffectType, Count + count, Sprite, Color);
    }
}
