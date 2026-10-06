using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public sealed class BattleReport
    {
        public string BattleId;
        public string EncounterId;
        public string NodeId;
        public string PetUid;
        public bool Won;
        public int Turns;
    }

    public sealed class RewardGrant
    {
        public string BattleId;
        public bool Won;
        public int Gold;
        public int PlayerExp;
        public int PetExp;
        public int PlayerLevelsGained;
        public int PetLevelsGained;
        public bool UnlockedNext;
        public string CapturedPetId;
        public readonly List<string> Lines = new List<string>();
    }

    public sealed class EntryCheck
    {
        public bool Ok;
        public string Reason;
    }

    /// <summary>Player level, pet level/EXP, energy and encounter difficulty are separate inputs.</summary>
    public sealed class ProgressionService
    {
        public readonly ContentDatabase Db;

        public const int LoadoutSize = 5;
        public const int MaxSkillsInLoadout = 2;

        public ProgressionService(ContentDatabase db)
        {
            Db = db;
        }

        public SaveData CreateNewSave(long nowUtcTicks)
        {
            var cfg = Db.Progression;
            var d = new SaveData { Gold = cfg.StartGold, Energy = cfg.MaxEnergy, EnergyStampUtcTicks = nowUtcTicks };
            for (int i = 0; i < cfg.StarterPetIds.Count; i++)
            {
                var pet = AddPet(d, cfg.StarterPetIds[i]);
                pet.Level = i < cfg.StarterPetLevels.Count ? cfg.StarterPetLevels[i] : 1;
            }
            foreach (var c in cfg.StarterCardIds) if (!d.Cards.Contains(c)) d.Cards.Add(c);
            foreach (var item in cfg.StarterItems) GrantItem(d, item, null);
            d.SelectedPetUid = d.Pets.Count > 0 ? d.Pets[0].Uid : null;
            foreach (var id in cfg.StarterSkillIds)
            {
                if (d.SkillCard(id) != null) continue;
                d.SkillCards.Add(new OwnedCard { Id = id });
                if (d.SelectedCardIds.Count < MaxSkillsInLoadout) d.SelectedCardIds.Add(id);
            }
            d.SelectedCardIds.AddRange(d.Cards.GetRange(0, Math.Min(LoadoutSize - d.SelectedCardIds.Count, d.Cards.Count)));
            new AvatarService(Db).GrantStarter(d);
            return d;
        }

        public OwnedPet AddPet(SaveData d, string petId)
        {
            var pet = new OwnedPet { Uid = "p" + d.NextPetUid++, PetId = petId, Level = 1 };
            for (int i = 0; i < Db.Upgrades.SocketCount; i++)
            {
                pet.SocketTiers.Add(0);
                pet.SocketElements.Add(Element.Neutral);
            }
            d.Pets.Add(pet);
            return pet;
        }

        public void TickEnergy(SaveData d, long nowUtcTicks)
        {
            var cfg = Db.Progression;
            if (d.Energy >= cfg.MaxEnergy)
            {
                d.EnergyStampUtcTicks = nowUtcTicks;
                return;
            }
            long elapsed = nowUtcTicks - d.EnergyStampUtcTicks;
            long per = TimeSpan.FromSeconds(Math.Max(1, cfg.EnergyRegenSeconds)).Ticks;
            if (elapsed < per) return;
            int gained = (int)Math.Min(cfg.MaxEnergy, elapsed / per);
            d.Energy = Math.Min(cfg.MaxEnergy, d.Energy + gained);
            d.EnergyStampUtcTicks += gained * per;
            if (d.Energy >= cfg.MaxEnergy) d.EnergyStampUtcTicks = nowUtcTicks;
        }

        public bool IsNodeUnlocked(SaveData d, MapNodeDef node)
        {
            if (string.IsNullOrEmpty(node.RequiresNodeId)) return true;
            var req = Db.Map.Nodes.Find(n => n.Id == node.RequiresNodeId);
            if (req == null) return true;
            return d.Wins(req.Id) >= Math.Max(1, req.WinsRequired);
        }

        public EntryCheck CanEnter(SaveData d, MapNodeDef node)
        {
            var enc = Db.Encounter(node.EncounterId);
            if (!IsNodeUnlocked(d, node)) return new EntryCheck { Reason = "Locked" };
            if (d.Energy < enc.EnergyCost) return new EntryCheck { Reason = "Not enough energy" };
            if (d.Pet(d.SelectedPetUid) == null) return new EntryCheck { Reason = "Pick a pet" };
            return new EntryCheck { Ok = true };
        }

        public StatBlock PetStats(OwnedPet pet)
        {
            var def = Db.Creature(pet.PetId);
            var st = def.StatsAt(pet.Level);
            var up = Db.Upgrades;
            float enh = 1f + pet.EnhanceLevel * up.EnhanceStatPctPerLevel;
            st.MaxHp = (int)Math.Floor(st.MaxHp * enh);
            st.Atk = (int)Math.Floor(st.Atk * enh);
            st.Def = (int)Math.Floor(st.Def * enh);
            foreach (var t in pet.SocketTiers)
            {
                if (t <= 0) continue;
                st.Atk += t * up.SocketAtkPerTier;
                st.MaxHp += t * up.SocketHpPerTier;
            }
            return st;
        }

        public int PetElementBonus(OwnedPet pet)
        {
            var def = Db.Creature(pet.PetId);
            int bonus = 0;
            for (int i = 0; i < pet.SocketTiers.Count; i++)
                if (pet.SocketTiers[i] > 0 && i < pet.SocketElements.Count && pet.SocketElements[i] == def.Element)
                    bonus += pet.SocketTiers[i] * Db.Upgrades.SocketElementBonusPerTier;
            return bonus;
        }

        public CombatantSetup BuildPlayer(SaveData d, OwnedPet pet, IList<string> cardIds)
        {
            var def = Db.Creature(pet.PetId);
            var s = new CombatantSetup
            {
                Creature = def,
                Level = pet.Level,
                Stats = PetStats(pet),
                ElementBonus = PetElementBonus(pet),
                Gems = Db.GemProfile(def.GemProfileId),
                Ai = Db.Ai("ai.autoplay")
            };
            foreach (var id in cardIds)
            {
                var owned = d.SkillCard(id);
                if (owned == null || s.Skills.Count >= MaxSkillsInLoadout || s.Skills.Exists(x => x.Id == id)) continue;
                s.Skills.Add(Db.Skill(id));
                s.SkillLevels[id] = owned.Level;
            }
            foreach (var ph in def.Phases)
                foreach (var id in ph.UnlockSkillIds)
                    s.LockedSkills.Add(Db.Skill(id));
            int n = s.Skills.Count;
            foreach (var id in cardIds)
            {
                if (n >= LoadoutSize) break;
                var c = Db.TryCard(id);
                if (c == null || !d.Cards.Contains(id)) continue;
                s.Cards.Add(c);
                n++;
            }
            return s;
        }

        public CombatantSetup BuildEnemy(EncounterDef enc)
        {
            var def = Db.Creature(enc.CreatureId);
            var s = new CombatantSetup
            {
                Creature = def,
                Level = enc.Level,
                StartHpPct = enc.StartHpPct,
                Gems = Db.GemProfile(def.GemProfileId),
                Ai = Db.Ai(enc.AiPolicyId)
            };
            foreach (var id in def.SkillIds)
            {
                s.Skills.Add(Db.Skill(id));
                s.SkillLevels[id] = enc.SkillLevel;
            }
            foreach (var id in def.CardIds) s.Cards.Add(Db.Card(id));
            foreach (var ph in def.Phases)
                foreach (var id in ph.UnlockSkillIds)
                {
                    s.LockedSkills.Add(Db.Skill(id));
                    s.SkillLevels[id] = enc.SkillLevel;
                }
            return s;
        }

        /// <summary>Spends energy and returns a battle setup with a unique id. The id is what makes the reward commit once.</summary>
        public BattleSetup StartBattle(SaveData d, MapNodeDef node, uint seed)
        {
            var enc = Db.Encounter(node.EncounterId);
            d.Energy -= enc.EnergyCost;
            d.BattlesStarted++;
            var pet = d.Pet(d.SelectedPetUid);
            var player = BuildPlayer(d, pet, d.SelectedCardIds);
            var enemy = BuildEnemy(enc);
            player.StartHpPct = Db.Rules.StartHpPct;
            enemy.StartHpPct = enc.StartHpPct * Db.Rules.StartHpPct;
            if (enc.HpVsPlayer > 0f)
            {
                var st = enemy.Creature.StatsAt(enc.Level);
                st.MaxHp = Math.Max(st.MaxHp, (int)Math.Ceiling(player.Stats.MaxHp * enc.HpVsPlayer));
                enemy.Stats = st;
            }
            return new BattleSetup
            {
                BattleId = node.Id + "#" + d.BattlesStarted + "#" + seed,
                Seed = seed,
                Player = player,
                Enemy = enemy,
                Board = Db.Board,
                Rules = Db.Rules,
                FirstTurn = enc.FirstTurn
            };
        }

        /// <summary>Applies the outcome exactly once per battle id. A second call returns null and changes nothing.</summary>
        public RewardGrant CommitBattle(SaveData d, BattleReport report)
        {
            if (string.IsNullOrEmpty(report.BattleId) || d.CommittedBattleIds.Contains(report.BattleId)) return null;
            d.CommittedBattleIds.Add(report.BattleId);
            if (d.CommittedBattleIds.Count > 200) d.CommittedBattleIds.RemoveAt(0);

            var grant = new RewardGrant { BattleId = report.BattleId, Won = report.Won };
            var node = Db.Map.Nodes.Find(n => n.Id == report.NodeId);
            var np = node != null ? d.Node(node.Id, true) : null;
            if (!report.Won)
            {
                if (np != null) np.Losses++;
                grant.Lines.Add("Defeat - no reward. Try again!");
                return grant;
            }
            var enc = Db.Encounter(report.EncounterId);
            var table = Db.Reward(enc.RewardTableId);
            bool firstClear = np == null || np.Wins == 0;
            bool wasNextLocked = NextNodes(node).Exists(n => !IsNodeUnlocked(d, n));
            if (np != null) np.Wins++;
            grant.UnlockedNext = wasNextLocked && NextNodes(node).Exists(n => IsNodeUnlocked(d, n));

            grant.Gold = table.Gold;
            d.Gold += table.Gold;
            grant.Lines.Add("+" + table.Gold + " Gold");

            grant.PlayerExp = table.PlayerExp;
            grant.PlayerLevelsGained = AddPlayerExp(d, table.PlayerExp);
            grant.Lines.Add("+" + table.PlayerExp + " Player EXP" + (grant.PlayerLevelsGained > 0 ? " (Level up! Lv " + d.PlayerLevel + ")" : ""));

            var pet = d.Pet(report.PetUid);
            if (pet != null)
            {
                grant.PetExp = table.PetExp;
                grant.PetLevelsGained = AddPetExp(pet, table.PetExp);
                grant.Lines.Add("+" + table.PetExp + " Pet EXP" + (grant.PetLevelsGained > 0 ? " (" + Db.Creature(pet.PetId).Name + " Lv " + pet.Level + ")" : ""));
            }

            var rng = new SeededRng(Hash(report.BattleId));
            foreach (var drop in table.Drops)
            {
                if (drop.FirstClearOnly && !firstClear) continue;
                if (drop.Chance < 1f && !rng.Chance(drop.Chance)) continue;
                GrantItem(d, drop, grant.Lines);
            }
            if (firstClear && enc.CaptureOnFirstWin && !d.Pets.Exists(p => p.PetId == enc.CreatureId))
            {
                AddPet(d, enc.CreatureId).Level = enc.Level;
                grant.CapturedPetId = enc.CreatureId;
                grant.Lines.Add("New pet: " + Db.Creature(enc.CreatureId).Name);
            }
            if (grant.UnlockedNext) grant.Lines.Add("New area unlocked!");
            return grant;
        }

        private List<MapNodeDef> NextNodes(MapNodeDef node)
        {
            if (node == null) return new List<MapNodeDef>();
            return Db.Map.Nodes.FindAll(n => n.RequiresNodeId == node.Id);
        }

        public void GrantItem(SaveData d, RewardDrop drop, List<string> lines)
        {
            switch (drop.Kind)
            {
                case RewardKind.Stone:
                    d.AddStones(drop.Element, drop.Tier, drop.Count);
                    lines?.Add("+" + drop.Count + " " + drop.Element + " Stone T" + drop.Tier);
                    break;
                case RewardKind.Card:
                    if (!d.Cards.Contains(drop.ItemId))
                    {
                        d.Cards.Add(drop.ItemId);
                        lines?.Add("New card: " + Db.Card(drop.ItemId).Name);
                    }
                    break;
                case RewardKind.LuckyCharm:
                    d.LuckyCharms += drop.Count;
                    lines?.Add("+" + drop.Count + " Lucky Charm");
                    break;
                case RewardKind.ProtectionCharm:
                    d.ProtectionCharms += drop.Count;
                    lines?.Add("+" + drop.Count + " Protection Charm");
                    break;
                case RewardKind.CardStone:
                    d.AddCardStones(drop.Tier, drop.Count);
                    lines?.Add("+" + drop.Count + " Card Stone T" + drop.Tier);
                    break;
                case RewardKind.SkillCard:
                    if (d.SkillCard(drop.ItemId) == null)
                    {
                        d.SkillCards.Add(new OwnedCard { Id = drop.ItemId });
                        lines?.Add("New card: " + Db.Skill(drop.ItemId).Name);
                    }
                    break;
                case RewardKind.Pet:
                    if (!d.Pets.Exists(p => p.PetId == drop.ItemId))
                    {
                        AddPet(d, drop.ItemId);
                        lines?.Add("New pet: " + Db.Creature(drop.ItemId).Name);
                    }
                    break;
            }
        }

        public int PlayerExpToNext(int level)
        {
            var t = Db.Progression.PlayerExpToNext;
            if (t == null || t.Length == 0) return 100 * level;
            return t[Math.Min(level - 1, t.Length - 1)] + Math.Max(0, level - t.Length) * 400;
        }

        public int PetExpToNext(int level) => Db.Progression.PetExpBase + Db.Progression.PetExpPerLevel * (level - 1);

        public int AddPlayerExp(SaveData d, int exp)
        {
            int gained = 0;
            d.PlayerExp += exp;
            while (d.PlayerExp >= PlayerExpToNext(d.PlayerLevel))
            {
                d.PlayerExp -= PlayerExpToNext(d.PlayerLevel);
                d.PlayerLevel++;
                gained++;
            }
            return gained;
        }

        public int AddPetExp(OwnedPet pet, int exp)
        {
            int gained = 0;
            pet.Exp += exp;
            while (pet.Level < Db.Progression.PetMaxLevel && pet.Exp >= PetExpToNext(pet.Level))
            {
                pet.Exp -= PetExpToNext(pet.Level);
                pet.Level++;
                gained++;
            }
            return gained;
        }

        public static uint Hash(string s)
        {
            uint h = 2166136261u;
            foreach (char c in s ?? "")
            {
                h ^= c;
                h *= 16777619u;
            }
            return h == 0 ? 1u : h;
        }
    }
}
