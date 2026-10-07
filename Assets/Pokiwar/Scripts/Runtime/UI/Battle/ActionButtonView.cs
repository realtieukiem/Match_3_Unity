using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>One slot of the action bar: a consumable card or a pet skill. They share a look but not rules.</summary>
    public sealed class ActionButtonView : MonoBehaviour
    {
        public Button Button;
        public Image Icon;
        public Text Title;
        public Text Cost;
        public Text Uses;
        public CanvasGroup Group;
        public Image Frame;
        public GameObject CostBar;
        public GameObject LevelBadge;
        [Tooltip("Big number across a single-use card: what one use gives.")]
        public Text Value;
        [Tooltip("Icon in the bottom strip: the sword beside a skill's damage, or the gem a single-use card costs.")]
        public Image PowerIcon;
        public Text Level;
        public GameObject PowerBar;
        public Text Power;

        /// <summary>The numbers on the card: cost at the top, level badge in the corner, damage along the bottom. Null hides a part.</summary>
        public void SetFace(string cost, string level, string power)
        {
            if (CostBar != null) CostBar.SetActive(!string.IsNullOrEmpty(cost));
            if (Cost != null) Cost.text = cost ?? "";
            if (LevelBadge != null) LevelBadge.SetActive(level != null);
            if (Level != null) Level.text = level ?? "";
            if (PowerBar != null) PowerBar.SetActive(power != null);
            if (Power != null) Power.text = power ?? "";
        }

        public void Bind(string title, string cost, string uses, Sprite icon, Color frame)
        {
            gameObject.SetActive(true);
            if (Title != null) Title.text = title;
            if (Cost != null) Cost.text = cost;
            if (Uses != null) Uses.text = uses;
            if (Icon != null && icon != null) Icon.sprite = icon;
            if (Frame != null) Frame.color = frame;
        }

        /// <summary>Single-use card: the number it gives across the face, its cost with the cost gem in the bottom strip. A free card has no strip.</summary>
        public void SetCard(string value, string cost, Sprite costGem)
        {
            SetFace(null, null, cost);
            if (Value != null) Value.text = value ?? "";
            if (PowerIcon != null && costGem != null) PowerIcon.sprite = costGem;
        }

        public void SetUsable(bool usable)
        {
            Button.interactable = usable;
            if (Group != null) Group.alpha = usable ? 1f : 0.45f;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
