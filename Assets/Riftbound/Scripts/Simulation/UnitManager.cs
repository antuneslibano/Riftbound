using System;
using System.Collections.Generic;
using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// Registry of all living units plus spatial queries. With a hard cap of a few dozen units
    /// brute-force loops are cheaper and simpler than any spatial structure.
    /// </summary>
    public class UnitManager
    {
        readonly List<Unit> all = new List<Unit>(64);
        readonly List<Unit>[] byTeam = { new List<Unit>(32), new List<Unit>(32) };
        int nextId = 1;

        public event Action<Unit> Spawned;
        public event Action<Unit> Died;

        public IReadOnlyList<Unit> All => all;

        public IReadOnlyList<Unit> OfTeam(Team t) => byTeam[(int)t];

        public int Count(Team t) => byTeam[(int)t].Count;

        public Unit Spawn(CardDefinition card, UnitStats stats, Team team, Vector2 position, System.Random rng)
        {
            var u = new Unit
            {
                Id = nextId++,
                Team = team,
                Card = card,
                Stats = stats,
                Name = card != null ? card.displayName : "UNIT",
                Position = position,
                Hp = stats.maxHp,
                State = UnitState.Idle,
                // Stagger think/retarget timers so all units don't evaluate on the same frame.
                ThinkTimer = (float)rng.NextDouble() * 0.3f,
                RetargetTimer = (float)rng.NextDouble() * 0.2f,
                AttackCooldown = stats.attackInterval * 0.5f,
            };
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = 0.15f + (float)rng.NextDouble() * 0.45f;
            u.HoldOffset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;

            all.Add(u);
            byTeam[(int)team].Add(u);
            Spawned?.Invoke(u);
            return u;
        }

        public Unit FindById(int id)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].Id == id) return all[i];
            return null;
        }

        /// <summary>Removes dead units from the lists and raises <see cref="Died"/>.</summary>
        public void RemoveDead()
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var u = all[i];
                if (u.Hp > 0f && !u.Removed) continue;
                u.Removed = true;
                u.State = UnitState.Dead;
                all.RemoveAt(i);
                byTeam[(int)u.Team].Remove(u);
                Died?.Invoke(u);
            }
        }

        /// <summary>Nearest living enemy of <paramref name="team"/> within <paramref name="range"/> (edge to edge).</summary>
        public Unit FindNearestEnemy(Team team, Vector2 pos, float range)
        {
            var list = byTeam[(int)team.Opponent()];
            Unit best = null;
            float bestD = float.PositiveInfinity;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (!e.Alive) continue;
                float d = Vector2.Distance(pos, e.Position) - e.Stats.bodyRadius;
                if (d <= range && d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        /// <summary>Enemy unit closest to a tapped point (used by Parasite targeting).</summary>
        public Unit FindEnemyNear(Team team, Vector2 point, float pickRadius)
        {
            var list = byTeam[(int)team.Opponent()];
            Unit best = null;
            float bestD = float.PositiveInfinity;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (!e.Alive) continue;
                float d = Vector2.Distance(point, e.Position) - e.Stats.bodyRadius;
                if (d <= pickRadius && d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        public int CountInRadius(Team team, Vector2 center, float radius)
        {
            var list = byTeam[(int)team];
            float r2 = radius * radius;
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Alive && (list[i].Position - center).sqrMagnitude <= r2) n++;
            return n;
        }

        /// <summary>Sum of <see cref="Unit.ThreatValue"/> of a team's units inside a radius.</summary>
        public float StrengthInRadius(Team team, Vector2 center, float radius)
        {
            var list = byTeam[(int)team];
            float r2 = radius * radius;
            float s = 0f;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Alive && (list[i].Position - center).sqrMagnitude <= r2) s += list[i].ThreatValue;
            return s;
        }

        /// <summary>Number of allies whose current goal is the given node (used to spread units).</summary>
        public int CountGoal(Team team, NodeId node, Unit exclude)
        {
            var list = byTeam[(int)team];
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var u = list[i];
                if (u != exclude && u.Alive && u.HasGoal && u.GoalNode == node) n++;
            }
            return n;
        }

        public void Clear()
        {
            all.Clear();
            byTeam[0].Clear();
            byTeam[1].Clear();
        }
    }
}
