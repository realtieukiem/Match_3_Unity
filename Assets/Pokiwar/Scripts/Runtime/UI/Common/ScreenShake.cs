using UnityEngine;

namespace Pokiwar.UI
{
    /// <summary>Trauma-based shake of a visual container; offset = max * trauma^2 * smooth noise, always returns to rest.</summary>
    public sealed class ScreenShake : MonoBehaviour
    {
        public RectTransform Target;
        [Tooltip("Max offset in canvas units at trauma 1.")]
        public float MaxOffset = 18f;
        [Tooltip("Max roll in degrees at trauma 1.")]
        public float MaxRoll = 1.2f;
        [Tooltip("Trauma lost per second.")]
        public float Decay = 1.8f;
        public bool Enabled = true;

        public float Trauma { get; private set; }
        public bool AtRest => Trauma <= 0f && Target != null && Target.anchoredPosition == rest && Target.localEulerAngles.z == 0f;

        private Vector2 rest;
        private float t;
        private bool restCaptured;

        private void Awake()
        {
            if (Target == null) Target = (RectTransform)transform;
            rest = Target.anchoredPosition;
            restCaptured = true;
        }

        public void Add(float amount)
        {
            if (!Enabled || amount <= 0f) return;
            Trauma = Mathf.Clamp01(Trauma + amount);
        }

        public void Stop()
        {
            Trauma = 0f;
            if (restCaptured && Target != null)
            {
                Target.anchoredPosition = rest;
                Target.localEulerAngles = Vector3.zero;
            }
        }

        private void LateUpdate()
        {
            if (Trauma <= 0f) return;
            float dt = Time.unscaledDeltaTime;
            Trauma = Mathf.Max(0f, Trauma - Decay * dt);
            if (Trauma <= 0f)
            {
                Stop();
                return;
            }
            t += dt;
            float s = Trauma * Trauma;
            float x = 0.6f * Mathf.Sin(t * 37f + 1.3f) + 0.4f * Mathf.Sin(t * 71f + 0.4f);
            float y = 0.6f * Mathf.Sin(t * 43f + 2.1f) + 0.4f * Mathf.Sin(t * 83f + 3.7f);
            float r = Mathf.Sin(t * 29f + 0.9f);
            Target.anchoredPosition = rest + new Vector2(x, y) * MaxOffset * s;
            Target.localEulerAngles = new Vector3(0, 0, r * MaxRoll * s);
        }

        private void OnDisable() => Stop();
    }
}
