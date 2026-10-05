using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Picks landscape or portrait from the screen shape and applies every OrientationLayout under the canvas.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvas : MonoBehaviour
    {
        public enum Mode { Auto, Landscape, Portrait }

        public static bool IsPortrait { get; private set; }
        public static event Action<bool> Changed;

        [Tooltip("Auto = theo hinh dang man hinh; chon Landscape/Portrait de ep mot huong khi test")]
        public Mode Force = Mode.Auto;
        public Vector2 LandscapeReference = new Vector2(1920, 1080);
        public Vector2 PortraitReference = new Vector2(1080, 1920);

        private OrientationLayout[] items;
        private CanvasScaler scaler;
        private int current = -1;

        private void Awake()
        {
            Collect();
            Apply(Detect());
        }

        private void Update()
        {
            bool p = Detect();
            if ((p ? 1 : 0) != current) Apply(p);
        }

        private bool Detect() => Force == Mode.Portrait || (Force == Mode.Auto && Screen.height > Screen.width);

        private void Collect()
        {
            scaler = GetComponent<CanvasScaler>();
            items = GetComponentsInChildren<OrientationLayout>(true);
        }

        public void Apply(bool portrait)
        {
            current = portrait ? 1 : 0;
            IsPortrait = portrait;
            scaler.referenceResolution = portrait ? PortraitReference : LandscapeReference;
            foreach (var it in items) if (it != null) it.Apply(portrait);
            Canvas.ForceUpdateCanvases();
            Changed?.Invoke(portrait);
        }

        [ContextMenu("Preview Portrait")]
        private void PreviewPortrait()
        {
            Collect();
            Apply(true);
        }

        [ContextMenu("Preview Landscape")]
        private void PreviewLandscape()
        {
            Collect();
            Apply(false);
        }
    }
}
