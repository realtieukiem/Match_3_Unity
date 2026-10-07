using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public sealed class ShopResult
    {
        public bool Ok;
        public string Message;
    }

    /// <summary>Gold shop: every item is a price plus one grant, listed in the progression config.</summary>
    public sealed class ShopService
    {
        private readonly ProgressionService progression;

        public ShopService(ProgressionService progression)
        {
            this.progression = progression;
        }

        public List<ShopItemDef> Items => progression.Db.Progression.Shop;

        public ShopItemDef Item(string id) => Items.Find(i => i.Id == id);

        public bool CanBuy(SaveData d, ShopItemDef item) => item != null && d.Gold >= item.Price;

        public int Owned(SaveData d, ShopItemDef item)
        {
            switch (item.Grant.Kind)
            {
                case RewardKind.CardStone: return d.CardStoneCount(item.Grant.Tier);
                case RewardKind.LuckyCharm: return d.LuckyCharms;
                case RewardKind.ProtectionCharm: return d.ProtectionCharms;
                case RewardKind.Stone: return d.StoneCount(item.Grant.Element, item.Grant.Tier);
                default: return 0;
            }
        }

        public ShopResult Buy(SaveData d, string itemId)
        {
            var item = Item(itemId);
            if (item == null) return new ShopResult { Message = "Not for sale" };
            if (d.Gold < item.Price) return new ShopResult { Message = "Need " + item.Price + " gold" };
            d.Gold -= item.Price;
            progression.GrantItem(d, item.Grant, null);
            return new ShopResult { Ok = true, Message = "Bought " + item.Name };
        }
    }
}
