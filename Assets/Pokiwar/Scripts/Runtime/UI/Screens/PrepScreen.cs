using System.Collections.Generic;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Preparation: preview the opponent, pick 1 pet and up to 5 cards (copies allowed; each copy is used once).</summary>
    public sealed class PrepScreen : MonoBehaviour
    {
        public const int MaxCards = 5;

        public Image EnemyImage;
        public Text EnemyTitle;
        public Text EnemyDetails;
        public Text RewardLabel;
        public RowView PetTemplate;
        public RowView CardTemplate;
        public Text PetDetails;
        public Text CardCountLabel;
        public Button FightButton;
        public Text FightLabel;
        public Button BackButton;

        private GameApp app;
        private MapNodeDef node;
        private TemplateList<RowView> petRows;
        private TemplateList<RowView> cardRows;

        private void Awake()
        {
            petRows = new TemplateList<RowView>(PetTemplate);
            cardRows = new TemplateList<RowView>(CardTemplate);
            BackButton.onClick.AddListener(() => app.ShowMap());
            FightButton.onClick.AddListener(() =>
            {
                app.Persist();
                app.StartBattle(node);
            });
        }

        public void Show(GameApp a, MapNodeDef n)
        {
            app = a;
            node = n;
            var enc = a.Db.Encounter(n.EncounterId);
            var c = a.Db.Creature(enc.CreatureId);
            var st = c.StatsAt(enc.Level);
            EnemyImage.sprite = a.Sprites.Get(c.SpriteKey);
            EnemyTitle.text = enc.Name + "  Lv " + enc.Level;
            var lines = new List<string>
            {
                "Element: " + a.ElementLabel(c.Element, c.ElementBonus),
                "HP " + Mathf.RoundToInt(st.MaxHp * enc.StartHpPct) + "/" + st.MaxHp + "   ATK " + st.Atk,
                "Mana " + st.MaxMana + "   Rage " + st.MaxRage
            };
            var kit = new List<string>();
            foreach (var id in c.SkillIds) kit.Add(a.Db.Skill(id).Name);
            foreach (var id in c.CardIds) kit.Add(a.Db.Card(id).Name);
            if (kit.Count > 0) lines.Add("Kit: " + string.Join(", ", kit));
            if (c.Phases.Count > 0) lines.Add("Boss: transforms " + c.Phases.Count + " time(s)!");
            EnemyDetails.text = string.Join("\n", lines);
            var table = a.Db.Reward(enc.RewardTableId);
            RewardLabel.text = "Reward: " + table.Gold + " Gold, " + table.PlayerExp + " EXP, Pet EXP " + table.PetExp + (table.Drops.Count > 0 ? " + items" : "");
            Refresh();
        }

        private void Refresh()
        {
            var s = app.Save;
            if (s.Pet(s.SelectedPetUid) == null && s.Pets.Count > 0) s.SelectedPetUid = s.Pets[0].Uid;
            s.SelectedCardIds.RemoveAll(id => !s.Cards.Contains(id));

            petRows.Clear();
            foreach (var pet in s.Pets)
            {
                var def = app.Db.Creature(pet.PetId);
                var st = app.Progression.PetStats(pet);
                var row = petRows.Add();
                bool sel = pet.Uid == s.SelectedPetUid;
                row.Set(def.Name + "  Lv " + pet.Level + (pet.EnhanceLevel > 0 ? " +" + pet.EnhanceLevel : ""),
                    def.Element + "  HP " + st.MaxHp + "  ATK " + st.Atk, app.Sprites.Get(def.SpriteKey), sel);
                var uid = pet.Uid;
                row.Button.onClick.AddListener(() =>
                {
                    s.SelectedPetUid = uid;
                    Refresh();
                });
            }
            var selected = s.Pet(s.SelectedPetUid);
            if (selected != null)
            {
                var def = app.Db.Creature(selected.PetId);
                var skills = new List<string>();
                foreach (var id in def.SkillIds)
                {
                    var sk = app.Db.Skill(id);
                    skills.Add(sk.Name + " (" + sk.ManaCost + " MP" + (sk.RageCost > 0 ? " " + sk.RageCost + " RG" : "") + ")");
                }
                PetDetails.text = "Skill: " + string.Join(", ", skills);
            }

            cardRows.Clear();
            foreach (var id in s.Cards)
            {
                var card = app.Db.TryCard(id);
                if (card == null) continue;
                var row = cardRows.Add();
                int copies = s.SelectedCardIds.FindAll(x => x == id).Count;
                string cost = (card.ManaCost > 0 ? card.ManaCost + " MP " : "") + (card.RageCost > 0 ? card.RageCost + " RG " : "") + (card.ManaCost == 0 && card.RageCost == 0 ? "Free " : "") + (card.EndTurnAfterUse ? " ends turn" : "");
                row.Set(card.Name + (copies > 0 ? "   x" + copies : ""), cost + "\n" + card.Description, app.Sprites.Get(card.IconKey), copies > 0);
                var cid = id;
                row.Button.onClick.AddListener(() =>
                {
                    if (s.SelectedCardIds.Count < MaxCards) s.SelectedCardIds.Add(cid);
                    else app.Toast("Max " + MaxCards + " cards");
                    Refresh();
                });
                if (row.ExtraA != null)
                {
                    row.ExtraA.gameObject.SetActive(copies > 0);
                    if (row.ExtraALabel != null) row.ExtraALabel.text = "REMOVE";
                    row.ExtraA.onClick.AddListener(() =>
                    {
                        s.SelectedCardIds.Remove(cid);
                        Refresh();
                    });
                }
            }
            CardCountLabel.text = "Cards " + s.SelectedCardIds.Count + "/" + MaxCards;
            var enc = app.Db.Encounter(node.EncounterId);
            var check = app.Progression.CanEnter(s, node);
            FightButton.interactable = check.Ok;
            FightLabel.text = check.Ok ? "FIGHT  (-" + enc.EnergyCost + " Energy)" : check.Reason;
        }
    }
}
