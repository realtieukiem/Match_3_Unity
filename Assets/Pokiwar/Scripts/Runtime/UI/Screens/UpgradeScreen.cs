using System.Collections.Generic;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Pets and stones: StoneMerge (3 -> 1 next tier) and PetEnhancement / sockets are separate actions.</summary>
    public sealed class UpgradeScreen : MonoBehaviour
    {
        public RowView PetTemplate;
        public RowView StoneTemplate;
        public Image PetImage;
        public Text PetDetails;
        public Text WalletLabel;
        public Text MessageLabel;
        public Toggle LuckyToggle;
        public Toggle ProtectToggle;
        public Button BackButton;

        private GameApp app;
        private string petUid;
        private TemplateList<RowView> petRows;
        private TemplateList<RowView> stoneRows;

        private void Awake()
        {
            petRows = new TemplateList<RowView>(PetTemplate);
            stoneRows = new TemplateList<RowView>(StoneTemplate);
            BackButton.onClick.AddListener(() => app.ShowHub());
            LuckyToggle.onValueChanged.AddListener(_ => Refresh());
            ProtectToggle.onValueChanged.AddListener(_ => Refresh());
        }

        public void Show(GameApp a)
        {
            app = a;
            petUid = a.Save.SelectedPetUid;
            MessageLabel.text = "Merge: 3 stones of one tier -> 1 stone of the next tier. Failure loses all 3.";
            Refresh();
        }

        private void Refresh()
        {
            var s = app.Save;
            var pet = s.Pet(petUid) ?? (s.Pets.Count > 0 ? s.Pets[0] : null);
            petUid = pet?.Uid;
            WalletLabel.text = "Gold " + s.Gold + "   Lucky " + s.LuckyCharms + "   Protect " + s.ProtectionCharms;
            if (LuckyToggle.isOn && s.LuckyCharms <= 0) LuckyToggle.SetIsOnWithoutNotify(false);
            if (ProtectToggle.isOn && s.ProtectionCharms <= 0) ProtectToggle.SetIsOnWithoutNotify(false);

            petRows.Clear();
            foreach (var p in s.Pets)
            {
                var def = app.Db.Creature(p.PetId);
                var row = petRows.Add();
                row.Set(def.Name + " Lv " + p.Level, "Enhance +" + p.EnhanceLevel, app.Sprites.Get(def.SpriteKey), p.Uid == petUid);
                var uid = p.Uid;
                row.Button.onClick.AddListener(() =>
                {
                    petUid = uid;
                    app.Save.SelectedPetUid = uid;
                    Refresh();
                });
            }

            if (pet != null)
            {
                var def = app.Db.Creature(pet.PetId);
                var st = app.Progression.PetStats(pet);
                PetImage.sprite = app.Sprites.Get(def.SpriteKey);
                var sockets = new List<string>();
                for (int i = 0; i < pet.SocketTiers.Count; i++)
                    sockets.Add(pet.SocketTiers[i] > 0 ? pet.SocketElements[i] + " T" + pet.SocketTiers[i] : "empty");
                PetDetails.text = def.Name + "  Lv " + pet.Level + "  EXP " + pet.Exp + "/" + app.Progression.PetExpToNext(pet.Level) +
                                  "\nElement " + app.ElementLabel(def.Element, def.ElementBonus + app.Progression.PetElementBonus(pet)) +
                                  "\nHP " + st.MaxHp + "  ATK " + st.Atk + "  DEF " + st.Def + "  MP " + st.MaxMana + "  RAGE " + st.MaxRage +
                                  "\nEnhance +" + pet.EnhanceLevel + "  (cost " + app.Upgrades.EnhanceCost(pet) + " gold, bonus chance +" + Mathf.RoundToInt(pet.EnhanceBonusChance * 100) + "%)" +
                                  "\nSockets: " + string.Join(", ", sockets);
            }

            stoneRows.Clear();
            bool lucky = LuckyToggle.isOn;
            foreach (var st in new List<StoneStack>(s.Stones))
            {
                var row = stoneRows.Add();
                float mc = app.Upgrades.MergeChance(st.Tier, lucky);
                row.Set(st.Element + " Stone  T" + st.Tier + "   x" + st.Count, "", app.Sprites.Get("stone." + st.Element), false);
                row.Icon.color = SpriteLibrary.ElementColor(st.Element);
                var element = st.Element;
                int tier = st.Tier;
                row.ExtraALabel.text = "Merge 3\n" + Mathf.RoundToInt(mc * 100) + "%  " + app.Upgrades.MergeCost(tier) + "g";
                row.ExtraA.interactable = st.Count >= 3;
                row.ExtraA.onClick.AddListener(() => Do(app.Upgrades.MergeStones(app.Save, element, tier, LuckyToggle.isOn, app.Rng)));
                if (pet != null)
                {
                    float ec = app.Upgrades.EnhanceChance(pet, tier, lucky);
                    row.ExtraBLabel.text = "Enhance\n" + Mathf.RoundToInt(ec * 100) + "%";
                    row.ExtraB.interactable = st.Count >= 1;
                    row.ExtraB.onClick.AddListener(() => Do(app.Upgrades.Enhance(app.Save, pet, element, tier, LuckyToggle.isOn, ProtectToggle.isOn, app.Rng)));
                    row.ExtraCLabel.text = "Socket";
                    row.ExtraC.interactable = st.Count >= 1;
                    row.ExtraC.onClick.AddListener(() => Do(app.Upgrades.Socket(app.Save, pet, element, tier)));
                }
            }
            if (s.Stones.Count == 0) MessageLabel.text = "No stones yet. Win battles to earn stones.";
        }

        private void Do(UpgradeResult r)
        {
            MessageLabel.text = r.Message;
            if (r.Attempted)
            {
                AudioDirector.Sfx(r.Success ? "upgrade.ok" : "upgrade.fail", 1f, 1f, false);
                if (r.Success && VfxLayer.Instance != null)
                {
                    VfxLayer.Instance.Burst(PetImage.rectTransform.position, new Color(1f, 0.85f, 0.3f), 30, 700f, 22f, 0.8f, 500f, VfxLayer.Instance.Star);
                    VfxLayer.Instance.Ring(PetImage.rectTransform.position, new Color(1f, 0.9f, 0.5f), 480f, 0.45f);
                }
                app.Persist();
                app.Toast(r.Success ? "Success!" : "Failed");
            }
            Refresh();
        }
    }
}
