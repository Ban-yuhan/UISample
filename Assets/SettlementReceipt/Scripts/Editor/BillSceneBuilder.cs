using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LogisticsTycoon.Settlement.Editor
{
    /// <summary>
    /// BillScene(일일 정산 영수증 출력기) 자동 구축.
    /// 원본 HTML의 CSS px 레이아웃을 K(=1.4)배 해서 1920x1080 캔버스에 배치한다.
    /// 메뉴: Tools/UI Showcase/Setup BillScene (Settlement Receipt)
    /// </summary>
    public static class BillSceneBuilder
    {
        /// <summary>이 기능의 모든 리소스가 들어 있는 루트 폴더 (다른 프로젝트로 옮기면 여기만 맞추면 됨)</summary>
        private const string Root = "Assets/SettlementReceipt";
        private const string ScenePath = Root + "/Scenes/BillScene.unity";
        private const string SpriteDir = Root + "/Sprites";
        private const float K = 1.4f;
        private const float RoundSpriteRadius = 48f;
        /// <summary>감열지 맨 앞의 빈 여백(리더). 롤 말림이 이 빈 종이를 감아서 헤더 글씨는 읽히게 남는다.</summary>
        private const float PaperLeader = 60f;
        /// <summary>true = 영수증 윗부분이 뒤로 넘어가 바닥 쪽으로 구부러짐, false = 앞으로 넘어와 늘어짐 (false면 PaperLeader 84 권장)</summary>
        private static readonly bool CurlBackward = true;

        private const string FontAssetPath = Root + "/Fonts/MALGUN SDF.asset";
        private static TMP_FontAsset tmpFont;
        private static Sprite sRound, sRoundTop, sRoundBottom, sFrame, sGlow, sCircle, sDash, sDot, sStripe;
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        private static float P(float css) => css * K;

        [MenuItem("Tools/UI Showcase/Setup BillScene (Settlement Receipt)")]
        public static void Build()
        {
            Debug.Log("<color=#F59E0B>[BillSceneBuilder]</color> 일일 정산 영수증 씬 구축 시작");
            tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (tmpFont == null)
            {
                Debug.LogError("[BillSceneBuilder] TMP 폰트 에셋을 찾을 수 없습니다: " + FontAssetPath);
                return;
            }
            CreateSprites();

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "BillCanvas" || root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    Object.DestroyImmediate(root);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Hex("#1c110a");
            }

            EnsureEventSystem();
            BuildCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("<color=#10B981>[BillSceneBuilder]</color> 완료: " + ScenePath + " — Play 하면 자동 출력, 오른쪽 패널에서 값 입력 후 [정산 출력]");
        }

        // =====================================================================================
        // Scene
        // =====================================================================================

        private static void BuildCanvas()
        {
            var canvasGo = new GameObject("BillCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            Transform root = canvasGo.transform;

            const float printerX = -380f;

            // ---------- 배경: 우드 데스크 ----------
            var bg = Img("WoodDesk", root, null, Hex("#2b1c12"));
            Stretch(bg.rectTransform);
            var stripes = Img("WoodStripes", bg.transform, sStripe, new Color(1f, 0.88f, 0.73f, 0.018f));
            stripes.type = Image.Type.Tiled;
            stripes.pixelsPerUnitMultiplier = 1f / K;
            Stretch(stripes.rectTransform);
            var spot = Img("Spotlight", bg.transform, sGlow, Hex("#4a3221"));
            Place(spot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(printerX, 120f), new Vector2(1900f, 1500f));
            var spot2 = Img("Spotlight_Inner", bg.transform, sGlow, new Color(0.36f, 0.24f, 0.15f, 0.6f));
            Place(spot2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(printerX, 160f), new Vector2(1100f, 900f));

            // ---------- 타이틀 바 ----------
            BuildTitleBar(root, printerX);

            // ---------- 프린터 그림자 ----------
            var shadow = Img("PrinterShadow", root, sGlow, new Color(0f, 0f, 0f, 0.75f));
            Place(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(printerX, 70f), new Vector2(P(470), P(150)));

            // ---------- 프린터 어셈블리 (진동 대상) ----------
            var assembly = RT("PrinterAssembly", root);
            assembly.anchorMin = assembly.anchorMax = new Vector2(0.5f, 0f);
            assembly.pivot = new Vector2(0.5f, 0f);
            assembly.anchoredPosition = new Vector2(printerX, 40f);
            assembly.sizeDelta = new Vector2(P(410), P(122));

            BuildBackBezel(assembly);
            var paperRefs = BuildViewportAndPaper(assembly);
            var hood = BuildFrontBody(assembly);

            // ---------- 입력 패널 ----------
            var panel = BuildInputPanel(root);

            // ---------- 뜯기 레이어 / 파티클 레이어 ----------
            var tearLayer = RT("TearLayer", root);
            Stretch(tearLayer);
            var fxLayer = RT("FxLayer", root);
            Stretch(fxLayer);
            var particles = fxLayer.gameObject.AddComponent<ReceiptParticlePool>();
            particles.circleSprite = sCircle;
            particles.unitScale = K;

            // ---------- 컨트롤러 ----------
            var ctrl = RT("SettlementReceiptController", root);
            Stretch(ctrl);
            var audio = ctrl.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var sfx = ctrl.gameObject.AddComponent<ReceiptSfx>();
            sfx.source = audio;

            var printerFx = assembly.gameObject.AddComponent<PrinterBodyFx>();
            printerFx.shakeTarget = assembly;
            printerFx.shakeAmplitude = K;
            printerFx.led = hood.led;
            printerFx.ledPing = hood.ledPing;
            printerFx.statusText = hood.status;

            var view = ctrl.gameObject.AddComponent<SettlementReceiptView>();
            view.viewport = paperRefs.viewport;
            view.paper = paperRefs.paper;
            view.paperGroup = paperRefs.paperGroup;
            view.paperGraphic = paperRefs.paperGraphic;
            view.curl = paperRefs.curl;
            view.tearLayer = tearLayer;
            view.headerRow = paperRefs.header;
            view.incomeRow = paperRefs.income;
            view.expenseRow = paperRefs.expense;
            view.deliveredRow = paperRefs.delivered;
            view.returnedRow = paperRefs.returned;
            view.netRow = paperRefs.net;
            view.forecastRow = paperRefs.forecast;
            view.dateText = paperRefs.dateText;
            view.weatherChip = paperRefs.weatherChip;
            view.weatherIcon = paperRefs.weatherIcon;
            view.weatherText = paperRefs.weatherText;
            view.eventText = paperRefs.eventText;
            view.stamp = paperRefs.stamp;
            view.stampGroup = paperRefs.stampGroup;
            view.sunnyIcon = Icons["sun"];
            view.cloudyIcon = Icons["cloud"];
            view.rainIcon = Icons["cloud_rain"];
            view.snowIcon = Icons["snow"];
            view.printer = printerFx;
            view.particles = particles;
            view.sfx = sfx;
            view.unitScale = K;
            view.curlRampDistance = P(400);

            panel.view = view;
            panel.feedButton = hood.feed;
            panel.cutButton = hood.cut;
        }

        private static void BuildTitleBar(Transform root, float x)
        {
            var bar = Img("TitleBar", root, sRound, new Color(0.137f, 0.086f, 0.051f, 0.92f), 22f / K);
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -28f);
            rt.sizeDelta = new Vector2(640f, 84f);

            var iconBox = Img("IconBox", rt, sRound, Hex("#492e1b"), 12f / K);
            Place(iconBox.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(48f, 48f));
            var icon = Img("Icon", iconBox.transform, Icons["receipt"], Hex("#fcd34d"));
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));

            var t1 = UTxt("Title", rt, "제3물류센터 일일 정산기", 26, Hex("#fde68a"), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(t1.rectTransform, new Vector2(0f, 0.5f), new Vector2(92f, 13f), new Vector2(520f, 34f), new Vector2(0f, 0.5f));
            var t2 = UTxt("Subtitle", rt, "BOTTOM-UP POS RECEIPT · DOTween", 15, new Color(0.99f, 0.9f, 0.54f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Normal);
            Place(t2.rectTransform, new Vector2(0f, 0.5f), new Vector2(92f, -16f), new Vector2(520f, 22f), new Vector2(0f, 0.5f));
        }

        private static void BuildBackBezel(RectTransform assembly)
        {
            // 아래로 30px 연장 → 본체(앞 레이어)가 덮어서 하단 모서리가 안 보임
            var outline = Img("BackBezel", assembly, sRoundTop, Hex("#6e492c"), 26f);
            TopCenter(outline.rectTransform, 0, 0, 410, 52);
            var fill = Img("Fill", outline.transform, sRoundTop, Color.white, 24f);
            StretchCss(fill.rectTransform, 2, 2, 2, 0);
            Gradient(fill, Hex("#efe2c8"), Hex("#dccaa8"));

            var label = Txt("Label", outline.transform, "▲ TEAR OFF UPWARD · 80MM THERMAL", 9, Hex("#7a583a"), TextAnchor.MiddleCenter, FontStyle.Bold);
            TopCenter(label.rectTransform, 0, 1, 400, 12);

            var slot = Img("SlotUpper", outline.transform, sRoundTop, Color.white, 9f);
            TopCenter(slot.rectTransform, 0, 13, 370, 18);
            Gradient(slot, Hex("#0d0704"), Hex("#1d120b"), Hex("#1d120b"), 0.5f);
        }

        private struct PaperRefs
        {
            public RectTransform viewport, paper;
            public CanvasGroup paperGroup;
            public ReceiptPaperGraphic paperGraphic;
            public ReceiptCurl curl;
            public ReceiptRow header, income, expense, delivered, returned, net, forecast;
            public TMP_Text dateText, weatherText, eventText;
            public RectTransform weatherChip, stamp;
            public Image weatherIcon;
            public CanvasGroup stampGroup;
        }

        private static PaperRefs BuildViewportAndPaper(RectTransform assembly)
        {
            var r = new PaperRefs();

            var viewport = RT("ReceiptViewport", assembly);
            viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.pivot = new Vector2(0.5f, 0f);
            viewport.anchoredPosition = new Vector2(0f, -P(22));
            viewport.sizeDelta = new Vector2(P(346), P(590));
            viewport.gameObject.AddComponent<RectMask2D>();
            r.viewport = viewport;

            var paper = RT("ReceiptPaper", viewport);
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0f);
            paper.pivot = new Vector2(0.5f, 1f);
            paper.anchoredPosition = Vector2.zero;
            paper.sizeDelta = new Vector2(P(322), P(480 + PaperLeader));
            var curl = paper.gameObject.AddComponent<ReceiptCurl>();
            // 말리는 구간(64)은 빈 리더 + 톱니(60 + 11) 안쪽 → 인쇄된 글씨는 접히지 않음
            curl.curlLength = P(64);
            curl.curlRadius = P(CurlBackward ? 20 : 10);
            curl.perspective = P(CurlBackward ? 420 : 1100);
            curl.towardViewer = !CurlBackward;
            if (paper.GetComponent<CanvasRenderer>() == null) paper.gameObject.AddComponent<CanvasRenderer>();
            var pg = paper.gameObject.AddComponent<ReceiptPaperGraphic>();
            pg.color = Hex("#fffdf7");
            pg.curl = curl;
            pg.toothWidth = P(15);
            pg.toothHeight = P(11);
            pg.dotSpacing = P(12);
            pg.raycastTarget = true;
            r.paperGroup = paper.gameObject.AddComponent<CanvasGroup>();
            r.paper = paper;
            r.paperGraphic = pg;
            r.curl = curl;

            // 그리는 순서:
            //  - 뒤로 넘어감: [루트 = 말린 부분(맨 뒤)] → [첫 자식 = 평평한 종이] → 글씨들
            //  - 앞으로 넘어옴: [루트 = 평평한 종이] → 글씨들 → [마지막 자식 = 말린 부분(맨 앞)]
            pg.part = CurlBackward ? ReceiptPaperGraphic.Part.Curl : ReceiptPaperGraphic.Part.Flat;
            if (CurlBackward) AddPaperPart(paper, pg, curl, "PaperFlat", ReceiptPaperGraphic.Part.Flat);

            // ---------------- STEP 1: 헤더 ----------------
            r.header = Row("Row_Header", paper, 23, 80, ReceiptRow.RevealStyle.Fade, 12);
            {
                Transform h = r.header.transform;
                var pill = Img("Pill", h, sRound, Hex("#f3ead8"), 9);
                TopCenter(pill.rectTransform, 0, 0, 164, 18);
                var pIcon = Img("Icon", pill.transform, Icons["package"], Hex("#6d4b2d"));
                Place(pIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(16), 0f), new Vector2(P(12), P(12)));
                var pText = Txt("Text", pill.transform, "제3물류센터 공식 정산표", 10, Hex("#6d4b2d"), TextAnchor.MiddleLeft, FontStyle.Bold);
                Place(pText.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(26), 0f), new Vector2(P(136), P(16)), new Vector2(0f, 0.5f));

                var title = Txt("Title", h, "[일일 물류 정산서]", 18, Hex("#2e1d10"), TextAnchor.MiddleCenter, FontStyle.Bold);
                TopCenter(title.rectTransform, 0, 22, 282, 26);

                r.dateText = Txt("Date", h, "DAY 14 · 2026.11.12 (THU)", 11, Hex("#7e6248"), TextAnchor.MiddleCenter, FontStyle.Normal);
                TopCenter(r.dateText.rectTransform, 0, 50, 282, 16);

                var dash = Img("DashedLine", h, sDash, Hex("#cbb99c"));
                dash.type = Image.Type.Tiled;
                TopCenter(dash.rectTransform, 0, 76, 282, 2);
            }

            // ---------------- STEP 2~5: 수치 행 ----------------
            r.income = MetricRow("Row_Income", paper, 113, "수입", "trend_up", Hex("#d1fae5"), Hex("#047857"),
                Hex("#059669"), 0.24f, 24, new[] { Hex("#10b981"), Hex("#34d399"), Hex("#fbbf24") }, "+485,000 G");
            r.expense = MetricRow("Row_Expense", paper, 158, "지출", "trend_down", Hex("#ffe4e6"), Hex("#e11d48"),
                Hex("#e11d48"), 0.22f, 20, new[] { Hex("#f43f5e"), Hex("#fb7185"), Hex("#fda4af") }, "-112,000 G");
            r.delivered = MetricRow("Row_Delivered", paper, 203, "배송 택배 수", "truck", Hex("#e0f2fe"), Hex("#0369a1"),
                Hex("#0369a1"), 0.22f, 22, new[] { Hex("#0ea5e9"), Hex("#38bdf8"), Hex("#fbbf24") }, "142 건");
            r.returned = MetricRow("Row_Returned", paper, 248, "반품 택배 수", "rotate_ccw", Hex("#ffedd5"), Hex("#c2410c"),
                Hex("#ea580c"), 0.22f, 18, new[] { Hex("#ea580c"), Hex("#fb923c"), Hex("#fed7aa") }, "4 건");
            r.expense.positive = false;
            r.returned.positive = false;

            // ---------------- STEP 6: 순수익 + 스탬프 ----------------
            r.net = Row("Row_Net", paper, 295, 50, ReceiptRow.RevealStyle.Pop, 14);
            {
                Transform n = r.net.transform;
                var flash = Img("Flash", n, sRound, new Color(0.96f, 0.62f, 0.04f, 0f), 8);
                StretchCss(flash.rectTransform, 0, 3, 0, 0);
                r.net.flashBackground = flash;
                r.net.flashAlpha = 0.26f;

                var dash = Img("DashedLine", n, sDash, Hex("#bfa886"));
                dash.type = Image.Type.Tiled;
                TopCenter(dash.rectTransform, 0, 0, 282, 2);

                var l1 = Txt("Label_KR", n, "금일 최종 순수익", 10, Hex("#856447"), TextAnchor.MiddleLeft, FontStyle.Bold);
                TopLeft(l1.rectTransform, 6, 12, 150, 14);
                var l2 = Txt("Label_EN", n, "NET PROFIT", 12, Hex("#382312"), TextAnchor.MiddleLeft, FontStyle.Bold);
                TopLeft(l2.rectTransform, 6, 27, 150, 17);

                ValueBlock(r.net, n, 30, 24, Hex("#047857"), "+373,000 G", 210, 34);
                r.net.particleCount = 30;
                r.net.accent = Hex("#047857");
                r.net.particleColors = new[] { Hex("#10b981"), Hex("#fbbf24"), Hex("#fde047") };

                // 정산 완료 스탬프
                var stamp = RT("SettledStamp", n);
                stamp.anchorMin = stamp.anchorMax = new Vector2(0.5f, 1f);
                stamp.pivot = new Vector2(0.5f, 0.5f);
                stamp.anchoredPosition = new Vector2(0f, -P(8));
                stamp.sizeDelta = new Vector2(P(68), P(20));
                stamp.localEulerAngles = new Vector3(0, 0, 8f);
                r.stampGroup = stamp.gameObject.AddComponent<CanvasGroup>();
                r.stampGroup.blocksRaycasts = false;
                var sBg = Img("Bg", stamp, sRound, new Color(1f, 1f, 1f, 0.9f), 4);
                Stretch(sBg.rectTransform);
                var frameImg = Img("Frame", stamp, sFrame, new Color(0.86f, 0.15f, 0.15f, 0.85f));
                frameImg.type = Image.Type.Sliced;
                frameImg.pixelsPerUnitMultiplier = 1.6f;
                Stretch(frameImg.rectTransform);
                var sText = Txt("Label", stamp, "정산 완료", 10, new Color(0.86f, 0.15f, 0.15f, 0.92f), TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch(sText.rectTransform);
                r.stamp = stamp;
            }

            // ---------------- STEP 7: 익일 예보 ----------------
            r.forecast = Row("Row_Forecast", paper, 353, 107, ReceiptRow.RevealStyle.Calm, 20);
            {
                Transform f = r.forecast.transform;
                var line = Img("TopLine", f, null, Hex("#dfd1b8"));
                TopCenter(line.rectTransform, 0, 0, 282, 1);

                var boxOutline = Img("Box", f, sRound, Hex("#dfd0b4"), 12);
                TopCenter(boxOutline.rectTransform, 0, 11, 282, 76);
                var box = Img("Fill", boxOutline.transform, sRound, Hex("#f5eee0"), 11);
                StretchCss(box.rectTransform, 1, 1, 1, 1);
                Transform b = boxOutline.transform;

                var cal = Img("CalendarIcon", b, Icons["calendar"], Hex("#7c5836"));
                TopLeft(cal.rectTransform, 10, 9, 14, 14);
                var title = Txt("Title", b, "익일 예보 (NEXT DAY)", 10.5f, Hex("#6b4c30"), TextAnchor.MiddleLeft, FontStyle.Bold);
                TopLeft(title.rectTransform, 28, 7, 150, 18);

                var chip = Img("WeatherChip", b, sRound, Hex("#e3d5bc"), 6);
                var chipRt = chip.rectTransform;
                chipRt.anchorMin = chipRt.anchorMax = new Vector2(1f, 1f);
                chipRt.pivot = new Vector2(1f, 1f);
                chipRt.anchoredPosition = new Vector2(-P(10), -P(6));
                chipRt.sizeDelta = new Vector2(P(112), P(20));
                var chipFill = Img("Fill", chipRt, sRound, new Color(1f, 1f, 1f, 0.85f), 5);
                StretchCss(chipFill.rectTransform, 1, 1, 1, 1);
                r.weatherIcon = Img("WeatherIcon", chipRt, Icons["cloud_rain"], Hex("#0284c7"));
                Place(r.weatherIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(7), 0f), new Vector2(P(14), P(14)), new Vector2(0f, 0.5f));
                r.weatherText = Txt("WeatherText", chipRt, "비 (이속 -10%)", 10, Hex("#4a3623"), TextAnchor.MiddleLeft, FontStyle.Bold);
                var wt = r.weatherText.rectTransform;
                wt.anchorMin = Vector2.zero;
                wt.anchorMax = Vector2.one;
                wt.pivot = new Vector2(0f, 0.5f);
                wt.offsetMin = new Vector2(P(25), 0f);
                wt.offsetMax = new Vector2(-P(7), 0f);
                r.weatherChip = chipRt;

                r.eventText = Txt("EventText", b, "특이사항: 명절 대량 물류 예고! (익일 물량 +180%)", 11, Hex("#5e442d"), TextAnchor.UpperLeft, FontStyle.Normal);
                r.eventText.textWrappingMode = TextWrappingModes.Normal;
                TopLeft(r.eventText.rectTransform, 10, 31, 262, 38);

                var hint = Txt("TearHint", f, "*** CLICK RECEIPT TO TEAR OFF ***", 9, Hex("#9c8267"), TextAnchor.MiddleCenter, FontStyle.Normal);
                TopCenter(hint.rectTransform, 0, 94, 282, 13);
            }

            // 앞으로 넘어오는 모드: 늘어진 윗부분이 글씨를 덮도록 마지막 자식으로
            if (!CurlBackward) AddPaperPart(paper, pg, curl, "PaperCurlFlap", ReceiptPaperGraphic.Part.Curl);

            return r;
        }

        private static void AddPaperPart(RectTransform paper, ReceiptPaperGraphic src, ReceiptCurl curl, string name, ReceiptPaperGraphic.Part part)
        {
            var rt = RT(name, paper);
            Stretch(rt);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<ReceiptPaperGraphic>();
            g.part = part;
            g.paperSpace = paper;
            g.color = src.color;
            g.curl = curl;
            g.toothWidth = src.toothWidth;
            g.toothHeight = src.toothHeight;
            g.dotSpacing = src.dotSpacing;
            g.shadowOffset = new Vector2(0f, -P(4));
            g.raycastTarget = false;
        }

        private struct HoodRefs
        {
            public Image led, ledPing;
            public TMP_Text status;
            public Button cut, feed;
        }

        private static HoodRefs BuildFrontBody(RectTransform assembly)
        {
            var h = new HoodRefs();

            var outline = Img("FrontBody", assembly, sRoundBottom, Hex("#6e492c"), 26f);
            TopCenter(outline.rectTransform, 0, 22, 410, 100);
            var fill = Img("Fill", outline.transform, sRoundBottom, Color.white, 24f);
            StretchCss(fill.rectTransform, 2, 0, 2, 2);
            Gradient(fill, Hex("#dccaa8"), Hex("#b1956b"), Hex("#c8b089"), 0.45f);
            var innerShade = Img("InnerShade", outline.transform, sRoundBottom, new Color(0.29f, 0.18f, 0.09f, 0.0f), 24f);
            StretchCss(innerShade.rectTransform, 2, 0, 2, 2);
            Gradient(innerShade, new Color(1, 1, 1, 0), new Color(0.29f, 0.18f, 0.09f, 0.45f), new Color(1, 1, 1, 0), 0.88f);
            innerShade.color = Color.white;
            Transform body = outline.transform;

            var slot = Img("SlotLower", body, sRoundBottom, Color.white, 4.5f);
            TopCenter(slot.rectTransform, 0, 0, 370, 9);
            Gradient(slot, Hex("#1d120b"), Hex("#2c1c11"));
            var slotHi = Img("SlotHighlight", body, null, new Color(1f, 1f, 1f, 0.45f));
            TopCenter(slotHi.rectTransform, 0, 9, 350, 1);

            // 조작 패널(후드)
            var hoodOutline = Img("Hood", body, sRound, Hex("#3d2514"), 16f);
            TopCenter(hoodOutline.rectTransform, 0, 21, 369, 44);
            var hood = Img("Fill", hoodOutline.transform, sRound, Color.white, 15f);
            StretchCss(hood.rectTransform, 1, 1, 1, 1);
            Gradient(hood, Hex("#5e3b22"), Hex("#462a16"));
            var hoodHi = Img("TopHighlight", hoodOutline.transform, null, new Color(1f, 0.88f, 0.7f, 0.3f));
            TopCenter(hoodHi.rectTransform, 0, 1, 330, 1);
            Transform ht = hoodOutline.transform;

            h.ledPing = Img("LedPing", ht, sCircle, new Color(0.2f, 0.83f, 0.6f, 0.35f));
            Place(h.ledPing.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(22), 0f), new Vector2(P(12), P(12)));
            h.ledPing.rectTransform.localScale = Vector3.one * 1.6f;
            var ledRim = Img("LedRim", ht, sCircle, Hex("#064e3b"));
            Place(ledRim.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(22), 0f), new Vector2(P(13), P(13)));
            h.led = Img("Led", ht, sCircle, Hex("#34d399"));
            Place(h.led.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(22), 0f), new Vector2(P(11), P(11)));

            var model = Txt("Model", ht, "K-POS 80MM", 12, new Color(1f, 0.95f, 0.78f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(model.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(36), P(6)), new Vector2(P(120), P(16)), new Vector2(0f, 0.5f));
            h.status = Txt("Status", ht, "READY", 9, new Color(0.99f, 0.9f, 0.54f, 0.7f), TextAnchor.MiddleLeft, FontStyle.Normal);
            Place(h.status.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(36), -P(9)), new Vector2(P(120), P(13)), new Vector2(0f, 0.5f));

            h.feed = CssButton(ht, "FeedButton", "FEED ▲", Hex("#3b2312"), Hex("#fde68a"), new Vector2(-P(12), 0f), new Vector2(P(60), P(22)));
            h.cut = CssButton(ht, "CutButton", "CUT", Hex("#2a180c"), Hex("#fecdd3"), new Vector2(-P(80), 0f), new Vector2(P(42), P(22)));

            // 고무 받침
            var feetL = Img("FootL", body, sRound, new Color(0.29f, 0.18f, 0.11f, 0.6f), 3f);
            TopCenter(feetL.rectTransform, -120, 76, 40, 6);
            var feetC = Img("FootC", body, sRound, new Color(0.29f, 0.18f, 0.11f, 0.4f), 3f);
            TopCenter(feetC.rectTransform, 0, 76, 64, 6);
            var feetR = Img("FootR", body, sRound, new Color(0.29f, 0.18f, 0.11f, 0.6f), 3f);
            TopCenter(feetR.rectTransform, 120, 76, 40, 6);

            return h;
        }

        private static Button CssButton(Transform parent, string name, string label, Color bg, Color fg, Vector2 rightPos, Vector2 size)
        {
            var img = Img(name, parent, sRound, bg, 8f);
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = rightPos;
            rt.sizeDelta = size;
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(fg.r, fg.g, fg.b, 0.3f);
            outline.effectDistance = new Vector2(1f, -1f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var t = Txt("Label", rt, label, 10, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform);
            return btn;
        }

        // ------------------------------------------------------------------ rows

        private static ReceiptRow Row(string name, Transform paper, float y, float h, ReceiptRow.RevealStyle style, float padCss)
        {
            var rt = RT(name, paper);
            TopCenter(rt, 0, y + PaperLeader, 282, h);
            var row = rt.gameObject.AddComponent<ReceiptRow>();
            row.group = rt.gameObject.AddComponent<CanvasGroup>();
            row.group.alpha = 0f;
            row.group.blocksRaycasts = false;
            row.style = style;
            row.revealPad = P(padCss);
            return row;
        }

        private static ReceiptRow MetricRow(string name, Transform paper, float y, string label, string icon,
            Color boxColor, Color iconColor, Color accent, float flashAlpha, int particles, Color[] particleColors, string sample)
        {
            var row = Row(name, paper, y, 37, ReceiptRow.RevealStyle.Pop, 12);
            Transform t = row.transform;

            var flash = Img("Flash", t, sRound, new Color(accent.r, accent.g, accent.b, 0f), 8);
            Stretch(flash.rectTransform);
            row.flashBackground = flash;
            row.flashAlpha = flashAlpha;

            var boxImg = Img("IconBox", t, sRound, boxColor, 6);
            Place(boxImg.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(6), 0f), new Vector2(P(20), P(20)), new Vector2(0f, 0.5f));
            var ic = Img("Icon", boxImg.transform, Icons[icon], iconColor);
            Place(ic.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(P(14), P(14)));

            var lbl = Txt("Label", t, label, 13, Hex("#5a3f28"), TextAnchor.MiddleLeft, FontStyle.Bold);
            Place(lbl.rectTransform, new Vector2(0f, 0.5f), new Vector2(P(32), 0f), new Vector2(P(130), P(20)), new Vector2(0f, 0.5f));

            ValueBlock(row, t, 18.5f, 18, accent, sample, 170, 28);
            row.accent = accent;
            row.particleCount = particles;
            row.particleColors = particleColors;

            var dots = Img("DottedLine", t, sDot, Hex("#e2d4bc"));
            dots.type = Image.Type.Tiled;
            var drt = dots.rectTransform;
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 0f);
            drt.pivot = new Vector2(0.5f, 0f);
            drt.anchoredPosition = Vector2.zero;
            drt.sizeDelta = new Vector2(0f, P(1.5f));
            return row;
        }

        /// <summary>오른쪽 정렬 수치 블록: [ValueAnchor(스케일 대상, pivot 오른쪽) → Glow, Text]</summary>
        private static void ValueBlock(ReceiptRow row, Transform parent, float centerYFromTop, float fontCss, Color accent, string sample, float wCss, float hCss)
        {
            var anchor = RT("ValueAnchor", parent);
            anchor.anchorMin = anchor.anchorMax = new Vector2(1f, 1f);
            anchor.pivot = new Vector2(1f, 0.5f);
            anchor.anchoredPosition = new Vector2(-P(6), -P(centerYFromTop));
            anchor.sizeDelta = new Vector2(P(wCss), P(hCss));

            var glow = Img("Glow", anchor, sGlow, new Color(accent.r, accent.g, accent.b, 0f));
            var grt = glow.rectTransform;
            grt.anchorMin = grt.anchorMax = new Vector2(1f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(P(wCss), P(hCss) * 2f);

            var txt = Txt("Value", anchor, sample, fontCss, accent, TextAnchor.MiddleRight, FontStyle.Bold);
            Stretch(txt.rectTransform);

            row.valueRect = anchor;
            row.valueText = txt;
            row.valueGlow = glow;
        }

        // ------------------------------------------------------------------ input panel

        private static SettlementInputPanel BuildInputPanel(Transform root)
        {
            var outline = Img("SettlementInputPanel", root, sRound, new Color(0.49f, 0.34f, 0.22f, 0.55f), 24f / K);
            outline.raycastTarget = true;
            var rt = outline.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-70f, 0f);
            rt.sizeDelta = new Vector2(660f, 1010f);
            var fill = Img("Fill", rt, sRound, new Color(0.137f, 0.086f, 0.051f, 0.95f), 22f / K);
            UStretch(fill.rectTransform, 2, 2, 2, 2);

            var panel = rt.gameObject.AddComponent<SettlementInputPanel>();

            var title = UTxt("Title", rt, "정산 데이터 입력", 30, Hex("#fde68a"), TextAnchor.MiddleLeft, FontStyle.Bold);
            UTopLeft(title.rectTransform, 34, 26, 600, 40);
            var sub = UTxt("Subtitle", rt, "값을 입력하고 [정산 출력]을 누르면 그대로 영수증에 찍힙니다", 17, new Color(0.99f, 0.9f, 0.54f, 0.6f), TextAnchor.MiddleLeft, FontStyle.Normal);
            UTopLeft(sub.rectTransform, 34, 68, 600, 26);

            float y = 112f;
            const float step = 64f;
            panel.dayField = FieldRow(rt, "DAY", ref y, step, TMP_InputField.ContentType.IntegerNumber, "14");
            panel.dateField = FieldRow(rt, "날짜", ref y, step, TMP_InputField.ContentType.Standard, "2026.11.12");
            panel.dateTagField = FieldRow(rt, "날짜 태그", ref y, step, TMP_InputField.ContentType.Standard, "(선택) 명절 대목");
            panel.incomeField = FieldRow(rt, "수입 (G)", ref y, step, TMP_InputField.ContentType.IntegerNumber, "485000");
            panel.expenseField = FieldRow(rt, "지출 (G)", ref y, step, TMP_InputField.ContentType.IntegerNumber, "112000");
            panel.deliveredField = FieldRow(rt, "배송 택배 수", ref y, step, TMP_InputField.ContentType.IntegerNumber, "142");
            panel.returnedField = FieldRow(rt, "반품 택배 수", ref y, step, TMP_InputField.ContentType.IntegerNumber, "4");
            panel.netField = FieldRow(rt, "순수익 (G)", ref y, step, TMP_InputField.ContentType.IntegerNumber, "비우면 수입-지출 자동");

            // 날씨 순환 버튼
            {
                var lbl = UTxt("Label_Weather", rt, "날씨", 21, Hex("#fed7aa"), TextAnchor.MiddleLeft, FontStyle.Bold);
                UTopLeft(lbl.rectTransform, 34, y, 190, 50);
                panel.weatherButton = UButton(rt, "WeatherButton", "◀  비  ▶", Hex("#3c2718"), Hex("#fde68a"), 21, 230, y, 396, 50, out panel.weatherButtonLabel);
                y += step;
            }
            panel.weatherEffectField = FieldRow(rt, "날씨 효과", ref y, step, TMP_InputField.ContentType.Standard, "이속 -10%");
            panel.eventField = FieldRow(rt, "특이사항", ref y, step, TMP_InputField.ContentType.Standard, "명절 대량 물류 예고!");

            y += 6f;
            panel.normalPresetButton = UButton(rt, "PresetNormal", "일반일 프리셋", Hex("#3c2718"), Hex("#fde68a"), 19, 34, y, 196, 52, out _);
            panel.holidayPresetButton = UButton(rt, "PresetHoliday", "명절 대목 프리셋", Hex("#065f46"), Hex("#d1fae5"), 19, 240, y, 210, 52, out _);
            panel.muteButton = UButton(rt, "MuteButton", "사운드 ON", Hex("#3c2718"), Hex("#fde68a"), 19, 460, y, 166, 52, out panel.muteLabel);
            y += 66f;

            panel.printButton = UButton(rt, "PrintButton", "▲  정산 출력", Hex("#d97706"), Hex("#fffbeb"), 26, 34, y, 380, 66, out _);
            panel.tearButton = UButton(rt, "TearButton", "영수증 뜯기", Hex("#3c2718"), Hex("#fecdd3"), 24, 424, y, 202, 66, out _);
            y += 80f;

            var hint = UTxt("Hint", rt, "출력 중 영수증 클릭 = 스킵  ·  출력 후 클릭 = 뜯기", 16, new Color(0.99f, 0.9f, 0.54f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UTopLeft(hint.rectTransform, 34, y, 592, 26);

            return panel;
        }

        private static TMP_InputField FieldRow(RectTransform parent, string label, ref float y, float step, TMP_InputField.ContentType ct, string placeholder)
        {
            var lbl = UTxt("Label_" + label, parent, label, 21, Hex("#fed7aa"), TextAnchor.MiddleLeft, FontStyle.Bold);
            UTopLeft(lbl.rectTransform, 34, y, 190, 50);

            var bg = Img("Input_" + label, parent, sRound, Hex("#fdf6e9"), 10f / K);
            bg.raycastTarget = true;
            UTopLeft(bg.rectTransform, 230, y, 396, 50);

            // TMP_InputField 구조: [배경+InputField] → Text Area(RectMask2D, 뷰포트) → Placeholder, Text
            var area = RT("Text Area", bg.rectTransform);
            UStretch(area, 16, 4, 16, 4);
            area.gameObject.AddComponent<RectMask2D>();

            var ph = UTxt("Placeholder", area, placeholder, 20, new Color(0.23f, 0.15f, 0.09f, 0.35f), TextAnchor.MiddleLeft, FontStyle.Italic);
            Stretch(ph.rectTransform);
            var text = UTxt("Text", area, "", 22, Hex("#3a2618"), TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(text.rectTransform);

            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = tmpFont;
            input.pointSize = 22;
            input.contentType = ct;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = Hex("#b45309");
            input.selectionColor = new Color(0.98f, 0.75f, 0.14f, 0.4f);

            y += step;
            return input;
        }

        private static Button UButton(RectTransform parent, string name, string label, Color bg, Color fg, int fontSize,
            float x, float y, float w, float h, out TMP_Text labelText)
        {
            var img = Img(name, parent, sRound, bg, 12f / K);
            img.raycastTarget = true;
            UTopLeft(img.rectTransform, x, y, w, h);
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(fg.r, fg.g, fg.b, 0.3f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            labelText = UTxt("Label", img.rectTransform, label, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(labelText.rectTransform);
            return btn;
        }

        // =====================================================================================
        // UI helpers
        // =====================================================================================

        private static RectTransform RT(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(string name, Transform parent, Sprite sprite, Color color, float radiusCss = -1f)
        {
            var rt = RT(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && radiusCss > 0f)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = RoundSpriteRadius / P(radiusCss);
            }
            else if (sprite != null && (sprite == sCircle || sprite == sGlow || Icons.ContainsValue(sprite)))
            {
                img.preserveAspect = sprite != sGlow;
            }
            return img;
        }

        /// <summary>CSS px 크기의 텍스트</summary>
        private static TextMeshProUGUI Txt(string name, Transform parent, string s, float sizeCss, Color c, TextAnchor anchor, FontStyle style)
        {
            return UTxt(name, parent, s, P(sizeCss), c, anchor, style);
        }

        private static TextMeshProUGUI UTxt(string name, Transform parent, string s, float size, Color c, TextAnchor anchor, FontStyle style)
        {
            var rt = RT(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = tmpFont;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = ToTmpAlignment(anchor);
            t.fontStyle = style == FontStyle.Bold ? FontStyles.Bold
                : style == FontStyle.Italic ? FontStyles.Italic
                : style == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic
                : FontStyles.Normal;
            t.raycastTarget = false;
            t.richText = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.margin = Vector4.zero;
            t.characterSpacing = -3f; // TMP의 맑은 고딕 자간이 브라우저보다 넓어서 살짝 좁힘
            return t;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static void Gradient(Image img, Color top, Color bottom, Color? middle = null, float middlePos = 0.45f)
        {
            var g = img.gameObject.AddComponent<UIVerticalGradient>();
            g.top = top;
            g.bottom = bottom;
            if (middle.HasValue)
            {
                g.useMiddle = true;
                g.middle = middle.Value;
                g.middlePosition = middlePos;
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchCss(RectTransform rt, float l, float t, float r, float b) => UStretch(rt, P(l), P(t), P(r), P(b));

        private static void UStretch(RectTransform rt, float l, float t, float r, float b)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>부모 상단 중앙 기준, x = 중앙에서의 오프셋, y = 위에서 아래로 (CSS px)</summary>
        private static void TopCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(P(x), -P(y));
            rt.sizeDelta = new Vector2(P(w), P(h));
        }

        private static void TopLeft(RectTransform rt, float x, float y, float w, float h) => UTopLeft(rt, P(x), P(y), P(w), P(h));

        private static void UTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            c.a = a;
            return c;
        }

        private static void EnsureEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null) go.AddComponent(inputModule);
            else go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // =====================================================================================
        // Procedural sprites
        // =====================================================================================

        private static void CreateSprites()
        {
            if (!Directory.Exists(SpriteDir)) Directory.CreateDirectory(SpriteDir);

            const int rs = 128;
            const float rr = RoundSpriteRadius;
            sRound = MakeSprite("SR_Round", rs, rs, (x, y) => White(RoundRectAlpha(x, y, rs, rs, rr, true, true, true, true)), new Vector4(rr, rr, rr, rr), false);
            sRoundTop = MakeSprite("SR_RoundTop", rs, rs, (x, y) => White(RoundRectAlpha(x, y, rs, rs, rr, true, true, false, false)), new Vector4(rr, 1, rr, rr), false);
            sRoundBottom = MakeSprite("SR_RoundBottom", rs, rs, (x, y) => White(RoundRectAlpha(x, y, rs, rs, rr, false, false, true, true)), new Vector4(rr, rr, rr, 1), false);

            sFrame = MakeSprite("SR_StampFrame", 64, 64, (x, y) =>
            {
                float d = RoundRectSdf(x + 0.5f, y + 0.5f, 64, 64, 10);
                float outer = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + 3.5f);
                float inner = Mathf.Clamp01(-4.5f - d) * Mathf.Clamp01(d + 6.5f);
                return White(Mathf.Max(outer, inner));
            }, new Vector4(16, 16, 16, 16), false);

            sGlow = MakeSprite("SR_Glow", 128, 128, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64)) / 64f;
                float a = Mathf.Clamp01(1f - d);
                return White(a * a * (3f - 2f * a));
            }, Vector4.zero, false);

            sCircle = MakeSprite("SR_Circle", 64, 64, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32));
                return White(Mathf.Clamp01(31.5f - d));
            }, Vector4.zero, false);

            sDash = MakeSprite("SR_DashTile", 16, 4, (x, y) => White(x < 9 ? 1f : 0f), Vector4.zero, true);
            sDot = MakeSprite("SR_DotTile", 4, 2, (x, y) => White(x < 2 ? 1f : 0f), Vector4.zero, true);
            sStripe = MakeSprite("SR_WoodStripe", 16, 4, (x, y) => White(x < 2 ? 1f : 0f), Vector4.zero, true);

            Icons.Clear();
            foreach (var kv in IconShapes())
                Icons[kv.Key] = MakeSprite("Icon_" + kv.Key, 64, 64, RasterStroke(kv.Value, 64, 2.1f), Vector4.zero, false);

            AssetDatabase.SaveAssets();
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

        private static float RoundRectAlpha(int x, int y, int w, int h, float r, bool tl, bool tr, bool bl, bool br)
        {
            float px = x + 0.5f, py = y + 0.5f;
            bool left = px < r, right = px > w - r, bottom = py < r, top = py > h - r;
            bool rounded = (left && top && tl) || (right && top && tr) || (left && bottom && bl) || (right && bottom && br);
            if (!rounded) return 1f;
            return Mathf.Clamp01(0.5f - RoundRectSdf(px, py, w, h, r));
        }

        // ------------------------------------------------------------------ icon rasterizer (24x24 SVG 좌표, 위→아래 y)

        private static Func<int, int, Color> RasterStroke(List<Vector2[]> lines, int size, float stroke)
        {
            float s = size / 24f;
            float half = stroke * 0.5f * s;
            return (x, y) =>
            {
                var p = new Vector2((x + 0.5f) / s, (size - (y + 0.5f)) / s);
                float best = float.MaxValue;
                foreach (var line in lines)
                {
                    for (int i = 0; i < line.Length - 1; i++)
                        best = Mathf.Min(best, SegDist(p, line[i], line[i + 1]));
                    if (line.Length == 1) best = Mathf.Min(best, Vector2.Distance(p, line[0]));
                }
                return White(Mathf.Clamp01(half + 0.5f - best * s));
            };
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        private static Vector2[] Pts(params float[] xy)
        {
            var r = new Vector2[xy.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return r;
        }

        private static Vector2[] Arc(float cx, float cy, float r, float a0, float a1, int segs = 28)
        {
            var pts = new Vector2[segs + 1];
            for (int i = 0; i <= segs; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)segs) * Mathf.Deg2Rad;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return pts;
        }

        private static Vector2[] Concat(params Vector2[][] parts)
        {
            var list = new List<Vector2>();
            foreach (var p in parts) list.AddRange(p);
            return list.ToArray();
        }

        private static Vector2[] RectPts(float x, float y, float w, float h) => Pts(x, y, x + w, y, x + w, y + h, x, y + h, x, y);

        private static Dictionary<string, List<Vector2[]>> IconShapes()
        {
            var d = new Dictionary<string, List<Vector2[]>>();
            d["trend_up"] = new List<Vector2[]> { Pts(23, 6, 13.5f, 15.5f, 8.5f, 10.5f, 1, 18), Pts(17, 6, 23, 6, 23, 12) };
            d["trend_down"] = new List<Vector2[]> { Pts(23, 18, 13.5f, 8.5f, 8.5f, 13.5f, 1, 6), Pts(17, 18, 23, 18, 23, 12) };
            d["truck"] = new List<Vector2[]>
            {
                RectPts(1, 3, 15, 13), Pts(16, 8, 20, 8, 23, 11, 23, 16, 16, 16, 16, 8),
                Arc(5.5f, 18.5f, 2.5f, 0, 360), Arc(18.5f, 18.5f, 2.5f, 0, 360)
            };
            d["rotate_ccw"] = new List<Vector2[]> { Pts(1, 4, 1, 10, 7, 10), Concat(Arc(12, 12, 9, 160.5f, -135f, 40), Pts(1, 10)) };
            d["calendar"] = new List<Vector2[]> { RectPts(3, 4, 18, 18), Pts(16, 2, 16, 6), Pts(8, 2, 8, 6), Pts(3, 10, 21, 10) };
            d["cloud_rain"] = new List<Vector2[]>
            {
                Concat(Arc(17.2f, 12.3f, 4.4f, 54.3f, -86f), Arc(9, 10, 7, -16.6f, -224.4f, 40)),
                Pts(16, 14, 16, 20), Pts(8, 14, 8, 20), Pts(12, 16, 12, 22)
            };
            d["cloud"] = new List<Vector2[]>
            {
                Concat(Arc(10, 13, 6, 90, 330, 36), Arc(17, 15, 4, 250, 450, 24), Pts(10, 19))
            };
            var sun = new List<Vector2[]> { Arc(12, 12, 5, 0, 360) };
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad;
                sun.Add(Pts(12 + Mathf.Cos(a) * 8.2f, 12 + Mathf.Sin(a) * 8.2f, 12 + Mathf.Cos(a) * 10.6f, 12 + Mathf.Sin(a) * 10.6f));
            }
            d["sun"] = sun;
            var snow = new List<Vector2[]>();
            for (int k = 0; k < 3; k++)
            {
                float a = (90f + k * 60f) * Mathf.Deg2Rad;
                float cx = Mathf.Cos(a), sy = Mathf.Sin(a);
                snow.Add(Pts(12 - cx * 10, 12 - sy * 10, 12 + cx * 10, 12 + sy * 10));
            }
            d["snow"] = snow;
            d["package"] = new List<Vector2[]>
            {
                Pts(12, 2, 21, 7, 21, 17, 12, 22, 3, 17, 3, 7, 12, 2), Pts(3, 7, 12, 12, 21, 7), Pts(12, 12, 12, 22)
            };
            d["receipt"] = new List<Vector2[]> { RectPts(4, 2, 16, 20), Pts(8, 6, 16, 6), Pts(8, 10, 16, 10), Pts(8, 14, 12, 14) };
            return d;
        }
    }
}
