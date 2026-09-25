using Riftbound.Config;
using Riftbound.Simulation;
using UnityEngine;

namespace Riftbound.AI
{
    /// <summary>
    /// Rule based bot. It only acts through <see cref="Match.Submit"/> with the same deck, Flux and rules as
    /// the player (no cheating). Every <see cref="BotProfile.decisionInterval"/> it reads the board and makes
    /// at most ONE play, in this priority order:
    /// <list type="number">
    /// <item>Core under Rift Break attack -> deploy defenders.</item>
    /// <item>Worthwhile spell (Pulse on a group / Parasite on a valuable unit).</item>
    /// <item>Own or contested Rift with enemies -> reinforce with a fighting unit.</item>
    /// <item>Neutral Rift nobody is heading to -> send a fast capturer.</item>
    /// <item>Enemy Rift -> push with a tank/fighter.</item>
    /// </list>
    /// Difficulty changes decision frequency, mistake chance, counter-picking and Flux saving.
    /// To replace the bot with network play, drop this class and submit commands received from the network.
    /// </summary>
    public class BotController
    {
        enum Purpose { Capture, Fight, Push, DefendCore }

        readonly Match match;
        readonly Team team;
        readonly Team opp;
        readonly System.Random rng;
        float timer;

        public BotDifficulty Difficulty { get; private set; }
        public BotProfile Profile { get; private set; }

        /// <summary>Last decision in plain text (Debug Mode).</summary>
        public string LastDecision { get; private set; } = "-";

        public BotController(Match match, Team team, BotDifficulty difficulty, int seed)
        {
            this.match = match;
            this.team = team;
            opp = team.Opponent();
            rng = new System.Random(seed);
            SetDifficulty(difficulty);
            timer = 1.0f + Profile.decisionInterval; // small grace period at match start
        }

        public void SetDifficulty(BotDifficulty difficulty)
        {
            Difficulty = difficulty;
            Profile = match.Config.GetBotProfile(difficulty);
        }

        public void Tick(float dt)
        {
            if (!match.IsRunning) return;
            timer -= dt;
            if (timer > 0f) return;
            timer = Profile.decisionInterval + (float)rng.NextDouble() * Profile.decisionJitter;
            Decide();
        }

        // ------------------------------------------------------------------ decision

        void Decide()
        {
            var flux = match.GetFlux(team);

            if (rng.NextDouble() < Profile.mistakeChance)
            {
                PlayRandom("mistake/random");
                return;
            }

            // 1. Our core is being assaulted.
            if (match.RiftBreak.IsActiveFor(opp))
            {
                var core = match.GetCore(team);
                Vector2 guard = core.Position + new Vector2(0f, team.Forward() * 1.2f);
                int attackers = match.Units.CountInRadius(opp, core.Position, 4f);
                if (attackers > 0 && TryPlayUnit(Purpose.DefendCore, guard, "defend core")) return;
            }

            // 2. Spells.
            if (TrySpells()) return;

            // 3. Reinforce fights on Rifts.
            Rift fight = null;
            float worstBalance = 0f;
            foreach (var r in match.Rifts)
            {
                float en = match.Units.StrengthInRadius(opp, r.Position, r.Radius + 0.8f);
                if (en <= 0f) continue;
                float mine = match.Units.StrengthInRadius(team, r.Position, r.Radius + 1.5f);
                bool relevant = r.Owner.Is(team) || mine > 0f || r.Owner == RiftOwner.Neutral;
                float balance = mine - en * (r.Owner.Is(team) ? 1.3f : 1f);
                if (relevant && balance < worstBalance) { worstBalance = balance; fight = r; }
            }
            if (fight != null && Profile.useCounters)
            {
                if (TryPlayUnit(Purpose.Fight, fight.Position, "reinforce " + fight.Name)) return;
            }

            // Hard bots save Flux for stronger pushes when nothing urgent is happening.
            bool saving = Profile.saveFluxUntil > 0 && flux.Current < Profile.saveFluxUntil && flux.Current < flux.Max - 0.1f;

            // 4. Capture neutral Rifts nobody is heading to.
            Rift capture = null;
            float bestCap = float.NegativeInfinity;
            foreach (var r in match.Rifts)
            {
                if (r.Owner != RiftOwner.Neutral) continue;
                int heading = match.Units.CountGoal(team, r.Node, null);
                float score = -heading * 5f - Vector2.Distance(r.Position, match.GetCore(team).Position);
                if (match.Units.StrengthInRadius(opp, r.Position, r.Radius + 0.8f) > 0f) score -= 3f;
                if (score > bestCap) { bestCap = score; capture = r; }
            }
            if (capture != null && match.Units.CountGoal(team, capture.Node, null) < 2 && !saving)
            {
                if (TryPlayUnit(Purpose.Capture, capture.Position, "capture " + capture.Name)) return;
            }

            // 5. Push an enemy Rift (the last one needed for a Rift Break is most valuable).
            Rift push = null;
            float bestPush = float.NegativeInfinity;
            foreach (var r in match.Rifts)
            {
                if (!r.Owner.Is(opp)) continue;
                float score = -match.Units.StrengthInRadius(opp, r.Position, r.Radius + 0.8f) * 2f
                              - Vector2.Distance(r.Position, match.GetCore(team).Position) * 0.5f;
                if (score > bestPush) { bestPush = score; push = r; }
            }
            if (push != null && !saving)
            {
                if (TryPlayUnit(Purpose.Push, push.Position, "push " + push.Name)) return;
            }

            // 6. Easy bots (no counters) still react to fights, just late and with any card.
            if (fight != null && !Profile.useCounters && !saving)
            {
                if (TryPlayUnit(Purpose.Fight, fight.Position, "reinforce " + fight.Name)) return;
            }

            // 7. Don't waste Flux at the cap.
            if (flux.Current >= flux.Max - 0.05f)
            {
                Rift any = capture ?? push ?? fight ?? match.Rifts[rng.Next(match.Rifts.Length)];
                if (TryPlayUnit(Purpose.Push, any.Position, "flux full")) return;
            }

            LastDecision = saving ? "saving flux" : "wait";
        }

