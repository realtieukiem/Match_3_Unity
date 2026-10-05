using System.Collections;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Shows the whole turn's tally (effective count, physical count when different) in resolve order.</summary>
    public sealed class GemSummaryView : MonoBehaviour
    {
        public CanvasGroup Group;
        public Text Header;
        public Image[] SlotIcons = new Image[6];
        public Text[] SlotLabels = new Text[6];
        public SpriteLibrary Sprites;

        private void Awake()
        {
            Group.alpha = 0f;
        }

        public IEnumerator Show(Side side, GemTally tally)
        {
            Header.text = side == Side.Player ? "YOU COLLECTED" : "ENEMY COLLECTED";
            int slot = 0;
            foreach (var t in Gems.ResolveOrder)
            {
                int eff = tally.EffectiveOf(t);
                if (eff <= 0) continue;
                int phys = tally.PhysicalOf(t);
                SlotIcons[slot].gameObject.SetActive(true);
                SlotIcons[slot].sprite = Sprites.Gem(t);
                SlotLabels[slot].text = "x" + eff + (phys != eff ? "\n(" + phys + ")" : "");
                slot++;
            }
            for (; slot < SlotIcons.Length; slot++) SlotIcons[slot].gameObject.SetActive(false);
            yield return Tween.Run(0.15f, t => Group.alpha = t);
            yield return Tween.Wait(0.55f);
        }

        public IEnumerator Hide()
        {
            yield return Tween.Run(0.2f, t => Group.alpha = 1f - t);
        }
    }
}
