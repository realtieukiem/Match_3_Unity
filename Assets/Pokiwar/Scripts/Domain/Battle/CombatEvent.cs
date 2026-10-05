using System.Collections.Generic;
using System.Text;

namespace Pokiwar.Domain
{
    public enum CombatEventKind
    {
        TurnStart,
        TurnSkipped,
        TurnEnd,
        BoardResolved,
        GemSummary,
        GemEffect,
        Attack,
        Damage,
        ResourceChange,
        Steal,
        CardUsed,
        SkillUsed,
        QteResolved,
        Buff,
        SummonCreated,
        SummonHit,
        PhaseTriggered,
        Death,
        BattleEnded,
        Reshuffled
    }

    /// <summary>One resolved step.</summary>
    public sealed class CombatEvent
    {
        public CombatEventKind Kind;
        public Side Actor;
        public Side Target;
        public GemType Gem = GemType.None;
        public int Physical;
        public int Effective;
        public ResourceKind Resource;
        public int Computed;
        public int Applied;
        public int Before;
        public int After;
        public int ShieldAbsorbed;
        public int ShieldBefore;
        public int ShieldAfter;
        public bool Strong;
        public string SourceId;
        public string Text;
        public readonly List<string> Modifiers = new List<string>();
        public CombatantSnapshot ActorAfter;
        public CombatantSnapshot TargetAfter;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(Kind).Append("] ").Append(Actor);
            if (Actor != Target || Kind == CombatEventKind.Damage) sb.Append("->").Append(Target);
            if (Gem != GemType.None) sb.Append(' ').Append(Gem).Append(" eff ").Append(Effective).Append(" phys ").Append(Physical);
            if (Resource != ResourceKind.None)
                sb.Append(' ').Append(Resource).Append(' ').Append(Before).Append("->").Append(After)
                  .Append(" (computed ").Append(Computed).Append(", applied ").Append(Applied).Append(')');
            if (ShieldAbsorbed > 0) sb.Append(" shield absorbed ").Append(ShieldAbsorbed).Append(' ').Append(ShieldBefore).Append("->").Append(ShieldAfter);
            if (Strong) sb.Append(" STRONG");
            if (!string.IsNullOrEmpty(SourceId)) sb.Append(" src=").Append(SourceId);
            if (Modifiers.Count > 0) sb.Append(" mods{").Append(string.Join("; ", Modifiers)).Append('}');
            if (!string.IsNullOrEmpty(Text)) sb.Append(" \"").Append(Text).Append('"');
            return sb.ToString();
        }
    }

    public enum ActionKind { None, Swap, Card, Skill, Timeout, TurnStart }

    public sealed class ActionResult
    {
        public ActionKind Kind;
        public Side Actor;
        public bool Accepted;
        public string RejectReason;
        public SwapResolution Board;
        public readonly List<CombatEvent> Events = new List<CombatEvent>();
        public bool EndsTurn;
        public bool BattleEnded;
        public Side Winner;
        public bool PhaseTriggered;

        public static ActionResult Reject(ActionKind kind, Side actor, string reason) =>
            new ActionResult { Kind = kind, Actor = actor, Accepted = false, RejectReason = reason };
    }
}
