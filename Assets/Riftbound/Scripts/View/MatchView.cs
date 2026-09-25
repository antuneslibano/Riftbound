using System.Collections.Generic;
using Riftbound.Config;
using Riftbound.Simulation;
using UnityEngine;

namespace Riftbound.View
{
    /// <summary>
    /// Builds and updates every world visual of a match from the simulation state:
    /// field, network lines, Rifts, Cores, pooled unit views, one-shot FX and the placement preview.
    /// Read-only with respect to the simulation.
    /// </summary>
    public class MatchView : MonoBehaviour
    {
        struct FxInstance
        {
            public SpriteRenderer Sprite;
            public float Age, Life, StartScale, EndScale;
            public Color Color;
        }

        Match match;
        readonly List<RiftView> riftViews = new List<RiftView>();
        readonly List<CoreView> coreViews = new List<CoreView>();
        readonly Dictionary<Unit, UnitView> unitViews = new Dictionary<Unit, UnitView>();
        readonly Stack<UnitView> unitPool = new Stack<UnitView>();
        readonly List<FxInstance> fx = new List<FxInstance>();
        readonly Stack<SpriteRenderer> fxPool = new Stack<SpriteRenderer>();
        readonly SpriteRenderer[,] edgeLines = new SpriteRenderer[MapNetwork.NodeCount, MapNetwork.NodeCount];
        int shownNetworkVersion = -1;

        Transform unitsRoot, fxRoot;

        // Placement preview
        SpriteRenderer homeZone, ghost, ghostRing;
        readonly List<SpriteRenderer> riftZones = new List<SpriteRenderer>();

        public bool DebugMode;

        public void Build(Match m)
        {
            match = m;
            var cfg = m.Config;
            Vector2 ext = cfg.map.halfExtents;

            // Field background + centre line.
            var field = ShapeLibrary.CreateSprite(transform, "Field", ShapeLibrary.Shape.Pixel, Palette.FieldTint, 0);
            field.transform.localScale = new Vector3(ext.x * 2f + 0.3f, ext.y * 2f + 0.3f, 1f);
            var center = ShapeLibrary.CreateSprite(transform, "CenterLine", ShapeLibrary.Shape.Pixel, new Color(1f, 1f, 1f, 0.06f), 1);
            center.transform.localScale = new Vector3(ext.x * 2f, 0.03f, 1f);

            // Deploy zone overlays (shown only while a unit card is selected).
            homeZone = ShapeLibrary.CreateSprite(transform, "HomeZone", ShapeLibrary.Shape.Pixel, Palette.Player.WithAlpha(0.1f), 5);
            float zoneH = ext.y - cfg.homeZoneMargin;
            homeZone.transform.localScale = new Vector3(ext.x * 2f, zoneH, 1f);
            homeZone.transform.localPosition = new Vector3(0f, -cfg.homeZoneMargin - zoneH * 0.5f, 0f);
            homeZone.enabled = false;
            foreach (var r in m.Rifts)
            {
                var z = ShapeLibrary.CreateSprite(transform, "RiftZone", ShapeLibrary.Shape.Circle, Palette.Player.WithAlpha(0.1f), 5);
                z.transform.localScale = Vector3.one * cfg.deployRadiusAroundOwnedRift * 2f;
                z.enabled = false;
                riftZones.Add(z);
            }

            // Network lines: one pooled line per node pair, toggled when the network changes.
            for (int a = 0; a < MapNetwork.NodeCount; a++)
            for (int b = a + 1; b < MapNetwork.NodeCount; b++)
            {
                var line = ShapeLibrary.CreateSprite(transform, "Edge_" + (NodeId)a + "_" + (NodeId)b, ShapeLibrary.Shape.Pixel, Palette.Edge, 10);
                line.enabled = false;
                edgeLines[a, b] = line;
            }

            foreach (var r in m.Rifts)
            {
                var go = new GameObject(r.Name);
                go.transform.SetParent(transform, false);
                var v = go.AddComponent<RiftView>();
                v.Build(r);
                riftViews.Add(v);
            }

            foreach (Team t in new[] { Team.Player, Team.Enemy })
            {
                var go = new GameObject(t + "Core");
                go.transform.SetParent(transform, false);
                var v = go.AddComponent<CoreView>();
                v.Build(m.GetCore(t));
                coreViews.Add(v);
            }

            unitsRoot = new GameObject("Units").transform;
            unitsRoot.SetParent(transform, false);
            fxRoot = new GameObject("Fx").transform;
            fxRoot.SetParent(transform, false);

            ghostRing = ShapeLibrary.CreateSprite(transform, "GhostRing", ShapeLibrary.Shape.Ring, Palette.Valid, 70);
            ghost = ShapeLibrary.CreateSprite(transform, "Ghost", ShapeLibrary.Shape.Circle, Palette.Valid, 71);
            ghost.enabled = ghostRing.enabled = false;

            m.Units.Spawned += OnUnitSpawned;
            m.Units.Died += OnUnitDied;
            m.Fx += OnFx;
            for (int i = 0; i < m.Units.All.Count; i++) OnUnitSpawned(m.Units.All[i]);

            SyncNetwork();
        }

