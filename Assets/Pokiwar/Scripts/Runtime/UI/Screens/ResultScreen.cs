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
            RetryButton.gameObject.SetActive(true);
            ContinueButton.gameObject.SetActive(true);
        }
    }
}
