using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.Settlement
{
    /// <summary>
    /// 영수증 출력기 본체 연출: 출력 중 미세 진동(0.11s 주기, ±1px), LED 앰버 핑 ↔ 초록, 상태 문구.
    /// </summary>
    public class PrinterBodyFx : MonoBehaviour
    {
        public RectTransform shakeTarget;
        [Tooltip("진동 진폭 (UI 유닛) — 원본 1px")] public float shakeAmplitude = 1.4f;
        public float shakePeriod = 0.11f;

        [Header("LED")]
        public Image led;
        public Image ledPing;
        public Color readyColor = new Color(0.20f, 0.83f, 0.60f);
        public Color printingColor = new Color(0.98f, 0.75f, 0.14f);

        [Header("Status")]
        public TMP_Text statusText;

        private Vector2 basePos;
        private bool hasBase;
        private Sequence shakeSeq;
        private Sequence pingSeq;

        private void Awake() => CacheBase();

        private void OnDisable()
        {
            shakeSeq?.Kill();
            pingSeq?.Kill();
        }

        private void CacheBase()
        {
            if (hasBase || shakeTarget == null) return;
            basePos = shakeTarget.anchoredPosition;
            hasBase = true;
        }

        public void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        public void SetPrinting(bool printing)
        {
            CacheBase();
            shakeSeq?.Kill();
            pingSeq?.Kill();
            if (shakeTarget != null) shakeTarget.anchoredPosition = basePos;

            if (printing)
            {
                SetStatus("PRINTING ▲");
                if (shakeTarget != null)
                {
                    float a = shakeAmplitude;
                    float q = shakePeriod * 0.25f;
                    shakeSeq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                        .Append(shakeTarget.DOAnchorPos(basePos + new Vector2(-1f, -0.8f) * a, q).SetEase(Ease.Linear))
                        .Append(shakeTarget.DOAnchorPos(basePos + new Vector2(1f, 0.6f) * a, q).SetEase(Ease.Linear))
                        .Append(shakeTarget.DOAnchorPos(basePos + new Vector2(-0.6f, 0.8f) * a, q).SetEase(Ease.Linear))
                        .Append(shakeTarget.DOAnchorPos(basePos, q).SetEase(Ease.Linear))
                        .SetLoops(-1, LoopType.Restart);
                }

                if (led != null) led.color = printingColor;
                if (ledPing != null)
                {
                    ledPing.gameObject.SetActive(true);
                    ledPing.color = printingColor;
                    ledPing.rectTransform.localScale = Vector3.one;
                    pingSeq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                        .Append(ledPing.rectTransform.DOScale(2.2f, 0.7f).From(1f).SetEase(Ease.OutCubic))
                        .Join(ledPing.DOFade(0f, 0.7f).From(0.85f).SetEase(Ease.OutCubic))
                        .SetLoops(-1, LoopType.Restart);
                }
            }
            else
            {
                if (led != null) led.color = readyColor;
                if (ledPing != null)
                {
                    var c = readyColor;
                    c.a = 0.35f;
                    ledPing.color = c;
                    ledPing.rectTransform.localScale = Vector3.one * 1.6f;
                }
            }
        }
    }
}
