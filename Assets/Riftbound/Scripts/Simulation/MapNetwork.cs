using System;
using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// The territorial network: 5 nodes (2 Cores + 3 Rifts) and the connections between them.
    /// Shortest paths are precomputed (Floyd-Warshall, 5 nodes) whenever the network changes,
    /// so path queries during Update are allocation free.
    /// </summary>
    public class MapNetwork
    {
        public const int NodeCount = 5;

        readonly Vector2[] positions = new Vector2[NodeCount];
        readonly bool[,] edges = new bool[NodeCount, NodeCount];
        readonly bool[,] highlighted = new bool[NodeCount, NodeCount];
        readonly float[,] dist = new float[NodeCount, NodeCount];
        readonly int[,] next = new int[NodeCount, NodeCount];

        /// <summary>Raised after <see cref="Commit"/>.</summary>
        public event Action Changed;

        /// <summary>Increments on every committed change (views use it to refresh lazily).</summary>
        public int Version { get; private set; }

        public MapNetwork(MapLayout layout)
        {
            for (int i = 0; i < NodeCount; i++)
                positions[i] = layout.GetPosition((NodeId)i);
            foreach (var e in layout.baseEdges)
                SetEdge(e.a, e.b, true);
            Commit();
        }

        public static bool IsRift(NodeId id) => id == NodeId.RiftA || id == NodeId.RiftB || id == NodeId.RiftC;

        public static NodeId CoreOf(Team team) => team == Team.Player ? NodeId.PlayerCore : NodeId.EnemyCore;

        public Vector2 GetPosition(NodeId id) => positions[(int)id];

        public void SetPosition(NodeId id, Vector2 pos) => positions[(int)id] = pos;

        public bool HasEdge(NodeId a, NodeId b) => edges[(int)a, (int)b];

        /// <summary>True if the connection was opened by the latest Rift Shift (drawn highlighted).</summary>
        public bool IsHighlighted(NodeId a, NodeId b) => highlighted[(int)a, (int)b];

        public void SetEdge(NodeId a, NodeId b, bool open, bool highlight = false)
        {
            edges[(int)a, (int)b] = edges[(int)b, (int)a] = open;
            highlighted[(int)a, (int)b] = highlighted[(int)b, (int)a] = open && highlight;
        }

        public void ClearHighlights()
        {
            Array.Clear(highlighted, 0, highlighted.Length);
        }

        /// <summary>Recomputes shortest paths and notifies listeners. Call after editing edges/positions.</summary>
        public void Commit()
        {
            for (int i = 0; i < NodeCount; i++)
            for (int j = 0; j < NodeCount; j++)
            {
                if (i == j) { dist[i, j] = 0f; next[i, j] = j; }
                else if (edges[i, j]) { dist[i, j] = Vector2.Distance(positions[i], positions[j]); next[i, j] = j; }
                else { dist[i, j] = float.PositiveInfinity; next[i, j] = -1; }
            }

            for (int k = 0; k < NodeCount; k++)
            for (int i = 0; i < NodeCount; i++)
            for (int j = 0; j < NodeCount; j++)
            {
                float d = dist[i, k] + dist[k, j];
                if (d < dist[i, j]) { dist[i, j] = d; next[i, j] = next[i, k]; }
            }

            Version++;
            Changed?.Invoke();
        }

        public float PathDistance(NodeId from, NodeId to) => dist[(int)from, (int)to];

        /// <summary>Next node to walk to on the shortest path; returns <paramref name="to"/> if unreachable (walk straight).</summary>
        public NodeId NextHop(NodeId from, NodeId to)
        {
            int n = next[(int)from, (int)to];
            return n < 0 ? to : (NodeId)n;
        }

        /// <summary>
        /// For a unit standing off the network: which node should it walk to first to reach <paramref name="target"/>?
        /// Off-network walking is penalised by <paramref name="offNetworkMultiplier"/> so units prefer joining the lines.
        /// </summary>
        public NodeId ChooseEntryNode(Vector2 pos, NodeId target, float offNetworkMultiplier)
        {
            NodeId best = target;
            float bestCost = float.PositiveInfinity;
            for (int i = 0; i < NodeCount; i++)
            {
                float graph = dist[i, (int)target];
                if (float.IsInfinity(graph)) continue;
                float cost = Vector2.Distance(pos, positions[i]) * offNetworkMultiplier + graph;
                if (cost < bestCost) { bestCost = cost; best = (NodeId)i; }
            }
            return best;
        }
    }
}
