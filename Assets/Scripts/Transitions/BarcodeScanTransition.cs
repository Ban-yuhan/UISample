using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 바코드 스캐너 빔 트랜지션
    /// In: 붉은 레이저 라인이 위에서 아래로 스캔하며 바코드 블라인드가 닫힘
    /// Midpoint: '삐빅!' 스캔 완료 비주얼 & 그린 승인 라이트 펄스
    /// Out: 바코드가 중앙에서 좌우로 갈라지며(Split) 씬 노출
    /// </summary>
    public class BarcodeScanTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform leftCurtain;
        [SerializeField] private RectTransform rightCurtain;
        [SerializeField] private RectTransform scanLaserLine;
        [SerializeField] private Image scanLaserImage;
        [SerializeField] private Image barcodeStatusBadge;
        [SerializeField] private Text scanStatusText;

        [Header("Colors")]
        [SerializeField] private Color laserRed = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color laserSuccessGreen = new Color(0.2f, 0.95f, 0.4f);
        [SerializeField] private Color curtainBg = new Color(0.12f, 0.12f, 0.14f);

        [Header("Timings")]
        [SerializeField] private float scanDuration = 0.48f;
        [SerializeField] private float splitDuration = 0.42f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence waitingSequence;
        private const float HalfScreenWidth = 1000f;
        private const float HalfScreenHeight = 560f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            waitingSequence?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (leftCurtain != null)
            {
                leftCurtain.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                leftCurtain.gameObject.SetActive(false);
            }

            if (rightCurtain != null)
            {
                rightCurtain.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                rightCurtain.gameObject.SetActive(false);
            }

            if (scanLaserLine != null)
            {
                scanLaserLine.anchoredPosition = new Vector2(0, HalfScreenHeight);
                scanLaserLine.gameObject.SetActive(false);
            }

            if (scanLaserImage != null)
            {
                scanLaserImage.color = laserRed;
            }

            if (barcodeStatusBadge != null)
            {
                barcodeStatusBadge.color = new Color(1, 1, 1, 0);
                barcodeStatusBadge.transform.localScale = Vector3.zero;
            }

            if (scanStatusText != null)
            {
                scanStatusText.color = new Color(1, 1, 1, 0);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
            IsPlaying = false;
        }

        public override void PlayIn(Action onInComplete = null)
        {
            if (IsPlaying) return;
            IsPlaying = true;

            KillAllTweens();

            if (container != null)
            {
                container.anchorMin = Vector2.zero;
                container.anchorMax = Vector2.one;
                container.offsetMin = Vector2.zero;
                container.offsetMax = Vector2.zero;
                container.localScale = Vector3.one;
            }

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            if (leftCurtain != null)
            {
                leftCurtain.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                leftCurtain.gameObject.SetActive(true);
            }

            if (rightCurtain != null)
            {
                rightCurtain.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                rightCurtain.gameObject.SetActive(true);
            }

            if (scanLaserLine != null)
            {
                scanLaserLine.anchoredPosition = new Vector2(0, HalfScreenHeight);
                scanLaserLine.gameObject.SetActive(false);
            }

            if (scanLaserImage != null)
            {
                scanLaserImage.color = laserRed;
            }

            inSequence = DOTween.Sequence();

            // 1. 좌우 바코드 커튼이 중앙으로 완전히 닫힘 (0.00s ~ 0.22s)
            float curtainDuration = scanDuration * 0.45f;
            if (leftCurtain != null)
                inSequence.Append(leftCurtain.DOAnchorPosX(0f, curtainDuration).SetEase(Ease.OutQuad));

            if (rightCurtain != null)
                inSequence.Join(rightCurtain.DOAnchorPosX(0f, curtainDuration).SetEase(Ease.OutQuad));

            // 2. 화면이 완전히 닫힌 후 비로소 레이저 스캐너 빔 출현 및 위 -> 아래 스캔 (0.22s ~ 0.48s)
            inSequence.AppendCallback(() =>
            {
                if (scanLaserLine != null)
                {
                    scanLaserLine.anchoredPosition = new Vector2(0, HalfScreenHeight);
                    scanLaserLine.gameObject.SetActive(true);
                }
            });

            if (scanLaserLine != null)
                inSequence.Append(scanLaserLine.DOAnchorPosY(-HalfScreenHeight, scanDuration * 0.55f).SetEase(Ease.Linear));

            // 3. 스캔 완료 즉시 그린 승인 컬러 변환 및 성공 배지 팝업
            if (scanLaserImage != null)
                inSequence.Append(scanLaserImage.DOColor(laserSuccessGreen, 0.08f));

            if (barcodeStatusBadge != null)
            {
                inSequence.Join(barcodeStatusBadge.DOFade(1f, 0.12f));
                inSequence.Join(barcodeStatusBadge.transform.DOScale(1f, 0.16f).SetEase(Ease.OutBack));
            }

            if (scanStatusText != null)
            {
                scanStatusText.text = "✔ BARCODE VERIFIED [OK]";
                inSequence.Join(scanStatusText.DOFade(1f, 0.12f));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            waitingSequence?.Kill();
            if (barcodeStatusBadge != null)
            {
                // 로딩 중 그린 승인 펄스 루프
                waitingSequence = DOTween.Sequence();
                waitingSequence.Append(barcodeStatusBadge.transform.DOScale(1.08f, 0.35f).SetEase(Ease.InOutSine))
                               .Append(barcodeStatusBadge.transform.DOScale(1.0f, 0.35f).SetEase(Ease.InOutSine))
                               .SetLoops(-1);
            }
        }

        public override void StopWaitingLoop()
        {
            waitingSequence?.Kill();
            if (barcodeStatusBadge != null)
            {
                barcodeStatusBadge.transform.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            if (barcodeStatusBadge != null)
                outSequence.Append(barcodeStatusBadge.transform.DOScale(0f, 0.15f).SetEase(Ease.InBack));

            if (scanLaserLine != null)
                outSequence.Join(scanLaserLine.DOScaleY(0f, 0.12f));

            // 4. 바코드가 중앙에서 좌우로 갈라지며(Split) 씬 오픈
            if (leftCurtain != null)
                outSequence.Append(leftCurtain.DOAnchorPosX(-HalfScreenWidth, splitDuration).SetEase(Ease.InCubic));

            if (rightCurtain != null)
                outSequence.Join(rightCurtain.DOAnchorPosX(HalfScreenWidth, splitDuration).SetEase(Ease.InCubic));

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
