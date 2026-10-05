using System;
using System.Collections;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Five-arrow combo followed by a timing bar. Produces a QteResult; the damage itself is computed by the engine.</summary>
    public sealed class QteView : MonoBehaviour
    {
        public GameObject Root;
        public Text Title;
        public Text DamageLabel;
        public Text Feedback;
        public Text Hint;
        public Image[] Arrows = new Image[5];
        public Image TimerFill;
        public RectTransform BarArea;
        public RectTransform Marker;
        public RectTransform GoodZone;
        public RectTransform PerfectZone;
        public Button Up;
        public Button Down;
        public Button Left;
        public Button Right;
        public Button Strike;

        private QteDir? pendingDir;
        private bool strikePressed;

        private void Awake()
        {
            Up.onClick.AddListener(() => pendingDir = QteDir.Up);
            Down.onClick.AddListener(() => pendingDir = QteDir.Down);
            Left.onClick.AddListener(() => pendingDir = QteDir.Left);
            Right.onClick.AddListener(() => pendingDir = QteDir.Right);
            Strike.onClick.AddListener(() => strikePressed = true);
            Root.SetActive(false);
        }

        private static float Angle(QteDir d)
        {
            switch (d)
            {
                case QteDir.Up: return 0f;
                case QteDir.Left: return 90f;
                case QteDir.Down: return 180f;
                default: return -90f;
            }
        }

        private void ReadKeys()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) pendingDir = QteDir.Up;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) pendingDir = QteDir.Down;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) pendingDir = QteDir.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) pendingDir = QteDir.Right;
            if (Input.GetKeyDown(KeyCode.Space)) strikePressed = true;
        }

        /// <param name="scripted">When set (AI or autoplay), the view only plays this result back.</param>
        public IEnumerator Run(SkillCheck check, string casterName, QteResult? scripted, Action<QteResult> done)
        {
            var qte = check.Skill.Qte;
            Root.SetActive(true);
            Title.text = casterName + " - " + check.Skill.Name;
            Feedback.text = "";
            Hint.text = scripted.HasValue ? "" : "Press the arrows in order (keys or buttons)";
            SetButtons(!scripted.HasValue, true);
            int n = Mathf.Min(qte.ArrowCount, Arrows.Length);
            for (int i = 0; i < Arrows.Length; i++)
            {
                Arrows[i].gameObject.SetActive(i < n);
                if (i < n)
                {
                    Arrows[i].rectTransform.localEulerAngles = new Vector3(0, 0, Angle(check.Sequence[i]));
                    Arrows[i].color = new Color(1f, 1f, 1f, 0.9f);
                }
            }
            DamageLabel.text = "DMG " + check.PreviewBase;
            float barHalf = BarArea.rect.width * 0.5f;
            GoodZone.sizeDelta = new Vector2(qte.GoodHalfWidth * barHalf * 2f, GoodZone.sizeDelta.y);
            PerfectZone.sizeDelta = new Vector2(qte.PerfectHalfWidth * barHalf * 2f, PerfectZone.sizeDelta.y);
            Marker.anchoredPosition = new Vector2(-barHalf, 0);

            int correct = 0;
            int idx = 0;
            pendingDir = null;
            strikePressed = false;
            float timeLeft = qte.ArrowSeconds;
            int scriptedWrongLeft = scripted.HasValue ? n - Mathf.Clamp(scripted.Value.Correct, 0, n) : 0;
            float scriptedDelay = 0.18f;
            while (idx < n && timeLeft > 0f)
            {
                QteDir? input = null;
                if (scripted.HasValue)
                {
                    yield return Tween.Wait(scriptedDelay);
                    bool wrong = scriptedWrongLeft > 0 && (n - idx) <= scriptedWrongLeft + (idx % 2);
                    if (wrong && scriptedWrongLeft > 0)
                    {
                        scriptedWrongLeft--;
                        input = (QteDir)(((int)check.Sequence[idx] + 1) % 4);
                    }
                    else input = check.Sequence[idx];
                }
                else
                {
                    ReadKeys();
                    timeLeft -= Time.unscaledDeltaTime * Tween.Speed;
                    TimerFill.fillAmount = Mathf.Clamp01(timeLeft / qte.ArrowSeconds);
                    if (pendingDir.HasValue)
                    {
                        input = pendingDir;
                        pendingDir = null;
                    }
                }
                if (input.HasValue)
                {
                    bool ok = input.Value == check.Sequence[idx];
                    Arrows[idx].color = ok ? new Color(0.35f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
                    if (ok) correct++;
                    AudioDirector.Sfx(ok ? "qte.ok" : "qte.bad", ok ? Mathf.Pow(2f, correct * 2f / 12f) : 1f, 1f, false);
                    if (ok && VfxLayer.Instance != null) VfxLayer.Instance.Burst(Arrows[idx].rectTransform.position, new Color(0.4f, 1f, 0.5f), 10, 420f, 16f, 0.4f, 600f, VfxLayer.Instance.Star);
                    DamageLabel.text = "DMG " + QteMath.Stepped(check.PreviewBase, qte, correct);
                    idx++;
                }
                if (!scripted.HasValue) yield return null;
            }
            for (int i = idx; i < n; i++) Arrows[i].color = new Color(1f, 0.3f, 0.3f, 0.6f);

            SetButtons(false, !scripted.HasValue);
            Hint.text = scripted.HasValue ? "" : "Tap STRIKE (or Space) when the marker is in the gold zone";
            QteTiming timing;
            if (scripted.HasValue)
            {
                timing = scripted.Value.Timing;
                float target = timing == QteTiming.Perfect ? 0f : timing == QteTiming.Good ? qte.GoodHalfWidth * 0.8f : 0.7f;
                yield return Tween.Run(0.5f, t => Marker.anchoredPosition = new Vector2(Mathf.Lerp(-barHalf, target * barHalf, t), 0));
            }
            else
            {
                float t = 0f;
                timing = QteTiming.Miss;
                strikePressed = false;
                bool struck = false;
                while (t < qte.BarSeconds)
                {
                    ReadKeys();
                    t += Time.unscaledDeltaTime * Tween.Speed;
                    float phase = Mathf.PingPong(t * qte.BarCyclesPerSecond * 2f, 1f);
                    float pos = phase * 2f - 1f;
                    Marker.anchoredPosition = new Vector2(pos * barHalf, 0);
                    TimerFill.fillAmount = 1f - t / qte.BarSeconds;
                    if (strikePressed)
                    {
                        timing = QteMath.Judge(qte, pos);
                        struck = true;
                        break;
                    }
                    yield return null;
                }
                if (!struck) timing = QteTiming.Miss;
            }
            var result = new QteResult(correct, timing);
            Feedback.text = timing == QteTiming.Perfect ? "PERFECT!" : timing == QteTiming.Good ? "GOOD" : "MISS";
            Feedback.color = timing == QteTiming.Perfect ? new Color(1f, 0.85f, 0.2f) : timing == QteTiming.Good ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            DamageLabel.text = "DMG " + QteMath.Apply(check.PreviewBase, qte, result);
            AudioDirector.Sfx(timing == QteTiming.Perfect ? "qte.perfect" : timing == QteTiming.Good ? "qte.good" : "qte.miss", 1f, 1f, false);
            if (timing != QteTiming.Miss && VfxLayer.Instance != null)
            {
                VfxLayer.Instance.Burst(Feedback.rectTransform.position, timing == QteTiming.Perfect ? new Color(1f, 0.85f, 0.2f) : new Color(0.4f, 1f, 0.5f), timing == QteTiming.Perfect ? 30 : 14, 800f, 22f, 0.6f, 600f, VfxLayer.Instance.Star);
                VfxLayer.Instance.Ring(Marker.position, Feedback.color, 300f, 0.35f);
            }
            StartCoroutine(CombatantHud.Pop(Feedback.rectTransform, 1.5f));
            SetButtons(false, false);
            yield return Tween.Wait(0.7f);
            Root.SetActive(false);
            done(result);
        }

        private void SetButtons(bool arrows, bool strike)
        {
            Up.interactable = Down.interactable = Left.interactable = Right.interactable = arrows;
            Strike.interactable = strike;
        }
    }
}
