using Pokiwar.App;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>World map between the town and the boss lobby: the open island leads to its lobby.</summary>
    public sealed class WorldScreen : MonoBehaviour
    {
        public Button IslandButton;
        [Tooltip("Islands that are not in the game yet; tapping one says so.")]
        public Button[] LockedIslands;
        public Button BackButton;
        public RectTransform Ship;
        public string LockedMessage = "This island is not open yet";
        [Tooltip("How far the ship drifts up and down, in canvas units.")]
        public float ShipBob = 12f;
        [Tooltip("Seconds for one full up-and-down drift of the ship.")]
        public float ShipBobSeconds = 2.4f;

        private GameApp app;
        private Vector2 shipRest;

        private void Awake()
        {
            IslandButton.onClick.AddListener(() => app.ShowMap());
            BackButton.onClick.AddListener(() => app.ShowHome());
            foreach (var b in LockedIslands) b.onClick.AddListener(() => app.Toast(LockedMessage));
            if (Ship != null) shipRest = Ship.anchoredPosition;
        }

        public void Show(GameApp a)
        {
            app = a;
        }

        private void Update()
        {
            if (Ship == null || ShipBobSeconds <= 0f) return;
            float phase = Time.unscaledTime / ShipBobSeconds * Mathf.PI * 2f;
            Ship.anchoredPosition = shipRest + new Vector2(0f, Mathf.Sin(phase) * ShipBob);
        }
    }
}
