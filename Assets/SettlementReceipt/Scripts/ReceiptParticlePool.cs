using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.Settlement
{
    /// <summary>
    /// 수치 팝 순간 터지는 색종이 파티클 (원본 캔버스 파티클 이식).
    /// 원본은 60fps 프레임 단위 값이라 초 단위로 환산: 속도 2.2~7.4px/f → ×60, 중력 0.16px/f² → ×3600, 수명 1/0.025 = 40f ≈ 0.67s.
    /// 오브젝트 풀링 + DOTween 가상 트윈으로 포물선 궤적을 계산한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ReceiptParticlePool : MonoBehaviour
    {
        public Sprite circleSprite;
        [Tooltip("CSS px → UI 유닛 배율")] public float unitScale = 1.4f;
        public int prewarm = 40;

        private readonly Stack<Image> free = new Stack<Image>();
        private readonly List<Tween> live = new List<Tween>();
        private RectTransform rect;

        private void Awake()
        {
            rect = (RectTransform)transform;
            for (int i = 0; i < prewarm; i++) free.Push(Create());
        }

        private void OnDisable() => KillAll();

        public void KillAll()
        {
            var snapshot = live.ToArray();
            live.Clear();
            foreach (var t in snapshot) t?.Kill();
        }

        public void Burst(RectTransform source, Color[] colors, int count)
        {
            if (source == null || colors == null || colors.Length == 0 || count <= 0) return;

            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);
            Vector2 a = rect.InverseTransformPoint(corners[0]);
            Vector2 b = rect.InverseTransformPoint(corners[2]);
            Vector2 center = (a + b) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));

            float u = unitScale;
            for (int i = 0; i < count; i++)
            {
                Image img = free.Count > 0 ? free.Pop() : Create();
                RectTransform rt = img.rectTransform;
                img.gameObject.SetActive(true);

                bool circle = Random.value > 0.45f;
                float s = (3f + Random.value * 4f) * u;
                img.sprite = circle ? circleSprite : null;
                rt.sizeDelta = circle ? new Vector2(s * 2f, s * 2f) : new Vector2(s * 2f, s * 1.3f);

                Color col = colors[Random.Range(0, colors.Length)];
                img.color = col;

                Vector2 p0 = center + new Vector2((Random.value - 0.5f) * size.x * 0.5f, (Random.value - 0.5f) * size.y * 0.4f);
                float angle = Random.value * Mathf.PI * 2f;
                float speed = (2.2f + Random.value * 5.2f) * 60f * u;
                Vector2 v0 = new Vector2(Mathf.Cos(angle) * speed, -Mathf.Sin(angle) * speed + 2.2f * 60f * u);
                float g = -0.16f * 3600f * u;
                float rot0 = Random.value * 360f;
                float vRot = (Random.value - 0.5f) * 0.25f * 60f * Mathf.Rad2Deg;
                const float life = 40f / 60f;

                rt.anchoredPosition = p0;
                rt.localEulerAngles = new Vector3(0, 0, rot0);

                Tween tw = null;
                tw = DOVirtual.Float(0f, life, life, t =>
                    {
                        rt.anchoredPosition = p0 + v0 * t + 0.5f * new Vector2(0f, g) * t * t;
                        rt.localEulerAngles = new Vector3(0f, 0f, rot0 + vRot * t);
                        var c = col;
                        c.a = 1f - t / life;
                        img.color = c;
                    })
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .OnKill(() =>
                    {
                        live.Remove(tw);
                        if (img != null)
                        {
                            img.gameObject.SetActive(false);
                            free.Push(img);
                        }
                    });
                live.Add(tw);
            }
        }

        private Image Create()
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }
    }
}
