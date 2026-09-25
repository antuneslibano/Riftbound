using UnityEngine;

namespace Riftbound.Config
{
    /// <summary>
    /// Built-in default content. Used (1) as a runtime fallback when the config asset is missing and
    /// (2) by the editor menu "Riftbound > Regenerate Default Config Assets".
    /// The shipped .asset files under Assets/Riftbound/Config contain the same values; once they exist,
    /// edit the assets (Inspector), not this file.
    /// </summary>
    public static class DefaultContent
    {
        public static GameConfig CreateConfig()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            cfg.name = "RiftboundConfig (defaults)";
            var cards = CreateCards(out _);
            cfg.deck.AddRange(cards);
            return cfg;
        }

        /// <summary>Creates the 8 deck cards (+ the hidden Spawnling card used by Parasite).</summary>
        public static CardDefinition[] CreateCards(out CardDefinition spawnling)
        {
            spawnling = Unit("SPAWNLING", "SPAWNLING", 0, "Hidden card: allies spawned by Parasite.",
                hp: 140, dmg: 20, interval: 0.8f, range: 0.35f, sight: 2.4f, speed: 2.2f, core: 0.3f,
                shape: UnitShape.Diamond, radius: 0.2f);

            var maw = Unit("MAW", "MAW", 5, "Tank. Huge HP, slow, moderate damage. Great Rift holder.",
                hp: 1400, dmg: 70, interval: 1.4f, range: 0.45f, sight: 2.4f, speed: 0.85f, core: 1.4f,
                shape: UnitShape.Circle, radius: 0.5f);
            maw.unit.holdsRifts = true;

            var blink = Unit("BLINK", "BLINK", 2, "Fast unit. Low HP/damage, very fast. Grabs empty Rifts.",
                hp: 180, dmg: 30, interval: 0.7f, range: 0.35f, sight: 2.2f, speed: 3.0f, core: 0.6f,
                shape: UnitShape.Triangle, radius: 0.24f);

            var leech = Unit("LEECH", "LEECH", 3, "Disruptor. Drains enemy Flux while inside an enemy-controlled Rift.",
                hp: 450, dmg: 25, interval: 1.0f, range: 1.4f, sight: 2.6f, speed: 1.4f, core: 0.6f,
                shape: UnitShape.Circle, radius: 0.26f);
            leech.unit.fluxDrainPerSecond = 0.35f;

            var anchor = Unit("ANCHOR", "ANCHOR", 4, "Defender. High HP, slow, takes 50% less damage inside any Rift.",
                hp: 1000, dmg: 45, interval: 1.2f, range: 0.45f, sight: 2.4f, speed: 0.9f, core: 0.8f,
                shape: UnitShape.Square, radius: 0.4f);
            anchor.unit.riftDamageReduction = 0.5f;
            anchor.unit.holdsRifts = true;

            var hunter = Unit("HUNTER", "HUNTER", 3, "Damage dealer. High damage, hunts enemy units first.",
                hp: 420, dmg: 110, interval: 1.1f, range: 1.6f, sight: 3.4f, speed: 1.6f, core: 1.1f,
                shape: UnitShape.Triangle, radius: 0.34f);
            hunter.unit.prioritizeUnits = true;

            var swarm = Unit("SWARM", "SWARM", 3, "Four tiny fast units. Strength in numbers.",
                hp: 110, dmg: 22, interval: 0.8f, range: 0.3f, sight: 2.2f, speed: 2.4f, core: 0.4f,
                shape: UnitShape.Circle, radius: 0.17f);
            swarm.spawnCount = 4;
            swarm.spawnSpread = 0.35f;

            var pulse = ScriptableObject.CreateInstance<CardDefinition>();
            Setup(pulse, "PULSE", "PULSE", 4, CardKind.PulseSpell, "Spell. Tap a Rift: instant damage to every enemy unit in/near it.");
            pulse.pulseDamage = 260f;
            pulse.pulseRadius = 1.7f;

            var parasite = ScriptableObject.CreateInstance<CardDefinition>();
            Setup(parasite, "PARASITE", "PARASITE", 3, CardKind.ParasiteSpell,
                "Spell. Tap an enemy unit: after 10s it takes heavy damage. If it dies, 2 Spawnlings join you.");
            parasite.parasiteDelay = 10f;
            parasite.parasiteDamage = 700f;
            parasite.parasiteSpawnCard = spawnling;
            parasite.parasiteSpawnCount = 2;
            parasite.targetPickRadius = 0.9f;

            return new[] { maw, blink, leech, anchor, hunter, swarm, pulse, parasite };
        }

        static CardDefinition Unit(string id, string displayName, int cost, string description,
            float hp, float dmg, float interval, float range, float sight, float speed, float core,
            UnitShape shape, float radius)
        {
            var c = ScriptableObject.CreateInstance<CardDefinition>();
            Setup(c, id, displayName, cost, CardKind.Unit, description);
            c.unit = new UnitStats
            {
                maxHp = hp,
                attackDamage = dmg,
                attackInterval = interval,
                attackRange = range,
                sightRange = sight,
                moveSpeed = speed,
                coreDamage = core,
                shape = shape,
                bodyRadius = radius,
            };
            return c;
        }

        static void Setup(CardDefinition c, string id, string displayName, int cost, CardKind kind, string description)
        {
            c.name = displayName;
            c.cardId = id;
            c.displayName = displayName;
            c.fluxCost = cost;
            c.kind = kind;
            c.description = description;
        }
    }
}
