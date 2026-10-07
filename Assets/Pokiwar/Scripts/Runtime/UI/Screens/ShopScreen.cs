using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Card shop: spend gold on the items listed in the progression config.</summary>
    public sealed class ShopScreen : MonoBehaviour
    {
        public Text WalletLabel;
        public RowView ItemTemplate;
        public Button BackButton;

        private GameApp app;
        private TemplateList<RowView> rows;

        private void Awake()
        {
            rows = new TemplateList<RowView>(ItemTemplate);
            BackButton.onClick.AddListener(() => app.Back());
        }

        public void Show(GameApp a)
        {
            app = a;
            Refresh();
        }

        public void Press(string itemId)
        {
            var r = app.Shop.Buy(app.Save, itemId);
            if (r.Ok)
            {
                app.Persist();
                AudioDirector.Sfx("upgrade.ok", 1f, 1f, false);
            }
            app.Toast(r.Message);
            Refresh();
        }

        private Sprite Icon(string key)
        {
            if (key != null && key.StartsWith("gem.") && System.Enum.TryParse(key.Substring(4), out GemType gem)) return app.Sprites.Gem(gem);
            return app.Sprites.Get(key);
        }

        private void Refresh()
        {
            var s = app.Save;
            WalletLabel.text = s.Gold.ToString();
            rows.Clear();
            foreach (var item in app.Shop.Items)
            {
                var row = rows.Add();
                row.Set(item.Name + "   x" + app.Shop.Owned(s, item), item.Price + " Gold\n" + item.Description, Icon(item.IconKey), false);
                row.ExtraALabel.text = "BUY";
                row.ExtraA.interactable = app.Shop.CanBuy(s, item);
                var id = item.Id;
                row.ExtraA.onClick.AddListener(() => Press(id));
            }
        }
    }
}