        // ------------------------------------------------------------------ spells

        bool TrySpells()
        {
            var deck = match.GetDeck(team);
            var flux = match.GetFlux(team);
            for (int slot = 0; slot < deck.HandSize; slot++)
            {
                var card = deck.GetSlot(slot);
                if (card == null || !card.IsSpell || !flux.CanAfford(card.fluxCost)) continue;

                if (card.kind == CardKind.PulseSpell)
                {
                    Rift best = null;
                    float bestValue = 0f;
                    int bestCount = 0;
                    foreach (var r in match.Rifts)
                    {
                        float value = 0f;
                        int count = 0;
                        var enemies = match.Units.OfTeam(opp);
                        for (int i = 0; i < enemies.Count; i++)
                        {
                            var e = enemies[i];
                            if (!e.Alive || Vector2.Distance(e.Position, r.Position) > card.pulseRadius) continue;
                            count++;
                            value += e.Hp <= card.pulseDamage ? e.ThreatValue : e.ThreatValue * 0.35f;
                        }
                        if (value > bestValue) { bestValue = value; best = r; bestCount = count; }
                    }
                    float threshold = Profile.smartSpells ? card.fluxCost + 0.5f : 1f;
                    if (best != null && (bestValue >= threshold || (Profile.smartSpells && bestCount >= 4)))
                        if (Submit(slot, best.Position, "PULSE " + best.Name + " (" + bestCount + " units)")) return true;
                }
                else if (card.kind == CardKind.ParasiteSpell)
                {
                    Unit best = null;
                    float bestValue = Profile.smartSpells ? 2.5f : 0f;
                    var enemies = match.Units.OfTeam(opp);
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var e = enemies[i];
                        if (!e.Alive || e.Infected) continue;
                        float value = e.ThreatValue;
                        if (Profile.smartSpells)
                        {
                            float reduction = e.InsideRift != null ? e.Stats.riftDamageReduction : 0f;
                            if (e.Hp > card.parasiteDamage * (1f - reduction)) value *= 0.3f; // won't kill it
                        }
                        else value = (float)rng.NextDouble() * 3f;
                        if (value > bestValue) { bestValue = value; best = e; }
                    }
                    if (best != null && Submit(slot, best.Position, "PARASITE on " + best.Name)) return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ units

        bool TryPlayUnit(Purpose purpose, Vector2 target, string reason)
        {
            var deck = match.GetDeck(team);
            var flux = match.GetFlux(team);
            int bestSlot = -1;
            float bestScore = float.NegativeInfinity;
            bool targetEnemyOwned = false;
            foreach (var r in match.Rifts)
                if (r.Contains(target) && r.Owner.Is(opp)) targetEnemyOwned = true;
            bool enemyHasTank = EnemyHasTankNear(target);

            for (int slot = 0; slot < deck.HandSize; slot++)
            {
                var card = deck.GetSlot(slot);
                if (card == null || card.kind != CardKind.Unit) continue;
                float score = Profile.useCounters
                    ? ScoreUnitCard(card, purpose, targetEnemyOwned, enemyHasTank)
                    : (float)rng.NextDouble();
                if (!flux.CanAfford(card.fluxCost))
                {
                    // Worth waiting for? Remember it but keep looking for an affordable one.
                    continue;
                }
                if (score > bestScore) { bestScore = score; bestSlot = slot; }
            }
            if (bestSlot < 0) return false;
            return Submit(bestSlot, FindDeployPosition(target), reason);
        }

        static float ScoreUnitCard(CardDefinition c, Purpose purpose, bool enemyOwned, bool enemyHasTank)
        {
            var s = c.unit;
            int n = Mathf.Max(1, c.spawnCount);
            float hp = s.maxHp * n;
            float dps = s.Dps * n;
            float cost = Mathf.Max(1, c.fluxCost);
            switch (purpose)
            {
                case Purpose.Capture:
                    return s.moveSpeed * 2f - cost * 0.9f + (enemyOwned && s.fluxDrainPerSecond > 0f ? 2f : 0f);
                case Purpose.Fight:
                    return (dps / 40f + hp / 350f) / cost * 3f
                           + (enemyHasTank && s.prioritizeUnits ? 2f : 0f)
                           + (s.holdsRifts ? 0.5f : 0f);
                case Purpose.DefendCore:
                    return dps / 30f + s.moveSpeed * 0.5f - cost * 0.2f;
                default: // Push
                    return hp / 300f + dps / 50f + (s.fluxDrainPerSecond > 0f ? 1f : 0f) - cost * 0.3f;
            }
        }

        bool EnemyHasTankNear(Vector2 pos)
        {
            var enemies = match.Units.OfTeam(opp);
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Stats.maxHp >= 800f && Vector2.Distance(enemies[i].Position, pos) < 3f) return true;
            return false;
        }

        /// <summary>Closest legal deploy point to <paramref name="target"/> on the line towards our Core.</summary>
        Vector2 FindDeployPosition(Vector2 target)
        {
            Vector2 home = match.GetCore(team).Position + new Vector2(0f, team.Forward() * 1.2f);
            float j = Profile.placementJitter;
            for (int k = 0; k <= 10; k++)
            {
                Vector2 p = Vector2.Lerp(target, home, k / 10f);
                Vector2 jittered = p + new Vector2((float)(rng.NextDouble() * 2 - 1) * j, (float)(rng.NextDouble() * 2 - 1) * j);
                if (match.Cards.IsValidDeploy(team, jittered)) return jittered;
                if (match.Cards.IsValidDeploy(team, p)) return p;
            }
            return home;
        }

        void PlayRandom(string reason)
        {
            var deck = match.GetDeck(team);
            var flux = match.GetFlux(team);
            int start = rng.Next(deck.HandSize);
            for (int i = 0; i < deck.HandSize; i++)
            {
                int slot = (start + i) % deck.HandSize;
                var card = deck.GetSlot(slot);
                if (card == null || !flux.CanAfford(card.fluxCost)) continue;

                Vector2 pos;
                if (card.kind == CardKind.Unit)
                    pos = FindDeployPosition(match.Rifts[rng.Next(match.Rifts.Length)].Position);
                else if (card.kind == CardKind.PulseSpell)
                    pos = match.Rifts[rng.Next(match.Rifts.Length)].Position;
                else
                {
                    var enemies = match.Units.OfTeam(opp);
                    if (enemies.Count == 0) continue;
                    pos = enemies[rng.Next(enemies.Count)].Position;
                }
                if (Submit(slot, pos, reason)) return;
            }
            LastDecision = reason + " (nothing playable)";
        }

        bool Submit(int slot, Vector2 pos, string reason)
        {
            var card = match.GetDeck(team).GetSlot(slot);
            var result = match.Submit(new PlayCardCommand(team, slot, pos));
            LastDecision = (card != null ? card.displayName : "?") + " -> " + reason + (result == PlayResult.Ok ? "" : " [" + result + "]");
            return result == PlayResult.Ok;
        }
    }
}
