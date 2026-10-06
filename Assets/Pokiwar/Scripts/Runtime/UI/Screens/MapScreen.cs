using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class MapScreen : MonoBehaviour
    {
        public Text Header;
        public Text GoldLabel;
        public Text EnergyLabel;
        public Sprite NodeOpen;
        public Sprite NodeLocked;
        public Sprite NodeBoss;
        public RectTransform NodeArea;
        public RowView NodeTemplate;
        public Image PathTemplate;
        public Button BackButton;

        private GameApp app;
        private TemplateList<RowView> nodes;
        private TemplateList<Image> paths;

        private void Awake()
        {
            nodes = new TemplateList<RowView>(NodeTemplate);
            paths = new TemplateList<Image>(PathTemplate);
            BackButton.onClick.AddListener(() => app.ShowHub());
        }

        private void OnEnable() => ResponsiveCanvas.Changed += Relayout;

        private void OnDisable() => ResponsiveCanvas.Changed -= Relayout;

        private void Relayout(bool portrait)
        {
            if (app != null) Show(app);
        }

        public void Show(GameApp a)
        {
            app = a;
            nodes.Clear();
            paths.Clear();
            var map = a.Db.Map;
            Header.text = map.Regions.Count > 0 ? map.Regions[0].Name : "Adventure";
            GoldLabel.text = a.Save.Gold.ToString();
            EnergyLabel.text = a.Save.Energy + "/" + a.Db.Progression.MaxEnergy;
            var size = NodeArea.rect.size;
            foreach (var n in map.Nodes)
            {
                if (string.IsNullOrEmpty(n.RequiresNodeId)) continue;
                var from = map.Nodes.Find(x => x.Id == n.RequiresNodeId);
                if (from == null) continue;
                var p = paths.Add();
                Vector2 pa = new Vector2((from.X - 0.5f) * size.x, (from.Y - 0.5f) * size.y);
                Vector2 pb = new Vector2((n.X - 0.5f) * size.x, (n.Y - 0.5f) * size.y);
                var rt = p.rectTransform;
                rt.anchoredPosition = (pa + pb) * 0.5f;
                rt.sizeDelta = new Vector2(Vector2.Distance(pa, pb), 12f);
                rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
                p.color = a.Progression.IsNodeUnlocked(a.Save, n) ? new Color(1f, 0.9f, 0.5f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.7f);
            }
            foreach (var n in map.Nodes)
            {
                var row = nodes.Add();
                var enc = a.Db.Encounter(n.EncounterId);
                var creature = a.Db.Creature(enc.CreatureId);
                bool unlocked = a.Progression.IsNodeUnlocked(a.Save, n);
                int wins = a.Save.Wins(n.Id);
                string sub = unlocked
                    ? enc.Name + " Lv " + enc.Level + "\nEnergy " + enc.EnergyCost + (wins > 0 ? "   Wins " + wins : "")
                    : "LOCKED";
                row.Set(n.Name, sub, a.Sprites.Get(creature.SpriteKey), wins > 0);
                row.Rect.anchoredPosition = new Vector2((n.X - 0.5f) * size.x, (n.Y - 0.5f) * size.y);
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
    }
}
