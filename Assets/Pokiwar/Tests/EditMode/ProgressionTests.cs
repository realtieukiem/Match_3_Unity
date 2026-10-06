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
        public void FirstWin_CapturesTheCreatureFought_Once()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            int pets = save.Pets.Count;
            var node = db.Node("node.1");

            var a = prog.StartBattle(save, node, 1);
            var first = prog.CommitBattle(save, new BattleReport { BattleId = a.BattleId, EncounterId = "enc.dunewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = true });
            var b = prog.StartBattle(save, node, 2);
            var second = prog.CommitBattle(save, new BattleReport { BattleId = b.BattleId, EncounterId = "enc.dunewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = true });

            Assert.AreEqual("mon.dunewing", first.CapturedPetId);
            Assert.IsNull(second.CapturedPetId, "only the first win captures");
            Assert.AreEqual(pets + 1, save.Pets.Count);
            var owned = save.Pets.Find(p => p.PetId == "mon.dunewing");
            Assert.AreEqual(1, owned.Level, "a captured creature arrives at level 1");
            CollectionAssert.Contains(first.Lines, "New pet: Dunewing");
        }

        [Test]
        public void Loss_ThenWin_StillCaptures_AndTheBossGivesItself()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var node = db.Node("node.3");

            var a = prog.StartBattle(save, node, 1);
            var lost = prog.CommitBattle(save, new BattleReport { BattleId = a.BattleId, EncounterId = "enc.azurewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = false });
            var b = prog.StartBattle(save, node, 2);
            var won = prog.CommitBattle(save, new BattleReport { BattleId = b.BattleId, EncounterId = "enc.azurewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = true });

            Assert.IsNull(lost.CapturedPetId);
            Assert.AreEqual("boss.azurewing", won.CapturedPetId);
            Assert.IsFalse(save.Pets.Exists(p => p.PetId == "pet.tidepup"));
            var mine = prog.BuildPlayer(save, save.Pets.Find(p => p.PetId == "boss.azurewing"), save.SelectedCardIds);
            Assert.AreEqual(0, mine.LockedSkills.Count, "an owned boss does not ascend in battle");
            Assert.IsTrue(mine.NoPhases);
        }

        [Test]
        public void Encounter_WithCaptureOff_GivesNoPet()
        {
            var db = DefaultContent.Create();
            db.Encounter("enc.dunewing").CaptureOnFirstWin = false;
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var a = prog.StartBattle(save, db.Node("node.1"), 1);
            var g = prog.CommitBattle(save, new BattleReport { BattleId = a.BattleId, EncounterId = "enc.dunewing", NodeId = "node.1", PetUid = save.SelectedPetUid, Won = true });
            Assert.IsNull(g.CapturedPetId);
            Assert.IsFalse(save.Pets.Exists(p => p.PetId == "mon.dunewing"));
        }

        [Test]
        public void Battle_StartsBelowFullHp_OnBothSides()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var e = new BattleEngine(prog.StartBattle(save, db.Node("node.1"), 3));
            foreach (var side in new[] { Side.Player, Side.Enemy })
            {
                var c = e.State.Get(side);
                Assert.AreEqual((int)System.Math.Round(c.Hp.Max * db.Rules.StartHpPct), c.Hp.Current, side + " starts at the rule's share of max HP");
                Assert.Less(c.Hp.Current, c.Hp.Max);
            }
        }

        [Test]
        public void Boss_KeepsItsOwnHp_AndMirrorsThePlayersMana()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            foreach (int level in new[] { 1, 8, 30, 60 })
            {
                var save = prog.CreateNewSave(0);
                var pet = save.Pet(save.SelectedPetUid);
                pet.Level = level;
                foreach (var node in db.Map.Nodes)
                {
                    var enc = db.Encounter(node.EncounterId);
                    save.Energy = enc.EnergyCost;
                    var setup = prog.StartBattle(save, node, 5);
                    Assert.AreEqual(db.Creature(enc.CreatureId).StatsAt(enc.Level).MaxHp, setup.Enemy.Stats.MaxHp, node.Id + " HP does not follow a pet at Lv " + level);
                    Assert.AreEqual((int)System.Math.Ceiling(setup.Player.Stats.MaxMana * enc.ManaVsPlayer), setup.Enemy.Stats.MaxMana, node.Id + " mana follows a pet at Lv " + level);
                }
            }
            var par = prog.CreateNewSave(0);
            var boss = db.Encounter("enc.azurewing");
            par.Pet(par.SelectedPetUid).Level = boss.Level;
            par.Energy = boss.EnergyCost;
            var atPar = prog.StartBattle(par, db.Node("node.3"), 5);
            Assert.GreaterOrEqual(atPar.Enemy.Stats.MaxHp, atPar.Player.Stats.MaxHp * 2, "the region boss has at least twice the HP of a pet of its own level");
        }

        [Test]
        public void ReusableCards_BelongToThePlayer_AndAnyPetUsesThem()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            Assert.AreEqual(ProgressionService.LoadoutSize, save.SelectedCardIds.Count);
            Assert.IsNotNull(save.SkillCard("skill.blaze_burst"));
            save.SkillCard("skill.blaze_burst").Level = 4;

            foreach (var pet in save.Pets)
            {
                var setup = prog.BuildPlayer(save, pet, save.SelectedCardIds);
                Assert.IsTrue(setup.Skills.Exists(s => s.Id == "skill.blaze_burst"), db.Creature(pet.PetId).Name + " carries the player's card");
                Assert.AreEqual(4, setup.SkillLevels["skill.blaze_burst"]);
                Assert.AreEqual(ProgressionService.LoadoutSize, setup.Skills.Count + setup.Cards.Count);
            }
            save.SelectedCardIds.Remove("skill.blaze_burst");
            Assert.IsFalse(prog.BuildPlayer(save, save.Pets[0], save.SelectedCardIds).Skills.Exists(s => s.Id == "skill.blaze_burst"));
        }

        [Test]
        public void CardLevel_RaisesItsDamage_ForBossesToo()
        {
            var db = DefaultContent.Create();
            var blaze = db.Skill("skill.blaze_burst");
            Assert.Greater(BattleEngine.SkillPower(100, blaze, 5), BattleEngine.SkillPower(100, blaze, 1));

            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var e = new BattleEngine(prog.StartBattle(save, db.Node("node.2"), 3));
            var foe = e.State.Get(Side.Enemy);
            Assert.AreEqual(db.Encounter("enc.psyling").SkillLevel, foe.SkillLevel(foe.Skills[0]));
        }

        [Test]
        public void CardStones_UpgradeCards_AndNeverTouchPetStones()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var up = new UpgradeService(db);
            var save = prog.CreateNewSave(0);
            save.Gold = 100000;
            var card = save.SkillCard("skill.blaze_burst");
            int petStones = 0;
            foreach (var st in save.Stones) petStones += st.Count;
            int cardStones = save.CardStoneCount(1);
            Assert.Greater(cardStones, 0, "a new save starts with card stones");

            int level = card.Level, attempts = 0;
            var rng = new SeededRng(7);
            while (save.CardStoneCount(1) > 0 && card.Level == level)
            {
                Assert.IsTrue(up.UpgradeCard(save, card, 1, false, false, rng).Attempted);
                attempts++;
            }
            Assert.AreEqual(cardStones - attempts, save.CardStoneCount(1));
            int after = 0;
            foreach (var st in save.Stones) after += st.Count;
            Assert.AreEqual(petStones, after, "pet stones untouched");
            Assert.IsFalse(up.UpgradeCard(save, card, 6, false, false, rng).Attempted, "no stone of that tier");
        }

        [Test]
        public void OldSave_GetsStarterReusableCards()
        {
            var db = DefaultContent.Create();
            var old = new SaveData { Version = 3 };
            old.SelectedCardIds.Add("card.mana_potion");
            var d = SaveMigrator.Migrate(old, db.Progression);
            Assert.AreEqual(SaveData.CurrentVersion, d.Version);
            foreach (var id in db.Progression.StarterSkillIds)
            {
                Assert.IsNotNull(d.SkillCard(id));
                CollectionAssert.Contains(d.SelectedCardIds, id);
            }
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
            Assert.AreEqual(SaveData.CurrentVersion, loaded.Version);
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
            var pet = new OwnedPet { Uid = "p1", PetId = "pet.emberkit", Level = 4 };

            up.Enhance(save, pet, Element.Fire, 1, false, true, new SeededRng(1));
            Assert.AreEqual(4, pet.Level);
            up.Enhance(save, pet, Element.Fire, 1, false, false, new SeededRng(1));
            Assert.AreEqual(3, pet.Level);
            save.AddStones(Element.Water, 1, 1);
            Assert.IsFalse(up.Enhance(save, pet, Element.Water, 1, false, false, new SeededRng(1)).Attempted, "only a stone of the pet's own element upgrades it");
            Assert.AreEqual(1, save.StoneCount(Element.Water, 1));
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

        [Test]
        public void Pet_EvolvesAtEnhanceFive_AndNeverChangesPhaseInBattle()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var pet = save.Pet(save.SelectedPetUid);
            string baseKey = db.Creature(pet.PetId).SpriteKey;
            pet.Level = db.Progression.EvolveAtLevel - 1;
            Assert.AreEqual(baseKey, prog.PetSpriteKey(pet));
            pet.Level = db.Progression.PetMaxLevel;
            Assert.AreEqual(baseKey, prog.PetSpriteKey(pet), "the free starter has no second form at any level");
            Assert.AreEqual(1, save.Pets.Count, "one free pet");
            Assert.AreEqual(1, prog.CreateNewSave(0).Pets[0].Level, "which starts at level 1");
            var beetle = prog.AddPet(save, "mon.dunewing");
            beetle.Level = db.Progression.EvolveAtLevel;
            Assert.AreEqual(db.Creature("mon.dunewing").Phases[0].SpriteKey, prog.PetSpriteKey(beetle), "a collected creature wears its boss second form");

            var boss = db.Creature("boss.azurewing");
            var owned = prog.AddPet(save, boss.Id);
            Assert.AreEqual(1, owned.Level);
            Assert.AreEqual(boss.SpriteKey, prog.PetSpriteKey(owned));
            owned.Level = db.Progression.EvolveAtLevel;
            Assert.AreEqual(boss.Phases[0].SpriteKey, prog.PetSpriteKey(owned));

            save.SelectedPetUid = owned.Uid;
            save.Energy = 10;
            var setup = prog.StartBattle(save, db.Node("node.3"), 7);
            Assert.AreEqual(0, Combatant.Create(Side.Player, setup.Player).Phases.Count, "a pet has no second phase in battle");
            Assert.AreEqual(boss.Phases[0].SpriteKey, Combatant.Create(Side.Player, setup.Player).SpriteKey);
            Assert.AreEqual(1, Combatant.Create(Side.Enemy, setup.Enemy).Phases.Count, "the boss keeps its second phase");
        }

        [Test]
        public void TrainerLevelAndClothes_AddToThePetInBattle()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            var pet = save.Pet(save.SelectedPetUid);
            var own = prog.PetStats(pet);
            var naked = prog.BattleStats(save, pet);
            Assert.AreEqual(own.MaxHp, naked.MaxHp, "starter clothes and trainer level 1 add nothing");

            save.PlayerLevel = 2;
            var lv2 = prog.BattleStats(save, pet);
            Assert.AreEqual(own.MaxHp + 5, lv2.MaxHp);
            Assert.AreEqual(own.MaxMana + 5, lv2.MaxMana);
            Assert.AreEqual(own.Atk, lv2.Atk);

            var jacket = db.TryAvatarItem("avatar.top.jacket");
            var cap = db.TryAvatarItem("avatar.hat.cap");
            Assert.Greater(jacket.BonusHp, 0);
            Assert.Greater(cap.BonusAtk, 0);
            save.AvatarWorn.Add(jacket.Id);
            save.AvatarWorn.Add(cap.Id);
            var dressed = prog.BattleStats(save, pet);
            Assert.AreEqual(lv2.MaxHp + jacket.BonusHp, dressed.MaxHp);
            Assert.AreEqual(lv2.Atk + cap.BonusAtk, dressed.Atk);

            save.Energy = 10;
            var setup = prog.StartBattle(save, db.Node("node.1"), 3);
            Assert.AreEqual(dressed.MaxHp, setup.Player.Stats.MaxHp);
            Assert.AreEqual(dressed.MaxMana, setup.Enemy.Stats.MaxMana, "the boss mirrors the mana the pet really has");
        }

        [Test]
        public void Exp_FollowsTheClips_WinByHuntLevel_LossOne()
        {
            var db = DefaultContent.Create();
            var prog = new ProgressionService(db);
            Assert.AreEqual(20, prog.ExpForWin(2, 1), "clip A: Flygon, hunt level 2, trainer 1");
            Assert.AreEqual(167, prog.ExpForWin(108, 35), 1, "clip B: BlueWings, trainer 35");
            Assert.AreEqual(162, prog.ExpForWin(108, 36), 1, "clip B: BlueWings, trainer 36");
            Assert.AreEqual(1, prog.ExpForWin(1, 50), "never below 1");

            var save = prog.CreateNewSave(0);
            var node = db.Node("node.1");
            int win = prog.ExpForWin(db.Encounter("enc.dunewing").Level, 1);
            save.Energy = 10;
            var a = prog.StartBattle(save, node, 1);
            var first = prog.CommitBattle(save, new BattleReport { BattleId = a.BattleId, EncounterId = "enc.dunewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = true });
            Assert.AreEqual(win, first.PlayerExp);
            var b = prog.StartBattle(save, node, 2);
            var lost = prog.CommitBattle(save, new BattleReport { BattleId = b.BattleId, EncounterId = "enc.dunewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = false });
            Assert.AreEqual(1, lost.PlayerExp, "a loss still gives 1 EXP");
            Assert.AreEqual(1, save.PlayerLevel, "one win and one loss do not reach level 2, as in clip A");
            var c = prog.StartBattle(save, node, 3);
            prog.CommitBattle(save, new BattleReport { BattleId = c.BattleId, EncounterId = "enc.dunewing", NodeId = node.Id, PetUid = save.SelectedPetUid, Won = true });
            Assert.AreEqual(2, save.PlayerLevel, "the second win does");
            Assert.Less(prog.ExpForWin(db.Encounter("enc.dunewing").Level, 2), win, "the same boss pays less at a higher trainer level");
        }
    }
}
