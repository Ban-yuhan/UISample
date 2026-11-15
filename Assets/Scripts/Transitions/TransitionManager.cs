using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    public enum TransitionType
    {
        GearCircleSmooth,
        GearCircle = GearCircleSmooth, // 하위 호환 매핑
        GearCircleTick,
        BoxFlap,
        TruckWipe,
        ConveyorShutter,
        StampInvoice,
        Forklift,
        ForkliftDrive = Forklift, // 하위 호환 매핑
        ForkliftLift = Forklift,  // 하위 호환 매핑
        BarcodeScan,
        ReceiptRoll,
        BubbleWrap = ReceiptRoll, // 하위 호환 매핑
        ContainerDoor,
        FragileTape,
        DeliveryDrone = FragileTape, // 하위 호환 매핑
        WorldMapPin,
        DiagonalMeteor,
        BouncingBall,
        RandomReturn, // 신규 복귀 트랜지션 중 랜덤
        RandomAll
    }

    /// <summary>
    /// 크로스 씬 전환을 총괄하는 DontDestroyOnLoad 싱글톤 매니저
    /// SceneA ↔ SceneB 간 화면 덮기 -> 비동기 로드 -> 새 씬 위에서 화면 열기 파이프라인 보장
    /// </summary>
    public class TransitionManager : MonoBehaviour
    {
        private static TransitionManager instance;
        public static TransitionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TransitionManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[TransitionManager]");
                        instance = go.AddComponent<TransitionManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Transition Prefabs (Resources/Prefabs)")]
        [SerializeField] private GameObject gearCircleSmoothPrefab;
        [SerializeField] private GameObject gearCirclePrefab; // 레거시 호환
        [SerializeField] private GameObject gearCircleTickPrefab;
        [SerializeField] private GameObject boxFlapPrefab;
        [SerializeField] private GameObject truckWipePrefab;
        [SerializeField] private GameObject conveyorShutterPrefab;
        [SerializeField] private GameObject stampInvoicePrefab;
        [SerializeField] private GameObject forkliftDrivePrefab;
        [SerializeField] private GameObject barcodeScanPrefab;
        [SerializeField] private GameObject receiptRollPrefab;
        [SerializeField] private GameObject containerDoorPrefab;
        [SerializeField] private GameObject fragileTapePrefab;
        [SerializeField] private GameObject worldMapPinPrefab;
        [SerializeField] private GameObject diagonalMeteorPrefab;
        [SerializeField] private GameObject bouncingBallPrefab;

        [Header("Runtime Settings")]
        [SerializeField] private float minimumLoadingTime = 1.45f;
        public float MinimumLoadingTime { get => minimumLoadingTime; set => minimumLoadingTime = value; }

        private Canvas transitionCanvas;
        private RectTransform canvasContainer;
        private BaseTransition currentActiveTransition;
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureTransitionCanvas();
            LoadPrefabsIfNull();
        }

        private void EnsureTransitionCanvas()
        {
            if (transitionCanvas != null) return;

            // 자체 최상단 캔버스 생성 및 DontDestroyOnLoad 설정
            GameObject canvasGo = new GameObject("DontDestroy_TransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            transitionCanvas = canvasGo.GetComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 9999; // 최상단 오버레이 보장

            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasContainer = canvasGo.GetComponent<RectTransform>();
        }

        public void LoadPrefabsIfNull()
        {
            if (gearCircleSmoothPrefab == null) gearCircleSmoothPrefab = Resources.Load<GameObject>("Transitions/GearCircleTransition");
            if (gearCirclePrefab == null) gearCirclePrefab = gearCircleSmoothPrefab;
            if (gearCircleTickPrefab == null) gearCircleTickPrefab = Resources.Load<GameObject>("Transitions/GearCircleTickTransition");
            if (boxFlapPrefab == null) boxFlapPrefab = Resources.Load<GameObject>("Transitions/BoxFlapTransition");
            if (truckWipePrefab == null) truckWipePrefab = Resources.Load<GameObject>("Transitions/TruckWipeTransition");
            if (conveyorShutterPrefab == null) conveyorShutterPrefab = Resources.Load<GameObject>("Transitions/ConveyorShutterTransition");
            if (stampInvoicePrefab == null) stampInvoicePrefab = Resources.Load<GameObject>("Transitions/StampInvoiceTransition");
            if (forkliftDrivePrefab == null) forkliftDrivePrefab = Resources.Load<GameObject>("Transitions/ForkliftDriveTransition");
            if (barcodeScanPrefab == null) barcodeScanPrefab = Resources.Load<GameObject>("Transitions/BarcodeScanTransition");
            if (receiptRollPrefab == null) receiptRollPrefab = Resources.Load<GameObject>("Transitions/ReceiptRollTransition");
            if (containerDoorPrefab == null) containerDoorPrefab = Resources.Load<GameObject>("Transitions/ContainerDoorTransition");
            if (fragileTapePrefab == null) fragileTapePrefab = Resources.Load<GameObject>("Transitions/FragileTapeTransition");
            if (worldMapPinPrefab == null) worldMapPinPrefab = Resources.Load<GameObject>("Transitions/WorldMapPinTransition");
            if (diagonalMeteorPrefab == null) diagonalMeteorPrefab = Resources.Load<GameObject>("Transitions/DiagonalMeteorTransition");
            if (bouncingBallPrefab == null) bouncingBallPrefab = Resources.Load<GameObject>("Transitions/BouncingBallTransition");
        }

        public void SetPrefabs(
            GameObject gearSmooth,
            GameObject gearTick,
            GameObject box,
            GameObject truck,
            GameObject shutter,
            GameObject stamp,
            GameObject forklift,
            GameObject barcode,
            GameObject receipt,
            GameObject container,
            GameObject tape,
            GameObject mapPin,
            GameObject meteor,
            GameObject bouncingBall = null)
        {
            gearCircleSmoothPrefab = gearSmooth;
            gearCirclePrefab = gearSmooth;
            gearCircleTickPrefab = gearTick;
            boxFlapPrefab = box;
            truckWipePrefab = truck;
            conveyorShutterPrefab = shutter;
            stampInvoicePrefab = stamp;
            forkliftDrivePrefab = forklift;
            barcodeScanPrefab = barcode;
            receiptRollPrefab = receipt;
            containerDoorPrefab = container;
            fragileTapePrefab = tape;
            worldMapPinPrefab = mapPin;
            diagonalMeteorPrefab = meteor;
            bouncingBallPrefab = bouncingBall;
        }

        /// <summary>
        /// 씬 전환 실행: In(덮기) -> 비동기 로드 -> Out(새 씬 위에서 열기)
        /// </summary>
        public void TransitionToScene(string targetSceneName, TransitionType type = TransitionType.TruckWipe, Action onComplete = null)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[TransitionManager] 이미 씬 전환이 진행 중입니다.");
                return;
            }

            StartCoroutine(RoutineTransitionToScene(targetSceneName, type, onComplete));
        }

        private IEnumerator RoutineTransitionToScene(string targetSceneName, TransitionType type, Action onComplete)
        {
            IsTransitioning = true;
            EnsureTransitionCanvas();

            GameObject prefab = ResolvePrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"[TransitionManager] {type}에 해당하는 프리팹을 찾을 수 없어 기본 씬 로드를 진행합니다.");
                SceneManager.LoadScene(targetSceneName);
                IsTransitioning = false;
                onComplete?.Invoke();
                yield break;
            }

            // 트랜지션 오브젝트 인스턴스화
            GameObject instanceGo = Instantiate(prefab, canvasContainer);
            instanceGo.name = $"Active_{prefab.name}";

            RectTransform rt = instanceGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            var transition = instanceGo.GetComponent<BaseTransition>();
            if (transition == null)
            {
                Debug.LogError($"[TransitionManager] {prefab.name}에 BaseTransition 컴포넌트가 없습니다.");
                Destroy(instanceGo);
                SceneManager.LoadScene(targetSceneName);
                IsTransitioning = false;
                onComplete?.Invoke();
                yield break;
            }

            currentActiveTransition = transition;

            // Phase 1: In 애니메이션 시작
            bool inCompleted = false;
            Debug.Log($"<color=#00BFFF>[Cross-Scene In]</color> '{targetSceneName}' 로드 시작 (연출: {prefab.name})");
            transition.PlayIn(() =>
            {
                inCompleted = true;
            });

            // 화면이 100% 덮일 때까지 대기
            while (!inCompleted)
            {
                yield return null;
            }

            // [사용자 제안 핵심 최적화]
            // 지도가 화면을 100% 덮은 정적인 상태에서 씬 전환(LoadSceneAsync)을 먼저 수행!
            // 이렇게 하면 씬 스위칭 시 발생하는 메인 스레드 멈춤(Hitch)이 유저 눈에 전혀 띄지 않음.
            AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                yield return null;
            }

            // 2프레임 대기하여 새 씬의 모든 Awake/Start/UI 초기화 및 프레임 안정화 완료 보장
            yield return null;
            yield return null;

            Debug.Log($"<color=#FFA500>[Cross-Scene Midpoint]</color> '{targetSceneName}' 로드 완료! 새 씬 위에서 안정적으로 마커 낙하 & 대기 연출 시작");

            // 새 씬이 완전히 로드된 깨끗한 60fps 상태에서 마커 낙하 & 대기 루프 시작!
            transition.StartWaitingLoop();

            // 마커 쿵 낙하 & 바운스 & 레이더 연출을 충분히 감상할 수 있도록 대기 시간 보장
            yield return new WaitForSecondsRealtime(minimumLoadingTime);

            // 로딩 대기 루프 종료
            transition.StopWaitingLoop();

            // Phase 2: 새 씬 위에서 화면 열림(Out) 애니메이션 시작
            bool outCompleted = false;
            transition.PlayOut(() =>
            {
                outCompleted = true;
            });

            while (!outCompleted)
            {
                yield return null;
            }

            // 정리
            Debug.Log($"<color=#32CD32>[Cross-Scene Complete]</color> '{targetSceneName}' 전환 완료!");
            Destroy(instanceGo);
            currentActiveTransition = null;
            IsTransitioning = false;
            onComplete?.Invoke();
        }

        private GameObject ResolvePrefab(TransitionType type)
        {
            switch (type)
            {
                case TransitionType.GearCircleSmooth: return gearCircleSmoothPrefab != null ? gearCircleSmoothPrefab : gearCirclePrefab;
                case TransitionType.GearCircleTick: return gearCircleTickPrefab;
                case TransitionType.BoxFlap: return boxFlapPrefab;
                case TransitionType.TruckWipe: return truckWipePrefab;
                case TransitionType.ConveyorShutter: return conveyorShutterPrefab;
                case TransitionType.StampInvoice: return stampInvoicePrefab;
                case TransitionType.Forklift: return forkliftDrivePrefab;
                case TransitionType.BarcodeScan: return barcodeScanPrefab;
                case TransitionType.ReceiptRoll: return receiptRollPrefab;
                case TransitionType.ContainerDoor: return containerDoorPrefab;
                case TransitionType.FragileTape: return fragileTapePrefab;
                case TransitionType.WorldMapPin: return worldMapPinPrefab;
                case TransitionType.DiagonalMeteor: return diagonalMeteorPrefab;
                case TransitionType.BouncingBall: return bouncingBallPrefab;

                case TransitionType.RandomReturn:
                {
                    GameObject[] returnPool = new GameObject[]
                    {
                        gearCircleTickPrefab,
                        forkliftDrivePrefab,
                        barcodeScanPrefab,
                        receiptRollPrefab,
                        containerDoorPrefab,
                        fragileTapePrefab,
                        worldMapPinPrefab,
                        diagonalMeteorPrefab,
                        bouncingBallPrefab
                    };
                    List<GameObject> valids = new List<GameObject>();
                    foreach (var p in returnPool) if (p != null) valids.Add(p);
                    if (valids.Count > 0) return valids[UnityEngine.Random.Range(0, valids.Count)];
                    return forkliftDrivePrefab;
                }

                case TransitionType.RandomAll:
                default:
                {
                    GameObject[] allPool = new GameObject[]
                    {
                        gearCircleSmoothPrefab != null ? gearCircleSmoothPrefab : gearCirclePrefab,
                        gearCircleTickPrefab,
                        boxFlapPrefab,
                        truckWipePrefab,
                        conveyorShutterPrefab,
                        stampInvoicePrefab,
                        forkliftDrivePrefab,
                        barcodeScanPrefab,
                        receiptRollPrefab,
                        containerDoorPrefab,
                        fragileTapePrefab,
                        worldMapPinPrefab,
                        diagonalMeteorPrefab,
                        bouncingBallPrefab
                    };
                    List<GameObject> valids = new List<GameObject>();
                    foreach (var p in allPool) if (p != null) valids.Add(p);
                    if (valids.Count > 0) return valids[UnityEngine.Random.Range(0, valids.Count)];
                    return truckWipePrefab;
                }
            }
        }
    }
}
