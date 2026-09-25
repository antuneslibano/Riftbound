using System.Collections.Generic;
using UnityEngine;

namespace Riftbound.Config
{
    /// <summary>What happens to capture progress while both teams stand inside a Rift.</summary>
    public enum ContestRule
    {
        /// <summary>Capture progress freezes while contested.</summary>
        Freeze = 0,
        /// <summary>The team with more units inside keeps capturing, at reduced speed.</summary>
        MajorityWins = 1,
    }

    /// <summary>
    /// Central balance/rules asset. Every important number of the prototype lives here or in a
    /// <see cref="CardDefinition"/>. The instance used at runtime is
    /// Assets/Riftbound/Resources/RiftboundConfig.asset (loaded through Resources).
    /// </summary>
    [CreateAssetMenu(fileName = "RiftboundConfig", menuName = "Riftbound/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        public const string ResourcesPath = "RiftboundConfig";

        [Header("Match")]
        [Tooltip("Match length in seconds (180 = 03:00).")]
        public float matchDuration = 180f;
        [Tooltip("Starting (and max) Stability of each Core.")]
        public float coreStability = 100f;

        [Header("Flux")]
        public float fluxStart = 5f;
        public float fluxMax = 10f;
        [Tooltip("Flux gained per second.")]
        public float fluxRegenPerSecond = 1f;

        [Header("Rifts - capture")]
        [Tooltip("Radius of a Rift (units inside count for capture).")]
        public float riftRadius = 1.1f;
        [Tooltip("Seconds for ONE unit to take a Rift from neutral to controlled (same time to neutralize an enemy Rift).")]
        public float captureSeconds = 5.5f;
        [Tooltip("Capture speed bonus per extra unit inside (0.25 = +25% per unit).")]
        public float extraUnitCaptureBonus = 0.25f;
        [Tooltip("Units beyond this count do not speed up capture.")]
        public int maxUnitsCountedForCapture = 4;
        [Tooltip("Seconds for an empty Rift to drift back to its owner's full control (or to neutral).")]
        public float emptyRiftRecoverSeconds = 8f;
        public ContestRule contestRule = ContestRule.Freeze;
        [Tooltip("MajorityWins only: capture speed multiplier while contested.")]
        public float contestedMajoritySpeed = 0.5f;

        [Header("Deploy rules")]
        [Tooltip("Units may be deployed in your own half: beyond this distance from the centre line towards your Core.")]
        public float homeZoneMargin = 0.4f;
        [Tooltip("Units may also be deployed within this radius of any Rift you control.")]
        public float deployRadiusAroundOwnedRift = 1.6f;
        [Tooltip("Max simultaneous units per team.")]
        public int maxUnitsPerTeam = 24;

        [Header("Rift Break")]
        [Tooltip("Seconds the enemy Core stays vulnerable.")]
        public float riftBreakDuration = 8f;
        [Tooltip("When a Rift Break starts all Rifts shatter back to Neutral, so the map must be conquered again.")]
        public bool resetRiftsOnBreak = true;

        [Header("Rift Shift")]
        [Tooltip("Master switch of the Rift Shift mechanic.")]
        public bool riftShiftEnabled = true;
        public float riftShiftInterval = 45f;
        [Tooltip("How far (world units) Rifts A/B move on a 'drift' shift.")]
        public float riftShiftDriftDistance = 0.8f;

        [Header("Sudden Rift (tie at 00:00)")]
        public float suddenFluxRegenMultiplier = 2f;
        public float suddenCaptureSpeedMultiplier = 2f;
        [Tooltip("If nobody hits a Core during this time, the team with more Rifts wins (else draw).")]
        public float suddenRiftMaxDuration = 60f;

        [Header("Unit movement")]
        [Tooltip("Cost multiplier for walking off the network lines. Higher = units stick to connections.")]
        public float offNetworkCostMultiplier = 2.5f;
        [Tooltip("Engaged targets are dropped beyond sightRange * this.")]
        public float leashMultiplier = 1.5f;
        [Tooltip("During its own Rift Break a unit only fights enemies within this distance.")]
        public float breakAggroRange = 1.0f;

        [Header("Deck")]
        [Tooltip("8 cards used by BOTH the player and the bot.")]
        public List<CardDefinition> deck = new List<CardDefinition>();
        public int handSize = 4;
        public bool shuffleDeck = true;

        [Header("Bot")]
        public BotDifficulty defaultBotDifficulty = BotDifficulty.Normal;
        public BotProfile botEasy = BotProfile.Create(2.6f, 1.2f, 0.45f, false, false, 0, 1.2f);
        public BotProfile botNormal = BotProfile.Create(1.5f, 0.6f, 0.12f, true, true, 0, 0.5f);
        public BotProfile botHard = BotProfile.Create(0.7f, 0.25f, 0f, true, true, 0, 0.2f);

        [Header("Map")]
        public MapLayout map = new MapLayout();

        public BotProfile GetBotProfile(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy: return botEasy;
                case BotDifficulty.Hard: return botHard;
                default: return botNormal;
            }
        }

        /// <summary>Loads the config from Resources, falling back to code defaults if the asset is missing.</summary>
        public static GameConfig LoadOrDefault()
        {
            var cfg = Resources.Load<GameConfig>(ResourcesPath);
            if (cfg != null && cfg.deck != null && cfg.deck.Count > 0 && !cfg.deck.Contains(null))
                return cfg;

            Debug.LogWarning("[Riftbound] Resources/RiftboundConfig missing or invalid - using built-in defaults (DefaultContent).");
            return DefaultContent.CreateConfig();
        }
    }
}
