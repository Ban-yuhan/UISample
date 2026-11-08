using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 시네마틱 배송 트럭 주행 & 와이프 트랜지션 (순수 궤도 회전 진입 버전)
    /// 
    /// 1. PlayIn: 
    ///    - Y축 솟구침 없이, 3단 아크 곡선들이 시작부터 원형 궤도를 타고 우측 바깥(-180°)에서
    ///      맹렬히 회전(Orbital Spin)하며 화면 안으로 감겨 들어옴.
    ///    - 머스타드 옐로우 커튼 전개와 함께 트럭이 좌측 바깥에서 가속 진입하여 회전하는 화이트 라인 정점에 안착.
    /// 2. WaitingLoop: 
    ///    - 감속이나 멈춤 없이, 진입하던 회전 관성 그대로 각자의 차등 속도(1.4s / 2.1s / 3.0s / 1.8s) 무한 회전 유지.
    ///    - 트럭 바디 노면 요철/엔진 진동(덜덜) + 바퀴 쾌속 회전.
    /// 3. PlayOut: 트럭 부스터 가속 우측 돌파 + 아크 라인 페이드아웃 + 슬라이드 우측 닫힘.
    /// </summary>
    public class TruckWipeTransition : BaseTransition
    {
        [Header("Curtain Panel")]
        [SerializeField] private RectTransform curtainPanel;

        [Header("Truck Components")]
        [SerializeField] private RectTransform truckRoot;
        [SerializeField] private RectTransform truckBodyVisual; // 서스펜션/엔진 진동 전담
        [SerializeField] private RectTransform frontWheel;
        [SerializeField] private RectTransform rearWheel;

        [Header("Multi-Speed Rounded Arcs (3단 차등 속도 곡선)")]
        [SerializeField] private RectTransform arcsContainer;
        [SerializeField] private RectTransform track1White;     // 1.4s 주기 (최고속 접지선)
        [SerializeField] private RectTransform track2Cream;     // 2.1s 주기 (중속 크림선)
        [SerializeField] private RectTransform track3Charcoal;  // 3.0s 주기 (하단 딥차콜선)
        [SerializeField] private RectTransform speedDotsTrack;  // 1.8s 주기 (속도 도트 트랙)

        [Header("Positions & Distances")]
        [SerializeField] private float offscreenLeft = -1550f;  // 화면 좌측 완전 바깥
        [SerializeField] private float waitingPosX = -40f;      // 아크 정점 안착 위치
        [SerializeField] private float offscreenRight = 1550f;  // 화면 우측 완전 바깥

        [Header("Timings & Speeds (3중 차등 속도 패럴랙스)")]
        [SerializeField] private float curtainSlideDuration = 0.45f;
        [SerializeField] private float truckEnterDuration = 0.38f;
        [SerializeField] private float exitDuration = 0.42f;
        [SerializeField] private float track1Cycle = 1.4f;      // Line 1: 최고속 접지선
        [SerializeField] private float track2Cycle = 2.1f;      // Line 2: 중속 크림선
        [SerializeField] private float track3Cycle = 3.0f;      // Line 3: 딥차콜 베이스선
        [SerializeField] private float dotsCycle = 1.8f;        // Speed Dots: 잔상 도트선

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence waitingLoopSequence;
        private Tween track1Tween;
        private Tween track2Tween;
        private Tween track3Tween;
        private Tween dotsTween;
        private Vector3 initialVisualPos;
        private Vector2 initialTruckPos;
        private CanvasGroup arcsCanvasGroup;

        protected override void Awake()
        {
            base.Awake();
            if (truckRoot != null)
            {
                initialTruckPos = truckRoot.anchoredPosition;
            }
            if (truckBodyVisual == null && truckRoot != null && truckRoot.childCount > 0)
            {
                truckBodyVisual = truckRoot.GetChild(0) as RectTransform;
            }
            if (truckBodyVisual != null)
            {
                initialVisualPos = truckBodyVisual.localPosition;
            }
            if (arcsContainer != null)
            {
                arcsCanvasGroup = arcsContainer.GetComponent<CanvasGroup>();
                if (arcsCanvasGroup == null)
                    arcsCanvasGroup = arcsContainer.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            inSequence?.Kill();
            outSequence?.Kill();
            waitingLoopSequence?.Kill();

            StopArcRotationTweens();

            if (frontWheel != null) DOTween.Kill(frontWheel);
            if (rearWheel != null) DOTween.Kill(rearWheel);
            if (truckBodyVisual != null) DOTween.Kill(truckBodyVisual);
            if (truckRoot != null) DOTween.Kill(truckRoot);
            if (arcsContainer != null) DOTween.Kill(arcsContainer);
            if (curtainPanel != null) DOTween.Kill(curtainPanel);
        }

        private void StopArcRotationTweens()
        {
            track1Tween?.Kill();
            track2Tween?.Kill();
            track3Tween?.Kill();
            dotsTween?.Kill();

            if (track1White != null) DOTween.Kill(track1White);
            if (track2Cream != null) DOTween.Kill(track2Cream);
            if (track3Charcoal != null) DOTween.Kill(track3Charcoal);
            if (speedDotsTrack != null) DOTween.Kill(speedDotsTrack);
        }

        private void StartArcRotationTweens()
        {
            StopArcRotationTweens();

            // 애초에 궤도 바깥(-180° / -210° / -250°)에서 시작하여, PlayIn부터 맹렬히 회전하며 화면 안으로 감겨 들어옴
            if (track1White != null)
            {
                track1White.localRotation = Quaternion.Euler(0, 0, -180f);
                track1Tween = track1White.DORotate(new Vector3(0, 0, 360f), track1Cycle, RotateMode.LocalAxisAdd)
                                         .SetLoops(-1, LoopType.Incremental)
                                         .SetEase(Ease.Linear);
            }

            if (track2Cream != null)
            {
                track2Cream.localRotation = Quaternion.Euler(0, 0, -210f);
                track2Tween = track2Cream.DORotate(new Vector3(0, 0, 360f), track2Cycle, RotateMode.LocalAxisAdd)
                                         .SetLoops(-1, LoopType.Incremental)
                                         .SetEase(Ease.Linear);
            }

            if (track3Charcoal != null)
            {
                track3Charcoal.localRotation = Quaternion.Euler(0, 0, -250f);
                track3Tween = track3Charcoal.DORotate(new Vector3(0, 0, 360f), track3Cycle, RotateMode.LocalAxisAdd)
                                             .SetLoops(-1, LoopType.Incremental)
                                             .SetEase(Ease.Linear);
            }

            if (speedDotsTrack != null)
            {
                speedDotsTrack.localRotation = Quaternion.Euler(0, 0, -190f);
                dotsTween = speedDotsTrack.DORotate(new Vector3(0, 0, 360f), dotsCycle, RotateMode.LocalAxisAdd)
                                          .SetLoops(-1, LoopType.Incremental)
                                          .SetEase(Ease.Linear);
            }
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (curtainPanel != null)
            {
                curtainPanel.anchorMin = Vector2.zero;
                curtainPanel.anchorMax = Vector2.one;
                curtainPanel.offsetMin = Vector2.zero;
                curtainPanel.offsetMax = Vector2.zero;
                curtainPanel.pivot = new Vector2(0f, 0.5f);
                curtainPanel.localScale = new Vector3(0, 1, 1);
                curtainPanel.gameObject.SetActive(false);
            }

            if (arcsContainer != null)
            {
                arcsContainer.anchoredPosition = Vector2.zero; // Y축 이동 없이 항상 정위치
                if (arcsCanvasGroup != null) arcsCanvasGroup.alpha = 1f;
                arcsContainer.gameObject.SetActive(false);
            }

            if (track1White != null) track1White.localRotation = Quaternion.Euler(0, 0, -180f);
            if (track2Cream != null) track2Cream.localRotation = Quaternion.Euler(0, 0, -210f);
            if (track3Charcoal != null) track3Charcoal.localRotation = Quaternion.Euler(0, 0, -250f);
            if (speedDotsTrack != null) speedDotsTrack.localRotation = Quaternion.Euler(0, 0, -190f);

            if (truckRoot != null)
            {
                float targetY = (initialTruckPos != Vector2.zero) ? initialTruckPos.y : truckRoot.anchoredPosition.y;
                truckRoot.anchoredPosition = new Vector2(offscreenLeft, targetY);
                truckRoot.gameObject.SetActive(false);
            }

            if (truckBodyVisual != null)
            {
                truckBodyVisual.localPosition = initialVisualPos;
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

            // 1. 커튼 초기화 (좌측 앵커에서 우측으로 확장 준비)
            if (curtainPanel != null)
            {
                curtainPanel.pivot = new Vector2(0f, 0.5f);
                curtainPanel.localScale = new Vector3(0, 1, 1);
                curtainPanel.gameObject.SetActive(true);
            }

            // 2. 3단 아크 컨테이너: Y축 이동 없이 원래 높이에 고정 & 즉시 궤도 회전 루프 시작!
            if (arcsContainer != null)
            {
                arcsContainer.anchoredPosition = Vector2.zero;
                if (arcsCanvasGroup != null) arcsCanvasGroup.alpha = 1f;
                arcsContainer.gameObject.SetActive(true);
            }

            // 애초에 뱅글뱅글 돌면서 화면 안으로 감겨 들어오도록 PlayIn 시작 즉시 회전 가동!
            StartArcRotationTweens();

            // 3. 트럭 초기화 (화면 좌측 완전 바깥)
            if (truckRoot != null)
            {
                float targetY = (initialTruckPos != Vector2.zero) ? initialTruckPos.y : truckRoot.anchoredPosition.y;
                truckRoot.anchoredPosition = new Vector2(offscreenLeft, targetY);
                truckRoot.gameObject.SetActive(true);
                truckRoot.SetAsLastSibling(); // 아크보다 항상 앞에 렌더링
            }

            if (truckBodyVisual != null)
            {
                truckBodyVisual.localPosition = initialVisualPos;
            }

            inSequence = DOTween.Sequence();

            // Step 1: 슬라이드 배경이 먼저 좌 -> 우로 화면 전체를 덮음 (0.45s)
            if (curtainPanel != null)
            {
                inSequence.Append(curtainPanel.DOScaleX(1f, curtainSlideDuration).SetEase(Ease.OutQuad));
            }

            // Step 2: 트럭이 화면 좌측 바깥에서 질주 진입하여 회전하는 화이트 라인 정점에 안착 (0.12s 시점 시작)
            if (truckRoot != null)
            {
                inSequence.Insert(0.12f, truckRoot.DOAnchorPosX(waitingPosX, truckEnterDuration).SetEase(Ease.OutCubic));
            }

            // 바퀴 회전
            if (frontWheel != null)
                inSequence.Insert(0.12f, frontWheel.DORotate(new Vector3(0, 0, -360f), truckEnterDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            if (rearWheel != null)
                inSequence.Insert(0.12f, rearWheel.DORotate(new Vector3(0, 0, -360f), truckEnterDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            // 아크 회전은 PlayIn부터 끊김 없이 계속 돌고 있으므로 트윈을 재시작하지 않고 유지합니다.
            if (track1Tween == null || !track1Tween.IsActive())
            {
                StartArcRotationTweens();
            }

            waitingLoopSequence?.Kill();
            waitingLoopSequence = DOTween.Sequence();

            // Step 3: 트럭 바디의 노면 요철/엔진 진동 (위아래 들썩들썩)
            if (truckBodyVisual != null)
            {
                waitingLoopSequence.Append(truckBodyVisual.DOLocalMoveY(initialVisualPos.y + 5f, 0.11f).SetEase(Ease.OutSine))
                                   .Append(truckBodyVisual.DOLocalMoveY(initialVisualPos.y - 2f, 0.09f).SetEase(Ease.InSine))
                                   .Append(truckBodyVisual.DOLocalMoveY(initialVisualPos.y, 0.08f).SetEase(Ease.OutSine))
                                   .SetLoops(-1);
            }

            // Step 4: 바퀴 지속 쾌속 회전
            if (frontWheel != null)
                frontWheel.DORotate(new Vector3(0, 0, -360f), 0.45f, RotateMode.LocalAxisAdd).SetLoops(-1).SetEase(Ease.Linear);
            if (rearWheel != null)
                rearWheel.DORotate(new Vector3(0, 0, -360f), 0.45f, RotateMode.LocalAxisAdd).SetLoops(-1).SetEase(Ease.Linear);
        }

        public override void StopWaitingLoop()
        {
            waitingLoopSequence?.Kill();

            if (frontWheel != null) DOTween.Kill(frontWheel);
            if (rearWheel != null) DOTween.Kill(rearWheel);
            if (truckBodyVisual != null)
            {
                DOTween.Kill(truckBodyVisual);
                truckBodyVisual.localPosition = initialVisualPos;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // Step 5: 커튼 피벗을 우측(1, 0.5)으로 전환하여 우측으로 닫히게 함
            if (curtainPanel != null)
            {
                curtainPanel.pivot = new Vector2(1f, 0.5f);
                outSequence.Append(curtainPanel.DOScaleX(0f, exitDuration).SetEase(Ease.InCubic));
            }

            // 트럭 부스터 가속 우측 돌파
            if (truckRoot != null)
            {
                outSequence.Join(truckRoot.DOAnchorPosX(offscreenRight, exitDuration).SetEase(Ease.InBack));
            }

            // 회전하던 아크 라인들은 자연스럽게 페이드아웃
            if (arcsCanvasGroup != null)
            {
                outSequence.Join(arcsCanvasGroup.DOFade(0f, exitDuration * 0.8f).SetEase(Ease.InQuad));
            }

            if (frontWheel != null)
                outSequence.Join(frontWheel.DORotate(new Vector3(0, 0, -720f), exitDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
            if (rearWheel != null)
                outSequence.Join(rearWheel.DORotate(new Vector3(0, 0, -720f), exitDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));

            outSequence.OnComplete(() =>
            {
                StopArcRotationTweens();
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
