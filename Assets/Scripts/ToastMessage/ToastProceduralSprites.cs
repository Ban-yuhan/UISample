using System.Collections.Generic;
using UnityEngine;

namespace ToastMessageSample
{
    public static class ToastProceduralSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite GetCircle() => GetOrCreate("Circle", CreateCircleSprite);
        public static Sprite GetCircleRing() => GetOrCreate("CircleRing", CreateCircleRingSprite);
        public static Sprite GetAuraGlow() => GetOrCreate("AuraGlow", CreateAuraGlowSprite);
        public static Sprite GetCapsulePill() => GetOrCreate("CapsulePill", CreateCapsulePillSprite);
        public static Sprite GetCapsuleOutline() => GetOrCreate("CapsuleOutline", CreateCapsuleOutlineSprite);
        public static Sprite GetRoundedRect() => GetOrCreate("RoundedRect", CreateRoundedRectSprite);
        public static Sprite GetRoundedRectOutline() => GetOrCreate("RoundedRectOutline", CreateRoundedRectOutlineSprite);
        public static Sprite GetSparkle4Point() => GetOrCreate("Sparkle4Point", CreateSparkle4PointSprite);
        public static Sprite GetGlintStrip() => GetOrCreate("GlintStrip", CreateGlintStripSprite);

        public static Sprite GetIcon(ToastIconType type)
        {
            string key = "Icon_" + type;
            return GetOrCreate(key, () => CreateIconSprite(type));
        }

        private static Sprite GetOrCreate(string key, System.Func<Sprite> factory)
        {
            if (Cache.TryGetValue(key, out Sprite existing) && existing != null)
            {
                return existing;
            }

            Sprite created = factory();
            Cache[key] = created;
            return created;
        }

