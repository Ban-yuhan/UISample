using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 원형 아이리스 확장 + 180도 절제된 묵직한 메카닉 톱니바퀴 회전 트랜지션
    /// </summary>
    public class GearCircleTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform circleCover;
        [SerializeField] private RectTransform gearRoot;
        [SerializeField] private Image circleImage;
        [SerializeField] private Image[] gearImages;
        [SerializeField] private Text gearEmojiText;

        [Header("Colors (Warm Logistics)")]
        [SerializeField] private Color primaryColor = new Color(0.96f, 0.58f, 0.23f); // 따뜻한 앰버 오렌지 (#F5943A)
        [SerializeField] private Color gearColor = new Color(0.99f, 0.95f, 0.88f);    // 부드러운 크림 아이보리 (#FCF2E0)
        [SerializeField] private Color gearAccent = new Color(0.55f, 0.32f, 0.16f);   // 브라운 악센트 (#8C5229)

        [Header("Animation Timings")]
        [SerializeField] private float circleExpandDuration = 0.55f;
        [SerializeField] private float gearPopDuration = 0.45f;
        [SerializeField] private float exitDuration = 0.45f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Tween waitingTween;

        protected override void Awake()
        {
            base.Awake();
            ApplyColors();
        }

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

        public void ApplyColors()
        {
            if (circleImage != null) circleImage.color = primaryColor;
            if (gearImages != null)
            {
                for (int i = 0; i < gearImages.Length; i++)
                {
                    if (gearImages[i] != null)
                        gearImages[i].color = (i == 0) ? gearColor : gearAccent;
                }
            }
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (circleCover != null)
            {
                circleCover.localScale = Vector3.zero;
                circleCover.gameObject.SetActive(false);
            }
            if (gearRoot != null)
            {
                gearRoot.localScale = Vector3.zero;
                gearRoot.localRotation = Quaternion.identity;
                gearRoot.gameObject.SetActive(false);
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

            if (circleCover != null)
            {
                circleCover.localScale = Vector3.zero;
                circleCover.gameObject.SetActive(true);
            }

            if (gearRoot != null)
            {
                gearRoot.localScale = Vector3.zero;
                gearRoot.localRotation = Quaternion.identity;
                gearRoot.gameObject.SetActive(true);
            }

            inSequence = DOTween.Sequence();

            // 1. 원형 아이리스 확장으로 화면 전체를 덮음
            if (circleCover != null)
                inSequence.Append(circleCover.DOScale(Vector3.one * 5.8f, circleExpandDuration).SetEase(Ease.OutCubic));

            // 2. 기어가 큼직하게 팝업되면서, 과도한 회전 없이 "딱 180도(반 바퀴)"만 묵직하게 회전 (OutCubic)
            if (gearRoot != null)
            {
                inSequence.Join(gearRoot.DOScale(Vector3.one * 1.5f, gearPopDuration).SetEase(Ease.OutBack));
                // -180도 정확히 반 바퀴 회전
                inSequence.Join(gearRoot.DORotate(new Vector3(0, 0, -180f), circleExpandDuration).SetEase(Ease.OutCubic));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            if (gearRoot != null)
            {
                // 로딩 대기 중: 래칫 시계처럼 묵직하게 추가 45도씩 간헐적으로 틱-틱 회전
                waitingTween = gearRoot.DORotate(new Vector3(0, 0, -360f), 2.5f, RotateMode.LocalAxisAdd)
                    .SetLoops(-1, LoopType.Incremental)
                    .SetEase(Ease.InOutSine);
            }
        }

        public override void StopWaitingLoop()
        {
            waitingTween?.Kill();
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 3. 기어가 찰칵 멈추며 쏙 들어가고 원형 커튼이 축소되며 화면 열림
            if (gearRoot != null)
                outSequence.Append(gearRoot.DOScale(Vector3.zero, exitDuration * 0.7f).SetEase(Ease.InBack));

            if (circleCover != null)
                outSequence.Join(circleCover.DOScale(Vector3.zero, exitDuration).SetEase(Ease.InCubic));

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
