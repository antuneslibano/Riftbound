using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// Rift capture rules (all tunable in <see cref="GameConfig"/>):
    /// <list type="bullet">
    /// <item>Only one team inside → control moves towards that team. 1 unit = <c>captureSeconds</c> from 0 to full,
    /// each extra unit adds <c>extraUnitCaptureBonus</c> (up to <c>maxUnitsCountedForCapture</c>).</item>
    /// <item>An enemy-owned Rift must first be neutralised (control back to 0 → Neutral), then captured.</item>
    /// <item>Both teams inside → CONTESTED: progress freezes (or majority captures slower, see <see cref="ContestRule"/>).</item>
    /// <item>Empty → control slowly drifts back to its owner's full value, or to 0 if neutral.</item>
    /// </list>
    /// </summary>
    public static class RiftCaptureSystem
    {
        /// <summary>Recomputes which units are inside which Rift.</summary>
        public static void UpdatePresence(Rift[] rifts, UnitManager units)
        {
            for (int r = 0; r < rifts.Length; r++)
            {
                rifts[r].Presence[0] = 0;
                rifts[r].Presence[1] = 0;
            }

            var all = units.All;
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                u.InsideRift = null;
                if (!u.Alive) continue;
                for (int r = 0; r < rifts.Length; r++)
                {
                    if (!rifts[r].Contains(u.Position)) continue;
                    u.InsideRift = rifts[r];
                    rifts[r].Presence[(int)u.Team]++;
                    break;
                }
            }
        }

        public static void Tick(Rift[] rifts, GameConfig cfg, float speedMultiplier, float dt)
        {
            float baseRate = cfg.captureSeconds > 0.01f ? 1f / cfg.captureSeconds : 100f;
            for (int i = 0; i < rifts.Length; i++)
                TickRift(rifts[i], cfg, baseRate * speedMultiplier, dt);
        }

        static void TickRift(Rift r, GameConfig cfg, float baseRate, float dt)
        {
            int p = r.Presence[(int)Team.Player];
            int e = r.Presence[(int)Team.Enemy];
            float delta = 0f;

            if (p > 0 && e > 0)
            {
                if (cfg.contestRule == ContestRule.MajorityWins && p != e)
                {
                    int diff = Mathf.Abs(p - e);
                    float dir = p > e ? 1f : -1f;
                    delta = dir * Rate(diff, cfg, baseRate) * cfg.contestedMajoritySpeed * dt;
                }
                // Freeze: delta stays 0.
            }
            else if (p > 0)
            {
                delta = Rate(p, cfg, baseRate) * dt;
            }
            else if (e > 0)
            {
                delta = -Rate(e, cfg, baseRate) * dt;
            }
            else
            {
                // Empty: recover towards the owner's full value (or neutral).
                float target = r.Owner == RiftOwner.Player ? 1f : r.Owner == RiftOwner.Enemy ? -1f : 0f;
                float recover = cfg.emptyRiftRecoverSeconds > 0.01f ? dt / cfg.emptyRiftRecoverSeconds : 1f;
                r.Control = Mathf.MoveTowards(r.Control, target, recover);
                return;
            }

            if (delta == 0f) return;

            float before = r.Control;
            float after = Mathf.Clamp(before + delta, -1f, 1f);

            // Neutralisation: an owned Rift loses its owner as soon as control crosses 0 against it.
            if (r.Owner == RiftOwner.Player && after <= 0f) { r.Owner = RiftOwner.Neutral; after = Mathf.Min(after, 0f); }
            if (r.Owner == RiftOwner.Enemy && after >= 0f) { r.Owner = RiftOwner.Neutral; after = Mathf.Max(after, 0f); }

            // Full capture.
            if (after >= 1f) { after = 1f; r.Owner = RiftOwner.Player; }
            if (after <= -1f) { after = -1f; r.Owner = RiftOwner.Enemy; }

            r.Control = after;
        }

        static float Rate(int units, GameConfig cfg, float baseRate)
        {
            int counted = Mathf.Clamp(units, 1, Mathf.Max(1, cfg.maxUnitsCountedForCapture));
            return baseRate * (1f + cfg.extraUnitCaptureBonus * (counted - 1));
        }
    }
}
