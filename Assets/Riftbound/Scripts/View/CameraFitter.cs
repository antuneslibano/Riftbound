using UnityEngine;

namespace Riftbound.View
{
    /// <summary>
    /// Makes the orthographic camera render the whole map inside the UI's field area
    /// (between the top and bottom bars, inside the safe area) for any screen aspect ratio.
    /// </summary>
    public class CameraFitter : MonoBehaviour
    {
        public Camera Target;
        public RectTransform FieldArea;
        /// <summary>Half size (world units) of the region that must always be visible.</summary>
        public Vector2 MapHalfExtents = new Vector2(4f, 6f);

        readonly Vector3[] corners = new Vector3[4];
        Rect lastPixelRect;
        int lastW, lastH;

        void LateUpdate()
        {
            if (Target == null || FieldArea == null) return;
            FieldArea.GetWorldCorners(corners); // overlay canvas: world == screen pixels
            var r = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
            if (r == lastPixelRect && lastW == Screen.width && lastH == Screen.height) return;
            lastPixelRect = r;
            lastW = Screen.width;
            lastH = Screen.height;
            if (r.width < 10f || r.height < 10f || Screen.width <= 0 || Screen.height <= 0) return;

            Target.rect = new Rect(r.x / Screen.width, r.y / Screen.height, r.width / Screen.width, r.height / Screen.height);
            float aspect = r.width / r.height;
            Target.orthographicSize = Mathf.Max(MapHalfExtents.y, MapHalfExtents.x / aspect);
        }
    }
}
