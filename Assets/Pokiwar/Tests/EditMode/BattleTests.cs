using NUnit.Framework;
using Pokiwar.Domain;

namespace Pokiwar.Tests
{
    public class BattleTests
    {
        private static readonly Pos A = new Pos(0, 0);
        private static readonly Pos B = new Pos(0, 1);

        [Test]
        public void SharedBoard_PlayerAndAiActOnTheSameBoardAcrossTurns()
        {
            var e = TestKit.Engine(TestKit.CascadeBoard(GemType.Heart), rules: r => r.ReshuffleWhenNoMoves = true);
            var board = e.State.Board;
            e.BeginTurn();
            Assert.IsTrue(e.Swap(Side.Player, A, B).Accepted);
            var afterPlayer = board.Clone();
            e.EndTurn();
            e.BeginTurn();

            Assert.AreEqual(Side.Enemy, e.State.Current);
            Assert.AreSame(board, e.State.Board, "no per-side board");
            Assert.IsTrue(board.SameLayout(afterPlayer), "AI starts from exactly what the player left");

            var d = AIPlanner.Decide(e, Side.Enemy, new AIPolicy { UseCards = false, UseSkills = false, MistakeChance = 0 }, new SeededRng(3));
            Assert.IsTrue(d.HasSwap);
            Assert.IsTrue(e.Swap(Side.Enemy, d.Swap.A, d.Swap.B).Accepted, "AI goes through the same Swap call");
            Assert.IsFalse(board.SameLayout(afterPlayer));
        }

        [Test]
        public void EffectsResolveInFixedOrder_HeartLightningFireYinYangShieldSword()
        {
            var e = TestKit.Engine();
            e.BeginTurn();
            var tally = new GemTally();
            foreach (var t in new[] { GemType.Sword, GemType.Shield, GemType.YinYang, GemType.Fire, GemType.Lightning, GemType.Heart })
                for (int i = 0; i < 3; i++) tally.Add(new Cell(t, 1, 1));
            var r = e.ApplyGemTally(Side.Player, tally);

            var order = new System.Collections.Generic.List<GemType>();
            foreach (var ev in r.Events)
                if (ev.Gem != GemType.None && !order.Contains(ev.Gem)) order.Add(ev.Gem);
            CollectionAssert.AreEqual(Gems.ResolveOrder, order);
        }

        [Test]
        public void FireBeforeSword_TurnsThatSwordIntoARageStrike()
        {
            var e = TestKit.Engine(TestKit.CascadeBoard(GemType.Sword), player: s => s.StartRage = 95, playerDef: TestKit.Creature("hero", 1000, 100, 1000, 200));
            e.BeginTurn();
            var r = e.Swap(Side.Player, A, B);
            Assert.IsTrue(r.Accepted);

            int fireIndex = r.Events.FindIndex(ev => ev.Gem == GemType.Fire);
            var hit = r.Events.Find(ev => ev.Kind == CombatEventKind.Damage);
            Assert.Greater(fireIndex, -1);
            Assert.Less(fireIndex, r.Events.IndexOf(hit), "Fire resolves before Sword");
            Assert.IsTrue(hit.Strong, "95 + Fire rage crossed the 100 threshold within the same turn");

            int fireRage = 6 * r.Board.Tally.EffectiveOf(GemType.Fire);
            int swords = r.Board.Tally.EffectiveOf(GemType.Sword);
            Assert.AreEqual((int)System.Math.Floor(100 * 0.6 * swords * 1.7 + 1e-6), hit.Computed);
            Assert.AreEqual(95 + fireRage - 100, e.State.Get(Side.Player).Rage.Current, "a strike spends the cost, it does not reset the bar");
        }

        [Test]
        public void RageStrike_On200Bar_SpendsCostNotWholeBar()
        {
            var e = TestKit.Engine(player: s => s.StartRage = 200, playerDef: TestKit.Creature("hero", 1000, 100, 1000, 200));
            e.BeginTurn();
            var tally = new GemTally();
            for (int i = 0; i < 3; i++) tally.Add(new Cell(GemType.Sword, 1, 1));
            e.ApplyGemTally(Side.Player, tally);
            Assert.AreEqual(100, e.State.Get(Side.Player).Rage.Current);
        }

