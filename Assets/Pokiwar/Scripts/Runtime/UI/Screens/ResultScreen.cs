using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Reward popup shown over the finished battle; closing it returns to the preparation room.</summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        public Text Title;
        public Text Lines;
        public RectTransform Panel;
        public Button CloseButton;

        private GameApp app;
        private string nodeId;

        private void Awake()
        {
            CloseButton.onClick.AddListener(() => app.ShowPrep(app.Db.Node(nodeId)));
        }

        public void Show(GameApp a, BattleReport report, RewardGrant grant)
        {
            app = a;
            nodeId = report.NodeId;
            Title.text = report.Won ? "VICTORY" : "DEFEAT";
            Lines.text = grant != null && grant.BattleId == report.BattleId && grant.Lines.Count > 0
                ? string.Join("\n", grant.Lines)
                : report.Won ? "" : "Try again!";
            if (report.Won && VfxLayer.Instance != null)
            {
                var v = VfxLayer.Instance;
                v.Burst(Title.rectTransform.position + new Vector3(-300, 0, 0), new Color(1f, 0.8f, 0.3f), 40, 1100f, 22f, 1.4f, 1000f, v.Star, 80f, 70f);
                v.Burst(Title.rectTransform.position + new Vector3(300, 0, 0), new Color(0.5f, 0.9f, 1f), 40, 1100f, 22f, 1.4f, 1000f, v.Star, 80f, 110f);
            }
            if (Panel != null) StartCoroutine(CombatantHud.Pop(Panel, 1.12f));
        }
    }
}
