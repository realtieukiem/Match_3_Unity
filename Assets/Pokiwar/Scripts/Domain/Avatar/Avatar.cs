using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    /// <summary>Paper-doll layers, drawn back to front in this order over the base body.</summary>
    public enum AvatarSlot
    {
        Bottom,
        Top,
        Hair,
        Hat
    }

    [Serializable]
    public class AvatarItemDef
    {
        public string Id;
        public string Name;
        public AvatarSlot Slot;
        public int Price;
        public string SpriteKey;
        public Confidence Confidence = Confidence.Provisional;
    }

    /// <summary>What another screen or a remote player needs to draw an avatar: a name and the worn item ids.</summary>
    [Serializable]
    public class AvatarLook
    {
        public string Name;
        public List<string> ItemIds = new List<string>();
    }

    public sealed class AvatarResult
    {
        public bool Ok;
        public string Message;
    }

    public sealed class AvatarService
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 16;

        private readonly ContentDatabase db;

        public AvatarService(ContentDatabase db)
        {
            this.db = db;
        }

        public void GrantStarter(SaveData d)
        {
            if (string.IsNullOrEmpty(d.PlayerName)) d.PlayerName = db.Progression.DefaultPlayerName;
            foreach (var id in db.Progression.StarterAvatarIds)
            {
                if (db.TryAvatarItem(id) == null) continue;
                if (!d.AvatarOwned.Contains(id)) d.AvatarOwned.Add(id);
                Wear(d, id);
            }
        }

        public bool Owns(SaveData d, string id) => d.AvatarOwned.Contains(id);

        public bool IsWorn(SaveData d, string id) => d.AvatarWorn.Contains(id);

        public string WornIn(SaveData d, AvatarSlot slot) =>
            d.AvatarWorn.Find(id => db.TryAvatarItem(id)?.Slot == slot);

        public AvatarResult Buy(SaveData d, string id)
        {
            var item = db.TryAvatarItem(id);
            if (item == null) return Fail("Unknown item");
            if (Owns(d, id)) return Fail("Already owned");
            if (d.Gold < item.Price) return Fail("Not enough gold");
            d.Gold -= item.Price;
            d.AvatarOwned.Add(id);
            Wear(d, id);
            return new AvatarResult { Ok = true, Message = item.Name + " bought" };
        }

        public AvatarResult Equip(SaveData d, string id)
        {
            var item = db.TryAvatarItem(id);
            if (item == null) return Fail("Unknown item");
            if (!Owns(d, id)) return Fail("Buy it first");
            Wear(d, id);
            return new AvatarResult { Ok = true, Message = item.Name + " on" };
        }

        public AvatarResult TakeOff(SaveData d, string id)
        {
            if (!d.AvatarWorn.Remove(id)) return Fail("Not worn");
            return new AvatarResult { Ok = true, Message = "Taken off" };
        }

        public AvatarResult Rename(SaveData d, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length < MinNameLength || name.Length > MaxNameLength)
                return Fail("Name must be " + MinNameLength + "-" + MaxNameLength + " letters");
            d.PlayerName = name;
            return new AvatarResult { Ok = true, Message = "Name saved" };
        }

        public AvatarLook Look(SaveData d)
        {
            var look = new AvatarLook { Name = d.PlayerName };
            foreach (AvatarSlot slot in Enum.GetValues(typeof(AvatarSlot)))
            {
                var id = WornIn(d, slot);
                if (id != null) look.ItemIds.Add(id);
            }
            return look;
        }

        private void Wear(SaveData d, string id)
        {
            var item = db.TryAvatarItem(id);
            if (item == null || d.AvatarWorn.Contains(id)) return;
            d.AvatarWorn.RemoveAll(w => db.TryAvatarItem(w)?.Slot == item.Slot);
            d.AvatarWorn.Add(id);
        }

        private static AvatarResult Fail(string msg) => new AvatarResult { Ok = false, Message = msg };
    }
}
