using Riftbound.View;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Riftbound.UI
{
    /// <summary>Tiny helpers to build uGUI from code (no prefabs needed for the prototype).</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors in normalised parent space + pixel offsets (reference resolution units).</summary>
        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt) => rt.Anchor(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static Image Panel(Transform parent, string name, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = ShapeLibrary.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, int fontSize, Color bg, UnityAction onClick, out Text text)
        {
            var img = Panel(parent, name, bg, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            text = Label(img.transform, "Text", label, fontSize, Color.white);
            text.rectTransform.Stretch();
            return btn;
        }

        public static Button Button(Transform parent, string name, string label, int fontSize, Color bg, UnityAction onClick)
            => Button(parent, name, label, fontSize, bg, onClick, out _);

        /// <summary>Simple outline made of 4 thin images (no sprite required).</summary>
        public static Image[] Outline(Transform parent, Color color, float thickness)
        {
            var result = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var img = Panel(parent, "Outline" + i, color);
                var rt = img.rectTransform;
                switch (i)
                {
                    case 0: rt.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -thickness), Vector2.zero); break;
                    case 1: rt.Anchor(new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, thickness)); break;
                    case 2: rt.Anchor(new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(thickness, 0)); break;
                    default: rt.Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-thickness, 0), Vector2.zero); break;
                }
                result[i] = img;
            }
            return result;
        }

        public static void SetColor(Image[] images, Color c)
        {
            if (images == null) return;
            foreach (var img in images) img.color = c;
        }

        /// <summary>Formats seconds as MM:SS without allocating more than the final string.</summary>
        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int m = s / 60;
            s %= 60;
            return (m < 10 ? "0" : "") + m + ":" + (s < 10 ? "0" : "") + s;
        }
    }
}
