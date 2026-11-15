using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToastMessageSample
{
    public sealed class ToastDemoController : MonoBehaviour
    {
        [Header("Font")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Header("Header & Version Switcher")]
        [SerializeField] private Button _ver1SwitchBtn;
        [SerializeField] private Button _ver2SwitchBtn;
        [SerializeField] private Image _ver1SwitchBg;
        [SerializeField] private Image _ver2SwitchBg;
        [SerializeField] private TextMeshProUGUI _ver1SwitchLabel;
        [SerializeField] private TextMeshProUGUI _ver2SwitchLabel;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI _modeIndicatorText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private Image _hudGoldIcon;
        [SerializeField] private Image _hudTruckIcon;

        [Header("Stage & Overlay")]
        [SerializeField] private Button _stageBackgroundBtn;
        [SerializeField] private RectTransform _toastOverlayLayer;
        [SerializeField] private List<WarehouseZoneCard> _zoneCards = new List<WarehouseZoneCard>();
        [SerializeField] private List<Image> _zoneIconImages = new List<Image>();
        [SerializeField] private List<ToastIconType> _zoneIconTypes = new List<ToastIconType>();

        [Header("Utility Buttons")]
        [SerializeField] private Button _dismissTopBtn;
        [SerializeField] private Button _burstDemoBtn;
        [SerializeField] private Button _clearAllBtn;

        [Header("Trigger Buttons (6 Presets)")]
        [SerializeField] private List<Button> _triggerButtons = new List<Button>();
        [SerializeField] private List<ToastPresetKey> _triggerPresetKeys = new List<ToastPresetKey>();
        [SerializeField] private List<Image> _triggerIcons = new List<Image>();

        [Header("Version Option Bars")]
        [SerializeField] private GameObject _v1OptionsGroup;
        [SerializeField] private GameObject _v2OptionsGroup;
        [SerializeField] private Button _v1ToggleAllBtn;
        [SerializeField] private Slider _v1PulseSlider;
        [SerializeField] private TextMeshProUGUI _v1PulseValueLabel;
        [SerializeField] private Toggle _v1AutoCollapseToggle;
        [SerializeField] private Toggle _v2AutoDismissToggle;
        [SerializeField] private TextMeshProUGUI _hintText;

        private ToastVersion _currentVersion = ToastVersion.Capsule;
        private float _pulseDuration = 2.1f;
        private bool _v1AutoCollapse = true;
        private bool _v2AutoDismiss = true;
        private int _goldAmount = 12450;
        private int _uidCounter;

        private readonly List<ToastSlotItem> _activeSlots = new List<ToastSlotItem>();
        private readonly Dictionary<string, Tween> _v1CollapseTimers = new Dictionary<string, Tween>();
        private readonly Dictionary<string, Tween> _v2DismissTimers = new Dictionary<string, Tween>();
        private readonly List<Tween> _burstTweens = new List<Tween>();

        private static readonly Color ActiveV1Color = ToastThemePalette.Hex("#C9534F");
        private static readonly Color ActiveV2Color = ToastThemePalette.Hex("#4C7599");
        private static readonly Color InactiveSwitchColor = new Color(0f, 0f, 0f, 0f);
        private static readonly Color InactiveSwitchTextColor = ToastThemePalette.Hex("#7A6B5D");

        private void Awake()
        {
            DOTween.SetTweensCapacity(500, 125);
            ApplyProceduralIcons();
            BindUIEvents();
            UpdateVersionSwitcherVisuals();
        }

        private void Start()
        {
            TriggerAlert(ToastPresetKey.ZoneA);
            DOVirtual.DelayedCall(0.18f, () =>
            {
                if (this != null && gameObject.activeInHierarchy)
                {
                    TriggerAlert(ToastPresetKey.DockFull);
                }
            }).SetLink(gameObject);
        }

        private void ApplyProceduralIcons()
        {
            if (_hudGoldIcon != null) _hudGoldIcon.sprite = ToastProceduralSprites.GetIcon(ToastIconType.Coin);
            if (_hudTruckIcon != null) _hudTruckIcon.sprite = ToastProceduralSprites.GetIcon(ToastIconType.Truck);

            for (int i = 0; i < _zoneIconImages.Count && i < _zoneIconTypes.Count; i++)
            {
                if (_zoneIconImages[i] != null)
                {
                    _zoneIconImages[i].sprite = ToastProceduralSprites.GetIcon(_zoneIconTypes[i]);
                }
            }

            for (int i = 0; i < _triggerIcons.Count && i < _triggerPresetKeys.Count; i++)
            {
                if (_triggerIcons[i] != null)
                {
                    ToastPresetData preset = ToastPresetCatalog.Get(_triggerPresetKeys[i]);
                    _triggerIcons[i].sprite = ToastProceduralSprites.GetIcon(preset.IconType);
                }
            }
        }

        private void BindUIEvents()
        {
            if (_ver1SwitchBtn != null) _ver1SwitchBtn.onClick.AddListener(() => SwitchVersion(ToastVersion.Capsule));
            if (_ver2SwitchBtn != null) _ver2SwitchBtn.onClick.AddListener(() => SwitchVersion(ToastVersion.Snackbar));

            if (_stageBackgroundBtn != null)
            {
                _stageBackgroundBtn.onClick.AddListener(CollapseAllV1Capsules);
            }

            if (_dismissTopBtn != null) _dismissTopBtn.onClick.AddListener(DismissTopAlert);
            if (_burstDemoBtn != null) _burstDemoBtn.onClick.AddListener(RunBurstDemo);
            if (_clearAllBtn != null) _clearAllBtn.onClick.AddListener(ClearAllAlerts);

            for (int i = 0; i < _triggerButtons.Count && i < _triggerPresetKeys.Count; i++)
            {
                ToastPresetKey key = _triggerPresetKeys[i];
                if (_triggerButtons[i] != null)
                {
                    _triggerButtons[i].onClick.AddListener(() => TriggerAlert(key));
                }
            }

            if (_v1ToggleAllBtn != null) _v1ToggleAllBtn.onClick.AddListener(ToggleAllV1Expanded);

            if (_v1PulseSlider != null)
            {
                _v1PulseSlider.minValue = 0.9f;
                _v1PulseSlider.maxValue = 3.6f;
                _v1PulseSlider.value = _pulseDuration;
                _v1PulseSlider.onValueChanged.AddListener(OnPulseSliderChanged);
                OnPulseSliderChanged(_pulseDuration);
            }

            if (_v1AutoCollapseToggle != null)
            {
                _v1AutoCollapseToggle.isOn = _v1AutoCollapse;
                _v1AutoCollapseToggle.onValueChanged.AddListener(OnV1AutoCollapseChanged);
            }

            if (_v2AutoDismissToggle != null)
            {
                _v2AutoDismissToggle.isOn = _v2AutoDismiss;
                _v2AutoDismissToggle.onValueChanged.AddListener(OnV2AutoDismissChanged);
            }
        }

        public void SwitchVersion(ToastVersion target)
        {
            if (_currentVersion == target) return;
            _currentVersion = target;

            ClearAllTimers();

            for (int i = 0; i < _activeSlots.Count; i++)
            {
                ToastSlotItem slot = _activeSlots[i];
                if (slot == null || slot.State == null || slot.State.Closing) continue;

                slot.SetVersionView(
                    _currentVersion,
                    _fontAsset,
                    _pulseDuration,
                    _v2AutoDismiss,
                    playEnterAnim: false,
                    onCapsuleClick: HandleCapsuleClick,
                    onSnackbarClick: HandleSnackbarClick,
                    onDismiss: state => DismissAlert(state.Uid));

                if (_currentVersion == ToastVersion.Capsule && slot.State.Expanded && _v1AutoCollapse)
                {
                    ScheduleV1Collapse(slot.State.Uid);
                }
                else if (_currentVersion == ToastVersion.Snackbar && _v2AutoDismiss)
                {
                    ScheduleV2Dismiss(slot.State.Uid);
                }
            }

            UpdateVersionSwitcherVisuals();
        }

        private void UpdateVersionSwitcherVisuals()
        {
            bool isV1 = _currentVersion == ToastVersion.Capsule;

            if (_ver1SwitchBg != null) _ver1SwitchBg.color = isV1 ? ActiveV1Color : InactiveSwitchColor;
            if (_ver2SwitchBg != null) _ver2SwitchBg.color = isV1 ? InactiveSwitchColor : ActiveV2Color;
            if (_ver1SwitchLabel != null) _ver1SwitchLabel.color = isV1 ? Color.white : InactiveSwitchTextColor;
            if (_ver2SwitchLabel != null) _ver2SwitchLabel.color = isV1 ? InactiveSwitchTextColor : Color.white;

            if (_v1OptionsGroup != null) _v1OptionsGroup.SetActive(isV1);
            if (_v2OptionsGroup != null) _v2OptionsGroup.SetActive(!isV1);

            if (_modeIndicatorText != null)
            {
                _modeIndicatorText.text = isV1
                    ? "현재 보기: Ver 1 (원형 -> 캡슐 확장형)"
                    : "현재 보기: Ver 2 (슬림 사각 스낵바형)";
            }

            if (_hintText != null)
            {
                _hintText.text = isV1
                    ? "원형 아이콘 클릭 시 가로 캡슐로 확장 / 윗 알림 종료 시 아래 알림이 부드럽게 위로 이동"
                    : "스낵바 클릭 시 맵 구역 하이라이트 / 왼쪽 스와이프(드래그) 시 즉시 닫힘";
            }
        }

        public void TriggerAlert(ToastPresetKey presetKey)
        {
            if (_toastOverlayLayer == null) return;
            ToastPresetData preset = ToastPresetCatalog.Get(presetKey);

            if (preset.GoldDelta > 0)
            {
                _goldAmount += preset.GoldDelta;
                if (_goldText != null)
                {
                    _goldText.text = string.Format("{0:N0}G", _goldAmount);
                }
            }

            PingZone(preset.TargetZoneId, preset.IsSuccessPing);

            // Refresh existing non-closing alert if duplicate preset
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                ToastSlotItem existing = _activeSlots[i];
                if (existing != null && existing.State != null && !existing.State.Closing && existing.State.Preset.Key == presetKey)
                {
                    if (_currentVersion == ToastVersion.Capsule)
                    {
                        SetV1Expanded(existing, true);
                    }
                    else
                    {
                        if (existing.SnackbarView != null)
                        {
                            existing.SnackbarView.SetCountdownActive(_v2AutoDismiss);
                        }
                        if (_v2AutoDismiss)
                        {
                            ScheduleV2Dismiss(existing.State.Uid);
                        }
                    }
                    return;
                }
            }

            // Cap at 3 visible alerts: collapse oldest top alert
            List<ToastSlotItem> visible = new List<ToastSlotItem>();
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i] != null && _activeSlots[i].State != null && !_activeSlots[i].State.Closing)
                {
                    visible.Add(_activeSlots[i]);
                }
            }
            if (visible.Count >= 3)
            {
                DismissAlert(visible[0].State.Uid);
            }

            var state = new ToastAlertState
            {
                Uid = "alert-" + (++_uidCounter),
                Preset = preset,
                Expanded = false,
                Closing = false
            };

            var slotGo = new GameObject("ToastSlot_" + state.Uid, typeof(RectTransform), typeof(LayoutElement), typeof(CanvasGroup));
            slotGo.transform.SetParent(_toastOverlayLayer, false);

            var slot = slotGo.AddComponent<ToastSlotItem>();
            slot.Initialize(
                state,
                _currentVersion,
                _fontAsset,
                _pulseDuration,
                _v2AutoDismiss,
                playEnterAnim: true,
                onCapsuleClick: HandleCapsuleClick,
                onSnackbarClick: HandleSnackbarClick,
                onDismiss: s => DismissAlert(s.Uid));

            _activeSlots.Add(slot);

            if (_currentVersion == ToastVersion.Snackbar && _v2AutoDismiss)
            {
                ScheduleV2Dismiss(state.Uid);
            }
        }

        private void HandleCapsuleClick(ToastAlertState state)
        {
            if (state == null || state.Closing) return;
            ToastSlotItem targetSlot = FindSlot(state.Uid);
            if (targetSlot == null) return;

            if (!state.Expanded)
            {
                // Collapse other expanded capsules
                for (int i = 0; i < _activeSlots.Count; i++)
                {
                    ToastSlotItem other = _activeSlots[i];
                    if (other != null && other.State != null && other.State.Uid != state.Uid && other.State.Expanded)
                    {
                        SetV1Expanded(other, false);
                    }
                }
                SetV1Expanded(targetSlot, true);
            }
            else
            {
                PingZone(state.Preset.TargetZoneId, state.Preset.IsSuccessPing);
                SetV1Expanded(targetSlot, false);
            }
        }

        private void HandleSnackbarClick(ToastAlertState state)
        {
            if (state == null || state.Closing) return;
            PingZone(state.Preset.TargetZoneId, state.Preset.IsSuccessPing);
            DismissAlert(state.Uid);
        }

        private void SetV1Expanded(ToastSlotItem slot, bool expanded)
        {
            if (slot == null || slot.State == null || slot.State.Closing) return;
            slot.State.Expanded = expanded;
            if (slot.CapsuleView != null)
            {
                slot.CapsuleView.SetExpanded(expanded, animate: true);
            }

            if (expanded && _v1AutoCollapse)
            {
                ScheduleV1Collapse(slot.State.Uid);
            }
            else
            {
                CancelTimer(_v1CollapseTimers, slot.State.Uid);
            }
        }

        private void DismissAlert(string uid)
        {
            ToastSlotItem slot = FindSlot(uid);
            if (slot == null || slot.State == null || slot.State.Closing) return;

            slot.State.Closing = true;
            CancelTimer(_v1CollapseTimers, uid);
            CancelTimer(_v2DismissTimers, uid);

            slot.CollapseAndDestroy(completed =>
            {
                _activeSlots.Remove(completed);
            });
        }

        private void DismissTopAlert()
        {
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                ToastSlotItem slot = _activeSlots[i];
                if (slot != null && slot.State != null && !slot.State.Closing)
                {
                    DismissAlert(slot.State.Uid);
                    return;
                }
            }
        }

        private void RunBurstDemo()
        {
            KillBurstTweens();
            ToastPresetKey[] seq = { ToastPresetKey.ZoneA, ToastPresetKey.DockFull, ToastPresetKey.StaffStress };
            for (int i = 0; i < seq.Length; i++)
            {
                ToastPresetKey key = seq[i];
                Tween t = DOVirtual.DelayedCall(i * 0.24f, () =>
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        TriggerAlert(key);
                    }
                }).SetLink(gameObject);
                _burstTweens.Add(t);
            }
        }

        private void ClearAllAlerts()
        {
            KillBurstTweens();
            ClearAllTimers();

            for (int i = _activeSlots.Count - 1; i >= 0; i--)
            {
                if (_activeSlots[i] != null)
                {
                    Destroy(_activeSlots[i].gameObject);
                }
            }
            _activeSlots.Clear();
        }

        private void ToggleAllV1Expanded()
        {
            List<ToastSlotItem> visible = new List<ToastSlotItem>();
            bool anyCollapsed = false;

            for (int i = 0; i < _activeSlots.Count; i++)
            {
                ToastSlotItem slot = _activeSlots[i];
                if (slot != null && slot.State != null && !slot.State.Closing)
                {
                    visible.Add(slot);
                    if (!slot.State.Expanded) anyCollapsed = true;
                }
            }

            if (visible.Count == 0)
            {
                TriggerAlert(ToastPresetKey.ZoneA);
                return;
            }

            for (int i = 0; i < visible.Count; i++)
            {
                SetV1Expanded(visible[i], anyCollapsed);
            }
        }

        private void CollapseAllV1Capsules()
        {
            if (_currentVersion != ToastVersion.Capsule) return;
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                ToastSlotItem slot = _activeSlots[i];
                if (slot != null && slot.State != null && !slot.State.Closing && slot.State.Expanded)
                {
                    SetV1Expanded(slot, false);
                }
            }
        }

        private void OnPulseSliderChanged(float value)
        {
            _pulseDuration = Mathf.Clamp(value, 0.9f, 3.6f);
            if (_v1PulseValueLabel != null)
            {
                _v1PulseValueLabel.text = string.Format("{0:F1}s", _pulseDuration);
            }

            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i] != null && _activeSlots[i].CapsuleView != null)
                {
                    _activeSlots[i].CapsuleView.SetPulseDuration(_pulseDuration);
                }
            }
        }

        private void OnV1AutoCollapseChanged(bool isOn)
        {
            _v1AutoCollapse = isOn;
            if (_v1AutoCollapse)
            {
                for (int i = 0; i < _activeSlots.Count; i++)
                {
                    ToastSlotItem slot = _activeSlots[i];
                    if (slot != null && slot.State != null && !slot.State.Closing && slot.State.Expanded)
                    {
                        ScheduleV1Collapse(slot.State.Uid);
                    }
                }
            }
            else
            {
                foreach (var kv in _v1CollapseTimers) kv.Value?.Kill();
                _v1CollapseTimers.Clear();
            }
        }

        private void OnV2AutoDismissChanged(bool isOn)
        {
            _v2AutoDismiss = isOn;
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                ToastSlotItem slot = _activeSlots[i];
                if (slot == null || slot.State == null || slot.State.Closing) continue;

                if (slot.SnackbarView != null)
                {
                    slot.SnackbarView.SetCountdownActive(_v2AutoDismiss);
                }

                if (_v2AutoDismiss)
                {
                    ScheduleV2Dismiss(slot.State.Uid);
                }
                else
                {
                    CancelTimer(_v2DismissTimers, slot.State.Uid);
                }
            }
        }

        private void PingZone(string zoneId, bool isSuccess)
        {
            for (int i = 0; i < _zoneCards.Count; i++)
            {
                if (_zoneCards[i] != null && _zoneCards[i].ZoneId == zoneId)
                {
                    _zoneCards[i].Ping(isSuccess, isInfo: false);
                    return;
                }
            }
        }

        private void ScheduleV1Collapse(string uid)
        {
            CancelTimer(_v1CollapseTimers, uid);
            if (!_v1AutoCollapse) return;

            Tween t = DOVirtual.DelayedCall(4.0f, () =>
            {
                ToastSlotItem slot = FindSlot(uid);
                if (slot != null) SetV1Expanded(slot, false);
            }).SetLink(gameObject);
            _v1CollapseTimers[uid] = t;
        }

        private void ScheduleV2Dismiss(string uid)
        {
            CancelTimer(_v2DismissTimers, uid);
            if (!_v2AutoDismiss) return;

            Tween t = DOVirtual.DelayedCall(4.0f, () =>
            {
                DismissAlert(uid);
            }).SetLink(gameObject);
            _v2DismissTimers[uid] = t;
        }

        private ToastSlotItem FindSlot(string uid)
        {
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i] != null && _activeSlots[i].State != null && _activeSlots[i].State.Uid == uid)
                {
                    return _activeSlots[i];
                }
            }
            return null;
        }

        private static void CancelTimer(Dictionary<string, Tween> map, string uid)
        {
            if (map.TryGetValue(uid, out Tween existing))
            {
                existing?.Kill();
                map.Remove(uid);
            }
        }

        private void ClearAllTimers()
        {
            foreach (var kv in _v1CollapseTimers) kv.Value?.Kill();
            _v1CollapseTimers.Clear();

            foreach (var kv in _v2DismissTimers) kv.Value?.Kill();
            _v2DismissTimers.Clear();
        }

        private void KillBurstTweens()
        {
            for (int i = 0; i < _burstTweens.Count; i++)
            {
                _burstTweens[i]?.Kill();
            }
            _burstTweens.Clear();
        }

        private void OnDestroy()
        {
            ClearAllTimers();
            KillBurstTweens();
        }
    }
}
