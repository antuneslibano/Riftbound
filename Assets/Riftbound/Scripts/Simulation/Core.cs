using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>A team's fortress. It only takes damage while it is vulnerable (enemy Rift Break).</summary>
    public class Core
    {
        public readonly Team Team;
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly float MaxStability;

        public float Stability;
        public bool Vulnerable;

        /// <summary>Seconds since the last hit (views use it for a flash).</summary>
        public float TimeSinceHit = 999f;

        public Core(Team team, Vector2 position, Vector2 size, float maxStability)
        {
            Team = team;
            Position = position;
            Size = size;
            MaxStability = maxStability;
            Stability = maxStability;
        }

        public bool Destroyed => Stability <= 0f;

        /// <summary>Distance from a point to the Core rectangle (0 when inside).</summary>
        public float DistanceTo(Vector2 point)
        {
            Vector2 d = point - Position;
            float dx = Mathf.Max(Mathf.Abs(d.x) - Size.x * 0.5f, 0f);
            float dy = Mathf.Max(Mathf.Abs(d.y) - Size.y * 0.5f, 0f);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Closest point of the Core rectangle to <paramref name="point"/>.</summary>
        public Vector2 ClosestPoint(Vector2 point)
        {
            Vector2 half = Size * 0.5f;
            return new Vector2(
                Mathf.Clamp(point.x, Position.x - half.x, Position.x + half.x),
                Mathf.Clamp(point.y, Position.y - half.y, Position.y + half.y));
        }

        /// <summary>Applies damage; returns the damage actually dealt.</summary>
        public float ApplyDamage(float amount)
        {
            if (amount <= 0f || Destroyed) return 0f;
            float dealt = Mathf.Min(amount, Stability);
            Stability -= dealt;
            TimeSinceHit = 0f;
            return dealt;
        }
    }
}
