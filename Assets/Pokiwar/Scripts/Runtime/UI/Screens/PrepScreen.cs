using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Preparation room: your pet on the left, the opponent on the right, five card slots (copies allowed; each copy is used once).</summary>
    public sealed class PrepScreen : MonoBehaviour
    {
        public const int MaxCards = 5;

        public Image EnemyImage;
        public Text EnemyTitle;
        public Image PetImage;
        public Text PetName;
        [Tooltip("How many battles the chosen pet has won.")]
        public Text PetWins;
        public AvatarView Avatar;
        public Button ChoosePetButton;
        public GameObject PetPicker;
        public Button PetPickerClose;
        public RowView PetTemplate;
        public GameObject CardPicker;
        public Button CardPickerClose;
        public RowView CardTemplate;
        public Button[] CardSlots = new Button[MaxCards];
        public Image[] CardSlotIcons = new Image[MaxCards];
        public Button[] CardSlotRemove = new Button[MaxCards];
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
            ChoosePetButton.onClick.AddListener(() => Open(PetPicker, true));
            PetPickerClose.onClick.AddListener(() => Open(PetPicker, false));
            CardPickerClose.onClick.AddListener(() => Open(CardPicker, false));
            for (int i = 0; i < MaxCards; i++)
            {
                int k = i;
                CardSlots[i].onClick.AddListener(() => Open(CardPicker, true));
                CardSlotRemove[i].onClick.AddListener(() =>
                {
                    var ids = app.Save.SelectedCardIds;
                    if (k < ids.Count) ids.RemoveAt(k);
                    Refresh();
                });
            }
        }

        public void Show(GameApp a, MapNodeDef n)
        {
            app = a;
            node = n;
            var enc = a.Db.Encounter(n.EncounterId);
            var c = a.Db.Creature(enc.CreatureId);
            EnemyImage.sprite = a.Sprites.Get(c.SpriteKey);
            EnemyTitle.text = enc.Name + "  (Hunt Lv " + enc.Level + ")";
            PetPicker.SetActive(false);
            CardPicker.SetActive(false);
            if (Avatar != null) Avatar.Show(a.Avatars.Look(a.Save), a.Db, a.Sprites);
            Refresh();
        }

        private void Open(GameObject picker, bool on)
        {
            picker.SetActive(on);
            Refresh();
        }

        private void Refresh()
        {
            var s = app.Save;
            if (s.Pet(s.SelectedPetUid) == null && s.Pets.Count > 0) s.SelectedPetUid = s.Pets[0].Uid;
            s.SelectedCardIds.RemoveAll(id => !s.Cards.Contains(id) && s.SkillCard(id) == null);

            var selected = s.Pet(s.SelectedPetUid);
            if (selected != null)
            {
                var def = app.Db.Creature(selected.PetId);
                PetImage.sprite = app.PetSprite(selected);
                PetName.text = def.Name;
                PetWins.text = selected.Wins.ToString();
            }

            for (int i = 0; i < MaxCards; i++)
            {
                bool has = i < s.SelectedCardIds.Count;
                var card = has ? app.Db.TryCard(s.SelectedCardIds[i]) : null;
                var skill = has && card == null ? app.Db.TrySkill(s.SelectedCardIds[i]) : null;
                string iconKey = card != null ? card.IconKey : skill?.IconKey;
                CardSlotIcons[i].gameObject.SetActive(iconKey != null);
                CardSlotIcons[i].preserveAspect = skill != null;
                if (iconKey != null) CardSlotIcons[i].sprite = app.Sprites.Get(iconKey);
                CardSlotRemove[i].gameObject.SetActive(iconKey != null);
            }

            petRows.Clear();
            if (PetPicker.activeSelf)
            {
                foreach (var pet in s.Pets)
                {
                    var def = app.Db.Creature(pet.PetId);
                    var st = app.Progression.BattleStats(s, pet);
                    var row = petRows.Add();
                    row.Set(def.Name + "  Lv " + pet.Level,
                        def.Element + "  HP " + st.MaxHp + "  ATK " + st.Atk, app.PetSprite(pet), pet.Uid == s.SelectedPetUid);
                    var uid = pet.Uid;
                    row.Button.onClick.AddListener(() =>
                    {
                        s.SelectedPetUid = uid;
                        Open(PetPicker, false);
                    });
                }
            }

            cardRows.Clear();
            if (CardPicker.activeSelf)
            {
                double atk = selected != null ? app.Progression.BattleStats(s, selected).Atk : 0;
                foreach (var oc in s.SkillCards)
                {
                    var sk = app.Db.TrySkill(oc.Id);
                    if (sk == null) continue;
                    var row = cardRows.Add();
                    bool equipped = s.SelectedCardIds.Contains(oc.Id);
                    row.Set(sk.Name + "  Lv " + oc.Level, sk.ManaCost + " MP   DMG " + BattleEngine.SkillPower(atk, sk, oc.Level) + "   reusable\n" + sk.Description, app.Sprites.Get(sk.IconKey), equipped);
                    var sid = oc.Id;
                    row.Button.onClick.AddListener(() =>
                    {
                        if (s.SelectedCardIds.Contains(sid)) return;
                        if (s.SelectedCardIds.FindAll(x => s.SkillCard(x) != null).Count >= ProgressionService.MaxSkillsInLoadout) app.Toast("Max " + ProgressionService.MaxSkillsInLoadout + " reusable cards");
                        else if (s.SelectedCardIds.Count < MaxCards) s.SelectedCardIds.Insert(0, sid);
                        else app.Toast("Max " + MaxCards + " cards");
                        Refresh();
                    });
                    if (row.ExtraA != null)
                    {
                        row.ExtraA.gameObject.SetActive(equipped);
                        row.ExtraA.onClick.AddListener(() =>
                        {
                            s.SelectedCardIds.Remove(sid);
                            Refresh();
                        });
                    }
                }
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
                        row.ExtraA.onClick.AddListener(() =>
                        {
                            s.SelectedCardIds.Remove(cid);
                            Refresh();
                        });
                    }
                }
            }

            var check = app.Progression.CanEnter(s, node);
            FightButton.interactable = check.Ok;
            FightLabel.text = check.Ok ? "READY" : check.Reason;
        }
    }
}
