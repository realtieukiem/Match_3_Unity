using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class ToastView : MonoBehaviour
    {
        public CanvasGroup Group;
        public Text Label;
        private Coroutine running;

        private void Awake()
        {
            if (Group != null) Group.alpha = 0f;
        }

        public void Show(string message, float seconds = 1.6f)
        {
            if (!isActiveAndEnabled) return;
            Label.text = message;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Play(seconds));
        }

        private IEnumerator Play(float seconds)
        {
            Group.alpha = 1f;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime * 3f)
            {
                Group.alpha = a;
                yield return null;
            }
            Group.alpha = 0f;
        }
    }
}
