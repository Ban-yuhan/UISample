using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 영수증 롤 초고속 출력 & 절취선 뜯김 트랜지션 (구 BubbleWrap 대체)
    /// PlayIn (0.42s): 상단 영수증 프린터 슬롯에서 긴 크림화이트 영수증이 "지지직!" 쏟아져 나와 화면을 100% 덮음 (Ease.OutCubic)
    /// WaitingLoop: 붉은색 승인 도장 [ PAID ] 쾅! 찍히거나 결제 완료 체크 반짝임
    /// PlayOut (0.38s): 절취선이 뜯어지며 영수증이 아래로 스르륵 낙하 퇴장 (Ease.InQuad)
    /// </summary>
    public class ReceiptRollTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform printerSlot;
        [SerializeField] private RectTransform receiptPaper;
        [SerializeField] private RectTransform paidStamp;
        [SerializeField] private Text paidStampText;
        [SerializeField] private RectTransform perforatedLine;

        [Header("Colors (Paper Receipt)")]
        [SerializeField] private Color paperColor = new Color(0.97f, 0.96f, 0.92f);
        [SerializeField] private Color stampColor = new Color(0.85f, 0.18f, 0.15f);

        [Header("Timings")]
        [SerializeField] private float printDuration = 0.42f;
        [SerializeField] private float dropDuration = 0.38f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence stampSequence;
        private const float FullPaperHeight = 1200f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            stampSequence?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (receiptPaper != null)
            {
                receiptPaper.anchoredPosition = new Vector2(0, FullPaperHeight);
                receiptPaper.gameObject.SetActive(false);
            }

            if (paidStamp != null)
            {
                paidStamp.localScale = Vector3.zero;
            }

            if (printerSlot != null)
            {
                printerSlot.gameObject.SetActive(false);
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

            if (printerSlot != null)
            {
                printerSlot.gameObject.SetActive(true);
            }

            if (receiptPaper != null)
            {
                receiptPaper.anchoredPosition = new Vector2(0, FullPaperHeight);
                receiptPaper.gameObject.SetActive(true);
            }

            if (paidStamp != null)
            {
                paidStamp.localScale = Vector3.zero;
            }

            inSequence = DOTween.Sequence();

            // 영수증 종이가 프린터 슬롯에서 지지직 빠르게 풀려나와 화면 100% 덮음
            if (receiptPaper != null)
            {
                inSequence.Append(receiptPaper.DOAnchorPosY(0f, printDuration).SetEase(Ease.OutCubic));
                // 영수증 끝단 미세 바운스
                inSequence.Append(receiptPaper.DOPunchPosition(new Vector3(0, 10f, 0), 0.12f, 4, 1));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            stampSequence?.Kill();
            stampSequence = DOTween.Sequence();

            if (paidStamp != null)
            {
                // [ PAID ] 도장 쾅! 찍히고 펄스
                paidStamp.localScale = Vector3.one * 2.2f;
                stampSequence.Append(paidStamp.DOScale(1.0f, 0.22f).SetEase(Ease.InQuad));
                stampSequence.Append(paidStamp.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.15f, 5, 1));
                stampSequence.Append(paidStamp.DOScale(1.06f, 0.35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
            }
        }

        public override void StopWaitingLoop()
        {
            stampSequence?.Kill();

            if (paidStamp != null)
            {
                paidStamp.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 절취선 뜯김: 영수증이 아래로 스르륵 떨어짐 (Ease.InQuad)
            if (receiptPaper != null)
            {
                // 절취선 순간 살짝 위로 튕겼다가 아래로 낙하
                outSequence.Append(receiptPaper.DOAnchorPosY(15f, 0.08f).SetEase(Ease.OutQuad));
                outSequence.Append(receiptPaper.DOAnchorPosY(-FullPaperHeight - 100f, dropDuration).SetEase(Ease.InQuad));
            }

            if (printerSlot != null)
            {
                outSequence.Join(printerSlot.DOAnchorPosY(100f, dropDuration * 0.5f).SetEase(Ease.InQuad));
            }

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                if (receiptPaper != null)
                    receiptPaper.gameObject.SetActive(false);
                if (printerSlot != null)
                    printerSlot.gameObject.SetActive(false);

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
