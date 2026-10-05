using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokiwar.App
{
    public sealed class ClickSound : MonoBehaviour, IPointerClickHandler
    {
        public string Key = "ui.click";

        public void OnPointerClick(PointerEventData e)
        {
            var sel = GetComponent<Selectable>();
            if (sel != null && !sel.IsInteractable()) return;
            AudioDirector.Sfx(Key);
        }
    }
}
