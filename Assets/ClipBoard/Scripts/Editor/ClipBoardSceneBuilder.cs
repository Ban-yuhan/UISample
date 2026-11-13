using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static LogisticsTycoon.ClipBoard.ClipUI;

namespace LogisticsTycoon.ClipBoard.Editor
{
    /// <summary>
    /// office_clipboard_ui.html 을 uGUI + DOTween 씬으로 구축.
    /// 메뉴: Tools/UI Showcase/Setup ClipBoardScene
    /// </summary>
    public static class ClipBoardSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ClipBoardScene.unity";
        private const string SpriteDir = "Assets/ClipBoard/Sprites";
        private const string FontPath = "Assets/SettlementReceipt/Fonts/MALGUN SDF.asset";
        private static readonly string[] StampTextures = { "Assets/Resources/Stamp_Front.png", "Assets/Resources/Stamp_Top.png" };

        private static ClipTheme theme;
        private static Sprite ringThin;

        [MenuItem("Tools/UI Showcase/Setup ClipBoardScene")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[ClipBoardSceneBuilder] TMP 폰트 에셋을 찾을 수 없습니다: " + FontPath);
                return;
            }
            EnsureStampSprites();
            CreateSprites(font);
            Use(theme);

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "ClipBoardCanvas" || root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    Object.DestroyImmediate(root);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Hex("#261a11");
            }

            EnsureEventSystem();
            BuildCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("<color=#10B981>[ClipBoardSceneBuilder]</color> 완료: " + ScenePath);
        }

        // =====================================================================================
        // Canvas
        // =====================================================================================

        private static void BuildCanvas()
        {
            var canvasGo = new GameObject("ClipBoardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            var ctrl = canvasGo.AddComponent<ClipboardController>();
            ctrl.theme = theme;

            // ---------- 배경: 나무 책상 ----------
            var bg = Img("WoodDesk", root, null, Hex("#261a11"));
            Stretch(bg.rectTransform);
            var spot = Img("Spotlight", bg.transform, theme.glow, Hex("#3b2a1e"));
            Place(spot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(2400, 1700));
            var grain = Img("WoodGrain", bg.transform, theme.dashV, new Color(1, 1, 1, 0.02f));
            grain.sprite = GrainSprite;
            grain.type = Image.Type.Tiled;
            Stretch(grain.rectTransform);

            BuildHeader(root, ctrl);
            BuildClipboard(root, ctrl);
            BuildSignModal(root, ctrl);

            // ---------- 토스트 ----------
            var toastBg = Card("ResetToast", root, Hex("#2a1a10", 0.95f), Hex("#fbbf24", 0.6f), 22, 2f, out var toastOuter);
            Place(toastOuter.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -118), new Vector2(620, 46));
            ctrl.toast = toastOuter.gameObject.AddComponent<CanvasGroup>();
            ctrl.toast.blocksRaycasts = false;
            ctrl.toastText = Txt("Text", toastBg.transform, "", 17, Hex("#fef3c7"), TextAlignmentOptions.Center, true);
            Stretch(ctrl.toastText.rectTransform);

            // ---------- 드래그 레이어 / FX 레이어 ----------
            var drag = RT("DragLayer", root);
            Stretch(drag);
            ctrl.stampTool.dragLayer = drag;

            var fxLayer = RT("FxLayer", root);
            Stretch(fxLayer);
            ctrl.fx = fxLayer.gameObject.AddComponent<ClipFx>();
            ctrl.fx.layer = fxLayer;
        }

        // =====================================================================================
        // HUD Header
        // =====================================================================================

        private static void BuildHeader(Transform root, ClipboardController ctrl)
        {
            var bar = Card("HeaderHUD", root, Hex("#2a1b12", 0.94f), Hex("#8a5d3b", 0.6f), 18, 2f, out var outer);
            Place(outer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(1400, 84), new Vector2(0.5f, 1f));
            var t = bar.transform;

            // 좌측: 레벨 + EXP
            var lv = Card("Level", t, Hex("#3d2718"), Hex("#f59e0b", 0.4f), 12, 1.5f, out var lvo);
            MidLeft(lvo.rectTransform, 18, 0, 116, 48);
            var lvt = Txt("Text", lv.transform, "<color=#fbbf24>箱</color> LV.4", 19, Hex("#fde68a"), TextAlignmentOptions.Center, true);
            Stretch(lvt.rectTransform);
            var el = Txt("ExpLabel", t, "EXP", 13, Hex("#fde68a", 0.8f), TextAlignmentOptions.Left);
            MidLeft(el.rectTransform, 148, 12, 190, 18);
            var ev = Txt("ExpValue", t, "1,240 / 2,000", 13, Hex("#fde68a", 0.8f), TextAlignmentOptions.Right);
            MidLeft(ev.rectTransform, 148, 12, 190, 18);
            var track = Img("ExpTrack", t, theme.round, Hex("#170f0a"), 5);
            MidLeft(track.rectTransform, 148, -10, 190, 10);
            var fill = Img("ExpFill", track.transform, theme.round, Color.white, 5);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            var g = fill.gameObject.AddComponent<ClipGradient>();
            g.top = Hex("#fde047");
            g.bottom = Hex("#f59e0b");

            // 중앙: 보유 재화 + 돼지 저금통
            var goldBox = Card("GoldBox", t, Hex("#170f0a"), Hex("#d97706", 0.45f), 14, 1.5f, out var gbo);
            Place(gbo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150, 0), new Vector2(380, 68));
            var coin = Img("CoinIcon", goldBox.transform, theme.circle, Hex("#10b981"));
            MidLeft(coin.rectTransform, 16, 0, 34, 34);
            var ct = Txt("G", coin.transform, "G", 18, Hex("#052e16"), TextAlignmentOptions.Center, true);
            Stretch(ct.rectTransform);
            ctrl.goldText = Txt("GoldText", goldBox.transform, "124,500G", 32, Hex("#fcd34d"), TextAlignmentOptions.Left, true);
            MidLeft(ctrl.goldText.rectTransform, 60, 0, 210, 44);
            ctrl.goldText.rectTransform.pivot = new Vector2(0.2f, 0.5f);
            ctrl.goldText.rectTransform.anchoredPosition = new Vector2(60 + 42, 0);
            ctrl.piggy = BuildPiggy(goldBox.transform);

            // 평판 별점
            var rep = Card("Reputation", t, Hex("#170f0a"), Hex("#d97706", 0.3f), 14, 1.5f, out var ro);
            Place(ro.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(160, 0), new Vector2(214, 50));
            var rl = Txt("Label", rep.transform, "평판", 14, Hex("#a8a29e"), TextAlignmentOptions.Left);
            MidLeft(rl.rectTransform, 14, 0, 40, 22);
            ctrl.stars = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var s = Img("Star" + (i + 1), rep.transform, theme.star, Hex("#57534e"));
                MidLeft(s.rectTransform, 56 + i * 29, 0, 25, 25);
                s.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                s.rectTransform.anchoredPosition = new Vector2(56 + i * 29 + 12.5f, 0);
                ctrl.stars[i] = s;
            }

            // 우측: 일일 정산 / 시연 초기화
            ctrl.settleButton = Btn("SettleButton", t, "일일 정산", 18, Hex("#059669"), Hex("#6ee7b7", 0.8f), Color.white, 14, out _);
            MidRight((RectTransform)ctrl.settleButton.transform, 204, 0, 150, 50);
            CenterPivot((RectTransform)ctrl.settleButton.transform);

            ctrl.resetButton = Btn("ResetButton", t, "      시연 초기화", 18, Hex("#d97706"), Hex("#fcd34d", 0.8f), Hex("#231206"), 14, out _);
            MidRight((RectTransform)ctrl.resetButton.transform, 18, 0, 176, 50);
            CenterPivot((RectTransform)ctrl.resetButton.transform);
            var icon = Img("ResetIcon", ctrl.resetButton.transform, ringThin, Hex("#231206"));
            icon.type = Image.Type.Filled;
            icon.fillMethod = Image.FillMethod.Radial360;
            icon.fillAmount = 0.8f;
            icon.fillOrigin = (int)Image.Origin360.Top;
            MidLeft(icon.rectTransform, 18, 0, 22, 22);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(29, 0);
            var head = Img("ArrowHead", icon.transform, null, Hex("#231206"));
            Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(-3, -2), new Vector2(7, 7));
            head.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            ctrl.resetIcon = icon.rectTransform;
        }

        private static void CenterPivot(RectTransform rt)
        {
            Vector2 size = rt.sizeDelta;
            Vector2 oldPivot = rt.pivot;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition += new Vector2((0.5f - oldPivot.x) * size.x, (0.5f - oldPivot.y) * size.y);
        }

        /// <summary>반투명 돼지 저금통 (HTML SVG viewBox 36x30 을 2.6배로 옮김)</summary>
        private static PiggyBankGauge BuildPiggy(Transform parent)
        {
            const float s = 2.6f;
            Vector2 M(float x, float y) => new Vector2((x - 18f) * s, (15f - y) * s);
            Color pink = Hex("#f472b6"), pinkLight = Hex("#f9a8d4");

            var pig = RT("PiggyBank", parent);
            MidRight(pig, 10, 0, 96, 80);
            CenterPivot(pig);

            Image P(string name, Sprite sp, Color c, Vector2 pos, Vector2 size, Transform p = null)
            {
                var img = Img(name, p ?? pig, sp, c);
                Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
                return img;
            }

            var ear = P("Ear", theme.round, WithA(pink, 0.65f), M(13f, 6.5f), new Vector2(12, 12));
            ear.type = Image.Type.Sliced;
            ear.pixelsPerUnitMultiplier = RoundRadius / 3f;
            ear.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var legL = P("LegL", theme.round, WithA(pink, 0.55f), M(12.5f, 25f), new Vector2(8, 11));
            legL.type = Image.Type.Sliced; legL.pixelsPerUnitMultiplier = RoundRadius / 3f;
            var legR = P("LegR", theme.round, WithA(pink, 0.55f), M(22.5f, 25f), new Vector2(8, 11));
            legR.type = Image.Type.Sliced; legR.pixelsPerUnitMultiplier = RoundRadius / 3f;

            Vector2 bodySize = new Vector2(24f * s, 19f * s);
            P("Body", theme.circle, new Color(1f, 0.894f, 0.902f, 0.16f), M(18f, 16f), bodySize);

            // 타원 마스크 안에서 바닥부터 차오르는 금화
            var mask = P("CoinMask", theme.circle, Color.white, M(18f, 16f), bodySize);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var fillRt = RT("CoinFill", mask.transform);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(0f, bodySize.y * 0.5f);
            fillRt.gameObject.AddComponent<RectMask2D>();
            var gold = Img("Gold", fillRt, null, Hex("#facc15", 0.85f));
            Stretch(gold.rectTransform);
            float bottomY = 16f + 9.5f;
            foreach (var c in new[] { new Vector2(13, 19), new Vector2(19, 21), new Vector2(23, 18), new Vector2(15.5f, 14.5f), new Vector2(21, 12), new Vector2(17, 9.5f) })
            {
                var coinImg = Img("Coin", fillRt, theme.circle, Hex("#fef08a"));
                Place(coinImg.rectTransform, new Vector2(0.5f, 0f), new Vector2((c.x - 18f) * s, (bottomY - c.y) * s), new Vector2(11, 11));
                var inner = Img("In", coinImg.transform, theme.circle, Hex("#eab308"));
                Stretch(inner.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
            }
            var surface = Img("Surface", fillRt, theme.round, Hex("#fef9c3"), 2);
            surface.rectTransform.anchorMin = new Vector2(0f, 1f);
            surface.rectTransform.anchorMax = new Vector2(1f, 1f);
            surface.rectTransform.pivot = new Vector2(0.5f, 1f);
            surface.rectTransform.anchoredPosition = Vector2.zero;
            surface.rectTransform.sizeDelta = new Vector2(0f, 3f);

            P("BodyOutline", ringThin, pinkLight, M(18f, 16f), bodySize + new Vector2(3, 3));
            // 코 & 눈
            P("Snout", theme.circle, WithA(pink, 0.5f), M(5.5f, 16f), new Vector2(4.4f * s, 6.4f * s));
            P("SnoutOutline", ringThin, pinkLight, M(5.5f, 16f), new Vector2(4.4f * s + 2, 6.4f * s + 2));
            P("Eye", theme.circle, Hex("#fce7f3"), M(10.5f, 13.5f), new Vector2(6, 6));
            var slot = P("Slot", theme.round, Hex("#fde047"), M(18f, 7.2f), new Vector2(6f * s, 4f));
            slot.type = Image.Type.Sliced;
            slot.pixelsPerUnitMultiplier = RoundRadius / 2f;

            var gauge = pig.gameObject.AddComponent<PiggyBankGauge>();
            gauge.pig = pig;
            gauge.fillRect = fillRt;
            gauge.surface = surface.rectTransform;
            gauge.fillMaxHeight = bodySize.y;
            gauge.slot = slot.rectTransform;
            gauge.coinSprite = theme.circle;
            gauge.coinDropPrefabParent = pig;
            return gauge;
        }

        // =====================================================================================
        // Clipboard
        // =====================================================================================

        private static void BuildClipboard(Transform root, ClipboardController ctrl)
        {
            var wrapper = RT("ClipboardWrapper", root);
            Place(wrapper, new Vector2(0.5f, 0.5f), new Vector2(-70, -82), new Vector2(1060, 840));
            ctrl.boardRoot = wrapper;

            // 그림자 + 하드보드
            var shadow = Img("BoardShadow", wrapper, theme.glow, new Color(0, 0, 0, 0.75f));
            Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(1320, 1100));
            var board = Card("Board", wrapper, Color.white, Hex("#492a14"), 28, 3f, out var boardOuter);
            Stretch(boardOuter.rectTransform);
            var bg = board.gameObject.AddComponent<ClipGradient>();
            bg.top = Hex("#875634");
            bg.bottom = Hex("#523018");
            var hl = Img("TopHighlight", board.transform, theme.round, Hex("#ffe0be", 0.18f), 26);
            Stretch(hl.rectTransform, 0, 0, 0, 0);
            hl.rectTransform.anchorMin = new Vector2(0, 1);
            hl.rectTransform.offsetMin = new Vector2(0, -6);

            // 종이 더미
            var stack = RT("PaperStack", wrapper);
            Stretch(stack, 24, 66, 24, 24);

            var back2 = PaperSheet("PaperBack2", stack, Hex("#e3d3b6"), Hex("#cbb794"), out _);
            back2.anchoredPosition = new Vector2(-4, -5);
            back2.localRotation = Quaternion.Euler(0, 0, -1.3f);
            var back1 = PaperSheet("PaperBack1", stack, Hex("#efe3cb"), Hex("#d6c3a1"), out _);
            back1.anchoredPosition = new Vector2(3, -3);
            back1.localRotation = Quaternion.Euler(0, 0, 0.9f);
            ctrl.paperBack1 = back1;
            ctrl.paperBack2 = back2;

            var tabsBehind = RT("TabsBehind", stack);
            Stretch(tabsBehind);

            var front = PaperSheet("PaperFront", stack, Hex("#fbf6eb"), Hex("#dfd0b4"), out var frontFill);
            ctrl.paperFront = front;
            var ruled = Img("RuledLines", frontFill, theme.ruled, Hex("#8b6545", 0.06f));
            ruled.type = Image.Type.Tiled;
            Stretch(ruled.rectTransform, 2, 2, 2, 2);

            var tabsFront = RT("TabsFront", stack);
            Stretch(tabsFront);

            // 금속 클립 (보드 위)
            BuildMetalClip(wrapper);

            // 인덱스 테이프
            string[] labels = { "알바 고용", "알바 관리", "회사 계약", "업그레이드" };
            string[] bodies = { "#f4a261", "#e9c46a", "#52b788", "#e76f51" };
            string[] borders = { "#c8722d", "#bfa03a", "#348a63", "#b94b30" };
            string[] texts = { "#3a1d08", "#3e2a07", "#0b3321", "#3b1208" };
            ctrl.tabs = new IndexTab[4];
            for (int i = 0; i < 4; i++)
                ctrl.tabs[i] = BuildTab(i, tabsBehind, tabsFront, labels[i], Hex(bodies[i]), Hex(borders[i]), Hex(texts[i]), i == 1);

            BuildSheet(frontFill, ctrl);
        }

        /// <summary>그림자 + 종이를 묶은 컨테이너 (컨테이너를 회전/이동). sheetFill = 내용물을 넣을 종이 채움 영역</summary>
        private static RectTransform PaperSheet(string name, Transform parent, Color fill, Color border, out Transform sheetFill)
        {
            var container = RT(name, parent);
            Stretch(container);
            var drop = Img("Shadow", container, theme.round, new Color(0, 0, 0, 0.22f), 18);
            Stretch(drop.rectTransform, -2, 2, -2, -6);
            var f = Card("Sheet", container, fill, border, 18, 1.5f, out var outer);
            Stretch(outer.rectTransform);
            sheetFill = f.transform;
            return container;
        }

        private static void BuildMetalClip(Transform wrapper)
        {
            var clip = RT("MetalClip", wrapper);
            Place(clip, new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(300, 80));

            var ring = Card("Ring", clip, Hex("#2a1b10"), Hex("#fde68a", 0.7f), 14, 5f, out var ringOuter);
            Place(ringOuter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(56, 34));

            var plate = Card("Plate", clip, Color.white, Hex("#6e532e"), 10, 1.5f, out var po);
            Place(po.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(290, 46));
            var pg = plate.gameObject.AddComponent<ClipGradient>();
            pg.top = Hex("#d8c39e");
            pg.bottom = Hex("#8f7248");
            var ps = Img("PlateShadow", clip, theme.round, new Color(0, 0, 0, 0.35f), 10);
            Place(ps.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -11), new Vector2(294, 48));
            ps.transform.SetSiblingIndex(po.transform.GetSiblingIndex());

            var label = Txt("Label", plate.transform, "LOGISTICS CLIP", 15, Hex("#3e2912"), TextAlignmentOptions.Center, true);
            label.characterSpacing = 22f;
            Stretch(label.rectTransform);
            for (int i = 0; i < 2; i++)
            {
                var dot = Img(i == 0 ? "RivetL" : "RivetR", plate.transform, theme.circle, Hex("#4e381c"));
                Place(dot.rectTransform, new Vector2(i == 0 ? 0f : 1f, 0.5f), new Vector2(i == 0 ? 24 : -24, 0), new Vector2(12, 12));
            }
        }

        private static IndexTab BuildTab(int index, RectTransform behind, RectTransform front, string label, Color body, Color border, Color text, bool withBadge)
        {
            var go = RT("IndexTab_" + label, behind);
            const float baseX = -12f;
            go.anchorMin = go.anchorMax = new Vector2(1f, 1f);
            go.pivot = new Vector2(0f, 0.5f); // transform-origin: left center
            go.anchoredPosition = new Vector2(baseX, -(100 + index * 80));
            go.sizeDelta = new Vector2(150, 66);

            var sh = Img("Shadow", go, theme.round, new Color(0, 0, 0, 0.28f), 12);
            Stretch(sh.rectTransform, 2, 4, -4, -4);
            var outer = Img("Border", go, theme.round, border, 12);
            Stretch(outer.rectTransform);
            outer.raycastTarget = true;
            var inner = Img("Body", outer.transform, theme.round, body, 10);
            Stretch(inner.rectTransform, 2, 2, 2, 2);
            var crease = Img("Crease", outer.transform, null, Hex("#3e2715", 0.2f));
            crease.rectTransform.anchorMin = new Vector2(0, 0);
            crease.rectTransform.anchorMax = new Vector2(0, 1);
            crease.rectTransform.offsetMin = new Vector2(13, 3);
            crease.rectTransform.offsetMax = new Vector2(14.5f, -3);
            var creaseHl = Img("CreaseHighlight", outer.transform, null, new Color(1, 1, 1, 0.35f));
            creaseHl.rectTransform.anchorMin = new Vector2(0, 0);
            creaseHl.rectTransform.anchorMax = new Vector2(0, 1);
            creaseHl.rectTransform.offsetMin = new Vector2(14.5f, 3);
            creaseHl.rectTransform.offsetMax = new Vector2(15.5f, -3);
            var t = Txt("Label", outer.transform, label, 19, text, TextAlignmentOptions.Left, true);
            Stretch(t.rectTransform, 26, 0, 6, 0);

            var tab = go.gameObject.AddComponent<IndexTab>();
            tab.index = index;
            tab.body = inner;
            tab.border = outer;
            tab.label = t;
            tab.bodyColor = body;
            tab.borderColor = border;
            tab.frontLayer = front;
            tab.backLayer = behind;
            tab.baseX = baseX;

            if (withBadge)
            {
                var badge = Img("NewBadge", go, theme.circle, Hex("#fbf6eb"));
                Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(-6, -4), new Vector2(22, 22));
                var dot = Img("Dot", badge.transform, theme.circle, Hex("#dc2626"));
                Stretch(dot.rectTransform, 3, 3, 3, 3);
                badge.gameObject.SetActive(false);
                tab.badge = badge.gameObject;
            }
            return tab;
        }

        // =====================================================================================
        // Sheet (front paper content)
        // =====================================================================================

        private static void BuildSheet(Transform paper, ClipboardController ctrl)
        {
            var sheet = RT("Sheet", paper);
            Stretch(sheet, 28, 24, 30, 24);

            // 공통 헤더
            var badge = Img("GlyphBadge", sheet, theme.round, Hex("#e6d2b1"), 12);
            Place(badge.rectTransform, new Vector2(0, 1), new Vector2(27, -28), new Vector2(54, 54));
            ctrl.sheetGlyphBadge = badge.rectTransform;
            ctrl.sheetGlyph = Txt("Glyph", badge.transform, "印", 28, Hex("#57391e"), TextAlignmentOptions.Center, true);
            Stretch(ctrl.sheetGlyph.rectTransform);
            ctrl.sheetTitle = Txt("Title", sheet, "알바 이력서 심사", 30, Hex("#3f2814"), TextAlignmentOptions.TopLeft, true);
            TopLeft(ctrl.sheetTitle.rectTransform, 68, 0, 600, 38);
            ctrl.sheetDesc = Txt("Desc", sheet, "지원자 카드를 확인하고 고용 도장을 끌어서 찍으세요.", 16, Hex("#856447"), TextAlignmentOptions.TopLeft);
            TopLeft(ctrl.sheetDesc.rectTransform, 68, 38, 700, 22);
            var doc = Txt("DocNo", sheet, "DOC #LC-04", 16, Hex("#8c6747"), TextAlignmentOptions.Right, true);
            TopRight(doc.rectTransform, 52, 16, 160, 24);
            var chk = Img("CheckSeal", sheet, ringThin, Hex("#ef4444", 0.4f));
            Place(chk.rectTransform, new Vector2(1, 1), new Vector2(-20, -28), new Vector2(38, 38));
            chk.rectTransform.localRotation = Quaternion.Euler(0, 0, 12);
            var chkT = Txt("T", chk.transform, "확인", 11, Hex("#dc2626", 0.5f), TextAlignmentOptions.Center, true);
            Stretch(chkT.rectTransform);
            var dash = DashH("HeaderDash", sheet, Hex("#d8c7a6"));
            dash.rectTransform.anchorMin = new Vector2(0, 1);
            dash.rectTransform.anchorMax = new Vector2(1, 1);
            dash.rectTransform.pivot = new Vector2(0.5f, 1);
            dash.rectTransform.offsetMin = new Vector2(0, -72);
            dash.rectTransform.offsetMax = new Vector2(0, -70);

            var panelsRoot = RT("Panels", sheet);
            Stretch(panelsRoot, 0, 84, 0, 0);
            ctrl.panels = new CanvasGroup[4];
            ctrl.panels[0] = BuildHirePanel(panelsRoot, ctrl);
            ctrl.panels[1] = BuildStaffPanel(panelsRoot, ctrl);
            ctrl.panels[2] = BuildContractPanel(panelsRoot, ctrl);
            ctrl.panels[3] = BuildUpgradePanel(panelsRoot, ctrl);
            for (int i = 1; i < 4; i++) ctrl.panels[i].gameObject.SetActive(false);
        }

        private static CanvasGroup Panel(string name, Transform parent)
        {
            var rt = RT(name, parent);
            Stretch(rt);
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        private static CanvasGroup BuildHirePanel(Transform parent, ClipboardController ctrl)
        {
            var p = Panel("Panel_Hire", parent);
            var list = RT("ResumeList", p.transform);
            Stretch(list, 0, 0, 0, 140);
            var vl = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 12;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            ctrl.resumeList = list;

            // 하단 나무 고무도장 거치대
            var dash = DashH("StampDash", p.transform, Hex("#d6c4a5"));
            dash.rectTransform.anchorMin = new Vector2(0, 0);
            dash.rectTransform.anchorMax = new Vector2(1, 0);
            dash.rectTransform.offsetMin = new Vector2(0, 130);
            dash.rectTransform.offsetMax = new Vector2(0, 132);

            var bar = Img("StampBar", p.transform, theme.round, Hex("#f0e5cf", 0.8f), 14);
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(1, 0);
            bar.rectTransform.pivot = new Vector2(0.5f, 0);
            bar.rectTransform.offsetMin = new Vector2(0, 0);
            bar.rectTransform.offsetMax = new Vector2(0, 120);
            var title = Txt("Title", bar.transform, "고용 승인 고무도장", 20, Hex("#452b14"), TextAlignmentOptions.Left, true);
            MidLeft(title.rectTransform, 24, 16, 600, 30);
            var desc = Txt("Desc", bar.transform, "도장을 마우스로 집어 이력서 카드 위에 끌어다 놓으면 <b>쾅!</b> 찍히며 고용이 확정됩니다.", 15, Hex("#6e4e32"), TextAlignmentOptions.Left);
            MidLeft(desc.rectTransform, 24, -16, 700, 24);

            var holderBg = Card("StampHolder", bar.transform, Hex("#e7d4b3"), Hex("#bfa274"), 14, 2f, out var holderOuter);
            MidRight(holderOuter.rectTransform, 14, 0, 150, 104);
            holderOuter.raycastTarget = true;
            var inkPad = Img("InkPad", holderBg.transform, theme.round, Hex("#7f1d1d", 0.35f), 8);
            Place(inkPad.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(110, 16));

            var stampRoot = RT("StampRoot", holderOuter.transform);
            Place(stampRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(124, 124));
            var stampShadow = Img("Shadow", stampRoot, theme.glow, new Color(0, 0, 0, 0.35f));
            Place(stampShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -46), new Vector2(118, 34));
            var stampImg = Img("Stamp", stampRoot, AssetDatabase.LoadAssetAtPath<Sprite>(StampTextures[0]), Color.white);
            stampImg.preserveAspect = true;
            Stretch(stampImg.rectTransform);

            var tool = holderOuter.gameObject.AddComponent<HireStampTool>();
            tool.controller = ctrl;
            tool.stampRoot = stampRoot;
            tool.stampImage = stampImg;
            tool.shadow = stampShadow;
            tool.holder = holderOuter.rectTransform;
            tool.frontSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StampTextures[0]);
            tool.topSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StampTextures[1]);
            ctrl.stampTool = tool;
            return p;
        }

        private static CanvasGroup BuildStaffPanel(Transform parent, ClipboardController ctrl)
        {
            var p = Panel("Panel_Staff", parent);
            var title = Txt("Title", p.transform, "<color=#b45309>■</color> 인력 배치 & 직원 관리", 19, Hex("#432b16"), TextAlignmentOptions.Left, true);
            TopLeft(title.rectTransform, 0, 0, 600, 32);
            ctrl.staffSummary = Txt("Summary", p.transform, "", 16, Hex("#6e4e32"), TextAlignmentOptions.Right);
            TopRight(ctrl.staffSummary.rectTransform, 0, 2, 400, 30);
            var scroll = ScrollArea("StaffScroll", p.transform, out var content);
            Stretch(scroll, 0, 42, 0, 0);
            ctrl.staffContent = content;
            return p;
        }

        private static CanvasGroup BuildContractPanel(Transform parent, ClipboardController ctrl)
        {
            var p = Panel("Panel_Contract", parent);
            var scroll = ScrollArea("ContractScroll", p.transform, out var content);
            Stretch(scroll, 0, 0, 0, 62);
            ctrl.contractContent = content;

            var bar = Img("BottomBar", p.transform, theme.round, Hex("#f0e5cf", 0.75f), 10);
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(1, 0);
            bar.rectTransform.pivot = new Vector2(0.5f, 0);
            bar.rectTransform.offsetMin = new Vector2(0, 0);
            bar.rectTransform.offsetMax = new Vector2(0, 50);
            var hint = Txt("Hint", bar.transform, "계약서 하단 <b>서명란</b>을 클릭해 서명하면 정기 물량이 입고됩니다.", 15, Hex("#78573a"), TextAlignmentOptions.Left);
            Stretch(hint.rectTransform, 16, 0, 200, 0);
            ctrl.contractCount = Txt("Count", bar.transform, "체결: 0/3", 17, Hex("#134e4a"), TextAlignmentOptions.Right, true);
            Stretch(ctrl.contractCount.rectTransform, 16, 0, 16, 0);
            return p;
        }

        private static CanvasGroup BuildUpgradePanel(Transform parent, ClipboardController ctrl)
        {
            var p = Panel("Panel_Upgrade", parent);
            var scroll = ScrollArea("UpgradeScroll", p.transform, out var content);
            Stretch(scroll, 0, 0, 0, 44);
            ctrl.upgradeContent = content;
            var note = Txt("Note", p.transform, "<color=#d97706>★</color> 강화 완료 시 현장 캐릭터의 이동 및 물류 처리 속도에 즉시 반영됩니다.", 15, Hex("#78573a"), TextAlignmentOptions.Left);
            note.rectTransform.anchorMin = new Vector2(0, 0);
            note.rectTransform.anchorMax = new Vector2(1, 0);
            note.rectTransform.pivot = new Vector2(0.5f, 0);
            note.rectTransform.offsetMin = new Vector2(4, 0);
            note.rectTransform.offsetMax = new Vector2(0, 36);
            return p;
        }

        /// <summary>크라프트 스크롤바가 달린 세로 스크롤 영역</summary>
        private static RectTransform ScrollArea(string name, Transform parent, out RectTransform content)
        {
            var root = RT(name, parent);
            var sr = root.gameObject.AddComponent<ScrollRect>();

            var viewport = Img("Viewport", root, null, new Color(0, 0, 0, 0));
            viewport.raycastTarget = true;
            Stretch(viewport.rectTransform, 0, 0, 18, 0);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = RT("Content", viewport.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 10;
            vl.padding = new RectOffset(0, 0, 2, 8);
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var track = Img("Scrollbar", root, theme.round, Hex("#efe5d1"), 5);
            track.raycastTarget = true;
            track.rectTransform.anchorMin = new Vector2(1, 0);
            track.rectTransform.anchorMax = new Vector2(1, 1);
            track.rectTransform.pivot = new Vector2(1, 0.5f);
            track.rectTransform.offsetMin = new Vector2(-9, 0);
            track.rectTransform.offsetMax = new Vector2(0, 0);
            var area = RT("SlidingArea", track.transform);
            Stretch(area);
            var handle = Img("Handle", area, theme.round, Hex("#bfa682"), 5);
            handle.raycastTarget = true;
            Stretch(handle.rectTransform);
            var sb = track.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;
            sb.direction = Scrollbar.Direction.BottomToTop;
            var cols = sb.colors;
            cols.highlightedColor = new Color(0.85f, 0.8f, 0.72f);
            sb.colors = cols;

            sr.viewport = viewport.rectTransform;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40f;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return root;
        }

        // =====================================================================================
        // Sign modal
        // =====================================================================================

        private static void BuildSignModal(Transform root, ClipboardController ctrl)
        {
            var dim = Img("SignModal", root, null, new Color(0, 0, 0, 0.65f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            ctrl.signModal = dim.gameObject.AddComponent<CanvasGroup>();

            var panel = Card("SignPanel", dim.transform, Hex("#fffdf8"), Hex("#a67c52"), 22, 4f, out var po);
            Place(po.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 450));
            ctrl.signPanel = po.rectTransform;
            var t = panel.transform;

            var sub = Txt("Sub", t, "LOGISTICS CONTRACT SIGNATURE", 12, Hex("#946b42"), TextAlignmentOptions.Center, true);
            sub.characterSpacing = 12f;
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(560, 20));
            ctrl.modalClientName = Txt("ClientName", t, "냥냥 펫푸드 물류 대행 계약", 26, Hex("#3e2715"), TextAlignmentOptions.Center, true);
            Place(ctrl.modalClientName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(560, 36));
            var hint = Txt("Hint", t, "서명란에 대표자 서명을 기입한 뒤 결재를 완료하세요.", 14, Hex("#78716c"), TextAlignmentOptions.Center);
            Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -92), new Vector2(560, 22));

            var close = Btn("CloseButton", t, "×", 30, Hex("#fffdf8"), Hex("#fffdf8"), Hex("#a8a29e"), 10, out _);
            Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(40, 40));
            ctrl.closeSignButton = close;

            var padBg = Card("PadBox", t, Hex("#faf6ec"), Hex("#cbb694"), 14, 2f, out var padOuter);
            Place(padOuter.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -212), new Vector2(580, 190));
            var raw = RT("SignaturePad", padBg.transform);
            Stretch(raw, 8, 8, 8, 8);
            var rawImg = raw.gameObject.AddComponent<RawImage>();
            rawImg.raycastTarget = true;
            var pad = raw.gameObject.AddComponent<SignaturePad>();
            pad.texWidth = 560;
            pad.texHeight = 170;
            var guide = Txt("Guide", padBg.transform, "마우스로 직접 서명하거나 [자동 정자 서명] 클릭", 17, Hex("#a8a29e"), TextAlignmentOptions.Center);
            Stretch(guide.rectTransform);
            pad.guide = guide.gameObject;
            ctrl.signPad = pad;

            ctrl.autoSignButton = Btn("AutoSignButton", t, "<color=#d97706>★</color> 자동 정자 서명", 16, Color.white, Hex("#c8b596"), Hex("#573c24"), 12, out _);
            Place((RectTransform)ctrl.autoSignButton.transform, new Vector2(0, 0), new Vector2(30 + 90, 54), new Vector2(180, 52));
            ctrl.clearSignButton = Btn("ClearButton", t, "지우기", 16, Color.white, Hex("#e7e5e4"), Hex("#78716c"), 12, out _);
            Place((RectTransform)ctrl.clearSignButton.transform, new Vector2(0, 0), new Vector2(222 + 50, 54), new Vector2(100, 52));
            ctrl.confirmSignButton = Btn("ConfirmButton", t, "서명 완료 & 직인", 18, Hex("#8b261e"), Hex("#5f1a14"), Hex("#fef3c7"), 12, out _);
            Place((RectTransform)ctrl.confirmSignButton.transform, new Vector2(1, 0), new Vector2(-30 - 137, 54), new Vector2(274, 52));

            // 직인 연출 레이어
            var seal = Img("SealFx", t, null, Hex("#450a0a", 0.15f));
            Stretch(seal.rectTransform);
            ctrl.sealFx = seal.gameObject.AddComponent<CanvasGroup>();
            ctrl.sealFx.blocksRaycasts = false;
            var mark = Img("SealMark", seal.transform, theme.circle, Hex("#ffffff", 0.96f));
            Place(mark.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
            var ring = Img("Ring", mark.transform, theme.ring, Hex("#dc2626"));
            Stretch(ring.rectTransform);
            var s1 = Txt("Top", mark.transform, "OFFICIAL SEAL", 12, Hex("#ef4444"), TextAlignmentOptions.Center, true);
            s1.characterSpacing = 10f;
            Place(s1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 42), new Vector2(180, 20));
            var s2 = Txt("Main", mark.transform, "계약 체결 완료", 27, Hex("#dc2626"), TextAlignmentOptions.Center, true);
            Place(s2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(200, 40));
            var line = Img("Line", mark.transform, null, Hex("#ef4444"));
            Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(120, 2));
            var s3 = Txt("Bottom", mark.transform, "APPROVED", 15, Hex("#dc2626"), TextAlignmentOptions.Center, true);
            Place(s3.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(180, 24));
            ctrl.sealMark = mark.rectTransform;
            seal.gameObject.SetActive(false);

            dim.gameObject.SetActive(false);
        }

        // =====================================================================================
        // Assets
        // =====================================================================================

        private static void EnsureEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null) go.AddComponent(inputModule);
            else go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        /// <summary>Resources/Stamp_Front, Stamp_Top 을 Sprite 로 임포트 (Resources.Load&lt;Sprite&gt; 가능하도록)</summary>
        private static void EnsureStampSprites()
        {
            foreach (var path in StampTextures)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null)
                {
                    Debug.LogWarning("[ClipBoardSceneBuilder] 도장 텍스처가 없습니다: " + path);
                    continue;
                }
                if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }

        private static Sprite GrainSprite;

        private static void CreateSprites(TMP_FontAsset font)
        {
            if (!Directory.Exists(SpriteDir)) Directory.CreateDirectory(SpriteDir);
            theme = new ClipTheme { font = font };

            const int rs = 96;
            float rr = RoundRadius;
            theme.round = MakeSprite("CB_Round", rs, rs, (x, y) => White(Mathf.Clamp01(0.5f - RoundRectSdf(x + 0.5f, y + 0.5f, rs, rs, rr))), new Vector4(rr, rr, rr, rr), false);

            theme.circle = MakeSprite("CB_Circle", 128, 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64));
                return White(Mathf.Clamp01(63.5f - d));
            }, Vector4.zero, false);

            theme.ring = MakeSprite("CB_SealRing", 160, 160, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(80, 80));
                float outer = Mathf.Clamp01(79.5f - d) * Mathf.Clamp01(d - 73.5f);
                float inner = Mathf.Clamp01(69.5f - d) * Mathf.Clamp01(d - 67f);
                return White(Mathf.Max(outer, inner));
            }, Vector4.zero, false);

            ringThin = MakeSprite("CB_RingThin", 96, 96, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(48, 48));
                return White(Mathf.Clamp01(47.5f - d) * Mathf.Clamp01(d - 41f));
            }, Vector4.zero, false);

            theme.frame = MakeSprite("CB_StampFrame", 64, 64, (x, y) =>
            {
                float d = RoundRectSdf(x + 0.5f, y + 0.5f, 64, 64, 10);
                float outer = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + 4f);
                float inner = Mathf.Clamp01(-6f - d) * Mathf.Clamp01(d + 7.8f);
                return White(Mathf.Max(outer, inner));
            }, new Vector4(16, 16, 16, 16), false);

            theme.glow = MakeSprite("CB_Glow", 128, 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64)) / 64f;
                float a = Mathf.Clamp01(1f - d);
                return White(a * a * (3f - 2f * a));
            }, Vector4.zero, false);

            theme.dash = MakeSprite("CB_DashH", 16, 4, (x, y) => White(x < 9 ? 1f : 0f), Vector4.zero, true);
            theme.dashV = MakeSprite("CB_DashV", 4, 14, (x, y) => White(y < 8 ? 1f : 0f), Vector4.zero, true);
            theme.ruled = MakeSprite("CB_Ruled", 8, 28, (x, y) => White(y == 27 ? 1f : 0f), Vector4.zero, true);
            GrainSprite = MakeSprite("CB_WoodGrain", 14, 4, (x, y) => White(x < 2 ? 1f : 0f), Vector4.zero, true);

            theme.star = MakeSprite("CB_Star", 64, 64, (x, y) =>
            {
                // 4x4 슈퍼샘플링 5각 별
                int hit = 0;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                    if (InStar(new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f), new Vector2(32, 31), 31f, 13f)) hit++;
                return White(hit / 16f);
            }, Vector4.zero, false);

            AssetDatabase.SaveAssets();
        }

        private static bool InStar(Vector2 p, Vector2 c, float rOuter, float rInner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? rOuter : rInner;
                pts[i] = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                if ((pts[i].y > p.y) != (pts[j].y > p.y) &&
                    p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            }
            return inside;
        }

        private static Color White(float a) => new Color(1f, 1f, 1f, a);

        private static Sprite MakeSprite(string file, int w, int h, Func<int, int, Color> pixel, Vector4 border, bool repeat)
        {
            string path = SpriteDir + "/" + file + ".png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 100;
            imp.spriteBorder = border;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float RoundRectSdf(float px, float py, float w, float h, float r)
        {
            float qx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
            float qy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
    }
}
