using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class BarView : MonoBehaviour
    {
        public Image Fill;
        public Text Label;
        public string Prefix;
        [Tooltip("Shows the next layer under the fill while a layered bar drains.")]
        public Image Under;
        [Tooltip("Colour of each layer of a layered bar, first to last.")]
        public Color[] LayerColors = { new Color(0.3f, 0.82f, 0.25f), new Color(1f, 0.78f, 0.12f), new Color(0.92f, 0.2f, 0.22f) };

        private int layers = 1;
        private int layerSpan;
        private bool homeKnown;
        private Color home;

        /// <summary>Splits the bar into layers of span points each; the bar empties once per layer. One layer is a plain bar.</summary>
        public void SetLayers(int count, int span)
        {
            if (Fill != null && !homeKnown)
            {
                home = Fill.color;
                homeKnown = true;
            }
            layers = Mathf.Max(1, Mathf.Min(count, LayerColors.Length));
            layerSpan = Mathf.Max(1, span);
            if (layers == 1)
            {
                if (Fill != null) Fill.color = home;
                if (Under != null) Under.enabled = false;
            }
        }

        public int LayersLeft(int current) => layers <= 1 ? 1 : Mathf.Clamp(Mathf.CeilToInt((float)current / layerSpan), 1, layers);

        public void Set(int current, int max)
        {
            float pct = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (layers > 1)
            {
                int left = LayersLeft(current);
                pct = current <= 0 ? 0f : Mathf.Clamp01((float)(current - (left - 1) * layerSpan) / layerSpan);
                if (Fill != null) Fill.color = LayerColors[layers - left];
                if (Under != null)
                {
                    Under.enabled = left > 1;
                    if (left > 1) Under.color = LayerColors[layers - left + 1];
                }
            }
            if (Fill != null)
            {
                if (Fill.type == Image.Type.Filled) Fill.fillAmount = pct;
                else
                {
                    Fill.enabled = pct > 0f;
                    Fill.rectTransform.anchorMax = new Vector2(pct, 1f);
                }
            }
            if (Label != null) Label.text = (string.IsNullOrEmpty(Prefix) ? "" : Prefix + " ") + current + "/" + max;
        }
    }
}
