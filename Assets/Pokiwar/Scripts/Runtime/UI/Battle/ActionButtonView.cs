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
        [Tooltip("Gem beside the cost: lightning for mana, fire for rage.")]
        public Image CostIcon;
        [Tooltip("Card background, tinted by what the card gives.")]
        public Image Plate;
        public GameObject LevelBadge;
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

        public void SetLook(Color plate, Sprite costIcon)
        {
            if (Plate != null) Plate.color = plate;
            if (CostIcon != null && costIcon != null) CostIcon.sprite = costIcon;
            if (Icon != null) Icon.preserveAspect = true;
        }

        public void SetUsable(bool usable)
        {
            Button.interactable = usable;
            if (Group != null) Group.alpha = usable ? 1f : 0.45f;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
