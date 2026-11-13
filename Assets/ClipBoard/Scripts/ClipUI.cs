using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>클립보드 씬에서 사용하는 공용 스프라이트/폰트 묶음 (빌더가 생성해서 컨트롤러에 연결)</summary>
    [Serializable]
    public class ClipTheme
    {
        public TMP_FontAsset font;
        public Sprite round;      // 9-slice 둥근 사각형 (모서리 반경 = ClipUI.RoundRadius px)
        public Sprite circle;     // 원
        public Sprite ring;       // 원 테두리(이중선) - 직인
        public Sprite frame;      // 둥근 사각 이중 테두리 - 고무도장 자국
        public Sprite glow;       // 부드러운 원형 그라디언트
        public Sprite dash;       // 가로 점선 타일
        public Sprite dashV;      // 세로 점선 타일
        public Sprite ruled;      // 종이 줄 타일
        public Sprite star;       // 별
    }

    /// <summary>uGUI 요소를 코드로 만드는 헬퍼 (에디터 빌더 + 런타임 행 생성 공용)</summary>
    public static class ClipUI
    {
        public const float RoundRadius = 32f;
        public static ClipTheme T;

        public static void Use(ClipTheme theme) => T = theme;

        public static RectTransform RT(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, float radius = -1f)
        {
            var rt = RT(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && radius > 0f)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = RoundRadius / radius;
            }
            return img;
        }

        /// <summary>둥근 사각형 + 테두리. 반환값은 내부(채움) 이미지, outer로 테두리 이미지</summary>
        public static Image Card(string name, Transform parent, Color fill, Color border, float radius, float borderW, out Image outer)
        {
            outer = Img(name, parent, T.round, border, radius);
            var inner = Img("Fill", outer.transform, T.round, fill, Mathf.Max(1f, radius - borderW));
            Stretch(inner.rectTransform, borderW, borderW, borderW, borderW);
            return inner;
        }

        public static TextMeshProUGUI Txt(string name, Transform parent, string s, float size, Color c,
            TextAlignmentOptions align = TextAlignmentOptions.Left, bool bold = false)
        {
            var rt = RT(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = T.font;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            t.richText = true;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.characterSpacing = -2f;
            return t;
        }

        public static Button Btn(string name, Transform parent, string label, float fontSize, Color fill, Color border, Color textColor, float radius, out TextMeshProUGUI text)
        {
            var bg = Card(name, parent, fill, border, radius, 2f, out var outer);
            outer.raycastTarget = true;
            var btn = outer.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            outer.gameObject.AddComponent<PressScale>();
            text = Txt("Label", outer.transform, label, fontSize, textColor, TextAlignmentOptions.Center, true);
            Stretch(text.rectTransform);
            return btn;
        }

        /// <summary>라벨 + 수치 + 가로 게이지. fill 이미지를 반환</summary>
        public static Image StatBar(Transform parent, string label, string value, float value01, Color barColor, Color valueColor, float x, float y, float w)
        {
            var root = RT("Stat_" + label, parent);
            TopLeft(root, x, y, w, 34);
            var l = Txt("Label", root, label, 15, Hex("#57534e"), TextAlignmentOptions.TopLeft, true);
            TopLeft(l.rectTransform, 0, 0, w, 20);
            var v = Txt("Value", root, value, 15, valueColor, TextAlignmentOptions.TopRight, true);
            TopLeft(v.rectTransform, 0, 0, w, 20);
            var track = Img("Track", root, T.round, Hex("#e7e5e4"), 5);
            TopLeft(track.rectTransform, 0, 22, w, 9);
            var fill = Img("Fill", track.transform, T.round, barColor, 5);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            return fill;
        }

        public static Image DashH(string name, Transform parent, Color c, float thickness = 2f)
        {
            var img = Img(name, parent, T.dash, c);
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 1f;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, thickness);
            return img;
        }

        public static Image DashV(string name, Transform parent, Color c)
        {
            var img = Img(name, parent, T.dashV, c);
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        // ---------------- RectTransform 배치 ----------------

        public static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>부모 좌상단 기준 (y는 아래로 +)</summary>
        public static void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>부모 우상단 기준 (x는 왼쪽으로 +, y는 아래로 +)</summary>
        public static void TopRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>부모 왼쪽 세로 중앙 기준</summary>
        public static void MidLeft(RectTransform rt, float x, float yOffset, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, yOffset);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void MidRight(RectTransform rt, float x, float yOffset, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-x, yOffset);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static LayoutElement Height(Component c, float h)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            return le;
        }

        public static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            c.a = a;
            return c;
        }

        public static Color WithA(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static string Gold(long v) => v.ToString("N0") + "G";

        /// <summary>비활성 계층에서도 동작하는 텍스트 선호 폭</summary>
        public static float PrefW(TextMeshProUGUI t) => t.GetPreferredValues(t.text).x;
    }
}
