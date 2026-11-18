using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LogisticsTycoon.UI.Editor
{
    public static class ButtonSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ButtonScene.unity";
        private const string PrefabDir = "Assets/Prefabs/UI";
        private const string RipplePrefabPath = PrefabDir + "/RippleEffect.prefab";
        private const string FloatingTextPrefabPath = PrefabDir + "/FloatingText.prefab";
        private const string SparkleBurstPrefabPath = PrefabDir + "/SparkleBurst.prefab";
        private const string ExhaustSmokePuffPrefabPath = PrefabDir + "/ExhaustSmokePuff.prefab";
        private const string LaserScanBeamPrefabPath = PrefabDir + "/LaserScanBeam.prefab";
        private const string SpritesDir = "Assets/Prefabs/UI/Sprites";
        private const string FontPath = "Assets/Fonts/MALGUN SDF.asset";
        private const string FloatingOutlineMatPath = "Assets/Fonts/MALGUN SDF - FloatingOutline.mat";

        private static TMP_FontAsset _font;

        private static TMP_FontAsset GetDefaultFont()
        {
            if (_font == null)
            {
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (_font == null)
                {
                    Debug.LogWarning($"[ButtonSceneBuilder] TMP_FontAsset을 찾을 수 없습니다: {FontPath}. TMP 기본 폰트를 사용합니다.");
                    _font = TMP_Settings.defaultFontAsset;
                }
            }
            return _font;
        }

        /// <summary>
        /// 플로팅 텍스트용 외곽선 머티리얼 프리셋 (딥 차콜 브라운 볼드 외곽선)
        /// </summary>
        private static Material GetOrCreateFloatingOutlineMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null) return null;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(FloatingOutlineMatPath);
            if (mat == null)
            {
                mat = new Material(font.material);
                AssetDatabase.CreateAsset(mat, FloatingOutlineMatPath);
            }
            else
            {
                mat.shader = font.material.shader;
                mat.CopyPropertiesFromMaterial(font.material);
            }

            mat.name = Path.GetFileNameWithoutExtension(FloatingOutlineMatPath);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.22f, 0.18f, 0.15f, 0.95f));
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static TextMeshProUGUI AddTmp(
            GameObject go,
            string text,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions align,
            Color color,
            Material sharedMaterial = null)
        {
            TMP_FontAsset font = GetDefaultFont();
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            Material mat = sharedMaterial != null ? sharedMaterial : (font != null ? font.material : null);
            if (mat != null) tmp.fontSharedMaterial = mat;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        [MenuItem("Tools/UI Showcase/Setup ButtonScene")]
        public static void SetupButtonScene()
        {
            Debug.Log("<color=#E67E22>[ButtonSceneBuilder]</color> 시작: 따뜻한 캐주얼 웜 톤(Warm Palette) UI 쇼케이스 자동 구축...");

            EnsureDirectories();
            Sprite circleSprite = GetOrCreateCircleSprite();
            Sprite roundedBoxSprite = GetOrCreateRoundedBoxSprite();
            Sprite vignetteSprite = GetOrCreateVignetteSprite();

            // FX Prefabs 생성/갱신 (따뜻한 웜 톤 팔레트 동기화)
            GameObject ripplePrefab = CreateOrUpdateRipplePrefab(circleSprite);
            GameObject floatingTextPrefab = CreateOrUpdateFloatingTextPrefab();
            GameObject sparkleBurstPrefab = CreateOrUpdateSparkleBurstPrefab(circleSprite);
            GameObject exhaustSmokePrefab = CreateOrUpdateExhaustSmokePrefab(circleSprite);
            GameObject laserScanBeamPrefab = CreateOrUpdateLaserScanBeamPrefab(roundedBoxSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Scene 열기
            Scene scene;
            if (File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
            SceneManager.SetActiveScene(scene);

            // 기존 Hierarchy 초기화 (카메라 및 조명 제외)
            ClearSceneHierarchy(scene);

            // 1. EventSystem 확인 및 생성
            EnsureEventSystem();

            // 2. Main Canvas 생성
            Canvas canvas = CreateMainCanvas();
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // 3. 소프트 웜 크림 베이지 배경 (Background Image 단색화 & Overlay 제거)
            CreateBackground(canvasRect);

            // FX Container 생성 (모든 UI보다 위에 오버레이되도록 최상단 배치)
            GameObject fxContainerObj = CreateUIElement("FX_Container", canvasRect);
            RectTransform fxContainerRect = fxContainerObj.GetComponent<RectTransform>();
            SetStretch(fxContainerRect);

            // Header / Title Panel (딥 네이비 슬레이트 카드 + 웜 골드/아이보리 텍스트)
            TMP_Text currentModeText = CreateHeader(canvasRect, roundedBoxSprite);

            // Center Panel (밀크 화이트 카드 + 머스타드 옐로우 배송 버튼 & 서브 버튼 3종)
            CreateCenterShowcase(
                canvasRect,
                ripplePrefab,
                floatingTextPrefab,
                sparkleBurstPrefab,
                exhaustSmokePrefab,
                laserScanBeamPrefab,
                fxContainerRect,
                roundedBoxSprite,
                circleSprite,
                out UIButtonFeedback mainFeedback,
                out UIButtonFeedback[] subFeedbacks);

            // Right Panel (밀크 화이트 카드 + 10종 파스텔 웜 톤 모드 프리셋 + 주문 폭주 피버 버튼)
            CreateControlPanel(
                canvasRect,
                roundedBoxSprite,
                out Button btnOrderRush,
                out TMP_Text txtOrderRush,
                out Image imgOrderRush,
                out Button btnJelly,
                out Button btnDepth,
                out Button btnStamp,
                out Button btnNeon,
                out Button btnGentle,
                out Button btnConveyor,
                out Button btnBoxJolt,
                out Button btnBarcode,
                out Button btnTruckRev,
                out Button btnTape,
                out Button btnTogRipple,
                out TMP_Text txtTogRipple,
                out Button btnTogFloat,
                out TMP_Text txtTogFloat,
                out Button btnTogSparkle,
                out TMP_Text txtTogSparkle,
                out Button btnReset);

            // 4. Order Rush Fever Overlay 생성 (FX_Container 바로 아래, 전체 화면 스트레칭)
            GameObject orderRushObj = CreateUIElement("OrderRushOverlay", canvasRect);
            RectTransform orderRushRt = orderRushObj.GetComponent<RectTransform>();
            SetStretch(orderRushRt);

            CanvasGroup rushCg = orderRushObj.AddComponent<CanvasGroup>();
            rushCg.blocksRaycasts = false;
            rushCg.interactable = false;
            rushCg.alpha = 0f;

            GameObject borderObj = CreateUIElement("PulsingRedBorder", orderRushRt);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            SetStretch(borderRt);

            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = vignetteSprite;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(0.92f, 0.18f, 0.15f, 1f);
            borderImg.raycastTarget = false;

            OrderRushOverlay orderRushOverlay = orderRushObj.AddComponent<OrderRushOverlay>();
            orderRushOverlay.SetReferences(rushCg, borderImg, borderRt);

            // 5. ButtonShowcaseController 설정
            GameObject controllerObj = new GameObject("ShowcaseController");
            ButtonShowcaseController controller = controllerObj.AddComponent<ButtonShowcaseController>();
            controller.SetReferences(
                mainFeedback,
                subFeedbacks,
                currentModeText,
                btnJelly,
                btnDepth,
                btnStamp,
                btnNeon,
                btnGentle,
                btnConveyor,
                btnBoxJolt,
                btnBarcode,
                btnTruckRev,
                btnTape,
                btnTogRipple,
                txtTogRipple,
                btnTogFloat,
                txtTogFloat,
                btnTogSparkle,
                txtTogSparkle,
                btnReset,
                orderRushOverlay,
                btnOrderRush,
                txtOrderRush,
                imgOrderRush
            );

            // 계층 정렬: OrderRushOverlay는 FX_Container 바로 아래, FX_Container는 최상단(LastSibling)
            orderRushObj.transform.SetAsLastSibling();
            fxContainerObj.transform.SetAsLastSibling();

            // 씬 저장
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#2ECC71>[ButtonSceneBuilder]</color> 성공: 따뜻한 캐주얼 웜 톤 ButtonScene 구축 및 저장 완료!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
            }
            if (!Directory.Exists(SpritesDir))
            {
                Directory.CreateDirectory(SpritesDir);
            }
            AssetDatabase.Refresh();
        }

        #region Resource Loading & Sprites Generation

        private static Sprite GetOrCreateCircleSprite()
        {
            string path = SpritesDir + "/CircleSprite.png";
            if (!File.Exists(path))
            {
                int size = 128;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color transparent = new Color(0, 0, 0, 0);
                float radius = size * 0.5f - 1.5f;
                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius)
                        {
                            float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                        else
                        {
                            tex.SetPixel(x, y, transparent);
                        }
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateRoundedBoxSprite()
        {
            string path = SpritesDir + "/RoundedBoxSprite.png";
            if (!File.Exists(path))
            {
                int size = 64;
                int cornerRadius = 14;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color transparent = new Color(0, 0, 0, 0);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int cx = x < cornerRadius ? cornerRadius : (x >= size - cornerRadius ? size - cornerRadius - 1 : x);
                        int cy = y < cornerRadius ? cornerRadius : (y >= size - cornerRadius ? size - cornerRadius - 1 : y);
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                        if (dist <= cornerRadius)
                        {
                            float alpha = Mathf.Clamp01(cornerRadius - dist + 0.5f);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                        else
                        {
                            tex.SetPixel(x, y, transparent);
                        }
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteBorder = new Vector4(16, 16, 16, 16); // 9-sliced
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateVignetteSprite()
        {
            string path = SpritesDir + "/VignetteBorderSprite.png";
            if (!File.Exists(path))
            {
                int size = 128;
                int borderThickness = 12;
                int fadeDepth = 48;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int distFromEdgeX = Mathf.Min(x, size - 1 - x);
                        int distFromEdgeY = Mathf.Min(y, size - 1 - y);
                        int distFromEdge = Mathf.Min(distFromEdgeX, distFromEdgeY);

                        float alpha = 0f;
                        if (distFromEdge < borderThickness)
                        {
                            alpha = 1f;
                        }
                        else if (distFromEdge < fadeDepth)
                        {
                            float t = (float)(distFromEdge - borderThickness) / (fadeDepth - borderThickness);
                            alpha = 0.5f * (1f + Mathf.Cos(t * Mathf.PI));
                        }
                        else
                        {
                            alpha = 0f;
                        }

                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteBorder = new Vector4(fadeDepth, fadeDepth, fadeDepth, fadeDepth); // 9-sliced
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        #endregion

        #region FX Prefab Builders

        private static GameObject CreateOrUpdateRipplePrefab(Sprite circleSprite)
        {
            GameObject go = new GameObject("RippleEffect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RippleEffect));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.sprite = circleSprite;
            img.color = new Color(1f, 0.98f, 0.92f, 0.65f); // 소프트 웜 크림 반투명
            img.raycastTarget = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, RipplePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdateFloatingTextPrefab()
        {
            GameObject go = new GameObject("FloatingText", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 70f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // 탐스러운 따뜻한 꿀벌 옐로우 + 딥 차콜 브라운 볼드 외곽선(머티리얼 프리셋)
            Material outlineMat = GetOrCreateFloatingOutlineMaterial(GetDefaultFont());
            AddTmp(go, "+100 EXP", 34f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1.0f, 0.85f, 0.25f, 1f), outlineMat);
            go.AddComponent<FloatingText>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, FloatingTextPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdateSparkleBurstPrefab(Sprite circleSprite)
        {
            GameObject go = new GameObject("SparkleBurst", typeof(RectTransform), typeof(SparkleBurst));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50f, 50f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, SparkleBurstPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdateExhaustSmokePrefab(Sprite circleSprite)
        {
            GameObject go = new GameObject("ExhaustSmokePuff", typeof(RectTransform), typeof(ExhaustSmokePuff));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50f, 50f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ExhaustSmokePuffPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdateLaserScanBeamPrefab(Sprite roundedSprite)
        {
            GameObject go = new GameObject("LaserScanBeam", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LaserScanBeam));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 10f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = new Color(0.92f, 0.25f, 0.20f, 0.95f); // 웜 레드 레이저
            img.raycastTarget = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, LaserScanBeamPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        #endregion

        #region Scene Hierarchy Construction

        private static void ClearSceneHierarchy(Scene targetScene)
        {
            var roots = targetScene.IsValid() ? targetScene.GetRootGameObjects() : SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                {
                    continue; // 카메라 및 기본 라이트는 유지
                }
                if (root.name == "BackToMainCanvas")
                {
                    continue; // MainHubAndBackBuilder가 주입한 '메인으로' 버튼 유지
                }
                Object.DestroyImmediate(root);
            }
        }

        private static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                es = esObj.GetComponent<UnityEngine.EventSystems.EventSystem>();
            }

            // 신형 Input System 모듈 타입 탐색
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standalone != null)
                {
                    Object.DestroyImmediate(standalone);
                }
                if (es.GetComponent(inputSystemType) == null)
                {
                    es.gameObject.AddComponent(inputSystemType);
                }
            }
            else
            {
                if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
        }

        private static Canvas CreateMainCanvas()
        {
            GameObject canvasObj = new GameObject("ShowcaseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateBackground(RectTransform canvasRect)
        {
            // 1. 눈이 편안하고 아늑한 소프트 웜 크림 베이지 배경 (#F4EEDD / Color(0.95f, 0.92f, 0.86f, 1f))
            GameObject bgObj = CreateUIElement("Background", canvasRect);
            RectTransform rt = bgObj.GetComponent<RectTransform>();
            SetStretch(rt);

            Image img = bgObj.AddComponent<Image>();
            img.color = new Color(0.95f, 0.92f, 0.86f, 1f);
            img.raycastTarget = false;
        }

        private static TMP_Text CreateHeader(RectTransform canvasRect, Sprite roundedSprite)
        {
            GameObject headerObj = CreateUIElement("HeaderPanel", canvasRect);
            RectTransform rt = headerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(-80f, 96f);

            // 헤더 카드 바닥 소프트 섀도우
            GameObject headerShadow = CreateUIElement("HeaderShadow", rt);
            RectTransform shadowRt = headerShadow.GetComponent<RectTransform>();
            SetStretch(shadowRt);
            shadowRt.anchoredPosition = new Vector2(0f, -4f);
            Image sImg = headerShadow.AddComponent<Image>();
            sImg.sprite = roundedSprite;
            sImg.type = Image.Type.Sliced;
            sImg.color = new Color(0.12f, 0.15f, 0.22f, 0.35f);
            sImg.raycastTarget = false;

            // 헤더 카드 본체 (딥 네이비 슬레이트)
            Image headerImg = headerObj.AddComponent<Image>();
            headerImg.sprite = roundedSprite;
            headerImg.type = Image.Type.Sliced;
            headerImg.color = new Color(0.18f, 0.23f, 0.32f, 0.95f);

            // Title
            GameObject titleObj = CreateUIElement("TitleText", rt);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.46f);
            titleRt.anchorMax = new Vector2(1f, 0.96f);
            titleRt.sizeDelta = Vector2.zero;
            AddTmp(titleObj, "LOGISTICS TYCOON - BUTTON INTERACTION SHOWCASE", 26f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(1.0f, 0.96f, 0.90f, 1f)); // #FFF6E5 웜 골드/아이보리

            // Status Text
            GameObject statusObj = CreateUIElement("StatusText", rt);
            RectTransform statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 0.06f);
            statusRt.anchorMax = new Vector2(1f, 0.48f);
            statusRt.sizeDelta = Vector2.zero;
            return AddTmp(statusObj,
                "[선택 모드]: <b>1. 젤리 스쿼시 (Jelly Squash)</b> | [활성 FX]: 물결 리플 ON | 플로팅 텍스트 ON",
                17f, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(0.95f, 0.88f, 0.76f, 1f)); // 소프트 웜 크림
        }

        private static void CreateCenterShowcase(
            RectTransform canvasRect,
            GameObject ripplePrefab,
            GameObject floatingTextPrefab,
            GameObject sparklePrefab,
            GameObject exhaustSmokePrefab,
            GameObject laserBeamPrefab,
            RectTransform fxContainer,
            Sprite roundedSprite,
            Sprite circleSprite,
            out UIButtonFeedback mainFeedback,
            out UIButtonFeedback[] subFeedbacks)
        {
            // Center Panel (화면 좌측~중앙 차지 밀크 화이트 카드)
            GameObject centerPanel = CreateUIElement("CenterShowcasePanel", canvasRect);
            RectTransform centerRt = centerPanel.GetComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0f, 0f);
            centerRt.anchorMax = new Vector2(0.64f, 1f);
            centerRt.offsetMin = new Vector2(40f, 30f);
            centerRt.offsetMax = new Vector2(-15f, -135f);

            // Center Panel 소프트 바닥 그림자 (Warm Shadow)
            GameObject centerShadow = CreateUIElement("CenterPanelShadow", centerRt);
            RectTransform centerShadowRt = centerShadow.GetComponent<RectTransform>();
            SetStretch(centerShadowRt);
            centerShadowRt.anchoredPosition = new Vector2(0f, -6f);
            Image centerShadowImg = centerShadow.AddComponent<Image>();
            centerShadowImg.sprite = roundedSprite;
            centerShadowImg.type = Image.Type.Sliced;
            centerShadowImg.color = new Color(0.72f, 0.66f, 0.58f, 0.35f);
            centerShadowImg.raycastTarget = false;

            // Center Panel 배경 이미지 (밀크 화이트 카드)
            Image centerImg = centerPanel.AddComponent<Image>();
            centerImg.sprite = roundedSprite;
            centerImg.type = Image.Type.Sliced;
            centerImg.color = new Color(0.99f, 0.98f, 0.96f, 0.95f);

            // Showcase Subtitle
            GameObject hintObj = CreateUIElement("HintText", centerRt);
            RectTransform hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0.88f);
            hintRt.anchorMax = new Vector2(0.5f, 0.88f);
            hintRt.sizeDelta = new Vector2(650f, 40f);
            AddTmp(hintObj, "아래의 버튼들을 클릭하여 손맛(컨베이어/박스덜컹/레이저/배송출발/테이프)을 체감해보세요!",
                20f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(0.36f, 0.39f, 0.44f, 1f)); // 가독성 높은 소프트 슬레이트 차콜

            // 1. 메인 대형 버튼 (CLICK ME!) - 레퍼런스의 '배송 보내기' 스타일
            GameObject mainBtnObj = CreateUIElement("MainBigButton", centerRt);
            RectTransform mainBtnRt = mainBtnObj.GetComponent<RectTransform>();
            mainBtnRt.anchorMin = new Vector2(0.5f, 0.56f);
            mainBtnRt.anchorMax = new Vector2(0.5f, 0.56f);
            mainBtnRt.sizeDelta = new Vector2(380f, 115f);

            // 바닥 그림자 (Shadow) - 꿀벌 옐로우 3D 뎁스 림
            GameObject shadowObj = CreateUIElement("Shadow", mainBtnRt);
            RectTransform shadowRt = shadowObj.GetComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.sizeDelta = Vector2.zero;
            shadowRt.anchoredPosition = new Vector2(0, -9f);
            Image shadowImg = shadowObj.AddComponent<Image>();
            shadowImg.sprite = roundedSprite;
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0.85f, 0.62f, 0.12f, 1f); // 3D 뎁스 림
            shadowImg.raycastTarget = false;

            // 본체 (Body) - 탐스럽고 따뜻한 꿀벌 머스타드 옐로우
            GameObject bodyObj = CreateUIElement("Body", mainBtnRt);
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.sizeDelta = Vector2.zero;
            bodyRt.anchoredPosition = Vector2.zero;
            Image bodyImg = bodyObj.AddComponent<Image>();
            bodyImg.sprite = roundedSprite;
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = new Color(1.0f, 0.78f, 0.22f, 1f); // #FFC837

            // 본체 내부 Glow
            GameObject glowObj = CreateUIElement("Glow", bodyRt);
            RectTransform glowRt = glowObj.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.sizeDelta = new Vector2(8f, 8f);
            glowRt.anchoredPosition = Vector2.zero;
            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = roundedSprite;
            glowImg.type = Image.Type.Sliced;
            glowImg.color = new Color(1f, 0.95f, 0.60f, 0f); // Hidden initially
            glowImg.raycastTarget = false;

            // 라벨 (딥 차콜 브라운 볼드)
            GameObject mainLabelObj = CreateUIElement("Label", bodyRt);
            RectTransform labelRt = mainLabelObj.GetComponent<RectTransform>();
            SetStretch(labelRt);
            AddTmp(mainLabelObj, "배송 보내기 (CLICK ME!)", 30f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Color(0.22f, 0.18f, 0.15f, 1f)); // 딥 차콜 브라운

            // 메인 버튼 Button & UIButtonFeedback 세팅
            Button mainButton = mainBtnObj.AddComponent<Button>();
            mainButton.targetGraphic = bodyImg;
            mainButton.transition = Selectable.Transition.None;

            mainFeedback = mainBtnObj.AddComponent<UIButtonFeedback>();
            mainFeedback.SetReferences(bodyRt, shadowRt, glowImg, fxContainer);
            mainFeedback.SetPrefabs(ripplePrefab, floatingTextPrefab, sparklePrefab, exhaustSmokePrefab, laserBeamPrefab);
            mainFeedback.SetDynamicSprites(circleSprite, roundedSprite);
            mainFeedback.SetMotionPreset(MotionPreset.JellySquash);

            // 2. 서브 버튼 3종 영역 (Horizontal Row)
            GameObject subRowObj = CreateUIElement("SubButtonsRow", centerRt);
            RectTransform subRowRt = subRowObj.GetComponent<RectTransform>();
            subRowRt.anchorMin = new Vector2(0.5f, 0.26f);
            subRowRt.anchorMax = new Vector2(0.5f, 0.26f);
            subRowRt.sizeDelta = new Vector2(640f, 95f);

            HorizontalLayoutGroup hlg = subRowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 35f;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            subFeedbacks = new UIButtonFeedback[3];

            // 서브 1: 사각 크라프트 택배 박스 버튼
            subFeedbacks[0] = CreateSubButton(
                subRowRt,
                "SubBtn_Parcel",
                new Vector2(175f, 80f),
                roundedSprite,
                new Color(0.78f, 0.54f, 0.35f),         // 골판지 크라프트 브라운
                new Color(0.58f, 0.38f, 0.22f, 1f),     // 3D 림 브라운
                Color.white,
                "PARCEL",
                20,
                ripplePrefab,
                floatingTextPrefab,
                sparklePrefab,
                exhaustSmokePrefab,
                laserBeamPrefab,
                circleSprite,
                roundedSprite,
                fxContainer);

            // 서브 2: 원형 트럭 배송 출발 버튼
            subFeedbacks[1] = CreateSubButton(
                subRowRt,
                "SubBtn_RevUp",
                new Vector2(80f, 80f),
                circleSprite,
                new Color(0.92f, 0.45f, 0.15f),          // 활기찬 트럭 오렌지
                new Color(0.72f, 0.32f, 0.10f, 1f),     // 오렌지 림
                Color.white,                             // 화이트 트럭 아이콘
                "▶",
                30,
                ripplePrefab,
                floatingTextPrefab,
                sparklePrefab,
                exhaustSmokePrefab,
                laserBeamPrefab,
                circleSprite,
                roundedSprite,
                fxContainer);

            // 서브 3: 트럭 발송 아이콘 버튼
            subFeedbacks[2] = CreateSubButton(
                subRowRt,
                "SubBtn_Dispatch",
                new Vector2(175f, 80f),
                roundedSprite,
                new Color(0.24f, 0.36f, 0.52f),         // 유니폼 소프트 네이비 블루
                new Color(0.16f, 0.24f, 0.38f, 1f),     // 네이비 림
                Color.white,
                "DISPATCH",
                20,
                ripplePrefab,
                floatingTextPrefab,
                sparklePrefab,
                exhaustSmokePrefab,
                laserBeamPrefab,
                circleSprite,
                roundedSprite,
                fxContainer);
        }

        private static UIButtonFeedback CreateSubButton(
            RectTransform parent,
            string name,
            Vector2 size,
            Sprite sprite,
            Color baseColor,
            Color shadowColor,
            Color textColor,
            string labelText,
            int fontSize,
            GameObject ripplePrefab,
            GameObject floatingTextPrefab,
            GameObject sparklePrefab,
            GameObject exhaustSmokePrefab,
            GameObject laserPrefab,
            Sprite circleSprite,
            Sprite roundedSprite,
            RectTransform fxContainer)
        {
            GameObject root = CreateUIElement(name, parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = size;

            // Shadow
            GameObject shadowObj = CreateUIElement("Shadow", rootRt);
            RectTransform shadowRt = shadowObj.GetComponent<RectTransform>();
            SetStretch(shadowRt);
            shadowRt.anchoredPosition = new Vector2(0, -6f);
            Image shadowImg = shadowObj.AddComponent<Image>();
            shadowImg.sprite = sprite;
            shadowImg.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            shadowImg.color = shadowColor;
            shadowImg.raycastTarget = false;

            // Body
            GameObject bodyObj = CreateUIElement("Body", rootRt);
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            SetStretch(bodyRt);
            Image bodyImg = bodyObj.AddComponent<Image>();
            bodyImg.sprite = sprite;
            bodyImg.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            bodyImg.color = baseColor;

            // Glow
            GameObject glowObj = CreateUIElement("Glow", bodyRt);
            RectTransform glowRt = glowObj.GetComponent<RectTransform>();
            SetStretch(glowRt);
            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = sprite;
            glowImg.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            glowImg.color = new Color(1f, 1f, 1f, 0f);
            glowImg.raycastTarget = false;

            // Label
            GameObject labelObj = CreateUIElement("Label", bodyRt);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            SetStretch(labelRt);
            AddTmp(labelObj, labelText, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor);

            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = bodyImg;
            btn.transition = Selectable.Transition.None;

            UIButtonFeedback feedback = root.AddComponent<UIButtonFeedback>();
            feedback.SetReferences(bodyRt, shadowRt, glowImg, fxContainer);
            feedback.SetPrefabs(ripplePrefab, floatingTextPrefab, sparklePrefab, exhaustSmokePrefab, laserPrefab);
            feedback.SetDynamicSprites(circleSprite, roundedSprite);
            feedback.SetMotionPreset(MotionPreset.JellySquash);

            return feedback;
        }

        private static void CreateControlPanel(
            RectTransform canvasRect,
            Sprite roundedSprite,
            out Button btnOrderRush,
            out TMP_Text txtOrderRush,
            out Image imgOrderRush,
            out Button btnJelly,
            out Button btnDepth,
            out Button btnStamp,
            out Button btnNeon,
            out Button btnGentle,
            out Button btnConveyor,
            out Button btnBoxJolt,
            out Button btnBarcode,
            out Button btnTruckRev,
            out Button btnTape,
            out Button btnTogRipple,
            out TMP_Text txtTogRipple,
            out Button btnTogFloat,
            out TMP_Text txtTogFloat,
            out Button btnTogSparkle,
            out TMP_Text txtTogSparkle,
            out Button btnReset)
        {
            TMP_FontAsset uiFont = GetDefaultFont();

            // Right Panel Root
            GameObject panelObj = CreateUIElement("RightControlPanel", canvasRect);
            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.64f, 0f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.offsetMin = new Vector2(15f, 30f);
            panelRt.offsetMax = new Vector2(-40f, -135f);

            // Right Panel 바닥 소프트 그림자 (Warm Shadow)
            GameObject panelShadow = CreateUIElement("RightControlPanelShadow", panelRt);
            RectTransform panelShadowRt = panelShadow.GetComponent<RectTransform>();
            SetStretch(panelShadowRt);
            panelShadowRt.anchoredPosition = new Vector2(0f, -6f);
            Image panelShadowImg = panelShadow.AddComponent<Image>();
            panelShadowImg.sprite = roundedSprite;
            panelShadowImg.type = Image.Type.Sliced;
            panelShadowImg.color = new Color(0.72f, 0.66f, 0.58f, 0.35f);
            panelShadowImg.raycastTarget = false;

            // Panel Background (밀크 화이트 카드)
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.sprite = roundedSprite;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.99f, 0.98f, 0.96f, 0.95f);

            VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(22, 22, 16, 16);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Section 0: Fever Mode (Order Rush)
            CreateSectionLabel("SPECIAL FEVER MODE", panelRt, uiFont);
            btnOrderRush = CreatePanelButton("Btn_OrderRush", "주문 폭주: OFF", new Color(0.85f, 0.34f, 0.25f), panelRt, roundedSprite, uiFont, out txtOrderRush);
            imgOrderRush = btnOrderRush.GetComponent<Image>();

            // Section 1: Motion Presets (10 Kinds)
            CreateSectionLabel("MOTION PRESETS (10 KINDS)", panelRt, uiFont);

            // 2열 그리드 컨테이너 생성
            GameObject gridObj = CreateUIElement("PresetsGrid", panelRt);
            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(0, 240f);

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(295f, 44f);
            glg.spacing = new Vector2(10f, 6f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            // 1~5 기본 프리셋 (따뜻한 파스텔 웜 톤)
            btnJelly = CreatePanelButton("Btn_Jelly", "1. 젤리 스쿼시", new Color(0.38f, 0.70f, 0.35f), gridRt, roundedSprite, uiFont);       // 세이지 클로버 그린 (#62B358)
            btnDepth = CreatePanelButton("Btn_Depth", "2. 3D 뎁스 스위치", new Color(0.28f, 0.42f, 0.60f), gridRt, roundedSprite, uiFont);   // 소프트 네이비 블루
            btnStamp = CreatePanelButton("Btn_Stamp", "3. 스탬프 쾅!", new Color(0.92f, 0.48f, 0.26f), gridRt, roundedSprite, uiFont);       // 테라코타 웜 오렌지 (#EB7A42)
            btnNeon = CreatePanelButton("Btn_Neon", "4. 네온 펄스", new Color(0.62f, 0.45f, 0.72f), gridRt, roundedSprite, uiFont);          // 소프트 라벤더 웜 바이올렛
            btnGentle = CreatePanelButton("Btn_Gentle", "5. 심플 팝", new Color(0.55f, 0.48f, 0.44f), gridRt, roundedSprite, uiFont);        // 웜 슬레이트 모카

            // 6~10 물류 특화 신규 프리셋 (따뜻한 파스텔 웜 톤)
            btnConveyor = CreatePanelButton("Btn_Conveyor", "6. 컨베이어 스냅", new Color(0.32f, 0.48f, 0.58f), gridRt, roundedSprite, uiFont); // 캐주얼 스틸 블루
            btnBoxJolt = CreatePanelButton("Btn_BoxJolt", "7. 박스 덜컹 들썩임", new Color(0.72f, 0.52f, 0.35f), gridRt, roundedSprite, uiFont); // 골판지 크라프트 브라운 (#B88559)
            btnBarcode = CreatePanelButton("Btn_Barcode", "8. 바코드 레이저", new Color(0.85f, 0.34f, 0.28f), gridRt, roundedSprite, uiFont);    // 브릭 레드 / 칠리 오렌지
            btnTruckRev = CreatePanelButton("Btn_TruckRev", "9. 배송 출발 (Rev-Up)", new Color(0.92f, 0.45f, 0.15f), gridRt, roundedSprite, uiFont); // 트럭 오렌지 (#EB7326)
            btnTape = CreatePanelButton("Btn_Tape", "10. 테이프 와이프", new Color(0.80f, 0.68f, 0.38f), gridRt, roundedSprite, uiFont);       // 크라프트 테이프 베이지

            // Section 2: FX Toggles
            CreateSectionLabel("ADD-ON FX TOGGLE", panelRt, uiFont);

            btnTogRipple = CreatePanelButton("Btn_TogRipple", "물결 리플: ON", new Color(0.32f, 0.58f, 0.50f), panelRt, roundedSprite, uiFont, out txtTogRipple); // 세이지 틸
            btnTogFloat = CreatePanelButton("Btn_TogFloat", "플로팅 텍스트: ON", new Color(0.82f, 0.55f, 0.25f), panelRt, roundedSprite, uiFont, out txtTogFloat); // 웜 앰버
            btnTogSparkle = CreatePanelButton("Btn_TogSparkle", "스파클 파티클: OFF", new Color(0.50f, 0.52f, 0.56f), panelRt, roundedSprite, uiFont, out txtTogSparkle); // 소프트 슬레이트

            // Section 3: Reset
            CreateSectionLabel("SYSTEM", panelRt, uiFont);
            btnReset = CreatePanelButton("Btn_Reset", "기본값으로 초기화", new Color(0.82f, 0.35f, 0.32f), panelRt, roundedSprite, uiFont); // 웜 코랄 레드
        }

        private static void CreateSectionLabel(string title, RectTransform parent, TMP_FontAsset font)
        {
            GameObject obj = CreateUIElement("Section_" + title, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 26f);

            AddTmp(obj, title, 16f, FontStyles.Bold, TextAlignmentOptions.Left,
                new Color(0.25f, 0.22f, 0.20f, 1f)); // 딥 차콜 브라운 (선명한 가독성)
        }

        private static Button CreatePanelButton(
            string name,
            string label,
            Color color,
            RectTransform parent,
            Sprite roundedSprite,
            TMP_FontAsset font)
        {
            return CreatePanelButton(name, label, color, parent, roundedSprite, font, out _);
        }

        private static Button CreatePanelButton(
            string name,
            string label,
            Color color,
            RectTransform parent,
            Sprite roundedSprite,
            TMP_FontAsset font,
            out TMP_Text outText)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 44f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.12f;
            cb.pressedColor = color * 0.88f;
            btn.colors = cb;

            GameObject textObj = CreateUIElement("Text", rt);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            SetStretch(textRt);

            outText = AddTmp(textObj, label, 15f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            return btn;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
