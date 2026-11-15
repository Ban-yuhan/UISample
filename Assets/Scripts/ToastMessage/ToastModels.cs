using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToastMessageSample
{
    public enum ToastVersion
    {
        Capsule = 0,
        Snackbar = 1
    }

    public enum ToastTone
    {
        Danger = 0,
        Staff = 1,
        Warning = 2,
        Order = 3,
        Success = 4
    }

    public enum ToastPresetKey
    {
        ZoneA = 0,
        DockFull = 1,
        StaffStress = 2,
        ZoneBWarn = 3,
        VipOrder = 4,
        TruckDone = 5
    }

    public enum ToastIconType
    {
        Siren = 0,
        Flame = 1,
        WarningTriangle = 2,
        Bolt = 3,
        Truck = 4,
        Box = 5,
        Coin = 6,
        Coffee = 7,
        CloseX = 8
    }

    [Serializable]
    public sealed class ToastPresetData
    {
        public ToastPresetKey Key;
        public string Title;
        public string Sub;
        public ToastTone Tone;
        public ToastIconType IconType;
        public string TargetZoneId;
        public bool IsSuccessPing;
        public bool IsDangerSeverity;
        public int GoldDelta;
        public string TagLabel;
    }

    public sealed class ToastThemePalette
    {
        public Color PillBackground;
        public Color PillBorder;
        public Color AuraGlowColor;
        public Color AuraRingColor;
        public Color SnackbarBorder;
        public Color SnackbarIconBg;
        public Color SnackbarIconTint;
        public Color SnackbarProgress;
        public Color BorderPulseColor;
        public Color IconTint;

        private static readonly Dictionary<ToastTone, ToastThemePalette> Palettes = new Dictionary<ToastTone, ToastThemePalette>
        {
            {
                ToastTone.Danger,
                new ToastThemePalette
                {
                    PillBackground = Hex("#C9534F"),
                    PillBorder = Hex("#FFFDF8"),
                    AuraGlowColor = new Color(0.788f, 0.325f, 0.310f, 0.45f),
                    AuraRingColor = new Color(0.788f, 0.325f, 0.310f, 0.50f),
                    SnackbarBorder = Hex("#C9534F"),
                    SnackbarIconBg = new Color(0.788f, 0.325f, 0.310f, 0.16f),
                    SnackbarIconTint = Hex("#C9534F"),
                    SnackbarProgress = Hex("#C9534F"),
                    BorderPulseColor = new Color(0.788f, 0.325f, 0.310f, 0.90f),
                    IconTint = Hex("#FFFDF8")
                }
            },
            {
                ToastTone.Staff,
                new ToastThemePalette
                {
                    PillBackground = Hex("#C26D4A"),
                    PillBorder = Hex("#FFFDF8"),
                    AuraGlowColor = new Color(0.761f, 0.427f, 0.290f, 0.45f),
                    AuraRingColor = new Color(0.761f, 0.427f, 0.290f, 0.50f),
                    SnackbarBorder = Hex("#C26D4A"),
                    SnackbarIconBg = new Color(0.761f, 0.427f, 0.290f, 0.16f),
                    SnackbarIconTint = Hex("#C26D4A"),
                    SnackbarProgress = Hex("#C26D4A"),
                    BorderPulseColor = new Color(0.761f, 0.427f, 0.290f, 0.90f),
                    IconTint = Hex("#FFFDF8")
                }
            },
            {
                ToastTone.Warning,
                new ToastThemePalette
                {
                    PillBackground = Hex("#D98A2B"),
                    PillBorder = Hex("#FFFDF8"),
                    AuraGlowColor = new Color(0.851f, 0.541f, 0.169f, 0.45f),
                    AuraRingColor = new Color(0.851f, 0.541f, 0.169f, 0.50f),
                    SnackbarBorder = Hex("#D98A2B"),
                    SnackbarIconBg = new Color(0.851f, 0.541f, 0.169f, 0.16f),
                    SnackbarIconTint = Hex("#D98A2B"),
                    SnackbarProgress = Hex("#D98A2B"),
                    BorderPulseColor = new Color(0.851f, 0.541f, 0.169f, 0.85f),
                    IconTint = Hex("#FFFDF8")
                }
            },
            {
                ToastTone.Order,
                new ToastThemePalette
                {
                    PillBackground = Hex("#4C7599"),
                    PillBorder = Hex("#FFFDF8"),
                    AuraGlowColor = new Color(0.298f, 0.459f, 0.600f, 0.45f),
                    AuraRingColor = new Color(0.298f, 0.459f, 0.600f, 0.50f),
                    SnackbarBorder = Hex("#4C7599"),
                    SnackbarIconBg = new Color(0.298f, 0.459f, 0.600f, 0.16f),
                    SnackbarIconTint = Hex("#4C7599"),
                    SnackbarProgress = Hex("#4C7599"),
                    BorderPulseColor = new Color(0.298f, 0.459f, 0.600f, 0.85f),
                    IconTint = Hex("#FFFDF8")
                }
            },
            {
                ToastTone.Success,
                new ToastThemePalette
                {
                    PillBackground = Hex("#4E8C6F"),
                    PillBorder = Hex("#FFFDF8"),
                    AuraGlowColor = new Color(0.306f, 0.549f, 0.435f, 0.45f),
                    AuraRingColor = new Color(0.306f, 0.549f, 0.435f, 0.50f),
                    SnackbarBorder = Hex("#4E8C6F"),
                    SnackbarIconBg = new Color(0.306f, 0.549f, 0.435f, 0.16f),
                    SnackbarIconTint = Hex("#4E8C6F"),
                    SnackbarProgress = Hex("#4E8C6F"),
                    BorderPulseColor = new Color(0.306f, 0.549f, 0.435f, 0.85f),
                    IconTint = Hex("#FFFDF8")
                }
            }
        };

        public static ToastThemePalette Get(ToastTone tone)
        {
            return Palettes.TryGetValue(tone, out ToastThemePalette palette)
                ? palette
                : Palettes[ToastTone.Danger];
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }
    }

    public sealed class ToastAlertState
    {
        public string Uid;
        public ToastPresetData Preset;
        public bool Expanded;
        public bool Closing;
    }

    public static class ToastPresetCatalog
    {
        private static readonly Dictionary<ToastPresetKey, ToastPresetData> Presets = new Dictionary<ToastPresetKey, ToastPresetData>
        {
            {
                ToastPresetKey.ZoneA,
                new ToastPresetData
                {
                    Key = ToastPresetKey.ZoneA,
                    Title = "A적재구역 가득 참!",
                    Sub = "터치하여 A구역 비우기 >",
                    Tone = ToastTone.Danger,
                    IconType = ToastIconType.Siren,
                    TargetZoneId = "zone-A",
                    IsSuccessPing = false,
                    IsDangerSeverity = true,
                    GoldDelta = 0,
                    TagLabel = "위험"
                }
            },
            {
                ToastPresetKey.DockFull,
                new ToastPresetData
                {
                    Key = ToastPresetKey.DockFull,
                    Title = "하차도크 가득 참!",
                    Sub = "컨베이어 적체 확인 >",
                    Tone = ToastTone.Danger,
                    IconType = ToastIconType.Siren,
                    TargetZoneId = "zone-dock",
                    IsSuccessPing = false,
                    IsDangerSeverity = true,
                    GoldDelta = 0,
                    TagLabel = "위험"
                }
            },
            {
                ToastPresetKey.StaffStress,
                new ToastPresetData
                {
                    Key = ToastPresetKey.StaffStress,
                    Title = "김알바 스트레스 폭발 직전!",
                    Sub = "휴게실 이동 또는 격려 필요 >",
                    Tone = ToastTone.Staff,
                    IconType = ToastIconType.Flame,
                    TargetZoneId = "zone-staff",
                    IsSuccessPing = false,
                    IsDangerSeverity = true,
                    GoldDelta = 0,
                    TagLabel = "알바"
                }
            },
            {
                ToastPresetKey.ZoneBWarn,
                new ToastPresetData
                {
                    Key = ToastPresetKey.ZoneBWarn,
                    Title = "B적재구역 잔여 20% 미만",
                    Sub = "출차 트럭 우선 배정 권장",
                    Tone = ToastTone.Warning,
                    IconType = ToastIconType.WarningTriangle,
                    TargetZoneId = "zone-B",
                    IsSuccessPing = false,
                    IsDangerSeverity = false,
                    GoldDelta = 0,
                    TagLabel = "주의"
                }
            },
            {
                ToastPresetKey.VipOrder,
                new ToastPresetData
                {
                    Key = ToastPresetKey.VipOrder,
                    Title = "VIP 특급 주문 도착!",
                    Sub = "C구역 특급 화물 상차 대기",
                    Tone = ToastTone.Order,
                    IconType = ToastIconType.Bolt,
                    TargetZoneId = "zone-C",
                    IsSuccessPing = true,
                    IsDangerSeverity = false,
                    GoldDelta = 0,
                    TagLabel = "주문"
                }
            },
            {
                ToastPresetKey.TruckDone,
                new ToastPresetData
                {
                    Key = ToastPresetKey.TruckDone,
                    Title = "1번 트럭 상차 완료! (+1,200G)",
                    Sub = "정시 출차 보너스 지급 완료",
                    Tone = ToastTone.Success,
                    IconType = ToastIconType.Truck,
                    TargetZoneId = "zone-gate",
                    IsSuccessPing = true,
                    IsDangerSeverity = false,
                    GoldDelta = 1200,
                    TagLabel = "완료"
                }
            }
        };

        public static ToastPresetData Get(ToastPresetKey key)
        {
            return Presets.TryGetValue(key, out ToastPresetData data)
                ? data
                : Presets[ToastPresetKey.ZoneA];
        }
    }
}
