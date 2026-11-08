using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 확실한 지게차 유압 리프트 트랜지션
    /// 비주얼: 디테일한 지게차 실루엣(운전석 롤 케이지, 2단 수직 마스트, 은빛 L포크, 바퀴, 상단 경광등) + 화면 전체를 100% 덮는 카톤 박스 적재 팔레트 화물
    /// PlayIn (0.48s): 아래(-1150)에서 위로 유압으로 번쩍 들어올려 화면 전체 커버 (Ease.OutBack)
    /// WaitingLoop: 상단 주황 경광등 삐뽀삐뽀 회전 점멸 & 유압 미세 진동
    /// PlayOut (0.40s): 화물 하강 및 지게차 하단 퇴장 (Ease.InQuad)
    /// </summary>
    public class ForkliftLiftTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform liftContainer;
        [SerializeField] private RectTransform cargoPallet;
        [SerializeField] private RectTransform forkliftBody;
        [SerializeField] private Image warningBeacon;
        [SerializeField] private Text statusText;

        [Header("Colors (Industrial Logistics)")]
        [SerializeField] private Color beaconOnColor = new Color(1f, 0.4f, 0.05f);
        [SerializeField] private Color beaconOffColor = new Color(0.35f, 0.12f, 0.02f);

        [Header("Timings")]
        [SerializeField] private float liftDuration = 0.48f;
        [SerializeField] private float lowerDuration = 0.40f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence beaconSequence;
        private const float ScreenHeight = 1150f;

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            beaconSequence?.Kill();
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (liftContainer != null)
            {
                liftContainer.anchoredPosition = new Vector2(0, -ScreenHeight);
                liftContainer.gameObject.SetActive(false);
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

            if (liftContainer != null)
            {
                liftContainer.anchoredPosition = new Vector2(0, -ScreenHeight);
                liftContainer.gameObject.SetActive(true);
            }

            inSequence = DOTween.Sequence();

            // 지게차와 화물 전체가 아래에서 번쩍 솟구쳐 올라와 화면을 100% 가림 (Ease.OutBack)
            if (liftContainer != null)
            {
                inSequence.Append(liftContainer.DOAnchorPosY(0f, liftDuration).SetEase(Ease.OutBack));
                // 리프트 도달 시 가벼운 유압 충격
                inSequence.Append(liftContainer.DOPunchPosition(new Vector3(0, 15f, 0), 0.15f, 6, 1));
            }

            if (statusText != null)
            {
                statusText.color = new Color(statusText.color.r, statusText.color.g, statusText.color.b, 0);
                inSequence.Join(statusText.DOFade(1f, 0.2f));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            beaconSequence?.Kill();
            beaconSequence = DOTween.Sequence();

            if (warningBeacon != null)
            {
                beaconSequence.Append(warningBeacon.DOColor(beaconOnColor, 0.12f))
                              .Append(warningBeacon.transform.DOScale(1.25f, 0.12f))
                              .Append(warningBeacon.DOColor(beaconOffColor, 0.12f))
                              .Append(warningBeacon.transform.DOScale(1.0f, 0.12f));
            }

            if (forkliftBody != null)
            {
                // 유압 엔진의 미세 떨림
                beaconSequence.Join(forkliftBody.DOAnchorPosY(forkliftBody.anchoredPosition.y + 2f, 0.24f).SetEase(Ease.InOutSine))
                              .Append(forkliftBody.DOAnchorPosY(forkliftBody.anchoredPosition.y, 0.24f).SetEase(Ease.InOutSine));
            }

            beaconSequence.SetLoops(-1);
        }

        public override void StopWaitingLoop()
        {
            beaconSequence?.Kill();

            if (warningBeacon != null)
            {
                warningBeacon.color = beaconOffColor;
                warningBeacon.transform.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            if (statusText != null)
            {
                outSequence.Append(statusText.DOFade(0f, 0.15f));
            }

            // 화물을 내리고 지게차가 하단으로 퇴장 (Ease.InQuad)
            if (liftContainer != null)
            {
                outSequence.Append(liftContainer.DOAnchorPosY(-ScreenHeight, lowerDuration).SetEase(Ease.InQuad));
            }

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                if (liftContainer != null)
                    liftContainer.gameObject.SetActive(false);

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
