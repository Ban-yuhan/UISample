using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 감성적인 슬림 별똥별(Shooting Star) 택배 트랜지션
    /// 
    /// PlayIn (0.45s):
    /// - 대각선(-40도) 각도의 7단계 웜 브라운(골판지 카톤/크라프트) 스트라이프가
    ///   각자 다른 속도와 시차로 좌상단 밖에서 들어와 화면을 빈틈없이 100% 밀폐 (Ease.OutCubic).
    /// - 배경에 은은한 밤하늘 별빛 도트(Star Dots) 18개가 부드럽게 반짝임.
    /// 
    /// StartWaitingLoop (Midpoint):
    /// - 3개의 분산된 슬림 별똥별(Shooting Star) 택배 편대:
    ///   * Star 1 (상단 궤적): Y +280, X -180, 시차 0.00s, 꼬리 길이 480px, 택배 42pt
    ///   * Star 2 (중앙 궤적): Y -30,  X +80,  시차 0.12s, 꼬리 길이 560px, 택배 48pt (메인)
    ///   * Star 3 (하단 궤적): Y -260, X -120, 시차 0.22s, 꼬리 길이 420px, 택배 38pt
    /// 
    /// 3단계 감성 시네마틱 타임라인:
    /// - Step 1 (0.55s, 스으으윽 감속 진입):
    ///   각 별똥별이 좌상단 밖에서 대각선 궤적을 타고 지정 좌표로 부드럽게 미끄러져 들어옴 (Ease.OutQuad).
    /// - Step 2 (0.50s, 멈칫 체공 & 꼬리 흔들림):
    ///   지정 좌표에 멈춰 체공하며 뒤쪽 뾰족한 테이퍼드 꼬리가 ScaleX(0.85f <-> 1.25f)로 늘어났다 줄어들고,
    ///   상하로 살랑살랑 파도치듯 물결치며(Wiggle 5px) 활공 호흡 연출.
    /// - Step 3 (0.45s, 우하단 스르륵 하강 퇴장):
    ///   다시 대각선 우하단 화면 밖으로 스르륵 빠져나가며 페이드아웃 (Ease.InQuad).
    /// - 대기 지속 시 자연스럽게 시차 재진입 루프.
    /// 
    /// PlayOut (0.45s):
    /// - 덮이던 진행 방향 그대로(우하단 쪽으로) 7개의 브라운 선들이 빠져나가며 다음 씬 오픈 (Ease.InCubic).
    /// </summary>
    public class DiagonalMeteorTransition : BaseTransition
    {
        [Header("Stripe Hierarchy")]
        [SerializeField] private RectTransform stripeContainer;
        [SerializeField] private RectTransform[] stripes;
        [SerializeField] private Image[] stripeImages;

        [Header("Star Dots Background (은은한 밤하늘 별가루)")]
        [SerializeField] private RectTransform starDotsContainer;
        [SerializeField] private Image[] starDots;

        [Header("Shooting Star Fleet (3 Parcels)")]
        [SerializeField] private RectTransform shootingStarContainer;

        [Header("Star 1 (상단 궤적: Y +280, X -180, 42pt)")]
        [SerializeField] private RectTransform star1Root;
        [SerializeField] private RectTransform star1Tail;
        [SerializeField] private Image star1TailImg;
        [SerializeField] private Image star1Glow;
        [SerializeField] private Text star1ParcelText;
        [SerializeField] private CanvasGroup star1CanvasGroup;

        [Header("Star 2 (중앙 궤적: Y -30, X +80, 48pt 메인)")]
        [SerializeField] private RectTransform star2Root;
        [SerializeField] private RectTransform star2Tail;
        [SerializeField] private Image star2TailImg;
        [SerializeField] private Image star2Glow;
        [SerializeField] private Text star2ParcelText;
        [SerializeField] private CanvasGroup star2CanvasGroup;

        [Header("Star 3 (하단 궤적: Y -260, X -120, 38pt)")]
        [SerializeField] private RectTransform star3Root;
        [SerializeField] private RectTransform star3Tail;
        [SerializeField] private Image star3TailImg;
        [SerializeField] private Image star3Glow;
        [SerializeField] private Text star3ParcelText;
        [SerializeField] private CanvasGroup star3CanvasGroup;

        [Header("Notice Text")]
        [SerializeField] private Text subNoticeText;

        // ==================== 레거시 호환 필드 ====================
        [Header("Legacy Compatibility Fields")]
        [SerializeField] private RectTransform meteorContainer;
        [SerializeField] private RectTransform meteorRoot;
        [SerializeField] private RectTransform leaderParcelRt;
        [SerializeField] private Text leaderParcelText;
        [SerializeField] private RectTransform follower1Rt;
        [SerializeField] private Text follower1Text;
        [SerializeField] private RectTransform follower2Rt;
        [SerializeField] private Text follower2Text;
        [SerializeField] private Text parcelEmoji;
        [SerializeField] private RectTransform[] speedTrails;
        [SerializeField] private CanvasGroup trailGroup;
        [SerializeField] private Image speedGlow;

        [Header("Colors (7-Step Warm Brown Palette)")]
        [SerializeField] private Color[] stripeColors = new Color[]
        {
            new Color(0.31f, 0.19f, 0.11f), // Stripe 0: 딥 카톤 브라운 (#4E311B)
            new Color(0.49f, 0.31f, 0.18f), // Stripe 1: 크라프트 박스 브라운 (#7D4E2D)
            new Color(0.65f, 0.41f, 0.24f), // Stripe 2: 카라멜 골판지 브라운 (#A6683C)
            new Color(0.76f, 0.49f, 0.29f), // Stripe 3: 웜 시나몬 브라운 (#C27E4B)
            new Color(0.85f, 0.59f, 0.36f), // Stripe 4: 골판지 코퍼 베이지 (#D9965B)
            new Color(0.90f, 0.70f, 0.52f), // Stripe 5: 소프트 밀크티 베이지 (#E5B285)
            new Color(0.24f, 0.15f, 0.08f)  // Stripe 6: 리치 다크 초콜릿 브라운 (#3E2514)
        };

        [Header("Animation Settings")]
        [SerializeField] private float inDuration = 0.45f;
        [SerializeField] private float slideOffset = 3800f; // 스트라이프 화면 밖 이동 거리
        [SerializeField] private float starSlideOffset = 2600f; // 별똥별 이동 오프셋

        // 7개 스트라이프에 적용할 시차(딜레이)
        private readonly float[] inDelays = new float[] { 0.00f, 0.035f, 0.06f, 0.02f, 0.05f, 0.03f, 0.07f };
        private readonly float[] outDelays = new float[] { 0.00f, 0.04f, 0.02f, 0.06f, 0.03f, 0.07f, 0.05f };

        // 별똥별 목표 좌표 및 설정
        private readonly Vector2 star1TargetPos = new Vector2(-180f, 280f);
        private readonly Vector2 star2TargetPos = new Vector2(80f, -30f);
        private readonly Vector2 star3TargetPos = new Vector2(-120f, -260f);

        private const float Star1Delay = 0.00f;
        private const float Star2Delay = 0.12f;
        private const float Star3Delay = 0.22f;

        private const float Step1Duration = 0.55f; // 스으으윽 감속 진입
        private const float Step2Duration = 0.50f; // 멈칫 체공 & 꼬리 신축/흔들림
        private const float Step3Duration = 0.45f; // 우하단 스르륵 하강 퇴장

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence shootingStarSequence;
        private readonly List<Tween> dotTweens = new List<Tween>();

        protected override void Awake()
        {
            base.Awake();
        }

        public override void PlayTransition(Action onMidpoint = null, Action onComplete = null)
        {
            PlayIn(() =>
            {
                onMidpoint?.Invoke();
                StartWaitingLoop();
                DOVirtual.DelayedCall(1.65f, () =>
                {
                    StopWaitingLoop();
                    PlayOut(onComplete);
                });
            });
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            shootingStarSequence?.Kill();
            KillDotTweens();
        }

        private void KillDotTweens()
        {
            if (dotTweens != null)
            {
                for (int i = 0; i < dotTweens.Count; i++)
                {
                    dotTweens[i]?.Kill();
                }
                dotTweens.Clear();
            }
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (container != null)
            {
                container.anchorMin = Vector2.zero;
                container.anchorMax = Vector2.one;
                container.offsetMin = Vector2.zero;
                container.offsetMax = Vector2.zero;
                container.localScale = Vector3.one;
            }

            if (stripeContainer != null)
            {
                stripeContainer.anchoredPosition = Vector2.zero;
                stripeContainer.localEulerAngles = new Vector3(0, 0, -40f);
            }

            if (stripes != null)
            {
                for (int i = 0; i < stripes.Length; i++)
                {
                    if (stripes[i] != null)
                    {
                        stripes[i].anchoredPosition = new Vector2(-slideOffset, stripes[i].anchoredPosition.y);
                    }
                }
            }

            if (shootingStarContainer != null)
            {
                shootingStarContainer.localEulerAngles = new Vector3(0, 0, -40f);
            }

            // 별똥별 3개 초기 위치 화면 밖(좌상단) 리셋
            ResetStar(star1Root, star1Tail, star1CanvasGroup, star1TargetPos);
            ResetStar(star2Root, star2Tail, star2CanvasGroup, star2TargetPos);
            ResetStar(star3Root, star3Tail, star3CanvasGroup, star3TargetPos);

            if (starDotsContainer != null)
            {
                starDotsContainer.gameObject.SetActive(false);
            }

            if (subNoticeText != null)
            {
                subNoticeText.color = new Color(1f, 1f, 1f, 0f);
            }

            // 레거시 호환
            if (meteorRoot != null)
            {
                meteorRoot.gameObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }

            IsPlaying = false;
        }

        private void ResetStar(RectTransform root, RectTransform tail, CanvasGroup cg, Vector2 targetPos)
        {
            if (root != null)
            {
                root.anchoredPosition = new Vector2(targetPos.x - starSlideOffset, targetPos.y);
                root.localScale = Vector3.one;
                root.gameObject.SetActive(false);
            }

            if (tail != null)
            {
                tail.localScale = new Vector3(0.6f, 1f, 1f);
                tail.anchoredPosition = Vector2.zero;
            }

            if (cg != null)
            {
                cg.alpha = 1f;
            }
        }

        public override void PlayIn(Action onInComplete = null)
        {
            if (IsPlaying) return;
            IsPlaying = true;

            KillAllTweens();
            ResetVisualsBeforeIn();

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            inSequence = DOTween.Sequence();

            // 7단계 브라운 스트라이프가 좌상단 밖(-slideOffset)에서 중앙(0)으로 부드럽게 진입하여 화면 밀폐
            if (stripes != null)
            {
                for (int i = 0; i < stripes.Length; i++)
                {
                    if (stripes[i] == null) continue;

                    RectTransform stripe = stripes[i];
                    float delay = (i < inDelays.Length) ? inDelays[i] : (i * 0.015f);
                    float dur = Mathf.Max(0.35f, inDuration - delay * 0.5f);

                    inSequence.Insert(delay, stripe.DOAnchorPosX(0f, dur).SetEase(Ease.OutCubic));
                }
            }

            // 별빛 도트 서서히 표시
            if (starDotsContainer != null)
            {
                inSequence.InsertCallback(0.20f, () =>
                {
                    starDotsContainer.gameObject.SetActive(true);
                    AnimateStarDots();
                });
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        private void ResetVisualsBeforeIn()
        {
            if (stripes != null)
            {
                for (int i = 0; i < stripes.Length; i++)
                {
                    if (stripes[i] != null)
                    {
                        stripes[i].anchoredPosition = new Vector2(-slideOffset, stripes[i].anchoredPosition.y);
                    }
                }
            }

            ResetStar(star1Root, star1Tail, star1CanvasGroup, star1TargetPos);
            ResetStar(star2Root, star2Tail, star2CanvasGroup, star2TargetPos);
            ResetStar(star3Root, star3Tail, star3CanvasGroup, star3TargetPos);

            if (starDotsContainer != null)
            {
                starDotsContainer.gameObject.SetActive(false);
            }

            if (subNoticeText != null)
            {
                subNoticeText.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void AnimateStarDots()
        {
            KillDotTweens();
            if (starDots == null || starDots.Length == 0) return;

            // 배경 별빛 도트들의 은은한 반짝임(Twinkle) - 개별 무한 트윈으로 관리
            for (int i = 0; i < starDots.Length; i++)
            {
                Image dot = starDots[i];
                if (dot == null) continue;

                float startAlpha = UnityEngine.Random.Range(0.15f, 0.35f);
                float targetAlpha = UnityEngine.Random.Range(0.60f, 0.90f);
                float duration = UnityEngine.Random.Range(0.6f, 1.2f);
                float delay = UnityEngine.Random.Range(0.0f, 0.4f);

                dot.color = new Color(dot.color.r, dot.color.g, dot.color.b, startAlpha);
                Tween t = dot.DOFade(targetAlpha, duration)
                    .SetDelay(delay)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
                dotTweens.Add(t);
            }
        }

        public override void StartWaitingLoop()
        {
            shootingStarSequence?.Kill();

            if (subNoticeText != null)
            {
                subNoticeText.text = "특급 별똥별 택배 배송 중... 📦✨";
                subNoticeText.color = new Color(1f, 0.96f, 0.82f, 0f);
            }

            shootingStarSequence = DOTween.Sequence();

            // 3개의 분산된 별똥별 타임라인 구성 (독립 시차 적용)
            BuildStarTimeline(shootingStarSequence, star1Root, star1Tail, star1Glow, star1CanvasGroup, star1TargetPos, Star1Delay);
            BuildStarTimeline(shootingStarSequence, star2Root, star2Tail, star2Glow, star2CanvasGroup, star2TargetPos, Star2Delay);
            BuildStarTimeline(shootingStarSequence, star3Root, star3Tail, star3Glow, star3CanvasGroup, star3TargetPos, Star3Delay);

            if (subNoticeText != null)
            {
                shootingStarSequence.Insert(0.15f, subNoticeText.DOFade(0.95f, 0.30f));
                shootingStarSequence.Insert(1.30f, subNoticeText.DOFade(0f, 0.30f));
            }

            // 루프 간격 처리 (비동기 로딩 지속 시 부드러운 재진입 루프)
            shootingStarSequence.AppendInterval(0.35f);
            shootingStarSequence.AppendCallback(() =>
            {
                ResetStar(star1Root, star1Tail, star1CanvasGroup, star1TargetPos);
                ResetStar(star2Root, star2Tail, star2CanvasGroup, star2TargetPos);
                ResetStar(star3Root, star3Tail, star3CanvasGroup, star3TargetPos);
            });

            shootingStarSequence.SetLoops(-1, LoopType.Restart);
        }

        /// <summary>
        /// 개별 별똥별의 3단계 시네마틱 타임라인 빌드
        /// Step 1 (0.55s): 스으으윽 부드럽게 감속 진입 (Ease.OutQuad)
        /// Step 2 (0.50s): 중앙 체공 & 뾰족한 꼬리 신축(0.85f <-> 1.25f) 및 위아래 살랑살랑 흔들림(Wiggle 5px)
        /// Step 3 (0.45s): 우하단 화면 밖으로 스르륵 하강 가속 퇴장 (Ease.InQuad)
        /// </summary>
        private void BuildStarTimeline(
            Sequence mainSeq,
            RectTransform starRoot,
            RectTransform starTail,
            Image starGlow,
            CanvasGroup starCg,
            Vector2 targetPos,
            float startDelay)
        {
            if (starRoot == null) return;

            float tStep1 = startDelay;
            float tStep2 = startDelay + Step1Duration;
            float tStep3 = tStep2 + Step2Duration;

            // =========================================================================
            // [시작 초기화 콜백]
            // =========================================================================
            mainSeq.InsertCallback(tStep1, () =>
            {
                if (starRoot != null)
                {
                    starRoot.gameObject.SetActive(true);
                    starRoot.anchoredPosition = new Vector2(targetPos.x - starSlideOffset, targetPos.y);
                }
                if (starTail != null)
                {
                    starTail.localScale = new Vector3(0.65f, 1f, 1f);
                    starTail.anchoredPosition = Vector2.zero;
                }
                if (starCg != null)
                {
                    starCg.alpha = 1f;
                }
                if (starGlow != null)
                {
                    starGlow.color = new Color(starGlow.color.r, starGlow.color.g, starGlow.color.b, 0.4f);
                }
            });

            // =========================================================================
            // [Step 1] 0.55s : 스으으윽 감속 진입 (Ease.OutQuad)
            // =========================================================================
            mainSeq.Insert(tStep1, starRoot.DOAnchorPosX(targetPos.x, Step1Duration).SetEase(Ease.OutQuad));
            if (starTail != null)
            {
                mainSeq.Insert(tStep1, starTail.DOScaleX(1.0f, Step1Duration).SetEase(Ease.OutQuad));
            }
            if (starGlow != null)
            {
                mainSeq.Insert(tStep1, starGlow.DOFade(0.85f, Step1Duration).SetEase(Ease.OutQuad));
            }

            // =========================================================================
            // [Step 2] 0.50s : 멈칫 체공 & 꼬리 신축/흔들림(Wiggle)
            // =========================================================================
            // 1. 별똥별 루트 살랑살랑 수직 부유 (DOLocalMoveY 5px / DOAnchorPosY)
            mainSeq.Insert(tStep2, starRoot.DOAnchorPosY(targetPos.y + 5f, 0.25f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));

            // 2. 뾰족한 꼬리 신축 (ScaleX 0.85f <-> 1.25f)
            if (starTail != null)
            {
                mainSeq.Insert(tStep2, starTail.DOScaleX(1.25f, 0.25f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
                // 꼬리 미세 파도타기 흔들림
                mainSeq.Insert(tStep2, starTail.DOAnchorPosY(4f, 0.16f).SetEase(Ease.InOutSine).SetLoops(3, LoopType.Yoyo));
            }

            if (starGlow != null)
            {
                mainSeq.Insert(tStep2, starGlow.DOFade(0.95f, 0.25f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
            }

            // =========================================================================
            // [Step 3] 0.45s : 우하단 화면 밖으로 스르륵 가속 퇴장 (Ease.InQuad)
            // =========================================================================
            mainSeq.Insert(tStep3, starRoot.DOAnchorPosX(targetPos.x + starSlideOffset, Step3Duration).SetEase(Ease.InQuad));
            if (starTail != null)
            {
                // 퇴장 시 가속으로 꼬리가 길어짐
                mainSeq.Insert(tStep3, starTail.DOScaleX(1.30f, Step3Duration).SetEase(Ease.InQuad));
            }
            if (starCg != null)
            {
                // 퇴장 후반부 부드러운 페이드아웃
                mainSeq.Insert(tStep3 + 0.15f, starCg.DOFade(0f, Step3Duration - 0.15f).SetEase(Ease.InQuad));
            }
        }

        public override void StopWaitingLoop()
        {
            shootingStarSequence?.Kill();
            KillDotTweens();

            ResetStar(star1Root, star1Tail, star1CanvasGroup, star1TargetPos);
            ResetStar(star2Root, star2Tail, star2CanvasGroup, star2TargetPos);
            ResetStar(star3Root, star3Tail, star3CanvasGroup, star3TargetPos);

            if (subNoticeText != null)
            {
                subNoticeText.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 덮이던 방향 그대로(우하단 방향 +slideOffset) 7개 브라운 선들이 시차를 두고 빠져나감
            if (stripes != null)
            {
                for (int i = 0; i < stripes.Length; i++)
                {
                    if (stripes[i] == null) continue;

                    RectTransform stripe = stripes[i];
                    float delay = (i < outDelays.Length) ? outDelays[i] : (i * 0.015f);
                    float dur = Mathf.Max(0.35f, inDuration - delay * 0.5f);

                    outSequence.Insert(delay, stripe.DOAnchorPosX(slideOffset, dur).SetEase(Ease.InCubic));
                }
            }

            // 별빛 도트 및 배경 페이드아웃
            if (starDotsContainer != null)
            {
                CanvasGroup dotsCg = starDotsContainer.GetComponent<CanvasGroup>();
                if (dotsCg != null)
                {
                    outSequence.Insert(0.0f, dotsCg.DOFade(0f, 0.25f));
                }
            }

            outSequence.OnComplete(() =>
            {
                if (starDotsContainer != null)
                {
                    starDotsContainer.gameObject.SetActive(false);
                }

                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
