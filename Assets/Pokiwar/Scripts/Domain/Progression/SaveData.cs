using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    [Serializable]
    public class OwnedPet
    {
        public string Uid;
        public string PetId;
        public int Level = 1;
        public int Exp;
        public int EnhanceLevel;
        public float EnhanceBonusChance;
        public List<int> SocketTiers = new List<int>();
        public List<Element> SocketElements = new List<Element>();
        public int Wins;
    }

    [Serializable]
    public class OwnedCard
    {
        public string Id;
        public int Level = 1;
    }

    [Serializable]
    public class StoneStack
    {
        public Element Element;
        public int Tier;
        public int Count;
    }

    [Serializable]
    public class NodeProgress
    {
        public string NodeId;
        public int Wins;
        public int Losses;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 5;

        public int Version = CurrentVersion;
        public int Gold;
        public int PlayerLevel = 1;
        public int PlayerExp;

        /// <summary>Copies of a single-use card in stock; each entry of Cards is one copy.</summary>
        public int CardCount(string id) => Cards.FindAll(c => c == id).Count;
        public int Energy;
        public long EnergyStampUtcTicks;
        public int LuckyCharms;
        public int ProtectionCharms;
        public int NextPetUid = 1;
        public uint RngState = 0xC0FFEEu;
        public List<OwnedPet> Pets = new List<OwnedPet>();
        public List<string> Cards = new List<string>();
        public List<StoneStack> Stones = new List<StoneStack>();
        public List<NodeProgress> Nodes = new List<NodeProgress>();
        public string SelectedPetUid;
        public List<string> SelectedCardIds = new List<string>();
        public List<string> CommittedBattleIds = new List<string>();
        public int BattlesStarted;
        public bool MusicOn = true;
        public bool SfxOn = true;
        public bool ShakeOn = true;
        public string PlayerName;
        public List<string> AvatarOwned = new List<string>();
        public List<string> AvatarWorn = new List<string>();
        public List<OwnedCard> SkillCards = new List<OwnedCard>();
        public List<int> CardStones = new List<int>();
        public int RankPoints;

        public OwnedPet Pet(string uid) => Pets.Find(p => p.Uid == uid);

        public OwnedCard SkillCard(string id) => SkillCards.Find(c => c.Id == id);

        public int CardStoneCount(int tier) => tier >= 1 && tier <= CardStones.Count ? CardStones[tier - 1] : 0;

        public void AddCardStones(int tier, int count)
        {
            while (CardStones.Count < tier) CardStones.Add(0);
            CardStones[tier - 1] = Math.Max(0, CardStones[tier - 1] + count);
        }

        public NodeProgress Node(string id, bool create)
        {
            var n = Nodes.Find(x => x.NodeId == id);
            if (n == null && create)
            {
                n = new NodeProgress { NodeId = id };
                Nodes.Add(n);
            }
            return n;
        }

        public int Wins(string nodeId) => Node(nodeId, false)?.Wins ?? 0;

        public StoneStack Stack(Element e, int tier, bool create)
        {
            var s = Stones.Find(x => x.Element == e && x.Tier == tier);
            if (s == null && create)
            {
                s = new StoneStack { Element = e, Tier = tier };
                Stones.Add(s);
                Stones.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Element.CompareTo(b.Element));
            }
            return s;
        }

        public int StoneCount(Element e, int tier) => Stack(e, tier, false)?.Count ?? 0;

        public void AddStones(Element e, int tier, int count)
        {
            var s = Stack(e, tier, true);
            s.Count += count;
            if (s.Count <= 0) Stones.Remove(s);
        }
    }

    /// <summary>Upgrades older saves in place. Each step only knows the version right before it.</summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData d, ProgressionConfig cfg)
        {
            if (d == null) return null;
            if (d.Version <= 0) d.Version = 1;
            if (d.Version == 1)
            {
                if (d.CommittedBattleIds == null) d.CommittedBattleIds = new List<string>();
                if (d.Energy <= 0) d.Energy = cfg.MaxEnergy;
                foreach (var p in d.Pets)
                {
                    if (p.SocketTiers == null) p.SocketTiers = new List<int>();
                    if (p.SocketElements == null) p.SocketElements = new List<Element>();
                }
                d.Version = 2;
            }
            if (d.Version == 2)
            {
                if (d.AvatarOwned == null) d.AvatarOwned = new List<string>();
                if (d.AvatarWorn == null) d.AvatarWorn = new List<string>();
                if (string.IsNullOrEmpty(d.PlayerName)) d.PlayerName = cfg.DefaultPlayerName;
                foreach (var id in cfg.StarterAvatarIds)
                {
                    if (!d.AvatarOwned.Contains(id)) d.AvatarOwned.Add(id);
                    if (!d.AvatarWorn.Contains(id)) d.AvatarWorn.Add(id);
                }
                d.Version = 3;
            }
            if (d.SkillCards == null) d.SkillCards = new List<OwnedCard>();
            if (d.CardStones == null) d.CardStones = new List<int>();
            if (d.SelectedCardIds == null) d.SelectedCardIds = new List<string>();
            if (d.Version == 3)
            {
                foreach (var id in cfg.StarterSkillIds)
                {
                    if (d.SkillCard(id) != null) continue;
                    d.SkillCards.Add(new OwnedCard { Id = id });
                    if (d.SelectedCardIds.Count < 5) d.SelectedCardIds.Insert(0, id);
                }
                d.Version = 4;
            }
            if (d.Version == 4)
            {
                d.PlayerExp = 0;
                d.Version = 5;
            }
            if (d.AvatarOwned == null) d.AvatarOwned = new List<string>();
            if (d.AvatarWorn == null) d.AvatarWorn = new List<string>();
            if (d.Pets == null) d.Pets = new List<OwnedPet>();
            if (d.Cards == null) d.Cards = new List<string>();
            if (d.Stones == null) d.Stones = new List<StoneStack>();
            if (d.Nodes == null) d.Nodes = new List<NodeProgress>();
            if (d.SelectedCardIds == null) d.SelectedCardIds = new List<string>();
            if (d.CommittedBattleIds == null) d.CommittedBattleIds = new List<string>();
            return d;
        }
    }

    public interface ISaveStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public interface ISaveSerializer
    {
        string ToJson(SaveData d);
        SaveData FromJson(string json);
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        public string Json;
        public string Read() => Json;
        public void Write(string json) => Json = json;
        public void Delete() => Json = null;
    }

    public sealed class SaveService
    {
        private readonly ISaveStore store;
        private readonly ISaveSerializer serializer;
        private readonly ProgressionService progression;

        public SaveService(ISaveStore store, ISaveSerializer serializer, ProgressionService progression)
        {
            this.store = store;
            this.serializer = serializer;
            this.progression = progression;
        }

        public SaveData LoadOrCreate()
        {
            string json = null;
            try { json = store.Read(); }
            catch (Exception) { json = null; }
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var d = serializer.FromJson(json);
                    if (d != null) return SaveMigrator.Migrate(d, progression.Db.Progression);
                }
                catch (Exception) { }
            }
            var fresh = progression.CreateNewSave(DateTime.UtcNow.Ticks);
            Save(fresh);
            return fresh;
        }

        public void Save(SaveData d) => store.Write(serializer.ToJson(d));

        public SaveData Reset()
        {
            store.Delete();
            return LoadOrCreate();
        }
    }
}
