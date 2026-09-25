using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// Movement: straight chase when engaging, otherwise follow the network connections
    /// (node to node) towards the unit's goal. No physics; a cheap separation pass keeps units readable.
    /// </summary>
    public static class UnitMotor
    {
        const float WaypointReach = 0.3f;

        public static void Tick(Unit u, Match m, float dt)
        {
            float step = u.Stats.moveSpeed * dt;
            if (step <= 0f) return;

            // 1. Chasing an enemy unit.
            if (u.TargetUnit != null)
            {
                float gap = EdgeDistance(u, u.TargetUnit);
                if (gap > u.Stats.attackRange)
                    MoveTowards(u, u.TargetUnit.Position, Mathf.Min(step, gap - u.Stats.attackRange * 0.8f));
                u.WaypointValid = false; // left the network - replan afterwards
                return;
            }

            if (!u.HasGoal) return;

            // 2. Assaulting the enemy Core.
            if (u.TargetingCore)
            {
                var core = m.GetCore(u.Team.Opponent());
                float gap = core.DistanceTo(u.Position) - u.Stats.bodyRadius;
                if (gap <= u.Stats.attackRange) return;
                // Close to the core: approach its nearest point directly.
                if (gap < 1.5f)
                {
                    MoveTowards(u, core.ClosestPoint(u.Position), Mathf.Min(step, gap));
                    return;
                }
            }

            // 3. Walk the network towards the goal node.
            NodeId goal = u.GoalNode;
            var net = m.Network;
            Vector2 goalPos = net.GetPosition(goal);
            Vector2 finalPos = MapNetwork.IsRift(goal)
                ? goalPos + u.HoldOffset * (m.Config.riftRadius * 0.9f)
                : goalPos;

            // Inside the goal Rift already: settle on the personal hold spot.
            if (MapNetwork.IsRift(goal) && (u.Position - goalPos).sqrMagnitude <= m.Config.riftRadius * m.Config.riftRadius)
            {
                MoveTowards(u, finalPos, step);
                return;
            }

            if (!u.WaypointValid || u.NavGoal != goal || u.NavVersion != net.Version)
            {
                u.Waypoint = net.ChooseEntryNode(u.Position, goal, m.Config.offNetworkCostMultiplier);
                u.WaypointValid = true;
                u.NavGoal = goal;
                u.NavVersion = net.Version;
            }

            if (u.Waypoint == goal)
            {
                MoveTowards(u, finalPos, step);
                return;
            }

            Vector2 wp = net.GetPosition(u.Waypoint);
            if ((u.Position - wp).sqrMagnitude <= WaypointReach * WaypointReach)
            {
                u.Waypoint = net.NextHop(u.Waypoint, goal);
                wp = u.Waypoint == goal ? finalPos : net.GetPosition(u.Waypoint);
            }
            MoveTowards(u, wp, step);
        }

        public static float EdgeDistance(Unit a, Unit b)
        {
            return Vector2.Distance(a.Position, b.Position) - a.Stats.bodyRadius - b.Stats.bodyRadius;
        }

        static void MoveTowards(Unit u, Vector2 dest, float maxStep)
        {
            if (maxStep <= 0f) return;
            u.Position = Vector2.MoveTowards(u.Position, dest, maxStep);
        }

        /// <summary>Pushes overlapping units apart and clamps everybody inside the map. O(n²) on ~50 units.</summary>
        public static void Separate(UnitManager units, MapLayout layout, float dt)
        {
            var all = units.All;
            int n = all.Count;
            for (int i = 0; i < n; i++)
            {
                var a = all[i];
                for (int j = i + 1; j < n; j++)
                {
                    var b = all[j];
                    float minDist = (a.Stats.bodyRadius + b.Stats.bodyRadius) * 0.9f;
                    Vector2 d = b.Position - a.Position;
                    float sq = d.sqrMagnitude;
                    if (sq >= minDist * minDist) continue;

                    float dist = Mathf.Sqrt(sq);
                    Vector2 dir = dist > 0.0001f ? d / dist : new Vector2(((a.Id + b.Id) & 1) == 0 ? 1f : -1f, 0.3f).normalized;
                    float push = Mathf.Min((minDist - dist) * 0.5f, 3f * dt);
                    // Heavier (bigger) units get pushed less.
                    float wa = b.Stats.bodyRadius / (a.Stats.bodyRadius + b.Stats.bodyRadius);
                    a.Position -= dir * push * 2f * wa;
                    b.Position += dir * push * 2f * (1f - wa);
                }
            }

            Vector2 ext = layout.halfExtents;
            for (int i = 0; i < n; i++)
            {
                var u = all[i];
                u.Position = new Vector2(Mathf.Clamp(u.Position.x, -ext.x, ext.x), Mathf.Clamp(u.Position.y, -ext.y, ext.y));
            }
        }
    }
}
