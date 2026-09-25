using UnityEngine;
using UnityEngine.EventSystems;
#if RIFTBOUND_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Riftbound.Controls
{
    /// <summary>Editor/desktop shortcuts.</summary>
    public enum Hotkey
    {
        ToggleDebug,
        Pause,
        Restart,
        Cancel,
        Card1,
        Card2,
        Card3,
        Card4,
    }

    /// <summary>One frame of the primary pointer (first touch, or the left mouse button).</summary>
    public struct PointerState
    {
        public bool Down;
        public bool Held;
        public bool Up;
        /// <summary>Mouse present and not pressed (hover preview; never used for touch).</summary>
        public bool Hover;
        public Vector2 Screen;
    }

    /// <summary>
    /// Thin input layer so the game works with either Unity input backend:
    /// the Input System package (RIFTBOUND_INPUT_SYSTEM is defined by the asmdef when the package is installed
    /// and "Active Input Handling" includes it), otherwise the legacy Input Manager.
    /// </summary>
    public static class RiftInput
    {
#if RIFTBOUND_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        public const string Backend = "Input System";

        public static PointerState ReadPointer()
        {
            var s = new PointerState();
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var touch = ts.primaryTouch;
                bool down = touch.press.wasPressedThisFrame;
                bool up = touch.press.wasReleasedThisFrame;
                bool held = touch.press.isPressed;
                if (down || up || held)
                {
                    s.Down = down;
                    s.Up = up;
                    s.Held = held && !down;
                    s.Screen = touch.position.ReadValue();
                    return s;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                s.Screen = mouse.position.ReadValue();
                s.Down = mouse.leftButton.wasPressedThisFrame;
                s.Up = mouse.leftButton.wasReleasedThisFrame;
                s.Held = mouse.leftButton.isPressed;
                s.Hover = !s.Held && !s.Up && ts == null;
            }
            return s;
        }

        public static bool KeyDown(Hotkey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case Hotkey.ToggleDebug: return kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame;
                case Hotkey.Pause: return kb.spaceKey.wasPressedThisFrame;
                case Hotkey.Restart: return kb.rKey.wasPressedThisFrame;
                case Hotkey.Cancel: return kb.escapeKey.wasPressedThisFrame;
                case Hotkey.Card1: return kb.digit1Key.wasPressedThisFrame;
                case Hotkey.Card2: return kb.digit2Key.wasPressedThisFrame;
                case Hotkey.Card3: return kb.digit3Key.wasPressedThisFrame;
                default: return kb.digit4Key.wasPressedThisFrame;
            }
        }

        public static void AddUiInputModule(GameObject eventSystem)
        {
            // Modules added from code don't get the default UI actions automatically.
            var module = eventSystem.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static void Configure() { }

#elif ENABLE_LEGACY_INPUT_MANAGER
        public const string Backend = "Input Manager (legacy)";

        public static PointerState ReadPointer()
        {
            var s = new PointerState();
            if (UnityEngine.Input.touchCount > 0)
            {
                var t = UnityEngine.Input.GetTouch(0);
                s.Screen = t.position;
                s.Down = t.phase == TouchPhase.Began;
                s.Up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                s.Held = !s.Down && !s.Up;
                return s;
            }

            s.Screen = UnityEngine.Input.mousePosition;
            s.Down = UnityEngine.Input.GetMouseButtonDown(0);
            s.Up = UnityEngine.Input.GetMouseButtonUp(0);
            s.Held = UnityEngine.Input.GetMouseButton(0);
            s.Hover = !s.Held && !s.Up && !UnityEngine.Input.touchSupported;
            return s;
        }

        public static bool KeyDown(Hotkey k)
        {
            switch (k)
            {
                case Hotkey.ToggleDebug: return UnityEngine.Input.GetKeyDown(KeyCode.F1) || UnityEngine.Input.GetKeyDown(KeyCode.BackQuote);
                case Hotkey.Pause: return UnityEngine.Input.GetKeyDown(KeyCode.Space);
                case Hotkey.Restart: return UnityEngine.Input.GetKeyDown(KeyCode.R);
                case Hotkey.Cancel: return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
                case Hotkey.Card1: return UnityEngine.Input.GetKeyDown(KeyCode.Alpha1);
                case Hotkey.Card2: return UnityEngine.Input.GetKeyDown(KeyCode.Alpha2);
                case Hotkey.Card3: return UnityEngine.Input.GetKeyDown(KeyCode.Alpha3);
                default: return UnityEngine.Input.GetKeyDown(KeyCode.Alpha4);
            }
        }

        public static void AddUiInputModule(GameObject eventSystem) => eventSystem.AddComponent<StandaloneInputModule>();

        public static void Configure() => UnityEngine.Input.multiTouchEnabled = false;

#else
        public const string Backend = "NONE";

        public static PointerState ReadPointer() => default;

        public static bool KeyDown(Hotkey k) => false;

        public static void AddUiInputModule(GameObject eventSystem) { }

        public static void Configure()
        {
            Debug.LogError("[Riftbound] No input backend: install the Input System package (com.unity.inputsystem) " +
                           "or enable the legacy Input Manager in Project Settings > Player > Active Input Handling.");
        }
#endif
    }
}
