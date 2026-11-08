using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 취급주의 패킹 테이프 실감 테이핑 & 역순 개봉 트랜지션
    /// 
    /// 1. PlayIn: 
    ///    - 크라프트 상자 패널이 닫히며 밀봉 준비.
    ///    - 사선 및 상하 테이프들이 RectMask2D를 통해 글자 왜곡 없이 실제 테이프 풀리듯(0 -> Full) 쭉- 붙음.
    ///    - 마지막에 중앙 메인 보안 테이프가 촥 발리고 [ EXAMINED & VERIFIED ] 검수 스탬프가 쾅! 강타.
    /// 2. WaitingLoop: 
    ///    - 중앙 검수 스탬프 미세 펄스 박동 (1.0 -> 1.06 -> 1.0)
    /// 3. PlayOut (완전한 역순 Reverse Unboxing):
    ///    - 중앙 스탬프 & 중앙 메인 테이프가 먼저 팍! 뜯어지며 해제.
    ///    - 교차 사선 테이프들이 붙었던 역순으로 샤샤샥- 감겨 해제.
    ///    - 크라프트 박스 양 패널이 활짝 열리며 새 씬 시원하게 공개.
    /// </summary>
    public class FragileTapeTransition : BaseTransition
    {
        [Header("Cardboard Panels")]
        [SerializeField] private RectTransform splitLeft;
        [SerializeField] private RectTransform splitRight;

        [Header("Tapes (Masked Strips)")]
        [SerializeField] private RectTransform[] tapeStripes;

        [Header("Central Stamp")]
        [SerializeField] private RectTransform examStamp;
        [SerializeField] private Text examStampText;

        [Header("Timings")]
        [SerializeField] private float sealDuration = 0.52f;
        [SerializeField] private float unboxDuration = 0.46f;

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence stampSequence;
        private const float HalfScreenWidth = 1050f;

        private float[] fullWidths;
        private float[] heights;

        protected override void Awake()
        {
            base.Awake();
            EnsureFullWidths();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            stampSequence?.Kill();

            if (splitLeft != null) DOTween.Kill(splitLeft);
            if (splitRight != null) DOTween.Kill(splitRight);
            if (examStamp != null) DOTween.Kill(examStamp);

            if (tapeStripes != null)
            {
                foreach (var tape in tapeStripes)
                {
                    if (tape != null) DOTween.Kill(tape);
                }
            }
        }

        private void EnsureFullWidths()
        {
            if (fullWidths != null && fullWidths.Length == (tapeStripes != null ? tapeStripes.Length : 0)) return;
            if (tapeStripes == null) return;

            fullWidths = new float[tapeStripes.Length];
            heights = new float[tapeStripes.Length];
            for (int i = 0; i < tapeStripes.Length; i++)
            {
                if (tapeStripes[i] != null)
                {
                    fullWidths[i] = tapeStripes[i].sizeDelta.x > 50f ? tapeStripes[i].sizeDelta.x : 2400f;
                    heights[i] = tapeStripes[i].sizeDelta.y > 10f ? tapeStripes[i].sizeDelta.y : 80f;
                }
            }
        }

        public override void ResetState()
        {
            KillAllTweens();
            EnsureFullWidths();

            if (splitLeft != null)
            {
                splitLeft.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                splitLeft.gameObject.SetActive(false);
            }

            if (splitRight != null)
            {
                splitRight.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                splitRight.gameObject.SetActive(false);
            }

            if (tapeStripes != null && fullWidths != null)
            {
                for (int i = 0; i < tapeStripes.Length; i++)
                {
                    if (tapeStripes[i] != null)
                    {
                        tapeStripes[i].sizeDelta = new Vector2(0f, heights[i]);
                        tapeStripes[i].localScale = Vector3.one;
                    }
                }
            }

            if (examStamp != null)
            {
                examStamp.localScale = Vector3.zero;
                examStamp.gameObject.SetActive(false);
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
            EnsureFullWidths();

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

            // 1. 크라프트 박스 좌우 패널 닫힘 준비
            if (splitLeft != null)
            {
                splitLeft.anchoredPosition = new Vector2(-HalfScreenWidth, 0);
                splitLeft.gameObject.SetActive(true);
            }

            if (splitRight != null)
            {
                splitRight.anchoredPosition = new Vector2(HalfScreenWidth, 0);
                splitRight.gameObject.SetActive(true);
            }

            // 테이프 너비 0으로 초기화
            if (tapeStripes != null && fullWidths != null)
            {
                for (int i = 0; i < tapeStripes.Length; i++)
                {
                    if (tapeStripes[i] != null)
                    {
                        tapeStripes[i].sizeDelta = new Vector2(0f, heights[i]);
                        tapeStripes[i].localScale = Vector3.one;
                    }
                }
            }

            if (examStamp != null)
            {
                examStamp.localScale = Vector3.zero;
                examStamp.gameObject.SetActive(false);
            }

            inSequence = DOTween.Sequence();

            // Step 1: 크라프트 상자 패널이 닫히며 바탕 완성 (0.00s ~ 0.20s)
            float boxDuration = sealDuration * 0.38f;
            if (splitLeft != null)
                inSequence.Append(splitLeft.DOAnchorPosX(0f, boxDuration).SetEase(Ease.OutQuad));
            if (splitRight != null)
                inSequence.Join(splitRight.DOAnchorPosX(0f, boxDuration).SetEase(Ease.OutQuad));

            // Step 2: 테이프 롤이 한쪽 끝에서 반대편 끝으로 풀리듯이 순차 발림 (글자 왜곡 없음)
            // [0] 상단 블루 (0.14s)
            // [1] 하단 그린 (0.16s)
            // [2] 사선 레드 FRAGILE (0.20s)
            // [3] 사선 옐로우 PRIORITY (0.26s)
            // [4] 중앙 메인 오렌지 SEALED CARGO (0.34s)
            if (tapeStripes != null && fullWidths != null)
            {
                if (tapeStripes.Length > 0 && tapeStripes[0] != null)
                    inSequence.Insert(0.14f, tapeStripes[0].DOSizeDelta(new Vector2(fullWidths[0], heights[0]), 0.17f).SetEase(Ease.OutQuad));

                if (tapeStripes.Length > 1 && tapeStripes[1] != null)
                    inSequence.Insert(0.16f, tapeStripes[1].DOSizeDelta(new Vector2(fullWidths[1], heights[1]), 0.17f).SetEase(Ease.OutQuad));

                if (tapeStripes.Length > 2 && tapeStripes[2] != null)
                    inSequence.Insert(0.20f, tapeStripes[2].DOSizeDelta(new Vector2(fullWidths[2], heights[2]), 0.19f).SetEase(Ease.OutQuad));

                if (tapeStripes.Length > 3 && tapeStripes[3] != null)
                    inSequence.Insert(0.26f, tapeStripes[3].DOSizeDelta(new Vector2(fullWidths[3], heights[3]), 0.19f).SetEase(Ease.OutQuad));

                if (tapeStripes.Length > 4 && tapeStripes[4] != null)
                    inSequence.Insert(0.34f, tapeStripes[4].DOSizeDelta(new Vector2(fullWidths[4], heights[4]), 0.16f).SetEase(Ease.OutQuad));
            }

            // Step 3: 피날레 - 테이프가 다 발린 후 비로소 출현하여 중앙 검수 확인 스탬프 쾅! 강타 (0.42s)
            if (examStamp != null)
            {
                inSequence.InsertCallback(0.40f, () =>
                {
                    if (examStamp != null)
                    {
                        examStamp.gameObject.SetActive(true);
                        examStamp.localScale = Vector3.one * 1.6f;
                    }
                });
                inSequence.Insert(0.40f, examStamp.DOScale(1f, 0.15f).SetEase(Ease.OutBounce));
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

            if (examStamp != null)
            {
                // 중앙 스탬프 미세 펄스 박동
                stampSequence.Append(examStamp.DOScale(1.06f, 0.20f).SetEase(Ease.OutSine))
                             .Append(examStamp.DOScale(1.0f, 0.16f).SetEase(Ease.InSine))
                             .AppendInterval(0.25f)
                             .SetLoops(-1);
            }
        }

        public override void StopWaitingLoop()
        {
            stampSequence?.Kill();

            if (examStamp != null)
            {
                DOTween.Kill(examStamp);
                examStamp.localScale = Vector3.one;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // Step 1: 마지막에 찍혔던 중앙 스탬프 팍! 튀어오르며 해제 (0.00s ~ 0.11s)
            if (examStamp != null)
            {
                outSequence.Append(examStamp.DOScale(0f, 0.11f).SetEase(Ease.InBack)
                                            .OnComplete(() => examStamp.gameObject.SetActive(false)));
            }

            // Step 2: 붙었던 순서의 완전한 역순(Reverse)으로 테이프들이 샤샤샥- 뜯기며 수축 해제
            // [4] 중앙 오렌지 ➔ [3] 사선 옐로우 ➔ [2] 사선 레드 ➔ [1] 하단 그린 ➔ [0] 상단 블루
            if (tapeStripes != null && fullWidths != null)
            {
                if (tapeStripes.Length > 4 && tapeStripes[4] != null)
                    outSequence.Insert(0.05f, tapeStripes[4].DOSizeDelta(new Vector2(0f, heights[4]), 0.13f).SetEase(Ease.InQuad));

                if (tapeStripes.Length > 3 && tapeStripes[3] != null)
                    outSequence.Insert(0.10f, tapeStripes[3].DOSizeDelta(new Vector2(0f, heights[3]), 0.14f).SetEase(Ease.InQuad));

                if (tapeStripes.Length > 2 && tapeStripes[2] != null)
                    outSequence.Insert(0.15f, tapeStripes[2].DOSizeDelta(new Vector2(0f, heights[2]), 0.14f).SetEase(Ease.InQuad));

                if (tapeStripes.Length > 1 && tapeStripes[1] != null)
                    outSequence.Insert(0.20f, tapeStripes[1].DOSizeDelta(new Vector2(0f, heights[1]), 0.13f).SetEase(Ease.InQuad));

                if (tapeStripes.Length > 0 && tapeStripes[0] != null)
                    outSequence.Insert(0.22f, tapeStripes[0].DOSizeDelta(new Vector2(0f, heights[0]), 0.13f).SetEase(Ease.InQuad));
            }

            // Step 3: 크라프트 상자 패널이 좌우로 활짝 열리며 새 씬 시원하게 공개! (0.26s ~ 0.44s)
            float openDuration = unboxDuration * 0.40f;
            if (splitLeft != null)
                outSequence.Insert(0.26f, splitLeft.DOAnchorPosX(-HalfScreenWidth, openDuration).SetEase(Ease.InCubic));
            if (splitRight != null)
                outSequence.Insert(0.26f, splitRight.DOAnchorPosX(HalfScreenWidth, openDuration).SetEase(Ease.InCubic));

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                if (splitLeft != null)
                    splitLeft.gameObject.SetActive(false);
                if (splitRight != null)
                    splitRight.gameObject.SetActive(false);

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
