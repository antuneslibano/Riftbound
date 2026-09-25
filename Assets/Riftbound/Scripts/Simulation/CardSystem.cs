using Riftbound.Config;
using UnityEngine;

namespace Riftbound.Simulation
{
    /// <summary>
    /// Validates and executes <see cref="PlayCardCommand"/>s with identical rules for every controller.
    /// Deploy rule for units: your own half of the map (beyond <c>homeZoneMargin</c> from the centre line)
    /// OR within <c>deployRadiusAroundOwnedRift</c> of any Rift you control. Controlling territory therefore
    /// extends where you can deploy.
    /// </summary>
    public class CardSystem
    {
        readonly Match match;

        public CardSystem(Match match)
        {
            this.match = match;
        }

        /// <summary>Validation without side effects (used for placement previews and by the bot).</summary>
        public PlayResult Validate(PlayCardCommand cmd)
        {
            if (!match.IsRunning) return PlayResult.MatchNotRunning;
            var card = match.GetDeck(cmd.Team).GetSlot(cmd.HandSlot);
            if (card == null) return PlayResult.InvalidSlot;
            return ValidateCard(card, cmd.Team, cmd.Position, true);
        }

        public PlayResult ValidateCard(CardDefinition card, Team team, Vector2 pos, bool checkFlux)
        {
            if (checkFlux && !match.GetFlux(team).CanAfford(card.fluxCost)) return PlayResult.NotEnoughFlux;

            switch (card.kind)
            {
                case CardKind.Unit:
                    if (match.Units.Count(team) + card.spawnCount > match.Config.maxUnitsPerTeam) return PlayResult.UnitLimit;
                    return IsValidDeploy(team, pos) ? PlayResult.Ok : PlayResult.InvalidPosition;

                case CardKind.PulseSpell:
                    return FindRiftAt(pos) != null ? PlayResult.Ok : PlayResult.NoRiftThere;

                case CardKind.ParasiteSpell:
                    var target = match.Units.FindEnemyNear(team, pos, card.targetPickRadius);
                    return target != null && !target.Infected ? PlayResult.Ok : PlayResult.NoEnemyUnitThere;
            }
            return PlayResult.InvalidSlot;
        }

        public PlayResult Execute(PlayCardCommand cmd)
        {
            var result = Validate(cmd);
            if (result != PlayResult.Ok) return result;

            var deck = match.GetDeck(cmd.Team);
            var card = deck.GetSlot(cmd.HandSlot);
            match.GetFlux(cmd.Team).TrySpend(card.fluxCost);
            deck.Consume(cmd.HandSlot);
            match.Stats.CardsPlayed[(int)cmd.Team]++;

            switch (card.kind)
            {
                case CardKind.Unit:
                    SpawnCardUnits(card, cmd.Team, cmd.Position);
                    break;
                case CardKind.PulseSpell:
                    CastPulse(card, cmd.Team, FindRiftAt(cmd.Position));
                    break;
                case CardKind.ParasiteSpell:
                    CastParasite(card, cmd.Team, match.Units.FindEnemyNear(cmd.Team, cmd.Position, card.targetPickRadius));
                    break;
            }
            return PlayResult.Ok;
        }

        public void SpawnCardUnits(CardDefinition card, Team team, Vector2 pos)
        {
            int n = Mathf.Max(1, card.spawnCount);
            for (int i = 0; i < n; i++)
            {
                Vector2 p = pos;
                if (n > 1)
                {
                    float a = i * Mathf.PI * 2f / n + Mathf.PI * 0.25f;
                    p += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * card.spawnSpread;
                }
                match.SpawnUnit(card, team, p);
            }
        }

        void CastPulse(CardDefinition card, Team team, Rift rift)
        {
            if (rift == null) return;
            Vector2 c = rift.Position;
            float r = card.pulseRadius;
            var enemies = match.Units.OfTeam(team.Opponent());
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e.Alive && Vector2.Distance(e.Position, c) - e.Stats.bodyRadius <= r)
                    match.ApplyRawDamage(e, card.pulseDamage, team);
            }
            match.RaiseFx(FxKind.Pulse, c, r, team);
        }

        void CastParasite(CardDefinition card, Team team, Unit target)
        {
            if (target == null) return;
            target.Infected = true;
            target.ParasiteTimer = card.parasiteDelay;
            target.ParasiteDamage = card.parasiteDamage;
            target.ParasiteOwner = team;
            target.ParasiteSpawnCard = card.parasiteSpawnCard;
            target.ParasiteSpawnCount = card.parasiteSpawnCount;
            match.RaiseFx(FxKind.ParasiteApplied, target.Position, 0.6f, team);
        }

        /// <summary>Is <paramref name="pos"/> a legal unit deploy position for <paramref name="team"/>?</summary>
        public bool IsValidDeploy(Team team, Vector2 pos)
        {
            var cfg = match.Config;
            Vector2 ext = cfg.map.halfExtents;
            if (Mathf.Abs(pos.x) > ext.x || Mathf.Abs(pos.y) > ext.y) return false;

            // Not inside a Core.
            if (match.GetCore(Team.Player).DistanceTo(pos) < 0.05f || match.GetCore(Team.Enemy).DistanceTo(pos) < 0.05f)
                return false;

            // Own half.
            if (pos.y * team.Forward() <= -cfg.homeZoneMargin) return true;

            // Near a controlled Rift.
            float r2 = cfg.deployRadiusAroundOwnedRift * cfg.deployRadiusAroundOwnedRift;
            foreach (var rift in match.Rifts)
                if (rift.Owner.Is(team) && (rift.Position - pos).sqrMagnitude <= r2) return true;

            return false;
        }

        /// <summary>Rift under a tap (radius slightly enlarged for comfortable touch).</summary>
        public Rift FindRiftAt(Vector2 pos)
        {
            Rift best = null;
            float bestD = float.PositiveInfinity;
            foreach (var r in match.Rifts)
            {
                float d = Vector2.Distance(pos, r.Position);
                if (d <= r.Radius + 0.4f && d < bestD) { bestD = d; best = r; }
            }
            return best;
        }
    }
}
