using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Generic list row / map node: a button with an icon, a title, a subtitle and a highlight frame.</summary>
    public sealed class RowView : MonoBehaviour
    {
        public Button Button;
        public Image Background;
        public Image Icon;
        public Text Title;
        public Text Subtitle;
        public GameObject Highlight;
        [Tooltip("Small mark on the row, such as a creature's element.")]
        public Image Badge;
        [Tooltip("Rim around the row's picture; a map node tints it by element.")]
        public Image Frame;
        [Tooltip("Short label beside the picture, such as a map node's number.")]
        public Text Tag;
        public Button ExtraA;
        public Text ExtraALabel;
        public Button ExtraB;
        public Text ExtraBLabel;
        public Button ExtraC;
        public Text ExtraCLabel;

        public RectTransform Rect => (RectTransform)transform;

        public void Set(string title, string subtitle, Sprite icon, bool highlighted)
        {
            if (Title != null) Title.text = title;
            if (Subtitle != null) Subtitle.text = subtitle;
            if (Icon != null)
            {
                Icon.gameObject.SetActive(icon != null);
                if (icon != null) Icon.sprite = icon;
            }
            if (Highlight != null) Highlight.SetActive(highlighted);
        }
    }
}
