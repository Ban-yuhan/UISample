using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 배송 영수증 슬라이드 & 합격 도장 스탬프 쾅! 트랜지션
    /// </summary>
    public class StampInvoiceTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform invoicePaper;
        [SerializeField] private RectTransform stampMark;
        [SerializeField] private Text stampText;

        [Header("Colors (Warm Paper & Ink)")]
        [SerializeField] private Color paperColor = new Color(0.97f, 0.94f, 0.88f); // 크림 베이지 양식지 (#F8F0E0)
        [SerializeField] private Color stampInkColor = new Color(0.85f, 0.22f, 0.18f); // 빈티지 레드 잉크 (#D8382E)

        [Header("Timings")]
        [SerializeField] private float paperSlideDuration = 0.45f;
        [SerializeField] private float stampDropDuration = 0.25f;
        [SerializeField] private float paperDropOutDuration = 0.4f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Tween waitingTween;
        private const float ScreenHeight = 1200f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            waitingTween?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (invoicePaper != null)
            {
                invoicePaper.anchoredPosition = new Vector2(0, ScreenHeight);
                invoicePaper.localRotation = Quaternion.Euler(0, 0, 3f);
                invoicePaper.gameObject.SetActive(false);
            }

            if (stampMark != null)
            {
                stampMark.localScale = Vector3.one * 2.5f;
                stampMark.gameObject.SetActive(false);
            }

            if (stampText != null)
            {
                stampText.color = new Color(stampInkColor.r, stampInkColor.g, stampInkColor.b, 0f);
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

            if (invoicePaper != null)
            {
                invoicePaper.anchoredPosition = new Vector2(0, ScreenHeight);
                invoicePaper.localRotation = Quaternion.Euler(0, 0, 3f);
                invoicePaper.gameObject.SetActive(true);
            }
            if (stampMark != null)
            {
                stampMark.localScale = Vector3.one * 2.5f;
                stampMark.gameObject.SetActive(false);
            }
            if (stampText != null)
            {
                stampText.color = new Color(stampInkColor.r, stampInkColor.g, stampInkColor.b, 0f);
            }

            inSequence = DOTween.Sequence();

            // 1. 송장 착지
            if (invoicePaper != null)
            {
                inSequence.Append(invoicePaper.DOAnchorPosY(0f, paperSlideDuration).SetEase(Ease.OutQuad));
                inSequence.Join(invoicePaper.DORotate(Vector3.zero, paperSlideDuration).SetEase(Ease.OutQuad));
            }

            // 2. 스탬프 쾅!
            if (stampMark != null)
            {
                inSequence.AppendCallback(() => stampMark.gameObject.SetActive(true));
                inSequence.Append(stampMark.DOScale(1f, stampDropDuration).SetEase(Ease.InQuad));
            }
            if (stampText != null)
            {
                inSequence.Join(stampText.DOFade(1f, stampDropDuration * 0.5f));
            }

            if (stampMark != null)
                inSequence.Append(stampMark.DOPunchScale(Vector3.one * 0.25f, 0.25f, 10, 1));
            if (invoicePaper != null)
                inSequence.Join(invoicePaper.DOPunchPosition(new Vector3(0, -15f, 0), 0.25f, 8, 1));

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            if (stampMark != null)
            {
                waitingTween = stampMark.DOPunchScale(Vector3.one * 0.08f, 0.6f, 2, 0.5f).SetLoops(-1);
            }
        }

        public override void StopWaitingLoop()
        {
            waitingTween?.Kill();
            if (stampMark != null)
            {
                stampMark.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            if (invoicePaper != null)
            {
                outSequence.Append(invoicePaper.DOAnchorPosY(-ScreenHeight, paperDropOutDuration).SetEase(Ease.InCubic));
                outSequence.Join(invoicePaper.DORotate(new Vector3(0, 0, -8f), paperDropOutDuration).SetEase(Ease.InCubic));
            }

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
