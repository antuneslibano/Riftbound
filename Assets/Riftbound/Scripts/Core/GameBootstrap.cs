using Riftbound.AI;
using Riftbound.Config;
using Riftbound.Controls;
using Riftbound.Simulation;
using Riftbound.UI;
using Riftbound.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Riftbound
{
    /// <summary>
    /// Entry point / game manager. Lives in Scenes/Main.unity. On Play it immediately starts a
    /// Player vs Bot match, builds all visuals/UI from code, ticks the simulation every frame and
    /// handles restart, pause and game speed.
    /// Frame order: player input → bot → simulation → views → HUD.
    /// </summary>
    public class GameBootstrap : MonoBehaviour, DebugPanel.IHost
    {
        const string DifficultyPrefKey = "riftbound.botDifficulty";
        const string DebugPrefKey = "riftbound.debug";

        [Tooltip("Optional. If empty, Resources/RiftboundConfig is used.")]
        [SerializeField] GameConfig config;
        [Tooltip("Start with Debug Mode visible.")]
        [SerializeField] bool startInDebugMode;
        [Tooltip("Fixed seed for reproducible matches (0 = random).")]
        [SerializeField] int seed;

        Camera fieldCamera;
        CameraFitter fitter;
        GameObject matchRoot, hudRoot;

        Match match;
        BotController bot;
        PlayerInputController input;
        MatchView view;
        HUDController hud;
        DebugPanel debugPanel;

        BotDifficulty difficulty;
        bool debugMode;
        float speed = 1f;
        bool paused;

        public Match Match => match;
        public bool Paused => paused;
        public float Speed => speed;

        // Safety net: if Main scene somehow lacks the bootstrap object, create it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBootstrap()
        {
            if (SceneManager.GetActiveScene().name != "Main") return;
#if UNITY_2023_1_OR_NEWER
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
#else
            if (FindObjectOfType<GameBootstrap>() != null) return;
#endif
            new GameObject("Riftbound (auto)").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            RiftInput.Configure();

            if (config == null) config = GameConfig.LoadOrDefault();
            difficulty = (BotDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, (int)config.defaultBotDifficulty), 0, 2);
            debugMode = startInDebugMode || PlayerPrefs.GetInt(DebugPrefKey, 0) == 1;

            CreateCameras();
            EnsureEventSystem();
            StartMatch();
        }

        // ------------------------------------------------------------------ setup

        void CreateCameras()
        {
            // Full-screen clear behind the UI (the field camera only covers the field area).
            var bgGo = new GameObject("BackgroundCamera");
            bgGo.transform.SetParent(transform, false);
            var bg = bgGo.AddComponent<Camera>();
            bg.clearFlags = CameraClearFlags.SolidColor;
            bg.backgroundColor = Palette.Background;
            bg.cullingMask = 0;
            bg.depth = -10;
            bg.orthographic = true;

            var camGo = new GameObject("FieldCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            fieldCamera = camGo.AddComponent<Camera>();
            fieldCamera.orthographic = true;
            fieldCamera.orthographicSize = 6.5f;
            fieldCamera.clearFlags = CameraClearFlags.SolidColor;
            fieldCamera.backgroundColor = Palette.Background;
            fieldCamera.nearClipPlane = 0.1f;
            fieldCamera.farClipPlane = 50f;
            fieldCamera.depth = 0;

            fitter = gameObject.AddComponent<CameraFitter>();
            fitter.Target = fieldCamera;
            fitter.MapHalfExtents = config.map.halfExtents + new Vector2(0.2f, 0.35f);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            RiftInput.AddUiInputModule(go);
            DontDestroyOnLoad(go);
        }

        void StartMatch()
        {
            paused = false;
            speed = 1f;
            Time.timeScale = 1f;

            int s = seed != 0 ? seed : System.Environment.TickCount;
            match = new Match(config, s);

            matchRoot = new GameObject("Match");
            view = matchRoot.AddComponent<MatchView>();
            view.Build(match);

            bot = new BotController(match, Team.Enemy, difficulty, s + 7919);
            input = new PlayerInputController(match, view, fieldCamera, Team.Player);

            hudRoot = new GameObject("HUD");
            hud = hudRoot.AddComponent<HUDController>();
            hud.Build(match, input, bot, PlayAgain, ToggleDebug);

            var dbgRt = UIFactory.Rect(hud.FieldArea, "DebugPanel").Stretch();
            debugPanel = dbgRt.gameObject.AddComponent<DebugPanel>();
            debugPanel.Build(match, bot, this);
            dbgRt.gameObject.SetActive(debugMode);

            fitter.FieldArea = hud.FieldArea;
        }

        // ------------------------------------------------------------------ loop

        void Update()
        {
            HandleHotkeys();
            if (match == null) return;

            // Clamp to keep the simulation stable after hitches (and at x4 speed).
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            float unscaled = Time.unscaledDeltaTime;

            input.Tick();
            bot.Tick(dt);
            match.Tick(dt);

            view.DebugMode = debugMode;
            view.Sync(unscaled);
            hud.Refresh(unscaled);
            if (debugMode) debugPanel.Tick(unscaled);
        }

        void HandleHotkeys()
        {
            if (RiftInput.KeyDown(Hotkey.ToggleDebug)) ToggleDebug();
            if (RiftInput.KeyDown(Hotkey.Pause)) TogglePause();
            if (RiftInput.KeyDown(Hotkey.Restart) && match != null && !match.IsRunning) Restart();
            if (input != null)
            {
                if (RiftInput.KeyDown(Hotkey.Card1)) input.SelectSlot(0);
                if (RiftInput.KeyDown(Hotkey.Card2)) input.SelectSlot(1);
                if (RiftInput.KeyDown(Hotkey.Card3)) input.SelectSlot(2);
                if (RiftInput.KeyDown(Hotkey.Card4)) input.SelectSlot(3);
                if (RiftInput.KeyDown(Hotkey.Cancel)) input.Deselect();
            }
        }

        // ------------------------------------------------------------------ session actions

        void PlayAgain(BotDifficulty diff)
        {
            difficulty = diff;
            if (bot != null) bot.SetDifficulty(diff);
            PlayerPrefs.SetInt(DifficultyPrefKey, (int)diff);
            PlayerPrefs.Save();
            Restart();
        }

        public void Restart()
        {
            if (bot != null) difficulty = bot.Difficulty;
            if (matchRoot != null) Destroy(matchRoot);
            if (hudRoot != null) Destroy(hudRoot);
            match = null;
            StartMatch();
        }

        void ToggleDebug()
        {
            debugMode = !debugMode;
            PlayerPrefs.SetInt(DebugPrefKey, debugMode ? 1 : 0);
            if (debugPanel != null) debugPanel.gameObject.SetActive(debugMode);
        }

        public void SetSpeed(float s)
        {
            speed = Mathf.Max(0.1f, s);
            paused = false;
            Time.timeScale = speed;
        }

        public void TogglePause()
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : speed;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
