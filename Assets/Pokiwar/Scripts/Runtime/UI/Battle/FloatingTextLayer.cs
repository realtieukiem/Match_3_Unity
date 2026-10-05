using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class FloatingTextLayer : MonoBehaviour
    {
        public Text Template;
        private readonly Stack<Text> pool = new Stack<Text>();

        private void Awake()
        {
            if (Template != null) Template.gameObject.SetActive(false);
        }

        public void Spawn(string text, Color color, RectTransform anchor, Vector2 offset, int size = 40)
        {
            if (!isActiveAndEnabled || Template == null) return;
            var t = pool.Count > 0 ? pool.Pop() : Instantiate(Template, transform);
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = color;
            t.fontSize = size;
            var rt = (RectTransform)t.transform;
            Vector3 world = anchor != null ? anchor.position : transform.position;
            rt.position = world;
            rt.anchoredPosition += offset;
            StartCoroutine(Rise(t, rt.anchoredPosition));
        }

        private IEnumerator Rise(Text t, Vector2 start)
        {
            var rt = (RectTransform)t.transform;
            var c = t.color;
            yield return Tween.Run(1.1f, k =>
            {
                rt.anchoredPosition = start + new Vector2(0, 90f * Tween.EaseOut(k));
                rt.localScale = Vector3.one * (k < 0.15f ? 0.6f + k / 0.15f * 0.5f : 1.1f - 0.1f * k);
                t.color = new Color(c.r, c.g, c.b, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
            });
            t.gameObject.SetActive(false);
            pool.Push(t);
        }
    }
}
