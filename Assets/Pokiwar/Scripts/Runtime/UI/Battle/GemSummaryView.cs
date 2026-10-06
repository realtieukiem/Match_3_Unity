using System.Collections;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Shows the whole turn's tally in resolve order while the board is hidden; each icon goes once its effect has played.</summary>
    public sealed class GemSummaryView : MonoBehaviour
    {
        public CanvasGroup Group;
        public Text Header;
        public Image[] SlotIcons = new Image[6];
        public Text[] SlotLabels = new Text[6];
        public SpriteLibrary Sprites;
        [Tooltip("Distance between gem icons (px); the row is always centred.")]
        public float SlotSpacing = 100f;

        private readonly GemType[] slotGem = new GemType[6];
        private int shown;

        private void Awake()
        {
            Group.alpha = 0f;
        }

        public IEnumerator Show(Side side, GemTally tally)
        {
            if (Header != null) Header.text = side == Side.Player ? "YOU" : "ENEMY";
            int slot = 0;
            foreach (var t in Gems.ResolveOrder)
            {
                int eff = tally.EffectiveOf(t);
                if (eff <= 0) continue;
                SlotIcons[slot].gameObject.SetActive(true);
                SlotIcons[slot].sprite = Sprites.Gem(t);
                SlotLabels[slot].text = eff.ToString();
                slotGem[slot] = t;
                slot++;
            }
            shown = slot;
            for (int i = 0; i < slot; i++)
            {
                var rt = SlotIcons[i].rectTransform;
                rt.anchoredPosition = new Vector2((i - (slot - 1) * 0.5f) * SlotSpacing, rt.anchoredPosition.y);
            }
            for (; slot < SlotIcons.Length; slot++) SlotIcons[slot].gameObject.SetActive(false);
            yield return Tween.Run(0.15f, t => Group.alpha = t);
            yield return Tween.Wait(0.45f);
        }

        /// <summary>Hides the icon of this gem and of every gem resolved before it.</summary>
        public void Consume(GemType gem)
        {
            int order = System.Array.IndexOf(Gems.ResolveOrder, gem);
            if (order < 0) return;
            for (int i = 0; i < shown; i++)
                if (System.Array.IndexOf(Gems.ResolveOrder, slotGem[i]) <= order) SlotIcons[i].gameObject.SetActive(false);
        }

        public IEnumerator Hide()
        {
            yield return Tween.Run(0.2f, t => Group.alpha = 1f - t);
        }
    }
}
