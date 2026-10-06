using System.Collections.Generic;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Wardrobe: try on, buy and wear paper-doll pieces, and set the player name.</summary>
    public sealed class AvatarScreen : MonoBehaviour
    {
        public AvatarView Preview;
        public InputField NameField;
        public Button SaveNameButton;
        public Text WalletLabel;
        public Button[] Tabs = new Button[4];
        public Image[] TabBackgrounds = new Image[4];
        public RowView ItemTemplate;
        public Button BackButton;

        private GameApp app;
        private AvatarSlot slot = AvatarSlot.Hair;
        private string tryOn;
        private TemplateList<RowView> rows;

        public AvatarSlot CurrentSlot => slot;

        private void Awake()
        {
            rows = new TemplateList<RowView>(ItemTemplate);
            BackButton.onClick.AddListener(() => app.ShowMap());
            SaveNameButton.onClick.AddListener(() => Do(app.Avatars.Rename(app.Save, NameField.text)));
            for (int i = 0; i < Tabs.Length; i++)
            {
                var s = (AvatarSlot)i;
                Tabs[i].onClick.AddListener(() => SelectSlot(s));
            }
        }

        public void Show(GameApp a)
        {
            app = a;
            tryOn = null;
            NameField.text = a.Save.PlayerName;
            Refresh();
        }

        public void SelectSlot(AvatarSlot s)
        {
            slot = s;
            tryOn = null;
            Refresh();
        }

        public void Press(string itemId)
        {
            var av = app.Avatars;
            var s = app.Save;
            if (!av.Owns(s, itemId)) Do(av.Buy(s, itemId));
            else if (av.IsWorn(s, itemId)) Do(av.TakeOff(s, itemId));
            else Do(av.Equip(s, itemId));
        }

        private void Refresh()
        {
            var s = app.Save;
            var av = app.Avatars;
            WalletLabel.text = s.Gold.ToString();
            for (int i = 0; i < TabBackgrounds.Length; i++)
                if (TabBackgrounds[i] != null) TabBackgrounds[i].color = i == (int)slot ? new Color(1f, 0.83f, 0.32f) : new Color(0.35f, 0.37f, 0.42f);

            var look = av.Look(s);
            if (tryOn != null)
            {
                var item = app.Db.TryAvatarItem(tryOn);
                look.ItemIds.RemoveAll(id => app.Db.TryAvatarItem(id)?.Slot == item.Slot);
                look.ItemIds.Add(tryOn);
            }
            Preview.Show(look, app.Db, app.Sprites);

            rows.Clear();
            foreach (var item in new List<AvatarItemDef>(app.Db.AvatarItems))
            {
                if (item.Slot != slot) continue;
                var row = rows.Add();
                bool owned = av.Owns(s, item.Id);
                bool worn = av.IsWorn(s, item.Id);
                row.Set(item.Name, owned ? item.BonusText : (item.Price + " Gold  " + item.BonusText).Trim(), AvatarView.Icon(item, app.Sprites), worn || item.Id == tryOn);
                row.ExtraALabel.text = !owned ? "BUY" : worn ? "TAKE OFF" : "WEAR";
                row.ExtraA.interactable = owned || s.Gold >= item.Price;
                var id = item.Id;
                row.Button.onClick.AddListener(() => { tryOn = id; Refresh(); });
                row.ExtraA.onClick.AddListener(() => Press(id));
            }
        }

        private void Do(AvatarResult r)
        {
            if (r.Ok)
            {
                tryOn = null;
                app.Persist();
                AudioDirector.Sfx("upgrade.ok", 1f, 1f, false);
            }
            app.Toast(r.Message);
            Refresh();
        }
    }
}