        void OnDestroy()
        {
            if (match == null) return;
            match.Units.Spawned -= OnUnitSpawned;
            match.Units.Died -= OnUnitDied;
            match.Fx -= OnFx;
        }

        void OnUnitSpawned(Unit u)
        {
            UnitView v;
            if (unitPool.Count > 0) v = unitPool.Pop();
            else
            {
                var go = new GameObject("Unit");
                go.transform.SetParent(unitsRoot, false);
                v = go.AddComponent<UnitView>();
                v.Build();
            }
            v.gameObject.name = u.Team + "_" + u.Name + "_" + u.Id;
            v.Bind(u);
            unitViews[u] = v;
        }

        void OnUnitDied(Unit u)
        {
            if (!unitViews.TryGetValue(u, out var v)) return;
            unitViews.Remove(u);
            v.Unbind();
            unitPool.Push(v);
        }

        // ------------------------------------------------------------------ per frame

        public void Sync(float unscaledDt)
        {
            float time = Time.unscaledTime;
            if (shownNetworkVersion != match.Network.Version) SyncNetwork();

            for (int i = 0; i < riftViews.Count; i++) riftViews[i].Sync(time);
            for (int i = 0; i < coreViews.Count; i++) coreViews[i].Sync(time);
            foreach (var kv in unitViews) kv.Value.Sync(DebugMode, unscaledDt);

            // Highlighted (new) edges pulse.
            for (int a = 0; a < MapNetwork.NodeCount; a++)
            for (int b = a + 1; b < MapNetwork.NodeCount; b++)
            {
                var line = edgeLines[a, b];
                if (!line.enabled) continue;
                bool hl = match.Network.IsHighlighted((NodeId)a, (NodeId)b);
                line.color = hl ? Color.Lerp(Palette.EdgeNew, Palette.Edge, Mathf.PingPong(time * 1.5f, 1f)) : Palette.Edge;
            }

            TickFx(unscaledDt);
        }

        void SyncNetwork()
        {
            shownNetworkVersion = match.Network.Version;
            var net = match.Network;
            for (int a = 0; a < MapNetwork.NodeCount; a++)
            for (int b = a + 1; b < MapNetwork.NodeCount; b++)
            {
                var line = edgeLines[a, b];
                bool open = net.HasEdge((NodeId)a, (NodeId)b);
                line.enabled = open;
                if (open) ShapeLibrary.SetLine(line.transform, net.GetPosition((NodeId)a), net.GetPosition((NodeId)b), 0.09f);
            }
        }

        // ------------------------------------------------------------------ placement preview

        /// <summary>Shows where the player's selected card can be played.</summary>
        public void ShowDeployZones(Team team, bool show)
        {
            homeZone.enabled = show;
            for (int i = 0; i < riftZones.Count; i++)
            {
                var r = match.Rifts[i];
                bool on = show && r.Owner.Is(team);
                riftZones[i].enabled = on;
                if (on) riftZones[i].transform.localPosition = new Vector3(r.Position.x, r.Position.y, 0f);
            }
        }

