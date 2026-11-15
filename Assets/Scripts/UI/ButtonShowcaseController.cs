using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using LogisticsTycoon.Transitions;

namespace LogisticsTycoon.UI
{
    /// <summary>
    /// ButtonScene 쇼케이스 관리 컨트롤러
    /// 총 10종의 물류 특화 모션 프리셋 실시간 변경 및 FX 토글 기능 제공
    /// </summary>
    public class ButtonShowcaseController : MonoBehaviour
    {
        [Header("Target Buttons")]
        [SerializeField] private UIButtonFeedback mainButton;
        [SerializeField] private UIButtonFeedback[] subButtons;

        [Header("Status UI")]
        [SerializeField] private Text currentModeText;

        [Header("Order Rush Fever UI")]
        [SerializeField] private OrderRushOverlay orderRushOverlay;
        [SerializeField] private Button btnOrderRush;
        [SerializeField] private Text textOrderRush;
        [SerializeField] private Image imgOrderRush;

        [Header("Motion Select Buttons (Presets 1~10)")]
        [SerializeField] private Button btnJelly;
        [SerializeField] private Button btnDepth;
        [SerializeField] private Button btnStamp;
        [SerializeField] private Button btnNeon;
        [SerializeField] private Button btnGentle;
        [SerializeField] private Button btnConveyor;
        [SerializeField] private Button btnBoxJolt;
        [SerializeField] private Button btnBarcode;
        [SerializeField] private Button btnTruckRev;
        [SerializeField] private Button btnTape;

        [Header("FX Toggle Buttons")]
        [SerializeField] private Button btnToggleRipple;
        [SerializeField] private Text textToggleRipple;
        [SerializeField] private Button btnToggleFloating;
        [SerializeField] private Text textToggleFloating;
        [SerializeField] private Button btnToggleSparkle;
        [SerializeField] private Text textToggleSparkle;

        [Header("Reset Button")]
        [SerializeField] private Button btnReset;

        private MotionPreset currentPreset = MotionPreset.JellySquash;
        private bool isRippleEnabled = true;
        private bool isFloatingEnabled = true;
        private bool isSparkleEnabled = false;
        private bool isOrderRushActive = false;
        private Tween bannerFeedbackTween;

        private void Start()
        {
            BindEventListeners();
            ApplySettingsToButtons();
            UpdateUIStateText();
        }

        public void BindEventListeners()
        {
            if (btnOrderRush != null)
                btnOrderRush.onClick.AddListener(ToggleOrderRush);

            if (btnJelly != null)
                btnJelly.onClick.AddListener(() => SelectMotion(MotionPreset.JellySquash));
            if (btnDepth != null)
                btnDepth.onClick.AddListener(() => SelectMotion(MotionPreset.DepthPress));
            if (btnStamp != null)
                btnStamp.onClick.AddListener(() => SelectMotion(MotionPreset.StampHit));
            if (btnNeon != null)
                btnNeon.onClick.AddListener(() => SelectMotion(MotionPreset.NeonPulse));
            if (btnGentle != null)
                btnGentle.onClick.AddListener(() => SelectMotion(MotionPreset.GentlePop));

            if (btnConveyor != null)
                btnConveyor.onClick.AddListener(() => SelectMotion(MotionPreset.ConveyorSnap));
            if (btnBoxJolt != null)
                btnBoxJolt.onClick.AddListener(() => SelectMotion(MotionPreset.BoxJolt));
            if (btnBarcode != null)
                btnBarcode.onClick.AddListener(() => SelectMotion(MotionPreset.BarcodeLaser));
            if (btnTruckRev != null)
                btnTruckRev.onClick.AddListener(() =>
                {
                    SelectMotion(MotionPreset.TruckRevUp);
                    ShowBannerFeedback("🚚 부르릉~! 상차 완료, 배송 트럭 출발 대기 중!");
                });
            if (btnTape != null)
                btnTape.onClick.AddListener(() => SelectMotion(MotionPreset.TapeStripWipe));

            if (btnToggleRipple != null)
                btnToggleRipple.onClick.AddListener(ToggleRipple);
            if (btnToggleFloating != null)
                btnToggleFloating.onClick.AddListener(ToggleFloatingText);
            if (btnToggleSparkle != null)
                btnToggleSparkle.onClick.AddListener(ToggleSparkle);

            if (btnReset != null)
                btnReset.onClick.AddListener(ResetToDefaults);

            // 쇼케이스 타깃 버튼 클릭 시 배너 피드백 연결
            if (mainButton != null)
            {
                Button mb = mainButton.GetComponent<Button>();
                if (mb != null) mb.onClick.AddListener(OnTargetButtonClicked);
            }

            if (subButtons != null)
            {
                foreach (var sb in subButtons)
                {
                    if (sb != null)
                    {
                        Button b = sb.GetComponent<Button>();
                        if (b != null) b.onClick.AddListener(OnTargetButtonClicked);
                    }
                }
            }
        }

