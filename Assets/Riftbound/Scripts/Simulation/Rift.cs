using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// State of one Rift. Capture is modelled as a single value <see cref="Control"/> in [-1, +1]:
    /// +1 = fully Player, -1 = fully Enemy. Ownership flips only at the extremes, and an owned Rift
    /// becomes Neutral again when its control crosses 0. Updated by <see cref="RiftCaptureSystem"/>.
    /// </summary>
    public class Rift
    {
        public readonly NodeId Node;
        public readonly string Name;
        public readonly MapNetwork Network;

        public float Radius;
        public float Control;
        public RiftOwner Owner = RiftOwner.Neutral;

        /// <summary>Units of each team inside the Rift this frame (index = (int)Team).</summary>
        public readonly int[] Presence = new int[2];

        public bool Contested => Presence[0] > 0 && Presence[1] > 0;

        public Vector2 Position => Network.GetPosition(Node);

        public Rift(NodeId node, string name, MapNetwork network, float radius)
        {
            Node = node;
            Name = name;
            Network = network;
            Radius = radius;
        }

        public bool Contains(Vector2 point, float extra = 0f)
        {
            float r = Radius + extra;
            return (point - Position).sqrMagnitude <= r * r;
        }

        /// <summary>Capture progress of <paramref name="team"/> in 0..1 (only meaningful towards that team).</summary>
        public float ProgressFor(Team team) => Mathf.Clamp01(Control * team.Forward());

        public void ResetToNeutral()
        {
            Control = 0f;
            Owner = RiftOwner.Neutral;
        }

        public void ForceOwner(Team team)
        {
            Owner = team.ToOwner();
            Control = team.Forward();
        }
    }
}
