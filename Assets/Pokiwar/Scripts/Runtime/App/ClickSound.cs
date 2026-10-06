using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokiwar.App
{
    public sealed class ClickSound : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public string Key = "ui.click";
        [Tooltip("Scale while the button is held down. 1 = no press feedback.")]
        public float PressScale = 0.94f;

        private Vector3 rest;
        private bool pressed;

        public void OnPointerClick(PointerEventData e)
        {
            if (!Usable()) return;
            AudioDirector.Sfx(Key);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pressed || !Usable()) return;
            pressed = true;
            rest = transform.localScale;
            transform.localScale = rest * PressScale;
        }

        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerExit(PointerEventData e) => Release();

        private void OnDisable() => Release();

        private void Release()
        {
            if (!pressed) return;
            pressed = false;
            transform.localScale = rest;
        }

        private bool Usable()
        {
            var sel = GetComponent<Selectable>();
            return sel == null || sel.IsInteractable();
        }
    }
}
