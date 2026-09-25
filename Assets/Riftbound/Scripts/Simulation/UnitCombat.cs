using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>Auto-attacks and timed status effects (Parasite).</summary>
    public static class UnitCombat
    {
        public static void Tick(Unit u, Match m, float dt)
        {
            u.AttackCooldown -= dt;
            u.TimeSinceHit += dt;
            u.TimeSinceAttack += dt;

            if (u.AttackCooldown > 0f) return;

            var target = u.TargetUnit;
            if (target != null && target.Alive)
            {
                if (UnitMotor.EdgeDistance(u, target) <= u.Stats.attackRange + 0.05f)
                {
                    m.DealDamage(u, target, u.Stats.attackDamage);
                    u.AttackCooldown = u.Stats.attackInterval;
                    u.TimeSinceAttack = 0f;
                    u.LastAttackPoint = target.Position;
                }
                return;
            }

            if (u.TargetingCore)
            {
                var core = m.GetCore(u.Team.Opponent());
                if (core.Vulnerable && core.DistanceTo(u.Position) - u.Stats.bodyRadius <= u.Stats.attackRange + 0.05f)
                {
                    m.DamageCore(u.Team, u.Stats.coreDamage, u);
                    u.AttackCooldown = u.Stats.attackInterval;
                    u.TimeSinceAttack = 0f;
                    u.LastAttackPoint = core.ClosestPoint(u.Position);
                }
            }
        }

        /// <summary>Counts down the Parasite infection and detonates it.</summary>
        public static void TickParasite(Unit u, Match m, float dt)
        {
            if (!u.Infected || !u.Alive) return;
            u.ParasiteTimer -= dt;
            if (u.ParasiteTimer > 0f) return;

            u.Infected = false;
            Vector2 pos = u.Position;
            m.ApplyRawDamage(u, u.ParasiteDamage, u.ParasiteOwner);
            m.RaiseFx(FxKind.ParasiteBurst, pos, 0.8f, u.ParasiteOwner);

            if (!u.Alive && u.ParasiteSpawnCard != null)
            {
                for (int i = 0; i < u.ParasiteSpawnCount; i++)
                {
                    float a = i * Mathf.PI * 2f / Mathf.Max(1, u.ParasiteSpawnCount);
                    Vector2 p = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.3f;
                    m.SpawnUnit(u.ParasiteSpawnCard, u.ParasiteOwner, p);
                }
            }
        }
    }
}
