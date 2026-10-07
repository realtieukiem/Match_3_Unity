using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Card forge: reusable cards level up with card stones, which are never pet stones.</summary>
    public sealed class CardForgeScreen : MonoBehaviour
    {
        public RowView CardTemplate;
        public RowView StoneTemplate;
        public ActionButtonView CardFace;
        public Text CardDetails;
        public Text GoldLabel;
        public Text LuckyLabel;
        public Text ProtectLabel;
        public Text MessageLabel;
        public Toggle LuckyToggle;
        public Toggle ProtectToggle;
        public Button BackButton;
        [Tooltip("Tint of the card stone icon, so it never reads as a pet stone.")]
        public Color CardStoneColor = new Color(1f, 0.82f, 0.25f);
        [Tooltip("On when the card stone has its own painted art, so it is drawn without the tint.")]
        public bool PaintedStone;

        private GameApp app;
        private string cardId;
        private TemplateList<RowView> cardRows;
        private TemplateList<RowView> stoneRows;

        private void Awake()
        {
            cardRows = new TemplateList<RowView>(CardTemplate);
            stoneRows = new TemplateList<RowView>(StoneTemplate);
            BackButton.onClick.AddListener(() => app.Back());
            LuckyToggle.onValueChanged.AddListener(_ => Refresh());
            ProtectToggle.onValueChanged.AddListener(_ => Refresh());
        }

        public void Show(GameApp a)
        {
            app = a;
            MessageLabel.text = "Feed a card stone to level a card up. Card stones drop from battles.";
            Refresh();
        }

        private double SelectedAtk()
        {
            var pet = app.Save.Pet(app.Save.SelectedPetUid);
            return pet != null ? app.Progression.PetStats(pet).Atk : 0;
        }

        private void Refresh()
        {
            var s = app.Save;
            var card = s.SkillCard(cardId) ?? (s.SkillCards.Count > 0 ? s.SkillCards[0] : null);
            cardId = card?.Id;
            GoldLabel.text = s.Gold.ToString();
            LuckyLabel.text = s.LuckyCharms.ToString();
            ProtectLabel.text = s.ProtectionCharms.ToString();
            if (LuckyToggle.isOn && s.LuckyCharms <= 0) LuckyToggle.SetIsOnWithoutNotify(false);
            if (ProtectToggle.isOn && s.ProtectionCharms <= 0) ProtectToggle.SetIsOnWithoutNotify(false);
            double atk = SelectedAtk();
            int maxLevel = app.Db.Upgrades.MaxCardLevel;

            cardRows.Clear();
            foreach (var oc in s.SkillCards)
            {
                var def = app.Db.TrySkill(oc.Id);
                if (def == null) continue;
                var row = cardRows.Add();
                row.Set(def.Name, "Lv " + oc.Level + "   " + def.ManaCost + " MP   DMG " + BattleEngine.SkillPower(atk, def, oc.Level), CardFaces.Sprite(app.Sprites, def), oc.Id == cardId);
                var id = oc.Id;
                row.Button.onClick.AddListener(() =>
                {
                    cardId = id;
                    Refresh();
                });
            }

            CardFace.gameObject.SetActive(card != null);
            if (card != null)
            {
                var def = app.Db.Skill(card.Id);
                int now = BattleEngine.SkillPower(atk, def, card.Level);
                CardFace.Bind(def.Name, "", "", CardFaces.Sprite(app.Sprites, def), new Color(0.9f, 0.6f, 0.2f));
                CardFace.SetFace(def.ManaCost.ToString(), card.Level.ToString(), now.ToString());
                CardDetails.text = def.Name + "\n" + def.Description +
                                   (card.Level >= maxLevel
                                       ? "\nMax level"
                                       : "\nNext level: DMG " + now + " -> " + BattleEngine.SkillPower(atk, def, card.Level + 1) +
                                         "\nCost " + app.Upgrades.CardUpgradeCost(card) + " gold + 1 card stone");
            }
            else CardDetails.text = "No reusable cards yet.";

            stoneRows.Clear();
            bool lucky = LuckyToggle.isOn;
            for (int tier = 1; tier <= s.CardStones.Count; tier++)
            {
                int count = s.CardStoneCount(tier);
                if (count <= 0) continue;
                var row = stoneRows.Add();
                row.Set("Card Stone  T" + tier + "   x" + count, "", app.Sprites.Get("stone.card"), false);
                row.Icon.color = PaintedStone ? Color.white : CardStoneColor;
                int t = tier;
                row.ExtraALabel.text = "Merge 3\n" + Mathf.RoundToInt(app.Upgrades.MergeChance(t, lucky) * 100) + "%  " + app.Upgrades.MergeCost(t) + "g";
                row.ExtraA.interactable = count >= 3 && t < app.Db.Upgrades.MaxCardStoneTier;
                row.ExtraA.onClick.AddListener(() => Do(app.Upgrades.MergeCardStones(app.Save, t, LuckyToggle.isOn, app.Rng)));
                row.ExtraB.gameObject.SetActive(card != null);
                if (card != null)
                {
                    row.ExtraBLabel.text = "Upgrade\n" + Mathf.RoundToInt(app.Upgrades.CardUpgradeChance(card, t, lucky) * 100) + "%";
                    row.ExtraB.interactable = card.Level < maxLevel;
                    row.ExtraB.onClick.AddListener(() => Do(app.Upgrades.UpgradeCard(app.Save, card, t, LuckyToggle.isOn, ProtectToggle.isOn, app.Rng)));
                }
            }
            if (stoneRows.Items.Count == 0) MessageLabel.text = "No card stones yet. Win battles to earn them.";
        }

        private void Do(UpgradeResult r)
        {
            MessageLabel.text = r.Message;
            if (r.Attempted)
            {
                AudioDirector.Sfx(r.Success ? "upgrade.ok" : "upgrade.fail", 1f, 1f, false);
                if (r.Success && VfxLayer.Instance != null)
                {
                    var at = ((RectTransform)CardFace.transform).position;
                    VfxLayer.Instance.Burst(at, new Color(1f, 0.85f, 0.3f), 30, 700f, 22f, 0.8f, 500f, VfxLayer.Instance.Star);
                    VfxLayer.Instance.Ring(at, new Color(1f, 0.9f, 0.5f), 480f, 0.45f);
                }
                app.Persist();
                app.Toast(r.Success ? "Success!" : "Failed");
            }
            Refresh();
        }
    }
}
