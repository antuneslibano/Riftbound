using System;

namespace Riftbound.Simulation
{
    /// <summary>
    /// RIFT BREAK: when one team controls all Rifts at once, the opponent's Core becomes vulnerable
    /// for <c>riftBreakDuration</c> seconds. With <c>resetRiftsOnBreak</c> the Rifts shatter back to Neutral
    /// at that moment, so another Break requires conquering the whole map again.
    /// </summary>
    public class RiftBreakManager
    {
        readonly Match match;

        public bool Active { get; private set; }
        public Team Breaker { get; private set; }
        public float Remaining { get; private set; }
        public int[] BreakCount { get; } = new int[2];

        public event Action<Team> Started;
        public event Action<Team> Ended;

        public RiftBreakManager(Match match)
        {
            this.match = match;
        }

        public bool IsActiveFor(Team team) => Active && Breaker == team;

        public void Tick(float dt)
        {
            if (Active)
            {
                Remaining -= dt;
                if (Remaining <= 0f) Stop();
                return;
            }

            if (match.OwnedRiftCount(Team.Player) == match.Rifts.Length) Begin(Team.Player);
            else if (match.OwnedRiftCount(Team.Enemy) == match.Rifts.Length) Begin(Team.Enemy);
        }

        /// <summary>Starts a Rift Break for <paramref name="team"/> (also used by the debug button).</summary>
        public void Begin(Team team)
        {
            if (Active) Stop();
            Active = true;
            Breaker = team;
            Remaining = match.Config.riftBreakDuration;
            BreakCount[(int)team]++;
            match.GetCore(team.Opponent()).Vulnerable = true;

            if (match.Config.resetRiftsOnBreak)
                foreach (var r in match.Rifts) r.ResetToNeutral();

            match.RethinkAllUnits();
            Started?.Invoke(team);
        }

        public void Stop()
        {
            if (!Active) return;
            Active = false;
            Remaining = 0f;
            match.GetCore(Breaker.Opponent()).Vulnerable = false;
            match.RethinkAllUnits();
            Ended?.Invoke(Breaker);
        }
    }
}
