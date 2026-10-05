using UnityEngine;

namespace Pokiwar.UI
{
    /// <summary>One element's placement for landscape and for portrait. ResponsiveCanvas switches between them.</summary>
    [DisallowMultipleComponent]
    public sealed class OrientationLayout : MonoBehaviour
    {
        public RectState Landscape;
        public RectState Portrait;
        [Tooltip("Tat = an phan tu nay khi man hinh ngang")]
        public bool VisibleInLandscape = true;
        [Tooltip("Tat = an phan tu nay khi man hinh doc")]
        public bool VisibleInPortrait = true;

        public void Apply(bool portrait)
        {
            (portrait ? Portrait : Landscape).ApplyTo((RectTransform)transform);
            if (!VisibleInLandscape || !VisibleInPortrait) gameObject.SetActive(portrait ? VisibleInPortrait : VisibleInLandscape);
        }

        [ContextMenu("Capture current as Landscape")]
        private void CaptureLandscape() => Landscape = RectState.Capture((RectTransform)transform);

        [ContextMenu("Capture current as Portrait")]
        private void CapturePortrait() => Portrait = RectState.Capture((RectTransform)transform);
    }
}
