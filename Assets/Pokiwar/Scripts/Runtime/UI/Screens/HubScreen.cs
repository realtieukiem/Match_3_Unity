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
        public AvatarView Avatar;
        public Button AvatarButton;
        public Button AdventureButton;
        public Button UpgradeButton;
        public Button ResetButton;
        public Button MusicButton;
        public Text MusicLabel;
        public Button SfxButton;
        public Text SfxLabel;
        public Button ShakeButton;
        public Text ShakeLabel;

        private GameApp app;

        private void Awake()
        {
            AdventureButton.onClick.AddListener(() => app.ShowMap());
            UpgradeButton.onClick.AddListener(() => app.ShowUpgrade());
            if (AvatarButton != null) AvatarButton.onClick.AddListener(() => app.ShowWardrobe());
            ResetButton.onClick.AddListener(() => app.ResetSave());
            MusicButton.onClick.AddListener(() => { app.Save.MusicOn = !app.Save.MusicOn; app.ApplySettings(); Show(app); });
            SfxButton.onClick.AddListener(() => { app.Save.SfxOn = !app.Save.SfxOn; app.ApplySettings(); Show(app); });
            ShakeButton.onClick.AddListener(() => { app.Save.ShakeOn = !app.Save.ShakeOn; app.ApplySettings(); Show(app); });
        }

        public void Show(GameApp a)
        {
            app = a;
            var s = a.Save;
            if (Avatar != null) Avatar.Show(a.Avatars.Look(s), a.Db, a.Sprites);
            PlayerLabel.text = s.PlayerName + "  Lv " + s.PlayerLevel + "   EXP " + s.PlayerExp + "/" + a.Progression.PlayerExpToNext(s.PlayerLevel);
            ResourcesLabel.text = "Gold " + s.Gold + "    Energy " + s.Energy + "/" + a.Db.Progression.MaxEnergy +
                                  "    Lucky " + s.LuckyCharms + "    Protect " + s.ProtectionCharms;
            MusicLabel.text = "MUSIC: " + (s.MusicOn ? "ON" : "OFF");
            SfxLabel.text = "SOUND: " + (s.SfxOn ? "ON" : "OFF");
            ShakeLabel.text = "SHAKE: " + (s.ShakeOn ? "ON" : "OFF");
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
