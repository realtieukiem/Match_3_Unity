namespace Pokiwar.Domain
{
    /// <summary>Owns the per-turn deadline. The HUD only reads Remaining; expiry is decided here.</summary>
    public sealed class TurnClock
    {
        public float Duration;
        public float Remaining { get; private set; }
        public bool Running { get; private set; }
        public bool Expired { get; private set; }

        public TurnClock(float duration)
        {
            Duration = duration;
            Remaining = duration;
        }

        public void Restart()
        {
            Remaining = Duration;
            Running = true;
            Expired = false;
        }

        /// <summary>An action was accepted before the deadline: freeze so queued resolution can finish.</summary>
        public void Freeze() => Running = false;

        public void Resume()
        {
            if (!Expired) Running = true;
        }

        /// <summary>Returns true exactly once, on the tick the deadline passes.</summary>
        public bool Tick(float dt)
        {
            if (!Running || Expired) return false;
            Remaining -= dt;
            if (Remaining > 0f) return false;
            Remaining = 0f;
            Running = false;
            Expired = true;
            return true;
        }
    }
}