        [Test]
        public void Timeout_OnlySkipsTheTurn()
        {
            var e = TestKit.Engine();
            e.BeginTurn();
            var board = e.State.Board.Clone();
            var p = e.State.Get(Side.Player).Snapshot();
            var en = e.State.Get(Side.Enemy).Snapshot();

            var r = e.Timeout(Side.Player);

            Assert.IsTrue(r.Accepted && r.EndsTurn);
            Assert.AreEqual(1, r.Events.Count);
            Assert.AreEqual(CombatEventKind.TurnSkipped, r.Events[0].Kind);
            Assert.IsNull(r.Board, "no auto move");
            Assert.IsTrue(e.State.Board.SameLayout(board));
            Assert.AreEqual(p.ToString(), e.State.Get(Side.Player).Snapshot().ToString());
            Assert.AreEqual(en.ToString(), e.State.Get(Side.Enemy).Snapshot().ToString());
            e.EndTurn();
            Assert.AreEqual(Side.Enemy, e.State.Current);
        }

        [Test]
        public void TurnClock_ExpiresOnce_AndFreezesWhileResolving()
        {
            var c = new TurnClock(10f);
            c.Restart();
            Assert.IsFalse(c.Tick(4f));
            c.Freeze();
            Assert.IsFalse(c.Tick(100f), "accepted action: resolution may outlast the deadline");
            c.Resume();
            Assert.IsFalse(c.Tick(5f));
            Assert.IsTrue(c.Tick(1.5f));
            Assert.IsFalse(c.Tick(1f));
        }

        [Test]
        public void ManaCard_DoesNotEndTheTurn_PlayerCanStillMatch()
        {
            var potion = TestKit.Card("potion", 0, 0, new EffectSpec(EffectKind.AddMana, TargetKind.Self, 100));
            var e = TestKit.Engine(TestKit.CascadeBoard(GemType.Heart), player: s => { s.Cards.Add(potion); s.StartMana = 0; });
            e.BeginTurn();
            var r = e.UseCard(Side.Player, 0);
            Assert.IsTrue(r.Accepted);
            Assert.IsFalse(r.EndsTurn);
            Assert.AreEqual(100, e.State.Get(Side.Player).Mana.Current);
            Assert.IsTrue(e.IsTurnOf(Side.Player));
            Assert.IsTrue(e.Swap(Side.Player, A, B).Accepted);
        }

        [Test]
        public void YinYangSteal_TakesAtMostWhatTargetHas_ReceiverCapped()
        {
            var e = TestKit.Engine(player: s => s.StartMana = 990, enemy: s => s.StartMana = 30);
            e.BeginTurn();
            var tally = new GemTally();
            for (int i = 0; i < 5; i++) tally.Add(new Cell(GemType.YinYang, 1, 1));
            var r = e.ApplyGemTally(Side.Player, tally);
            var steal = r.Events.Find(ev => ev.Kind == CombatEventKind.Steal);

            Assert.AreEqual(ResourceKind.Mana, steal.Resource);
            Assert.AreEqual(100, steal.Computed, "2% of 1000 max x5");
            Assert.AreEqual(30, steal.Applied);
            Assert.AreEqual(0, e.State.Get(Side.Enemy).Mana.Current);
            Assert.AreEqual(1000, e.State.Get(Side.Player).Mana.Current);
            Assert.AreEqual(1000, e.State.Get(Side.Player).Hp.Current, "YinYang never touches HP");
        }

        [Test]
        public void Shield_AbsorbsBeforeHp_AndHpDamageFeedsRage()
        {
            var e = TestKit.Engine(enemy: s => s.StartRage = 0);
            e.BeginTurn();
            e.State.Get(Side.Enemy).Shield.Set(50);
            var tally = new GemTally();
            for (int i = 0; i < 5; i++) tally.Add(new Cell(GemType.Sword, 1, 1));
            var r = e.ApplyGemTally(Side.Player, tally);
            var hit = r.Events.Find(ev => ev.Kind == CombatEventKind.Damage);

            Assert.AreEqual(300, hit.Computed, "label before shield");
            Assert.AreEqual(50, hit.ShieldAbsorbed);
            Assert.AreEqual(250, hit.Applied, "HP damage after shield");
            Assert.AreEqual(25, e.State.Get(Side.Enemy).Rage.Current, "floor(100 x 250 / 1000)");
        }

