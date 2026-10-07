using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Lobby: region grid on the left, the region's opponents on a ring, resources on top, navigation along the bottom.</summary>
    public sealed class MapScreen : MonoBehaviour
    {
        public Text Header;
        public Text PlayerLabel;
        public Text GoldLabel;
        public Text EnergyLabel;
        public Sprite NodeOpen;
        public Sprite NodeLocked;
        public Sprite NodeBoss;
        public RectTransform NodeArea;
        public RowView NodeTemplate;
        public Image PathTemplate;
        public RowView RegionTemplate;
        public Sprite RegionLocked;
        public Color RegionOpenTint = new Color(0.16f, 0.42f, 0.78f, 0.9f);
        public Color RegionLockedTint = new Color(0.2f, 0.26f, 0.42f, 0.85f);
        public Button BackButton;
        public Button CloseButton;
        public Button AvatarButton;
        public Button CardsButton;
        public Button PetsButton;

        [Tooltip("Opponents sit on a ring, as in the original lobby. Off = use each node's own X/Y.")]
        public bool RingLayout = true;
        [Tooltip("Ring radius as a share of the node area's width and height.")]
        public Vector2 RingRadius = new Vector2(0.34f, 0.28f);
        [Tooltip("How far the ring sits above the centre of the node area, as a share of its height; leaves room for the label under the lowest node.")]
        public float RingLift = 0.07f;
        [Tooltip("How many region tiles the grid shows; regions the game does not have yet show as locked.")]
        public int RegionSlots = 12;

        private GameApp app;
        private TemplateList<RowView> nodes;
        private TemplateList<Image> paths;
        private TemplateList<RowView> regions;

        private void Awake()
        {
            nodes = new TemplateList<RowView>(NodeTemplate);
            paths = new TemplateList<Image>(PathTemplate);
            if (RegionTemplate != null) regions = new TemplateList<RowView>(RegionTemplate);
            BackButton.onClick.AddListener(() => app.ShowHub());
            if (CloseButton != null) CloseButton.onClick.AddListener(() => app.ShowWorld());
            if (AvatarButton != null) AvatarButton.onClick.AddListener(() => app.ShowWardrobe());
            if (CardsButton != null) CardsButton.onClick.AddListener(() => app.ShowCardForge());
            if (PetsButton != null) PetsButton.onClick.AddListener(() => app.ShowUpgrade());
        }

        private void OnEnable() => ResponsiveCanvas.Changed += Relayout;

        private void OnDisable() => ResponsiveCanvas.Changed -= Relayout;

        private void Relayout(bool portrait)
        {
            if (app != null) Show(app);
        }

        private Vector2 NodePosition(MapNodeDef n, int index, int count, Vector2 size)
        {
            if (!RingLayout || count < 2) return new Vector2((n.X - 0.5f) * size.x, (n.Y - 0.5f) * size.y);
            float angle = (90f - index * 360f / count) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle) * RingRadius.x * size.x, (Mathf.Sin(angle) * RingRadius.y + RingLift) * size.y);
        }

        public void Show(GameApp a)
        {
            app = a;
            nodes.Clear();
            paths.Clear();
            var map = a.Db.Map;
            Header.text = map.Regions.Count > 0 ? map.Regions[0].Name : "Adventure";
            if (PlayerLabel != null) PlayerLabel.text = a.Save.PlayerName + "  Lv " + a.Save.PlayerLevel;
            GoldLabel.text = a.Save.Gold.ToString();
            EnergyLabel.text = a.Save.Energy + "/" + a.Db.Progression.MaxEnergy;
            ShowRegions(map);
            Canvas.ForceUpdateCanvases();
            var size = NodeArea.rect.size;
            for (int i = 0; i < map.Nodes.Count; i++)
            {
                var n = map.Nodes[i];
                if (string.IsNullOrEmpty(n.RequiresNodeId)) continue;
                int fromIndex = map.Nodes.FindIndex(x => x.Id == n.RequiresNodeId);
                if (fromIndex < 0) continue;
                var p = paths.Add();
                Vector2 pa = NodePosition(map.Nodes[fromIndex], fromIndex, map.Nodes.Count, size);
                Vector2 pb = NodePosition(n, i, map.Nodes.Count, size);
                var rt = p.rectTransform;
                rt.anchoredPosition = (pa + pb) * 0.5f;
                rt.sizeDelta = new Vector2(Vector2.Distance(pa, pb), 12f);
                rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
                p.color = a.Progression.IsNodeUnlocked(a.Save, n) ? new Color(1f, 0.9f, 0.5f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.7f);
            }
            for (int i = 0; i < map.Nodes.Count; i++)
            {
                var n = map.Nodes[i];
                var row = nodes.Add();
                var enc = a.Db.Encounter(n.EncounterId);
                var creature = a.Db.Creature(enc.CreatureId);
                bool unlocked = a.Progression.IsNodeUnlocked(a.Save, n);
                int wins = a.Save.Wins(n.Id);
                string sub = unlocked
                    ? enc.Name + "  Hunt Lv " + enc.Level + "\nWins " + wins + "/" + Mathf.Max(1, n.WinsRequired) + "   Energy " + enc.EnergyCost
                    : "LOCKED";
                row.Set((i + 1) + ". " + n.Name, sub, a.Sprites.Get(creature.SpriteKey), wins > 0);
                row.Rect.anchoredPosition = NodePosition(n, i, map.Nodes.Count, size);
                var skin = !unlocked ? NodeLocked : enc.IsBoss ? NodeBoss : NodeOpen;
                if (skin != null)
                {
                    row.Background.sprite = skin;
                    row.Background.color = Color.white;
                }
                else row.Background.color = !unlocked ? new Color(0.25f, 0.25f, 0.28f) : enc.IsBoss ? new Color(0.65f, 0.2f, 0.25f) : new Color(0.2f, 0.45f, 0.7f);
                row.Icon.color = unlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f);
                row.Button.interactable = unlocked;
                var node = n;
                row.Button.onClick.AddListener(() => app.ShowPrep(node));
            }
        }

        private void ShowRegions(MapDef map)
        {
            if (regions == null) return;
            regions.Clear();
            for (int i = 0; i < Mathf.Max(RegionSlots, map.Regions.Count); i++)
            {
                var tile = regions.Add();
                bool real = i < map.Regions.Count;
                Sprite emblem = RegionLocked;
                if (real)
                {
                    var last = map.Nodes.FindLast(n => n.RegionId == map.Regions[i].Id);
                    emblem = app.Sprites.Has(map.Regions[i].Id) ? app.Sprites.Get(map.Regions[i].Id) : last == null ? null : app.Sprites.Get(app.Db.Creature(app.Db.Encounter(last.EncounterId).CreatureId).SpriteKey);
                }
                tile.Set(real ? map.Regions[i].Name : "???", "", emblem, real && i == 0);
                tile.Background.color = real ? RegionOpenTint : RegionLockedTint;
                tile.Button.interactable = real;
            }
        }
    }
}
