using System.Collections.Generic;

namespace Pokiwar.Domain
{
    /// <summary>Seed content for the vertical slice. The editor seeder writes it into ScriptableObjects; tests use it directly.</summary>
    public static class DefaultContent
    {
        public static ContentDatabase Create()
        {
            var db = new ContentDatabase();
            AddGemProfiles(db);
            AddCards(db);
            AddSkills(db);
            AddCreatures(db);
            AddAi(db);
            AddRewards(db);
            AddEncounters(db);
            AddMap(db);
            db.Progression = new ProgressionConfig
            {
                StartGold = 500,
                StarterPetIds = new List<string> { "pet.emberkit", "pet.leafling" },
                StarterPetLevels = new List<int> { 3, 2 },
                StarterCardIds = new List<string> { "card.mana_potion", "card.herbal_salve", "card.fire_bolt", "card.summon_sprite", "card.iron_skin" },
                StarterSkillIds = new List<string> { "skill.blaze_burst", "skill.thorn_bind" },
                StarterItems = new List<RewardDrop>
                {
                    new RewardDrop { Kind = RewardKind.CardStone, Tier = 1, Count = 3 },
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Fire, Tier = 1, Count = 4 },
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Wood, Tier = 1, Count = 2 },
                    new RewardDrop { Kind = RewardKind.LuckyCharm, Count = 1 },
                    new RewardDrop { Kind = RewardKind.ProtectionCharm, Count = 1 }
                },
                DefaultPlayerName = "Trainer",
                StarterAvatarIds = new List<string> { "avatar.hair.spiky", "avatar.top.tee", "avatar.bottom.shorts" }
            };
            AddAvatarItems(db);
            return db;
        }

        private static void AddAvatarItems(ContentDatabase db)
        {
            void A(string id, string name, AvatarSlot slot, int price) =>
                db.AvatarItems.Add(new AvatarItemDef { Id = id, Name = name, Slot = slot, Price = price, SpriteKey = id });
            A("avatar.hair.spiky", "Spiky Hair", AvatarSlot.Hair, 0);
            A("avatar.hair.bob", "Bob Cut", AvatarSlot.Hair, 300);
            A("avatar.hair.ponytail", "Ponytail", AvatarSlot.Hair, 300);
            A("avatar.top.tee", "Canyon Tee", AvatarSlot.Top, 0);
            A("avatar.top.jacket", "Explorer Jacket", AvatarSlot.Top, 400);
            A("avatar.top.robe", "Mage Robe", AvatarSlot.Top, 600);
            A("avatar.bottom.shorts", "Shorts", AvatarSlot.Bottom, 0);
            A("avatar.bottom.pants", "Cargo Pants", AvatarSlot.Bottom, 300);
            A("avatar.bottom.skirt", "Pleated Skirt", AvatarSlot.Bottom, 300);
            A("avatar.hat.cap", "Trainer Cap", AvatarSlot.Hat, 250);
            A("avatar.hat.wizard", "Wizard Hat", AvatarSlot.Hat, 500);
        }

        private static void AddGemProfiles(ContentDatabase db)
        {
            db.GemProfiles.Add(new GemEffectProfile
            {
                Id = "gem.legacy_low",
                Confidence = Confidence.Inferred,
                HeartPctMaxHpPerGem = 0.0145f,
                LightningPctMaxManaPerGem = 0.0625f,
                FireRagePerGem = 6,
                ShieldPctMaxHpPerGem = 0.012f,
                YinYangManaPctOfTargetMaxPerGem = 0.02f,
                YinYangRagePerGem = 5,
                SwordAtkPerGem = 0.75f
            });
            db.GemProfiles.Add(new GemEffectProfile
            {
                Id = "gem.balanced",
                Confidence = Confidence.Provisional,
                HeartPctMaxHpPerGem = 0.03f,
                LightningPctMaxManaPerGem = 0.031f,
                FireRagePerGem = 6,
                ShieldPctMaxHpPerGem = 0.015f,
                YinYangManaPctOfTargetMaxPerGem = 0.025f,
                YinYangRagePerGem = 6,
                SwordAtkPerGem = 0.75f
            });
            db.GemProfiles.Add(new GemEffectProfile
            {
                Id = "gem.monster",
                Confidence = Confidence.Provisional,
                HeartPctMaxHpPerGem = 0.022f,
                LightningPctMaxManaPerGem = 0.03f,
                FireRagePerGem = 5,
                ShieldPctMaxHpPerGem = 0.012f,
                SwordAtkPerGem = 0.7f
            });
            db.GemProfiles.Add(new GemEffectProfile
            {
                Id = "gem.boss",
                Confidence = Confidence.Provisional,
                HeartPctMaxHpPerGem = 0.012f,
                LightningPctMaxManaPerGem = 0.013f,
                FireRagePerGem = 8,
                ShieldPctMaxHpPerGem = 0.01f,
                YinYangManaPctOfTargetMaxPerGem = 0.02f,
                YinYangRagePerGem = 8,
                SwordAtkPerGem = 0.65f
            });
        }

        private static CardDef Card(string id, string name, string desc, int mana, int rage, params EffectSpec[] fx)
        {
            var c = new CardDef { Id = id, Name = name, Description = desc, IconKey = id, ManaCost = mana, RageCost = rage };
            c.Effects.AddRange(fx);
            return c;
        }

        private static void AddCards(ContentDatabase db)
        {
            db.Cards.Add(Card("card.mana_potion", "Mana Potion", "+100 mana. You can still match this turn.", 0, 0,
                new EffectSpec(EffectKind.AddMana, TargetKind.Self, 100)));
            db.Cards.Add(Card("card.herbal_salve", "Herbal Salve", "Heal 15% max HP.", 120, 0,
                new EffectSpec(EffectKind.HealPctMax, TargetKind.Self, 0.15f)));
            db.Cards.Add(Card("card.fire_bolt", "Fire Bolt", "Deal 150% ATK damage.", 200, 0,
                new EffectSpec(EffectKind.AtkDamage, TargetKind.Opponent, 1.5f)));
            db.Cards.Add(Card("card.summon_sprite", "Summon Sprite", "Sprite hits for 35% ATK on your next 3 turns.", 250, 0,
                new EffectSpec(EffectKind.Summon, TargetKind.Self, 0.35f, 3, "Sprite")));
            db.Cards.Add(Card("card.iron_skin", "Iron Skin", "Gain a shield of 12% max HP.", 0, 40,
                new EffectSpec(EffectKind.AddShieldPctMax, TargetKind.Self, 0.12f)));
            db.Cards.Add(Card("card.mana_leech", "Mana Leech", "Drain 30% of the enemy's current mana.", 0, 30,
                new EffectSpec(EffectKind.DrainManaPctOfCurrent, TargetKind.Opponent, 0.30f)));
            db.Cards.Add(Card("card.war_cry", "War Cry", "+25% ATK for 2 turns.", 0, 50,
                new EffectSpec(EffectKind.BuffAtk, TargetKind.Self, 0.25f, 2, "War Cry")));
            var finisher = Card("card.meteor", "Meteor", "Deal 300% ATK damage. Ends your turn.", 450, 0,
                new EffectSpec(EffectKind.AtkDamage, TargetKind.Opponent, 3.0f));
            finisher.EndTurnAfterUse = true;
            db.Cards.Add(finisher);
        }

        private static void AddSkills(ContentDatabase db)
        {
            db.Skills.Add(new SkillDef { Id = "skill.blaze_burst", Name = "Blaze Burst", Description = "Arrow combo fire blast.", IconKey = "skill.blaze_burst", ManaCost = 400, AtkMultiplier = 4.0f, FlatPerLevel = 70 });
            var thorn = new SkillDef { Id = "skill.thorn_bind", Name = "Thorn Bind", Description = "Arrow combo, then -20% enemy ATK for 2 turns.", IconKey = "skill.thorn_bind", ManaCost = 350, AtkMultiplier = 3.0f, FlatPerLevel = 50 };
            thorn.PostEffects.Add(new EffectSpec(EffectKind.DebuffAtk, TargetKind.Opponent, 0.2f, 2, "Thorn Bind"));
            db.Skills.Add(thorn);
            db.Skills.Add(new SkillDef { Id = "skill.tide_lance", Name = "Tide Lance", Description = "Arrow combo water strike.", IconKey = "skill.tide_lance", ManaCost = 380, AtkMultiplier = 3.8f, FlatPerLevel = 65 });
            db.Skills.Add(new SkillDef { Id = "skill.mind_spark", Name = "Mind Spark", Description = "Arrow combo psychic jolt.", IconKey = "skill.mind_spark", ManaCost = 450, AtkMultiplier = 3.0f, FlatPerLevel = 55 });
            var siphon = new SkillDef
            {
                Id = "skill.tidal_siphon", Name = "Tidal Siphon", Description = "Costs 200 mana + 200 rage. Strikes, then drains 70% of the target's mana.",
                IconKey = "skill.tidal_siphon", ManaCost = 200, RageCost = 200, AtkMultiplier = 3.2f, FlatPerLevel = 60
            };
            siphon.PostEffects.Add(new EffectSpec(EffectKind.DrainManaPctOfCurrent, TargetKind.Opponent, 0.70f));
            db.Skills.Add(siphon);
        }

        private static void AddCreatures(ContentDatabase db)
        {
            db.Creatures.Add(new CreatureDef
            {
                Id = "pet.emberkit", Name = "Emberkit", SpriteKey = "emberkit", Element = Element.Fire, ElementBonus = 2,
                BaseStats = new StatBlock(1800, 120, 20, 1000, 100), PerLevel = new StatBlock(140, 10, 2, 70, 0),
                GemProfileId = "gem.legacy_low", SkillIds = { "skill.blaze_burst" }
            });
            db.Creatures.Add(new CreatureDef
            {
                Id = "pet.leafling", Name = "Leafling", SpriteKey = "leafling", Element = Element.Wood, ElementBonus = 2,
                BaseStats = new StatBlock(2100, 100, 26, 1100, 100), PerLevel = new StatBlock(160, 8, 3, 80, 0),
                GemProfileId = "gem.balanced", SkillIds = { "skill.thorn_bind" }
            });
            db.Creatures.Add(new CreatureDef
            {
                Id = "pet.tidepup", Name = "Tidepup", SpriteKey = "tidepup", Element = Element.Water, ElementBonus = 3,
                BaseStats = new StatBlock(2000, 115, 22, 1200, 200), PerLevel = new StatBlock(150, 10, 2, 80, 0),
                GemProfileId = "gem.balanced", SkillIds = { "skill.tide_lance" },
                Rage = new RageProfile { AttackThreshold = 100, AttackCost = 100 }
            });
            db.Creatures.Add(new CreatureDef
            {
                Id = "mon.dunewing", Name = "Dunewing", SpriteKey = "dunewing", Element = Element.Earth, ElementBonus = 1,
                BaseStats = new StatBlock(2800, 150, 20, 800, 100), PerLevel = new StatBlock(130, 8, 2, 50, 0),
                GemProfileId = "gem.monster", CardIds = { "card.herbal_salve" }
            });
            db.Creatures.Add(new CreatureDef
            {
                Id = "mon.psyling", Name = "Psyling", SpriteKey = "psyling", Element = Element.Metal, ElementBonus = 1,
                BaseStats = new StatBlock(3800, 175, 24, 1000, 100), PerLevel = new StatBlock(140, 8, 2, 60, 0),
                GemProfileId = "gem.monster", SkillIds = { "skill.mind_spark" }, CardIds = { "card.mana_leech" }
            });
            var boss = new CreatureDef
            {
                Id = "boss.azurewing", Name = "Azurewing", SpriteKey = "azurewing", Element = Element.Water, ElementBonus = 2,
                BaseStats = new StatBlock(4850, 90, 22, 1600, 200), PerLevel = new StatBlock(180, 5, 2, 60, 0),
                GemProfileId = "gem.boss", CardIds = { "card.iron_skin", "card.war_cry" },
                Rage = new RageProfile { AttackThreshold = 100, AttackCost = 100, StrongMultiplier = 1.7f }
            };
            boss.Phases.Add(new BossPhaseDef
            {
                Id = "phase.azurewing.ascend", FormName = "Azurewing Ascended", SpriteKey = "azurewing_ascended",
                TriggerValue = 0.32f, OneShot = true, TriggerOnLethal = true, SetHpPctOfMax = 0.5f, AtkMultiplier = 1.15f,
                UnlockSkillIds = { "skill.tidal_siphon" }, ContinueTurn = true
            });
            db.Creatures.Add(boss);
        }

        private static void AddAi(ContentDatabase db)
        {
            db.AiPolicies.Add(new AIPolicy { Id = "ai.easy", Temperature = 0.8f, TopK = 5, MistakeChance = 0.2f, OpponentOpportunityWeight = 0f, QteAvgCorrect = 2.5f, QtePerfectChance = 0.1f, QteGoodChance = 0.4f });
            db.AiPolicies.Add(new AIPolicy { Id = "ai.normal" });
            db.AiPolicies.Add(new AIPolicy { Id = "ai.boss", Temperature = 0.2f, TopK = 2, MistakeChance = 0.04f, OpponentOpportunityWeight = 0.5f, QteAvgCorrect = 4f, QtePerfectChance = 0.35f, QteGoodChance = 0.5f });
            db.AiPolicies.Add(new AIPolicy { Id = "ai.autoplay", Temperature = 0.3f, TopK = 2, MistakeChance = 0.02f, QteAvgCorrect = 4.5f, QtePerfectChance = 0.4f, QteGoodChance = 0.5f });
        }

        private static void AddRewards(ContentDatabase db)
        {
            db.RewardTables.Add(new RewardTable
            {
                Id = "rw.dunewing", PlayerExp = 60, Gold = 150,
                Drops =
                {
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Earth, Tier = 1, Count = 2 },
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Fire, Tier = 1, Count = 1, Chance = 0.6f },
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Wood, Tier = 1, Count = 1, Chance = 0.6f },
                    new RewardDrop { Kind = RewardKind.CardStone, Tier = 1, Count = 2 },
                    new RewardDrop { Kind = RewardKind.LuckyCharm, Count = 1, Chance = 0.3f }
                }
            });
            db.RewardTables.Add(new RewardTable
            {
                Id = "rw.psyling", PlayerExp = 90, Gold = 220,
                Drops =
                {
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Metal, Tier = 1, Count = 3 },
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Fire, Tier = 1, Count = 1, Chance = 0.6f },
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Wood, Tier = 1, Count = 1, Chance = 0.6f },
                    new RewardDrop { Kind = RewardKind.CardStone, Tier = 1, Count = 3 },
                    new RewardDrop { Kind = RewardKind.SkillCard, ItemId = "skill.mind_spark", FirstClearOnly = true },
                    new RewardDrop { Kind = RewardKind.Card, ItemId = "card.mana_leech", FirstClearOnly = true },
                    new RewardDrop { Kind = RewardKind.ProtectionCharm, Count = 1, Chance = 0.5f }
                }
            });
            db.RewardTables.Add(new RewardTable
            {
                Id = "rw.azurewing", PlayerExp = 200, Gold = 600,
                Drops =
                {
                    new RewardDrop { Kind = RewardKind.Stone, Element = Element.Water, Tier = 2, Count = 2 },
                    new RewardDrop { Kind = RewardKind.CardStone, Tier = 2, Count = 2 },
                    new RewardDrop { Kind = RewardKind.SkillCard, ItemId = "skill.tide_lance", FirstClearOnly = true },
                    new RewardDrop { Kind = RewardKind.Card, ItemId = "card.war_cry", FirstClearOnly = true },
                    new RewardDrop { Kind = RewardKind.Card, ItemId = "card.meteor", FirstClearOnly = true }
                }
            });
        }

        private static void AddEncounters(ContentDatabase db)
        {
            db.Encounters.Add(new EncounterDef { Id = "enc.dunewing", Name = "Dunewing", CreatureId = "mon.dunewing", Level = 3, AiPolicyId = "ai.normal", RewardTableId = "rw.dunewing", EnergyCost = 1, Difficulty = 1, ManaVsPlayer = 1f });
            db.Encounters.Add(new EncounterDef { Id = "enc.psyling", Name = "Psyling", CreatureId = "mon.psyling", Level = 6, AiPolicyId = "ai.normal", RewardTableId = "rw.psyling", EnergyCost = 2, Difficulty = 2, SkillLevel = 2, ManaVsPlayer = 1f });
            db.Encounters.Add(new EncounterDef { Id = "enc.azurewing", Name = "Azurewing (Boss)", CreatureId = "boss.azurewing", Level = 8, AiPolicyId = "ai.boss", RewardTableId = "rw.azurewing", IsBoss = true, EnergyCost = 3, Difficulty = 4, ManaVsPlayer = 1.6f, SkillLevel = 3 });
        }

        private static void AddMap(ContentDatabase db)
        {
            db.Map.Regions.Add(new RegionDef { Id = "region.sunny", Name = "Sunny Isle" });
            db.Map.Nodes.Add(new MapNodeDef { Id = "node.1", Name = "Dune Beach", RegionId = "region.sunny", EncounterId = "enc.dunewing", X = 0.18f, Y = 0.30f, WinsRequired = 1 });
            db.Map.Nodes.Add(new MapNodeDef { Id = "node.2", Name = "Echo Cave", RegionId = "region.sunny", EncounterId = "enc.psyling", RequiresNodeId = "node.1", X = 0.48f, Y = 0.62f, WinsRequired = 2 });
            db.Map.Nodes.Add(new MapNodeDef { Id = "node.3", Name = "Azure Peak", RegionId = "region.sunny", EncounterId = "enc.azurewing", RequiresNodeId = "node.2", X = 0.80f, Y = 0.40f, WinsRequired = 3 });
        }
    }
}