        [Test]
        public void ElementRelation_ChangesRealHpDamage()
        {
            var bolt = TestKit.Card("bolt", 0, 0, new EffectSpec(EffectKind.FlatDamage, TargetKind.Opponent, 400));
            int Hit(Element defender)
            {
                var e = TestKit.Engine(player: s => s.Cards.Add(bolt),
                    playerDef: TestKit.Creature("hero", 1000, 100, 1000, 100, Element.Fire),
                    enemyDef: TestKit.Creature("foe", 1000, 100, 1000, 100, defender));
                e.BeginTurn();
                return e.UseCard(Side.Player, 0).Events.Find(ev => ev.Kind == CombatEventKind.Damage).Applied;
            }
            Assert.AreEqual(500, Hit(Element.Metal), "Fire beats Metal");
            Assert.AreEqual(320, Hit(Element.Water), "Water beats Fire");
            Assert.AreEqual(400, Hit(Element.Earth));
        }

        [Test]
        public void CheckValuesFromTheClip_QteSteps()
        {
            var q = new QteProfile();
            int[] expected = { 2836, 3403, 3970, 4537, 5104, 5671 };
            for (int i = 0; i <= 5; i++) Assert.AreEqual(expected[i], QteMath.Stepped(2836, q, i));
            Assert.AreEqual(7032, QteMath.Apply(2836, q, new QteResult(5, QteTiming.Good)));
            Assert.AreEqual(7089, QteMath.Apply(2836, q, new QteResult(5, QteTiming.Perfect)));
        }

        [Test]
        public void SkillCostThenDrain_MatchesTheClip()
        {
            var siphon = new SkillDef { Id = "siphon", Name = "siphon", ManaCost = 200, RageCost = 200, AtkMultiplier = 0, UseQte = false };
            siphon.PostEffects.Add(new EffectSpec(EffectKind.DrainManaPctOfCurrent, TargetKind.Opponent, 0.70f));
            var e = TestKit.Engine(
                player: s => { s.Skills.Add(siphon); s.StartMana = 893; s.StartRage = 200; },
                enemy: s => s.StartMana = 548,
                playerDef: TestKit.Creature("hero", 1000, 100, 2410, 200),
                enemyDef: TestKit.Creature("foe", 1000, 100, 3856, 200));
            e.BeginTurn();
            var r = e.UseSkill(Side.Player, 0, QteResult.None);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(165, e.State.Get(Side.Enemy).Mana.Current);
            Assert.AreEqual(1076, e.State.Get(Side.Player).Mana.Current);
            Assert.AreEqual(0, e.State.Get(Side.Player).Rage.Current);
        }

        private static CreatureDef Boss()
        {
            var b = TestKit.Creature("boss", 1000, 100, 1000, 200);
            b.Phases.Add(new BossPhaseDef { Id = "p2", FormName = "Boss II", SpriteKey = "boss2", TriggerValue = 0.32f, SetHpPctOfMax = 0.5f, AtkMultiplier = 1.2f, UnlockSkillIds = { "ult" }, ContinueTurn = true });
            return b;
        }

