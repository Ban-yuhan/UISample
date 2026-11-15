using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace LogisticsTycoon.UI
{
    public enum MotionPreset
    {
        JellySquash,     // [1] 젤리 스쿼시
        DepthPress,      // [2] 3D 뎁스 스위치
        StampHit,        // [3] 스탬프 쾅!
        NeonPulse,       // [4] 네온 펄스 번쩍임
        GentlePop,       // [5] 심플 팝
        ConveyorSnap,    // [6] 컨베이어 벨트 밀림 & 찰칵 맞물림 복귀
        BoxJolt,         // [7] 택배 상자 위로 퐁 튀고 덜컹덜컹
        BarcodeLaser,    // [8] 붉은 레이저 스캔 빔 & 그린 승인
        TruckRevUp,      // [9] 배송 트럭 시동 & 배기 연기 퐁퐁
        TapeStripWipe    // [10] 박스 테이프 좌->우 쫘악 밀착
    }

    /// <summary>
    /// 모든 유니티 Button에 부착하여 고품질 DOTween 손맛과 FX를 제공하는 모듈형 피드백 컴포넌트
    /// 물류 타이쿤 테마 10종 인터랙션 완비
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Motion Settings")]
        [SerializeField] private MotionPreset motionType = MotionPreset.JellySquash;
        [SerializeField] private float animDuration = 0.3f;
        [SerializeField] private float punchStrength = 0.15f;

        [Header("Target Graphic / Body")]
        [Tooltip("트윈이 적용될 버튼 본체 RectTransform. 비어있으면 본인의 RectTransform이 사용됩니다.")]
        [SerializeField] private RectTransform targetBody;

        [Header("DepthPress Reference")]
        [Tooltip("DepthPress 모드에서 쑥 들어가는 물리적 깊이감을 줄 바닥 그림자 RectTransform")]
        [SerializeField] private RectTransform shadowTransform;
        [SerializeField] private Vector2 shadowPressedOffset = Vector2.zero;

        [Header("NeonPulse & Approval Glow Reference")]
        [Tooltip("NeonPulse 모드 및 Barcode 승인 모드에서 번쩍일 외곽선/글로우 Graphic")]
        [SerializeField] private Graphic glowGraphic;

        [Header("FX Features")]
        [SerializeField] private bool enableRipple = true;
        [SerializeField] private bool enableFloatingText = true;
        [SerializeField] private bool enableSparkle = false;

        [Header("FX Prefabs")]
        [SerializeField] private GameObject ripplePrefab;
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private GameObject sparklePrefab;
        [SerializeField] private GameObject exhaustSmokePrefab;
        [SerializeField] private GameObject laserBeamPrefab;
        [Tooltip("FX 인스턴스가 생성될 컨테이너 Transform (비어있으면 버튼 또는 최상위 캔버스)")]
        [SerializeField] private Transform fxContainer;

        [Header("Sprites for Dynamic FX")]
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite roundedBoxSprite;

        // Internal State
        private Button button;
        private RectTransform rootRect;
        private Vector3 originalScale = Vector3.one;
        private Vector2 originalBodyPos = Vector2.zero;
        private Vector2 originalShadowPos = Vector2.zero;
        private Color originalGlowColor = Color.clear;

        private Tween bodyTween;
        private Tween shadowTween;
        private Sequence glowSequence;
        private Sequence motionSequence;
        private bool isPointerDown = false;

        private static readonly string[] FloatingMessages = new string[]
        {
            "+100 EXP",
            "SUCCESS!",
            "+10 GOLD",
            "DELIVERED!",
            "+50 SCORE",
            "SORTED!",
            "DISPATCHED!"
        };

        public MotionPreset MotionType
        {
            get => motionType;
            set => motionType = value;
        }

        public bool EnableRipple
        {
            get => enableRipple;
            set => enableRipple = value;
        }

        public bool EnableFloatingText
        {
            get => enableFloatingText;
            set => enableFloatingText = value;
        }

        public bool EnableSparkle
        {
            get => enableSparkle;
            set => enableSparkle = value;
        }

        public RectTransform TargetBody => targetBody != null ? targetBody : rootRect;

        private void Awake()
        {
            button = GetComponent<Button>();
            rootRect = GetComponent<RectTransform>();

            if (targetBody == null)
            {
                targetBody = rootRect;
            }

            CacheInitialStates();
        }

        private void CacheInitialStates()
        {
            if (targetBody != null)
            {
                originalScale = targetBody.localScale;
                originalBodyPos = targetBody.anchoredPosition;
            }

            if (shadowTransform != null)
            {
                originalShadowPos = shadowTransform.anchoredPosition;
            }

            if (glowGraphic != null)
            {
                originalGlowColor = glowGraphic.color;
                glowGraphic.raycastTarget = false;
                // 평소에는 투명 상태 유지
                Color hiddenColor = originalGlowColor;
                hiddenColor.a = 0f;
                glowGraphic.color = hiddenColor;
            }
        }

        private void OnEnable()
        {
            ResetVisualState();
        }

        private void OnDisable()
        {
            KillAllTweens();
            ResetVisualState();
            isPointerDown = false;
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        public void KillAllTweens()
        {
            bodyTween?.Kill();
            bodyTween = null;

            shadowTween?.Kill();
            shadowTween = null;

            glowSequence?.Kill();
            glowSequence = null;

            motionSequence?.Kill();
            motionSequence = null;

            if (targetBody != null)
            {
                DOTween.Kill(targetBody);
                targetBody.localRotation = Quaternion.identity;
            }
            if (shadowTransform != null)
            {
                DOTween.Kill(shadowTransform);
            }
            if (glowGraphic != null)
            {
                DOTween.Kill(glowGraphic);
            }
        }

        public void ResetVisualState()
        {
            if (targetBody != null)
            {
                targetBody.localScale = originalScale;
                targetBody.anchoredPosition = originalBodyPos;
                targetBody.localRotation = Quaternion.identity;
            }

            if (shadowTransform != null)
            {
                shadowTransform.anchoredPosition = originalShadowPos;
            }

            if (glowGraphic != null)
            {
                Color c = originalGlowColor;
                c.a = 0f;
                glowGraphic.color = c;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;

            isPointerDown = true;
            KillAllTweens();

            RectTransform body = TargetBody;
            if (body == null) return;

            switch (motionType)
            {
                case MotionPreset.JellySquash:
                    // 젤리 스쿼시: 가로 1.15f, 세로 0.82f로 납작해짐 (0.1s, OutQuad)
                    bodyTween = body.DOScale(new Vector3(originalScale.x * 1.15f, originalScale.y * 0.82f, originalScale.z), 0.1f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.DepthPress:
                    // 3D 뎁스: 본체 Y축 -6px 하강, 바닥 그림자 축소
                    bodyTween = body.DOAnchorPosY(originalBodyPos.y - 6f, 0.08f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);

                    if (shadowTransform != null)
                    {
                        shadowTween = shadowTransform.DOAnchorPos(shadowPressedOffset, 0.08f)
                            .SetEase(Ease.OutQuad)
                            .SetUpdate(true);
                    }
                    break;

                case MotionPreset.StampHit:
                    // 스탬프: 위로 살짝 들림 (Y +8px, Scale 1.05f)
                    body.localRotation = Quaternion.identity;
                    Sequence prepSeq = DOTween.Sequence();
                    prepSeq.SetUpdate(true);
                    prepSeq.Join(body.DOAnchorPosY(originalBodyPos.y + 8f, 0.1f).SetEase(Ease.OutQuad));
                    prepSeq.Join(body.DOScale(originalScale * 1.05f, 0.1f).SetEase(Ease.OutQuad));
                    bodyTween = prepSeq;
                    break;

                case MotionPreset.NeonPulse:
                    // 네온: 살짝 작아지며 대기
                    bodyTween = body.DOScale(originalScale * 0.97f, 0.08f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.GentlePop:
                    // 심플 팝: Scale 0.94f (0.08s)
                    bodyTween = body.DOScale(originalScale * 0.94f, 0.08f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.ConveyorSnap:
                    // [신규 6] 컨베이어 벨트 밀림: 오른쪽으로 스르륵 +12px 밀림
                    bodyTween = body.DOAnchorPosX(originalBodyPos.x + 12f, 0.1f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.BoxJolt:
                    // [신규 7] 박스 눌림: 웅크림 (X 1.06, Y 0.92)
                    bodyTween = body.DOScale(new Vector3(originalScale.x * 1.06f, originalScale.y * 0.92f, originalScale.z), 0.08f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.BarcodeLaser:
                    // [신규 8] 바코드 스캔 대기: 약간 축소
                    bodyTween = body.DOScale(originalScale * 0.98f, 0.08f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.TruckRevUp:
                    // [신규 9] 출발 대기 준비: 살짝 뒤로 당겨짐 (X -6px, Scale 0.96f, 0.08s)
                    Sequence truckPrep = DOTween.Sequence();
                    truckPrep.SetUpdate(true);
                    truckPrep.Join(body.DOAnchorPosX(originalBodyPos.x - 6f, 0.08f).SetEase(Ease.OutQuad));
                    truckPrep.Join(body.DOScale(new Vector3(originalScale.x * 0.96f, originalScale.y * 0.96f, originalScale.z), 0.08f).SetEase(Ease.OutQuad));
                    bodyTween = truckPrep;
                    break;

                case MotionPreset.TapeStripWipe:
                    // [신규 10] 테이프 버튼 눌림
                    bodyTween = body.DOScale(originalScale * 0.97f, 0.08f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;
            }

            // 물결 리플 스폰 (터치 위치 즉각 피드백)
            if (enableRipple)
            {
                SpawnRipple(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            if (!isPointerDown) return;
            isPointerDown = false;

            KillAllTweens();

            RectTransform body = TargetBody;
            if (body == null) return;

            switch (motionType)
            {
                case MotionPreset.JellySquash:
                    // 젤리 스쿼시: 탄성 있게 띠용~ 원복 (0.45s, OutElastic)
                    bodyTween = body.DOScale(originalScale, 0.45f)
                        .SetEase(Ease.OutElastic)
                        .SetUpdate(true);
                    break;

                case MotionPreset.DepthPress:
                    // 3D 뎁스: Y축 0px 복귀 및 그림자 복원 (OutBack)
                    bodyTween = body.DOAnchorPosY(originalBodyPos.y, 0.18f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);

                    if (shadowTransform != null)
                    {
                        shadowTween = shadowTransform.DOAnchorPos(originalShadowPos, 0.18f)
                            .SetEase(Ease.OutBack)
                            .SetUpdate(true);
                    }
                    break;

                case MotionPreset.StampHit:
                    // 스탬프 쾅!: 아래로 내려찍으며 PunchScale & PunchRotation (회전 드리프트 방지)
                    body.localRotation = Quaternion.identity;
                    Sequence hitSeq = DOTween.Sequence();
                    hitSeq.SetUpdate(true);
                    hitSeq.Append(body.DOAnchorPosY(originalBodyPos.y, 0.05f).SetEase(Ease.InQuad));
                    hitSeq.Join(body.DOScale(originalScale, 0.05f).SetEase(Ease.InQuad));
                    hitSeq.AppendCallback(() =>
                    {
                        if (body != null)
                        {
                            body.localRotation = Quaternion.identity;
                            body.localScale = originalScale;
                            body.anchoredPosition = originalBodyPos;
                        }
                    });
                    hitSeq.Append(body.DOPunchScale(Vector3.one * punchStrength, 0.35f, 10, 1f));
                    hitSeq.Join(body.DOPunchRotation(new Vector3(0, 0, 5f), 0.35f, 10, 1f));
                    hitSeq.OnComplete(() =>
                    {
                        if (body != null)
                        {
                            body.localRotation = Quaternion.identity;
                            body.localScale = originalScale;
                            body.anchoredPosition = originalBodyPos;
                        }
                    });
                    hitSeq.OnKill(() =>
                    {
                        if (body != null)
                        {
                            body.localRotation = Quaternion.identity;
                            body.localScale = originalScale;
                            body.anchoredPosition = originalBodyPos;
                        }
                    });
                    bodyTween = hitSeq;
                    break;

                case MotionPreset.NeonPulse:
                    bodyTween = body.DOScale(originalScale, 0.15f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.GentlePop:
                    // 심플 팝: Scale 1.0f 원복
                    bodyTween = body.DOScale(originalScale, animDuration * 0.4f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.ConveyorSnap:
                    // [신규 6] 컨베이어 스냅: 기어가 찰칵 맞물리듯 착 달라붙음 (OutBack)
                    bodyTween = body.DOAnchorPosX(originalBodyPos.x, animDuration * 0.85f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                    break;

                case MotionPreset.BoxJolt:
                    // [신규 7] 클릭 핸들러에서 튀어오를 것이므로 스케일 즉시 복원
                    bodyTween = body.DOScale(originalScale, 0.1f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                    break;

                case MotionPreset.TruckRevUp:
                    // [신규 9] 클릭 핸들러에서 펀치 및 틸트가 실행되므로 스케일 및 위치 복원
                    Sequence truckUpSeq = DOTween.Sequence();
                    truckUpSeq.SetUpdate(true);
                    truckUpSeq.Join(body.DOScale(originalScale, 0.12f).SetEase(Ease.OutQuad));
                    truckUpSeq.Join(body.DOAnchorPosX(originalBodyPos.x, 0.12f).SetEase(Ease.OutQuad));
                    bodyTween = truckUpSeq;
                    break;

                case MotionPreset.BarcodeLaser:
                case MotionPreset.TapeStripWipe:
                    bodyTween = body.DOScale(originalScale, 0.15f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                    break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;

            RectTransform body = TargetBody;

            switch (motionType)
            {
                case MotionPreset.NeonPulse:
                    if (body != null) TriggerNeonFlash(body);
                    break;

                case MotionPreset.BoxJolt:
                    if (body != null) TriggerBoxJolt(body);
                    break;

                case MotionPreset.BarcodeLaser:
                    if (body != null) TriggerBarcodeLaser(body);
                    break;

                case MotionPreset.TruckRevUp:
                    if (body != null) TriggerTruckRevUp(body);
                    break;

                case MotionPreset.TapeStripWipe:
                    if (body != null) TriggerTapeStripWipe(body);
                    break;
            }

            // 플로팅 텍스트 스폰
            if (enableFloatingText)
            {
                SpawnFloatingText(eventData);
            }

            // 스파클 버스트 스폰
            if (enableSparkle)
            {
                SpawnSparkle(eventData);
            }
        }

        #region Motion Presets Logic

        private void TriggerNeonFlash(RectTransform body)
        {
            glowSequence?.Kill();
            glowSequence = DOTween.Sequence();
            glowSequence.SetUpdate(true);

            // 스케일 펄스 (1.08f -> 1.0f, 0.3s)
            glowSequence.Join(body.DOScale(originalScale * 1.08f, 0.08f).SetEase(Ease.OutQuad));
            glowSequence.Append(body.DOScale(originalScale, 0.22f).SetEase(Ease.OutQuad));

            // 글로우 번쩍임
            if (glowGraphic != null)
            {
                Color highlight = originalGlowColor;
                highlight.a = 1f;
                glowGraphic.color = highlight;

                glowSequence.Join(glowGraphic.DOFade(0f, 0.35f).SetEase(Ease.OutQuad));
            }
            else
            {
                // 글로우 그래픽이 없으면 본체 Graphic의 일시적 플래시
                Graphic g = body.GetComponent<Graphic>();
                if (g != null)
                {
                    Color baseCol = g.color;
                    glowSequence.Join(g.DOColor(Color.white, 0.06f).SetEase(Ease.OutQuad));
                    glowSequence.Append(g.DOColor(baseCol, 0.24f).SetEase(Ease.OutQuad));
                }
            }
        }

        /// <summary>
        /// [신규 7] 박스 덜컹 들썩임: 위로 퐁 튀고(Y +18px) 좌우로 덜컹뒤뚱거리며 착지
        /// </summary>
        private void TriggerBoxJolt(RectTransform body)
        {
            motionSequence?.Kill();
            motionSequence = DOTween.Sequence();
            motionSequence.SetUpdate(true);

            // 1. 위로 퐁 튀어오름
            motionSequence.Append(body.DOAnchorPosY(originalBodyPos.y + 18f, 0.12f).SetEase(Ease.OutQuad));
            // 2. 바닥으로 덜컹 착지 (바운스)
            motionSequence.Append(body.DOAnchorPosY(originalBodyPos.y, 0.22f).SetEase(Ease.OutBounce));

            // 3. 동시에 Z축 뒤뚱거림 (PunchRotation)
            motionSequence.Join(body.DOPunchRotation(new Vector3(0, 0, 8f), 0.35f, 8, 0.7f));

            motionSequence.OnComplete(() =>
            {
                if (body != null)
                {
                    body.anchoredPosition = originalBodyPos;
                    body.localRotation = Quaternion.identity;
                }
            });
        }

        /// <summary>
        /// [신규 8] 바코드 스캔 레이저: 붉은 레이저 바 스캔 + 에메랄드 그린 승인 플래시
        /// </summary>
        private void TriggerBarcodeLaser(RectTransform body)
        {
            // 레이저 스캔 빔 생성
            if (laserBeamPrefab != null)
            {
                GameObject beamObj = Instantiate(laserBeamPrefab, body);
                LaserScanBeam laser = beamObj.GetComponent<LaserScanBeam>();
                if (laser != null)
                {
                    laser.Play(body, roundedBoxSprite, () => TriggerGreenApproval(body));
                }
            }
            else
            {
                // 프로시저얼 레이저 빔 방어 코드
                SpawnProceduralLaserBeam(body, () => TriggerGreenApproval(body));
            }
        }

        private void SpawnProceduralLaserBeam(RectTransform body, System.Action onComplete)
        {
            GameObject beamObj = new GameObject("ProceduralLaserBeam", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LaserScanBeam));
            LaserScanBeam laser = beamObj.GetComponent<LaserScanBeam>();
            laser.Play(body, roundedBoxSprite, onComplete);
        }

        private void TriggerGreenApproval(RectTransform body)
        {
            if (glowGraphic != null)
            {
                glowSequence?.Kill();
                glowSequence = DOTween.Sequence();
                glowSequence.SetUpdate(true);

                Color approvalGreen = new Color(0.2f, 1f, 0.5f, 0.95f); // 싱그러운 에메랄드 승인 그린
                glowGraphic.color = approvalGreen;

                // 승인 펄스 (1.05배 팝 & 페이드)
                glowSequence.Join(body.DOScale(originalScale * 1.05f, 0.08f).SetEase(Ease.OutQuad));
                glowSequence.Append(body.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad));
                glowSequence.Join(glowGraphic.DOFade(0f, 0.32f).SetEase(Ease.OutQuad));
            }
        }

        /// <summary>
        /// [신규 9] 상차 완료 배송 출발 (Truck Rev-Up):
        /// 제자리 서스펜션 들썩임, 전방 틸트 펀치 및 왼쪽 하단 배기 연기 FX 스폰
        /// </summary>
        private void TriggerTruckRevUp(RectTransform body)
        {
            motionSequence?.Kill();
            motionSequence = DOTween.Sequence();
            motionSequence.SetUpdate(true);

            // 1. 전방 급발진 틸트 (Z축 -4도) & 제자리 서스펜션 들썩임 (PunchPosition Y 14f)
            motionSequence.Append(body.DORotate(new Vector3(0, 0, -4f), 0.1f).SetEase(Ease.OutQuad));
            motionSequence.Join(body.DOPunchPosition(new Vector3(0, 14f, 0), 0.35f, 12, 0.8f));

            // 2. 자세 복귀 (회전 0도 복귀, OutBack)
            motionSequence.Append(body.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack));

            motionSequence.OnComplete(() =>
            {
                if (body != null)
                {
                    body.anchoredPosition = originalBodyPos;
                    body.localRotation = Quaternion.identity;
                }
            });

            // 3. 버튼 왼쪽 하단 배기구 연기 FX 스폰
            SpawnExhaustSmoke(body);
        }

        private void SpawnExhaustSmoke(RectTransform body)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = fxContainer != null ? fxContainer : (canvas != null ? canvas.transform : transform.root);
            if (fxContainer != null)
            {
                fxContainer.SetAsLastSibling();
            }

            // 버튼 본체의 왼쪽 하단(Left-Bottom, 배기구 위치) 월드 좌표 계산
            Vector3 worldLeftBottom = body.TransformPoint(new Vector3(body.rect.xMin + 14f, body.rect.yMin + 8f, 0f));
            Vector2 spawnPos = parent.InverseTransformPoint(worldLeftBottom);

            if (exhaustSmokePrefab != null)
            {
                GameObject smokeObj = Instantiate(exhaustSmokePrefab, parent);
                ExhaustSmokePuff smoke = smokeObj.GetComponent<ExhaustSmokePuff>();
                if (smoke != null)
                {
                    smoke.Play(spawnPos, circleSprite);
                }
            }
            else
            {
                // 프로시저얼 배기 연기 방어 코드
                GameObject smokeObj = new GameObject("ProceduralExhaustSmoke", typeof(RectTransform), typeof(ExhaustSmokePuff));
                smokeObj.transform.SetParent(parent, false);
                ExhaustSmokePuff smoke = smokeObj.GetComponent<ExhaustSmokePuff>();
                smoke.Play(spawnPos, circleSprite);
            }
        }

        /// <summary>
        /// [신규 10] 박스 테이핑 라벨: 좌 ➡️ 우로 '쫘악' 가로지르며 붙었다가 서서히 스며듦
        /// </summary>
        private void TriggerTapeStripWipe(RectTransform body)
        {
            GameObject tapeObj = new GameObject("TapeStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tapeObj.transform.SetParent(body, false);
            tapeObj.transform.SetAsLastSibling();

            RectTransform tapeRt = tapeObj.GetComponent<RectTransform>();
            // 중앙을 가로지르는 밴드 형태
            tapeRt.anchorMin = new Vector2(0f, 0.32f);
            tapeRt.anchorMax = new Vector2(1f, 0.68f);
            tapeRt.pivot = new Vector2(0f, 0.5f); // 좌측 피벗 기준으로 우측으로 쫘악 늘어남!
            tapeRt.offsetMin = new Vector2(-4f, 0f);
            tapeRt.offsetMax = new Vector2(4f, 0f);
            tapeRt.localScale = new Vector3(0f, 1f, 1f);
            tapeRt.localRotation = Quaternion.Euler(0, 0, -1.5f); // 박스 테이프 특유의 살짝 삐뚤어진 리얼리티

            Image tapeImg = tapeObj.GetComponent<Image>();
            tapeImg.sprite = roundedBoxSprite;
            tapeImg.type = roundedBoxSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            // 투명 크라프트 박스테이프 옐로우/황갈색 반투명
            Color tapeColor = new Color(0.96f, 0.86f, 0.58f, 0.82f);
            tapeImg.color = tapeColor;
            tapeImg.raycastTarget = false;

            Sequence tapeSeq = DOTween.Sequence();
            tapeSeq.SetUpdate(true);

            // 좌 -> 우로 쫘악 가로지르며 부착 (0.22s, OutQuad)
            tapeSeq.Append(tapeRt.DOScaleX(1f, 0.22f).SetEase(Ease.OutQuad));
            // 살짝 바디 흔들림 쾌감
            tapeSeq.Join(body.DOPunchPosition(new Vector3(3f, 0, 0), 0.2f, 8, 0.5f));
            // 0.15s 대기 후 부드럽게 스며들며 페이드아웃
            tapeSeq.AppendInterval(0.12f);
            tapeSeq.Append(tapeImg.DOFade(0f, 0.28f).SetEase(Ease.OutQuad));
            tapeSeq.OnComplete(() =>
            {
                if (tapeObj != null)
                {
                    Destroy(tapeObj);
                }
            });
        }

        #endregion

        #region FX Spawners

        private void SpawnRipple(PointerEventData eventData)
        {
            if (ripplePrefab == null) return;

            RectTransform targetRect = TargetBody;
            if (targetRect == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? eventData.pressEventCamera : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                eventData.position,
                cam,
                out Vector2 localPos))
            {
                Transform parent = targetRect;
                GameObject rippleObj = Instantiate(ripplePrefab, parent);
                rippleObj.transform.SetAsLastSibling();

                RippleEffect ripple = rippleObj.GetComponent<RippleEffect>();
                if (ripple != null)
                {
                    ripple.Play(localPos);
                }
                else
                {
                    RectTransform rt = rippleObj.GetComponent<RectTransform>();
                    Image img = rippleObj.GetComponent<Image>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = localPos;
                        rt.localScale = Vector3.zero;
                        rt.DOScale(Vector3.one * 3f, 0.45f).SetEase(Ease.OutQuad).SetUpdate(true);
                    }
                    if (img != null)
                    {
                        img.DOFade(0f, 0.45f).SetEase(Ease.OutQuad).SetUpdate(true)
                            .OnComplete(() => Destroy(rippleObj));
                    }
                    else
                    {
                        Destroy(rippleObj, 0.5f);
                    }
                }
            }
        }

        private void SpawnFloatingText(PointerEventData eventData)
        {
            if (floatingTextPrefab == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = fxContainer != null ? fxContainer : (canvas != null ? canvas.transform : transform.root);
            if (fxContainer != null)
            {
                fxContainer.SetAsLastSibling();
            }

            GameObject textObj = Instantiate(floatingTextPrefab, parent);
            RectTransform textRect = textObj.GetComponent<RectTransform>();

            if (textRect != null)
            {
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? eventData.pressEventCamera : null;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent as RectTransform,
                    eventData.position + new Vector2(0, 30f),
                    cam,
                    out Vector2 localPos))
                {
                    textRect.anchoredPosition = localPos;
                }
            }

            string msg = FloatingMessages[Random.Range(0, FloatingMessages.Length)];
            FloatingText ft = textObj.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.Play(msg);
            }
            else
            {
                Text t = textObj.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.text = msg;
                    t.DOFade(0f, 0.65f).SetUpdate(true);
                }
                if (textRect != null)
                {
                    textRect.DOAnchorPosY(textRect.anchoredPosition.y + 60f, 0.65f).SetUpdate(true)
                        .OnComplete(() => Destroy(textObj));
                }
                else
                {
                    Destroy(textObj, 0.7f);
                }
            }
        }

        private void SpawnSparkle(PointerEventData eventData)
        {
            if (sparklePrefab == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = fxContainer != null ? fxContainer : (canvas != null ? canvas.transform : transform.root);
            if (fxContainer != null)
            {
                fxContainer.SetAsLastSibling();
            }

            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? eventData.pressEventCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent as RectTransform,
                eventData.position,
                cam,
                out Vector2 localPos))
            {
                GameObject sparkleObj = Instantiate(sparklePrefab, parent);
                SparkleBurst sparkle = sparkleObj.GetComponent<SparkleBurst>();
                if (sparkle != null)
                {
                    sparkle.Play(localPos);
                }
                else
                {
                    sparkleObj.transform.localPosition = localPos;
                    Destroy(sparkleObj, 0.5f);
                }
            }
        }

        #endregion

        #region Public API for Showcase and Dynamic Control

        public void SetMotionPreset(MotionPreset preset)
        {
            motionType = preset;
            ResetVisualState();
        }

        public void SetFXOption(bool ripple, bool text, bool sparkle)
        {
            enableRipple = ripple;
            enableFloatingText = text;
            enableSparkle = sparkle;
        }

        public void ToggleRipple(bool isOn)
        {
            enableRipple = isOn;
        }

        public void ToggleFloatingText(bool isOn)
        {
            enableFloatingText = isOn;
        }

        public void ToggleSparkle(bool isOn)
        {
            enableSparkle = isOn;
        }

        public void SetPrefabs(
            GameObject ripple,
            GameObject floatingText,
            GameObject sparkle,
            GameObject exhaustSmoke = null,
            GameObject laserBeam = null)
        {
            ripplePrefab = ripple;
            floatingTextPrefab = floatingText;
            sparklePrefab = sparkle;
            exhaustSmokePrefab = exhaustSmoke;
            laserBeamPrefab = laserBeam;
        }

        public void SetDynamicSprites(Sprite circle, Sprite roundedBox)
        {
            circleSprite = circle;
            roundedBoxSprite = roundedBox;
        }

        public void SetReferences(RectTransform body, RectTransform shadow, Graphic glow, Transform container = null)
        {
            targetBody = body;
            shadowTransform = shadow;
            glowGraphic = glow;
            fxContainer = container;
            CacheInitialStates();
        }

        #endregion
    }
}
