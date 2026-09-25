using System;
using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    public enum MatchPhase
    {
        Running = 0,
        SuddenRift = 1,
        Ended = 2,
    }

    public enum FxKind
    {
        Pulse = 0,
        ParasiteApplied,
        ParasiteBurst,
        CoreHit,
        UnitDied,
    }

    /// <summary>One-shot visual event emitted by the simulation (views decide how to draw it).</summary>
    public struct FxEvent
    {
        public FxKind Kind;
        public Vector2 Position;
        public float Radius;
        public Team Team;
    }

    /// <summary>
    /// The whole match simulation in plain C# (no MonoBehaviour, no rendering).
    /// Controllers submit <see cref="PlayCardCommand"/>s, <see cref="GameBootstrap"/> calls <see cref="Tick"/> every frame,
    /// and views read the public state. This separation is what allows replacing the bot with network input later.
    /// </summary>
    public class Match
    {
        public readonly GameConfig Config;
        public readonly System.Random Rng;
        public readonly MapNetwork Network;
        public readonly Rift[] Rifts;
        public readonly UnitManager Units = new UnitManager();
        public readonly MatchStats Stats = new MatchStats();
        public readonly CardSystem Cards;
        public readonly RiftBreakManager RiftBreak;
        public readonly RiftShiftManager RiftShift;

        readonly Core[] cores = new Core[2];
        readonly FluxSystem[] flux = new FluxSystem[2];
        readonly DeckController[] decks = new DeckController[2];

        public MatchPhase Phase { get; private set; } = MatchPhase.Running;
        public float TimeRemaining { get; private set; }
        public float SuddenElapsed { get; private set; }
        public float Elapsed { get; private set; }
        public bool HasWinner { get; private set; }
        public Team Winner { get; private set; }
        public string EndReason { get; private set; } = "";

        public bool IsRunning => Phase != MatchPhase.Ended;
        public float CaptureSpeedMultiplier => Phase == MatchPhase.SuddenRift ? Config.suddenCaptureSpeedMultiplier : 1f;

        /// <summary>Visual one-shot events (pulse flash, parasite burst, core hit...).</summary>
        public event Action<FxEvent> Fx;
        /// <summary>Big centre-screen announcements: (title, subtitle, colour).</summary>
        public event Action<string, string, Color> Announced;
        public event Action Ended;

        public Match(GameConfig config, int seed)
        {
            Config = config;
            Rng = new System.Random(seed);
            Network = new MapNetwork(config.map);

            Rifts = new[]
            {
                new Rift(NodeId.RiftA, "RIFT A", Network, config.riftRadius),
                new Rift(NodeId.RiftB, "RIFT B", Network, config.riftRadius),
                new Rift(NodeId.RiftC, "RIFT C", Network, config.riftRadius),
            };

            cores[(int)Team.Player] = new Core(Team.Player, config.map.playerCore, config.map.coreSize, config.coreStability);
            cores[(int)Team.Enemy] = new Core(Team.Enemy, config.map.enemyCore, config.map.coreSize, config.coreStability);

            for (int t = 0; t < 2; t++)
            {
                flux[t] = new FluxSystem(config);
                decks[t] = new DeckController(config.deck, config.handSize, config.shuffleDeck, Rng);
            }

            Cards = new CardSystem(this);
            RiftBreak = new RiftBreakManager(this);
            RiftShift = new RiftShiftManager(this);

            RiftBreak.Started += team => Announced?.Invoke("RIFT BREAK!",
                team == Team.Player ? "ENEMY CORE VULNERABLE - ATTACK!" : "YOUR CORE IS VULNERABLE!", Palette.Of(team));
            RiftShift.Shifted += desc => Announced?.Invoke("RIFT SHIFT!", desc, Palette.Contested);

            TimeRemaining = config.matchDuration;
        }

        // ------------------------------------------------------------------ accessors

        public Core GetCore(Team t) => cores[(int)t];
        public FluxSystem GetFlux(Team t) => flux[(int)t];
        public DeckController GetDeck(Team t) => decks[(int)t];

        public Rift GetRift(NodeId id)
        {
            for (int i = 0; i < Rifts.Length; i++)
                if (Rifts[i].Node == id) return Rifts[i];
            return null;
        }

        public int OwnedRiftCount(Team t)
        {
            int n = 0;
            for (int i = 0; i < Rifts.Length; i++)
                if (Rifts[i].Owner.Is(t)) n++;
            return n;
        }

        // ------------------------------------------------------------------ commands

        /// <summary>Entry point for every controller (player input, bot, future network).</summary>
        public PlayResult Submit(PlayCardCommand cmd) => Cards.Execute(cmd);

        // ------------------------------------------------------------------ simulation

        public void Tick(float dt)
        {
            if (Phase == MatchPhase.Ended || dt <= 0f) return;
            Elapsed += dt;

            UpdateClock(dt);
            if (Phase == MatchPhase.Ended) return;

            float regenMul = Phase == MatchPhase.SuddenRift ? Config.suddenFluxRegenMultiplier : 1f;
            for (int t = 0; t < 2; t++)
            {
                flux[t].RegenMultiplier = regenMul;
                flux[t].Tick(dt);
            }

            RiftCaptureSystem.UpdatePresence(Rifts, Units);
            RiftCaptureSystem.Tick(Rifts, Config, CaptureSpeedMultiplier, dt);
            RiftBreak.Tick(dt);
            RiftShift.Tick(dt);

            var all = Units.All;
            for (int i = 0; i < all.Count; i++) if (all[i].Alive) UnitBrain.Tick(all[i], this, dt);
            for (int i = 0; i < all.Count; i++) if (all[i].Alive) UnitMotor.Tick(all[i], this, dt);
            UnitMotor.Separate(Units, Config.map, dt);
            for (int i = 0; i < all.Count && Phase != MatchPhase.Ended; i++)
            {
                var u = all[i];
                if (!u.Alive) continue;
                UnitCombat.Tick(u, this, dt);
                UnitCombat.TickParasite(u, this, dt);
                TickLeech(u, dt);
            }
            Units.RemoveDead();

            cores[0].TimeSinceHit += dt;
            cores[1].TimeSinceHit += dt;
            Stats.Tick(dt);
        }

        void UpdateClock(float dt)
        {
            if (Phase == MatchPhase.Running)
            {
                TimeRemaining = Mathf.Max(0f, TimeRemaining - dt);
                if (TimeRemaining > 0f) return;

                float p = cores[(int)Team.Player].Stability;
                float e = cores[(int)Team.Enemy].Stability;
                if (Mathf.Abs(p - e) > 0.001f)
                {
                    End(p > e ? Team.Player : Team.Enemy, false, "TIME UP - HIGHER CORE STABILITY");
                    return;
                }

                Phase = MatchPhase.SuddenRift;
                SuddenElapsed = 0f;
                Announced?.Invoke("SUDDEN RIFT!", "FIRST CORE DAMAGE WINS", Palette.Contested);
            }
            else if (Phase == MatchPhase.SuddenRift)
            {
                SuddenElapsed += dt;
                if (SuddenElapsed < Config.suddenRiftMaxDuration) return;

                int p = OwnedRiftCount(Team.Player);
                int e = OwnedRiftCount(Team.Enemy);
                if (p == e) End(Team.Player, true, "SUDDEN RIFT EXPIRED - DRAW");
                else End(p > e ? Team.Player : Team.Enemy, false, "SUDDEN RIFT EXPIRED - MORE RIFTS CONTROLLED");
            }
        }

        void TickLeech(Unit u, float dt)
        {
            float drain = u.Stats.fluxDrainPerSecond;
            if (drain <= 0f || u.InsideRift == null) return;
            Team opp = u.Team.Opponent();
            if (u.InsideRift.Owner.Is(opp)) flux[(int)opp].Drain(drain * dt);
        }

        // ------------------------------------------------------------------ actions used by systems

        /// <summary>Spawns a unit of a card (respects the unit cap). Returns null if the cap is reached.</summary>
        public Unit SpawnUnit(CardDefinition card, Team team, Vector2 pos)
        {
            if (Units.Count(team) >= Config.maxUnitsPerTeam) return null;
            Vector2 ext = Config.map.halfExtents;
            pos = new Vector2(Mathf.Clamp(pos.x, -ext.x, ext.x), Mathf.Clamp(pos.y, -ext.y, ext.y));
            var u = Units.Spawn(card, card.unit, team, pos, Rng);
            Stats.UnitsSpawned[(int)team]++;
            return u;
        }

        /// <summary>Unit-to-unit attack damage.</summary>
        public void DealDamage(Unit attacker, Unit victim, float amount)
        {
            float dealt = ApplyRawDamage(victim, amount, attacker.Team);
            attacker.DamageDealt += dealt;
        }

        /// <summary>Applies damage to a unit (Anchor's in-Rift reduction included). Returns damage dealt.</summary>
        public float ApplyRawDamage(Unit victim, float amount, Team source)
        {
            if (!victim.Alive || amount <= 0f) return 0f;
            if (victim.InsideRift != null && victim.Stats.riftDamageReduction > 0f)
                amount *= 1f - victim.Stats.riftDamageReduction;

            float dealt = Mathf.Min(amount, victim.Hp);
            victim.Hp -= amount;
            victim.TimeSinceHit = 0f;
            Stats.RecordDamage(source, dealt);

            if (victim.Hp <= 0f)
            {
                Stats.UnitsLost[(int)victim.Team]++;
                RaiseFx(FxKind.UnitDied, victim.Position, victim.Stats.bodyRadius, victim.Team);
            }
            return dealt;
        }

        /// <summary>Damage to the Core of <paramref name="attacker"/>'s opponent. Only works while it is vulnerable.</summary>
        public void DamageCore(Team attacker, float amount, Unit source)
        {
            var core = cores[(int)attacker.Opponent()];
            if (!core.Vulnerable || !IsRunning) return;
            ApplyCoreDamage(attacker, amount);
            if (source != null) source.DamageDealt += amount;
        }

        void ApplyCoreDamage(Team attacker, float amount)
        {
            var core = cores[(int)attacker.Opponent()];
            float dealt = core.ApplyDamage(amount);
            if (dealt <= 0f) return;
            Stats.CoreDamageDealt[(int)attacker] += dealt;
            RaiseFx(FxKind.CoreHit, core.Position, core.Size.x * 0.5f, attacker);

            if (core.Destroyed) End(attacker, false, core.Team == Team.Enemy ? "ENEMY CORE DESTROYED" : "PLAYER CORE DESTROYED");
            else if (Phase == MatchPhase.SuddenRift) End(attacker, false, "SUDDEN RIFT - FIRST CORE DAMAGE");
        }

        public void RaiseFx(FxKind kind, Vector2 pos, float radius, Team team)
        {
            Fx?.Invoke(new FxEvent { Kind = kind, Position = pos, Radius = radius, Team = team });
        }

        public void RethinkAllUnits()
        {
            var all = Units.All;
            for (int i = 0; i < all.Count; i++) UnitBrain.Rethink(all[i]);
        }

        void End(Team winner, bool draw, string reason)
        {
            if (Phase == MatchPhase.Ended) return;
            Phase = MatchPhase.Ended;
            HasWinner = !draw;
            Winner = winner;
            EndReason = reason;
            RiftBreak.Stop();
            Ended?.Invoke();
        }

        // ------------------------------------------------------------------ debug helpers

        /// <summary>Free spawn (no Flux, no placement rules) for testing.</summary>
        public void DebugSpawn(CardDefinition card, Team team)
        {
            if (card == null || card.kind != CardKind.Unit) return;
            Vector2 home = cores[(int)team].Position + new Vector2(((float)Rng.NextDouble() - 0.5f) * 2f, team.Forward() * 1.2f);
            Cards.SpawnCardUnits(card, team, home);
        }

        /// <summary>Direct Core damage ignoring vulnerability (debug only).</summary>
        public void DebugDamageCore(Team victim, float amount)
        {
            if (!IsRunning) return;
            ApplyCoreDamage(victim.Opponent(), amount);
        }

        public void DebugForceEnd(Team winner) => End(winner, false, "DEBUG");
    }
}
