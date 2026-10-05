using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pokiwar.Domain;

namespace Pokiwar.Tests
{
    internal static class TestKit
    {
        private const string Letters = "SLFHDY";

        /// <summary>Type at (x, y) is (x + 2y) mod 6: no row or column ever holds three in a line.</summary>
        public static BoardState BaseBoard()
        {
            var b = new BoardState(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    b[x, y] = b.NewCell(BoardState.FromChar(Letters[(x + 2 * y) % 6]));
            return b;
        }

        public static void Put(BoardState b, int x, int y, GemType t, int mult = 1) => b[x, y] = b.NewCell(t, mult);

        public static void AssertStable(BoardState b) =>
            Assert.That(BoardEngine.FindRuns(b, 3), Is.Empty, "test board must start without matches:\n" + b);

        /// <summary>Swapping (0,0) with (0,1) makes a row-0 triple of `first`; the gravity that follows lines up a Fire triple.</summary>
        public static BoardState CascadeBoard(GemType first, int middleMultiplier = 1)
        {
            var b = BaseBoard();
            Put(b, 0, 0, GemType.Lightning);
            Put(b, 1, 0, first, middleMultiplier);
            Put(b, 2, 0, first);
            Put(b, 3, 0, GemType.Fire);
            Put(b, 0, 1, first);
            Put(b, 1, 1, GemType.Fire);
            Put(b, 2, 1, GemType.Fire);
            AssertStable(b);
            return b;
        }

        public static CreatureDef Creature(string id, int hp, int atk, int mana, int rage, Element element = Element.Neutral)
        {
            return new CreatureDef
            {
                Id = id,
                Name = id,
                SpriteKey = id,
                Element = element,
                BaseStats = new StatBlock(hp, atk, 0, mana, rage),
                PerLevel = new StatBlock(),
                Rage = new RageProfile { RageOnOwnTurnStart = 0 }
            };
        }

        public static GemEffectProfile Gems() => new GemEffectProfile
        {
            HeartPctMaxHpPerGem = 0.01f,
            LightningPctMaxManaPerGem = 0.01f,
            FireRagePerGem = 6,
            ShieldPctMaxHpPerGem = 0.01f,
            ShieldCapPctMaxHp = 0.5f,
            YinYangManaPctOfTargetMaxPerGem = 0.02f,
            YinYangRagePerGem = 5,
            YinYangManaChance = 1f,
            SwordAtkPerGem = 0.6f
        };

        public static CombatantSetup Setup(CreatureDef c, Action<CombatantSetup> tweak = null)
        {
            var s = new CombatantSetup { Creature = c, Level = 1, Gems = Gems() };
            tweak?.Invoke(s);
            return s;
        }

        public static BattleEngine Engine(BoardState board = null, Action<CombatantSetup> player = null, Action<CombatantSetup> enemy = null,
            Side first = Side.Player, Action<BoardRuleProfile> rules = null, CreatureDef playerDef = null, CreatureDef enemyDef = null)
        {
            var br = new BoardRuleProfile { ReshuffleWhenNoMoves = false };
            rules?.Invoke(br);
            return new BattleEngine(new BattleSetup
            {
                BattleId = "test",
                Seed = 777,
                Player = Setup(playerDef ?? Creature("hero", 1000, 100, 1000, 100), player),
                Enemy = Setup(enemyDef ?? Creature("foe", 1000, 100, 1000, 100), enemy),
                Board = br,
                FirstTurn = first,
                InitialBoard = board ?? BaseBoard(),
                SpawnerOverride = new SafeCycleSpawner()
            });
        }

        public static CardDef Card(string id, int mana, int rage, params EffectSpec[] fx)
        {
            var c = new CardDef { Id = id, Name = id, ManaCost = mana, RageCost = rage, UsesPerBattle = 9 };
            c.Effects.AddRange(fx);
            return c;
        }

        public static List<CombatEvent> Of(this ActionResult r, CombatEventKind k) => r.Events.FindAll(e => e.Kind == k);
    }
}
