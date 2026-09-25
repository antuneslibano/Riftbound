using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>Flux (mana) of one team: 0..max, regenerates over time.</summary>
    public class FluxSystem
    {
        public float Current;
        public float Max;
        public float RegenPerSecond;
        public float RegenMultiplier = 1f;

        /// <summary>Total Flux drained from this team by enemy Leeches (for debug/stats).</summary>
        public float TotalDrained;

        public FluxSystem(GameConfig cfg)
        {
            Max = cfg.fluxMax;
            Current = Mathf.Min(cfg.fluxStart, Max);
            RegenPerSecond = cfg.fluxRegenPerSecond;
        }

        public int Whole => Mathf.FloorToInt(Current + 0.0001f);

        public void Tick(float dt)
        {
            Current = Mathf.Min(Max, Current + RegenPerSecond * RegenMultiplier * dt);
        }

        public bool CanAfford(int cost) => Current + 0.0001f >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Current = Mathf.Max(0f, Current - cost);
            return true;
        }

        public void Add(float amount) => Current = Mathf.Clamp(Current + amount, 0f, Max);

        public void Drain(float amount)
        {
            float before = Current;
            Current = Mathf.Max(0f, Current - amount);
            TotalDrained += before - Current;
        }
    }
}
