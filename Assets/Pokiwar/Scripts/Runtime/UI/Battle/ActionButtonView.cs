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

        public void Bind(string title, string cost, string uses, Sprite icon, Color frame)
        {
            gameObject.SetActive(true);
            if (Title != null) Title.text = title;
            if (Cost != null) Cost.text = cost;
            if (Uses != null) Uses.text = uses;
            if (Icon != null && icon != null) Icon.sprite = icon;
            if (Frame != null) Frame.color = frame;
        }

        public void SetUsable(bool usable)
        {
            Button.interactable = usable;
            if (Group != null) Group.alpha = usable ? 1f : 0.45f;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
