using System;
using UnityEngine;

namespace Riftbound.Config
{
    /// <summary>What happens when a card is played.</summary>
    public enum CardKind
    {
        /// <summary>Spawns <see cref="CardDefinition.spawnCount"/> units at the chosen position.</summary>
        Unit = 0,
        /// <summary>Instant area damage on a chosen Rift.</summary>
        PulseSpell = 1,
        /// <summary>Delayed heavy damage on a chosen enemy unit; spawns allies if it kills.</summary>
        ParasiteSpell = 2,
    }

    /// <summary>Placeholder geometry used to draw a unit.</summary>
    public enum UnitShape
    {
        Circle = 0,
        Triangle = 1,
        Square = 2,
        Hexagon = 3,
        Diamond = 4,
    }

    /// <summary>
    /// Combat / movement stats of a unit. All balance numbers of a unit live here.
    /// Attack Speed is expressed as <see cref="attackInterval"/> (seconds between hits).
    /// </summary>
    [Serializable]
    public class UnitStats
    {
        [Header("Survivability")]
        public float maxHp = 300f;
        [Tooltip("0..1 damage reduction applied while the unit stands inside ANY Rift (Anchor).")]
        [Range(0f, 0.9f)] public float riftDamageReduction = 0f;

        [Header("Offense")]
        public float attackDamage = 30f;
        [Tooltip("Seconds between two attacks. Attack speed = 1 / interval.")]
        public float attackInterval = 1f;
        [Tooltip("Distance (edge to edge) at which the unit can hit.")]
        public float attackRange = 0.5f;
        [Tooltip("Distance at which the unit notices and engages enemies.")]
        public float sightRange = 2.6f;
        [Tooltip("Core Stability removed per hit while the enemy Core is vulnerable (Rift Break).")]
        public float coreDamage = 2f;
        [Tooltip("Hunter behaviour: always prefers enemy units over objectives and sees further.")]
        public bool prioritizeUnits = false;

        [Header("Movement")]
        public float moveSpeed = 1.5f;

        [Header("Special")]
        [Tooltip("Leech: Flux drained per second from the opponent while standing in a Rift the opponent controls.")]
        public float fluxDrainPerSecond = 0f;
        [Tooltip("Holder units (tank/defender) prefer to stay in the Rift they are holding.")]
        public bool holdsRifts = false;

        [Header("Visual")]
        public UnitShape shape = UnitShape.Circle;
        [Tooltip("Body radius in world units (also used for spacing/collisions).")]
        public float bodyRadius = 0.3f;

        public float Dps => attackInterval > 0f ? attackDamage / attackInterval : 0f;

        public UnitStats Clone() => (UnitStats)MemberwiseClone();
    }

    /// <summary>
    /// A card of the deck. Create new ones with Assets > Create > Riftbound > Card Definition
    /// and add them to the deck list of the GameConfig asset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Riftbound/Card Definition", order = 1)]
    public class CardDefinition : ScriptableObject
    {
        [Header("Card")]
        public string cardId = "NEW";
        public string displayName = "NEW";
        [Min(0)] public int fluxCost = 3;
        public CardKind kind = CardKind.Unit;
        [TextArea] public string description = "";

        [Header("Unit (kind = Unit)")]
        public UnitStats unit = new UnitStats();
        [Min(1)] public int spawnCount = 1;
        [Tooltip("Radius of the circle multiple units are spawned on.")]
        public float spawnSpread = 0.35f;

        [Header("Pulse (kind = PulseSpell)")]
        public float pulseDamage = 260f;
        [Tooltip("Radius around the Rift centre that is hit.")]
        public float pulseRadius = 1.7f;

        [Header("Parasite (kind = ParasiteSpell)")]
        public float parasiteDelay = 10f;
        public float parasiteDamage = 700f;
        [Tooltip("Card whose unit stats are used for the allies spawned when the host dies from the parasite.")]
        public CardDefinition parasiteSpawnCard;
        [Min(0)] public int parasiteSpawnCount = 2;
        [Tooltip("How close (world units) a tap must be to an enemy unit to select it.")]
        public float targetPickRadius = 0.9f;

        public bool IsSpell => kind != CardKind.Unit;
    }
}
