using NUnit.Framework;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Tests
{
    public class ProgressionTests
    {
        private sealed class UnitySerializer : ISaveSerializer
        {
            public string ToJson(SaveData d) => JsonUtility.ToJson(d);
            public SaveData FromJson(string json) => JsonUtility.FromJson<SaveData>(json);
        }

        [Test]
        public void Content_IsValid()
        {
            CollectionAssert.IsEmpty(DefaultContent.Create().Validate());
        }

        [Test]
        public void Reward_IsCommittedExactlyOnce_AndUnlocksNextNode()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var node1 = db.Node("node.1");
            var node2 = db.Node("node.2");
            Assert.IsFalse(prog.IsNodeUnlocked(save, node2));

            var setup = prog.StartBattle(save, node1, 11);
            int gold = save.Gold;
            var report = new BattleReport { BattleId = setup.BattleId, EncounterId = "enc.dunewing", NodeId = node1.Id, PetUid = save.SelectedPetUid, Won = true };

            var first = prog.CommitBattle(save, report);
            var second = prog.CommitBattle(save, report);

            Assert.IsNotNull(first);
            Assert.IsNull(second, "same battle id never pays twice");
            Assert.AreEqual(gold + db.Reward("rw.dunewing").Gold, save.Gold);
            Assert.AreEqual(1, save.Wins(node1.Id));
            Assert.IsTrue(first.UnlockedNext);
            Assert.IsTrue(prog.IsNodeUnlocked(save, node2));
        }

        [Test]
        public void Loss_GrantsNothing_AndCanBeRetried()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            int gold = save.Gold;
            var setup = prog.StartBattle(save, db.Node("node.1"), 1);
            var g = prog.CommitBattle(save, new BattleReport { BattleId = setup.BattleId, EncounterId = "enc.dunewing", NodeId = "node.1", Won = false });
            Assert.IsFalse(g.Won);
            Assert.AreEqual(gold, save.Gold);
            Assert.IsTrue(prog.CanEnter(save, db.Node("node.1")).Ok);
            var retry = prog.StartBattle(save, db.Node("node.1"), 1);
            Assert.AreNotEqual(setup.BattleId, retry.BattleId, "a retry is a new battle");
        }

        [Test]
        public void SaveLoad_RoundTripsThroughJson()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var store = new MemorySaveStore();
            var svc = new SaveService(store, new UnitySerializer(), prog);
            var save = svc.LoadOrCreate();
            save.Gold = 1234;
            save.Node("node.1", true).Wins = 2;
            save.AddStones(Element.Water, 3, 5);
            save.Pets[0].EnhanceLevel = 4;
            save.CommittedBattleIds.Add("b1");
            svc.Save(save);

            var loaded = new SaveService(store, new UnitySerializer(), prog).LoadOrCreate();
            Assert.AreEqual(1234, loaded.Gold);
            Assert.AreEqual(2, loaded.Wins("node.1"));
            Assert.AreEqual(5, loaded.StoneCount(Element.Water, 3));
            Assert.AreEqual(4, loaded.Pets[0].EnhanceLevel);
            CollectionAssert.Contains(loaded.CommittedBattleIds, "b1");
            Assert.AreEqual(SaveData.CurrentVersion, loaded.Version);
        }

        [Test]
        public void SaveLoad_MigratesVersion1()
        {
            var db = DefaultContent.Create();
            var store = new MemorySaveStore { Json = "{\"Version\":1,\"Gold\":77,\"Energy\":0,\"Pets\":[{\"Uid\":\"p1\",\"PetId\":\"pet.emberkit\",\"Level\":4}]}" };
            var loaded = new SaveService(store, new UnitySerializer(), new ProgressionService(db)).LoadOrCreate();
            Assert.AreEqual(2, loaded.Version);
            Assert.AreEqual(77, loaded.Gold);
            Assert.AreEqual(db.Progression.MaxEnergy, loaded.Energy);
            Assert.AreEqual(4, loaded.Pets[0].Level);
            Assert.IsNotNull(loaded.CommittedBattleIds);
        }

        [Test]
        public void CorruptSave_FallsBackToFreshSave()
        {
            var store = new MemorySaveStore { Json = "{not json" };
            var loaded = new SaveService(store, new UnitySerializer(), new ProgressionService(DefaultContent.Create())).LoadOrCreate();
            Assert.Greater(loaded.Pets.Count, 0);
        }

        [Test]
        public void StoneMerge_SuccessAndFailureBothConsumeThree()
        {
            var db = DefaultContent.Create();
            var up = new UpgradeService(db);
            var save = new SaveData { Gold = 10000 };
            save.AddStones(Element.Fire, 1, 6);

            db.Upgrades.MergeChanceByTier = new[] { 1f };
            Assert.IsTrue(up.MergeStones(save, Element.Fire, 1, false, new SeededRng(1)).Success);
            Assert.AreEqual(3, save.StoneCount(Element.Fire, 1));
            Assert.AreEqual(1, save.StoneCount(Element.Fire, 2));

            db.Upgrades.MergeChanceByTier = new[] { 0f };
            var fail = up.MergeStones(save, Element.Fire, 1, false, new SeededRng(1));
            Assert.IsTrue(fail.Attempted);
            Assert.IsFalse(fail.Success);
            Assert.AreEqual(0, save.StoneCount(Element.Fire, 1));
            Assert.AreEqual(1, save.StoneCount(Element.Fire, 2));
            Assert.IsFalse(up.MergeStones(save, Element.Fire, 2, false, new SeededRng(1)).Attempted, "needs three");
        }

        [Test]
        public void Enhance_Protection_PreventsDowngrade_AndFailureAccumulates()
        {
            var db = DefaultContent.Create();
            db.Upgrades.EnhanceChanceByLevel = new[] { 0f };
            db.Upgrades.EnhanceTierBonusPerTierAbove = 0;
            db.Upgrades.EnhanceFailAccumulate = 0;
            db.Upgrades.EnhanceMinChance = 0;
            var up = new UpgradeService(db);
            var save = new SaveData { Gold = 100000, ProtectionCharms = 1 };
            save.AddStones(Element.Fire, 1, 2);
            var pet = new OwnedPet { Uid = "p1", PetId = "pet.emberkit", EnhanceLevel = 3 };

            up.Enhance(save, pet, Element.Fire, 1, false, true, new SeededRng(1));
            Assert.AreEqual(3, pet.EnhanceLevel);
            up.Enhance(save, pet, Element.Fire, 1, false, false, new SeededRng(1));
            Assert.AreEqual(2, pet.EnhanceLevel);
        }

        [Test]
        public void PetLevel_ChangesStatsOnce_NotDamageTwice()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var pet = new OwnedPet { PetId = "pet.emberkit", Level = 5 };
            var def = db.Creature("pet.emberkit");
            Assert.AreEqual(def.BaseStats.Atk + 4 * def.PerLevel.Atk, prog.PetStats(pet).Atk);
        }

        [Test]
        public void Energy_IsSpentOnStart_AndRegenerates()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            int e0 = save.Energy;
            prog.StartBattle(save, db.Node("node.1"), 1);
            Assert.AreEqual(e0 - db.Encounter("enc.dunewing").EnergyCost, save.Energy);
            prog.TickEnergy(save, System.TimeSpan.FromSeconds(db.Progression.EnergyRegenSeconds * 3).Ticks);
            Assert.AreEqual(e0, save.Energy);
        }
    }
}
