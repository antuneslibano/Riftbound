using System;
using Riftbound.AI;
using Riftbound.Config;
using Riftbound.Controls;
using Riftbound.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Riftbound.UI
{
    /// <summary>
    /// Deliberately plain HUD built from code:
    /// top = enemy Core + timer, bottom = player Core, Flux bar and 4 cards, plus banners, toasts and the end screen.
    /// Layout is in reference units of a 1080x1920 portrait screen, inside the device safe area.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public const float TopHeight = 170f;
        public const float BottomHeight = 480f;

        Match match;
        PlayerInputController input;
        BotController bot;

        public RectTransform SafeRoot { get; private set; }
        /// <summary>Area between the top and bottom bars: the camera renders the field there.</summary>
        public RectTransform FieldArea { get; private set; }

        Text enemyCoreText, playerCoreText, timerText, statusText, fluxText, nextText, hintText, toastText;
        Text bannerTitle, bannerSub;
        Image[] fluxCells;
        Image fluxPartial;
        CardButton[] cards;
        GameObject endPanel;
        Text endTitle, endReason, endStats;
        Text[] difficultyLabels;

        float bannerTime, toastTime;
        int shownEnemyCore = -1, shownPlayerCore = -1, shownClock = -1, shownFlux = -1, shownStatusKey = -1;
        int shownDeckVersion = -1;
        CardDefinition shownHintCard;

        Action<BotDifficulty> onPlayAgain;
        Action onToggleDebug;
        BotDifficulty selectedDifficulty;

        public void Build(Match m, PlayerInputController playerInput, BotController botController,
            Action<BotDifficulty> playAgain, Action toggleDebug)
        {
            match = m;
            input = playerInput;
            bot = botController;
            onPlayAgain = playAgain;
            onToggleDebug = toggleDebug;
            selectedDifficulty = bot.Difficulty;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // portrait: width drives the scale, extra height goes to the field
            gameObject.AddComponent<GraphicRaycaster>();

            SafeRoot = UIFactory.Rect(transform, "SafeArea");
            SafeRoot.gameObject.AddComponent<SafeAreaFitter>();

            BuildTop();
            BuildBottom();

            FieldArea = UIFactory.Rect(SafeRoot, "FieldArea").Anchor(Vector2.zero, Vector2.one, new Vector2(0f, BottomHeight), new Vector2(0f, -TopHeight));

            hintText = UIFactory.Label(FieldArea, "Hint", "", 34, Palette.Text);
            hintText.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 60f));
            toastText = UIFactory.Label(FieldArea, "Toast", "", 44, Palette.Invalid);
            toastText.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 70f), new Vector2(0f, 130f));
            AddShadow(hintText);
            AddShadow(toastText);

            bannerTitle = UIFactory.Label(FieldArea, "BannerTitle", "", 120, Color.white);
            bannerTitle.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 150f));
            bannerSub = UIFactory.Label(FieldArea, "BannerSub", "", 44, Color.white);
            bannerSub.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -70f), new Vector2(0f, 0f));
            AddShadow(bannerTitle);
            AddShadow(bannerSub);

            BuildEndPanel();

            match.Announced += ShowBanner;
            match.Ended += OnMatchEnded;
            input.Feedback += ShowToast;
        }

        void OnDestroy()
        {
            if (match != null)
            {
                match.Announced -= ShowBanner;
                match.Ended -= OnMatchEnded;
            }
            if (input != null) input.Feedback -= ShowToast;
        }

        static void AddShadow(Text t)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(3f, -3f);
        }

        // ------------------------------------------------------------------ build

        void BuildTop()
        {
            var top = UIFactory.Panel(SafeRoot, "TopBar", Palette.Panel).rectTransform;
            top.Anchor(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -TopHeight), Vector2.zero);

            enemyCoreText = UIFactory.Label(top, "EnemyCore", "ENEMY CORE: 100", 42, Palette.Enemy, TextAnchor.MiddleLeft);
            enemyCoreText.rectTransform.Anchor(new Vector2(0f, 0.35f), new Vector2(0.45f, 1f), new Vector2(28f, 0f), Vector2.zero);

            timerText = UIFactory.Label(top, "Timer", "03:00", 80, Color.white);
            timerText.rectTransform.Anchor(new Vector2(0.35f, 0.3f), new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero);

            statusText = UIFactory.Label(top, "Status", "", 32, Palette.TextDim);
            statusText.rectTransform.Anchor(new Vector2(0f, 0f), new Vector2(1f, 0.35f), new Vector2(0f, 6f), Vector2.zero);

            var dbg = UIFactory.Button(top, "DebugButton", "DEBUG", 30, new Color(0.25f, 0.25f, 0.3f), () => onToggleDebug?.Invoke());
            dbg.GetComponent<RectTransform>().Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -100f), new Vector2(-16f, -14f));
        }

        void BuildBottom()
        {
            var bottom = UIFactory.Panel(SafeRoot, "BottomBar", Palette.Panel).rectTransform;
            bottom.Anchor(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, BottomHeight));

            playerCoreText = UIFactory.Label(bottom, "PlayerCore", "PLAYER CORE: 100", 42, Palette.Player, TextAnchor.MiddleLeft);
            playerCoreText.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(28f, -70f), new Vector2(0f, -8f));
            nextText = UIFactory.Label(bottom, "Next", "NEXT: -", 30, Palette.TextDim, TextAnchor.MiddleRight, FontStyle.Normal);
            nextText.rectTransform.Anchor(new Vector2(0.55f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(-28f, -8f));

            // Flux row: label + 10 cells.
            fluxText = UIFactory.Label(bottom, "Flux", "FLUX: 5/10", 40, new Color(0.55f, 0.8f, 1f), TextAnchor.MiddleLeft);
            fluxText.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0.32f, 1f), new Vector2(28f, -150f), new Vector2(0f, -80f));
            var bar = UIFactory.Rect(bottom, "FluxBar").Anchor(new Vector2(0.32f, 1f), new Vector2(1f, 1f), new Vector2(0f, -140f), new Vector2(-28f, -90f));
            int cells = Mathf.Max(1, Mathf.RoundToInt(match.Config.fluxMax));
            fluxCells = new Image[cells];
            for (int i = 0; i < cells; i++)
            {
                float x0 = (float)i / cells, x1 = (float)(i + 1) / cells;
                var back = UIFactory.Panel(bar, "CellBack" + i, new Color(1f, 1f, 1f, 0.08f));
                back.rectTransform.Anchor(new Vector2(x0, 0f), new Vector2(x1, 1f), new Vector2(3f, 0f), new Vector2(-3f, 0f));
                fluxCells[i] = UIFactory.Panel(back.transform, "Cell", new Color(0.35f, 0.6f, 1f));
                fluxCells[i].rectTransform.Stretch();
            }
            fluxPartial = UIFactory.Panel(bar, "Partial", new Color(0.35f, 0.6f, 1f, 0.35f));

            // Cards.
            var hand = UIFactory.Rect(bottom, "Hand").Anchor(Vector2.zero, new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-20f, -160f));
            int n = match.GetDeck(Team.Player).HandSize;
            cards = new CardButton[n];
            for (int i = 0; i < n; i++)
            {
                cards[i] = new CardButton(hand, i, input.SelectSlot);
                var rt = cards[i].Button.GetComponent<RectTransform>();
                rt.Anchor(new Vector2((float)i / n, 0f), new Vector2((float)(i + 1) / n, 1f), new Vector2(8f, 0f), new Vector2(-8f, 0f));
            }
        }

        void BuildEndPanel()
        {
            var panel = UIFactory.Panel(SafeRoot, "EndPanel", new Color(0f, 0f, 0f, 0.85f), true);
            panel.rectTransform.Stretch();
            endPanel = panel.gameObject;

            endTitle = UIFactory.Label(panel.transform, "Title", "VICTORY", 150, Color.white);
            endTitle.rectTransform.Anchor(new Vector2(0f, 0.72f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero);
            endReason = UIFactory.Label(panel.transform, "Reason", "", 40, Palette.TextDim);
            endReason.rectTransform.Anchor(new Vector2(0f, 0.66f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
            endStats = UIFactory.Label(panel.transform, "Stats", "", 34, Palette.Text, TextAnchor.UpperCenter, FontStyle.Normal);
            endStats.rectTransform.Anchor(new Vector2(0f, 0.42f), new Vector2(1f, 0.64f), Vector2.zero, Vector2.zero);

            var diffLabel = UIFactory.Label(panel.transform, "DiffLabel", "BOT DIFFICULTY", 32, Palette.TextDim);
            diffLabel.rectTransform.Anchor(new Vector2(0f, 0.37f), new Vector2(1f, 0.41f), Vector2.zero, Vector2.zero);
            difficultyLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var d = (BotDifficulty)i;
                var b = UIFactory.Button(panel.transform, "Diff" + d, d.ToString().ToUpperInvariant(), 38, new Color(0.2f, 0.22f, 0.28f),
                    () => { selectedDifficulty = d; RefreshDifficultyButtons(); }, out difficultyLabels[i]);
                b.GetComponent<RectTransform>().Anchor(new Vector2(0.1f + i * 0.27f, 0.29f), new Vector2(0.1f + i * 0.27f + 0.25f, 0.36f), Vector2.zero, Vector2.zero);
            }

            var play = UIFactory.Button(panel.transform, "PlayAgain", "PLAY AGAIN", 70, new Color(0.2f, 0.45f, 0.95f),
                () => onPlayAgain?.Invoke(selectedDifficulty));
            play.GetComponent<RectTransform>().Anchor(new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.25f), Vector2.zero, Vector2.zero);

            endPanel.SetActive(false);
        }

        void RefreshDifficultyButtons()
        {
            for (int i = 0; i < difficultyLabels.Length; i++)
                difficultyLabels[i].color = i == (int)selectedDifficulty ? Palette.Contested : new Color(0.6f, 0.6f, 0.65f);
        }

        // ------------------------------------------------------------------ events

        void ShowBanner(string title, string sub, Color color)
        {
            bannerTitle.text = title;
            bannerTitle.color = color;
            bannerSub.text = sub;
            bannerTime = 2.4f;
        }

        void ShowToast(string text, bool error)
        {
            toastText.text = text;
            toastText.color = error ? Palette.Invalid : Palette.Valid;
            toastTime = 1.4f;
        }

        void OnMatchEnded()
        {
            endPanel.SetActive(true);
            endPanel.transform.SetAsLastSibling();
            if (!match.HasWinner)
            {
                endTitle.text = "DRAW";
                endTitle.color = Palette.Contested;
            }
            else if (match.Winner == Team.Player)
            {
                endTitle.text = "VICTORY";
                endTitle.color = Palette.Player;
            }
            else
            {
                endTitle.text = "DEFEAT";
                endTitle.color = Palette.Enemy;
            }
            endReason.text = match.EndReason;
            var s = match.Stats;
            endStats.text =
                "YOUR CORE " + Mathf.CeilToInt(match.GetCore(Team.Player).Stability) + "  vs  ENEMY CORE " + Mathf.CeilToInt(match.GetCore(Team.Enemy).Stability) + "\n" +
                "RIFT BREAKS  " + match.RiftBreak.BreakCount[0] + " - " + match.RiftBreak.BreakCount[1] + "\n" +
                "CARDS PLAYED  " + s.CardsPlayed[0] + " - " + s.CardsPlayed[1] + "\n" +
                "UNITS LOST  " + s.UnitsLost[0] + " - " + s.UnitsLost[1] + "\n" +
                "MATCH TIME  " + UIFactory.Clock(match.Elapsed) + "   BOT: " + bot.Difficulty.ToString().ToUpperInvariant();
            selectedDifficulty = bot.Difficulty;
            RefreshDifficultyButtons();
        }

        // ------------------------------------------------------------------ per frame

        public void Refresh(float unscaledDt)
        {
            var pCore = match.GetCore(Team.Player);
            var eCore = match.GetCore(Team.Enemy);
            int ps = Mathf.CeilToInt(pCore.Stability), es = Mathf.CeilToInt(eCore.Stability);
            if (es != shownEnemyCore) { shownEnemyCore = es; enemyCoreText.text = "ENEMY CORE: " + es; }
            if (ps != shownPlayerCore) { shownPlayerCore = ps; playerCoreText.text = "PLAYER CORE: " + ps; }
            enemyCoreText.color = eCore.Vulnerable ? Color.Lerp(Palette.Enemy, Palette.Contested, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Palette.Enemy;
            playerCoreText.color = pCore.Vulnerable ? Color.Lerp(Palette.Player, Palette.Contested, Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : Palette.Player;

            // Clock
            bool sudden = match.Phase == MatchPhase.SuddenRift;
            int clock = sudden ? -2 - Mathf.FloorToInt(match.SuddenElapsed) : Mathf.CeilToInt(match.TimeRemaining);
            if (clock != shownClock)
            {
                shownClock = clock;
                timerText.text = sudden ? "SUDDEN" : UIFactory.Clock(match.TimeRemaining);
                timerText.color = sudden ? Palette.Contested : match.TimeRemaining <= 30f ? Palette.HpLow : Color.white;
            }

            // Status line under the clock: Rift Break > Sudden Rift > next Rift Shift.
            int statusKey;
            if (match.RiftBreak.Active) statusKey = 1000 + Mathf.CeilToInt(match.RiftBreak.Remaining * 10f);
            else if (sudden) statusKey = 2000 + Mathf.CeilToInt(Mathf.Max(0f, match.Config.suddenRiftMaxDuration - match.SuddenElapsed));
            else if (match.RiftShift.Enabled) statusKey = 3000 + Mathf.CeilToInt(match.RiftShift.TimeToNext);
            else statusKey = 0;
            if (statusKey != shownStatusKey)
            {
                shownStatusKey = statusKey;
                if (match.RiftBreak.Active)
                {
                    statusText.text = (match.RiftBreak.Breaker == Team.Player ? "RIFT BREAK - ATTACK! " : "ENEMY RIFT BREAK - DEFEND! ") + match.RiftBreak.Remaining.ToString("0.0") + "s";
                    statusText.color = Palette.Of(match.RiftBreak.Breaker);
                }
                else if (sudden)
                {
                    statusText.text = "SUDDEN RIFT - FIRST CORE HIT WINS (" + (statusKey - 2000) + "s)";
                    statusText.color = Palette.Contested;
                }
                else if (statusKey > 0)
                {
                    statusText.text = "RIFT SHIFT IN " + (statusKey - 3000) + "s";
                    statusText.color = Palette.TextDim;
                }
                else statusText.text = "";
            }

            // Flux
            var flux = match.GetFlux(Team.Player);
            int whole = flux.Whole;
            if (whole != shownFlux)
            {
                shownFlux = whole;
                fluxText.text = "FLUX: " + whole + "/" + Mathf.RoundToInt(flux.Max);
                for (int i = 0; i < fluxCells.Length; i++) fluxCells[i].enabled = i < whole;
            }
            if (whole < fluxCells.Length)
            {
                float frac = flux.Current - whole;
                float x0 = (float)whole / fluxCells.Length;
                float x1 = x0 + frac / fluxCells.Length;
                fluxPartial.enabled = true;
                fluxPartial.rectTransform.Anchor(new Vector2(x0, 0f), new Vector2(x1, 1f), new Vector2(3f, 0f), new Vector2(-3f, 0f));
            }
            else fluxPartial.enabled = false;

            // Cards
            var deck = match.GetDeck(Team.Player);
            for (int i = 0; i < cards.Length; i++)
                cards[i].Refresh(deck.GetSlot(i), flux.Current, input.SelectedSlot == i);
            if (deck.Version != shownDeckVersion)
            {
                shownDeckVersion = deck.Version;
                nextText.text = "NEXT: " + (deck.Next != null ? deck.Next.displayName : "-");
            }

            // Hint
            var card = input.SelectedCard;
            if (card != shownHintCard)
            {
                shownHintCard = card;
                hintText.text = card == null ? "" :
                    card.kind == CardKind.Unit ? card.displayName + ": TAP YOUR HALF OR A RIFT YOU CONTROL" :
                    card.kind == CardKind.PulseSpell ? card.displayName + ": TAP A RIFT" : card.displayName + ": TAP AN ENEMY UNIT";
            }

            // Timed texts
            if (bannerTime > 0f)
            {
                bannerTime -= unscaledDt;
                float a = Mathf.Clamp01(bannerTime / 0.5f);
                bannerTitle.color = bannerTitle.color.WithAlpha(a);
                bannerSub.color = bannerSub.color.WithAlpha(a);
                if (bannerTime <= 0f) { bannerTitle.text = ""; bannerSub.text = ""; }
            }
            if (toastTime > 0f)
            {
                toastTime -= unscaledDt;
                if (toastTime <= 0f) toastText.text = "";
            }
        }
    }
}