        public void ShowGhost(CardDefinition card, Vector2 pos, bool valid, Vector2 snap)
        {
            Color c = valid ? Palette.Valid : Palette.Invalid;
            ghostRing.enabled = true;
            ghostRing.color = c;
            switch (card.kind)
            {
                case CardKind.Unit:
                    ghost.enabled = true;
                    ghost.sprite = ShapeLibrary.Get(card.unit.shape);
                    ghost.color = c.WithAlpha(0.55f);
                    ghost.transform.localPosition = pos;
                    ghost.transform.localScale = Vector3.one * card.unit.bodyRadius * 2f;
                    ghostRing.transform.localPosition = pos;
                    ghostRing.transform.localScale = Vector3.one * (card.unit.bodyRadius * 2f + (card.spawnCount > 1 ? card.spawnSpread * 2f : 0.3f));
                    break;
                case CardKind.PulseSpell:
                    ghost.enabled = false;
                    ghostRing.transform.localPosition = valid ? snap : pos;
                    ghostRing.transform.localScale = Vector3.one * card.pulseRadius * 2f;
                    break;
                default:
                    ghost.enabled = false;
                    ghostRing.transform.localPosition = valid ? snap : pos;
                    ghostRing.transform.localScale = Vector3.one * 0.9f;
                    break;
            }
        }

        public void HideGhost()
        {
            ghost.enabled = false;
            ghostRing.enabled = false;
        }

        /// <summary>Brief red ring where an invalid play was attempted.</summary>
        public void FlashInvalid(Vector2 pos)
        {
            SpawnFx(ShapeLibrary.Shape.Ring, pos, 0.5f, 0.9f, Palette.Invalid, 0.45f);
        }

        // ------------------------------------------------------------------ fx

        void OnFx(FxEvent e)
        {
            Color tc = Palette.Of(e.Team);
            switch (e.Kind)
            {
                case FxKind.Pulse:
                    SpawnFx(ShapeLibrary.Shape.Circle, e.Position, e.Radius * 2f, e.Radius * 2.1f, tc.WithAlpha(0.45f), 0.5f);
                    SpawnFx(ShapeLibrary.Shape.Ring, e.Position, e.Radius * 1.4f, e.Radius * 2.1f, Color.white.WithAlpha(0.8f), 0.4f);
                    break;
                case FxKind.ParasiteApplied:
                    SpawnFx(ShapeLibrary.Shape.Ring, e.Position, 0.3f, 1.0f, Palette.Parasite, 0.4f);
                    break;
                case FxKind.ParasiteBurst:
                    SpawnFx(ShapeLibrary.Shape.Circle, e.Position, 0.4f, 1.6f, Palette.Parasite.WithAlpha(0.6f), 0.45f);
                    break;
                case FxKind.CoreHit:
                    SpawnFx(ShapeLibrary.Shape.Ring, e.Position, 1.0f, e.Radius * 2.4f, tc.WithAlpha(0.7f), 0.35f);
                    break;
                case FxKind.UnitDied:
                    SpawnFx(ShapeLibrary.Shape.Ring, e.Position, e.Radius * 2f, e.Radius * 4f, tc.WithAlpha(0.6f), 0.3f);
                    break;
            }
        }

        void SpawnFx(ShapeLibrary.Shape shape, Vector2 pos, float startScale, float endScale, Color color, float life)
        {
            var sr = fxPool.Count > 0 ? fxPool.Pop() : ShapeLibrary.CreateSprite(fxRoot, "Fx", shape, color, 60);
            sr.sprite = ShapeLibrary.Get(shape);
            sr.enabled = true;
            sr.transform.localPosition = pos;
            sr.transform.localScale = Vector3.one * startScale;
            sr.color = color;
            fx.Add(new FxInstance { Sprite = sr, Life = life, StartScale = startScale, EndScale = endScale, Color = color });
        }

        void TickFx(float dt)
        {
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i];
                f.Age += Mathf.Max(dt, 0f);
                float t = f.Age / f.Life;
                if (t >= 1f)
                {
                    f.Sprite.enabled = false;
                    fxPool.Push(f.Sprite);
                    fx.RemoveAt(i);
                    continue;
                }
                f.Sprite.transform.localScale = Vector3.one * Mathf.Lerp(f.StartScale, f.EndScale, t);
                f.Sprite.color = f.Color.WithAlpha(f.Color.a * (1f - t));
                fx[i] = f;
            }
        }
    }
}
