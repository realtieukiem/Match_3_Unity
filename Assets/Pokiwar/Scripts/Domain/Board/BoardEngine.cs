using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    /// <summary>Tunable board rules. Everything marked PROVISIONAL is a playtest baseline, not a verified Pokiwar rule.</summary>
    [Serializable]
    public class BoardRuleProfile
    {
        public string Id = "board.default";
        public int Width = 8;
        public int Height = 8;
        public int MinMatch = 3;
        public float[] SpawnWeights = { 1.5f, 1f, 1f, 1f, 1.1f, 0.9f };
        public float[] InitialSpawnWeights = { 0.2f, 1f, 1f, 1f, 1f, 1f };
        public float X2SpawnChance = 0.05f;
        public float X3SpawnChance = 0.015f;
        public bool MultipliersOnInitialFill = true;
        public bool InvalidSwapReverts = true;
        public bool ReshuffleWhenNoMoves = true;
        public bool ExtraTurnOnMatch4;
        public bool ExtraTurnOnMatch5;
        public int MaxCascadeSteps = 40;
        public float TurnSeconds = 10f;

        public Confidence MultiplierSpawnConfidence = Confidence.Provisional;
        public Confidence InvalidSwapConfidence = Confidence.Provisional;
        public Confidence DeadBoardConfidence = Confidence.Provisional;
        public Confidence ExtraTurnConfidence = Confidence.Provisional;
        public Confidence TurnSecondsConfidence = Confidence.Video;
    }

    public interface IGemSpawner
    {
        Cell Spawn(BoardState board, Pos at);
    }

    public sealed class RandomGemSpawner : IGemSpawner
    {
        private readonly BoardRuleProfile rules;
        private readonly SeededRng rng;

        public RandomGemSpawner(BoardRuleProfile rules, SeededRng rng)
        {
            this.rules = rules;
            this.rng = rng;
        }

        public Cell Spawn(BoardState board, Pos at)
        {
            var type = (GemType)rng.PickWeighted(rules.SpawnWeights);
            int mult = 1;
            double r = rng.NextDouble();
            if (r < rules.X3SpawnChance) mult = 3;
            else if (r < rules.X3SpawnChance + rules.X2SpawnChance) mult = 2;
            return board.NewCell(type, mult);
        }
    }

    /// <summary>Deterministic spawner that never completes a line with its neighbours. Used by tests and by AI look-ahead.</summary>
    public sealed class SafeCycleSpawner : IGemSpawner
    {
        private int cursor;

        public Cell Spawn(BoardState board, Pos at)
        {
            for (int k = 0; k < Gems.Count; k++)
            {
                var t = (GemType)((cursor + k) % Gems.Count);
                if (!WouldMatch(board, at, t))
                {
                    cursor = (cursor + k + 1) % Gems.Count;
                    return board.NewCell(t);
                }
            }
            cursor = (cursor + 1) % Gems.Count;
            return board.NewCell((GemType)cursor);
        }

        private static bool WouldMatch(BoardState b, Pos p, GemType t)
        {
            int h = 1 + Count(b, p, -1, 0, t) + Count(b, p, 1, 0, t);
            int v = 1 + Count(b, p, 0, -1, t) + Count(b, p, 0, 1, t);
            return h >= 3 || v >= 3;
        }

        private static int Count(BoardState b, Pos p, int dx, int dy, GemType t)
        {
            int n = 0;
            int x = p.X + dx, y = p.Y + dy;
            while (x >= 0 && y >= 0 && x < b.Width && y < b.Height && b[x, y].Type == t)
            {
                n++;
                x += dx;
                y += dy;
            }
            return n;
        }
    }

    public sealed class MatchRun
    {
        public GemType Type;
        public bool Horizontal;
        public readonly List<Pos> Cells = new List<Pos>();
        public int PhysicalLength => Cells.Count;
    }

    public struct SwapMove
    {
        public Pos A;
        public Pos B;

        public SwapMove(Pos a, Pos b)
        {
            A = a;
            B = b;
        }

        public override string ToString() => A + "<->" + B;
    }

    [Serializable]
    public sealed class GemTally
    {
        public int[] Physical = new int[Gems.Count];
        public int[] Effective = new int[Gems.Count];

        public void Add(Cell c)
        {
            if (c.IsEmpty) return;
            Physical[(int)c.Type] += 1;
            Effective[(int)c.Type] += Math.Max(1, c.Multiplier);
        }

        public void Merge(GemTally o)
        {
            for (int i = 0; i < Gems.Count; i++)
            {
                Physical[i] += o.Physical[i];
                Effective[i] += o.Effective[i];
            }
        }

        public int PhysicalOf(GemType t) => t == GemType.None ? 0 : Physical[(int)t];
        public int EffectiveOf(GemType t) => t == GemType.None ? 0 : Effective[(int)t];

        public int TotalEffective
        {
            get
            {
                int s = 0;
                for (int i = 0; i < Gems.Count; i++) s += Effective[i];
                return s;
            }
        }

        public bool IsEmpty => TotalEffective == 0;

        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var t in Gems.ResolveOrder)
            {
                int p = PhysicalOf(t);
                if (p == 0) continue;
                int e = EffectiveOf(t);
                parts.Add(t + " " + (e == p ? e.ToString() : e + "(phys " + p + ")"));
            }
            return parts.Count == 0 ? "-" : string.Join(", ", parts);
        }
    }

    public struct ClearedCell
    {
        public Pos At;
        public Cell Cell;
    }

    public struct FallMove
    {
        public int Id;
        public Pos From;
        public Pos To;
    }

    public struct SpawnedCell
    {
        public Pos At;
        public Cell Cell;
        public int StartY;
    }

    public sealed class CascadeStep
    {
        public readonly List<MatchRun> Runs = new List<MatchRun>();
        public readonly List<ClearedCell> Cleared = new List<ClearedCell>();
        public readonly List<FallMove> Falls = new List<FallMove>();
        public readonly List<SpawnedCell> Spawns = new List<SpawnedCell>();
        public readonly GemTally Tally = new GemTally();
    }

    public sealed class SwapResolution
    {
        public bool Valid;
        public SwapMove Move;
        public readonly List<CascadeStep> Steps = new List<CascadeStep>();
        public readonly GemTally Tally = new GemTally();
        public int LongestRun;
        public bool Reshuffled;
        public BoardState FinalBoard;
    }

    public static class BoardEngine
    {
        public static void FillInitial(BoardState b, BoardRuleProfile rules, SeededRng rng)
        {
            var weights = rules.InitialSpawnWeights != null && rules.InitialSpawnWeights.Length == rules.SpawnWeights.Length ? rules.InitialSpawnWeights : rules.SpawnWeights;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    for (int x = 0; x < b.Width; x++)
                    {
                        GemType t;
                        int guard = 0;
                        do
                        {
                            t = (GemType)rng.PickWeighted(weights);
                            guard++;
                        } while (guard < 50 && CreatesInitialRun(b, x, y, t));
                        int mult = 1;
                        if (rules.MultipliersOnInitialFill)
                        {
                            double r = rng.NextDouble();
                            if (r < rules.X3SpawnChance) mult = 3;
                            else if (r < rules.X3SpawnChance + rules.X2SpawnChance) mult = 2;
                        }
                        b[x, y] = b.NewCell(t, mult);
                    }
                }
                if (FindRuns(b, rules.MinMatch).Count == 0 && HasMove(b, rules.MinMatch)) return;
            }
        }

        private static bool CreatesInitialRun(BoardState b, int x, int y, GemType t)
        {
            if (x >= 2 && b[x - 1, y].Type == t && b[x - 2, y].Type == t) return true;
            if (y >= 2 && b[x, y - 1].Type == t && b[x, y - 2].Type == t) return true;
            return false;
        }

        public static void SwapCells(BoardState b, Pos a, Pos c)
        {
            var t = b[a];
            b[a] = b[c];
            b[c] = t;
        }

        public static List<MatchRun> FindRuns(BoardState b, int minMatch)
        {
            var runs = new List<MatchRun>();
            for (int y = 0; y < b.Height; y++)
            {
                int x = 0;
                while (x < b.Width)
                {
                    var t = b[x, y].Type;
                    int end = x + 1;
                    while (end < b.Width && t != GemType.None && b[end, y].Type == t) end++;
                    if (t != GemType.None && end - x >= minMatch)
                    {
                        var run = new MatchRun { Type = t, Horizontal = true };
                        for (int i = x; i < end; i++) run.Cells.Add(new Pos(i, y));
                        runs.Add(run);
                    }
                    x = end;
                }
            }
            for (int x = 0; x < b.Width; x++)
            {
                int y = 0;
                while (y < b.Height)
                {
                    var t = b[x, y].Type;
                    int end = y + 1;
                    while (end < b.Height && t != GemType.None && b[x, end].Type == t) end++;
                    if (t != GemType.None && end - y >= minMatch)
                    {
                        var run = new MatchRun { Type = t, Horizontal = false };
                        for (int i = y; i < end; i++) run.Cells.Add(new Pos(x, i));
                        runs.Add(run);
                    }
                    y = end;
                }
            }
            return runs;
        }

        public static bool HasMatchAt(BoardState b, Pos p, int minMatch)
        {
            var t = b[p].Type;
            if (t == GemType.None) return false;
            int h = 1, v = 1;
            for (int x = p.X - 1; x >= 0 && b[x, p.Y].Type == t; x--) h++;
            for (int x = p.X + 1; x < b.Width && b[x, p.Y].Type == t; x++) h++;
            for (int y = p.Y - 1; y >= 0 && b[p.X, y].Type == t; y--) v++;
            for (int y = p.Y + 1; y < b.Height && b[p.X, y].Type == t; y++) v++;
            return h >= minMatch || v >= minMatch;
        }

        public static bool IsMatchingSwap(BoardState b, Pos a, Pos c, int minMatch)
        {
            if (!b.InBounds(a) || !b.InBounds(c) || !a.IsAdjacent(c)) return false;
            if (b[a].Type == b[c].Type) return false;
            SwapCells(b, a, c);
            bool ok = HasMatchAt(b, a, minMatch) || HasMatchAt(b, c, minMatch);
            SwapCells(b, a, c);
            return ok;
        }

        public static List<SwapMove> FindMoves(BoardState b, int minMatch)
        {
            var moves = new List<SwapMove>();
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var p = new Pos(x, y);
                    if (x + 1 < b.Width && IsMatchingSwap(b, p, new Pos(x + 1, y), minMatch)) moves.Add(new SwapMove(p, new Pos(x + 1, y)));
                    if (y + 1 < b.Height && IsMatchingSwap(b, p, new Pos(x, y + 1), minMatch)) moves.Add(new SwapMove(p, new Pos(x, y + 1)));
                }
            }
            return moves;
        }

        public static bool HasMove(BoardState b, int minMatch)
        {
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var p = new Pos(x, y);
                    if (x + 1 < b.Width && IsMatchingSwap(b, p, new Pos(x + 1, y), minMatch)) return true;
                    if (y + 1 < b.Height && IsMatchingSwap(b, p, new Pos(x, y + 1), minMatch)) return true;
                }
            }
            return false;
        }

        /// <summary>Applies a swap, clears every cascade until stable and returns the whole turn's tally. Board is mutated in place.</summary>
        public static SwapResolution ResolveSwap(BoardState b, Pos a, Pos c, BoardRuleProfile rules, IGemSpawner spawner, SeededRng rng)
        {
            var res = new SwapResolution { Move = new SwapMove(a, c) };
            if (!b.InBounds(a) || !b.InBounds(c) || !a.IsAdjacent(c))
            {
                res.FinalBoard = b;
                return res;
            }
            SwapCells(b, a, c);
            var runs = FindRuns(b, rules.MinMatch);
            if (runs.Count == 0)
            {
                if (rules.InvalidSwapReverts) SwapCells(b, a, c);
                res.FinalBoard = b;
                return res;
            }
            res.Valid = true;
            int guard = 0;
            while (runs.Count > 0 && guard++ < rules.MaxCascadeSteps)
            {
                res.Steps.Add(ApplyStep(b, runs, spawner, res));
                runs = FindRuns(b, rules.MinMatch);
            }
            if (rules.ReshuffleWhenNoMoves && !HasMove(b, rules.MinMatch))
            {
                Reshuffle(b, rules, rng);
                res.Reshuffled = true;
            }
            res.FinalBoard = b;
            return res;
        }

        private static CascadeStep ApplyStep(BoardState b, List<MatchRun> runs, IGemSpawner spawner, SwapResolution res)
        {
            var step = new CascadeStep();
            step.Runs.AddRange(runs);
            var seen = new HashSet<Pos>();
            foreach (var run in runs)
            {
                if (run.PhysicalLength > res.LongestRun) res.LongestRun = run.PhysicalLength;
                foreach (var p in run.Cells)
                {
                    if (!seen.Add(p)) continue;
                    var cell = b[p];
                    step.Cleared.Add(new ClearedCell { At = p, Cell = cell });
                    step.Tally.Add(cell);
                }
            }
            foreach (var cc in step.Cleared) b[cc.At] = Cell.Empty;
            res.Tally.Merge(step.Tally);

            for (int x = 0; x < b.Width; x++)
            {
                int write = 0;
                for (int y = 0; y < b.Height; y++)
                {
                    var cell = b[x, y];
                    if (cell.IsEmpty) continue;
                    if (y != write)
                    {
                        b[x, write] = cell;
                        b[x, y] = Cell.Empty;
                        step.Falls.Add(new FallMove { Id = cell.Id, From = new Pos(x, y), To = new Pos(x, write) });
                    }
                    write++;
                }
                int missing = b.Height - write;
                for (int y = write; y < b.Height; y++)
                {
                    var p = new Pos(x, y);
                    var cell = spawner.Spawn(b, p);
                    b[p] = cell;
                    step.Spawns.Add(new SpawnedCell { At = p, Cell = cell, StartY = y + missing });
                }
            }
            return step;
        }

        public static void Reshuffle(BoardState b, BoardRuleProfile rules, SeededRng rng)
        {
            var pool = new List<Cell>();
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                    pool.Add(b[x, y]);
            for (int attempt = 0; attempt < 200; attempt++)
            {
                rng.Shuffle(pool);
                int i = 0;
                for (int y = 0; y < b.Height; y++)
                    for (int x = 0; x < b.Width; x++)
                        b[x, y] = pool[i++];
                if (FindRuns(b, rules.MinMatch).Count == 0 && HasMove(b, rules.MinMatch)) return;
            }
            FillInitial(b, rules, rng);
        }

        /// <summary>Look-ahead used by the AI: resolves a swap on a copy where refills are unknown (left empty), so cascades only count gems already visible on the board.</summary>
        public static SwapResolution SimulateKnown(BoardState source, SwapMove move, int minMatch)
        {
            var b = source.Clone();
            var res = new SwapResolution { Move = move };
            if (!IsMatchingSwap(b, move.A, move.B, minMatch))
            {
                res.FinalBoard = b;
                return res;
            }
            SwapCells(b, move.A, move.B);
            res.Valid = true;
            var unknown = new UnknownSpawner();
            var runs = FindRuns(b, minMatch);
            int guard = 0;
            while (runs.Count > 0 && guard++ < 20)
            {
                res.Steps.Add(ApplyStep(b, runs, unknown, res));
                runs = FindRuns(b, minMatch);
            }
            res.FinalBoard = b;
            return res;
        }

        private sealed class UnknownSpawner : IGemSpawner
        {
            public Cell Spawn(BoardState board, Pos at) => Cell.Empty;
        }
    }
}
