using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class Shape : MonoBehaviour
{
    private const string CountTextName = "CountText";

    public TextMeshPro CountText;
    public ShapeEffectType ItemEffect = ShapeEffectType.ByPrefabName;

    public BonusType Bonus { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }

    public string Type { get; set; }
    public int ItemCount { get; private set; }

    public Shape()
    {
        Bonus = BonusType.None;
        ItemCount = 1;
    }

    private void Awake()
    {
        if (ItemCount <= 0)
            ItemCount = 1;
    }

    /// <summary>
    /// Checks if the current shape is of the same type as the parameter
    /// </summary>
    /// <param name="otherShape"></param>
    /// <returns></returns>
    public bool IsSameType(Shape otherShape)
    {
        if (otherShape == null)
            throw new ArgumentException("otherShape");

        return string.Compare(Type, otherShape.Type) == 0;
    }

    /// <summary>
    /// Constructor alternative
    /// </summary>
    /// <param name="type"></param>
    /// <param name="row"></param>
    /// <param name="column"></param>
    public void Assign(string type, int row, int column)
    {
        Assign(type, row, column, 1);
    }

    public void Assign(string type, int row, int column, int itemCount)
    {

        if (string.IsNullOrEmpty(type))
            throw new ArgumentException("type");

        Column = column;
        Row = row;
        Type = type;
        SetItemCount(itemCount);
    }

    public void SetItemCount(int itemCount)
    {
        ItemCount = Mathf.Clamp(itemCount, 1, 3);
        UpdateCountText();
    }

    private void UpdateCountText()
    {
        TextMeshPro countText = GetCountText();
        if (countText == null)
            return;

        countText.text = ItemCount > 1 ? "x" + ItemCount.ToString() : string.Empty;
        countText.gameObject.SetActive(ItemCount > 1);
    }

    private TextMeshPro GetCountText()
    {
        if (CountText != null)
            return CountText;

        Transform textTransform = transform.Find(CountTextName);
        if (textTransform != null)
        {
            CountText = textTransform.GetComponent<TextMeshPro>();
            if (CountText != null)
                return CountText;
        }

        GameObject textGo = new GameObject(CountTextName);
        textGo.transform.SetParent(transform);
        textGo.transform.localPosition = new Vector3(0f, -1.15f, -0.1f);
        textGo.transform.localRotation = Quaternion.identity;
        textGo.transform.localScale = Vector3.one * 0.45f;

        CountText = textGo.AddComponent<TextMeshPro>();
        CountText.alignment = TextAlignmentOptions.Center;
        CountText.fontSize = 48;
        CountText.color = Color.white;

        RefreshCountTextSorting();

        return CountText;
    }

    public void RefreshCountTextSorting()
    {
        TextMeshPro countText = GetCountText();
        if (countText == null)
            return;

        MeshRenderer renderer = countText.GetComponent<MeshRenderer>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
            renderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 1;
        }
    }

    public ShapeEffectType GetResolvedItemEffect()
    {
        if (ItemEffect != ShapeEffectType.ByPrefabName)
            return ItemEffect;

        if (string.Compare(Type, "Attack", true) == 0)
            return ShapeEffectType.Attack;

        if (string.Compare(Type, "Heal", true) == 0 || string.Compare(Type, "Heart", true) == 0)
            return ShapeEffectType.Heal;

        if (string.Compare(Type, "Mana", true) == 0)
            return ShapeEffectType.Mana;

        if (string.Compare(Type, "Rage", true) == 0 || string.Compare(Type, "Angry", true) == 0)
            return ShapeEffectType.Rage;

        if (string.Compare(Type, "Absorb", true) == 0)
            return ShapeEffectType.Absorb;

        if (string.Compare(Type, "Armor", true) == 0)
            return ShapeEffectType.Armor;

        return ShapeEffectType.None;
    }

    /// <summary>
    /// Swaps properties of the two shapes
    /// We could do a shallow copy/exchange here, but anyway...
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static void SwapColumnRow(Shape a, Shape b)
    {
        int temp = a.Row;
        a.Row = b.Row;
        b.Row = temp;

        temp = a.Column;
        a.Column = b.Column;
        b.Column = temp;
    }
}



