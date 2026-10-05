using Pokiwar.App;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    public sealed class HubScreen : MonoBehaviour
    {
        public Text PlayerLabel;
        public Text ResourcesLabel;
        public Text PetLabel;
        public Image PetImage;
        public Button AdventureButton;
        public Button UpgradeButton;
        public Button ResetButton;

        private GameApp app;

        private void Awake()
        {
            AdventureButton.onClick.AddListener(() => app.ShowMap());
            UpgradeButton.onClick.AddListener(() => app.ShowUpgrade());
            ResetButton.onClick.AddListener(() => app.ResetSave());
        }

        public void Show(GameApp a)
        {
            app = a;
            var s = a.Save;
            PlayerLabel.text = "Trainer Lv " + s.PlayerLevel + "   EXP " + s.PlayerExp + "/" + a.Progression.PlayerExpToNext(s.PlayerLevel);
            ResourcesLabel.text = "Gold " + s.Gold + "    Energy " + s.Energy + "/" + a.Db.Progression.MaxEnergy +
                                  "    Lucky " + s.LuckyCharms + "    Protect " + s.ProtectionCharms;
            var pet = s.Pet(s.SelectedPetUid);
            if (pet != null)
            {
                var def = a.Db.Creature(pet.PetId);
                var st = a.Progression.PetStats(pet);
                PetLabel.text = def.Name + "  Lv " + pet.Level + (pet.EnhanceLevel > 0 ? "  +" + pet.EnhanceLevel : "") +
                                "\n" + def.Element + "   HP " + st.MaxHp + "   ATK " + st.Atk;
                PetImage.sprite = a.Sprites.Get(def.SpriteKey);
            }
        }
    }
}
