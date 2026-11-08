using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// [지게차 운반 트랜지션]
    /// 컨셉: 중앙 택배 파렛트 관통 레이어링 (Pallet_Back -> ForkliftRoot -> Pallet_Front)
    /// 1. 파렛트 전면 프레임 안쪽(포크 홀 개구부)으로 은빛 강철 포크 날이 쑤욱- 관통 도킹
    /// 2. 유압으로 파렛트(Back + Front)와 포크를 서서히 화면 중간 위(Y: +60, liftHeight: 180)까지 부드럽게 인양
    /// 3. 인양 완료 피스톤 정지 반동 (Y: 6px) 및 아이들링 진동 대기
    /// 4. 들린 상태 그대로 우측(+1600)으로 가속 출고
    /// </summary>
    public class ForkliftDriveTransition : BaseTransition
    {
        [Header("Curtain & Background")]
        [SerializeField] private RectTransform curtainPanel;

        [Header("Forklift Elements")]
        [SerializeField] private RectTransform forkliftRoot;
        [SerializeField] private RectTransform forkliftVisual;
        [SerializeField] private RectTransform steelForks;
        [SerializeField] private RectTransform frontWheel;
        [SerializeField] private RectTransform rearWheel;
        [SerializeField] private Image warningBeacon;

        [Header("Pallet Layers (Through-Pocket Layering)")]
        [SerializeField] private RectTransform palletBack;   // Layer 1 (포크 뒤쪽 뒷면 목재/그림자)
        [SerializeField] private RectTransform palletFront;  // Layer 3 (포크 앞쪽 전면 프레임 & 적재 박스)
        [SerializeField] private RectTransform cargoPallet;  // 레거시 통합 호환용

        [Header("Positions & Distances")]
        [SerializeField] private float offscreenLeft = -1550f;
        [SerializeField] private float dockPosX = -260f;
        [SerializeField] private float offscreenRight = 1600f;
        [SerializeField] private float liftHeight = 180f;

        [Header("Timings")]
        [SerializeField] private float driveEnterDuration = 0.28f;
        [SerializeField] private float liftDuration = 0.50f;
        [SerializeField] private float exitDuration = 0.42f;

        [Header("Beacon Colors")]
        [SerializeField] private Color beaconOnColor = new Color(1f, 0.5f, 0.1f);
        [SerializeField] private Color beaconOffColor = new Color(0.35f, 0.12f, 0.02f);

        private Sequence inSequence;
        private Sequence outSequence;
        private Sequence loopSequence;
        private Vector2 initialVisualPos;
        private Vector2 initialForksPos;
        private Vector2 initialPalletPos;
        private Vector2 initialBackPos;
        private Vector2 initialFrontPos;

        protected override void Awake()
        {
            base.Awake();
            if (forkliftVisual != null)
                initialVisualPos = forkliftVisual.anchoredPosition;
            if (steelForks != null)
                initialForksPos = steelForks.anchoredPosition;
            if (cargoPallet != null)
                initialPalletPos = cargoPallet.anchoredPosition;
            if (palletBack != null)
                initialBackPos = palletBack.anchoredPosition;
            if (palletFront != null)
                initialFrontPos = palletFront.anchoredPosition;
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

            if (forkliftRoot != null) DOTween.Kill(forkliftRoot);
            if (forkliftVisual != null) DOTween.Kill(forkliftVisual);
            if (steelForks != null) DOTween.Kill(steelForks);
            if (frontWheel != null) DOTween.Kill(frontWheel);
            if (rearWheel != null) DOTween.Kill(rearWheel);
            if (cargoPallet != null) DOTween.Kill(cargoPallet);
            if (palletBack != null) DOTween.Kill(palletBack);
            if (palletFront != null) DOTween.Kill(palletFront);
            if (curtainPanel != null) DOTween.Kill(curtainPanel);
            if (warningBeacon != null) DOTween.Kill(warningBeacon);
        }

        public override void ResetState()
        {
            KillAllTweens();

            if (curtainPanel != null)
            {
                curtainPanel.pivot = new Vector2(0f, 0.5f);
                curtainPanel.localScale = new Vector3(0, 1, 1);
                curtainPanel.gameObject.SetActive(false);
            }

            if (palletBack != null)
            {
                palletBack.anchoredPosition = initialBackPos;
                palletBack.gameObject.SetActive(false);
            }

            if (palletFront != null)
            {
                palletFront.anchoredPosition = initialFrontPos;
                palletFront.gameObject.SetActive(false);
            }

            if (cargoPallet != null)
            {
                cargoPallet.anchoredPosition = initialPalletPos;
                cargoPallet.gameObject.SetActive(false);
            }

            if (forkliftRoot != null)
            {
                forkliftRoot.anchoredPosition = new Vector2(offscreenLeft, 0);
                forkliftRoot.gameObject.SetActive(false);
            }

            if (forkliftVisual != null)
            {
                forkliftVisual.anchoredPosition = initialVisualPos;
            }

            if (steelForks != null)
            {
                steelForks.anchoredPosition = initialForksPos;
            }

            if (warningBeacon != null)
            {
                warningBeacon.color = beaconOffColor;
                warningBeacon.transform.localScale = Vector3.one;
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

            // 1. 커튼 전개 및 중앙 파렛트 대기
            if (curtainPanel != null)
            {
                curtainPanel.pivot = new Vector2(0f, 0.5f);
                curtainPanel.localScale = new Vector3(0, 1, 1);
                curtainPanel.gameObject.SetActive(true);
            }

            if (palletBack != null)
            {
                palletBack.anchoredPosition = initialBackPos;
                palletBack.gameObject.SetActive(true);
            }

            if (palletFront != null)
            {
                palletFront.anchoredPosition = initialFrontPos;
                palletFront.gameObject.SetActive(true);
            }

            if (cargoPallet != null)
            {
                cargoPallet.anchoredPosition = initialPalletPos;
                cargoPallet.gameObject.SetActive(true);
            }

            if (forkliftRoot != null)
            {
                forkliftRoot.anchoredPosition = new Vector2(offscreenLeft, 0);
                forkliftRoot.gameObject.SetActive(true);
            }

            if (steelForks != null)
            {
                steelForks.anchoredPosition = initialForksPos;
            }

            inSequence = DOTween.Sequence();

            // 1단계: 배경 슬레이트 커튼 좌 ➔ 우 확장 (0.00s ~ 0.20s)
            if (curtainPanel != null)
            {
                inSequence.Append(curtainPanel.DOScaleX(1f, 0.20f).SetEase(Ease.OutQuad));
            }

            // 2단계: 지게차가 좌측 바깥에서 달려와 중앙 파렛트 앞(dockPosX)까지 진입하여 포크를 꽂음 (0.08s ~ 0.36s)
            // 포크는 Pallet_Front 프레임 안쪽(포크 홀)으로 쑤욱 관통되어 들어감
            if (forkliftRoot != null)
            {
                inSequence.Insert(0.08f, forkliftRoot.DOAnchorPosX(dockPosX, driveEnterDuration).SetEase(Ease.OutCubic));
            }

            if (frontWheel != null)
                inSequence.Insert(0.08f, frontWheel.DORotate(new Vector3(0, 0, -360f), driveEnterDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            if (rearWheel != null)
                inSequence.Insert(0.08f, rearWheel.DORotate(new Vector3(0, 0, -360f), driveEnterDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

            // 3단계: 포크 꽂힘 직후 묵직한 유압으로 파렛트(Back & Front)와 포크를 화면 중간 위(+60)까지 인양 (0.50s, Ease.InOutQuad)
            float dockFinishTime = 0.08f + driveEnterDuration;
            if (palletBack != null)
            {
                inSequence.Insert(dockFinishTime, palletBack.DOAnchorPosY(initialBackPos.y + liftHeight, liftDuration).SetEase(Ease.InOutQuad));
            }
            if (palletFront != null)
            {
                inSequence.Insert(dockFinishTime, palletFront.DOAnchorPosY(initialFrontPos.y + liftHeight, liftDuration).SetEase(Ease.InOutQuad));
            }
            if (cargoPallet != null)
            {
                inSequence.Insert(dockFinishTime, cargoPallet.DOAnchorPosY(initialPalletPos.y + liftHeight, liftDuration).SetEase(Ease.InOutQuad));
            }
            if (steelForks != null)
            {
                inSequence.Insert(dockFinishTime, steelForks.DOAnchorPosY(initialForksPos.y + liftHeight, liftDuration).SetEase(Ease.InOutQuad));
            }

            // 인양 완료 시 피스톤 정지 반동 (미세 충격 Y: 6px)
            float liftFinishTime = dockFinishTime + liftDuration;
            if (palletFront != null)
            {
                inSequence.Insert(liftFinishTime, palletFront.DOPunchPosition(new Vector3(0, 6f, 0), 0.20f, 6, 0.5f));
            }
            if (palletBack != null)
            {
                inSequence.Insert(liftFinishTime, palletBack.DOPunchPosition(new Vector3(0, 6f, 0), 0.20f, 6, 0.5f));
            }
            if (cargoPallet != null)
            {
                inSequence.Insert(liftFinishTime, cargoPallet.DOPunchPosition(new Vector3(0, 6f, 0), 0.20f, 6, 0.5f));
            }
            if (forkliftVisual != null)
            {
                inSequence.Insert(liftFinishTime, forkliftVisual.DOPunchPosition(new Vector3(0, 6f, 0), 0.20f, 6, 0.5f));
            }

            inSequence.OnComplete(() =>
            {
                onInComplete?.Invoke();
            });
        }

        public override void StartWaitingLoop()
        {
            loopSequence?.Kill();
            loopSequence = DOTween.Sequence();

            // 1. 상단 경광등 삐뽀삐뽀 회전 점멸
            if (warningBeacon != null)
            {
                loopSequence.Append(warningBeacon.DOColor(beaconOnColor, 0.12f))
                            .Join(warningBeacon.transform.DOScale(1.22f, 0.12f))
                            .Append(warningBeacon.DOColor(beaconOffColor, 0.12f))
                            .Join(warningBeacon.transform.DOScale(1.0f, 0.12f));
            }

            // 2. 파렛트를 들린 높이(+60) 유지한 채 디젤 엔진 아이들링 미세 진동
            if (forkliftVisual != null)
            {
                loopSequence.Join(forkliftVisual.DOLocalMoveY(initialVisualPos.y + 2.5f, 0.11f).SetEase(Ease.OutSine))
                            .Append(forkliftVisual.DOLocalMoveY(initialVisualPos.y, 0.11f).SetEase(Ease.InSine));
            }

            if (palletBack != null)
            {
                float liftedBackY = initialBackPos.y + liftHeight;
                loopSequence.Join(palletBack.DOLocalMoveY(liftedBackY + 2.5f, 0.11f).SetEase(Ease.OutSine))
                            .Append(palletBack.DOLocalMoveY(liftedBackY, 0.11f).SetEase(Ease.InSine));
            }

            if (palletFront != null)
            {
                float liftedFrontY = initialFrontPos.y + liftHeight;
                loopSequence.Join(palletFront.DOLocalMoveY(liftedFrontY + 2.5f, 0.11f).SetEase(Ease.OutSine))
                            .Append(palletFront.DOLocalMoveY(liftedFrontY, 0.11f).SetEase(Ease.InSine));
            }

            if (cargoPallet != null)
            {
                float liftedY = initialPalletPos.y + liftHeight;
                loopSequence.Join(cargoPallet.DOLocalMoveY(liftedY + 2.5f, 0.11f).SetEase(Ease.OutSine))
                            .Append(cargoPallet.DOLocalMoveY(liftedY, 0.11f).SetEase(Ease.InSine));
            }

            if (steelForks != null)
            {
                float liftedForksY = initialForksPos.y + liftHeight;
                loopSequence.Join(steelForks.DOLocalMoveY(liftedForksY + 2.5f, 0.11f).SetEase(Ease.OutSine))
                            .Append(steelForks.DOLocalMoveY(liftedForksY, 0.11f).SetEase(Ease.InSine));
            }

            loopSequence.SetLoops(-1);
        }

        public override void StopWaitingLoop()
        {
            loopSequence?.Kill();

            if (warningBeacon != null)
            {
                warningBeacon.color = beaconOffColor;
                warningBeacon.transform.localScale = Vector3.one;
            }
            if (forkliftVisual != null)
            {
                forkliftVisual.anchoredPosition = initialVisualPos;
            }
        }

        public override void PlayOut(Action onOutComplete = null)
        {
            StopWaitingLoop();
            outSequence?.Kill();

            outSequence = DOTween.Sequence();

            // 1. 커튼 피벗 우측으로 전환하여 열림
            if (curtainPanel != null)
            {
                curtainPanel.pivot = new Vector2(1f, 0.5f);
                outSequence.Append(curtainPanel.DOScaleX(0f, exitDuration).SetEase(Ease.InCubic));
            }

            // 2. 지게차가 파렛트(Back + Front)를 든 상태 그대로 우측으로 가속 출고!
            if (forkliftRoot != null)
            {
                outSequence.Join(forkliftRoot.DOAnchorPosX(offscreenRight, exitDuration).SetEase(Ease.InCubic));
            }

            if (palletBack != null)
            {
                float targetX = offscreenRight + (initialBackPos.x - dockPosX);
                outSequence.Join(palletBack.DOAnchorPosX(targetX, exitDuration).SetEase(Ease.InCubic));
            }

            if (palletFront != null)
            {
                float targetX = offscreenRight + (initialFrontPos.x - dockPosX);
                outSequence.Join(palletFront.DOAnchorPosX(targetX, exitDuration).SetEase(Ease.InCubic));
            }

            if (cargoPallet != null)
            {
                float palletTargetX = offscreenRight + (initialPalletPos.x - dockPosX);
                outSequence.Join(cargoPallet.DOAnchorPosX(palletTargetX, exitDuration).SetEase(Ease.InCubic));
            }

            if (frontWheel != null)
                outSequence.Join(frontWheel.DORotate(new Vector3(0, 0, -720f), exitDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
            if (rearWheel != null)
                outSequence.Join(rearWheel.DORotate(new Vector3(0, 0, -720f), exitDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));

            outSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
                if (forkliftRoot != null)
                    forkliftRoot.gameObject.SetActive(false);
                if (palletBack != null)
                    palletBack.gameObject.SetActive(false);
                if (palletFront != null)
                    palletFront.gameObject.SetActive(false);
                if (cargoPallet != null)
                    cargoPallet.gameObject.SetActive(false);
                if (curtainPanel != null)
                    curtainPanel.gameObject.SetActive(false);
                if (steelForks != null)
                    steelForks.anchoredPosition = initialForksPos;

                IsPlaying = false;
                onOutComplete?.Invoke();
            });
        }
    }
}
