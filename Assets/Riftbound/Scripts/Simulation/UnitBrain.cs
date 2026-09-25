using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// Decision making of units (targeting + objective selection). Deliberately simple and predictable:
    /// <list type="number">
    /// <item>Enemy unit in sight → engage it (Hunters always, others unless pushing a Rift Break).</item>
    /// <item>Own Rift Break active → assault the enemy Core.</item>
    /// <item>Otherwise pick a Rift by score: defend threatened &gt; capture neutral &gt; attack enemy Rift,
    /// minus distance and crowding.</item>
    /// </list>
    /// </summary>
    public static class UnitBrain
    {
        const float RetargetInterval = 0.25f;
        const float ThinkInterval = 0.6f;

        // Objective scores (priority order requested by the design).
        const float ScoreDefend = 100f;
        const float ScoreCapture = 70f;
        const float ScoreAttack = 50f;
        const float ScoreHoldOwned = 15f;
        const float DistancePenalty = 4f;
        const float CrowdPenalty = 7f;
        const int CrowdFree = 2;

        public static void Tick(Unit u, Match m, float dt)
        {
            var cfg = m.Config;
            bool ownBreak = m.RiftBreak.IsActiveFor(u.Team);

            // ---- Targeting
            if (u.TargetUnit != null)
            {
                float leash = u.Stats.sightRange * cfg.leashMultiplier;
                if (!u.TargetUnit.Alive || Vector2.Distance(u.Position, u.TargetUnit.Position) > leash)
                    u.TargetUnit = null;
            }

            u.RetargetTimer -= dt;
            if (u.RetargetTimer <= 0f)
            {
                u.RetargetTimer = RetargetInterval;
                float sight = (ownBreak && !u.Stats.prioritizeUnits) ? cfg.breakAggroRange : u.Stats.sightRange;
                var nearest = m.Units.FindNearestEnemy(u.Team, u.Position, sight);
                if (nearest != null)
                {
                    if (u.TargetUnit == null)
                        u.TargetUnit = nearest;
                    else if (nearest != u.TargetUnit)
                    {
                        // Switch only if the new one is clearly closer (avoids jitter).
                        float dCur = Vector2.Distance(u.Position, u.TargetUnit.Position);
                        float dNew = Vector2.Distance(u.Position, nearest.Position);
                        if (dNew < dCur * 0.6f) u.TargetUnit = nearest;
                    }
                }
                else if (ownBreak && !u.Stats.prioritizeUnits)
                {
                    // Pushing the Core: don't chase enemies that are not in the way.
                    u.TargetUnit = null;
                }
            }

            // ---- Objective
            u.ThinkTimer -= dt;
            if (u.ThinkTimer <= 0f)
            {
                u.ThinkTimer = ThinkInterval;
                ChooseGoal(u, m, ownBreak);
            }

            u.TargetingCore = ownBreak && u.TargetUnit == null;

            // ---- State (for debug display)
            if (u.TargetUnit != null) u.State = UnitState.Engaging;
            else if (u.TargetingCore) u.State = UnitState.AssaultingCore;
            else if (u.HasGoal && u.InsideRift != null && u.InsideRift.Node == u.GoalNode) u.State = UnitState.HoldingRift;
            else if (u.HasGoal) u.State = UnitState.MovingToRift;
            else u.State = UnitState.Idle;
        }

        /// <summary>Forces an immediate re-evaluation (e.g. when a Rift Break starts/ends).</summary>
        public static void Rethink(Unit u)
        {
            u.ThinkTimer = 0f;
            u.RetargetTimer = 0f;
        }

        static void ChooseGoal(Unit u, Match m, bool ownBreak)
        {
            if (ownBreak)
            {
                u.HasGoal = true;
                u.GoalNode = MapNetwork.CoreOf(u.Team.Opponent());
                return;
            }

            Team team = u.Team;
            Team opp = team.Opponent();
            Rift best = null;
            float bestScore = float.NegativeInfinity;

            var rifts = m.Rifts;
            for (int i = 0; i < rifts.Length; i++)
            {
                var r = rifts[i];
                bool own = r.Owner.Is(team);
                bool enemyOwned = r.Owner.Is(opp);
                bool inside = u.InsideRift == r;
                float score;

                if (own && r.Presence[(int)opp] > 0) score = ScoreDefend;
                else if (!own && !enemyOwned) score = ScoreCapture;
                else if (enemyOwned) score = ScoreAttack;
                else
                {
                    score = ScoreHoldOwned;
                    // Own Rift that is being eroded: top it up.
                    if (r.ProgressFor(team) < 0.99f) score += 30f;
                }

                // Don't walk away from a capture in progress.
                if (inside && !own) score += 40f;
                if (inside && own) score += u.Stats.holdsRifts ? 45f : 10f;

                score -= Vector2.Distance(u.Position, r.Position) * DistancePenalty;

                int crowd = m.Units.CountGoal(team, r.Node, u);
                if (crowd > CrowdFree) score -= (crowd - CrowdFree) * CrowdPenalty;

                if (score > bestScore) { bestScore = score; best = r; }
            }

            if (best != null)
            {
                u.HasGoal = true;
                u.GoalNode = best.Node;
            }
            else u.HasGoal = false;
        }
    }
}
