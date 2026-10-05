using NUnit.Framework;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Tests
{
    public class AvatarTests
    {
        private sealed class UnitySerializer : ISaveSerializer
        {
            public string ToJson(SaveData d) => JsonUtility.ToJson(d);
            public SaveData FromJson(string json) => JsonUtility.FromJson<SaveData>(json);
        }

        [Test]
        public void NewSave_WearsStarterOutfit()
        {
            var db = DefaultContent.Create();
            var save = new ProgressionService(db).CreateNewSave(0);
            Assert.AreEqual("Trainer", save.PlayerName);
            CollectionAssert.AreEquivalent(db.Progression.StarterAvatarIds, save.AvatarWorn);
            CollectionAssert.AreEquivalent(db.Progression.StarterAvatarIds, save.AvatarOwned);
        }

        [Test]
        public void Buy_SpendsGold_AndWearsItInPlaceOfTheSameSlot()
        {
            var db = DefaultContent.Create();
            var save = new ProgressionService(db).CreateNewSave(0);
            var av = new AvatarService(db);
            int gold = save.Gold;
            var r = av.Buy(save, "avatar.hair.bob");
            Assert.IsTrue(r.Ok, r.Message);
            Assert.AreEqual(gold - db.TryAvatarItem("avatar.hair.bob").Price, save.Gold);
            Assert.AreEqual("avatar.hair.bob", av.WornIn(save, AvatarSlot.Hair));
            Assert.IsFalse(av.IsWorn(save, "avatar.hair.spiky"));
            Assert.IsTrue(av.Owns(save, "avatar.hair.spiky"));
            Assert.IsFalse(av.Buy(save, "avatar.hair.bob").Ok);
        }

        [Test]
        public void Buy_WithoutEnoughGold_ChangesNothing()
        {
            var db = DefaultContent.Create();
            var save = new ProgressionService(db).CreateNewSave(0);
            save.Gold = 10;
            var r = new AvatarService(db).Buy(save, "avatar.top.robe");
            Assert.IsFalse(r.Ok);
            Assert.AreEqual(10, save.Gold);
            Assert.IsFalse(save.AvatarOwned.Contains("avatar.top.robe"));
        }

        [Test]
        public void Equip_NeedsOwnership_TakeOffEmptiesTheSlot()
        {
            var db = DefaultContent.Create();
            var save = new ProgressionService(db).CreateNewSave(0);
            var av = new AvatarService(db);
            Assert.IsFalse(av.Equip(save, "avatar.hat.cap").Ok);
            save.Gold = 1000;
            av.Buy(save, "avatar.hat.cap");
            Assert.AreEqual("avatar.hat.cap", av.WornIn(save, AvatarSlot.Hat));
            Assert.IsTrue(av.TakeOff(save, "avatar.hat.cap").Ok);
            Assert.IsNull(av.WornIn(save, AvatarSlot.Hat));
            Assert.IsTrue(av.Equip(save, "avatar.hat.cap").Ok);
            Assert.AreEqual(4, av.Look(save).ItemIds.Count);
        }

        [Test]
        public void Rename_RejectsTooShortOrTooLong()
        {
            var db = DefaultContent.Create();
            var save = new ProgressionService(db).CreateNewSave(0);
            var av = new AvatarService(db);
            Assert.IsFalse(av.Rename(save, "ab").Ok);
            Assert.IsFalse(av.Rename(save, new string('x', AvatarService.MaxNameLength + 1)).Ok);
            Assert.IsTrue(av.Rename(save, "  Phuong  ").Ok);
            Assert.AreEqual("Phuong", save.PlayerName);
        }

        [Test]
        public void Version2Save_GetsStarterOutfitOnLoad()
        {
            var db = DefaultContent.Create();
            var store = new MemorySaveStore { Json = "{\"Version\":2,\"Gold\":90,\"Energy\":5,\"Pets\":[{\"Uid\":\"p1\",\"PetId\":\"pet.emberkit\",\"Level\":2}]}" };
            var loaded = new SaveService(store, new UnitySerializer(), new ProgressionService(db)).LoadOrCreate();
            Assert.AreEqual(SaveData.CurrentVersion, loaded.Version);
            Assert.AreEqual(90, loaded.Gold);
            Assert.AreEqual("Trainer", loaded.PlayerName);
            CollectionAssert.AreEquivalent(db.Progression.StarterAvatarIds, loaded.AvatarWorn);
        }
    }
}
