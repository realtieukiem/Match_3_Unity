using UnityEngine;

namespace Pokiwar.UI
{
    /// <summary>Scales a painted map and everything placed on it as one piece, so markers stay on their spots on any screen shape.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MapFitter : MonoBehaviour
    {
        [Tooltip("The map and its markers. Laid out at Design Size, then scaled and moved as one piece.")]
        public RectTransform Content;
        [Tooltip("Size the map was laid out at.")]
        public Vector2 DesignSize = new Vector2(1920f, 1080f);
        [Tooltip("Part of the map kept on screen, in map units from the map centre: left, bottom, width, height. The width is never cropped; the height is centred between the bars.")]
        public Rect SafeArea = new Rect(-880f, -260f, 1760f, 690f);
        [Tooltip("Screen height covered by the bar at the top, in canvas units.")]
        public float TopInset = 100f;
        [Tooltip("Screen height covered by the bar at the bottom, in canvas units.")]
        public float BottomInset = 130f;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            if (Content == null) return;
            Vector2 screen = ((RectTransform)transform).rect.size;
            if (screen.x <= 0f || screen.y <= 0f || DesignSize.x <= 0f || DesignSize.y <= 0f) return;
            float cover = Mathf.Max(screen.x / DesignSize.x, screen.y / DesignSize.y);
            float scale = Mathf.Min(cover, screen.x / Mathf.Max(1f, SafeArea.width));
            Vector2 target = new Vector2(0f, (BottomInset - TopInset) * 0.5f) - SafeArea.center * scale;
            Vector2 slack = (DesignSize * scale - screen) * 0.5f;
            Content.anchorMin = Content.anchorMax = Content.pivot = new Vector2(0.5f, 0.5f);
            Content.sizeDelta = DesignSize;
            Content.localScale = new Vector3(scale, scale, 1f);
            Content.anchoredPosition = new Vector2(
                slack.x >= 0f ? Mathf.Clamp(target.x, -slack.x, slack.x) : target.x,
                slack.y >= 0f ? Mathf.Clamp(target.y, -slack.y, slack.y) : -slack.y - TopGap(-slack.y * 2f));
        }

        private float TopGap(float gap)
        {
            float bars = TopInset + BottomInset;
            return bars <= 0f ? gap * 0.5f : Mathf.Min(TopInset, gap * TopInset / bars);
        }
    }
}
