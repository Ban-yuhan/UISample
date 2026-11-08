using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 해상 화물 컨테이너 문 트랜지션
    /// In: 중후한 스틸 컨테이너 양문이 좌우에서 중앙으로 쾅! 닫힘
    /// Midpoint: 잠금 봉(Lock Rod / Latch)이 찰칵 결합 체결됨
    /// Out: 래치가 풀리며 양문이 밖으로 활짝 열리며 씬 노출
    /// </summary>
    public class ContainerDoorTransition : BaseTransition
    {
        [Header("Components")]
        [SerializeField] private RectTransform doorLeft;
        [SerializeField] private RectTransform doorRight;
        [SerializeField] private RectTransform lockRod;
        [SerializeField] private Image lockHandle;
        [SerializeField] private Text containerSerialText;

        [Header("Colors (Heavy Steel Cargo)")]
        [SerializeField] private Color containerColor = new Color(0.18f, 0.44f, 0.72f); // 오션 블루 컨테이너 (#2E70B8)
        [SerializeField] private Color steelRibColor = new Color(0.14f, 0.35f, 0.58f);  // 주름 리브 음영 (#245994)
        [SerializeField] private Color lockSteelColor = new Color(0.85f, 0.85f, 0.88f); // 스틸 래치 실버

        [Header("Timings")]
        [SerializeField] private float doorCloseDuration = 0.45f;
        [SerializeField] private float lockLatchDuration = 0.25f;
        [SerializeField] private float doorOpenDuration = 0.45f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Tween waitingTween;
        private const float HalfScreenWidth = 1000f;

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

            if (doorLeft != null)
            {
                doorLeft.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                doorLeft.gameObject.SetActive(false);
            }

            if (doorRight != null)
            {
                doorRight.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                doorRight.gameObject.SetActive(false);
            }

            if (lockRod != null)
            {
                lockRod.localScale = new Vector3(1, 0, 1);
                lockRod.gameObject.SetActive(false);
            }

            if (lockHandle != null)
            {
                lockHandle.transform.localRotation = Quaternion.Euler(0, 0, 75f);
            }

            if (containerSerialText != null)
            {
                containerSerialText.color = new Color(1, 1, 1, 0);
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

            if (doorLeft != null)
            {
                doorLeft.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                doorLeft.gameObject.SetActive(true);
            }

            if (doorRight != null)
            {
                doorRight.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                doorRight.gameObject.SetActive(true);
            }

            if (lockRod != null)
            {
                lockRod.localScale = new Vector3(1, 0, 1);
                lockRod.gameObject.SetActive(false);
            }

            inSequence = DOTween.Sequence();

            // 1. 거대한 컨테이너 양문이 쾅! 중앙으로 닫힘
            if (doorLeft != null)
                inSequence.Append(doorLeft.DOAnchorPosX(0f, doorCloseDuration).SetEase(Ease.OutCubic));

            if (doorRight != null)
                inSequence.Join(doorRight.DOAnchorPosX(0f, doorCloseDuration).SetEase(Ease.OutCubic));

            // 문이 닫힐 때 묵직한 충격 흔들림
            if (doorLeft != null)
                inSequence.Append(doorLeft.DOPunchPosition(new Vector3(-12f, 0, 0), 0.15f, 5, 1));
            if (doorRight != null)
                inSequence.Join(doorRight.DOPunchPosition(new Vector3(12f, 0, 0), 0.15f, 5, 1));

            // 2. 잠금 봉(Lock Rod)이 위아래로 신장하며 체결
            if (lockRod != null)
            {
                inSequence.AppendCallback(() => lockRod.gameObject.SetActive(true));
                inSequence.Append(lockRod.DOScaleY(1f, lockLatchDuration).SetEase(Ease.OutBack));
            }

            // 래치 핸들이 찰칵 회전하여 잠금
            if (lockHandle != null)
            {
                inSequence.Join(lockHandle.transform.DORotate(Vector3.zero, lockLatchDuration).SetEase(Ease.OutBounce));
            }

            if (containerSerialText != null)
            {
                inSequence.Join(containerSerialText.DOFade(1f, 0.2f));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            if (lockHandle != null)
            {
                waitingTween = lockHandle.transform.DOPunchRotation(new Vector3(0, 0, 8f), 0.5f, 3, 0.5f).SetLoops(-1);
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

            // 3. 래치 해제 및 핸들 회전
            if (lockHandle != null)
            {
                outSequence.Append(lockHandle.transform.DORotate(new Vector3(0, 0, 75f), 0.15f).SetEase(Ease.InQuad));
            }

            if (lockRod != null)
            {
                outSequence.Append(lockRod.DOScaleY(0f, 0.12f).SetEase(Ease.InQuad));
            }

            if (containerSerialText != null)
            {
                outSequence.Join(containerSerialText.DOFade(0f, 0.1f));
            }

            // 4. 컨테이너 문이 밖으로 활짝 열림
            if (doorLeft != null)
                outSequence.Append(doorLeft.DOAnchorPosX(-HalfScreenWidth, doorOpenDuration).SetEase(Ease.InCubic));

            if (doorRight != null)
                outSequence.Join(doorRight.DOAnchorPosX(HalfScreenWidth, doorOpenDuration).SetEase(Ease.InCubic));

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
