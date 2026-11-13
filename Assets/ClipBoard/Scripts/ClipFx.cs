using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>파티클 버스트 / 플로팅 텍스트 / 도장 자국 슬램 등 DOTween 연출 모음</summary>
    public class ClipFx : MonoBehaviour
    {
        public RectTransform layer; // 최상단 FX 레이어 (stretch)

        private Vector2 ToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local);
            return local;
        }

        /// <summary>CSS의 burstParticles: 사방으로 튀고 중력으로 떨어지며 사라지는 원형 입자</summary>
        public void Burst(Vector2 screenPos, int count, params Color[] colors)
        {
            Vector2 origin = ToLocal(screenPos);
            for (int i = 0; i < count; i++)
            {
                var img = ClipUIRuntime.SimpleImage("Particle", layer, ClipUI.T.circle, colors[Random.Range(0, colors.Length)]);
                float size = Random.Range(7f, 15f);
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = origin;

                float ang = Random.Range(0f, Mathf.PI * 2f);
                float spd = Random.Range(160f, 520f);
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd + new Vector2(0f, 160f);
                const float gravity = -1100f;
                float life = Random.Range(0.55f, 0.85f);

                DOVirtual.Float(0f, 1f, life, t =>
                {
                    if (rt == null) return;
                    float s = t * life;
                    rt.anchoredPosition = origin + vel * s + new Vector2(0f, 0.5f * gravity * s * s);
                    var c = img.color;
                    c.a = 1f - t;
                    img.color = c;
                }).SetEase(Ease.Linear).OnComplete(() => { if (rt != null) Destroy(rt.gameObject); });
            }
        }

        /// <summary>하얀 알약 배경 위에 떠오르며 사라지는 문구</summary>
        public void FloatText(string text, Vector2 screenPos, Color color)
        {
            var bg = ClipUI.Img("FloatText", layer, ClipUI.T.round, ClipUI.Hex("#ffffff", 0.96f), 18);
            var label = ClipUI.Txt("Label", bg.transform, text, 20, color, TextAlignmentOptions.Center, true);
            ClipUI.Stretch(label.rectTransform);
            float w = ClipUI.PrefW(label) + 36f;
            var rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(w, 40f);

            Vector2 p = ToLocal(screenPos);
            Rect r = layer.rect;
            p.x = Mathf.Clamp(p.x, r.xMin + w * 0.5f + 10f, r.xMax - w * 0.5f - 10f);
            rt.anchoredPosition = p + new Vector2(0f, -8f);
            rt.localScale = Vector3.one * 0.85f;

            var cg = bg.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var seq = DOTween.Sequence();
            seq.Append(cg.DOFade(1f, 0.25f));
            seq.Join(rt.DOAnchorPosY(p.y + 14f, 0.25f).SetEase(Ease.OutQuad));
            seq.Join(rt.DOScale(1.1f, 0.25f).SetEase(Ease.OutBack));
            seq.Append(rt.DOAnchorPosY(p.y + 46f, 0.9f).SetEase(Ease.OutSine));
            seq.Join(rt.DOScale(0.98f, 0.9f));
            seq.Join(cg.DOFade(0f, 0.9f).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(bg.gameObject));
        }

        /// <summary>CSS stampSlam: 크게 → 작게(쾅) → 살짝 튐 → 정착</summary>
        public static Sequence SlamIn(RectTransform mark, float finalRot, float finalAlpha = 0.96f)
        {
            var cg = mark.GetComponent<CanvasGroup>();
            if (cg == null) cg = mark.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            mark.localScale = Vector3.one * 2.6f;
            mark.localRotation = Quaternion.Euler(0, 0, finalRot - 9f);

            var seq = DOTween.Sequence();
            seq.Append(mark.DOScale(0.9f, 0.2f).SetEase(Ease.InQuad));
            seq.Join(cg.DOFade(1f, 0.14f));
            seq.Join(mark.DOLocalRotate(new Vector3(0, 0, finalRot + 1f), 0.2f));
            seq.Append(mark.DOScale(1.06f, 0.07f).SetEase(Ease.OutQuad));
            seq.Join(mark.DOLocalRotate(new Vector3(0, 0, finalRot - 2f), 0.07f));
            seq.Append(mark.DOScale(1f, 0.08f));
            seq.Join(mark.DOLocalRotate(new Vector3(0, 0, finalRot), 0.08f));
            seq.Join(cg.DOFade(finalAlpha, 0.08f));
            return seq;
        }
    }
}
