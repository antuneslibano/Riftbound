using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>High level AI state of a unit (shown in Debug Mode).</summary>
    public enum UnitState
    {
        Idle = 0,
        MovingToRift,
        HoldingRift,
        Engaging,
        AssaultingCore,
        Dead,
    }

    /// <summary>
    /// Pure simulation data of a unit (no GameObject). Behaviour lives in
    /// <see cref="UnitBrain"/> (decisions), <see cref="UnitMotor"/> (movement) and <see cref="UnitCombat"/> (attacks).
    /// Views read this object; they never write to it.
    /// </summary>
    public class Unit
    {
        public int Id;
        public Team Team;
        public CardDefinition Card;
        public UnitStats Stats;
        public string Name;

        // --- Runtime state
        public Vector2 Position;
        public float Hp;
        public bool Removed;
        public UnitState State;

        // --- Targeting
        public Unit TargetUnit;
        public bool TargetingCore;
        public float RetargetTimer;

        // --- Objective / navigation
        public bool HasGoal;
        public NodeId GoalNode;
        public NodeId Waypoint;
        public bool WaypointValid;
        public NodeId NavGoal;
        public int NavVersion = -1;
        public Vector2 HoldOffset;
        public float ThinkTimer;
        public Rift InsideRift;

        // --- Combat
        public float AttackCooldown;
        public float TimeSinceHit = 999f;
        public float TimeSinceAttack = 999f;
        public Vector2 LastAttackPoint;

        // --- Parasite status
        public bool Infected;
        public float ParasiteTimer;
        public float ParasiteDamage;
        public Team ParasiteOwner;
        public CardDefinition ParasiteSpawnCard;
        public int ParasiteSpawnCount;

        // --- Stats (debug)
        public float DamageDealt;

        public bool Alive => !Removed && Hp > 0f;

        public float HpFraction => Stats.maxHp > 0f ? Mathf.Clamp01(Hp / Stats.maxHp) : 0f;

        /// <summary>Rough value of the unit, used by the bot to weigh threats.</summary>
        public float ThreatValue => (Card != null ? Mathf.Max(1, Card.fluxCost) / (float)Mathf.Max(1, Card.spawnCount) : 1f) * (0.35f + 0.65f * HpFraction);
    }
}
