using System.Collections.Generic;
using Riftbound.Config;
using UnityEngine;

namespace Riftbound.View
{
    /// <summary>
    /// Procedurally generated placeholder sprites (circle, triangle, square, hexagon, ring...) and world text.
    /// Nothing is loaded from disk: shapes are rasterised once from signed distance functions at startup.
    /// Every sprite is 1 world unit wide at scale 1.
    /// </summary>
    public static class ShapeLibrary
    {
        public enum Shape { Circle, Triangle, Square, Hexagon, Diamond, Ring, Pixel }

        const int Resolution = 128;
        static readonly Dictionary<Shape, Sprite> cache = new Dictionary<Shape, Sprite>();
        static Font font;

        public static Sprite Get(UnitShape s)
        {
            switch (s)
            {
                case UnitShape.Triangle: return Get(Shape.Triangle);
                case UnitShape.Square: return Get(Shape.Square);
                case UnitShape.Hexagon: return Get(Shape.Hexagon);
                case UnitShape.Diamond: return Get(Shape.Diamond);
                default: return Get(Shape.Circle);
            }
        }

        public static Sprite Get(Shape s)
        {
            if (cache.TryGetValue(s, out var sprite) && sprite != null) return sprite;
            sprite = s == Shape.Pixel ? BuildPixel() : Build(s);
            cache[s] = sprite;
            return sprite;
        }

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                // Unity 2022.2+ ships "LegacyRuntime.ttf"; older versions "Arial.ttf".
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return font;
            }
        }

        // ------------------------------------------------------------------ factories

        public static SpriteRenderer CreateSprite(Transform parent, string name, Shape shape, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Get(shape);
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        /// <summary>World-space text. <paramref name="height"/> is the approximate line height in world units.</summary>
        public static TextMesh CreateText(Transform parent, string name, string text, float height, Color color, int sortingOrder,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.fontSize = 48;
            // TextMesh line height ≈ fontSize * characterSize * 0.1 world units.
            tm.characterSize = height * 10f / tm.fontSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            if (Font != null) mr.sharedMaterial = Font.material;
            mr.sortingOrder = sortingOrder;
            return tm;
        }

        /// <summary>Positions/rotates/scales a Pixel sprite so it draws a line from a to b.</summary>
        public static void SetLine(Transform line, Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a;
            line.localPosition = (a + b) * 0.5f;
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            line.localScale = new Vector3(d.magnitude, width, 1f);
        }

        // ------------------------------------------------------------------ rasterisation

        static Sprite BuildPixel()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
        }

        static Sprite Build(Shape shape)
        {
            int n = Resolution;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Shape_" + shape,
            };
            var px = new Color32[n * n];
            float pixel = 2f / n; // size of a pixel in normalised [-1,1] space
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                float d = Sdf(shape, p);
                float a = Mathf.Clamp01(0.5f - d / pixel);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
        }

        static float Sdf(Shape shape, Vector2 p)
        {
            switch (shape)
            {
                case Shape.Circle:
                    return p.magnitude - 0.96f;
                case Shape.Ring:
                    return Mathf.Abs(p.magnitude - 0.9f) - 0.07f;
                case Shape.Square:
                    return Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - 0.9f;
                case Shape.Diamond:
                    return (Mathf.Abs(p.x) + Mathf.Abs(p.y)) * 0.7071f - 0.68f;
                case Shape.Triangle:
                    return SdTriangle(new Vector2(p.x, p.y + 0.18f), 0.85f);
                case Shape.Hexagon:
                    return SdHexagon(new Vector2(p.y, p.x), 0.84f);
                default:
                    return -1f;
            }
        }

        // Equilateral triangle pointing up (Inigo Quilez).
        static float SdTriangle(Vector2 p, float r)
        {
            const float k = 1.7320508f;
            p.x = Mathf.Abs(p.x) - r;
            p.y = p.y + r / k;
            if (p.x + k * p.y > 0f) p = new Vector2(p.x - k * p.y, -k * p.x - p.y) * 0.5f;
            p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
            return -p.magnitude * Mathf.Sign(p.y);
        }

        // Regular hexagon, r = inner radius (Inigo Quilez).
        static float SdHexagon(Vector2 p, float r)
        {
            const float kx = -0.866025404f, ky = 0.5f, kz = 0.577350269f;
            p = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            float dot = Mathf.Min(kx * p.x + ky * p.y, 0f);
            p -= 2f * dot * new Vector2(kx, ky);
            p -= new Vector2(Mathf.Clamp(p.x, -kz * r, kz * r), r);
            return p.magnitude * Mathf.Sign(p.y);
        }
    }
}
