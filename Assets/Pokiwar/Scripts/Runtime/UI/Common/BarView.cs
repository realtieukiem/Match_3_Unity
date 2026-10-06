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

        public void Set(int current, int max)
        {
            float pct = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
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
