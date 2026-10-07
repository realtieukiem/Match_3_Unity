using Pokiwar.App;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Town map, the first screen: every building is a way into one part of the game.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        public Text PlayerLabel;
        public Text GoldLabel;
        public Text EnergyLabel;
        public Text RankLabel;
        public Button HuntButton;
        public Button EvolveButton;
        public Button AvatarShopButton;
        public Button CardShopButton;
        public Button InfoButton;
        public Button AvatarButton;
        public Button CardsButton;
        public Button PetsButton;
        [Tooltip("Buildings whose feature is not in the game yet; tapping one says so.")]
        public Button[] SoonButtons;
        public string SoonMessage = "Coming soon";

        private GameApp app;

        private void Awake()
        {
            HuntButton.onClick.AddListener(() => app.ShowWorld());
            EvolveButton.onClick.AddListener(() => app.ShowUpgrade());
            AvatarShopButton.onClick.AddListener(() => app.ShowWardrobe());
            CardShopButton.onClick.AddListener(() => app.ShowShop());
            InfoButton.onClick.AddListener(() => app.ShowHub());
            AvatarButton.onClick.AddListener(() => app.ShowWardrobe());
            CardsButton.onClick.AddListener(() => app.ShowCardForge());
            PetsButton.onClick.AddListener(() => app.ShowUpgrade());
            foreach (var b in SoonButtons) b.onClick.AddListener(() => app.Toast(SoonMessage));
        }

        public void Show(GameApp a)
        {
            app = a;
            var s = a.Save;
            PlayerLabel.text = s.PlayerName + "  Lv " + s.PlayerLevel + "   EXP " + s.PlayerExp + "/" + a.Progression.PlayerExpToNext(s.PlayerLevel);
            GoldLabel.text = s.Gold.ToString();
            EnergyLabel.text = s.Energy + "/" + a.Db.Progression.MaxEnergy;
            RankLabel.text = s.RankPoints.ToString();
        }
    }
}
