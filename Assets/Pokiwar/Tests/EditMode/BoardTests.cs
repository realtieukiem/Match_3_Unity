using System.Collections.Generic;
using NUnit.Framework;
using Pokiwar.Domain;

namespace Pokiwar.Tests
{
    public class BoardTests
    {
        [Test]
        public void Multiplier_RidesWithCell_And_EffectiveCountDiffersFromPhysical()
        {
            var b = TestKit.CascadeBoard(GemType.Heart, middleMultiplier: 2);
            var res = BoardEngine.ResolveSwap(b, new Pos(0, 0), new Pos(0, 1), new BoardRuleProfile { ReshuffleWhenNoMoves = false }, new SafeCycleSpawner(), new SeededRng(1));

            Assert.IsTrue(res.Valid);
            var first = res.Steps[0].Tally;
            Assert.AreEqual(3, first.PhysicalOf(GemType.Heart), "one physical line of three");
            Assert.AreEqual(4, first.EffectiveOf(GemType.Heart), "x2 counts double: 1 + 2 + 1");
            Assert.AreEqual(3, res.Steps[0].Runs[0].PhysicalLength, "a count of 4 is not a match-4");
        }

        [Test]
        public void Cascade_ResolvesUntilStable_AndTalliesEveryStep()
        {
            var b = TestKit.CascadeBoard(GemType.Heart);
            var res = BoardEngine.ResolveSwap(b, new Pos(0, 0), new Pos(0, 1), new BoardRuleProfile { ReshuffleWhenNoMoves = false }, new SafeCycleSpawner(), new SeededRng(1));

            Assert.GreaterOrEqual(res.Steps.Count, 2, "gravity should line up the Fire triple");
            Assert.AreEqual(3, res.Steps[1].Tally.PhysicalOf(GemType.Fire));
            Assert.AreEqual(3, res.Tally.PhysicalOf(GemType.Heart));
            Assert.GreaterOrEqual(res.Tally.PhysicalOf(GemType.Fire), 3);
            Assert.That(BoardEngine.FindRuns(b, 3), Is.Empty, "board is stable after resolve");
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    Assert.IsFalse(b[x, y].IsEmpty, "board fully refilled");
        }

        [Test]
        public void FallingCellsKeepTheirId()
        {
            var b = TestKit.CascadeBoard(GemType.Heart);
            int idAbove = b[1, 2].Id;
            var res = BoardEngine.ResolveSwap(b, new Pos(0, 0), new Pos(0, 1), new BoardRuleProfile { ReshuffleWhenNoMoves = false }, new SafeCycleSpawner(), new SeededRng(1));
            var fall = res.Steps[0].Falls.Find(f => f.Id == idAbove);
            Assert.AreEqual(new Pos(1, 2), fall.From);
            Assert.AreEqual(new Pos(1, 1), fall.To);
        }

        [Test]
        public void InvalidSwap_IsRevertedAndChangesNothing()
        {
            var b = TestKit.BaseBoard();
            var before = b.Clone();
            var res = BoardEngine.ResolveSwap(b, new Pos(3, 3), new Pos(4, 3), new BoardRuleProfile(), new SafeCycleSpawner(), new SeededRng(1));
            Assert.IsFalse(res.Valid);
            Assert.IsTrue(b.SameLayout(before));
        }

        [Test]
        public void InitialFill_HasNoMatches_AndHasAMove()
        {
            for (uint seed = 1; seed < 30; seed++)
            {
                var b = new BoardState(8, 8);
                BoardEngine.FillInitial(b, new BoardRuleProfile(), new SeededRng(seed));
                Assert.That(BoardEngine.FindRuns(b, 3), Is.Empty);
                Assert.IsTrue(BoardEngine.HasMove(b, 3));
            }
        }

        [Test]
        public void DeadBoard_IsReshuffled_KeepingTheSameGems()
        {
            var b = BoardState.Parse(
                "SLFHSLFH",
                "FHSLFHSL",
                "SLFHSLFH",
                "FHSLFHSL",
                "SLFHSLFH",
                "FHSLFHSL",
                "SLFHSLFH",
                "FHSLFHSL");
            Assert.IsFalse(BoardEngine.HasMove(b, 3), "fixture must be dead");
            var ids = new List<int>();
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) ids.Add(b[x, y].Id);

            BoardEngine.Reshuffle(b, new BoardRuleProfile(), new SeededRng(5));

            Assert.That(BoardEngine.FindRuns(b, 3), Is.Empty);
            Assert.IsTrue(BoardEngine.HasMove(b, 3));
            var after = new List<int>();
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) after.Add(b[x, y].Id);
            CollectionAssert.AreEquivalent(ids, after);
        }

        [Test]
        public void AiLookAhead_DoesNotSeeRefills()
        {
            var b = TestKit.CascadeBoard(GemType.Heart);
            var sim = BoardEngine.SimulateKnown(b, new SwapMove(new Pos(0, 0), new Pos(0, 1)), 3);
            Assert.IsTrue(sim.Valid);
            int empty = 0;
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (sim.FinalBoard[x, y].IsEmpty) empty++;
            Assert.Greater(empty, 0, "refilled cells stay unknown");
            Assert.AreEqual(GemType.Lightning, b[0, 0].Type, "source board untouched");
        }
    }
}