        private void OnTargetButtonClicked()
        {
            if (currentPreset == MotionPreset.TruckRevUp)
            {
                ShowBannerFeedback("🚚 부르릉~! 상차 완료, 배송 트럭 출발 대기 중!");
            }
        }

        public void ShowBannerFeedback(string message, float duration = 2.0f)
        {
            if (currentModeText == null) return;
            bannerFeedbackTween?.Kill();

            currentModeText.text = $"<color=#FFA502><b>{message}</b></color>";
            currentModeText.transform.DOKill();
            currentModeText.transform.DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.25f, 8, 0.5f);

            bannerFeedbackTween = DOVirtual.DelayedCall(duration, () => UpdateUIStateText()).SetUpdate(true);
        }

        public void ToggleOrderRush()
        {
            isOrderRushActive = !isOrderRushActive;

            if (orderRushOverlay != null)
            {
                if (isOrderRushActive)
                    orderRushOverlay.Show();
                else
                    orderRushOverlay.Hide();
            }

            if (btnOrderRush != null)
            {
                // 누를 때 쿵쾅거리는 펀치 스케일 모션
                btnOrderRush.transform.DOKill(true);
                btnOrderRush.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.35f, 10, 1f).SetEase(Ease.OutQuad);
            }

            UpdateUIStateText();
        }

        public void SelectMotion(MotionPreset preset)
        {
            currentPreset = preset;
            ApplySettingsToButtons();
            UpdateUIStateText();
        }

        public void ToggleRipple()
        {
            isRippleEnabled = !isRippleEnabled;
            ApplySettingsToButtons();
            UpdateUIStateText();
        }

        public void ToggleFloatingText()
        {
            isFloatingEnabled = !isFloatingEnabled;
            ApplySettingsToButtons();
            UpdateUIStateText();
        }

        public void ToggleSparkle()
        {
            isSparkleEnabled = !isSparkleEnabled;
            ApplySettingsToButtons();
            UpdateUIStateText();
        }

        public void ResetToDefaults()
        {
            currentPreset = MotionPreset.JellySquash;
            isRippleEnabled = true;
            isFloatingEnabled = true;
            isSparkleEnabled = false;

            if (isOrderRushActive)
            {
                isOrderRushActive = false;
                if (orderRushOverlay != null)
                {
                    orderRushOverlay.Hide();
                }
            }

            ApplySettingsToButtons();

            if (mainButton != null)
            {
                mainButton.ResetVisualState();
            }
            if (subButtons != null)
            {
                foreach (var btn in subButtons)
                {
                    if (btn != null)
                    {
                        btn.ResetVisualState();
                    }
                }
            }

            UpdateUIStateText();
        }

        private void ApplySettingsToButtons()
        {
            if (mainButton != null)
            {
                mainButton.SetMotionPreset(currentPreset);
                mainButton.SetFXOption(isRippleEnabled, isFloatingEnabled, isSparkleEnabled);
            }

            if (subButtons != null)
            {
                foreach (var btn in subButtons)
                {
                    if (btn != null)
                    {
                        btn.SetMotionPreset(currentPreset);
                        btn.SetFXOption(isRippleEnabled, isFloatingEnabled, isSparkleEnabled);
                    }
                }
            }
        }

        private void UpdateUIStateText()
        {
            string modeName = currentPreset switch
            {
                MotionPreset.JellySquash => "1. 젤리 스쿼시 (Jelly Squash)",
                MotionPreset.DepthPress => "2. 3D 뎁스 스위치 (Depth Press)",
                MotionPreset.StampHit => "3. 스탬프 쾅! (Stamp Hit)",
                MotionPreset.NeonPulse => "4. 네온 펄스 (Neon Pulse)",
                MotionPreset.GentlePop => "5. 세련된 심플 팝 (Gentle Pop)",
                MotionPreset.ConveyorSnap => "6. 컨베이어 스냅 (Conveyor Snap)",
                MotionPreset.BoxJolt => "7. 박스 덜컹 들썩임 (Box Jolt)",
                MotionPreset.BarcodeLaser => "8. 바코드 레이저 스캔 (Barcode Laser)",
                MotionPreset.TruckRevUp => "9. 🚚 배송 출발 (Rev-Up)",
                MotionPreset.TapeStripWipe => "10. 박스 테이프 와이프 (Tape Wipe)",
                _ => currentPreset.ToString()
            };

            string rippleStr = isRippleEnabled ? "<color=#2ECC71>ON</color>" : "<color=#E74C3C>OFF</color>";
            string floatingStr = isFloatingEnabled ? "<color=#2ECC71>ON</color>" : "<color=#E74C3C>OFF</color>";
            string sparkleStr = isSparkleEnabled ? "<color=#2ECC71>ON</color>" : "<color=#95A5A6>OFF</color>";

            if (currentModeText != null)
            {
                if (isOrderRushActive)
                {
                    currentModeText.text = $"<color=#FF4757><b>🚨 주문 폭주 피버 모드 발동 중! (택배 처리 속도 2배 FEVER!)</b></color>\n[선택 모드]: <b>{modeName}</b> | [활성 FX]: 물결 {rippleStr} | 플로팅 {floatingStr} | 스파클 {sparkleStr}";
                }
                else
                {
                    currentModeText.text = $"[선택 모드]: <b>{modeName}</b>\n[활성 FX]: 물결 리플 {rippleStr} | 플로팅 텍스트 {floatingStr} | 스파클 {sparkleStr}";
                }
            }

            if (textOrderRush != null)
            {
                textOrderRush.text = isOrderRushActive ? "🔥 주문 폭주: ON (FEVER!)" : "🔥 주문 폭주: OFF";
            }

            if (imgOrderRush != null)
            {
                imgOrderRush.color = isOrderRushActive ? new Color(0.92f, 0.18f, 0.15f, 1f) : new Color(0.85f, 0.34f, 0.25f, 1f);
            }

            if (textToggleRipple != null)
            {
                textToggleRipple.text = $"🌊 물결 리플: {(isRippleEnabled ? "ON" : "OFF")}";
            }

            if (textToggleFloating != null)
            {
                textToggleFloating.text = $"✨ 플로팅 텍스트: {(isFloatingEnabled ? "ON" : "OFF")}";
            }

            if (textToggleSparkle != null)
            {
                textToggleSparkle.text = $"⭐ 스파클 파티클: {(isSparkleEnabled ? "ON" : "OFF")}";
            }
        }

        #region Public Setters for Scene Builder

        public void SetReferences(
            UIButtonFeedback main,
            UIButtonFeedback[] subs,
            Text status,
            Button jelly,
            Button depth,
            Button stamp,
            Button neon,
            Button gentle,
            Button conveyor,
            Button boxJolt,
            Button barcode,
            Button truckRev,
            Button tape,
            Button togRipple,
            Text togRippleTxt,
            Button togFloat,
            Text togFloatTxt,
            Button togSparkle,
            Text togSparkleTxt,
            Button resetBtn,
            OrderRushOverlay rushOverlay = null,
            Button rushBtn = null,
            Text rushTxt = null,
            Image rushImg = null)
        {
            mainButton = main;
            subButtons = subs;
            currentModeText = status;

            btnJelly = jelly;
            btnDepth = depth;
            btnStamp = stamp;
            btnNeon = neon;
            btnGentle = gentle;
            btnConveyor = conveyor;
            btnBoxJolt = boxJolt;
            btnBarcode = barcode;
            btnTruckRev = truckRev;
            btnTape = tape;

            btnToggleRipple = togRipple;
            textToggleRipple = togRippleTxt;
            btnToggleFloating = togFloat;
            textToggleFloating = togFloatTxt;
            btnToggleSparkle = togSparkle;
            textToggleSparkle = togSparkleTxt;
            btnReset = resetBtn;

            orderRushOverlay = rushOverlay;
            btnOrderRush = rushBtn;
            textOrderRush = rushTxt;
            imgOrderRush = rushImg;
        }

        #endregion
    }
}
