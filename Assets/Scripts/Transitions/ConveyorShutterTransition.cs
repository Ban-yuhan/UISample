using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 공장/물류창고 강철 롤 셔터 트랜지션
    /// PlayIn: 상단(+1150)에서 아래로 수직 급강하하여 바닥에 "철컹!" 묵직하게 바운스(Ease.OutBounce)하며 닫힘
    /// WaitingLoop: 셔터가 화면을 100% 닫은 채 유지되며 경광등 점멸
    /// PlayOut: 상단으로 롤업(0 -> +1150, Ease.InCubic)되어 화면 오픈
    /// </summary>
    public class ConveyorShutterTransition : BaseTransition
    {
        [Header("Root & Containers")]
        [SerializeField] private RectTransform shutterRoot;
        [SerializeField] private RectTransform[] shutterSlats;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private RectTransform centerBadge;
        [SerializeField] private Text badgeText;
        [SerializeField] private Image warningBeacon;

        [Header("Colors (Industrial Dark Steel)")]
        [SerializeField] private Color slatColorDark = new Color(0.18f, 0.22f, 0.26f);  // 차콜 스틸
        [SerializeField] private Color slatColorLight = new Color(0.25f, 0.30f, 0.36f); // 메탈릭 하이라이트
        [SerializeField] private Color hazardYellow = new Color(0.96f, 0.76f, 0.15f);   // 안전 경고 옐로우
        [SerializeField] private Color beaconOnColor = new Color(1.0f, 0.35f, 0.05f);   // 경광등 ON
        [SerializeField] private Color beaconOffColor = new Color(0.3f, 0.12f, 0.05f);  // 경광등 OFF

        [Header("Timings")]
        [SerializeField] private float inDuration = 0.42f;
        [SerializeField] private float outDuration = 0.38f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence waitingSequence;
        private const float ScreenHeight = 1150f;

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

            if (shutterRoot != null)
            {
                shutterRoot.anchoredPosition = new Vector2(0, ScreenHeight);
                shutterRoot.gameObject.SetActive(false);
            }

            if (warningBeacon != null)
            {
                warningBeacon.color = beaconOffColor;
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

            if (shutterRoot != null)
            {
                shutterRoot.anchoredPosition = new Vector2(0, ScreenHeight);
                shutterRoot.gameObject.SetActive(true);
            }

            inSequence = DOTween.Sequence();

            // 1. 상단(+1150)에서 0으로 급강하하며 Ease.OutBounce로 바닥에 묵직하게 바운스
            if (shutterRoot != null)
            {
                inSequence.Append(shutterRoot.DOAnchorPosY(0f, inDuration).SetEase(Ease.OutBounce));
            }

            // 2. 바닥 착지 시 스텐실 배지 펀치 충격
            if (centerBadge != null)
            {
                inSequence.Append(centerBadge.DOPunchScale(new Vector3(0.12f, 0.12f, 0), 0.15f, 6, 1));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            waitingSequence?.Kill();
            waitingSequence = DOTween.Sequence();

            if (warningBeacon != null)
            {
                waitingSequence.Append(warningBeacon.DOColor(beaconOnColor, 0.18f))
                               .Append(warningBeacon.transform.DOScale(1.25f, 0.18f))
                               .Append(warningBeacon.DOColor(beaconOffColor, 0.18f))
                               .Append(warningBeacon.transform.DOScale(1.0f, 0.18f))
                               .SetLoops(-1);
            }

            if (centerBadge != null)
            {
                waitingSequence.Join(centerBadge.DOScale(1.03f, 0.36f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
            }
        }

        public override void StopWaitingLoop()
        {
            waitingSequence?.Kill();

            if (warningBeacon != null)
            {
                warningBeacon.color = beaconOffColor;
                warningBeacon.transform.localScale = Vector3.one;
            }

            if (centerBadge != null)
            {
                centerBadge.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 상단으로 고속 롤업되어 올라감 (0 -> +1150, Ease.InCubic)
            if (shutterRoot != null)
            {
                outSequence.Append(shutterRoot.DOAnchorPosY(ScreenHeight, outDuration).SetEase(Ease.InCubic));
            }

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                if (shutterRoot != null)
                    shutterRoot.gameObject.SetActive(false);

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
