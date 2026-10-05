using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public sealed class AIDecision
    {
        public readonly List<int> CardsBeforeMatch = new List<int>();
        public int SkillIndex = -1;
        public bool HasSwap;
        public SwapMove Swap;
        public float Score;
        public string Reason;
    }

    /// <summary>Chooses an action from what is visible on the shared board right now.</summary>
    public static class AIPlanner
    {
        public static AIDecision Decide(BattleEngine engine, Side me, AIPolicy policy, SeededRng rng)
        {
            policy = policy ?? new AIPolicy();
            var s = engine.State;
            var self = s.Get(me);
            var foe = s.Opponent(me);
            var d = new AIDecision();

            if (policy.UseCards)
            {
                for (int i = 0; i < self.Cards.Count; i++)
                {
                    if (engine.CardBlockReason(me, i) != null) continue;
                    if (self.Cards[i].Def.EndTurnAfterUse) continue;
                    if (CardWorthIt(self, foe, self.Cards[i].Def)) d.CardsBeforeMatch.Add(i);
                }
            }

            if (policy.UseSkills)
            {
                int best = -1;
                int bestDmg = 0;
                for (int i = 0; i < self.Skills.Count; i++)
                {
                    if (SkillAffordable(self, self.Skills[i], d, s))
                    {
                        int dmg = BattleEngine.SkillBase(self, self.Skills[i]);
                        if (best < 0 || dmg > bestDmg)
                        {
                            best = i;
                            bestDmg = dmg;
                        }
                    }
                }
                if (best >= 0)
                {
                    d.SkillIndex = best;
                    d.Reason = "skill " + self.Skills[best].Name;
                    return d;
                }
            }

            var moves = BoardEngine.FindMoves(s.Board, engine.BoardRules.MinMatch);
            if (moves.Count == 0)
            {
                d.Reason = "no moves";
                return d;
            }
            var scored = new List<(SwapMove move, float score)>();
            foreach (var m in moves)
            {
                var sim = BoardEngine.SimulateKnown(s.Board, m, engine.BoardRules.MinMatch);
                scored.Add((m, Score(sim, self, foe, policy)));
            }
            scored.Sort((a, b) => b.score.CompareTo(a.score));

            int consider = Math.Min(scored.Count, Math.Max(4, policy.TopK * 2));
            if (policy.OpponentOpportunityWeight > 0)
            {
                for (int i = 0; i < consider; i++)
                {
                    var sim = BoardEngine.SimulateKnown(s.Board, scored[i].move, engine.BoardRules.MinMatch);
                    float leave = BestOpponentGain(sim.FinalBoard, engine.BoardRules.MinMatch);
                    scored[i] = (scored[i].move, scored[i].score - leave * policy.OpponentOpportunityWeight);
                }
                scored.Sort(0, consider, Comparer<(SwapMove, float)>.Create((a, b) => b.Item2.CompareTo(a.Item2)));
            }

            int pick = 0;
            if (rng.Chance(policy.MistakeChance)) pick = rng.Next(scored.Count);
            else
            {
                int k = Math.Min(Math.Max(1, policy.TopK), scored.Count);
                var w = new float[k];
                float top = scored[0].score;
                for (int i = 0; i < k; i++) w[i] = (float)Math.Exp((scored[i].score - top) / Math.Max(0.01f, policy.Temperature));
                pick = rng.PickWeighted(w);
            }
            d.HasSwap = true;
            d.Swap = scored[pick].move;
            d.Score = scored[pick].score;
            d.Reason = "swap " + d.Swap + " score " + d.Score.ToString("0.00");
            return d;
        }

        public static QteResult RollQte(AIPolicy policy, QteProfile qte, SeededRng rng)
        {
            policy = policy ?? new AIPolicy();
            int correct = 0;
            double p = MathUtil.Clamp(policy.QteAvgCorrect / Math.Max(1, qte.ArrowCount), 0, 1);
            for (int i = 0; i < qte.ArrowCount; i++) if (rng.Chance(p)) correct++;
            double r = rng.NextDouble();
            var timing = r < policy.QtePerfectChance ? QteTiming.Perfect : r < policy.QtePerfectChance + policy.QteGoodChance ? QteTiming.Good : QteTiming.Miss;
            return new QteResult(correct, timing);
        }

        private static bool SkillAffordable(Combatant self, SkillDef sk, AIDecision d, BattleState s)
        {
            int mana = self.Mana.Current;
            int rage = self.Rage.Current;
            foreach (var i in d.CardsBeforeMatch)
            {
                mana -= self.Cards[i].Def.ManaCost;
                rage -= self.Cards[i].Def.RageCost;
                foreach (var e in self.Cards[i].Def.Effects)
                    if (e.Kind == EffectKind.AddMana && e.Target == TargetKind.Self) mana = Math.Min(self.Mana.Max, mana + (int)e.Value);
            }
            return mana >= sk.ManaCost && rage >= sk.RageCost && !s.HasSwappedThisTurn;
        }

        private static bool CardWorthIt(Combatant self, Combatant foe, CardDef card)
        {
            foreach (var e in card.Effects)
            {
                switch (e.Kind)
                {
                    case EffectKind.HealFlat:
                    case EffectKind.HealPctMax:
                        if (e.Target == TargetKind.Self && self.Hp.Ratio < 0.55f) return true;
                        break;
                    case EffectKind.AddMana:
                        if (e.Target == TargetKind.Self && self.Mana.Room >= e.Value * 0.8f) return true;
                        break;
                    case EffectKind.AddShieldPctMax:
                        if (self.Shield.Ratio < 0.3f && self.Hp.Ratio < 0.8f) return true;
                        break;
                    case EffectKind.FlatDamage:
                    case EffectKind.AtkDamage:
                    case EffectKind.DrainManaPctOfCurrent:
                    case EffectKind.DrainRage:
                    case EffectKind.DebuffAtk:
                        return true;
                    case EffectKind.Summon:
                        if (self.Summons.Count == 0) return true;
                        break;
                    case EffectKind.BuffAtk:
                        if (self.Buffs.Count == 0) return true;
                        break;
                }
            }
            return false;
        }

        private static float Score(SwapResolution sim, Combatant self, Combatant foe, AIPolicy p)
        {
            var t = sim.Tally;
            float hpNeed = 0.3f + 1.7f * (1f - self.Hp.Ratio);
            float manaNeed = NextSkillManaNeed(self);
            float rageNeed = self.Rage.Current >= self.RageRules.AttackThreshold ? 0.25f : 1f;
            float threat = foe.Rage.Current >= foe.RageRules.AttackThreshold ? 1.3f : 0.6f;
            float swordValue = self.Rage.Current >= self.RageRules.AttackThreshold ? 1.6f : 1f;
            if (foe.Hp.Ratio < 0.25f) swordValue *= 1.5f;
            float stealValue = 0.4f + 0.6f * Math.Max(foe.Mana.Ratio, foe.Rage.Ratio);

            float score = 0;
            score += t.EffectiveOf(GemType.Sword) * p.SwordWeight * swordValue;
            score += t.EffectiveOf(GemType.Heart) * p.HeartWeight * hpNeed;
            score += t.EffectiveOf(GemType.Lightning) * p.LightningWeight * manaNeed;
            score += t.EffectiveOf(GemType.Fire) * p.FireWeight * rageNeed;
            score += t.EffectiveOf(GemType.Shield) * p.ShieldWeight * threat * (1f - self.Shield.Ratio);
            score += t.EffectiveOf(GemType.YinYang) * p.YinYangWeight * stealValue;
            score += (sim.Steps.Count - 1) * p.CascadeWeight;
            return score;
        }

        private static float NextSkillManaNeed(Combatant self)
        {
            if (self.Skills.Count == 0) return self.Mana.IsFull ? 0.1f : 0.5f;
            int cost = int.MaxValue;
            foreach (var s in self.Skills) cost = Math.Min(cost, s.ManaCost);
            if (self.Mana.Current >= cost) return 0.3f;
            return 1.2f;
        }

        private static float BestOpponentGain(BoardState b, int minMatch)
        {
            float best = 0;
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var p = new Pos(x, y);
                    if (x + 1 < b.Width) best = Math.Max(best, Gain(b, p, new Pos(x + 1, y), minMatch));
                    if (y + 1 < b.Height) best = Math.Max(best, Gain(b, p, new Pos(x, y + 1), minMatch));
                }
            }
            return best;
        }

        private static float Gain(BoardState b, Pos a, Pos c, int minMatch)
        {
            if (b[a].IsEmpty || b[c].IsEmpty) return 0;
            if (!BoardEngine.IsMatchingSwap(b, a, c, minMatch)) return 0;
            BoardEngine.SwapCells(b, a, c);
            int n = 0;
            foreach (var run in BoardEngine.FindRuns(b, minMatch))
                foreach (var p in run.Cells)
                    n += b[p].Multiplier;
            BoardEngine.SwapCells(b, a, c);
            return n;
        }
    }
}
