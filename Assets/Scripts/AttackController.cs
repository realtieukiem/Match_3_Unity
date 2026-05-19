using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    [Header("Amounts")]
    public int AttackAmount = 10;
    public int HealAmount = 10;
    public int ManaAmount = 10;
    public int RageAmount = 10;
    public int AbsorbAmount = 10;
    public int ArmorAmount = 10;

    [Header("Item Visuals")]
    public Transform ItemDisplayRoot;
    public Transform ItemVisualParent;
    public Vector2 ItemDisplaySpacing = new Vector2(0.7f, 0f);
    public float ItemVisualScale = 0.45f;
    public float ItemShowDuration = 0.2f;
    public float ItemFlyDuration = 0.45f;
    public Ease ItemShowEase = Ease.OutBack;
    public Ease ItemFlyEase = Ease.InBack;

    public IEnumerator PlayMatchedItems(IEnumerable<ShapeMatchData> matchedItems, PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        if (owner == null || opponent == null)
            yield break;

        List<ShapeMatchData> items = GetPlayableItems(matchedItems);
        List<GameObject> itemVisuals = CreateItemVisuals(items);
        if (itemVisuals.Count == 0)
            yield break;

        Sequence showSequence = DOTween.Sequence();
        for (int i = 0; i < itemVisuals.Count; i++)
            showSequence.Join(itemVisuals[i].transform.DOScale(Vector3.one * ItemVisualScale, ItemShowDuration).SetEase(ItemShowEase));

        yield return showSequence.WaitForCompletion();

        int index = 0;
        foreach (ShapeMatchData item in items)
        {
            GameObject visual = itemVisuals[index];
            Transform target = GetTarget(item.EffectType, owner, opponent);

            if (target != null)
            {
                Sequence flySequence = DOTween.Sequence();
                flySequence.Join(visual.transform.DOMove(target.position, ItemFlyDuration).SetEase(ItemFlyEase));
                flySequence.Join(visual.transform.DOScale(Vector3.zero, ItemFlyDuration));
                yield return flySequence.WaitForCompletion();
            }

            ApplyItem(item, owner, opponent);
            Destroy(visual);
            index++;
        }
    }

    private List<GameObject> CreateItemVisuals(IEnumerable<ShapeMatchData> matchedItems)
    {
        List<GameObject> visuals = new List<GameObject>();
        Vector3 rootPosition = ItemDisplayRoot != null ? ItemDisplayRoot.position : transform.position;
        Transform parent = ItemVisualParent != null ? ItemVisualParent : transform;
        List<ShapeMatchData> items = new List<ShapeMatchData>(matchedItems);
        Vector3 firstItemOffset = new Vector3(
            -ItemDisplaySpacing.x * (items.Count - 1) * 0.5f,
            -ItemDisplaySpacing.y * (items.Count - 1) * 0.5f,
            0f);

        int index = 0;
        foreach (ShapeMatchData item in items)
        {
            GameObject visual = new GameObject("MatchedItem_" + item.EffectType);
            visual.transform.SetParent(parent);
            visual.transform.position = rootPosition + firstItemOffset + new Vector3(ItemDisplaySpacing.x * index, ItemDisplaySpacing.y * index, 0f);
            visual.transform.localScale = Vector3.zero;

            if (item.Sprite != null)
            {
                SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = item.Sprite;
                spriteRenderer.color = GetOpaqueColor(item.Color);
                spriteRenderer.sortingOrder = 20;
            }

            if (item.Count > 1)
                CreateCountText(visual.transform, item.Count);

            visuals.Add(visual);
            index++;
        }

        return visuals;
    }

    private List<ShapeMatchData> GetPlayableItems(IEnumerable<ShapeMatchData> matchedItems)
    {
        List<ShapeMatchData> items = new List<ShapeMatchData>();
        foreach (ShapeMatchData item in matchedItems)
        {
            if (item.EffectType == ShapeEffectType.None)
                continue;

            if (items.Count > 0 && items[items.Count - 1].EffectType == item.EffectType)
                items[items.Count - 1] = items[items.Count - 1].WithAddedCount(item.Count);
            else
                items.Add(item);
        }

        return items;
    }

    private void CreateCountText(Transform parent, int count)
    {
        GameObject textGo = new GameObject("CountText");
        textGo.transform.SetParent(parent);
        textGo.transform.localPosition = new Vector3(0f, -1.15f, -0.1f);
        textGo.transform.localRotation = Quaternion.identity;
        textGo.transform.localScale = Vector3.one * 0.45f;

        TextMeshPro text = textGo.AddComponent<TextMeshPro>();
        text.text = "x" + count.ToString();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 48;
        text.color = Color.white;

        MeshRenderer renderer = textGo.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 21;
    }

    private Color GetOpaqueColor(Color color)
    {
        color.a = 1f;
        return color;
    }

    private Transform GetTarget(ShapeEffectType effectType, PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        switch (effectType)
        {
            case ShapeEffectType.Attack:
            case ShapeEffectType.Absorb:
                return opponent.GetEffectTarget();
            case ShapeEffectType.Heal:
            case ShapeEffectType.Mana:
            case ShapeEffectType.Rage:
            case ShapeEffectType.Armor:
                return owner.GetEffectTarget();
            default:
                return owner.GetEffectTarget();
        }
    }

    private void ApplyItem(ShapeMatchData item, PlayerControllerBase owner, PlayerControllerBase opponent)
    {
        int amount = GetAmount(item.EffectType, item.Count);

        switch (item.EffectType)
        {
            case ShapeEffectType.Attack:
                opponent.TakeDamage(owner.GetAttackDamageWithRageBonus(amount));
                break;
            case ShapeEffectType.Heal:
                owner.Heal(amount);
                break;
            case ShapeEffectType.Mana:
                owner.AddMana(amount);
                break;
            case ShapeEffectType.Rage:
                owner.AddRage(amount);
                break;
            case ShapeEffectType.Absorb:
                ApplyAbsorb(owner, opponent, amount);
                break;
            case ShapeEffectType.Armor:
                owner.AddArmor(amount);
                break;
        }
    }

    private int GetAmount(ShapeEffectType effectType, int itemCount)
    {
        int multiplier = Mathf.Max(1, itemCount);

        switch (effectType)
        {
            case ShapeEffectType.Attack:
                return AttackAmount * multiplier;
            case ShapeEffectType.Heal:
                return HealAmount * multiplier;
            case ShapeEffectType.Mana:
                return ManaAmount * multiplier;
            case ShapeEffectType.Rage:
                return RageAmount * multiplier;
            case ShapeEffectType.Absorb:
                return AbsorbAmount * multiplier;
            case ShapeEffectType.Armor:
                return ArmorAmount * multiplier;
            default:
                return 0;
        }
    }

    private void ApplyAbsorb(PlayerControllerBase owner, PlayerControllerBase opponent, int amount)
    {
        if (Random.value < 0.5f)
            owner.AddMana(opponent.DrainMana(amount));
        else
            owner.AddRage(opponent.DrainRage(amount));
    }
}
