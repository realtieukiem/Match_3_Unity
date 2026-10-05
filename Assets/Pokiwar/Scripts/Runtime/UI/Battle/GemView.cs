using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class GemView : MonoBehaviour
    {
        public Image Icon;
        public GameObject MultiplierBadge;
        public Text MultiplierLabel;

        public int Id { get; private set; }
        public GemType Type { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        public void Bind(Cell cell, Sprite sprite)
        {
            Id = cell.Id;
            Type = cell.Type;
            Icon.sprite = sprite;
            Icon.color = Color.white;
            bool mult = cell.Multiplier > 1;
            if (MultiplierBadge != null) MultiplierBadge.SetActive(mult);
            if (MultiplierLabel != null) MultiplierLabel.text = mult ? "x" + cell.Multiplier : "";
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }
    }
}
