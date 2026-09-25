using System;
using UnityEngine;

namespace Riftbound.Config
{
    public enum BotDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
    }

    /// <summary>
    /// Tuning of one bot difficulty. The bot never cheats: every profile uses the same Flux,
    /// deck and rules as the player. Difficulty only changes how often and how well it decides.
    /// </summary>
    [Serializable]
    public class BotProfile
    {
        [Tooltip("Seconds between two decisions.")]
        public float decisionInterval = 1.5f;
        [Tooltip("Random extra delay added to each decision (0..value seconds).")]
        public float decisionJitter = 0.5f;
        [Tooltip("Chance (0..1) that a decision is replaced by a random affordable card at a random valid spot.")]
        [Range(0f, 1f)] public float mistakeChance = 0.15f;
        [Tooltip("If true the bot evaluates threats and picks counter cards (Pulse vs groups, Hunter vs tanks...).")]
        public bool useCounters = true;
        [Tooltip("If true Pulse/Parasite are only used on worthwhile targets.")]
        public bool smartSpells = true;
        [Tooltip("When nothing urgent happens the bot waits until it has at least this much Flux (saving for pushes).")]
        public int saveFluxUntil = 0;
        [Tooltip("Random offset (world units) applied to deploy positions.")]
        public float placementJitter = 0.5f;

        public static BotProfile Create(float interval, float jitter, float mistake, bool counters, bool smart, int save, float placeJitter)
        {
            return new BotProfile
            {
                decisionInterval = interval,
                decisionJitter = jitter,
                mistakeChance = mistake,
                useCounters = counters,
                smartSpells = smart,
                saveFluxUntil = save,
                placementJitter = placeJitter,
            };
        }
    }
}
