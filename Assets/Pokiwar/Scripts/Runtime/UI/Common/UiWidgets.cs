using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public static class Tween
    {
        public static float Speed = 1f;

        public static IEnumerator Run(float duration, Action<float> step)
        {
            float d = duration / Mathf.Max(0.01f, Speed);
            float t = 0f;
            while (t < d)
            {
                step(Mathf.Clamp01(t / d));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            step(1f);
        }

        public static IEnumerator Wait(float seconds)
        {
            float d = seconds / Mathf.Max(0.01f, Speed);
            float t = 0f;
            while (t < d)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
        }

        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseIn(float t) => t * t;
    }

    /// <summary>Clones a disabled template child for data-driven rows (map nodes, pet and card lists).</summary>
    public sealed class TemplateList<T> where T : Component
    {
        public readonly T Template;
        private readonly List<T> items = new List<T>();

        public TemplateList(T template)
        {
            Template = template;
            if (template != null) template.gameObject.SetActive(false);
        }

        public IReadOnlyList<T> Items => items;

        public T Add()
        {
            var item = UnityEngine.Object.Instantiate(Template, Template.transform.parent);
            item.gameObject.SetActive(true);
            items.Add(item);
            return item;
        }

        public void Clear()
        {
            foreach (var i in items) if (i != null) UnityEngine.Object.Destroy(i.gameObject);
            items.Clear();
            if (Template != null) Template.gameObject.SetActive(false);
        }
    }
}
