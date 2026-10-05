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
        public Text InfoLabel;
        public BarView Hp;
        public BarView Mana;
        public BarView Rage;
        public BarView Shield;
        public GameObject TurnMarker;
        public RectTransform FloatAnchor;
        public Text StatusLabel;

        private Vector2 portraitHome;
        private bool homeSet;
        private float direction = 1f;

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
            Portrait.sprite = sprites.Get(c.SpriteKey);
            Portrait.rectTransform.localScale = new Vector3(facesRight ? 1f : -1f, 1f, 1f);
            InfoLabel.text = "Lv " + c.Level + "  " + c.Element + (c.ElementBonus > 0 ? " +" + c.ElementBonus : "") + "  ATK " + c.EffectiveAtk;
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
            Shield.Set(s.Shield, s.MaxShield);
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

        public RectTransform BarRect(ResourceKind k)
        {
            switch (k)
            {
                case ResourceKind.Hp: return (RectTransform)Hp.transform;
                case ResourceKind.Mana: return (RectTransform)Mana.transform;
                case ResourceKind.Rage: return (RectTransform)Rage.transform;
                case ResourceKind.Shield: return (RectTransform)Shield.transform;
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
        }

        public IEnumerator Lunge()
        {
            var rt = Portrait.rectTransform;
            yield return Tween.Run(0.12f, t => rt.anchoredPosition = portraitHome + new Vector2(60f * direction * t, 0));
            yield return Tween.Run(0.12f, t => rt.anchoredPosition = portraitHome + new Vector2(60f * direction * (1f - t), 0));
        }

        public IEnumerator Shake()
        {
            var rt = Portrait.rectTransform;
            Portrait.color = new Color(1f, 0.55f, 0.55f);
            yield return Tween.Run(0.25f, t => rt.anchoredPosition = portraitHome + new Vector2(Mathf.Sin(t * 40f) * 14f * (1f - t), 0));
            rt.anchoredPosition = portraitHome;
            Portrait.color = Color.white;
        }

        public IEnumerator Transform(Sprite newSprite)
        {
            var rt = Portrait.rectTransform;
            var baseScale = rt.localScale;
            yield return Tween.Run(0.3f, t => rt.localScale = new Vector3(baseScale.x * (1f - t), baseScale.y * (1f + 0.3f * t), 1f));
            Portrait.sprite = newSprite;
            yield return Tween.Run(0.35f, t => rt.localScale = new Vector3(baseScale.x * t, baseScale.y * (1.3f - 0.3f * t), 1f));
            rt.localScale = baseScale;
        }

        public IEnumerator Die()
        {
            yield return Tween.Run(0.5f, t => Portrait.color = new Color(1f, 1f, 1f, 1f - 0.8f * t));
        }
    }
}
