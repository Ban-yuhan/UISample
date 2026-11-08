using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// [시계식 래칫 톱니바퀴 트랜지션]
    /// 컨셉: 회전 톱니와 동일한 패밀리룩(웜 앰버 12치 인더스트리얼 기어) 공유.
    /// PlayIn (0.42s): 중앙 2.8x 스케일업 & 120도 매끄러운 곡선 회전(Ease.OutQuad)으로 100% 암전 덮기.
    /// WaitingLoop: 3회 묵직한 기계식 비트 (30° 스냅 + 0.24초 안정적 정지 반복) -> 씬 전환 스터터 완벽 제거.
    /// PlayOut (0.36s): 중앙 단일 점(0.0x)으로 쇽- 축소(Ease.InCubic)되며 새 씬 오픈.
    /// </summary>
    public class GearCircleTickTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform circleCover;
        [SerializeField] private RectTransform gearRoot;
        [SerializeField] private Image circleImage;
        [SerializeField] private Image[] gearImages;
        [SerializeField] private Text gearEmojiText;

        [Header("Colors (Warm Logistics Family Look)")]
        [SerializeField] private Color primaryColor = new Color(0.96f, 0.58f, 0.23f); // 따뜻한 앰버 오렌지 (#F5943A)
        [SerializeField] private Color gearColor = new Color(0.99f, 0.95f, 0.88f);    // 크림 아이보리 (#FCF2E0)
        [SerializeField] private Color gearAccent = new Color(0.55f, 0.32f, 0.16f);   // 브라운 악센트 (#8C5229)

        [Header("Animation Timings")]
        [SerializeField] private float circleExpandDuration = 0.42f;
        [SerializeField] private float gearPopDuration = 0.42f;
        [SerializeField] private float tickStepDuration = 0.08f;
        [SerializeField] private float tickInterval1 = 0.6f;
        [SerializeField] private float tickInterval2 = 0.6f;
        [SerializeField] private float tickInterval3 = 0.6f;
        [SerializeField] private float exitDuration = 0.36f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence loopSequence;

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
            loopSequence?.Kill();
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

            // 1. 원형 아이리스 확장으로 화면 전체를 100% 덮음 (0.42s, Ease.OutQuad)
            if (circleCover != null)
                inSequence.Append(circleCover.DOScale(Vector3.one * 5.8f, circleExpandDuration).SetEase(Ease.OutQuad));

            // 2. 기어가 웅장하게 2.8x로 스케일업되며 약 120도 매끄러운 곡선 회전(Ease.OutQuad)으로 부드럽게 안착
            if (gearRoot != null)
            {
                inSequence.Join(gearRoot.DOScale(Vector3.one * 1.6f, gearPopDuration).SetEase(Ease.OutQuad));
                inSequence.Join(gearRoot.DORotate(new Vector3(0, 0, -120f), circleExpandDuration).SetEase(Ease.OutQuad));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            loopSequence?.Kill();
            if (gearRoot == null) return;

            loopSequence = DOTween.Sequence();

            // 3회 묵직한 기계식 비트 ("철컥 - [안정적 정지] - 철컥 - [안정적 정지] - 철컥")
            // 1st 틱: 30도 스냅 회전 (0.08s, Ease.OutBack) -> 0.24초 안정적 정지
            loopSequence.Append(gearRoot.DORotate(new Vector3(0, 0, -30f), tickStepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack, 1.35f));
            loopSequence.AppendInterval(tickInterval1);

            // 2nd 틱: 30도 스냅 회전 (0.08s, Ease.OutBack) -> 0.24초 안정적 정지 (백그라운드 씬 전환 수행 타이밍)
            loopSequence.Append(gearRoot.DORotate(new Vector3(0, 0, -30f), tickStepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack, 1.35f));
            loopSequence.AppendInterval(tickInterval2);

            // 3rd 틱: 30도 스냅 회전 (0.08s, Ease.OutBack) -> 0.20초 안정적 정지 피날레
            loopSequence.Append(gearRoot.DORotate(new Vector3(0, 0, -30f), tickStepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack, 1.35f));
            loopSequence.AppendInterval(tickInterval3);

            // 추가 대기 시간 발생 시 묵직한 30도 스냅 리듬을 계속 유지
            loopSequence.Append(gearRoot.DORotate(new Vector3(0, 0, -30f), tickStepDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack, 1.35f));
            loopSequence.AppendInterval(tickInterval1);

            loopSequence.SetLoops(-1);
        }

        public override void StopWaitingLoop()
        {
            loopSequence?.Kill();
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 회전 톱니와 동일하게 중앙 단일 점(0.0x)으로 쇽- 축소(Ease.InCubic)되며 새 씬 시원하게 오픈
            if (gearRoot != null)
                outSequence.Append(gearRoot.DOScale(Vector3.zero, exitDuration * 0.75f).SetEase(Ease.InBack));

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
