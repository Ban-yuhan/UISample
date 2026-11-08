using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace LogisticsTycoon.Transitions
{
    /// <summary>
    /// 물류 타이쿤 트랜지션 크로스 씬 전환 컨트롤러
    /// SceneA ↔ SceneB 간 14종 트랜지션을 통한 양방향 이동 총괄
    /// </summary>
    public class TransitionDemoController : MonoBehaviour
    {
        [Header("UI Text")]
        [SerializeField] private Text headerText;
        [SerializeField] private Text statusText;

        [Header("Cross-Scene Navigation")]
        [SerializeField] private Button btnGoToButtonScene;
        public Button BtnGoToButtonScene { get => btnGoToButtonScene; set => btnGoToButtonScene = value; }

        [Header("Transition Navigation Buttons (01 ~ 14)")]
        [SerializeField] private Button btn01GearCircleSmooth;
        [SerializeField] private Button btn02GearCircleTick;
        [SerializeField] private Button btn03BoxFlap;
        [SerializeField] private Button btn04TruckWipe;
        [SerializeField] private Button btn05ConveyorShutter;
        [SerializeField] private Button btn06StampInvoice;
        [SerializeField] private Button btn07Forklift;
        [SerializeField] private Button btn08BarcodeScan;
        [SerializeField] private Button btn09ReceiptRoll;
        [SerializeField] private Button btn10ContainerDoor;
        [SerializeField] private Button btn11FragileTape;
        [SerializeField] private Button btn12WorldMapPin;
        [SerializeField] private Button btn13DiagonalMeteor;
        [SerializeField] private Button btn14RandomAll;

        private void Awake()
        {
            Application.runInBackground = true;
            DOTween.Init(false, true, LogBehaviour.Default);
        }

        private void Start()
        {
            BindButtons();
            UpdateHeaderAndStatus();
        }

        public void UpdateHeaderAndStatus()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            string targetScene = (currentScene == "SceneA") ? "SceneB" : "SceneA";
            string currentSceneTitle = (currentScene == "SceneA") ? "🏭 물류 센터 A구역 (Scene A)" : "📦 물류 허브 B구역 (Scene B)";

            if (headerText != null)
                headerText.text = currentSceneTitle;

            if (statusText != null)
                statusText.text = $"★ 현재 위치: {currentScene} ➔ 원하는 트랜지션 버튼을 누르면 {targetScene}로 전환됩니다.";
        }

        public void BindButtons()
        {
            BindButton(btn01GearCircleSmooth, TransitionType.GearCircleSmooth, "01. ⚙️ 톱니(회전)");
            BindButton(btn02GearCircleTick, TransitionType.GearCircleTick, "02. ⚙️ 톱니(철컥)");
            BindButton(btn03BoxFlap, TransitionType.BoxFlap, "03. 📦 택배박스");
            BindButton(btn04TruckWipe, TransitionType.TruckWipe, "04. 🚚 배송트럭");
            BindButton(btn05ConveyorShutter, TransitionType.ConveyorShutter, "05. 🪟 롤 셔터");
            BindButton(btn06StampInvoice, TransitionType.StampInvoice, "06. 🔖 송장봉투");
            BindButton(btn07Forklift, TransitionType.Forklift, "07. 🚜 지게차");
            BindButton(btn08BarcodeScan, TransitionType.BarcodeScan, "08. 🏷️ 바코드");
            BindButton(btn09ReceiptRoll, TransitionType.ReceiptRoll, "09. 🧾 영수증");
            BindButton(btn10ContainerDoor, TransitionType.ContainerDoor, "10. 🚪 컨테이너");
            BindButton(btn11FragileTape, TransitionType.FragileTape, "11. ⚠️ 주의테이프");
            BindButton(btn12WorldMapPin, TransitionType.WorldMapPin, "12. 🗺️ 월드맵");
            BindButton(btn13DiagonalMeteor, TransitionType.DiagonalMeteor, "13. 🌠 별똥별메테오");
            BindButton(btn14RandomAll, TransitionType.RandomAll, "14. 🎲 전체 랜덤");

            if (btnGoToButtonScene != null)
            {
                btnGoToButtonScene.onClick.RemoveAllListeners();
                btnGoToButtonScene.onClick.AddListener(OnGoToButtonSceneClicked);
            }
        }

        public void OnGoToButtonSceneClicked()
        {
            if (TransitionManager.Instance != null && TransitionManager.Instance.IsTransitioning) return;

            if (statusText != null)
            {
                statusText.text = "[이동 중] 🔘 버튼 쇼케이스(ButtonScene)로 이동합니다...";
            }

            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.TransitionToScene("ButtonScene", TransitionType.RandomAll);
            }
            else
            {
                SceneManager.LoadScene("ButtonScene");
            }
        }

        private void BindButton(Button btn, TransitionType type, string label)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnTransitionButtonClicked(type, label));
        }

        private void OnTransitionButtonClicked(TransitionType type, string label)
        {
            if (TransitionManager.Instance.IsTransitioning) return;

            string currentScene = SceneManager.GetActiveScene().name;
            string targetScene = (currentScene == "SceneA") ? "SceneB" : "SceneA";

            if (statusText != null)
            {
                statusText.text = $"[전환 중] {label} 연출을 통해 {targetScene}로 이동합니다...";
            }

            TransitionManager.Instance.TransitionToScene(targetScene, type);
        }
    }
}
