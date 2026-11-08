using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 배송 드론 착륙 & 택배 오픈 트랜지션
    /// In: 프로펠러를 돌리는 배송 드론이 거대한 상자를 매달고 상공에서 급강하
    /// Midpoint: 드론 집게가 풀리며 배송 완료 체크 표시
    /// Out: 드론이 상공으로 솟구쳐 이륙하고 상자가 열리며 씬 노출
    /// </summary>
    public class DeliveryDroneTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform droneRoot;
        [SerializeField] private RectTransform[] propellers;
        [SerializeField] private RectTransform boxCover;
        [SerializeField] private RectTransform boxLeft;
        [SerializeField] private RectTransform boxRight;
        [SerializeField] private Image checkMark;
        [SerializeField] private Text droneStatusText;

        [Header("Colors (Modern Drone & Parcel)")]
        [SerializeField] private Color droneBodyColor = new Color(0.2f, 0.22f, 0.25f); // 텍티컬 매트 그레이 (#333840)
        [SerializeField] private Color droneAccentColor = new Color(0.25f, 0.78f, 0.95f); // 사이안 하이라이트 (#40C7F2)
        [SerializeField] private Color parcelColor = new Color(0.82f, 0.58f, 0.35f);   // 소포 박스

        [Header("Timings")]
        [SerializeField] private float diveDuration = 0.48f;
        [SerializeField] private float takeoffDuration = 0.45f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence propSequence;
        private const float ScreenHeight = 1100f;
        private const float HalfScreenWidth = 1000f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            propSequence?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (droneRoot != null)
            {
                droneRoot.anchoredPosition = new Vector2(0, ScreenHeight);
                droneRoot.gameObject.SetActive(false);
            }

            if (boxCover != null)
            {
                boxCover.localScale = Vector3.one;
                boxCover.gameObject.SetActive(false);
            }

            if (boxLeft != null)
            {
                boxLeft.anchoredPosition = Vector2.zero;
            }

            if (boxRight != null)
            {
                boxRight.anchoredPosition = Vector2.zero;
            }

            if (checkMark != null)
            {
                checkMark.color = new Color(1, 1, 1, 0);
                checkMark.transform.localScale = Vector3.zero;
            }

            if (droneStatusText != null)
            {
                droneStatusText.color = new Color(1, 1, 1, 0);
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

            if (boxCover != null)
            {
                boxCover.gameObject.SetActive(true);
            }

            if (boxLeft != null) boxLeft.anchoredPosition = Vector2.zero;
            if (boxRight != null) boxRight.anchoredPosition = Vector2.zero;

            if (droneRoot != null)
            {
                droneRoot.anchoredPosition = new Vector2(0, ScreenHeight);
                droneRoot.gameObject.SetActive(true);
            }

            StartPropellerSpin();

            inSequence = DOTween.Sequence();

            // 1. 드론과 택배 상자가 상공에서 급강하 착지
            if (droneRoot != null)
                inSequence.Append(droneRoot.DOAnchorPosY(160f, diveDuration).SetEase(Ease.OutCubic));

            if (boxCover != null)
            {
                boxCover.anchoredPosition = new Vector2(0, ScreenHeight);
                inSequence.Join(boxCover.DOAnchorPosY(0f, diveDuration).SetEase(Ease.OutCubic));
            }

            // 착지 충격 바운스
            if (droneRoot != null)
                inSequence.Append(droneRoot.DOPunchPosition(new Vector3(0, 15f, 0), 0.18f, 6, 1));

            // 2. 집게가 탁 풀리며 체크마크 팝업
            if (checkMark != null)
            {
                inSequence.Append(checkMark.DOFade(1f, 0.15f));
                inSequence.Join(checkMark.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
            }

            if (droneStatusText != null)
            {
                droneStatusText.text = "AIR DELIVERY COMPLETED";
                inSequence.Join(droneStatusText.DOFade(1f, 0.15f));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        private void StartPropellerSpin()
        {
            propSequence?.Kill();
            propSequence = DOTween.Sequence();
            if (propellers != null)
            {
                foreach (var p in propellers)
                {
                    if (p != null)
                    {
                        propSequence.Join(p.DORotate(new Vector3(0, 0, -360f), 0.25f, RotateMode.LocalAxisAdd)
                                           .SetLoops(-1, LoopType.Incremental)
                                           .SetEase(Ease.Linear));
                    }
                }
            }
        }

        public override void StartWaitingLoop()
        {
            if (droneRoot != null)
            {
                // 로딩 중 드론 호버링 미세 상하 흔들림
                droneRoot.DOLocalMoveY(175f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }

        public override void StopWaitingLoop()
        {
            if (droneRoot != null)
            {
                DOTween.Kill(droneRoot);
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            if (checkMark != null)
                outSequence.Append(checkMark.transform.DOScale(0f, 0.12f));

            if (droneStatusText != null)
                outSequence.Join(droneStatusText.DOFade(0f, 0.12f));

            // 3. 드론이 상공으로 급상승 이륙
            if (droneRoot != null)
                outSequence.Append(droneRoot.DOAnchorPosY(ScreenHeight, takeoffDuration).SetEase(Ease.InCubic));

            // 4. 소포 박스가 좌우로 쪼개져 씬 노출
            if (boxLeft != null)
                outSequence.Join(boxLeft.DOAnchorPosX(-HalfScreenWidth, takeoffDuration).SetEase(Ease.InCubic));

            if (boxRight != null)
                outSequence.Join(boxRight.DOAnchorPosX(HalfScreenWidth, takeoffDuration).SetEase(Ease.InCubic));

            outSequence.OnComplete(() =>
            {
                propSequence?.Kill();
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
