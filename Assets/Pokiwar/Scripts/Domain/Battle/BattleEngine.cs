using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    public enum QteDir { Up, Down, Left, Right }

    public enum QteTiming { Miss, Good, Perfect }

    public struct QteResult
    {
        public int Correct;
        public QteTiming Timing;

        public QteResult(int correct, QteTiming timing)
        {
            Correct = correct;
            Timing = timing;
        }

        public static QteResult None => new QteResult(0, QteTiming.Miss);
    }

    public static class QteMath
    {
        /// <summary>Manaphy calibration: base 2836, each correct arrow adds floor(20% of base) (3403 -> ...</summary>
        public static int Apply(int baseDamage, QteProfile p, QteResult r)
        {
            int correct = MathUtil.Clamp(r.Correct, 0, p.ArrowCount);
            int step = (int)Math.Floor(baseDamage * p.PerCorrectBonus + 1e-4);
            int stepped = baseDamage + correct * step;
            return MathUtil.Round(stepped * TimingMultiplier(p, r.Timing), RoundingMode.Round);
        }

        public static int Stepped(int baseDamage, QteProfile p, int correct) =>
            baseDamage + MathUtil.Clamp(correct, 0, p.ArrowCount) * (int)Math.Floor(baseDamage * p.PerCorrectBonus + 1e-4);

        public static float TimingMultiplier(QteProfile p, QteTiming t) =>
            t == QteTiming.Perfect ? p.PerfectMultiplier : t == QteTiming.Good ? p.GoodMultiplier : p.MissMultiplier;

        public static QteTiming Judge(QteProfile p, float distanceFromCenter)
        {
            float d = Math.Abs(distanceFromCenter);
            if (d <= p.PerfectHalfWidth) return QteTiming.Perfect;
            if (d <= p.GoodHalfWidth) return QteTiming.Good;
            return QteTiming.Miss;
        }
    }

    public sealed class SkillCheck
    {
        public bool Ok;
        public string Reason;
        public SkillDef Skill;
        public int PreviewBase;
        public readonly List<QteDir> Sequence = new List<QteDir>();
    }

    public sealed class BattleSetup
    {
        public string BattleId = "battle";
        public uint Seed = 12345;
        public CombatantSetup Player;
        public CombatantSetup Enemy;
        public BoardRuleProfile Board = new BoardRuleProfile();
        public BattleRules Rules = new BattleRules();
        public Side FirstTurn = Side.Player;
        public BoardState InitialBoard;
        public IGemSpawner SpawnerOverride;
    }

    public sealed class BattleState
    {
        public string BattleId;
        public BoardState Board;
        public readonly Combatant[] Actors = new Combatant[2];
        public Side Current;
        public int TurnNumber;
        public bool TurnOpen;
        public bool HasSwappedThisTurn;
        public int CardsUsedThisTurn;
        public bool ExtraTurnPending;
        public Side? ContinueTurnFor;
        public bool Ended;
        public Side Winner;
        public readonly List<CombatEvent> Log = new List<CombatEvent>();

        public Combatant Get(Side s) => Actors[(int)s];
        public Combatant Opponent(Side s) => Actors[(int)s.Other()];
    }

    /// <summary>Pure turn-based battle on one shared board.</summary>
    public sealed class BattleEngine
    {
        public readonly BattleState State;
        public readonly BoardRuleProfile BoardRules;
        public readonly BattleRules Rules;
        private readonly SeededRng rng;
        private readonly SeededRng boardRng;
        private readonly IGemSpawner spawner;

        public BattleEngine(BattleSetup setup)
        {
            BoardRules = setup.Board ?? new BoardRuleProfile();
            Rules = setup.Rules ?? new BattleRules();
            var root = new SeededRng(setup.Seed);
            rng = root.Fork(1);
            boardRng = root.Fork(2);
            spawner = setup.SpawnerOverride ?? new RandomGemSpawner(BoardRules, boardRng);
            State = new BattleState { BattleId = setup.BattleId, Current = setup.FirstTurn };
            State.Actors[0] = Combatant.Create(Side.Player, setup.Player);
            State.Actors[1] = Combatant.Create(Side.Enemy, setup.Enemy);
            if (setup.InitialBoard != null) State.Board = setup.InitialBoard;
            else
            {
                State.Board = new BoardState(BoardRules.Width, BoardRules.Height);
                BoardEngine.FillInitial(State.Board, BoardRules, boardRng);
            }
        }

        public bool IsTurnOf(Side s) => !State.Ended && State.TurnOpen && State.Current == s;

        public ActionResult BeginTurn()
        {
            var r = new ActionResult { Kind = ActionKind.TurnStart, Actor = State.Current, Accepted = !State.Ended };
            if (State.Ended) return r;
            State.TurnNumber++;
            State.TurnOpen = true;
            State.HasSwappedThisTurn = false;
            State.CardsUsedThisTurn = 0;
            State.ExtraTurnPending = false;
            var me = State.Get(State.Current);
            var foe = State.Opponent(State.Current);
            Emit(r, new CombatEvent { Kind = CombatEventKind.TurnStart, Actor = me.Side, Target = me.Side, Text = "Turn " + State.TurnNumber });
            if (Rules.ShieldExpiresOnOwnTurnStart && me.Shield.Current > 0)
                ChangeResource(r, me, me, ResourceKind.Shield, -me.Shield.Current, "shield-expire", null);
            if (me.RageRules.RageOnOwnTurnStart != 0)
                ChangeResource(r, me, me, ResourceKind.Rage, me.RageRules.RageOnOwnTurnStart, "turn-start", null);
            for (int i = me.Summons.Count - 1; i >= 0; i--)
            {
                var s = me.Summons[i];
                var hit = new CombatEvent { Kind = CombatEventKind.SummonHit, Actor = me.Side, Target = foe.Side, SourceId = s.Name, Text = s.Name };
                Emit(r, hit);
                DealDamage(r, me, foe, s.DamagePerTurn, "summon:" + s.Name, false, false);
                s.TurnsLeft--;
                if (s.TurnsLeft <= 0) me.Summons.RemoveAt(i);
                if (foe.IsDead) break;
            }
            FinishAction(r);
            return r;
        }

        public ActionResult Swap(Side side, Pos a, Pos b)
        {
            if (!IsTurnOf(side)) return ActionResult.Reject(ActionKind.Swap, side, "not your turn");
            if (State.HasSwappedThisTurn) return ActionResult.Reject(ActionKind.Swap, side, "already matched this turn");
            var res = BoardEngine.ResolveSwap(State.Board, a, b, BoardRules, spawner, boardRng);
            if (!res.Valid)
            {
                var rej = ActionResult.Reject(ActionKind.Swap, side, "no match");
                rej.Board = res;
                return rej;
            }
            var r = new ActionResult { Kind = ActionKind.Swap, Actor = side, Accepted = true, Board = res };
            State.HasSwappedThisTurn = true;
            Emit(r, new CombatEvent { Kind = CombatEventKind.BoardResolved, Actor = side, Target = side, Text = res.Steps.Count + " cascade step(s)" });
            Emit(r, new CombatEvent { Kind = CombatEventKind.GemSummary, Actor = side, Target = side, Text = res.Tally.ToString() });
            ResolveGems(r, State.Get(side), State.Opponent(side), res.Tally);
            if (res.Reshuffled) Emit(r, new CombatEvent { Kind = CombatEventKind.Reshuffled, Actor = side, Target = side, Text = "No moves left, board reshuffled" });
            bool extra = (BoardRules.ExtraTurnOnMatch5 && res.LongestRun >= 5) || (BoardRules.ExtraTurnOnMatch4 && res.LongestRun >= 4);
            State.ExtraTurnPending = extra;
            r.EndsTurn = true;
            FinishAction(r);
            return r;
        }

        public ActionResult UseCard(Side side, int index)
        {
            if (!IsTurnOf(side)) return ActionResult.Reject(ActionKind.Card, side, "not your turn");
            var me = State.Get(side);
            if (index < 0 || index >= me.Cards.Count) return ActionResult.Reject(ActionKind.Card, side, "no card");
            var reason = CardBlockReason(side, index);
            if (reason != null) return ActionResult.Reject(ActionKind.Card, side, reason);
            var slot = me.Cards[index];
            var def = slot.Def;
            var r = new ActionResult { Kind = ActionKind.Card, Actor = side, Accepted = true };
            slot.UsesLeft--;
            State.CardsUsedThisTurn++;
            var ev = new CombatEvent { Kind = CombatEventKind.CardUsed, Actor = side, Target = side, SourceId = def.Id, Text = def.Name };
            Emit(r, ev);
            if (def.ManaCost > 0) ChangeResource(r, me, me, ResourceKind.Mana, -def.ManaCost, "cost:" + def.Id, null);
            if (def.RageCost > 0) ChangeResource(r, me, me, ResourceKind.Rage, -def.RageCost, "cost:" + def.Id, null);
            foreach (var e in def.Effects)
            {
                ApplyEffect(r, me, e, def.Id);
                if (AnyDead()) break;
            }
            r.EndsTurn = def.EndTurnAfterUse;
            FinishAction(r);
            return r;
        }

        public string CardBlockReason(Side side, int index)
        {
            var me = State.Get(side);
            if (index < 0 || index >= me.Cards.Count) return "no card";
            var slot = me.Cards[index];
            if (slot.UsesLeft <= 0) return "no uses left";
            if (me.Mana.Current < slot.Def.ManaCost) return "not enough mana";
            if (me.Rage.Current < slot.Def.RageCost) return "not enough rage";
            if (!State.HasSwappedThisTurn && !slot.Def.CanUseBeforeMatch) return "only after a match";
            if (State.HasSwappedThisTurn && !slot.Def.CanUseAfterMatch) return "only before a match";
            return null;
        }

        public string SkillBlockReason(Side side, int index)
        {
            var me = State.Get(side);
            if (index < 0 || index >= me.Skills.Count) return "no skill";
            var sk = me.Skills[index];
            if (me.Mana.Current < sk.ManaCost) return "not enough mana";
            if (me.Rage.Current < sk.RageCost) return "not enough rage";
            if (State.HasSwappedThisTurn) return "already matched this turn";
            return null;
        }

        public SkillCheck PrepareSkill(Side side, int index)
        {
            var check = new SkillCheck();
            if (!IsTurnOf(side))
            {
                check.Reason = "not your turn";
                return check;
            }
            check.Reason = SkillBlockReason(side, index);
            if (check.Reason != null) return check;
            var me = State.Get(side);
            check.Skill = me.Skills[index];
            check.PreviewBase = SkillBase(me, check.Skill);
            check.Ok = true;
            for (int i = 0; i < check.Skill.Qte.ArrowCount; i++) check.Sequence.Add((QteDir)rng.Next(4));
            return check;
        }

        public static int SkillBase(Combatant c, SkillDef s) => SkillPower(c.EffectiveAtk, s, c.SkillLevel(s));

        /// <summary>The damage number printed on a reusable card: ATK share plus a flat part that grows with the card's level.</summary>
        public static int SkillPower(double atk, SkillDef s, int level) =>
            (int)Math.Floor(atk * s.AtkMultiplier + s.FlatBase + s.FlatPerLevel * (Math.Max(1, level) - 1) + 1e-4);

        public ActionResult UseSkill(Side side, int index, QteResult qte)
        {
            if (!IsTurnOf(side)) return ActionResult.Reject(ActionKind.Skill, side, "not your turn");
            var reason = SkillBlockReason(side, index);
            if (reason != null) return ActionResult.Reject(ActionKind.Skill, side, reason);
            var me = State.Get(side);
            var foe = State.Opponent(side);
            var sk = me.Skills[index];
            var r = new ActionResult { Kind = ActionKind.Skill, Actor = side, Accepted = true };
            Emit(r, new CombatEvent { Kind = CombatEventKind.SkillUsed, Actor = side, Target = foe.Side, SourceId = sk.Id, Text = sk.Name });
            if (sk.ManaCost > 0) ChangeResource(r, me, me, ResourceKind.Mana, -sk.ManaCost, "cost:" + sk.Id, null);
            if (sk.RageCost > 0) ChangeResource(r, me, me, ResourceKind.Rage, -sk.RageCost, "cost:" + sk.Id, null);
            int baseDmg = SkillBase(me, sk);
            if (baseDmg > 0)
            {
                var q = sk.UseQte ? qte : new QteResult(sk.Qte.ArrowCount, QteTiming.Good);
                int label = sk.UseQte ? QteMath.Apply(baseDmg, sk.Qte, q) : baseDmg;
                var qev = new CombatEvent
                {
                    Kind = CombatEventKind.QteResolved, Actor = side, Target = foe.Side, SourceId = sk.Id, Computed = label,
                    Text = sk.UseQte ? q.Correct + "/" + sk.Qte.ArrowCount + " " + q.Timing : "no QTE"
                };
                qev.Modifiers.Add("base " + baseDmg);
                if (sk.UseQte)
                {
                    qev.Modifiers.Add("arrows -> " + QteMath.Stepped(baseDmg, sk.Qte, q.Correct));
                    qev.Modifiers.Add("timing x" + QteMath.TimingMultiplier(sk.Qte, q.Timing));
                }
                Emit(r, qev);
                DealDamage(r, me, foe, label, "skill:" + sk.Id, true, false);
            }
            foreach (var e in sk.PostEffects)
            {
                if (AnyDead()) break;
                ApplyEffect(r, me, e, sk.Id);
            }
            r.EndsTurn = sk.EndTurnAfterUse;
            if (r.EndsTurn) State.HasSwappedThisTurn = true;
            FinishAction(r);
            return r;
        }

        /// <summary>Timer ran out before an action was accepted: the turn is skipped, nothing else happens.</summary>
        public ActionResult Timeout(Side side)
        {
            if (!IsTurnOf(side)) return ActionResult.Reject(ActionKind.Timeout, side, "not your turn");
            var r = new ActionResult { Kind = ActionKind.Timeout, Actor = side, Accepted = true, EndsTurn = true };
            Emit(r, new CombatEvent { Kind = CombatEventKind.TurnSkipped, Actor = side, Target = side, Text = "Time out" });
            return r;
        }

        /// <summary>Closes the current turn and hands it to whoever acts next. Call BeginTurn afterwards.</summary>
        public ActionResult EndTurn()
        {
            var r = new ActionResult { Kind = ActionKind.None, Actor = State.Current, Accepted = !State.Ended };
            if (State.Ended) return r;
            var me = State.Get(State.Current);
            for (int i = me.Buffs.Count - 1; i >= 0; i--)
            {
                me.Buffs[i].TurnsLeft--;
                if (me.Buffs[i].TurnsLeft <= 0) me.Buffs.RemoveAt(i);
            }
            State.TurnOpen = false;
            Side next;
            if (State.ContinueTurnFor.HasValue)
            {
                next = State.ContinueTurnFor.Value;
                State.ContinueTurnFor = null;
            }
            else if (State.ExtraTurnPending) next = State.Current;
            else next = State.Current.Other();
            Emit(r, new CombatEvent { Kind = CombatEventKind.TurnEnd, Actor = me.Side, Target = next, Text = "Next: " + next });
            State.Current = next;
            return r;
        }

        /// <summary>Resolves a tally as if it had been matched by this side, without touching the board. Debug and test entry point.</summary>
        public ActionResult ApplyGemTally(Side side, GemTally tally)
        {
            var r = new ActionResult { Kind = ActionKind.Swap, Actor = side, Accepted = true };
            ResolveGems(r, State.Get(side), State.Opponent(side), tally);
            FinishAction(r);
            return r;
        }

        private void ResolveGems(ActionResult r, Combatant me, Combatant foe, GemTally tally)
        {
            var g = me.Gems;
            foreach (var type in Gems.ResolveOrder)
            {
                int eff = tally.EffectiveOf(type);
                if (eff <= 0) continue;
                int phys = tally.PhysicalOf(type);
                switch (type)
                {
                    case GemType.Heart:
                    {
                        int amt = Scaled(me.Hp.Max * g.HeartPctMaxHpPerGem * eff + g.HeartFlatPerGem * eff, g);
                        ChangeResource(r, me, me, ResourceKind.Hp, amt, "gem", ev => Tag(ev, type, phys, eff));
                        break;
                    }
                    case GemType.Lightning:
                    {
                        int amt = Scaled(me.Mana.Max * g.LightningPctMaxManaPerGem * eff + g.LightningFlatPerGem * eff, g);
                        ChangeResource(r, me, me, ResourceKind.Mana, amt, "gem", ev => Tag(ev, type, phys, eff));
                        break;
                    }
                    case GemType.Fire:
                    {
                        int amt = Scaled(g.FireRagePerGem * eff, g);
                        ChangeResource(r, me, me, ResourceKind.Rage, amt, "gem", ev => Tag(ev, type, phys, eff));
                        break;
                    }
                    case GemType.YinYang:
                        Steal(r, me, foe, eff, phys);
                        break;
                    case GemType.Shield:
                    {
                        int amt = Scaled(me.Hp.Max * g.ShieldPctMaxHpPerGem * eff + g.ShieldFlatPerGem * eff, g);
                        ChangeResource(r, me, me, ResourceKind.Shield, amt, "gem", ev => Tag(ev, type, phys, eff));
                        break;
                    }
                    case GemType.Sword:
                    {
                        double raw = me.EffectiveAtk * g.SwordAtkPerGem * eff;
                        var atk = new CombatEvent { Kind = CombatEventKind.Attack, Actor = me.Side, Target = foe.Side, Gem = type, Physical = phys, Effective = eff, SourceId = "gem" };
                        atk.Modifiers.Add("atk " + me.EffectiveAtk + " x " + g.SwordAtkPerGem + " x eff " + eff);
                        Emit(r, atk);
                        DealDamage(r, me, foe, Scaled(raw, g), "gem:Sword", true, true);
                        break;
                    }
                }
                if (AnyDead()) break;
            }
        }

        private static void Tag(CombatEvent ev, GemType t, int phys, int eff)
        {
            ev.Kind = CombatEventKind.GemEffect;
            ev.Gem = t;
            ev.Physical = phys;
            ev.Effective = eff;
        }

        private int Scaled(double v, GemEffectProfile g)
        {
            if (g.Variation > 0) v *= 1 + (rng.NextDouble() * 2 - 1) * g.Variation;
            return Math.Max(0, MathUtil.Round(v, g.Rounding));
        }

        private void Steal(ActionResult r, Combatant me, Combatant foe, int eff, int phys)
        {
            var g = me.Gems;
            bool mana = rng.Chance(g.YinYangManaChance);
            var kind = mana ? ResourceKind.Mana : ResourceKind.Rage;
            int computed = mana
                ? MathUtil.Round(foe.Mana.Max * g.YinYangManaPctOfTargetMaxPerGem * eff, g.Rounding)
                : g.YinYangRagePerGem * eff;
            var from = foe.Get(kind);
            var to = me.Get(kind);
            int fromBefore = from.Current;
            int taken = Math.Min(computed, from.Current);
            from.Add(-taken);
            int toBefore = to.Current;
            int received = to.Add(taken);
            var ev = new CombatEvent
            {
                Kind = CombatEventKind.Steal, Actor = me.Side, Target = foe.Side, Gem = GemType.YinYang, Physical = phys, Effective = eff,
                Resource = kind, Computed = computed, Applied = taken, Before = fromBefore, After = from.Current, SourceId = "gem"
            };
            ev.Modifiers.Add("received " + received + " (" + toBefore + "->" + to.Current + ")");
            if (taken < computed) ev.Modifiers.Add("capped by target " + fromBefore);
            if (received < taken) ev.Modifiers.Add("capped by receiver max " + to.Max);
            Emit(r, ev);
        }

        private void ChangeResource(ActionResult r, Combatant actor, Combatant target, ResourceKind kind, int delta, string source, Action<CombatEvent> decorate)
        {
            var res = target.Get(kind);
            int before = res.Current;
            int applied = res.Add(delta);
            var ev = new CombatEvent
            {
                Kind = CombatEventKind.ResourceChange, Actor = actor.Side, Target = target.Side, Resource = kind,
                Computed = delta, Applied = applied, Before = before, After = res.Current, SourceId = source
            };
            if (applied != delta) ev.Modifiers.Add("capped at " + (delta > 0 ? "max " + res.Max : "0"));
            decorate?.Invoke(ev);
            Emit(r, ev);
        }

        /// <summary>Computes damage label (with strong rage and element), then lets the defense policy decide HP damage.</summary>
        private void DealDamage(ActionResult r, Combatant attacker, Combatant defender, int baseAmount, string source, bool applyElement, bool allowStrong)
        {
            var ev = new CombatEvent { Kind = CombatEventKind.Damage, Actor = attacker.Side, Target = defender.Side, Resource = ResourceKind.Hp, SourceId = source };
            double raw = baseAmount;
            ev.Modifiers.Add("base " + baseAmount);
            if (allowStrong && attacker.Rage.Current >= attacker.RageRules.AttackThreshold)
            {
                int rb = attacker.Rage.Current;
                attacker.Rage.Add(-attacker.RageRules.AttackCost);
                raw *= attacker.RageRules.StrongMultiplier;
                ev.Strong = true;
                ev.Modifiers.Add("rage strike x" + attacker.RageRules.StrongMultiplier + " rage " + rb + "->" + attacker.Rage.Current);
            }
            if (applyElement)
            {
                float em = Rules.Elements.Modifier(attacker.Element, attacker.ElementBonus, defender.Element, defender.ElementBonus);
                if (Math.Abs(em - 1f) > 1e-4)
                {
                    raw *= em;
                    ev.Modifiers.Add("element " + attacker.Element + " vs " + defender.Element + " x" + em.ToString("0.###"));
                }
            }
            int computed = Math.Max(0, MathUtil.Round(raw, RoundingMode.Floor));
            ev.Computed = computed;
            ApplyHit(r, attacker, defender, computed, ev);
        }

        private void ApplyHit(ActionResult r, Combatant attacker, Combatant defender, int computed, CombatEvent ev)
        {
            var d = Rules.Defense;
            int hpDamage;
            ev.ShieldBefore = defender.Shield.Current;
            switch (d.Mode)
            {
                case DefenseMode.FlatDef:
                {
                    int def = defender.Def + defender.Shield.Current;
                    hpDamage = Math.Max(Rules.MinimumHpDamageOnHit, computed - def);
                    ev.Modifiers.Add("flat DEF " + def);
                    break;
                }
                case DefenseMode.PercentReduction:
                {
                    double red = Math.Min(d.MaxPercentReduction, defender.Shield.Current * d.PercentPerShieldPoint);
                    hpDamage = MathUtil.Round(computed * (1 - red), RoundingMode.Floor);
                    ev.Modifiers.Add("reduction " + (red * 100).ToString("0.#") + "%");
                    break;
                }
                default:
                {
                    int absorbed = Math.Min(defender.Shield.Current, computed);
                    defender.Shield.Add(-absorbed);
                    ev.ShieldAbsorbed = absorbed;
                    hpDamage = computed - absorbed;
                    if (d.ApplyDefStat && hpDamage > 0) hpDamage = Math.Max(Rules.MinimumHpDamageOnHit, hpDamage - defender.Def);
                    break;
                }
            }
            if (computed <= 0) hpDamage = 0;
            ev.ShieldAfter = defender.Shield.Current;
            int before = defender.Hp.Current;
            int newHp = before - hpDamage;
            BossPhaseDef lethalPhase = null;
            if (newHp <= 0) lethalPhase = PendingPhase(defender, true);
            if (lethalPhase != null) newHp = 1;
            defender.Hp.Set(newHp);
            ev.Before = before;
            ev.After = defender.Hp.Current;
            ev.Applied = before - defender.Hp.Current;
            Emit(r, ev);

            if (ev.Applied > 0 && defender.Hp.Max > 0)
            {
                int rage = (int)Math.Floor(defender.RageRules.RageFromHpDamageScale * ev.Applied / defender.Hp.Max + 1e-4);
                if (rage > 0) ChangeResource(r, attacker, defender, ResourceKind.Rage, rage, "hp-damage", null);
            }
            var phase = lethalPhase ?? PendingPhase(defender, false);
            if (phase != null) TriggerPhase(r, defender, phase);
        }

        private static BossPhaseDef PendingPhase(Combatant c, bool lethal)
        {
            foreach (var p in c.Phases)
            {
                if (p.OneShot && c.FiredPhases.Contains(p.Id)) continue;
                if (lethal)
                {
                    if (p.TriggerOnLethal) return p;
                    continue;
                }
                if (p.Trigger == PhaseTriggerKind.HpAtOrBelowPct && c.Hp.Current > 0 && c.Hp.Current <= c.Hp.Max * p.TriggerValue) return p;
            }
            return null;
        }

        private void TriggerPhase(ActionResult r, Combatant c, BossPhaseDef p)
        {
            c.FiredPhases.Add(p.Id);
            int before = c.Hp.Current;
            if (!string.IsNullOrEmpty(p.FormName)) c.FormName = p.FormName;
            if (!string.IsNullOrEmpty(p.SpriteKey)) c.SpriteKey = p.SpriteKey;
            if (p.SetHpPctOfMax > 0) c.Hp.Set((int)Math.Floor(c.Hp.Max * p.SetHpPctOfMax + 1e-4));
            if (p.AtkMultiplier > 0) c.FormAtkMultiplier *= p.AtkMultiplier;
            foreach (var id in p.UnlockSkillIds)
            {
                int i = c.LockedSkills.FindIndex(s => s.Id == id);
                if (i < 0) continue;
                c.Skills.Add(c.LockedSkills[i]);
                c.LockedSkills.RemoveAt(i);
            }
            if (p.ContinueTurn) State.ContinueTurnFor = c.Side;
            r.PhaseTriggered = true;
            var ev = new CombatEvent
            {
                Kind = CombatEventKind.PhaseTriggered, Actor = c.Side, Target = c.Side, Resource = ResourceKind.Hp,
                Before = before, After = c.Hp.Current, Applied = c.Hp.Current - before, SourceId = p.Id, Text = c.FormName
            };
            ev.Modifiers.Add("trigger " + p.Trigger + " " + p.TriggerValue + " (" + p.TriggerConfidence + ")");
            if (p.ContinueTurn) ev.Modifiers.Add("continue turn");
            Emit(r, ev);
        }

        private void ApplyEffect(ActionResult r, Combatant me, EffectSpec e, string sourceId)
        {
            var target = e.Target == TargetKind.Self ? me : State.Opponent(me.Side);
            var foe = State.Opponent(me.Side);
            switch (e.Kind)
            {
                case EffectKind.FlatDamage:
                    DealDamage(r, me, target, (int)e.Value, "card:" + sourceId, true, false);
                    break;
                case EffectKind.AtkDamage:
                    DealDamage(r, me, target, (int)Math.Floor(me.EffectiveAtk * e.Value + 1e-4), "card:" + sourceId, true, false);
                    break;
                case EffectKind.HealFlat:
                    ChangeResource(r, me, target, ResourceKind.Hp, (int)e.Value, sourceId, null);
                    break;
                case EffectKind.HealPctMax:
                    ChangeResource(r, me, target, ResourceKind.Hp, (int)Math.Floor(target.Hp.Max * e.Value + 1e-4), sourceId, null);
                    break;
                case EffectKind.AddMana:
                    ChangeResource(r, me, target, ResourceKind.Mana, (int)e.Value, sourceId, null);
                    break;
                case EffectKind.AddRage:
                    ChangeResource(r, me, target, ResourceKind.Rage, (int)e.Value, sourceId, null);
                    break;
                case EffectKind.AddShieldPctMax:
                    ChangeResource(r, me, target, ResourceKind.Shield, (int)Math.Floor(target.Hp.Max * e.Value + 1e-4), sourceId, null);
                    break;
                case EffectKind.DrainManaPctOfCurrent:
                    Drain(r, me, foe, ResourceKind.Mana, (int)Math.Floor(foe.Mana.Current * e.Value + 1e-4), sourceId);
                    break;
                case EffectKind.DrainRage:
                    Drain(r, me, foe, ResourceKind.Rage, (int)e.Value, sourceId);
                    break;
                case EffectKind.BuffAtk:
                case EffectKind.DebuffAtk:
                {
                    float mult = e.Kind == EffectKind.BuffAtk ? 1f + e.Value : Math.Max(0.1f, 1f - e.Value);
                    target.Buffs.Add(new Buff { Label = e.Label ?? sourceId, AtkMultiplier = mult, TurnsLeft = Math.Max(1, e.Turns) });
                    var ev = new CombatEvent { Kind = CombatEventKind.Buff, Actor = me.Side, Target = target.Side, SourceId = sourceId, Text = (e.Label ?? sourceId) + " ATK x" + mult.ToString("0.##") };
                    Emit(r, ev);
                    break;
                }
                case EffectKind.Summon:
                {
                    var s = new Summon { Name = e.Label ?? "Summon", DamagePerTurn = Math.Max(1, (int)Math.Floor(me.EffectiveAtk * e.Value)), TurnsLeft = Math.Max(1, e.Turns) };
                    me.Summons.Add(s);
                    Emit(r, new CombatEvent { Kind = CombatEventKind.SummonCreated, Actor = me.Side, Target = me.Side, SourceId = sourceId, Computed = s.DamagePerTurn, Text = s.Name + " x" + s.TurnsLeft + " turns" });
                    break;
                }
                case EffectKind.SetHpPctOfMax:
                {
                    int before = target.Hp.Current;
                    target.Hp.Set((int)Math.Floor(target.Hp.Max * e.Value + 1e-4));
                    Emit(r, new CombatEvent { Kind = CombatEventKind.ResourceChange, Actor = me.Side, Target = target.Side, Resource = ResourceKind.Hp, Before = before, After = target.Hp.Current, Applied = target.Hp.Current - before, SourceId = sourceId });
                    break;
                }
            }
        }

        private void Drain(ActionResult r, Combatant me, Combatant foe, ResourceKind kind, int computed, string sourceId)
        {
            var from = foe.Get(kind);
            var to = me.Get(kind);
            int before = from.Current;
            int taken = Math.Min(Math.Max(0, computed), from.Current);
            from.Add(-taken);
            int toBefore = to.Current;
            int received = to.Add(taken);
            var ev = new CombatEvent
            {
                Kind = CombatEventKind.Steal, Actor = me.Side, Target = foe.Side, Resource = kind, Computed = computed, Applied = taken,
                Before = before, After = from.Current, SourceId = sourceId
            };
            ev.Modifiers.Add("received " + received + " (" + toBefore + "->" + to.Current + ")");
            Emit(r, ev);
        }

        private bool AnyDead() => State.Actors[0].IsDead || State.Actors[1].IsDead;

        private void FinishAction(ActionResult r)
        {
            if (State.Ended || !AnyDead()) return;
            var p = State.Actors[0];
            var e = State.Actors[1];
            Side winner;
            if (p.IsDead && e.IsDead) winner = r.Actor;
            else winner = p.IsDead ? Side.Enemy : Side.Player;
            foreach (var c in State.Actors)
                if (c.IsDead) Emit(r, new CombatEvent { Kind = CombatEventKind.Death, Actor = c.Side, Target = c.Side, Text = c.FormName });
            State.Ended = true;
            State.Winner = winner;
            State.TurnOpen = false;
            r.BattleEnded = true;
            r.Winner = winner;
            r.EndsTurn = true;
            Emit(r, new CombatEvent { Kind = CombatEventKind.BattleEnded, Actor = winner, Target = winner.Other(), Text = winner + " wins" });
        }

        private void Emit(ActionResult r, CombatEvent ev)
        {
            ev.ActorAfter = State.Get(ev.Actor).Snapshot();
            ev.TargetAfter = State.Get(ev.Target).Snapshot();
            r.Events.Add(ev);
            State.Log.Add(ev);
        }
    }
}
