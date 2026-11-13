using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>RawImage 위에 마우스/터치로 직접 서명하는 패드 + 자동 정자 서명 애니메이션</summary>
    [RequireComponent(typeof(RawImage))]
    public class SignaturePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public int texWidth = 520;
        public int texHeight = 170;
        public float brushRadius = 2.6f;
        public Color inkColor = new Color(0.12f, 0.16f, 0.23f, 1f);
        public Color autoInkColor = new Color(0.12f, 0.23f, 0.54f, 1f);
        public GameObject guide;
        public Action onStroke;

        public bool HasStroke { get; private set; }

        private RawImage raw;
        private Texture2D tex;
        private Color32[] pixels;
        private bool dirty;
        private bool drawing;
        private Vector2 last;
        private Tween autoTween;

        private void Awake()
        {
            raw = GetComponent<RawImage>();
            tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            pixels = new Color32[texWidth * texHeight];
            raw.texture = tex;
            Clear();
            LateUpdate(); // 새 텍스처는 회색 초기값이므로 즉시 투명으로 적용
        }

        private void OnDestroy()
        {
            if (tex != null) Destroy(tex);
        }

        public void Clear()
        {
            autoTween?.Kill();
            if (pixels == null) return;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            dirty = true;
            HasStroke = false;
            if (guide != null) guide.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            tex.SetPixels32(pixels);
            tex.Apply(false);
        }

        private Vector2 ToTex(Vector2 screen)
        {
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var local);
            Rect r = rt.rect;
            return new Vector2((local.x - r.xMin) / r.width * texWidth, (local.y - r.yMin) / r.height * texHeight);
        }

        public void OnPointerDown(PointerEventData e)
        {
            autoTween?.Kill();
            drawing = true;
            last = ToTex(e.position);
            MarkStroke();
            Dot(last, brushRadius, inkColor);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!drawing) return;
            var p = ToTex(e.position);
            Line(last, p, brushRadius, inkColor);
            last = p;
            onStroke?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) => drawing = false;

        private void MarkStroke()
        {
            HasStroke = true;
            if (guide != null) guide.SetActive(false);
        }

        /// <summary>만년필 필기체 경로를 DOTween으로 따라 그리기</summary>
        public Tween AutoSign(float duration = 0.75f)
        {
            Clear();
            MarkStroke();
            float w = texWidth, h = texHeight;
            // 리사주 + 감쇠로 만든 흘림체 곡선
            Func<float, Vector2> path = t =>
            {
                float x = Mathf.Lerp(0.14f, 0.84f, t) * w + Mathf.Sin(t * Mathf.PI * 9f) * 0.035f * w;
                float y = h * 0.5f + Mathf.Sin(t * Mathf.PI * 7f + 0.6f) * h * 0.24f * (1f - 0.45f * t)
                          + Mathf.Cos(t * Mathf.PI * 2f) * h * 0.05f;
                return new Vector2(x, y);
            };
            Vector2 prev = path(0f);
            autoTween = DOVirtual.Float(0f, 1f, duration, t =>
            {
                var p = path(t);
                Line(prev, p, brushRadius * 1.15f, autoInkColor);
                prev = p;
            }).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                // 마무리 밑줄 획
                Line(new Vector2(w * 0.2f, h * 0.2f), new Vector2(w * 0.78f, h * 0.24f), brushRadius, autoInkColor);
            });
            return autoTween;
        }

        private void Line(Vector2 a, Vector2 b, float radius, Color c)
        {
            float dist = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / (radius * 0.5f)));
            for (int i = 0; i <= steps; i++) Dot(Vector2.Lerp(a, b, i / (float)steps), radius, c);
        }

        private void Dot(Vector2 p, float radius, Color c)
        {
            int r = Mathf.CeilToInt(radius + 1f);
            int cx = Mathf.RoundToInt(p.x), cy = Mathf.RoundToInt(p.y);
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= texHeight) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= texWidth) continue;
                    float d = Vector2.Distance(new Vector2(x, y), p);
                    float a = Mathf.Clamp01(radius + 0.5f - d);
                    if (a <= 0f) continue;
                    int idx = y * texWidth + x;
                    byte na = (byte)Mathf.Max(pixels[idx].a, a * 255f);
                    pixels[idx] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), na);
                }
            }
            dirty = true;
        }
    }
}
