using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToastMessageSample.Editor
{
    public static class ToastSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ToastMessageScene.unity";
        private const string FontPath = "Assets/Fonts/MALGUN SDF.asset";

        [MenuItem("Tools/Toast Message Sample/Rebuild ToastMessageScene")]
        public static void BuildScene()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[ToastSceneBuilder] Could not find font at " + FontPath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "ToastCanvas" || root.name == "EventSystem")
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            EnsureEventSystem();

            // 1. Canvas
            var canvasGo = new GameObject("ToastCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 886f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var controller = canvasGo.AddComponent<ToastDemoController>();

            // 2. Fullscreen Background (Warm Sand Beige)
            var bgGo = CreateUI("Background", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = ToastThemePalette.Hex("#EAE1D2");
            bgImg.raycastTarget = false;

            // 3. Top Header (Soft Warm Cream + Tan Border)
            var headerGo = CreateUI("TopHeader", canvasGo.transform);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(24f, -86f);
            headerRect.offsetMax = new Vector2(-24f, -10f);
            var headerBorder = headerGo.AddComponent<Image>();
            headerBorder.color = ToastThemePalette.Hex("#D8CCB8");

            var headerInner = CreateUI("Inner", headerRect);
            var headerInnerRect = headerInner.GetComponent<RectTransform>();
            StretchFull(headerInnerRect);
            headerInnerRect.offsetMin = new Vector2(1.5f, 1.5f);
            headerInnerRect.offsetMax = new Vector2(-1.5f, -1.5f);
            headerInner.AddComponent<Image>().color = ToastThemePalette.Hex("#F7F2E8");

            var badgeGo = CreateUI("TitleBadge", headerRect);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(20f, -10f);
            badgeRect.sizeDelta = new Vector2(320f, 20f);
            badgeGo.AddComponent<Image>().color = ToastThemePalette.Hex("#EFE7D8");
            var badgeTmp = CreateTmp(badgeRect, font, "LANDSCAPE 19.5:9 - TOAST UI COMPARATOR", 11f, FontStyles.Bold, ToastThemePalette.Hex("#7A6B5D"), TextAlignmentOptions.Center);
            StretchFull(badgeTmp.rectTransform);

            var mainTitleGo = CreateUI("MainTitle", headerRect);
            var mainTitleRect = mainTitleGo.GetComponent<RectTransform>();
            mainTitleRect.anchorMin = new Vector2(0f, 0f);
            mainTitleRect.anchorMax = new Vector2(0f, 0f);
            mainTitleRect.pivot = new Vector2(0f, 0f);
            mainTitleRect.anchoredPosition = new Vector2(20f, 10f);
            mainTitleRect.sizeDelta = new Vector2(520f, 32f);
            CreateTmpOn(mainTitleGo, font, "택배물류 타이쿤 UI 프로토타입 비교기", 22f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            // Version Switcher
            var switcherGo = CreateUI("VersionSwitcher", headerRect);
            var switcherRect = switcherGo.GetComponent<RectTransform>();
            switcherRect.anchorMin = new Vector2(1f, 0.5f);
            switcherRect.anchorMax = new Vector2(1f, 0.5f);
            switcherRect.pivot = new Vector2(1f, 0.5f);
            switcherRect.anchoredPosition = new Vector2(-18f, 0f);
            switcherRect.sizeDelta = new Vector2(510f, 48f);
            switcherGo.AddComponent<Image>().color = ToastThemePalette.Hex("#EFE7D8");

            Button v1Btn = CreateSwitchButton("Ver1Btn", switcherRect, font, "Ver 1: 원형 -> 캡슐 확장형 [54px]", new Vector2(4f, 0f), new Vector2(248f, 40f), true, out Image v1Bg, out TextMeshProUGUI v1Label);
            Button v2Btn = CreateSwitchButton("Ver2Btn", switcherRect, font, "Ver 2: 슬림 사각 스낵바형 [248px]", new Vector2(258f, 0f), new Vector2(248f, 40f), false, out Image v2Bg, out TextMeshProUGUI v2Label);

            // 4. Center Viewport Stage (Warm Light Linen + Tan Border)
            var stageGo = CreateUI("ViewportStage", canvasGo.transform);
            var stageRect = stageGo.GetComponent<RectTransform>();
            stageRect.anchorMin = Vector2.zero;
            stageRect.anchorMax = Vector2.one;
            stageRect.offsetMin = new Vector2(24f, 232f);
            stageRect.offsetMax = new Vector2(-24f, -96f);
            stageGo.AddComponent<Image>().color = ToastThemePalette.Hex("#D8CCB8");
            var stageBtn = stageGo.AddComponent<Button>();
            stageBtn.transition = Selectable.Transition.None;

            var stageInner = CreateUI("StageFloor", stageRect);
            var stageInnerRect = stageInner.GetComponent<RectTransform>();
            StretchFull(stageInnerRect);
            stageInnerRect.offsetMin = new Vector2(1.5f, 1.5f);
            stageInnerRect.offsetMax = new Vector2(-1.5f, -1.5f);
            var stageFloorImg = stageInner.AddComponent<Image>();
            stageFloorImg.color = ToastThemePalette.Hex("#F3ECE0");
            stageFloorImg.raycastTarget = false;

            // Left Notch
            var notchGo = CreateUI("LeftNotch", stageRect);
            var notchRect = notchGo.GetComponent<RectTransform>();
            notchRect.anchorMin = new Vector2(0f, 0.25f);
            notchRect.anchorMax = new Vector2(0f, 0.75f);
            notchRect.pivot = new Vector2(0f, 0.5f);
            notchRect.offsetMin = Vector2.zero;
            notchRect.offsetMax = new Vector2(20f, 0f);
            notchGo.AddComponent<Image>().color = ToastThemePalette.Hex("#E8DFCE");

            // Top HUD
            var hudGo = CreateUI("IngameHUD", stageRect);
            var hudRect = hudGo.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.offsetMin = new Vector2(36f, -54f);
            hudRect.offsetMax = new Vector2(-20f, -10f);

            // HUD Left Cluster
            var hubChip = CreateHudChip("HubChip", hudRect, new Vector2(0f, 0f), new Vector2(210f, 38f), true);
            var lvBadge = CreateUI("LvBadge", hubChip.transform);
            var lvRect = lvBadge.GetComponent<RectTransform>();
            lvRect.anchorMin = new Vector2(0f, 0.5f);
            lvRect.anchorMax = new Vector2(0f, 0.5f);
            lvRect.pivot = new Vector2(0f, 0.5f);
            lvRect.anchoredPosition = new Vector2(10f, 0f);
            lvRect.sizeDelta = new Vector2(48f, 24f);
            lvBadge.AddComponent<Image>().color = ToastThemePalette.Hex("#4A3B31");
            var lvTmp = CreateTmp(lvRect, font, "LV.7", 12f, FontStyles.Bold, ToastThemePalette.Hex("#FFFDF8"), TextAlignmentOptions.Center);
            StretchFull(lvTmp.rectTransform);

            var hubTextGo = CreateUI("HubName", hubChip.transform);
            var hubTextRect = hubTextGo.GetComponent<RectTransform>();
            hubTextRect.anchorMin = new Vector2(0f, 0.5f);
            hubTextRect.anchorMax = new Vector2(1f, 0.5f);
            hubTextRect.offsetMin = new Vector2(66f, -16f);
            hubTextRect.offsetMax = new Vector2(-8f, 16f);
            CreateTmpOn(hubTextGo, font, "제2 물류허브", 14f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            var modeChip = CreateUI("ModeIndicator", hudRect);
            var modeRect = modeChip.GetComponent<RectTransform>();
            modeRect.anchorMin = new Vector2(0f, 0.5f);
            modeRect.anchorMax = new Vector2(0f, 0.5f);
            modeRect.pivot = new Vector2(0f, 0.5f);
            modeRect.anchoredPosition = new Vector2(224f, 0f);
            modeRect.sizeDelta = new Vector2(380f, 32f);
            var modeIndicatorText = CreateTmpOn(modeChip, font, "현재 보기: Ver 1 (원형 -> 캡슐 확장형)", 13f, FontStyles.Bold, ToastThemePalette.Hex("#7A6B5D"), TextAlignmentOptions.MidlineLeft);

            // HUD Right Cluster
            var goldChip = CreateHudChip("GoldChip", hudRect, new Vector2(0f, 0f), new Vector2(165f, 38f), false);
            var goldChipRect = goldChip.GetComponent<RectTransform>();
            goldChipRect.anchorMin = new Vector2(1f, 0.5f);
            goldChipRect.anchorMax = new Vector2(1f, 0.5f);
            goldChipRect.pivot = new Vector2(1f, 0.5f);
            goldChipRect.anchoredPosition = new Vector2(-190f, 0f);
            Image hudGoldIcon = CreateHudIcon(goldChipRect, ToastThemePalette.Hex("#D98A2B"));
            var goldTextGo = CreateUI("GoldText", goldChipRect);
            var goldTextRect = goldTextGo.GetComponent<RectTransform>();
            goldTextRect.anchorMin = Vector2.zero;
            goldTextRect.anchorMax = Vector2.one;
            goldTextRect.offsetMin = new Vector2(36f, 0f);
            goldTextRect.offsetMax = new Vector2(-10f, 0f);
            TextMeshProUGUI goldText = CreateTmpOn(goldTextGo, font, "12,450G", 15f, FontStyles.Bold, ToastThemePalette.Hex("#B45309"), TextAlignmentOptions.MidlineRight);

            var truckChip = CreateHudChip("TruckChip", hudRect, new Vector2(0f, 0f), new Vector2(178f, 38f), false);
            var truckChipRect = truckChip.GetComponent<RectTransform>();
            truckChipRect.anchorMin = new Vector2(1f, 0.5f);
            truckChipRect.anchorMax = new Vector2(1f, 0.5f);
            truckChipRect.pivot = new Vector2(1f, 0.5f);
            truckChipRect.anchoredPosition = new Vector2(0f, 0f);
            Image hudTruckIcon = CreateHudIcon(truckChipRect, ToastThemePalette.Hex("#4E8C6F"));
            var truckTextGo = CreateUI("TruckText", truckChipRect);
            var truckTextRect = truckTextGo.GetComponent<RectTransform>();
            truckTextRect.anchorMin = Vector2.zero;
            truckTextRect.anchorMax = Vector2.one;
            truckTextRect.offsetMin = new Vector2(36f, 0f);
            truckTextRect.offsetMax = new Vector2(-10f, 0f);
            CreateTmpOn(truckTextGo, font, "상차 트럭 2/3", 14f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineRight);

            // 5. Warehouse 6 Zone Cards
            var zonesRoot = CreateUI("WarehouseZonesGrid", stageRect);
            var zonesRootRect = zonesRoot.GetComponent<RectTransform>();
            zonesRootRect.anchorMin = Vector2.zero;
            zonesRootRect.anchorMax = Vector2.one;
            zonesRootRect.offsetMin = new Vector2(410f, 18f);
            zonesRootRect.offsetMax = new Vector2(-24f, -68f);

            var zoneCards = new List<WarehouseZoneCard>();
            var zoneIcons = new List<Image>();
            var zoneIconTypes = new List<ToastIconType>();

            zoneCards.Add(CreateZoneCard(zonesRootRect, font, "zone-dock", "하차도크", "적체 주의", ToastThemePalette.Hex("#C9534F"),
                "하차 벨트", "18 / 20", 0.90f, ToastThemePalette.Hex("#C9534F"),
                new Vector2(0f, 0f), new Vector2(0.24f, 1f), out Image dockIcon));
            zoneIcons.Add(dockIcon);
            zoneIconTypes.Add(ToastIconType.Box);

            zoneCards.Add(CreateZoneCard(zonesRootRect, font, "zone-A", "A적재구역", "가득 참", ToastThemePalette.Hex("#C9534F"),
                "적재율", "20 / 20", 1.0f, ToastThemePalette.Hex("#C9534F"),
                new Vector2(0.26f, 0.52f), new Vector2(0.495f, 1f), out Image aIcon));
            zoneIcons.Add(aIcon);
            zoneIconTypes.Add(ToastIconType.Box);

            zoneCards.Add(CreateZoneCard(zonesRootRect, font, "zone-B", "B적재구역", "잔여 15%", ToastThemePalette.Hex("#D98A2B"),
                "적재율", "17 / 20", 0.85f, ToastThemePalette.Hex("#D98A2B"),
                new Vector2(0.505f, 0.52f), new Vector2(0.74f, 1f), out Image bIcon));
            zoneIcons.Add(bIcon);
            zoneIconTypes.Add(ToastIconType.Box);

            zoneCards.Add(CreateZoneCard(zonesRootRect, font, "zone-C", "C적재구역", "여유", ToastThemePalette.Hex("#4E8C6F"),
                "적재율", "9 / 20", 0.45f, ToastThemePalette.Hex("#4E8C6F"),
                new Vector2(0.26f, 0f), new Vector2(0.495f, 0.48f), out Image cIcon));
            zoneIcons.Add(cIcon);
            zoneIconTypes.Add(ToastIconType.Box);

            zoneCards.Add(CreateZoneCard(zonesRootRect, font, "zone-staff", "알바 휴게실", "김알바 한계", ToastThemePalette.Hex("#C26D4A"),
                "김알바", "스트레스 94%", 0.94f, ToastThemePalette.Hex("#C26D4A"),
                new Vector2(0.505f, 0f), new Vector2(0.74f, 0.48f), out Image staffIcon));
            zoneIcons.Add(staffIcon);
            zoneIconTypes.Add(ToastIconType.Coffee);

            zoneCards.Add(CreateZoneCard(zonesRootRect, font, "zone-gate", "상차 게이트", "2/3 도킹", ToastThemePalette.Hex("#4C7599"),
                "출차 대기", "1번·2번 트럭", 0.67f, ToastThemePalette.Hex("#4C7599"),
                new Vector2(0.76f, 0f), new Vector2(1f, 1f), out Image gateIcon));
            zoneIcons.Add(gateIcon);
            zoneIconTypes.Add(ToastIconType.Truck);

            // 6. Toast Overlay Layer
            var toastLayerGo = CreateUI("ToastOverlayLayer", stageRect);
            var toastLayerRect = toastLayerGo.GetComponent<RectTransform>();
            toastLayerRect.anchorMin = new Vector2(0f, 1f);
            toastLayerRect.anchorMax = new Vector2(0f, 1f);
            toastLayerRect.pivot = new Vector2(0f, 1f);
            toastLayerRect.anchoredPosition = new Vector2(40f, -68f);
            toastLayerRect.sizeDelta = new Vector2(360f, 440f);
            var vlg = toastLayerGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 0f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // 7. Bottom Control Deck (Soft Warm Cream + Tan Border)
            var deckGo = CreateUI("ControlDeck", canvasGo.transform);
            var deckRect = deckGo.GetComponent<RectTransform>();
            deckRect.anchorMin = new Vector2(0f, 0f);
            deckRect.anchorMax = new Vector2(1f, 0f);
            deckRect.pivot = new Vector2(0.5f, 0f);
            deckRect.offsetMin = new Vector2(24f, 10f);
            deckRect.offsetMax = new Vector2(-24f, 222f);
            deckGo.AddComponent<Image>().color = ToastThemePalette.Hex("#D8CCB8");

            var deckInner = CreateUI("Inner", deckRect);
            var deckInnerRect = deckInner.GetComponent<RectTransform>();
            StretchFull(deckInnerRect);
            deckInnerRect.offsetMin = new Vector2(1.5f, 1.5f);
            deckInnerRect.offsetMax = new Vector2(-1.5f, -1.5f);
            deckInner.AddComponent<Image>().color = ToastThemePalette.Hex("#F7F2E8");

            // Deck Top Row
            var deckTitleGo = CreateUI("DeckTitle", deckRect);
            var deckTitleRect = deckTitleGo.GetComponent<RectTransform>();
            deckTitleRect.anchorMin = new Vector2(0f, 1f);
            deckTitleRect.anchorMax = new Vector2(0f, 1f);
            deckTitleRect.pivot = new Vector2(0f, 1f);
            deckTitleRect.anchoredPosition = new Vector2(20f, -12f);
            deckTitleRect.sizeDelta = new Vector2(620f, 28f);
            CreateTmpOn(deckTitleGo, font, "인게임 알림 이벤트 호출 (버전 전환 시 현재 알림 그대로 유지)", 14f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            Button dismissTopBtn = CreateUtilityButton("BtnDismissTop", deckRect, font, "맨 위 알림 닫기", new Vector2(-432f, -10f), new Vector2(145f, 32f), ToastThemePalette.Hex("#FBEAE9"), ToastThemePalette.Hex("#A83B37"));
            Button burstBtn = CreateUtilityButton("BtnBurstDemo", deckRect, font, "다중 알림 테스트 (3개 연속)", new Vector2(-192f, -10f), new Vector2(230f, 32f), ToastThemePalette.Hex("#E8EFF5"), ToastThemePalette.Hex("#395B78"));
            Button clearBtn = CreateUtilityButton("BtnClearAll", deckRect, font, "전체 초기화 (Clear)", new Vector2(-18f, -10f), new Vector2(164f, 32f), ToastThemePalette.Hex("#EFE7D8"), ToastThemePalette.Hex("#3B3026"));

            // Trigger Grid (3x2)
            ToastPresetKey[] triggerKeys =
            {
                ToastPresetKey.ZoneA,
                ToastPresetKey.DockFull,
                ToastPresetKey.StaffStress,
                ToastPresetKey.ZoneBWarn,
                ToastPresetKey.VipOrder,
                ToastPresetKey.TruckDone
            };

            var triggerBtns = new List<Button>();
            var triggerIcons = new List<Image>();

            var triggerGridGo = CreateUI("TriggerGrid", deckRect);
            var triggerGridRect = triggerGridGo.GetComponent<RectTransform>();
            triggerGridRect.anchorMin = new Vector2(0f, 1f);
            triggerGridRect.anchorMax = new Vector2(1f, 1f);
            triggerGridRect.pivot = new Vector2(0.5f, 1f);
            triggerGridRect.offsetMin = new Vector2(20f, -148f);
            triggerGridRect.offsetMax = new Vector2(-20f, -50f);

            var grid = triggerGridGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(600f, 44f);
            grid.spacing = new Vector2(16f, 10f);

            for (int i = 0; i < triggerKeys.Length; i++)
            {
                ToastPresetData p = ToastPresetCatalog.Get(triggerKeys[i]);
                Button tb = CreateTriggerButton(triggerGridRect, font, p, out Image iconImg);
                triggerBtns.Add(tb);
                triggerIcons.Add(iconImg);
            }

            // Version Option Bar
            var optionBarGo = CreateUI("ModeOptionsBar", deckRect);
            var optionBarRect = optionBarGo.GetComponent<RectTransform>();
            optionBarRect.anchorMin = new Vector2(0f, 0f);
            optionBarRect.anchorMax = new Vector2(1f, 0f);
            optionBarRect.pivot = new Vector2(0.5f, 0f);
            optionBarRect.offsetMin = new Vector2(20f, 10f);
            optionBarRect.offsetMax = new Vector2(-20f, 52f);

            var v1OptGo = CreateUI("V1OptionsGroup", optionBarRect);
            StretchFull(v1OptGo.GetComponent<RectTransform>());
            Button v1ToggleAllBtn = CreateUtilityButton("BtnToggleAllV1", v1OptGo.transform, font, "전체 펼치기 / 접기", new Vector2(0f, 0f), new Vector2(160f, 34f), ToastThemePalette.Hex("#EFE7D8"), ToastThemePalette.Hex("#3B3026"), leftAligned: true);

            Slider pulseSlider = CreatePulseSlider(v1OptGo.transform, font, new Vector2(174f, 0f), out TextMeshProUGUI pulseLabel);
            Toggle v1AutoToggle = CreateOptionToggle("ToggleV1Auto", v1OptGo.transform, font, "펼침 후 4초 자동 복귀", new Vector2(460f, 0f), new Vector2(210f, 34f), true);

            var v2OptGo = CreateUI("V2OptionsGroup", optionBarRect);
            StretchFull(v2OptGo.GetComponent<RectTransform>());
            Toggle v2AutoToggle = CreateOptionToggle("ToggleV2Auto", v2OptGo.transform, font, "자동 소멸 ON/OFF (4초)", new Vector2(0f, 0f), new Vector2(220f, 34f), true);
            v2OptGo.SetActive(false);

            var hintGo = CreateUI("HintText", optionBarRect);
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1f, 0.5f);
            hintRect.anchorMax = new Vector2(1f, 0.5f);
            hintRect.pivot = new Vector2(1f, 0.5f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(680f, 32f);
            TextMeshProUGUI hintTmp = CreateTmpOn(hintGo, font, "원형 아이콘 클릭 시 가로 캡슐로 확장 / 윗 알림 종료 시 아래 알림이 부드럽게 위로 이동", 12.5f, FontStyles.Normal, ToastThemePalette.Hex("#7A6B5D"), TextAlignmentOptions.MidlineRight);

            // Bind SerializedFields on ToastDemoController
            var so = new SerializedObject(controller);
            so.FindProperty("_fontAsset").objectReferenceValue = font;
            so.FindProperty("_ver1SwitchBtn").objectReferenceValue = v1Btn;
            so.FindProperty("_ver2SwitchBtn").objectReferenceValue = v2Btn;
            so.FindProperty("_ver1SwitchBg").objectReferenceValue = v1Bg;
            so.FindProperty("_ver2SwitchBg").objectReferenceValue = v2Bg;
            so.FindProperty("_ver1SwitchLabel").objectReferenceValue = v1Label;
            so.FindProperty("_ver2SwitchLabel").objectReferenceValue = v2Label;
            so.FindProperty("_modeIndicatorText").objectReferenceValue = modeIndicatorText;
            so.FindProperty("_goldText").objectReferenceValue = goldText;
            so.FindProperty("_hudGoldIcon").objectReferenceValue = hudGoldIcon;
            so.FindProperty("_hudTruckIcon").objectReferenceValue = hudTruckIcon;
            so.FindProperty("_stageBackgroundBtn").objectReferenceValue = stageBtn;
            so.FindProperty("_toastOverlayLayer").objectReferenceValue = toastLayerRect;

            SetListProperty(so.FindProperty("_zoneCards"), zoneCards);
            SetListProperty(so.FindProperty("_zoneIconImages"), zoneIcons);
            var zTypesProp = so.FindProperty("_zoneIconTypes");
            zTypesProp.arraySize = zoneIconTypes.Count;
            for (int i = 0; i < zoneIconTypes.Count; i++)
            {
                zTypesProp.GetArrayElementAtIndex(i).enumValueIndex = (int)zoneIconTypes[i];
            }

            so.FindProperty("_dismissTopBtn").objectReferenceValue = dismissTopBtn;
            so.FindProperty("_burstDemoBtn").objectReferenceValue = burstBtn;
            so.FindProperty("_clearAllBtn").objectReferenceValue = clearBtn;

            SetListProperty(so.FindProperty("_triggerButtons"), triggerBtns);
            SetListProperty(so.FindProperty("_triggerIcons"), triggerIcons);
            var tKeysProp = so.FindProperty("_triggerPresetKeys");
            tKeysProp.arraySize = triggerKeys.Length;
            for (int i = 0; i < triggerKeys.Length; i++)
            {
                tKeysProp.GetArrayElementAtIndex(i).enumValueIndex = (int)triggerKeys[i];
            }

            so.FindProperty("_v1OptionsGroup").objectReferenceValue = v1OptGo;
            so.FindProperty("_v2OptionsGroup").objectReferenceValue = v2OptGo;
            so.FindProperty("_v1ToggleAllBtn").objectReferenceValue = v1ToggleAllBtn;
            so.FindProperty("_v1PulseSlider").objectReferenceValue = pulseSlider;
            so.FindProperty("_v1PulseValueLabel").objectReferenceValue = pulseLabel;
            so.FindProperty("_v1AutoCollapseToggle").objectReferenceValue = v1AutoToggle;
            so.FindProperty("_v2AutoDismissToggle").objectReferenceValue = v2AutoToggle;
            so.FindProperty("_hintText").objectReferenceValue = hintTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[ToastSceneBuilder] Rebuilt and saved warm beige " + ScenePath);
        }

        private static void SetListProperty<T>(SerializedProperty prop, List<T> items) where T : UnityEngine.Object
        {
            prop.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void EnsureEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null) go.AddComponent(inputModule);
            else go.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateTmp(RectTransform parent, TMP_FontAsset font, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = CreateUI("Text", parent);
            return CreateTmpOn(go, font, text, size, style, color, align);
        }

        private static TextMeshProUGUI CreateTmpOn(GameObject go, TMP_FontAsset font, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            if (font != null && font.material != null)
            {
                tmp.fontSharedMaterial = font.material;
            }
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;

            var tmpSo = new SerializedObject(tmp);
            tmpSo.FindProperty("m_fontAsset").objectReferenceValue = font;
            if (font != null && font.material != null)
            {
                tmpSo.FindProperty("m_sharedMaterial").objectReferenceValue = font.material;
            }
            tmpSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        private static Button CreateSwitchButton(string name, RectTransform parent, TMP_FontAsset font, string label, Vector2 pos, Vector2 size, bool active, out Image bg, out TextMeshProUGUI tmp)
        {
            var go = CreateUI(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            bg = go.AddComponent<Image>();
            bg.color = active ? ToastThemePalette.Hex("#C9534F") : new Color(0f, 0f, 0f, 0f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            tmp = CreateTmp(rect, font, label, 13.5f, FontStyles.Bold, active ? Color.white : ToastThemePalette.Hex("#7A6B5D"), TextAlignmentOptions.Center);
            StretchFull(tmp.rectTransform);
            return btn;
        }

        private static GameObject CreateHudChip(string name, RectTransform parent, Vector2 pos, Vector2 size, bool anchorLeft)
        {
            var go = CreateUI(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorLeft ? 0f : 1f, 0.5f);
            rect.anchorMax = new Vector2(anchorLeft ? 0f : 1f, 0.5f);
            rect.pivot = new Vector2(anchorLeft ? 0f : 1f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = ToastThemePalette.Hex("#D6C9B4");

            var inner = CreateUI("Inner", rect);
            var innerRect = inner.GetComponent<RectTransform>();
            StretchFull(innerRect);
            innerRect.offsetMin = new Vector2(1f, 1f);
            innerRect.offsetMax = new Vector2(-1f, -1f);
            inner.AddComponent<Image>().color = ToastThemePalette.Hex("#FFFDF8");
            return go;
        }

        private static Image CreateHudIcon(RectTransform parent, Color tint)
        {
            var go = CreateUI("Icon", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            rect.sizeDelta = new Vector2(20f, 20f);
            var img = go.AddComponent<Image>();
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        private static WarehouseZoneCard CreateZoneCard(
            RectTransform parent,
            TMP_FontAsset font,
            string zoneId,
            string title,
            string statusTag,
            Color tagColor,
            string footerLeft,
            string footerRight,
            float fillRatio,
            Color fillColor,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Image iconImg)
        {
            var go = CreateUI("Zone_" + zoneId, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = ToastThemePalette.Hex("#D6C9B4");

            var pulseGo = CreateUI("BorderPulse", rect);
            StretchFull(pulseGo.GetComponent<RectTransform>());
            var pulseImg = pulseGo.AddComponent<Image>();
            pulseImg.color = new Color(1f, 1f, 1f, 0f);
            pulseImg.raycastTarget = false;

            var innerGo = CreateUI("Inner", rect);
            var innerRect = innerGo.GetComponent<RectTransform>();
            StretchFull(innerRect);
            innerRect.offsetMin = new Vector2(2.5f, 2.5f);
            innerRect.offsetMax = new Vector2(-2.5f, -2.5f);
            innerGo.AddComponent<Image>().color = ToastThemePalette.Hex("#FFFDF8");

            var iconGo = CreateUI("ZoneIcon", innerRect);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(14f, -14f);
            iconRect.sizeDelta = new Vector2(22f, 22f);
            iconImg = iconGo.AddComponent<Image>();
            iconImg.color = fillColor;
            iconImg.raycastTarget = false;

            var titleGo = CreateUI("Title", innerRect);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(44f, -38f);
            titleRect.offsetMax = new Vector2(-100f, -12f);
            CreateTmpOn(titleGo, font, title, 16f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            var tagGo = CreateUI("StatusTag", innerRect);
            var tagRect = tagGo.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(1f, 1f);
            tagRect.anchorMax = new Vector2(1f, 1f);
            tagRect.pivot = new Vector2(1f, 1f);
            tagRect.anchoredPosition = new Vector2(-12f, -12f);
            tagRect.sizeDelta = new Vector2(96f, 24f);
            tagGo.AddComponent<Image>().color = new Color(tagColor.r, tagColor.g, tagColor.b, 0.16f);
            var tagTmp = CreateTmp(tagRect, font, statusTag, 11.5f, FontStyles.Bold, tagColor, TextAlignmentOptions.Center);
            StretchFull(tagTmp.rectTransform);

            var meterGo = CreateUI("MeterTrack", innerRect);
            var meterRect = meterGo.GetComponent<RectTransform>();
            meterRect.anchorMin = new Vector2(0f, 0.45f);
            meterRect.anchorMax = new Vector2(1f, 0.45f);
            meterRect.pivot = new Vector2(0.5f, 0.5f);
            meterRect.offsetMin = new Vector2(14f, -6f);
            meterRect.offsetMax = new Vector2(-14f, 6f);
            meterGo.AddComponent<Image>().color = ToastThemePalette.Hex("#EFE7D8");

            var fillGo = CreateUI("MeterFill", meterRect);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(fillRatio), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillGo.AddComponent<Image>().color = fillColor;

            var fLeftGo = CreateUI("FooterLeft", innerRect);
            var fLeftRect = fLeftGo.GetComponent<RectTransform>();
            fLeftRect.anchorMin = new Vector2(0f, 0f);
            fLeftRect.anchorMax = new Vector2(0.55f, 0f);
            fLeftRect.offsetMin = new Vector2(14f, 10f);
            fLeftRect.offsetMax = new Vector2(0f, 34f);
            CreateTmpOn(fLeftGo, font, footerLeft, 13f, FontStyles.Normal, ToastThemePalette.Hex("#7A6B5D"), TextAlignmentOptions.MidlineLeft);

            var fRightGo = CreateUI("FooterRight", innerRect);
            var fRightRect = fRightGo.GetComponent<RectTransform>();
            fRightRect.anchorMin = new Vector2(0.45f, 0f);
            fRightRect.anchorMax = new Vector2(1f, 0f);
            fRightRect.offsetMin = new Vector2(0f, 10f);
            fRightRect.offsetMax = new Vector2(-14f, 34f);
            CreateTmpOn(fRightGo, font, footerRight, 13f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineRight);

            var card = go.AddComponent<WarehouseZoneCard>();
            card.Initialize(zoneId, pulseImg, rect);
            return card;
        }

        private static Button CreateUtilityButton(string name, Transform parent, TMP_FontAsset font, string label, Vector2 pos, Vector2 size, Color bgCol, Color textCol, bool leftAligned = false)
        {
            var go = CreateUI(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(leftAligned ? 0f : 1f, leftAligned ? 0.5f : 1f);
            rect.anchorMax = new Vector2(leftAligned ? 0f : 1f, leftAligned ? 0.5f : 1f);
            rect.pivot = new Vector2(leftAligned ? 0f : 1f, leftAligned ? 0.5f : 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var bg = go.AddComponent<Image>();
            bg.color = bgCol;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            var tmp = CreateTmp(rect, font, label, 12.5f, FontStyles.Bold, textCol, TextAlignmentOptions.Center);
            StretchFull(tmp.rectTransform);
            return btn;
        }

        private static Button CreateTriggerButton(RectTransform parent, TMP_FontAsset font, ToastPresetData preset, out Image iconImg)
        {
            ToastThemePalette palette = ToastThemePalette.Get(preset.Tone);
            var go = CreateUI("Trigger_" + preset.Key, parent);
            var rect = go.GetComponent<RectTransform>();

            var borderImg = go.AddComponent<Image>();
            borderImg.color = ToastThemePalette.Hex("#D6C9B4");

            var inner = CreateUI("Inner", rect);
            var innerRect = inner.GetComponent<RectTransform>();
            StretchFull(innerRect);
            innerRect.offsetMin = new Vector2(1f, 1f);
            innerRect.offsetMax = new Vector2(-1f, -1f);
            var bg = inner.AddComponent<Image>();
            bg.color = ToastThemePalette.Hex("#FFFDF8");

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            var barGo = CreateUI("AccentBar", rect);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.sizeDelta = new Vector2(5f, 0f);
            barGo.AddComponent<Image>().color = palette.PillBackground;

            var iconGo = CreateUI("Icon", rect);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            iconRect.sizeDelta = new Vector2(20f, 20f);
            iconImg = iconGo.AddComponent<Image>();
            iconImg.color = palette.PillBackground;
            iconImg.raycastTarget = false;

            var titleGo = CreateUI("Title", rect);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(44f, 0f);
            titleRect.offsetMax = new Vector2(-68f, 0f);
            CreateTmpOn(titleGo, font, preset.Title, 13.5f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            var tagGo = CreateUI("Tag", rect);
            var tagRect = tagGo.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(1f, 0.5f);
            tagRect.anchorMax = new Vector2(1f, 0.5f);
            tagRect.pivot = new Vector2(1f, 0.5f);
            tagRect.anchoredPosition = new Vector2(-12f, 0f);
            tagRect.sizeDelta = new Vector2(46f, 22f);
            tagGo.AddComponent<Image>().color = new Color(palette.PillBackground.r, palette.PillBackground.g, palette.PillBackground.b, 0.14f);
            var tagTmp = CreateTmp(tagRect, font, preset.TagLabel, 11f, FontStyles.Bold, palette.PillBackground, TextAlignmentOptions.Center);
            StretchFull(tagTmp.rectTransform);

            return btn;
        }

        private static Slider CreatePulseSlider(Transform parent, TMP_FontAsset font, Vector2 pos, out TextMeshProUGUI valueLabel)
        {
            var container = CreateUI("PulseSliderChip", parent);
            var cRect = container.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0.5f);
            cRect.anchorMax = new Vector2(0f, 0.5f);
            cRect.pivot = new Vector2(0f, 0.5f);
            cRect.anchoredPosition = pos;
            cRect.sizeDelta = new Vector2(272f, 34f);
            container.AddComponent<Image>().color = ToastThemePalette.Hex("#EFE7D8");

            var lblGo = CreateUI("Label", cRect);
            var lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0.5f);
            lblRect.anchorMax = new Vector2(0f, 0.5f);
            lblRect.pivot = new Vector2(0f, 0.5f);
            lblRect.anchoredPosition = new Vector2(12f, 0f);
            lblRect.sizeDelta = new Vector2(72f, 24f);
            CreateTmpOn(lblGo, font, "발광 속도", 12.5f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            var sliderGo = CreateUI("Slider", cRect);
            var sRect = sliderGo.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0.5f);
            sRect.anchorMax = new Vector2(0f, 0.5f);
            sRect.pivot = new Vector2(0f, 0.5f);
            sRect.anchoredPosition = new Vector2(88f, 0f);
            sRect.sizeDelta = new Vector2(124f, 20f);

            var bgGo = CreateUI("Background", sRect);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.AddComponent<Image>().color = ToastThemePalette.Hex("#D6C9B4");

            var fillArea = CreateUI("FillArea", sRect);
            StretchFull(fillArea.GetComponent<RectTransform>());
            var fillGo = CreateUI("Fill", fillArea.transform);
            StretchFull(fillGo.GetComponent<RectTransform>());
            fillGo.AddComponent<Image>().color = ToastThemePalette.Hex("#C9534F");

            var handleArea = CreateUI("HandleArea", sRect);
            StretchFull(handleArea.GetComponent<RectTransform>());
            var handleGo = CreateUI("Handle", handleArea.transform);
            var hRect = handleGo.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(16f, 20f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = ToastThemePalette.Hex("#3B3026");

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillGo.GetComponent<RectTransform>();
            slider.handleRect = hRect;
            slider.targetGraphic = handleImg;
            slider.minValue = 0.9f;
            slider.maxValue = 3.6f;
            slider.value = 2.1f;

            var valGo = CreateUI("ValueLabel", cRect);
            var vRect = valGo.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(1f, 0.5f);
            vRect.anchorMax = new Vector2(1f, 0.5f);
            vRect.pivot = new Vector2(1f, 0.5f);
            vRect.anchoredPosition = new Vector2(-10f, 0f);
            vRect.sizeDelta = new Vector2(44f, 24f);
            valueLabel = CreateTmpOn(valGo, font, "2.1s", 12.5f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineRight);

            return slider;
        }

        private static Toggle CreateOptionToggle(string name, Transform parent, TMP_FontAsset font, string label, Vector2 pos, Vector2 size, bool defaultOn)
        {
            var container = CreateUI(name, parent);
            var cRect = container.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0.5f);
            cRect.anchorMax = new Vector2(0f, 0.5f);
            cRect.pivot = new Vector2(0f, 0.5f);
            cRect.anchoredPosition = pos;
            cRect.sizeDelta = size;
            container.AddComponent<Image>().color = ToastThemePalette.Hex("#EFE7D8");

            var boxGo = CreateUI("Box", cRect);
            var boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(10f, 0f);
            boxRect.sizeDelta = new Vector2(18f, 18f);
            var boxBg = boxGo.AddComponent<Image>();
            boxBg.color = ToastThemePalette.Hex("#D6C9B4");

            var checkGo = CreateUI("Checkmark", boxRect);
            var checkRect = checkGo.GetComponent<RectTransform>();
            StretchFull(checkRect);
            checkRect.offsetMin = new Vector2(3f, 3f);
            checkRect.offsetMax = new Vector2(-3f, -3f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = ToastThemePalette.Hex("#4C7599");

            var lblGo = CreateUI("Label", cRect);
            var lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = new Vector2(36f, 0f);
            lblRect.offsetMax = new Vector2(-8f, 0f);
            CreateTmpOn(lblGo, font, label, 12.5f, FontStyles.Bold, ToastThemePalette.Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            var toggle = container.AddComponent<Toggle>();
            toggle.targetGraphic = boxBg;
            toggle.graphic = checkImg;
            toggle.isOn = defaultOn;
            return toggle;
        }
    }
}