        private static Sprite MakeSprite(Texture2D tex, Vector4 border = default)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(false, true);
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
        }

        private static float SdfRoundedBox(Vector2 p, Vector2 halfSize, float radius)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + new Vector2(radius, radius);
            return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude - radius;
        }

        private static float SdfSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude;
        }

        private static float SdfTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(
                Vector2.Min(
                    new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                    new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        private static float SmoothAlpha(float dist, float aa = 1.2f)
        {
            return Mathf.Clamp01(0.5f - dist / aa);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f - 1.5f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude - r;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex);
        }

        private static Sprite CreateCircleRingSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f - 3f;
            float halfStroke = 1.8f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs((new Vector2(x + 0.5f, y + 0.5f) - center).magnitude - r) - halfStroke;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex);
        }

        private static Sprite CreateAuraGlowSprite()
        {
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float maxR = size * 0.5f;
            Vector2 center = new Vector2(maxR, maxR);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float norm = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / maxR;
                    float a = Mathf.Clamp01(1f - norm);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return MakeSprite(tex);
        }

        private static Sprite CreateCapsulePillSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 half = new Vector2(size * 0.5f - 1f, size * 0.5f - 1f);
            float radius = size * 0.5f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = SdfRoundedBox(new Vector2(x + 0.5f, y + 0.5f) - center, half, radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex, new Vector4(31, 31, 31, 31));
        }

        private static Sprite CreateCapsuleOutlineSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 half = new Vector2(size * 0.5f - 2.5f, size * 0.5f - 2.5f);
            float radius = size * 0.5f - 2.5f;
            float halfStroke = 1.8f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(SdfRoundedBox(new Vector2(x + 0.5f, y + 0.5f) - center, half, radius)) - halfStroke;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex, new Vector4(31, 31, 31, 31));
        }

        private static Sprite CreateRoundedRectSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 half = new Vector2(size * 0.5f - 1f, size * 0.5f - 1f);
            const float radius = 14f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = SdfRoundedBox(new Vector2(x + 0.5f, y + 0.5f) - center, half, radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex, new Vector4(18, 18, 18, 18));
        }

        private static Sprite CreateRoundedRectOutlineSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 half = new Vector2(size * 0.5f - 2.5f, size * 0.5f - 2.5f);
            const float radius = 14f;
            const float halfStroke = 1.6f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(SdfRoundedBox(new Vector2(x + 0.5f, y + 0.5f) - center, half, radius)) - halfStroke;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex, new Vector4(18, 18, 18, 18));
        }

        private static Sprite CreateSparkle4PointSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - half) / (half - 1.5f);
                    float py = Mathf.Abs(y + 0.5f - half) / (half - 1.5f);
                    // Superellipse (astroid-like) 4-point star
                    float val = Mathf.Pow(px, 0.52f) + Mathf.Pow(py, 0.52f);
                    float d = (val - 1f) * 8f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, SmoothAlpha(d)));
                }
            }
            return MakeSprite(tex);
        }

        private static Sprite CreateGlintStripSprite()
        {
            const int w = 32;
            const int h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (x + 0.5f) / w;
                    float a = Mathf.Sin(t * Mathf.PI);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return MakeSprite(tex);
        }

        private static Sprite CreateIconSprite(ToastIconType type)
        {
            const int size = 40;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float a = EvalIconAlpha(type, p);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return MakeSprite(tex);
        }

        private static float EvalIconAlpha(ToastIconType type, Vector2 p)
        {
            switch (type)
            {
                case ToastIconType.Siren:
                {
                    // Exclamation circle/siren dome
                    float dome = SdfRoundedBox(p - new Vector2(0f, -1f), new Vector2(8f, 9f), 7f);
                    float baseBar = SdfRoundedBox(p - new Vector2(0f, -10.5f), new Vector2(11f, 2.2f), 1.5f);
                    float ray1 = SdfSegment(p, new Vector2(-12f, 6f), new Vector2(-15f, 9f)) - 1.3f;
                    float ray2 = SdfSegment(p, new Vector2(12f, 6f), new Vector2(15f, 9f)) - 1.3f;
                    float ray3 = SdfSegment(p, new Vector2(0f, 11f), new Vector2(0f, 15f)) - 1.3f;
                    float d = Mathf.Min(Mathf.Min(dome, baseBar), Mathf.Min(ray1, Mathf.Min(ray2, ray3)));
                    return SmoothAlpha(d);
                }

                case ToastIconType.Flame:
                {
                    float outer = (p - new Vector2(0f, -2f)).magnitude - 10.5f;
                    float topTri = SdfTriangle(p, new Vector2(-9f, -1f), new Vector2(9f, -1f), new Vector2(1f, 14f));
                    float body = Mathf.Min(outer, topTri);
                    float innerHole = (p - new Vector2(0f, -4.5f)).magnitude - 4.2f;
                    float d = Mathf.Max(body, -innerHole);
                    return SmoothAlpha(d);
                }

                case ToastIconType.WarningTriangle:
                {
                    float tri = SdfTriangle(p, new Vector2(-13f, -10f), new Vector2(13f, -10f), new Vector2(0f, 12.5f)) - 1.5f;
                    float stem = SdfSegment(p, new Vector2(0f, 4.5f), new Vector2(0f, -2.5f)) - 1.6f;
                    float dot = (p - new Vector2(0f, -6.5f)).magnitude - 1.7f;
                    float cutout = Mathf.Min(stem, dot);
                    float d = Mathf.Max(tri, -cutout);
                    return SmoothAlpha(d);
                }

                case ToastIconType.Bolt:
                {
                    float t1 = SdfTriangle(p, new Vector2(-1f, 14f), new Vector2(-9f, -1f), new Vector2(2f, -1f));
                    float t2 = SdfTriangle(p, new Vector2(1f, -14f), new Vector2(9f, 1f), new Vector2(-2f, 1f));
                    return SmoothAlpha(Mathf.Min(t1, t2));
                }

                case ToastIconType.Truck:
                {
                    float cargo = SdfRoundedBox(p - new Vector2(-4f, 1.5f), new Vector2(9f, 7f), 2f);
                    float cab = SdfRoundedBox(p - new Vector2(9f, -0.5f), new Vector2(4.5f, 5f), 2f);
                    float w1 = (p - new Vector2(-7f, -8f)).magnitude - 3.3f;
                    float w2 = (p - new Vector2(8.5f, -8f)).magnitude - 3.3f;
                    float d = Mathf.Min(Mathf.Min(cargo, cab), Mathf.Min(w1, w2));
                    return SmoothAlpha(d);
                }

                case ToastIconType.Box:
                {
                    float box = SdfRoundedBox(p, new Vector2(10.5f, 9.5f), 2f);
                    float slit = SdfSegment(p, new Vector2(-10f, 3.5f), new Vector2(10f, 3.5f)) - 1.1f;
                    float tape = SdfRoundedBox(p - new Vector2(0f, 6.5f), new Vector2(2.5f, 3f), 0.5f);
                    float d = Mathf.Max(box, -slit);
                    d = Mathf.Max(d, -tape);
                    return SmoothAlpha(d);
                }

                case ToastIconType.Coin:
                {
                    float outer = p.magnitude - 12f;
                    float ring = Mathf.Abs(p.magnitude - 8.2f) - 1.1f;
                    float bar = SdfRoundedBox(p, new Vector2(1.5f, 4.5f), 0.8f);
                    float d = Mathf.Max(outer, -ring);
                    d = Mathf.Min(d, bar);
                    return SmoothAlpha(d);
                }

                case ToastIconType.Coffee:
                {
                    float cup = SdfRoundedBox(p - new Vector2(-1.5f, -2.5f), new Vector2(8f, 7f), 2.5f);
                    float handle = Mathf.Abs((p - new Vector2(7.5f, -2f)).magnitude - 4f) - 1.4f;
                    float steam1 = SdfSegment(p, new Vector2(-4f, 7f), new Vector2(-3f, 12f)) - 1.1f;
                    float steam2 = SdfSegment(p, new Vector2(1f, 7f), new Vector2(2f, 12f)) - 1.1f;
                    float d = Mathf.Min(Mathf.Min(cup, handle), Mathf.Min(steam1, steam2));
                    return SmoothAlpha(d);
                }

                case ToastIconType.CloseX:
                default:
                {
                    float s1 = SdfSegment(p, new Vector2(-7.5f, -7.5f), new Vector2(7.5f, 7.5f)) - 1.9f;
                    float s2 = SdfSegment(p, new Vector2(-7.5f, 7.5f), new Vector2(7.5f, -7.5f)) - 1.9f;
                    return SmoothAlpha(Mathf.Min(s1, s2));
                }
            }
        }
    }
}
