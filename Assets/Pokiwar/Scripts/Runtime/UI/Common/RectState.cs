using System;
using UnityEngine;

namespace Pokiwar.UI
{
    [Serializable]
    public struct RectState
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 Position;
        public Vector2 Size;
        public Vector3 Scale;

        public static RectState Capture(RectTransform rt) => new RectState
        {
            AnchorMin = rt.anchorMin,
            AnchorMax = rt.anchorMax,
            Pivot = rt.pivot,
            Position = rt.anchoredPosition,
            Size = rt.sizeDelta,
            Scale = rt.localScale
        };

        public void ApplyTo(RectTransform rt)
        {
            rt.anchorMin = AnchorMin;
            rt.anchorMax = AnchorMax;
            rt.pivot = Pivot;
            rt.anchoredPosition = Position;
            rt.sizeDelta = Size;
            rt.localScale = Scale;
        }
    }
}
