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
            if (Fill != null) Fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (Label != null) Label.text = (string.IsNullOrEmpty(Prefix) ? "" : Prefix + " ") + current + "/" + max;
        }
    }
}
