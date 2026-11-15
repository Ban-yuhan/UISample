using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using ToastMessageSample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISample.Common.Editor
{
    public static class MainHubAndBackBuilder
    {
        private const string FontPath = "Assets/Fonts/MALGUN SDF.asset";
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string RoundedRectPath = "Assets/Generated/ToastMessage/RoundedRect.png";
        private const string RoundedRectOutlinePath = "Assets/Generated/ToastMessage/RoundedRectOutline.png";

        private struct SampleSceneEntry
        {
            public string ScenePath;
            public string SceneName;
            public string Title;
            public string Subtitle;
            public Color AccentColor;
            public Vector2 BackAnchor;
            public Vector2 BackPivot;
            public Vector2 BackOffset;
            public bool ShowOnMainHub;
        }

        private static readonly SampleSceneEntry[] SampleScenes =
        {
            new SampleSceneEntry
            {
                ScenePath = "Assets/Scenes/ButtonScene.unity",
                SceneName = "ButtonScene",
                Title = "버튼 인터랙션 샘플",
                Subtitle = "ButtonScene - 다양한 버튼 모션 및 피드백",
                AccentColor = Hex("#4C7599"),
                BackAnchor = new Vector2(1f, 1f),
                BackPivot = new Vector2(1f, 1f),
                BackOffset = new Vector2(-28f, -26f),
                ShowOnMainHub = true
            },
            new SampleSceneEntry
            {
                ScenePath = "Assets/Scenes/ClipBoardScene.unity",
                SceneName = "ClipBoardScene",
                Title = "클립보드 / 도장 / 서명",
                Subtitle = "ClipBoardScene - 직원 채용 및 스탬프 UI",
                AccentColor = Hex("#D98A2B"),
                BackAnchor = new Vector2(0f, 1f),
                BackPivot = new Vector2(0f, 1f),
                BackOffset = new Vector2(24f, -24f),
                ShowOnMainHub = true
            },
            new SampleSceneEntry
            {
                ScenePath = "Assets/Scenes/SceneA.unity",
                SceneName = "SceneA",
                Title = "트랜지션 샘플",
                Subtitle = "SceneA <-> SceneB 화면 전환 효과 데모",
                AccentColor = Hex("#4A3B31"),
                BackAnchor = new Vector2(0f, 1f),
                BackPivot = new Vector2(0f, 1f),
                BackOffset = new Vector2(24f, -24f),
                ShowOnMainHub = true
            },
            new SampleSceneEntry
            {
                ScenePath = "Assets/Scenes/SceneB.unity",
                SceneName = "SceneB",
                Title = "트랜지션 샘플 B",
                Subtitle = "SceneB - 화면 전환 연출 테스트 B",
                AccentColor = Hex("#4A3B31"),
                BackAnchor = new Vector2(0f, 1f),
                BackPivot = new Vector2(0f, 1f),
                BackOffset = new Vector2(24f, -24f),
                ShowOnMainHub = false
            },
            new SampleSceneEntry
            {
                ScenePath = "Assets/Scenes/ToastMessageScene.unity",
                SceneName = "ToastMessageScene",
                Title = "토스트 메시지 (캡슐 / 스낵바)",
                Subtitle = "ToastMessageScene - Ver 1 캡슐 & Ver 2 스낵바",
                AccentColor = Hex("#C9534F"),
                BackAnchor = new Vector2(0.5f, 1f),
                BackPivot = new Vector2(0.5f, 1f),
                BackOffset = new Vector2(0f, -28f),
                ShowOnMainHub = true
            },
            new SampleSceneEntry
            {
                ScenePath = "Assets/Scenes/UnlockScene.unity",
                SceneName = "UnlockScene",
                Title = "지역 해금 연출",
                Subtitle = "UnlockScene - 자물쇠 해제 및 확장 애니메이션",
                AccentColor = Hex("#4E8C6F"),
                BackAnchor = new Vector2(0f, 0f),
                BackPivot = new Vector2(0f, 0f),
                BackOffset = new Vector2(24f, 24f),
                ShowOnMainHub = true
            },
            new SampleSceneEntry
            {
                ScenePath = "Assets/SettlementReceipt/Scenes/BillScene.unity",
                SceneName = "BillScene",
                Title = "정산 영수증",
                Subtitle = "BillScene - 영수증 출력 및 찢기 연출",
                AccentColor = Hex("#C26D4A"),
                BackAnchor = new Vector2(0f, 1f),
                BackPivot = new Vector2(0f, 1f),
                BackOffset = new Vector2(24f, -24f),
                ShowOnMainHub = true
            }
        };

        [MenuItem("Tools/UI Sample Hub/Build MainScene, Back Buttons & BuildSettings")]
        public static void BuildAll()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[MainHubAndBackBuilder] Font not found: " + FontPath);
                return;
            }

            EnsureRoundedSprites(out Sprite roundedFill, out Sprite roundedOutline);

            BuildMainScene(font, roundedFill, roundedOutline);
            InjectBackButtonsIntoAllSampleScenes(font, roundedFill, roundedOutline);
            RegisterBuildSettings();

            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Debug.Log("[MainHubAndBackBuilder] Built warm beige MainScene, updated Back buttons in 7 scenes, removed cross-links, and updated EditorBuildSettings.");
        }

        private static void EnsureRoundedSprites(out Sprite fillSprite, out Sprite outlineSprite)
        {
            string dir = Path.GetDirectoryName(RoundedRectPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(RoundedRectPath))
            {
                SaveRoundedTexture(RoundedRectPath, isOutline: false);
            }
            if (!File.Exists(RoundedRectOutlinePath))
            {
                SaveRoundedTexture(RoundedRectOutlinePath, isOutline: true);
            }

            AssetDatabase.Refresh();
            ConfigureNineSliceImporter(RoundedRectPath);
            ConfigureNineSliceImporter(RoundedRectOutlinePath);

            fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectPath);
            outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectOutlinePath);
        }

        private static void SaveRoundedTexture(string assetPath, bool isOutline)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 half = new Vector2(size * 0.5f - (isOutline ? 2.5f : 1f), size * 0.5f - (isOutline ? 2.5f : 1f));
            const float radius = 14f;
            const float halfStroke = 1.6f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    Vector2 dVec = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(radius, radius);
                    float boxDist = Mathf.Min(Mathf.Max(dVec.x, dVec.y), 0f) + new Vector2(Mathf.Max(dVec.x, 0f), Mathf.Max(dVec.y, 0f)).magnitude - radius;
                    float dist = isOutline ? (Mathf.Abs(boxDist) - halfStroke) : boxDist;
                    float alpha = Mathf.Clamp01(0.5f - dist / 1.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ConfigureNineSliceImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.spriteBorder != new Vector4(18f, 18f, 18f, 18f))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(18f, 18f, 18f, 18f);
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void BuildMainScene(TMP_FontAsset font, Sprite roundedFill, Sprite roundedOutline)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = Hex("#EAE1D2");
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            EnsureEventSystem();

            var canvasGo = new GameObject("MainHubCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var hub = canvasGo.AddComponent<MainHubController>();

            // Warm Sand Beige Background
            var bgGo = CreateUI("Background", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = Hex("#EAE1D2");
            bgImg.raycastTarget = false;

            // Header (Soft Warm Cream + Tan Border, Rounded Rect)
            var headerGo = CreateUI("Header", canvasGo.transform);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(80f, -180f);
            headerRect.offsetMax = new Vector2(-80f, -44f);
            var headerBg = headerGo.AddComponent<Image>();
            headerBg.sprite = roundedFill;
            headerBg.type = Image.Type.Sliced;
            headerBg.color = Hex("#F7F2E8");

            var headerBorder = CreateUI("Border", headerRect);
            StretchFull(headerBorder.GetComponent<RectTransform>());
            var hbImg = headerBorder.AddComponent<Image>();
            hbImg.sprite = roundedOutline;
            hbImg.type = Image.Type.Sliced;
            hbImg.color = Hex("#D8CCB8");
            hbImg.raycastTarget = false;

            var badgeGo = CreateUI("Badge", headerRect);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(28f, -18f);
            badgeRect.sizeDelta = new Vector2(280f, 24f);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = roundedFill;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = Hex("#EFE7D8");
            CreateTmp(badgeRect, font, "UNITY UI & DOTWEEN SAMPLE HUB", 12f, FontStyles.Bold, Hex("#7A6B5D"), TextAlignmentOptions.Center);

            var titleGo = CreateUI("Title", headerRect);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0.7f, 0.5f);
            titleRect.offsetMin = new Vector2(28f, -12f);
            titleRect.offsetMax = new Vector2(0f, 24f);
            CreateTmpOn(titleGo, font, "UI 샘플 프로젝트 허브", 30f, FontStyles.Bold, Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            var subGo = CreateUI("Subtitle", headerRect);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(0.7f, 0f);
            subRect.offsetMin = new Vector2(28f, 14f);
            subRect.offsetMax = new Vector2(0f, 40f);
            CreateTmpOn(subGo, font, "원하는 샘플 씬을 선택하세요", 15f, FontStyles.Normal, Hex("#7A6B5D"), TextAlignmentOptions.MidlineLeft);

            // Quit Button (Rounded Rect)
            var quitGo = CreateUI("QuitButton", headerRect);
            var quitRect = quitGo.GetComponent<RectTransform>();
            quitRect.anchorMin = new Vector2(1f, 0.5f);
            quitRect.anchorMax = new Vector2(1f, 0.5f);
            quitRect.pivot = new Vector2(1f, 0.5f);
            quitRect.anchoredPosition = new Vector2(-28f, 0f);
            quitRect.sizeDelta = new Vector2(160f, 48f);
            var quitBg = quitGo.AddComponent<Image>();
            quitBg.sprite = roundedFill;
            quitBg.type = Image.Type.Sliced;
            quitBg.color = Hex("#FFFDF8");
            var quitBtn = quitGo.AddComponent<Button>();
            quitBtn.targetGraphic = quitBg;

            var quitBorder = CreateUI("Border", quitRect);
            StretchFull(quitBorder.GetComponent<RectTransform>());
            var qbImg = quitBorder.AddComponent<Image>();
            qbImg.sprite = roundedOutline;
            qbImg.type = Image.Type.Sliced;
            qbImg.color = Hex("#D6C9B4");
            qbImg.raycastTarget = false;

            CreateTmp(quitRect, font, "앱 종료 (Quit)", 15f, FontStyles.Bold, Hex("#3B3026"), TextAlignmentOptions.Center);

            // Grid of 6 Scene Cards
            var gridGo = CreateUI("SceneGrid", canvasGo.transform);
            var gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(80f, 70f);
            gridRect.offsetMax = new Vector2(-80f, -214f);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(760f, 185f);
            grid.spacing = new Vector2(32f, 24f);
            grid.childAlignment = TextAnchor.UpperCenter;

            var buttons = new List<Button>();
            var names = new List<string>();

            int cardIdx = 0;
            for (int i = 0; i < SampleScenes.Length; i++)
            {
                SampleSceneEntry entry = SampleScenes[i];
                if (!entry.ShowOnMainHub) continue;
                cardIdx++;
                Button btn = CreateSceneCard(gridRect, font, roundedFill, roundedOutline, entry, cardIdx);
                buttons.Add(btn);
                names.Add(entry.SceneName);
            }

            var so = new SerializedObject(hub);
            var btnsProp = so.FindProperty("_sceneButtons");
            btnsProp.arraySize = buttons.Count;
            for (int i = 0; i < buttons.Count; i++) btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];

            var namesProp = so.FindProperty("_sceneNames");
            namesProp.arraySize = names.Count;
            for (int i = 0; i < names.Count; i++) namesProp.GetArrayElementAtIndex(i).stringValue = names[i];

            so.FindProperty("_quitButton").objectReferenceValue = quitBtn;
            so.FindProperty("_sceneGrid").objectReferenceValue = grid;
            so.FindProperty("_sceneGridRect").objectReferenceValue = gridRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        private static Button CreateSceneCard(
            RectTransform parent,
            TMP_FontAsset font,
            Sprite roundedFill,
            Sprite roundedOutline,
            SampleSceneEntry entry,
            int index)
        {
            var cardGo = CreateUI("Card_" + entry.SceneName, parent);
            var cardRect = cardGo.GetComponent<RectTransform>();
            var cardBg = cardGo.AddComponent<Image>();
            cardBg.sprite = roundedFill;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Hex("#FFFDF8");
            var btn = cardGo.AddComponent<Button>();
            btn.targetGraphic = cardBg;

            // Rounded Border
            var borderGo = CreateUI("Border", cardRect);
            StretchFull(borderGo.GetComponent<RectTransform>());
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = roundedOutline;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Hex("#D6C9B4");
            borderImg.raycastTarget = false;

            // Index badge (warm tinted rounded rect)
            var numGo = CreateUI("IndexBadge", cardRect);
            var numRect = numGo.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 1f);
            numRect.anchorMax = new Vector2(0f, 1f);
            numRect.pivot = new Vector2(0f, 1f);
            numRect.anchoredPosition = new Vector2(28f, -24f);
            numRect.sizeDelta = new Vector2(54f, 26f);
            var numImg = numGo.AddComponent<Image>();
            numImg.sprite = roundedFill;
            numImg.type = Image.Type.Sliced;
            numImg.color = entry.AccentColor;
            CreateTmp(numRect, font, "0" + index, 13f, FontStyles.Bold, Hex("#FFFDF8"), TextAlignmentOptions.Center);

            // Title
            var titleGo = CreateUI("Title", cardRect);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 0.5f);
            titleRect.offsetMin = new Vector2(28f, -12f);
            titleRect.offsetMax = new Vector2(-148f, 28f);
            CreateTmpOn(titleGo, font, entry.Title, 24f, FontStyles.Bold, Hex("#3B3026"), TextAlignmentOptions.MidlineLeft);

            // Subtitle
            var subGo = CreateUI("Subtitle", cardRect);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0f);
            subRect.offsetMin = new Vector2(28f, 24f);
            subRect.offsetMax = new Vector2(-148f, 54f);
            CreateTmpOn(subGo, font, entry.Subtitle, 14f, FontStyles.Normal, Hex("#756657"), TextAlignmentOptions.MidlineLeft);

            // Open Tag (Rounded Rect)
            var openGo = CreateUI("OpenTag", cardRect);
            var openRect = openGo.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 0.5f);
            openRect.anchorMax = new Vector2(1f, 0.5f);
            openRect.pivot = new Vector2(1f, 0.5f);
            openRect.anchoredPosition = new Vector2(-28f, 0f);
            openRect.sizeDelta = new Vector2(98f, 40f);
            var openImg = openGo.AddComponent<Image>();
            openImg.sprite = roundedFill;
            openImg.type = Image.Type.Sliced;
            openImg.color = new Color(entry.AccentColor.r, entry.AccentColor.g, entry.AccentColor.b, 0.14f);
            CreateTmp(openRect, font, "열기 >", 15f, FontStyles.Bold, entry.AccentColor, TextAlignmentOptions.Center);

            return btn;
        }

        private static void InjectBackButtonsIntoAllSampleScenes(TMP_FontAsset font, Sprite roundedFill, Sprite roundedOutline)
        {
            for (int i = 0; i < SampleScenes.Length; i++)
            {
                SampleSceneEntry entry = SampleScenes[i];
                Scene scene = EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);

                // Clean up cross-scene links if present
                RemoveCrossSceneNavButtons(scene);

                // Remove existing BackToMainCanvas if re-running
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "BackToMainCanvas")
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                    }
                }

                if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                {
                    EnsureEventSystem();
                }

                var canvasGo = new GameObject("BackToMainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var backComp = canvasGo.AddComponent<BackToMainButton>();

                var btnGo = CreateUI("Btn_BackToMain", canvasGo.transform);
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.anchorMin = entry.BackAnchor;
                btnRect.anchorMax = entry.BackAnchor;
                btnRect.pivot = entry.BackPivot;
                btnRect.anchoredPosition = entry.BackOffset;
                btnRect.sizeDelta = new Vector2(142f, 40f);

                // Warm Rounded-Rectangle Ivory Fill + Tan Border + Espresso Text
                var bgImg = btnGo.AddComponent<Image>();
                bgImg.sprite = roundedFill;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Hex("#FFFDF8");

                var borderGo = CreateUI("Border", btnRect);
                StretchFull(borderGo.GetComponent<RectTransform>());
                var borderImg = borderGo.AddComponent<Image>();
                borderImg.sprite = roundedOutline;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = Hex("#D6C9B4");
                borderImg.raycastTarget = false;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = bgImg;

                CreateTmp(btnRect, font, "< 메인으로", 15f, FontStyles.Bold, Hex("#3B3026"), TextAlignmentOptions.Center);

                var so = new SerializedObject(backComp);
                so.FindProperty("_button").objectReferenceValue = btn;
                so.FindProperty("_mainSceneName").stringValue = "MainScene";
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, entry.ScenePath);
            }
        }

        private static void RemoveCrossSceneNavButtons(Scene scene)
        {
            string[] targetNames = { "Btn_GoToSceneA", "Btn_GoToButtonScene" };
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    for (int i = 0; i < targetNames.Length; i++)
                    {
                        if (t.name == targetNames[i])
                        {
                            UnityEngine.Object.DestroyImmediate(t.gameObject);
                            break;
                        }
                    }
                }
            }
        }

        private static void RegisterBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            for (int i = 0; i < SampleScenes.Length; i++)
            {
                list.Add(new EditorBuildSettingsScene(SampleScenes[i].ScenePath, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
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
            StretchFull(go.GetComponent<RectTransform>());
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

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }
}