        [Test]
        public void BossPhase_FiresOnce_AndSetsHpToHalf()
        {
            var nuke = TestKit.Card("nuke", 0, 0, new EffectSpec(EffectKind.FlatDamage, TargetKind.Opponent, 200));
            var ult = new SkillDef { Id = "ult", Name = "ult", ManaCost = 0, AtkMultiplier = 1 };
            var e = TestKit.Engine(player: s => s.Cards.Add(nuke), enemy: s => { s.StartHpPct = 0.4f; s.LockedSkills.Add(ult); }, enemyDef: Boss());
            e.BeginTurn();
            var boss = e.State.Get(Side.Enemy);

            var r1 = e.UseCard(Side.Player, 0);
            Assert.AreEqual(1, r1.Of(CombatEventKind.PhaseTriggered).Count);
            Assert.AreEqual(500, boss.Hp.Current, "400 - 200 = 200 <= 32%, then set to 50% of 1000");
            Assert.AreEqual("Boss II", boss.FormName);
            Assert.AreEqual("boss2", boss.SpriteKey);
            Assert.AreEqual(1, boss.Skills.Count, "phase unlocked its action");
            Assert.AreEqual(Side.Enemy, e.State.ContinueTurnFor);

            e.UseCard(Side.Player, 0);
            var r3 = e.UseCard(Side.Player, 0);
            Assert.AreEqual(0, r3.Of(CombatEventKind.PhaseTriggered).Count, "one-shot");
            Assert.AreEqual(100, boss.Hp.Current);
        }

        [Test]
        public void BossPhase_LethalHitTransformsInsteadOfKilling()
        {
            var nuke = TestKit.Card("nuke", 0, 0, new EffectSpec(EffectKind.FlatDamage, TargetKind.Opponent, 5000));
            var e = TestKit.Engine(player: s => s.Cards.Add(nuke), enemyDef: Boss());
            e.BeginTurn();
            var r = e.UseCard(Side.Player, 0);
            Assert.IsFalse(r.BattleEnded);
            Assert.AreEqual(500, e.State.Get(Side.Enemy).Hp.Current);
            var r2 = e.UseCard(Side.Player, 0);
            Assert.IsTrue(r2.BattleEnded);
            Assert.AreEqual(Side.Player, r2.Winner);
        }

        [Test]
        public void ContinueTurn_BossActsRightAfterTransform_OverridingAnExtraTurn()
        {
            var nuke = TestKit.Card("nuke", 0, 0, new EffectSpec(EffectKind.FlatDamage, TargetKind.Opponent, 300));
            var e = TestKit.Engine(enemyDef: Boss(), enemy: s => s.StartHpPct = 0.4f, player: s => s.Cards.Add(nuke));
            e.BeginTurn();
            Assert.AreEqual(1, e.UseCard(Side.Player, 0).Of(CombatEventKind.PhaseTriggered).Count);
            e.State.ExtraTurnPending = true;
            e.EndTurn();
            Assert.AreEqual(Side.Enemy, e.State.Current);
            Assert.IsNull(e.State.ContinueTurnFor, "consumed once");
        }
        [Test]
        public void AiVsAi_SameSeedReplaysIdentically()
        {
            string Run()
            {
                var db = DefaultContent.Create();
                var prog = new ProgressionService(db);
                var save = prog.CreateNewSave(0);
                var setup = prog.StartBattle(save, db.Node("node.1"), 4242);
                var e = new BattleEngine(setup);
                var rng = new SeededRng(99);
                int guard = 0;
                while (!e.State.Ended && guard++ < 300)
                {
                    e.BeginTurn();
                    if (e.State.Ended) break;
                    var side = e.State.Current;
                    var policy = e.State.Get(side).Ai;
                    var d = AIPlanner.Decide(e, side, policy, rng);
                    foreach (var c in d.CardsBeforeMatch) e.UseCard(side, c);
                    bool acted = false;
                    if (d.SkillIndex >= 0 && e.PrepareSkill(side, d.SkillIndex).Ok)
                        acted = e.UseSkill(side, d.SkillIndex, AIPlanner.RollQte(policy, e.State.Get(side).Skills[d.SkillIndex].Qte, rng)).Accepted;
                    if (!acted && !e.State.Ended)
                    {
                        var d2 = AIPlanner.Decide(e, side, new AIPolicy { UseCards = false, UseSkills = false }, rng);
                        if (d2.HasSwap) e.Swap(side, d2.Swap.A, d2.Swap.B);
                        else e.Timeout(side);
                    }
                    e.EndTurn();
                }
                Assert.IsTrue(e.State.Ended, "battle must finish");
                return e.State.Winner + "/" + e.State.TurnNumber + "/" + e.State.Log.Count + "/" + e.State.Board;
            }
            Assert.AreEqual(Run(), Run());
        }
    }
}
