using System;
using System.Collections.Generic;
using UnityEngine;

namespace Riftbound.Config
{
    /// <summary>Fixed node identifiers of the territorial network.</summary>
    public enum NodeId
    {
        PlayerCore = 0,
        EnemyCore = 1,
        RiftA = 2,
        RiftB = 3,
        RiftC = 4,
    }

    [Serializable]
    public struct EdgeDef
    {
        public NodeId a;
        public NodeId b;

        public EdgeDef(NodeId a, NodeId b)
        {
            this.a = a;
            this.b = b;
        }
    }

    /// <summary>
    /// World-space layout of the map. The player sits at the bottom (negative Y), the enemy at the top.
    /// The default layout is point-symmetric around (0,0) so neither side has a positional advantage:
    /// Rift A is the player's "home" Rift, Rift B the enemy's, Rift C the centre.
    /// </summary>
    [Serializable]
    public class MapLayout
    {
        [Header("Node positions (world units)")]
        public Vector2 playerCore = new Vector2(0f, -5.0f);
        public Vector2 enemyCore = new Vector2(0f, 5.0f);
        public Vector2 riftA = new Vector2(-2.6f, -1.3f);
        public Vector2 riftB = new Vector2(2.6f, 1.3f);
        public Vector2 riftC = new Vector2(0f, 0f);

        [Header("Core rectangle size")]
        public Vector2 coreSize = new Vector2(2.6f, 0.8f);

        [Header("Playable bounds (units are clamped inside)")]
        public Vector2 halfExtents = new Vector2(3.9f, 5.8f);

        [Header("Connections active at match start")]
        public List<EdgeDef> baseEdges = new List<EdgeDef>
        {
            new EdgeDef(NodeId.PlayerCore, NodeId.RiftA),
            new EdgeDef(NodeId.PlayerCore, NodeId.RiftC),
            new EdgeDef(NodeId.RiftA, NodeId.RiftC),
            new EdgeDef(NodeId.RiftC, NodeId.RiftB),
            new EdgeDef(NodeId.EnemyCore, NodeId.RiftB),
            new EdgeDef(NodeId.EnemyCore, NodeId.RiftC),
        };

        public Vector2 GetPosition(NodeId id)
        {
            switch (id)
            {
                case NodeId.PlayerCore: return playerCore;
                case NodeId.EnemyCore: return enemyCore;
                case NodeId.RiftA: return riftA;
                case NodeId.RiftB: return riftB;
                default: return riftC;
            }
        }
    }
}
