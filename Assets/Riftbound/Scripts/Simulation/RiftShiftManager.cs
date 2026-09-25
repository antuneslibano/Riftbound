using System;
using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// RIFT SHIFT: every <c>riftShiftInterval</c> seconds the network changes. Each shift first restores the
    /// base network and then applies ONE modification. All modifications are point-symmetric, so the map
    /// stays fair for both sides. Toggle with <c>GameConfig.riftShiftEnabled</c> (or the debug panel).
    /// Add new shift types by extending <see cref="ShiftType"/> and <see cref="Apply"/>.
    /// </summary>
    public class RiftShiftManager
    {
        public enum ShiftType
        {
            FlankRoutesOpen = 0,
            CenterLinksCollapse = 1,
            RiftsDrift = 2,
            CoreLinksSevered = 3,
        }

        static readonly int TypeCount = Enum.GetValues(typeof(ShiftType)).Length;

        readonly Match match;
        readonly MapLayout layout;
        int lastType = -1;

        public bool Enabled;
        public float TimeToNext { get; private set; }
        public int ShiftCount { get; private set; }
        public string CurrentDescription { get; private set; } = "BASE NETWORK";

        /// <summary>Raised with a short human readable description.</summary>
        public event Action<string> Shifted;

        public RiftShiftManager(Match match)
        {
            this.match = match;
            layout = match.Config.map;
            Enabled = match.Config.riftShiftEnabled;
            TimeToNext = match.Config.riftShiftInterval;
        }

        public void Tick(float dt)
        {
            if (!Enabled) return;
            TimeToNext -= dt;
            if (TimeToNext <= 0f) Shift();
        }

        /// <summary>Performs a shift now (also used by the debug button).</summary>
        public void Shift()
        {
            TimeToNext = Mathf.Max(5f, match.Config.riftShiftInterval);

            int type;
            do { type = match.Rng.Next(TypeCount); } while (type == lastType && TypeCount > 1);
            lastType = type;

            RestoreBase();
            CurrentDescription = Apply((ShiftType)type);
            match.Network.Commit();
            ShiftCount++;
            Shifted?.Invoke(CurrentDescription);
        }

        void RestoreBase()
        {
            var net = match.Network;
            for (int a = 0; a < MapNetwork.NodeCount; a++)
            for (int b = a + 1; b < MapNetwork.NodeCount; b++)
                net.SetEdge((NodeId)a, (NodeId)b, false);
            foreach (var e in layout.baseEdges) net.SetEdge(e.a, e.b, true);
            net.ClearHighlights();
            for (int i = 0; i < MapNetwork.NodeCount; i++)
                net.SetPosition((NodeId)i, layout.GetPosition((NodeId)i));
        }

        string Apply(ShiftType type)
        {
            var net = match.Network;
            switch (type)
            {
                case ShiftType.FlankRoutesOpen:
                    net.SetEdge(NodeId.PlayerCore, NodeId.RiftB, true, true);
                    net.SetEdge(NodeId.EnemyCore, NodeId.RiftA, true, true);
                    return "FLANK ROUTES OPEN";

                case ShiftType.CenterLinksCollapse:
                    net.SetEdge(NodeId.RiftA, NodeId.RiftC, false);
                    net.SetEdge(NodeId.RiftB, NodeId.RiftC, false);
                    // Keep the side Rifts reachable through a new lateral link.
                    net.SetEdge(NodeId.PlayerCore, NodeId.RiftB, true, true);
                    net.SetEdge(NodeId.EnemyCore, NodeId.RiftA, true, true);
                    return "CENTER LINKS COLLAPSE";

                case ShiftType.RiftsDrift:
                {
                    float d = match.Config.riftShiftDriftDistance;
                    // Random direction; B mirrors A through the centre (point symmetry keeps fairness).
                    float ang = (float)match.Rng.NextDouble() * Mathf.PI * 2f;
                    Vector2 off = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * d;
                    Vector2 a = ClampInside(layout.riftA + off);
                    net.SetPosition(NodeId.RiftA, a);
                    net.SetPosition(NodeId.RiftB, -a + (layout.riftA + layout.riftB));
                    return "RIFTS DRIFT";
                }

                default:
                    net.SetEdge(NodeId.PlayerCore, NodeId.RiftC, false);
                    net.SetEdge(NodeId.EnemyCore, NodeId.RiftC, false);
                    return "CORE LINKS TO CENTER SEVERED";
            }
        }

        Vector2 ClampInside(Vector2 p)
        {
            float r = match.Config.riftRadius;
            Vector2 ext = layout.halfExtents;
            return new Vector2(Mathf.Clamp(p.x, -ext.x + r, ext.x - r), Mathf.Clamp(p.y, -ext.y + 2.5f, ext.y - 2.5f));
        }
    }
}
