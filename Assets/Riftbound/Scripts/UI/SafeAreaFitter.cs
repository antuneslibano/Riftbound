using UnityEngine;

namespace Riftbound.UI
{
    /// <summary>Fits its RectTransform to Screen.safeArea (notches, rounded corners, gesture bars).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect applied;
        Vector2Int appliedScreen;

        void Awake() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied || appliedScreen.x != Screen.width || appliedScreen.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            var rt = (RectTransform)transform;
            Rect safe = Screen.safeArea;
            applied = safe;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
