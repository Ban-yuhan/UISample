using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 에어캡 뽁뽁이 트랜지션
    /// In: 반투명-스카이블루 에어캡 뽁뽁이 시트가 화면을 덮음
    /// Midpoint: 뽁! 뽁! 뽁! 에어캡 버블들이 팡팡 터지는 연출
    /// Out: 에어캡 시트가 가운데를 따라 좌우로 찢어지며 씬 오픈
    /// </summary>
    public class BubbleWrapTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform sheetLeft;
        [SerializeField] private RectTransform sheetRight;
        [SerializeField] private RectTransform[] bubbleNodes; // 터지는 뽁뽁이 원형 노드들
        [SerializeField] private Text bubbleLabelText;

        [Header("Colors (Air Bubble Theme)")]
        [SerializeField] private Color sheetColor = new Color(0.72f, 0.88f, 0.98f, 0.95f); // 맑은 에어캡 스카이블루 (#B8E0FA)
        [SerializeField] private Color bubblePopColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("Timings")]
        [SerializeField] private float sheetDropDuration = 0.42f;
        [SerializeField] private float tearDuration = 0.45f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence popLoopSequence;
        private const float HalfScreenWidth = 1000f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            popLoopSequence?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (sheetLeft != null)
            {
                sheetLeft.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                sheetLeft.gameObject.SetActive(false);
            }

            if (sheetRight != null)
            {
                sheetRight.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                sheetRight.gameObject.SetActive(false);
            }

            ResetBubbles();

            if (bubbleLabelText != null)
            {
                bubbleLabelText.color = new Color(0.2f, 0.4f, 0.6f, 0f);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
            IsPlaying = false;
        }

        private void ResetBubbles()
        {
            if (bubbleNodes != null)
            {
                foreach (var b in bubbleNodes)
                {
                    if (b != null)
                    {
                        b.localScale = Vector3.one;
                        b.gameObject.SetActive(true);
                        var img = b.GetComponent<Image>();
                        if (img != null) img.color = bubblePopColor;
                    }
                }
            }
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

            if (sheetLeft != null)
            {
                sheetLeft.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                sheetLeft.gameObject.SetActive(true);
            }

            if (sheetRight != null)
            {
                sheetRight.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                sheetRight.gameObject.SetActive(true);
            }

            ResetBubbles();

            inSequence = DOTween.Sequence();

            // 1. 에어캡 시트가 좌우에서 빠르게 결합하며 화면을 덮음
            if (sheetLeft != null)
                inSequence.Append(sheetLeft.DOAnchorPosX(0f, sheetDropDuration).SetEase(Ease.OutQuad));

            if (sheetRight != null)
                inSequence.Join(sheetRight.DOAnchorPosX(0f, sheetDropDuration).SetEase(Ease.OutQuad));

            if (bubbleLabelText != null)
                inSequence.Join(bubbleLabelText.DOFade(1f, sheetDropDuration * 0.7f));

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            popLoopSequence?.Kill();
            popLoopSequence = DOTween.Sequence();

            // 2. 로딩 중 버블들이 뽁! 뽁! 팡! 터지는 연출
            if (bubbleNodes != null && bubbleNodes.Length > 0)
            {
                for (int i = 0; i < bubbleNodes.Length; i++)
                {
                    var node = bubbleNodes[i];
                    if (node == null) continue;

                    float delay = i * 0.18f;
                    popLoopSequence.Insert(delay, node.DOScale(1.4f, 0.12f).SetEase(Ease.OutQuad));
                    popLoopSequence.Insert(delay + 0.12f, node.DOScale(0.2f, 0.08f).SetEase(Ease.InQuad));
                    var img = node.GetComponent<Image>();
                    if (img != null)
                    {
                        popLoopSequence.Insert(delay + 0.12f, img.DOFade(0.2f, 0.08f));
                    }
                }
            }
        }

        public override void StopWaitingLoop()
        {
            popLoopSequence?.Kill();
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            if (bubbleLabelText != null)
                outSequence.Append(bubbleLabelText.DOFade(0f, 0.12f));

            // 3. 에어캡 시트가 가운데 절취선을 따라 좌우로 찢어지며 씬 노출
            if (sheetLeft != null)
                outSequence.Append(sheetLeft.DOAnchorPosX(-HalfScreenWidth, tearDuration).SetEase(Ease.InCubic));

            if (sheetRight != null)
                outSequence.Join(sheetRight.DOAnchorPosX(HalfScreenWidth, tearDuration).SetEase(Ease.InCubic));

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
