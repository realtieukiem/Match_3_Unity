using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Pooled UI-sprite particles (bursts, rings, homing orbs) plus a screen flash, drawn on an overlay canvas.</summary>
    public sealed class VfxLayer : MonoBehaviour
    {
        public static VfxLayer Instance { get; private set; }

        public Image Template;
        public Image Flash;
        public Sprite Dot;
        public Sprite RingSprite;
        public Sprite Star;
        [Tooltip("Hard cap on live particles.")]
        public int MaxParticles = 400;

        public int ActiveCount { get; private set; }
        public int SpawnedTotal { get; private set; }
        public int BehindCount { get; private set; }

        private enum Kind { Burst, RingFx, Orb, Decal, Rise, Orbit }

        private sealed class P
        {
            public Image Img;
            public RectTransform Rt;
            public RectTransform Body;
            public Kind Kind;
            public Vector2 Pos, Vel, From, Ctrl, To;
            public float Life, MaxLife, Delay, Size0, Size1, Spin, Rot, Gravity, Drag, Angle;
            public Color Color;
            public Action Arrived;
            public bool Alive;
        }

        private readonly List<P> live = new List<P>();
        private readonly Stack<P> pool = new Stack<P>();
        private RectTransform rect;
        private float flashAlpha;
        private float flashDecay;
        private Color flashColor;

        private void Awake()
        {
            Instance = this;
            rect = (RectTransform)transform;
            if (Template != null) Template.gameObject.SetActive(false);
            if (Flash != null)
            {
                Flash.raycastTarget = false;
                Flash.color = new Color(1, 1, 1, 0);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Vector2 ToLocal(Vector3 world) => rect.InverseTransformPoint(world);

        private P Spawn(Kind kind, Sprite sprite, Color c, Vector2 pos, float life, float size0, float size1)
        {
            if (live.Count >= MaxParticles || Template == null) return null;
            var p = pool.Count > 0 ? pool.Pop() : NewParticle();
            p.Kind = kind;
            p.Img.sprite = sprite != null ? sprite : Dot;
            p.Color = c;
            p.Pos = pos;
            p.Vel = Vector2.zero;
            p.Life = 0;
            p.MaxLife = Mathf.Max(0.05f, life);
            p.Delay = 0;
            p.Size0 = size0;
            p.Size1 = size1;
            p.Spin = 0;
            p.Rot = 0;
            p.Gravity = 0;
            p.Drag = 0;
            p.Angle = 0;
            p.Arrived = null;
            p.Body = null;
            p.Alive = true;
            p.Rt.anchoredPosition = pos;
            p.Rt.sizeDelta = new Vector2(size0, size0);
            p.Img.color = new Color(c.r, c.g, c.b, 0f);
            p.Img.gameObject.SetActive(true);
            p.Rt.SetAsLastSibling();
            live.Add(p);
            SpawnedTotal++;
            return p;
        }

        private P NewParticle()
        {
            var img = Instantiate(Template, transform);
            img.raycastTarget = false;
            return new P { Img = img, Rt = img.rectTransform };
        }

        /// <summary>Radial burst; gravity > 0 falls (debris, confetti), &lt; 0 rises (sparkles).</summary>
        public void Burst(Vector3 world, Color color, int count, float speed = 600f, float size = 22f, float life = 0.55f, float gravity = 900f, Sprite sprite = null, float spread = 360f, float direction = 90f)
        {
            var c = ToLocal(world);
            for (int i = 0; i < count; i++)
            {
                float a = (direction + UnityEngine.Random.Range(-spread * 0.5f, spread * 0.5f)) * Mathf.Deg2Rad;
                float sp = speed * UnityEngine.Random.Range(0.45f, 1f);
                var tint = Color.Lerp(color, Color.white, UnityEngine.Random.Range(0f, 0.45f));
                var p = Spawn(Kind.Burst, sprite, tint, c, life * UnityEngine.Random.Range(0.7f, 1.1f), size * UnityEngine.Random.Range(0.6f, 1.2f), 0f);
                if (p == null) return;
                p.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
                p.Gravity = gravity;
                p.Drag = 1.8f;
                p.Spin = UnityEngine.Random.Range(-540f, 540f);
            }
        }

        public void Ring(Vector3 world, Color color, float size = 260f, float life = 0.4f)
        {
            Spawn(Kind.RingFx, RingSprite, color, ToLocal(world), life, size * 0.2f, size);
        }

        /// <summary>Authored effect sprite: pops in with overshoot, holds, then fades. Rot in degrees.</summary>
        public void Decal(Vector3 world, Sprite sprite, float size, float life = 0.6f, float rot = 0f, Color? tint = null)
        {
            if (sprite == null) return;
            var p = Spawn(Kind.Decal, sprite, tint ?? Color.white, ToLocal(world), life, size * 0.55f, size);
            if (p != null) p.Rot = rot;
        }

        /// <summary>Buff on a body: a glow swells, a ring opens at the feet and the icons float up one after another.</summary>
        public void Buff(Vector3 world, Sprite icon, Color color, int icons = 7, float width = 240f, float rise = 280f, float iconSize = 60f)
        {
            var c = ToLocal(world);
            Spawn(Kind.RingFx, Dot, new Color(color.r, color.g, color.b, 0.5f), c, 0.55f, width * 0.6f, width * 2f);
            Spawn(Kind.RingFx, RingSprite, color, c + new Vector2(0f, -rise * 0.4f), 0.6f, width * 0.3f, width * 1.7f);
            for (int i = 0; i < icons; i++)
            {
                var from = c + new Vector2(UnityEngine.Random.Range(-width * 0.5f, width * 0.5f), UnityEngine.Random.Range(-rise * 0.45f, -rise * 0.1f));
                var p = Spawn(Kind.Rise, icon, icon != null ? Color.white : color, from, UnityEngine.Random.Range(0.75f, 1.05f), iconSize * UnityEngine.Random.Range(0.6f, 1f), 0f);
                if (p == null) return;
                p.Vel = new Vector2(UnityEngine.Random.Range(-18f, 18f), rise / p.MaxLife * UnityEngine.Random.Range(0.8f, 1.1f));
                p.Delay = i * 0.07f;
            }
        }

        /// <summary>Fire vortex at the feet: flames circle on a flat ellipse, widening and climbing as they go. With a body, the ground glow and the far half of the circle draw behind it.</summary>
        public void Swirl(Vector3 feetWorld, Sprite flame, Color color, RectTransform body = null, int flames = 20, float radiusX = 170f, float radiusY = 44f, float rise = 110f, float size = 84f, float turnsPerSecond = 1.3f)
        {
            var c = ToLocal(feetWorld);
            var glow = Spawn(Kind.RingFx, Dot, new Color(color.r, color.g, color.b, 0.55f), c, 0.7f, radiusX * 0.8f, radiusX * 2.4f);
            if (glow != null) glow.Body = body;
            var ground = Spawn(Kind.RingFx, RingSprite, color, c, 0.6f, radiusX * 0.4f, radiusX * 2.2f);
            if (ground != null) ground.Body = body;
            for (int i = 0; i < flames; i++)
            {
                var p = Spawn(Kind.Orbit, flame, flame != null ? Color.white : color, c, UnityEngine.Random.Range(0.85f, 1.1f), size * UnityEngine.Random.Range(0.65f, 1f), 0f);
                if (p == null) return;
                p.Body = body;
                p.From = c;
                p.Ctrl = new Vector2(radiusX, radiusY);
                p.Angle = i * 137.5f;
                p.Spin = 360f * turnsPerSecond;
                p.Vel = new Vector2(0f, rise / p.MaxLife * UnityEngine.Random.Range(0.5f, 1f));
                p.Delay = i * 0.03f;
            }
        }

        /// <summary>Suction: motes are torn off one body and pulled into the other, which closes rings around itself. Returns the time until the first lands.</summary>
        public float Siphon(Vector3 fromWorld, Vector3 toWorld, Color color, Sprite icon = null, int motes = 30, float spread = 130f, float travel = 0.55f)
        {
            var from = ToLocal(fromWorld);
            var to = ToLocal(toWorld);
            Spawn(Kind.RingFx, RingSprite, color, from, 0.4f, 90f, 340f);
            for (int i = 0; i < motes; i++)
            {
                var start = from + UnityEngine.Random.insideUnitCircle * spread;
                var p = Spawn(Kind.Orb, icon, icon != null ? Color.white : Color.Lerp(color, Color.white, UnityEngine.Random.Range(0f, 0.3f)), start, travel, UnityEngine.Random.Range(40f, 66f), 22f);
                if (p == null) break;
                p.From = start;
                p.To = to + UnityEngine.Random.insideUnitCircle * 24f;
                p.Ctrl = (start + p.To) * 0.5f + Vector2.Perpendicular(p.To - start).normalized * UnityEngine.Random.Range(-170f, 170f);
                p.Delay = i * 0.022f;
            }
            for (int r = 0; r < 3; r++)
            {
                var ring = Spawn(Kind.RingFx, RingSprite, new Color(color.r, color.g, color.b, 0.6f), to, 0.4f, 300f, 60f);
                if (ring != null) ring.Delay = travel * 0.5f + r * 0.16f;
            }
            return travel;
        }

        /// <summary>Homing orbs on a curved path; onFirstArrive fires once when the first lands. Returns the time until then.</summary>
        public float Orbs(IList<Vector3> fromWorld, Vector3 toWorld, Color color, int perSource = 2, float travel = 0.42f, float size = 26f, Action onFirstArrive = null)
        {
            var to = ToLocal(toWorld);
            bool first = true;
            float firstTime = travel;
            int k = 0;
            foreach (var w in fromWorld)
            {
                for (int i = 0; i < perSource; i++)
                {
                    var from = ToLocal(w) + UnityEngine.Random.insideUnitCircle * 14f;
                    var tint = Color.Lerp(color, Color.white, 0.35f);
                    var p = Spawn(Kind.Orb, Dot, tint, from, travel, size, size * 0.6f);
                    if (p == null) break;
                    p.From = from;
                    p.To = to;
                    var mid = (from + to) * 0.5f;
                    var side = Vector2.Perpendicular(to - from).normalized * UnityEngine.Random.Range(-260f, 260f);
                    p.Ctrl = mid + side + new Vector2(0, 120f);
                    p.Delay = k * 0.025f;
                    if (first)
                    {
                        p.Arrived = onFirstArrive;
                        firstTime = travel + p.Delay;
                        first = false;
                    }
                    k++;
                }
            }
            if (first) onFirstArrive?.Invoke();
            return first ? 0f : firstTime;
        }

        public void ScreenFlash(Color color, float alpha = 0.45f, float seconds = 0.25f)
        {
            if (Flash == null) return;
            flashColor = color;
            flashAlpha = alpha;
            flashDecay = alpha / Mathf.Max(0.05f, seconds);
            Flash.transform.SetAsLastSibling();
        }

        public void Clear()
        {
            foreach (var p in live) Release(p, false);
            live.Clear();
            ActiveCount = 0;
        }

        private void Release(P p, bool remove)
        {
            p.Alive = false;
            p.Body = null;
            if (p.Rt.parent != transform)
            {
                p.Rt.SetParent(transform, false);
                p.Rt.localScale = Vector3.one;
            }
            p.Img.gameObject.SetActive(false);
            pool.Push(p);
            if (remove) live.Remove(p);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * Mathf.Max(0.01f, Tween.Speed);
            int behind = 0;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                if (p.Delay > 0f)
                {
                    p.Delay -= dt;
                    continue;
                }
                p.Life += dt;
                float k = Mathf.Clamp01(p.Life / p.MaxLife);
                switch (p.Kind)
                {
                    case Kind.Burst:
                        p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                        p.Vel.y -= p.Gravity * dt;
                        p.Pos += p.Vel * dt;
                        p.Rot += p.Spin * dt;
                        Draw(p, Mathf.Lerp(p.Size0, p.Size1, k * k), k < 0.1f ? k / 0.1f : 1f - Mathf.Max(0f, k - 0.4f) / 0.6f);
                        break;
                    case Kind.RingFx:
                        Draw(p, Mathf.Lerp(p.Size0, p.Size1, 1f - (1f - k) * (1f - k)), 1f - k);
                        break;
                    case Kind.Decal:
                        float q = Mathf.Clamp01(k / 0.22f) - 1f;
                        float pop = 1f + 2.70158f * q * q * q + 1.70158f * q * q;
                        Draw(p, Mathf.LerpUnclamped(p.Size0, p.Size1, pop), k < 0.55f ? 1f : (1f - k) / 0.45f);
                        break;
                    case Kind.Rise:
                        p.Pos += p.Vel * dt;
                        float grow = Mathf.Clamp01(k / 0.2f);
                        Draw(p, p.Size0 * (0.4f + 0.6f * grow * (2f - grow)), k < 0.15f ? k / 0.15f : 1f - Mathf.Max(0f, k - 0.55f) / 0.45f);
                        break;
                    case Kind.Orbit:
                        p.Angle += p.Spin * dt;
                        float rad = p.Angle * Mathf.Deg2Rad;
                        float widen = 0.5f + 0.5f * k;
                        p.Pos = p.From + new Vector2(Mathf.Cos(rad) * p.Ctrl.x * widen, Mathf.Sin(rad) * p.Ctrl.y * widen + p.Vel.y * p.Life);
                        Draw(p, p.Size0 * (0.8f - 0.2f * Mathf.Sin(rad)) * Mathf.Min(1f, 0.4f + k * 3f), k < 0.12f ? k / 0.12f : 1f - Mathf.Max(0f, k - 0.6f) / 0.4f);
                        break;
                    case Kind.Orb:
                        float e = k * k;
                        float u = 1f - e;
                        p.Pos = u * u * p.From + 2f * u * e * p.Ctrl + e * e * p.To;
                        Draw(p, Mathf.Lerp(p.Size0, p.Size1, k), k < 0.15f ? k / 0.15f : 1f);
                        break;
                }
                if (p.Body != null && Seat(p, p.Kind != Kind.Orbit || Mathf.Sin(p.Angle * Mathf.Deg2Rad) > 0f)) behind++;
                if (k >= 1f)
                {
                    var arrived = p.Arrived;
                    p.Arrived = null;
                    live.RemoveAt(i);
                    Release(p, false);
                    arrived?.Invoke();
                }
            }
            ActiveCount = live.Count;
            BehindCount = behind;
            if (Flash != null)
            {
                if (flashAlpha > 0f)
                {
                    flashAlpha = Mathf.Max(0f, flashAlpha - flashDecay * dt);
                    Flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
                }
                else if (Flash.color.a != 0f) Flash.color = new Color(1, 1, 1, 0);
            }
        }

        private bool Seat(P p, bool behind)
        {
            var parent = behind ? p.Body.parent : transform;
            if (p.Rt.parent != parent)
            {
                p.Rt.SetParent(parent, false);
                p.Rt.localScale = behind ? Vector3.one * (rect.lossyScale.x / Mathf.Max(0.0001f, parent.lossyScale.x)) : Vector3.one;
                if (behind) p.Rt.SetSiblingIndex(p.Body.GetSiblingIndex());
                else p.Rt.SetAsLastSibling();
            }
            if (behind) p.Rt.position = rect.TransformPoint(p.Pos);
            return behind;
        }

        private static void Draw(P p, float size, float alpha)
        {
            p.Rt.anchoredPosition = p.Pos;
            p.Rt.sizeDelta = new Vector2(size, size);
            p.Rt.localEulerAngles = new Vector3(0, 0, p.Rot);
            p.Img.color = new Color(p.Color.r, p.Color.g, p.Color.b, p.Color.a * Mathf.Clamp01(alpha));
        }
    }
}
