using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class ResultScreen : MonoBehaviour
    {
        public Text Title;
        public Text Lines;
        public Button ContinueButton;
        public Button RetryButton;
        public Button HubButton;

        private GameApp app;
        private string nodeId;

        private void Awake()
        {
            ContinueButton.onClick.AddListener(() => app.ShowMap());
            HubButton.onClick.AddListener(() => app.ShowHub());
            RetryButton.onClick.AddListener(() =>
            {
                var node = app.Db.Node(nodeId);
                app.ShowPrep(node);
            });
        }

        public void Show(GameApp a, BattleReport report, RewardGrant grant)
        {
            app = a;
            nodeId = report.NodeId;
            Title.text = report.Won ? "VICTORY" : "DEFEAT";
            Title.color = report.Won ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.4f);
            Lines.text = grant != null && grant.BattleId == report.BattleId
                ? string.Join("\n", grant.Lines) + "\n\nTurns: " + report.Turns
                : "Turns: " + report.Turns;
            if (report.Won && VfxLayer.Instance != null)
            {
                var v = VfxLayer.Instance;
                v.Burst(Title.rectTransform.position + new Vector3(-300, 0, 0), new Color(1f, 0.8f, 0.3f), 40, 1100f, 22f, 1.4f, 1000f, v.Star, 80f, 70f);
                v.Burst(Title.rectTransform.position + new Vector3(300, 0, 0), new Color(0.5f, 0.9f, 1f), 40, 1100f, 22f, 1.4f, 1000f, v.Star, 80f, 110f);
            }
            StartCoroutine(CombatantHud.Pop(Title.rectTransform, 1.35f));
            RetryButton.gameObject.SetActive(true);
            ContinueButton.gameObject.SetActive(true);
        }
    }
}
