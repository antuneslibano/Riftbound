using System.Text;
using Riftbound.AI;
using Riftbound.Config;
using Riftbound.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Riftbound.UI
{
    /// <summary>
    /// DEBUG MODE overlay: live numbers (Flux, Rift ownership, DPS, unit counts, timers, bot decision)
    /// and buttons to manipulate the match quickly. Toggle with the DEBUG button (or F1 in the Editor).
    /// Unit-level info (HP, target, AI state, goal Rift) is drawn above each unit by UnitView.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        /// <summary>Actions the panel needs from the session (implemented by GameBootstrap).</summary>
        public interface IHost
        {
            void Restart();
            void SetSpeed(float speed);
            void TogglePause();
            bool Paused { get; }
            float Speed { get; }
        }

        Match match;
        BotController bot;
        IHost host;
        Text info;
        Text shiftToggleLabel, teamToggleLabel, difficultyLabel;
        GameObject buttonsRoot;
        float refresh;
        Team spawnTeam = Team.Player;
        readonly StringBuilder sb = new StringBuilder(1024);

        /// <summary>Call on a component added to a stretched RectTransform inside the HUD field area.</summary>
        public void Build(Match m, BotController b, IHost h)
        {
            match = m;
            bot = b;
            host = h;
            var rt = (RectTransform)transform;

            var infoBg = UIFactory.Panel(rt, "InfoBg", new Color(0f, 0f, 0f, 0.55f));
            infoBg.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(8f, -520f), new Vector2(0f, -8f));
            info = UIFactory.Label(infoBg.transform, "Info", "", 24, new Color(0.8f, 1f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal);
            info.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-6f, -6f));

            var grid = UIFactory.Panel(rt, "Buttons", new Color(0f, 0f, 0f, 0.35f), true).rectTransform;
            grid.Anchor(new Vector2(0.63f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f - 9 * 74f), new Vector2(-8f, -8f));
            buttonsRoot = grid.gameObject;

            string[] labels =
            {
                "+1 FLUX", "+10 FLUX", "SPAWN MAW", "SPAWN BLINK", "DMG P.CORE", "DMG E.CORE",
                "FORCE BREAK", "FORCE SHIFT", "PAUSE", "SPEED x1", "SPEED x2", "SPEED x4",
                "RESTART", "SHIFT: ON", "TEAM: PLAYER", "BOT: NORMAL",
            };
            const int cols = 2;
            int rows = (labels.Length + cols - 1) / cols;
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                int c = i % cols, r = i / cols;
                var btn = UIFactory.Button(grid, "Dbg" + i, labels[i], 24, new Color(0.22f, 0.24f, 0.3f, 0.95f), () => OnButton(index), out var label);
                btn.GetComponent<RectTransform>().Anchor(
                    new Vector2((float)c / cols, 1f - (float)(r + 1) / rows), new Vector2((float)(c + 1) / cols, 1f - (float)r / rows),
                    new Vector2(4f, 4f), new Vector2(-4f, -4f));
                if (i == 13) shiftToggleLabel = label;
                if (i == 14) teamToggleLabel = label;
                if (i == 15) difficultyLabel = label;
            }
            RefreshLabels();
        }

        void OnButton(int i)
        {
            var cfg = match.Config;
            switch (i)
            {
                case 0: match.GetFlux(spawnTeam).Add(1f); break;
                case 1: match.GetFlux(spawnTeam).Add(10f); break;
                case 2: match.DebugSpawn(FindCard("MAW"), spawnTeam); break;
                case 3: match.DebugSpawn(FindCard("BLINK"), spawnTeam); break;
                case 4: match.DebugDamageCore(Team.Player, 10f); break;
                case 5: match.DebugDamageCore(Team.Enemy, 10f); break;
                case 6: match.RiftBreak.Begin(spawnTeam); break;
                case 7: match.RiftShift.Shift(); break;
                case 8: host.TogglePause(); break;
                case 9: host.SetSpeed(1f); break;
                case 10: host.SetSpeed(2f); break;
                case 11: host.SetSpeed(4f); break;
                case 12: host.Restart(); return;
                case 13: match.RiftShift.Enabled = !match.RiftShift.Enabled; break;
                case 14: spawnTeam = spawnTeam.Opponent(); break;
                case 15: bot.SetDifficulty((BotDifficulty)(((int)bot.Difficulty + 1) % 3)); break;
            }
            RefreshLabels();
        }

        CardDefinition FindCard(string id)
        {
            foreach (var c in match.Config.deck)
                if (c != null && c.cardId == id) return c;
            return match.Config.deck.Count > 0 ? match.Config.deck[0] : null;
        }

        void RefreshLabels()
        {
            shiftToggleLabel.text = "SHIFT: " + (match.RiftShift.Enabled ? "ON" : "OFF");
            teamToggleLabel.text = "TEAM: " + spawnTeam.Label();
            teamToggleLabel.color = Palette.Of(spawnTeam);
            difficultyLabel.text = "BOT: " + bot.Difficulty.ToString().ToUpperInvariant();
        }

        public void Tick(float unscaledDt)
        {
            refresh -= unscaledDt;
            if (refresh > 0f) return;
            refresh = 0.2f;

            sb.Length = 0;
            sb.Append("TIME ").Append(UIFactory.Clock(match.TimeRemaining)).Append("  PHASE ").Append(match.Phase)
              .Append("  SPEED x").Append(host.Paused ? "0 (PAUSED)" : host.Speed.ToString("0")).Append('\n');
            for (int t = 0; t < 2; t++)
            {
                var team = (Team)t;
                var f = match.GetFlux(team);
                sb.Append(team.Label()).Append(": FLUX ").Append(f.Current.ToString("0.0")).Append("/").Append(f.Max.ToString("0"))
                  .Append(" UNITS ").Append(match.Units.Count(team))
                  .Append(" DPS ").Append(match.Stats.Dps(team).ToString("0"))
                  .Append(" RIFTS ").Append(match.OwnedRiftCount(team))
                  .Append(" DRAINED ").Append(f.TotalDrained.ToString("0.0")).Append('\n');
            }
            foreach (var r in match.Rifts)
            {
                sb.Append(r.Name).Append(": ").Append(r.Owner).Append(" ctrl ").Append(r.Control.ToString("+0.00;-0.00"))
                  .Append(" P").Append(r.Presence[0]).Append(" E").Append(r.Presence[1]);
                if (r.Contested) sb.Append(" CONTESTED");
                sb.Append('\n');
            }
            sb.Append("RIFT BREAK: ");
            if (match.RiftBreak.Active) sb.Append(match.RiftBreak.Breaker.Label()).Append(' ').Append(match.RiftBreak.Remaining.ToString("0.0")).Append("s");
            else sb.Append("-");
            sb.Append("  COUNT ").Append(match.RiftBreak.BreakCount[0]).Append('/').Append(match.RiftBreak.BreakCount[1]).Append('\n');
            sb.Append("SHIFT: ").Append(match.RiftShift.Enabled ? match.RiftShift.TimeToNext.ToString("0") + "s" : "OFF")
              .Append(" [").Append(match.RiftShift.CurrentDescription).Append("]\n");
            sb.Append("BOT ").Append(bot.Difficulty).Append(": ").Append(bot.LastDecision).Append('\n');
            sb.Append("CARDS ").Append(match.Stats.CardsPlayed[0]).Append('/').Append(match.Stats.CardsPlayed[1])
              .Append("  LOST ").Append(match.Stats.UnitsLost[0]).Append('/').Append(match.Stats.UnitsLost[1])
              .Append("  FPS ").Append((1f / Mathf.Max(0.0001f, unscaledDt)).ToString("0"));
            info.text = sb.ToString();
        }
    }
}
