using UnityEngine;
using System.Collections.Generic;

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

    public bool CanMergeWith(ShapeMatchData other)
    {
        return EffectType != ShapeEffectType.None
            && EffectType == other.EffectType;
    }
}

public static class ShapeMatchDataAggregator
{
    public static List<ShapeMatchData> MergeByEffectType(IEnumerable<ShapeMatchData> matchedItems)
    {
        List<ShapeMatchData> mergedItems = new List<ShapeMatchData>();
        if (matchedItems == null)
            return mergedItems;

        foreach (ShapeMatchData item in matchedItems)
        {
            if (item.EffectType == ShapeEffectType.None)
                continue;

            int existingItemIndex = FindMergeableItemIndex(mergedItems, item);
            if (existingItemIndex >= 0)
                mergedItems[existingItemIndex] = mergedItems[existingItemIndex].WithAddedCount(item.Count);
            else
                mergedItems.Add(item);
        }

        return mergedItems;
    }

    private static int FindMergeableItemIndex(List<ShapeMatchData> items, ShapeMatchData item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].CanMergeWith(item))
                return i;
        }

        return -1;
    }
}
