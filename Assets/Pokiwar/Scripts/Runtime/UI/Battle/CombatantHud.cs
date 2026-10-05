using System.Collections;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class CombatantHud : MonoBehaviour
    {
        public Image Portrait;
        public Text NameLabel;
        public Image ElementIcon;
        public BarView Hp;
        public BarView Mana;
        public BarView Rage;
        public Image ShieldBubble;
        public GameObject ShieldBadge;
        public Text ShieldLabel;
        public GameObject TurnMarker;
        public RectTransform AttackArrow;
        public RectTransform FloatAnchor;
        public Text StatusLabel;

        [Tooltip("How far the turn arrow nudges toward the opponent (px).")]
        public float ArrowNudge = 16f;
        [Tooltip("Turn arrow nudges per second.")]
        public float ArrowSpeed = 1.6f;

        private Vector2 portraitHome;
        private bool homeSet;
        private Vector2 arrowHome;
        private bool arrowHomeSet;
        private int shownShield;
        private float direction = 1f;
        private SpriteLibrary lib;
        private string key;
        private bool busy;
        private bool dead;
        private float bobPhase;

        public void Setup(Combatant c, SpriteLibrary sprites, bool facesRight)
        {
            direction = facesRight ? 1f : -1f;
            if (!homeSet)
            {
                portraitHome = Portrait.rectTransform.anchoredPosition;
                homeSet = true;
            }
            Portrait.rectTransform.anchoredPosition = portraitHome;
            Portrait.color = Color.white;
            lib = sprites;
            key = c.SpriteKey;
            busy = false;
            dead = false;
            bobPhase = facesRight ? 0f : 1.3f;
            Portrait.sprite = Pose(null);
            Portrait.rectTransform.localScale = Vector3.one;
            if (ElementIcon != null)
            {
                string ek = "element." + c.Element;
                ElementIcon.gameObject.SetActive(sprites.Has(ek));
                if (sprites.Has(ek)) ElementIcon.sprite = sprites.Get(ek);
            }
            Set(c.Snapshot());
            SetTurn(false);
            SetStatus(c);
        }

        public void Set(CombatantSnapshot s)
        {
            NameLabel.text = s.Name;
            Hp.Set(s.Hp, s.MaxHp);
            Mana.Set(s.Mana, s.MaxMana);
            Rage.Set(s.Rage, s.MaxRage);
            SetShield(s.Shield);
        }

        private void SetShield(int amount)
        {
            bool on = amount > 0;
            if (ShieldBubble != null)
            {
                bool appeared = on && !ShieldBubble.gameObject.activeSelf;
                ShieldBubble.gameObject.SetActive(on);
                if (appeared && isActiveAndEnabled) StartCoroutine(Pop(ShieldBubble.rectTransform, 1.18f));
            }
            if (ShieldBadge != null) ShieldBadge.SetActive(on);
            if (ShieldLabel != null && amount != shownShield) ShieldLabel.text = amount.ToString();
            shownShield = amount;
        }

        public void SetStatus(Combatant c)
        {
            if (StatusLabel == null) return;
            var parts = new System.Collections.Generic.List<string>();
            foreach (var b in c.Buffs) parts.Add(b.Label + " x" + b.AtkMultiplier.ToString("0.##") + " (" + b.TurnsLeft + "t)");
            foreach (var s in c.Summons) parts.Add(s.Name + " " + s.DamagePerTurn + "/t (" + s.TurnsLeft + "t)");
            if (c.Rage.Current >= c.RageRules.AttackThreshold) parts.Add("RAGE READY");
            StatusLabel.text = string.Join("   ", parts);
        }

        public void SetSprite(Sprite s) => Portrait.sprite = s;

        public Vector3 BodyCenter => Portrait.rectTransform.TransformPoint(Portrait.rectTransform.rect.center);

        /// <summary>Sprite for "key.pose" when authored, else the idle sprite.</summary>
        public Sprite Pose(string pose)
        {
            if (lib == null) return Portrait.sprite;
            return pose != null && lib.Has(key + "." + pose) ? lib.Get(key + "." + pose) : lib.Get(key);
        }

        private void Update()
        {
            if (AttackArrow != null && AttackArrow.gameObject.activeSelf)
            {
                float n = Mathf.Abs(Mathf.Sin(Time.unscaledTime * ArrowSpeed * Mathf.PI));
                AttackArrow.anchoredPosition = arrowHome + new Vector2(ArrowNudge * direction * n, 0f);
            }
            if (busy || dead || Portrait == null) return;
            float s = Mathf.Sin((Time.unscaledTime / 1.2f + bobPhase) * Mathf.PI * 2f);
            Portrait.rectTransform.localScale = new Vector3(1f + 0.006f * s, 1f + 0.016f * s, 1f);
        }

        public RectTransform BarRect(ResourceKind k)
        {
            switch (k)
            {
                case ResourceKind.Hp: return (RectTransform)Hp.transform;
                case ResourceKind.Mana: return (RectTransform)Mana.transform;
                case ResourceKind.Rage: return (RectTransform)Rage.transform;
                case ResourceKind.Shield: return ShieldBubble != null ? ShieldBubble.rectTransform : Portrait.rectTransform;
                default: return Portrait.rectTransform;
            }
        }

        public void PopBar(ResourceKind k)
        {
            if (isActiveAndEnabled) StartCoroutine(Pop(BarRect(k), 1.12f));
        }

        public static IEnumerator Pop(RectTransform rt, float peak)
        {
            yield return Tween.Run(0.18f, t =>
            {
                float back = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(peak, 1f, back);
            });
            rt.localScale = Vector3.one;
        }

        public void SetTurn(bool on)
        {
            if (TurnMarker != null) TurnMarker.SetActive(on);
            if (AttackArrow == null) return;
            if (!arrowHomeSet)
            {
                arrowHome = AttackArrow.anchoredPosition;
                arrowHomeSet = true;
            }
            AttackArrow.anchoredPosition = arrowHome;
            AttackArrow.gameObject.SetActive(on);
        }

        public IEnumerator Lunge()
        {
            var rt = Portrait.rectTransform;
            busy = true;
            rt.localScale = Vector3.one;
            yield return Tween.Run(0.1f, t => rt.anchoredPosition = portraitHome + new Vector2(-14f * direction * t, 0));
            Portrait.sprite = Pose("attack");
            yield return Tween.Run(0.1f, t => rt.anchoredPosition = portraitHome + new Vector2(direction * Mathf.Lerp(-14f, 60f, t), 0));
            yield return Tween.Run(0.16f, t => rt.anchoredPosition = portraitHome + new Vector2(60f * direction * (1f - t), 0));
            if (!dead) Portrait.sprite = Pose(null);
            busy = false;
        }

        public IEnumerator Shake()
        {
            var rt = Portrait.rectTransform;
            busy = true;
            rt.localScale = Vector3.one;
            Portrait.sprite = Pose("hit");
            Portrait.color = new Color(1f, 0.6f, 0.6f);
            yield return Tween.Run(0.28f, t => rt.anchoredPosition = portraitHome + new Vector2(Mathf.Sin(t * 40f) * 14f * (1f - t), 0));
            rt.anchoredPosition = portraitHome;
            Portrait.color = Color.white;
            if (!dead) Portrait.sprite = Pose(null);
            busy = false;
        }

        public IEnumerator Transform(string newKey)
        {
            var rt = Portrait.rectTransform;
            busy = true;
            var baseScale = Vector3.one;
            yield return Tween.Run(0.3f, t => rt.localScale = new Vector3(baseScale.x * (1f - t), baseScale.y * (1f + 0.3f * t), 1f));
            key = newKey;
            Portrait.sprite = Pose(null);
            yield return Tween.Run(0.35f, t => rt.localScale = new Vector3(baseScale.x * t, baseScale.y * (1.3f - 0.3f * t), 1f));
            rt.localScale = baseScale;
            busy = false;
        }

        public IEnumerator Die()
        {
            dead = true;
            Portrait.rectTransform.localScale = Vector3.one;
            bool authored = lib != null && lib.Has(key + ".defeat");
            Portrait.sprite = Pose("defeat");
            float floor = authored ? 0.75f : 0.2f;
            yield return Tween.Run(0.5f, t => Portrait.color = new Color(1f, 1f, 1f, 1f - (1f - floor) * t));
        }
    }
}
