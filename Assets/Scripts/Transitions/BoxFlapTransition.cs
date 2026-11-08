using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 택배 골판지 박스 상하 날개 닫힘 & 좌->우 테이핑 트랜지션
    /// </summary>
    public class BoxFlapTransition : BaseTransition
    {
        [Header("Flaps")]
        [SerializeField] private RectTransform topFlap;
        [SerializeField] private RectTransform bottomFlap;
        [SerializeField] private RectTransform tapeStrip;
        [SerializeField] private Image tapeImage;
        [SerializeField] private Text boxLabelText;

        [Header("Colors (Cardboard & Kraft)")]
        [SerializeField] private Color cardboardColor = new Color(0.72f, 0.49f, 0.28f); // 크라프트 박스 브라운 (#B87D47)
        [SerializeField] private Color tapeColor = new Color(0.96f, 0.77f, 0.42f);      // 포장용 크라프트 테이프 (#F5C46B)
        [SerializeField] private Color labelTextColor = new Color(0.16f, 0.09f, 0.03f); // 진한 잉크 블랙브라운 (#2A1808)

        [Header("Timings")]
        [SerializeField] private float flapCloseDuration = 0.42f;
        [SerializeField] private float tapeStickDuration = 0.38f;
        [SerializeField] private float flapOpenDuration = 0.4f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Tween waitingTween;

        private const float HalfScreenHeight = 720f;

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

            if (topFlap != null)
            {
                topFlap.anchoredPosition = new Vector2(0, HalfScreenHeight);
                topFlap.gameObject.SetActive(false);
            }
            if (bottomFlap != null)
            {
                bottomFlap.anchoredPosition = new Vector2(0, -HalfScreenHeight);
                bottomFlap.gameObject.SetActive(false);
            }
            if (tapeStrip != null)
            {
                tapeStrip.pivot = new Vector2(0f, 0.5f);
                tapeStrip.localScale = new Vector3(0, 1, 1);
                tapeStrip.gameObject.SetActive(false);
            }
            if (boxLabelText != null)
            {
                boxLabelText.color = new Color(labelTextColor.r, labelTextColor.g, labelTextColor.b, 0f);
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

            if (topFlap != null)
            {
                topFlap.anchoredPosition = new Vector2(0, HalfScreenHeight);
                topFlap.gameObject.SetActive(true);
            }
            if (bottomFlap != null)
            {
                bottomFlap.anchoredPosition = new Vector2(0, -HalfScreenHeight);
                bottomFlap.gameObject.SetActive(true);
            }
            if (tapeStrip != null)
            {
                tapeStrip.pivot = new Vector2(0f, 0.5f);
                tapeStrip.localScale = new Vector3(0, 1, 1);
                tapeStrip.gameObject.SetActive(false);
            }
            if (boxLabelText != null)
            {
                boxLabelText.color = new Color(labelTextColor.r, labelTextColor.g, labelTextColor.b, 0f);
            }

            inSequence = DOTween.Sequence();

            // 1. 위아래 날개가 단단하게 "착!" 빈틈없이 닫힘 (OutQuad 적용 - 통통 튀는 OutBounce 완전 제거)
            if (bottomFlap != null)
                inSequence.Append(bottomFlap.DOAnchorPosY(0f, flapCloseDuration).SetEase(Ease.OutQuad));
            if (topFlap != null)
                inSequence.Insert(0.04f, topFlap.DOAnchorPosY(0f, flapCloseDuration).SetEase(Ease.OutQuad));

            // 2. 날개가 완전히 닫힌 직후, 테이프가 좌측(0)에서 우측으로 '쫘악' 스케일 전개
            if (tapeStrip != null)
            {
                inSequence.AppendCallback(() => tapeStrip.gameObject.SetActive(true));
                inSequence.Append(tapeStrip.DOScaleX(1f, tapeStickDuration).SetEase(Ease.OutQuad));
            }
            if (boxLabelText != null)
            {
                // 테이프 전개와 함께 36pt 볼드 고대비 라벨 페이드인
                inSequence.Join(boxLabelText.DOFade(1f, tapeStickDuration * 0.75f).SetEase(Ease.OutQuad));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            if (boxLabelText != null)
            {
                // 로딩 대기 중 라벨 텍스트가 미세하게 숨쉬듯 펄스
                waitingTween = boxLabelText.transform.DOScale(1.04f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }

        public override void StopWaitingLoop()
        {
            waitingTween?.Kill();
            if (boxLabelText != null)
            {
                boxLabelText.transform.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 3. 테이프가 뜯기듯 얇아지며 사라지고 상하 날개가 열림
            if (tapeStrip != null)
                outSequence.Append(tapeStrip.DOScaleY(0f, 0.14f).SetEase(Ease.InQuad));
            if (boxLabelText != null)
                outSequence.Join(boxLabelText.DOFade(0f, 0.12f));

            if (topFlap != null)
                outSequence.Append(topFlap.DOAnchorPosY(HalfScreenHeight, flapOpenDuration).SetEase(Ease.InCubic));
            if (bottomFlap != null)
                outSequence.Join(bottomFlap.DOAnchorPosY(-HalfScreenHeight, flapOpenDuration).SetEase(Ease.InCubic));

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
