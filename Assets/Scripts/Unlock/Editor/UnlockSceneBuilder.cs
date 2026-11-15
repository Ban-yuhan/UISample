using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnlockSystem.Editor
{
    /// <summary>
    /// Assets/Scenes/UnlockScene.unity 및 필요한 프로시저럴 스프라이트/머티리얼 에셋을 자동 구축합니다.
    /// </summary>
    public static class UnlockSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/UnlockScene.unity";
        private const string GenDir = "Assets/Generated/Unlock";
        private const string FontPath = "Assets/Fonts/MALGUN SDF.asset";

        // 팔레트 정의
        private static readonly Color ColBg = Hex("#D7E5B2");
        private static readonly Color ColGrassTop = Hex("#9CD34A");
        private static readonly Color ColGrassGrid = Hex("#82BA32");
        private static readonly Color ColGrassLeft = Hex("#78B332");
        private static readonly Color ColGrassRight = Hex("#639924");
        private static readonly Color ColCream = Hex("#FAF5EB");
        private static readonly Color ColDarkBrown = Hex("#3B2314");
        private static readonly Color ColSubBrown = Hex("#6A4A35");
        private static readonly Color ColLime = Hex("#7AC71E");
        private static readonly Color ColLimeHighlight = Hex("#9BE33D");
        private static readonly Color ColYellow = Hex("#FFD028");
        private static readonly Color ColBadgeBeige = Hex("#F7E3BA");
        private static readonly Color ColOlive = Hex("#627941");
        private static readonly Color ColBlue = Hex("#3B82F6");
        private static readonly Color ColSkin = Hex("#FFE0BD");
        private static readonly Color ColOrange = Hex("#FF6B4A");

        private static TMP_FontAsset _uiFont;

        [MenuItem("Tools/UnlockScene/Build Unlock Scene")]
        public static void BuildScene()
        {
            EnsureFolder(GenDir);
            LoadFont();

            // 1. 프로시저럴 스프라이트 & 텍스처 생성
            Sprite sprRounded = CreateRoundedRectSprite("RoundedRect.png", 96, 20, Color.white, 22);
            Sprite sprOutline = CreateRoundedOutlineSprite("RoundedOutline.png", 96, 20, 5, Color.white, 22);
            Sprite sprCircle = CreateCircleSprite("Circle.png", 96, Color.white);

            Sprite sprIconLock = CreateLockIconSprite("Icon_Lock.png", 64);
            Sprite sprIconBolt = CreateBoltIconSprite("Icon_Bolt.png", 64);
            Sprite sprIconReset = CreateResetIconSprite("Icon_Reset.png", 64);
            Sprite sprIconSoundOn = CreateSoundIconSprite("Icon_SoundOn.png", 64, true);
            Sprite sprIconSoundOff = CreateSoundIconSprite("Icon_SoundOff.png", 64, false);
            Sprite sprIconCash = CreateCashIconSprite("Icon_Cash.png", 64);
            Sprite sprIconPadExpand = CreatePadExpandIconSprite("Icon_PadExpand.png", 96);
            Sprite sprIconSparkle = CreateSparkleSprite("Icon_Sparkle.png", 64);

            Sprite sprIconWave = CreateWaveIconSprite("Icon_Wave.png", 64);
            Sprite sprIconExpand = CreateExpandIconSprite("Icon_Expand.png", 64);
            Sprite sprIconCircleMode = CreateCircleModeIconSprite("Icon_CircleMode.png", 64);
            Sprite sprIconDrop = CreateDropIconSprite("Icon_Drop.png", 64);
            Sprite sprIconBounce = CreateBounceIconSprite("Icon_Bounce.png", 64);

            Sprite sprPadlockBody = CreatePadlockBodySprite("Padlock_Body.png", 128);
            Sprite sprPadlockShackle = CreatePadlockShackleSprite("Padlock_Shackle.png", 128);

            Texture2D texDashedZone = CreateDashedZoneTexture("DashedZone.png", 512);
            Texture2D texGlowRing = CreateGlowRingTexture("GlowRing.png", 256);
            Texture2D texDustRing = CreateDustSquareTexture("DustSquareRing.png", 256);
            Texture2D texDropShadow = CreateDropShadowTexture("DropShadow.png", 256);

            // 2. URP 머티리얼 생성
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");

            Material matGrassTop = CreateColorMaterial("Mat_GrassTop.mat", urpUnlit, ColGrassTop);
            Material matGrassLeft = CreateColorMaterial("Mat_GrassLeft.mat", urpUnlit, ColGrassLeft);
            Material matGrassRight = CreateColorMaterial("Mat_GrassRight.mat", urpUnlit, ColGrassRight);
            Material matGrassGrid = CreateColorMaterial("Mat_GrassGrid.mat", urpUnlit, new Color(ColGrassGrid.r, ColGrassGrid.g, ColGrassGrid.b, 0.55f), transparent: true);
            Material matGroundBgGrid = CreateColorMaterial("Mat_GroundBgGrid.mat", urpUnlit, new Color(0.51f, 0.62f, 0.30f, 0.22f), transparent: true);

            Shader circleShader = Shader.Find("Custom/ZoneCircleReveal");
            if (circleShader == null)
            {
                Debug.LogError("[UnlockSceneBuilder] Custom/ZoneCircleReveal 셰이더를 찾을 수 없습니다.");
                circleShader = urpUnlit;
            }
            Material matCircleReveal = CreateCircleRevealMaterial("Mat_ZoneCircleReveal.mat", circleShader);

            Material matDashedZone = CreateTextureMaterial("Mat_DashedZone.mat", urpUnlit, texDashedZone, Color.white, transparent: true);
            Material matGlowRing = CreateTextureMaterial("Mat_GlowRing.mat", urpUnlit, texGlowRing, new Color(0.75f, 1f, 0.45f, 0.9f), transparent: true);
            Material matDustRing = CreateTextureMaterial("Mat_DustRing.mat", urpUnlit, texDustRing, new Color(0.95f, 0.92f, 0.80f, 0.85f), transparent: true);
            Material matDropShadow = CreateTextureMaterial("Mat_DropShadow.mat", urpUnlit, texDropShadow, new Color(0.14f, 0.22f, 0.06f, 0.85f), transparent: true);
            Material matExpandGlow = CreateColorMaterial("Mat_ExpandGlow.mat", urpUnlit, new Color(0.92f, 1f, 0.65f, 0.9f), transparent: true);

            Material matPlayerBlue = CreateColorMaterial("Mat_PlayerBlue.mat", urpUnlit, ColBlue);
            Material matPlayerSkin = CreateColorMaterial("Mat_PlayerSkin.mat", urpUnlit, ColSkin);
            Material matPlayerOrange = CreateColorMaterial("Mat_PlayerOrange.mat", urpUnlit, ColOrange);
            Material matDarkBrown = CreateColorMaterial("Mat_DarkBrown.mat", urpUnlit, ColDarkBrown);
            Material matShadow = CreateColorMaterial("Mat_Shadow.mat", urpUnlit, new Color(0.15f, 0.22f, 0.08f, 0.25f), transparent: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. 씬 열기 / 초기화
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 4. 카메라 & 라이트
            Camera mainCam = CreateIsometricCamera();
            CreateDirectionalLight();
            CreateEventSystem();

            // 5. 배경 바닥 그리드 & 기존 해금 필드 & 미해금 점선 구역
            CreateBackgroundGrid(matGroundBgGrid);
            CreateGrassSlab("UnlockedField", new Vector3(-1.5f, 0f, 0f), 5f, 5f, 0.36f,
                matGrassTop, matGrassLeft, matGrassRight, matGrassGrid, matShadow, withGrid: true);
            GameObject lockedZone = CreateLockedZoneOutline(matDashedZone);

            // 6. 캐릭터 (PlayerVisual)
            Transform playerRoot = CreatePlayerVisual(out Transform playerBody, matPlayerBlue, matPlayerSkin, matPlayerOrange, matDarkBrown, matShadow);

            // 7. 해금 발판 (UnlockPadUI - World-Space Canvas)
            UnlockPadUI padUI = CreateUnlockPadUI(sprRounded, sprOutline, sprIconPadExpand, sprIconCash);

            // 8. 중앙 자물쇠 (CenterPadlock - World-Space Canvas)
            UnlockPadlockVisual padlockVisual = CreatePadlockVisual(sprRounded, sprOutline, sprPadlockBody, sprPadlockShackle);

            // 9. 5종 필드 해금 비주얼 + UnlockFieldAnimator
            GameObject animatorObj = new GameObject("UnlockFieldAnimator");
            UnlockFieldAnimator fieldAnimator = animatorObj.AddComponent<UnlockFieldAnimator>();
            SetupFieldAnimator(animatorObj.transform, fieldAnimator, mainCam, lockedZone,
                matGrassTop, matGrassLeft, matGrassRight, matGrassGrid,
                matCircleReveal, matExpandGlow, matGlowRing, matDustRing, matDropShadow);

            // 10. 파티클 템플릿 (지폐 비행, 스파클)
            GameObject cashTemplate = CreateBillboardSpriteTemplate("CashFlyTemplate", sprIconCash, 0.42f);
            GameObject sparkleTemplate = CreateBillboardSpriteTemplate("SparkleTemplate", sprIconSparkle, 0.34f);

            // 11. 절차적 오디오
            GameObject audioObj = new GameObject("UnlockProceduralAudio");
            audioObj.AddComponent<AudioSource>();
            UnlockProceduralAudio procAudio = audioObj.AddComponent<UnlockProceduralAudio>();

            // 12. HUD Canvas & UnlockZoneController
            GameObject ctrlObj = new GameObject("UnlockZoneController");
            UnlockZoneController controller = ctrlObj.AddComponent<UnlockZoneController>();

            CreateHudCanvas(controller, sprRounded, sprOutline, sprCircle,
                sprIconLock, sprIconBolt, sprIconReset, sprIconSoundOn, sprIconSoundOff, sprIconCash,
                sprIconWave, sprIconExpand, sprIconCircleMode, sprIconDrop, sprIconBounce);

            // Controller 핵심 레퍼런스 직렬화 연결
            SerializedObject ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("padUI").objectReferenceValue = padUI;
            ctrlSo.FindProperty("padlockVisual").objectReferenceValue = padlockVisual;
            ctrlSo.FindProperty("fieldAnimator").objectReferenceValue = fieldAnimator;
            ctrlSo.FindProperty("proceduralAudio").objectReferenceValue = procAudio;
            ctrlSo.FindProperty("playerRoot").objectReferenceValue = playerRoot;
            ctrlSo.FindProperty("playerBodyVisual").objectReferenceValue = playerBody;
            ctrlSo.FindProperty("cashFlyTemplate").objectReferenceValue = cashTemplate;
            ctrlSo.FindProperty("sparkleTemplate").objectReferenceValue = sparkleTemplate;
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            // 13. 씬 저장
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[UnlockSceneBuilder] UnlockScene.unity 자동 구축 완료!");
        }

        private static Camera CreateIsometricCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();

            cam.orthographic = true;
            cam.orthographicSize = 4.65f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColBg;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;

            Quaternion rot = Quaternion.Euler(35.264f, 45f, 0f);
            camObj.transform.rotation = rot;
            Vector3 lookTarget = new Vector3(1.0f, 0.15f, 0f);
            camObj.transform.position = lookTarget - (rot * Vector3.forward) * 20f;
            return cam;
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();

            System.Type inputSysType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSysType != null)
                es.AddComponent(inputSysType);
            else
                es.AddComponent<StandaloneInputModule>();
        }

        private static void CreateBackgroundGrid(Material matGrid)
        {
            GameObject root = new GameObject("BackgroundGrid");
            const float y = -0.37f;
            const float minX = -12f;
            const float maxX = 14f;
            const float minZ = -10f;
            const float maxZ = 10f;

            for (int x = -12; x <= 14; x++)
            {
                CreateLineQuad(root.transform, $"GridX_{x}", new Vector3(x, y, (minZ + maxZ) * 0.5f),
                    new Vector3(0.025f, 1f, maxZ - minZ), matGrid);
            }
            for (int z = -10; z <= 10; z++)
            {
                CreateLineQuad(root.transform, $"GridZ_{z}", new Vector3((minX + maxX) * 0.5f, y, z),
                    new Vector3(maxX - minX, 1f, 0.025f), matGrid);
            }
        }

        private static GameObject CreateGrassSlab(string name, Vector3 topCenter, float sizeX, float sizeZ, float thickness,
            Material matTop, Material matLeft, Material matRight, Material matGrid, Material matShadow, bool withGrid)
        {
            GameObject root = new GameObject(name);
            root.transform.position = topCenter;

            // 바닥 그림자
            if (matShadow != null)
            {
                GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shadow.name = "Shadow";
                Object.DestroyImmediate(shadow.GetComponent<Collider>());
                shadow.transform.SetParent(root.transform, false);
                shadow.transform.localPosition = new Vector3(-0.08f, -thickness - 0.005f, -0.08f);
                shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shadow.transform.localScale = new Vector3(sizeX + 0.18f, sizeZ + 0.18f, 1f);
                shadow.GetComponent<Renderer>().sharedMaterial = matShadow;
            }

            // 윗면
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Quad);
            top.name = "Top";
            Object.DestroyImmediate(top.GetComponent<Collider>());
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = Vector3.zero;
            top.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            top.transform.localScale = new Vector3(sizeX, sizeZ, 1f);
            top.GetComponent<Renderer>().sharedMaterial = matTop;

            // 좌측면 (-X 방향, 카메라에서 보이는 좌측 하단면)
            GameObject sideX = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sideX.name = "SideLeft";
            Object.DestroyImmediate(sideX.GetComponent<Collider>());
            sideX.transform.SetParent(root.transform, false);
            sideX.transform.localPosition = new Vector3(-sizeX * 0.5f, -thickness * 0.5f, 0f);
            sideX.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            sideX.transform.localScale = new Vector3(sizeZ, thickness, 1f);
            sideX.GetComponent<Renderer>().sharedMaterial = matLeft;

            // 우측면 (-Z 방향, 카메라에서 보이는 우측 하단면)
            GameObject sideZ = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sideZ.name = "SideRight";
            Object.DestroyImmediate(sideZ.GetComponent<Collider>());
            sideZ.transform.SetParent(root.transform, false);
            sideZ.transform.localPosition = new Vector3(0f, -thickness * 0.5f, -sizeZ * 0.5f);
            sideZ.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            sideZ.transform.localScale = new Vector3(sizeX, thickness, 1f);
            sideZ.GetComponent<Renderer>().sharedMaterial = matRight;

            // 내부 타일 눈금 (5x5)
            if (withGrid && matGrid != null)
            {
                int cols = Mathf.RoundToInt(sizeX);
                int rows = Mathf.RoundToInt(sizeZ);
                for (int i = 1; i < cols; i++)
                {
                    float lx = -sizeX * 0.5f + i;
                    CreateLineQuad(root.transform, $"TileGridX_{i}", new Vector3(lx, 0.003f, 0f),
                        new Vector3(0.03f, 1f, sizeZ), matGrid);
                }
                for (int j = 1; j < rows; j++)
                {
                    float lz = -sizeZ * 0.5f + j;
                    CreateLineQuad(root.transform, $"TileGridZ_{j}", new Vector3(0f, 0.003f, lz),
                        new Vector3(sizeX, 1f, 0.03f), matGrid);
                }
            }

            return root;
        }

        private static void CreateLineQuad(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false);
            q.transform.localPosition = localPos;
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(scale.x, scale.z, 1f);
            q.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject CreateLockedZoneOutline(Material matDashed)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "LockedZone";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.position = new Vector3(3.5f, -0.36f, 0f);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(5f, 5f, 1f);
            q.GetComponent<Renderer>().sharedMaterial = matDashed;
            return q;
        }

        private static Transform CreatePlayerVisual(out Transform bodyVisual, Material matBlue, Material matSkin, Material matOrange, Material matBrown, Material matShadow)
        {
            GameObject root = new GameObject("PlayerVisual");
            root.transform.position = new Vector3(-2.2f, 0f, 0.4f);
            root.transform.rotation = Quaternion.LookRotation(new Vector3(1f, 0f, -0.2f));

            // 바닥 그림자
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow";
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            shadow.transform.localScale = new Vector3(0.62f, 0.01f, 0.62f);
            shadow.GetComponent<Renderer>().sharedMaterial = matShadow;

            GameObject body = new GameObject("BodyVisual");
            body.transform.SetParent(root.transform, false);
            bodyVisual = body.transform;

            // 몸통 (파란 멜빵)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            Object.DestroyImmediate(torso.GetComponent<Collider>());
            torso.transform.SetParent(bodyVisual, false);
            torso.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            torso.transform.localScale = new Vector3(0.46f, 0.34f, 0.44f);
            torso.GetComponent<Renderer>().sharedMaterial = matBlue;

            // 머리 (피부색)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(bodyVisual, false);
            head.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            head.transform.localScale = new Vector3(0.44f, 0.42f, 0.44f);
            head.GetComponent<Renderer>().sharedMaterial = matSkin;

            // 모자 크라운 (주황 캡)
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Cap";
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.SetParent(bodyVisual, false);
            cap.transform.localPosition = new Vector3(0f, 0.91f, -0.02f);
            cap.transform.localScale = new Vector3(0.46f, 0.25f, 0.46f);
            cap.GetComponent<Renderer>().sharedMaterial = matOrange;

            // 모자 챙
            GameObject brim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brim.name = "CapBrim";
            Object.DestroyImmediate(brim.GetComponent<Collider>());
            brim.transform.SetParent(bodyVisual, false);
            brim.transform.localPosition = new Vector3(0f, 0.88f, 0.21f);
            brim.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            brim.transform.localScale = new Vector3(0.36f, 0.05f, 0.22f);
            brim.GetComponent<Renderer>().sharedMaterial = matOrange;

            // 눈 2개
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = i < 0 ? "EyeL" : "EyeR";
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(bodyVisual, false);
                eye.transform.localPosition = new Vector3(i * 0.09f, 0.77f, 0.19f);
                eye.transform.localScale = Vector3.one * 0.06f;
                eye.GetComponent<Renderer>().sharedMaterial = matBrown;
            }

            return root.transform;
        }

        private static UnlockPadUI CreateUnlockPadUI(Sprite sprRounded, Sprite sprOutline, Sprite sprExpand, Sprite sprCash)
        {
            GameObject padObj = new GameObject("UnlockPadUI");
            padObj.transform.position = new Vector3(-0.1f, 0.02f, 0f);
            padObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Canvas canvas = padObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasGroup cg = padObj.AddComponent<CanvasGroup>();

            RectTransform canvasRt = padObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(165f, 165f);
            canvasRt.localScale = Vector3.one * 0.01f;

            // 크림색 바탕 + 마스크
            Image bgImg = CreateUiImage("Background", canvasRt, sprRounded, ColCream, sliced: true);
            StretchFull(bgImg.rectTransform);
            Mask mask = bgImg.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // 충전 게이지 Fill (아래 -> 위)
            Image fillImg = CreateUiImage("Fill", bgImg.rectTransform, sprRounded, ColLime, sliced: false);
            StretchFull(fillImg.rectTransform);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Vertical;
            fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImg.fillAmount = 0f;

            // 게이지 상단 하이라이트 라인
            Image hlImg = CreateUiImage("FillHighlight", bgImg.rectTransform, null, ColLimeHighlight, sliced: false);
            hlImg.rectTransform.anchorMin = new Vector2(0f, 0f);
            hlImg.rectTransform.anchorMax = new Vector2(1f, 0f);
            hlImg.rectTransform.pivot = new Vector2(0.5f, 1f);
            hlImg.rectTransform.sizeDelta = new Vector2(0f, 8f);
            hlImg.enabled = false;

            // 다크브라운 외곽 테두리
            Image borderImg = CreateUiImage("Border", canvasRt, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(borderImg.rectTransform);

            // 상단 4방향 확장 아이콘
            Image expandIcon = CreateUiImage("ExpandIcon", canvasRt, sprExpand, ColDarkBrown, sliced: false);
            expandIcon.rectTransform.anchoredPosition = new Vector2(0f, 44f);
            expandIcon.rectTransform.sizeDelta = new Vector2(46f, 46f);

            // 중앙 텍스트 "새 구역"
            TextMeshProUGUI titleTxt = CreateUiText("Title", canvasRt, "새 구역", 24, ColDarkBrown, TextAlignmentOptions.Center);
            titleTxt.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            titleTxt.rectTransform.sizeDelta = new Vector2(140f, 34f);

            // 하단 금액 알약 배지
            Image pillBg = CreateUiImage("CostBadge", canvasRt, sprRounded, ColBadgeBeige, sliced: true);
            pillBg.rectTransform.anchoredPosition = new Vector2(0f, -44f);
            pillBg.rectTransform.sizeDelta = new Vector2(114f, 36f);

            Image pillBorder = CreateUiImage("CostBadgeBorder", pillBg.rectTransform, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(pillBorder.rectTransform);

            Image cashIcon = CreateUiImage("CashIcon", pillBg.rectTransform, sprCash, Color.white, sliced: false);
            cashIcon.rectTransform.anchoredPosition = new Vector2(-28f, 0f);
            cashIcon.rectTransform.sizeDelta = new Vector2(24f, 24f);

            TextMeshProUGUI costTxt = CreateUiText("CostText", pillBg.rectTransform, "200", 22, ColDarkBrown, TextAlignmentOptions.Center);
            costTxt.rectTransform.anchoredPosition = new Vector2(14f, 0f);
            costTxt.rectTransform.sizeDelta = new Vector2(68f, 32f);

            UnlockPadUI pad = padObj.AddComponent<UnlockPadUI>();
            SerializedObject so = new SerializedObject(pad);
            so.FindProperty("fillImage").objectReferenceValue = fillImg;
            so.FindProperty("fillHighlightImage").objectReferenceValue = hlImg;
            so.FindProperty("borderImage").objectReferenceValue = borderImg;
            so.FindProperty("costText").objectReferenceValue = costTxt;
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return pad;
        }

        private static UnlockPadlockVisual CreatePadlockVisual(Sprite sprRounded, Sprite sprOutline, Sprite sprBody, Sprite sprShackle)
        {
            GameObject root = new GameObject("CenterPadlock");
            root.transform.position = new Vector3(3.5f, 0.45f, 0f);
            root.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            Canvas canvas = visualRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasGroup cg = visualRoot.AddComponent<CanvasGroup>();
            RectTransform rt = visualRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 190f);
            rt.localScale = Vector3.one * 0.01f;

            // 은색 U자형 걸이 (Shackle)
            Image shackleImg = CreateUiImage("Shackle", rt, sprShackle, Color.white, sliced: false);
            shackleImg.rectTransform.anchoredPosition = new Vector2(0f, 44f);
            shackleImg.rectTransform.sizeDelta = new Vector2(88f, 76f);

            // 황금색 자물쇠 몸통
            Image bodyImg = CreateUiImage("Body", rt, sprBody, Color.white, sliced: false);
            bodyImg.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            bodyImg.rectTransform.sizeDelta = new Vector2(104f, 92f);

            // 하단 올리브색 알약 배지 "Lv. 4"
            Image lvBg = CreateUiImage("LvBadge", rt, sprRounded, ColOlive, sliced: true);
            lvBg.rectTransform.anchoredPosition = new Vector2(0f, -68f);
            lvBg.rectTransform.sizeDelta = new Vector2(96f, 36f);

            Image lvBorder = CreateUiImage("LvBadgeBorder", lvBg.rectTransform, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(lvBorder.rectTransform);

            TextMeshProUGUI lvTxt = CreateUiText("LvText", lvBg.rectTransform, "Lv. 4", 21, Color.white, TextAlignmentOptions.Center);
            StretchFull(lvTxt.rectTransform);

            UnlockPadlockVisual padlock = root.AddComponent<UnlockPadlockVisual>();
            SerializedObject so = new SerializedObject(padlock);
            so.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            so.FindProperty("shackleRect").objectReferenceValue = shackleImg.rectTransform;
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return padlock;
        }

        private static void SetupFieldAnimator(Transform parent, UnlockFieldAnimator anim, Camera cam, GameObject lockedZone,
            Material matTop, Material matLeft, Material matRight, Material matGrid,
            Material matCircleReveal, Material matExpandGlow, Material matGlowRing, Material matDustRing, Material matDropShadow)
        {
            // 1. Wave (5x5 Tiles)
            GameObject waveRoot = new GameObject("WaveRoot");
            waveRoot.transform.SetParent(parent, false);
            Transform[] tiles = new Transform[25];
            int idx = 0;
            for (int ix = 0; ix < 5; ix++)
            {
                for (int iz = 0; iz < 5; iz++)
                {
                    float wx = 1.5f + ix;
                    float wz = -2.0f + iz;
                    GameObject tile = CreateGrassSlab($"WaveTile_{ix}_{iz}", new Vector3(wx, 0f, wz), 0.98f, 0.98f, 0.36f,
                        matTop, matLeft, matRight, null, null, withGrid: false);
                    tile.transform.SetParent(waveRoot.transform, true);
                    tiles[idx++] = tile.transform;
                }
            }
            waveRoot.SetActive(false);

            // 2. Expand (Left Pivot x=1)
            GameObject expandPivot = new GameObject("ExpandPivot");
            expandPivot.transform.SetParent(parent, false);
            expandPivot.transform.position = new Vector3(1.0f, 0f, 0f);
            GameObject expandSlab = CreateGrassSlab("ExpandSlab", new Vector3(3.5f, 0f, 0f), 5f, 5f, 0.36f,
                matTop, matLeft, matRight, matGrid, null, withGrid: true);
            expandSlab.transform.SetParent(expandPivot.transform, true);
            expandPivot.SetActive(false);

            GameObject expandGlow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            expandGlow.name = "ExpandEdgeGlow";
            Object.DestroyImmediate(expandGlow.GetComponent<Collider>());
            expandGlow.transform.SetParent(parent, false);
            expandGlow.transform.position = new Vector3(1f, 0.04f, 0f);
            expandGlow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            expandGlow.transform.localScale = new Vector3(0.28f, 5.1f, 1f);
            Renderer expandGlowRend = expandGlow.GetComponent<Renderer>();
            expandGlowRend.sharedMaterial = matExpandGlow;
            expandGlow.SetActive(false);

            // 3. Circle (URP Custom Shader Slab)
            GameObject circleSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            circleSlab.name = "CircleRevealSlab";
            Object.DestroyImmediate(circleSlab.GetComponent<Collider>());
            circleSlab.transform.SetParent(parent, false);
            circleSlab.transform.position = new Vector3(3.5f, -0.18f, 0f);
            circleSlab.transform.localScale = new Vector3(5f, 0.36f, 5f);
            circleSlab.GetComponent<Renderer>().sharedMaterial = matCircleReveal;
            circleSlab.SetActive(false);

            GameObject circleRing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circleRing.name = "CircleGlowRing";
            Object.DestroyImmediate(circleRing.GetComponent<Collider>());
            circleRing.transform.SetParent(parent, false);
            circleRing.transform.position = new Vector3(-0.1f, 0.03f, 0f);
            circleRing.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            circleRing.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            Renderer circleRingRend = circleRing.GetComponent<Renderer>();
            circleRingRend.sharedMaterial = matGlowRing;
            circleRing.SetActive(false);

            // 4. Drop (Bottom Pivot + Sky Fall Slab + DropShadow + Dust Ring)
            GameObject dropRoot = new GameObject("DropRoot");
            dropRoot.transform.SetParent(parent, false);
            dropRoot.transform.position = new Vector3(3.5f, -0.36f, 0f);

            GameObject dropPivot = new GameObject("DropPivot");
            dropPivot.transform.SetParent(dropRoot.transform, false);
            dropPivot.transform.localPosition = new Vector3(0f, 12f, 0f);

            GameObject dropSlab = CreateGrassSlab("DropSlab", new Vector3(0f, 0.36f, 0f), 5f, 5f, 0.36f,
                matTop, matLeft, matRight, matGrid, null, withGrid: true);
            dropSlab.transform.SetParent(dropPivot.transform, false);
            dropPivot.SetActive(false);

            GameObject dropShadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dropShadow.name = "DropShadow";
            Object.DestroyImmediate(dropShadow.GetComponent<Collider>());
            dropShadow.transform.SetParent(parent, false);
            dropShadow.transform.position = new Vector3(3.5f, -0.355f, 0f);
            dropShadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            dropShadow.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            Renderer dropShadowRend = dropShadow.GetComponent<Renderer>();
            dropShadowRend.sharedMaterial = matDropShadow;
            dropShadow.SetActive(false);

            GameObject dustRing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dustRing.name = "DustRing";
            Object.DestroyImmediate(dustRing.GetComponent<Collider>());
            dustRing.transform.SetParent(parent, false);
            dustRing.transform.position = new Vector3(3.5f, -0.35f, 0f);
            dustRing.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            dustRing.transform.localScale = new Vector3(4.9f, 4.9f, 1f);
            Renderer dustRend = dustRing.GetComponent<Renderer>();
            dustRend.sharedMaterial = matDustRing;
            dustRing.SetActive(false);

            // 5. Bounce (Center Pivot)
            GameObject bounceSlab = CreateGrassSlab("BounceSlab", new Vector3(3.5f, 0f, 0f), 5f, 5f, 0.36f,
                matTop, matLeft, matRight, matGrid, null, withGrid: true);
            bounceSlab.transform.SetParent(parent, true);
            bounceSlab.SetActive(false);

            // 직렬화 연결
            SerializedObject so = new SerializedObject(anim);
            so.FindProperty("waveRoot").objectReferenceValue = waveRoot;
            SerializedProperty tilesProp = so.FindProperty("waveTiles");
            tilesProp.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
                tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];

            so.FindProperty("expandPivot").objectReferenceValue = expandPivot.transform;
            so.FindProperty("expandEdgeGlow").objectReferenceValue = expandGlow.transform;
            so.FindProperty("expandEdgeGlowRenderer").objectReferenceValue = expandGlowRend;

            so.FindProperty("circleSlab").objectReferenceValue = circleSlab;
            so.FindProperty("circleRevealMaterial").objectReferenceValue = matCircleReveal;
            so.FindProperty("circleGlowRing").objectReferenceValue = circleRing.transform;
            so.FindProperty("circleGlowRingRenderer").objectReferenceValue = circleRingRend;

            so.FindProperty("dropSlab").objectReferenceValue = dropPivot.transform;
            so.FindProperty("dropShadow").objectReferenceValue = dropShadow.transform;
            so.FindProperty("dropShadowRenderer").objectReferenceValue = dropShadowRend;
            so.FindProperty("dustRing").objectReferenceValue = dustRing.transform;
            so.FindProperty("dustRingRenderer").objectReferenceValue = dustRend;
            so.FindProperty("mainCamera").objectReferenceValue = cam;

            so.FindProperty("bounceSlab").objectReferenceValue = bounceSlab.transform;
            so.FindProperty("lockedZoneOutline").objectReferenceValue = lockedZone;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateBillboardSpriteTemplate(string name, Sprite sprite, float scale)
        {
            GameObject go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 25;
            go.SetActive(false);
            return go;
        }

        private static void CreateHudCanvas(UnlockZoneController ctrl, Sprite sprRounded, Sprite sprOutline, Sprite sprCircle,
            Sprite sprLock, Sprite sprBolt, Sprite sprReset, Sprite sprSoundOn, Sprite sprSoundOff, Sprite sprCash,
            Sprite sprWave, Sprite sprExpand, Sprite sprCircleMode, Sprite sprDrop, Sprite sprBounce)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            RectTransform rootRt = canvasObj.GetComponent<RectTransform>();

            // === 상단 좌측 상태 카드 ===
            Image statusCard = CreateUiImage("StatusCard", rootRt, sprRounded, new Color(ColCream.r, ColCream.g, ColCream.b, 0.96f), sliced: true);
            RectTransform scRt = statusCard.rectTransform;
            scRt.anchorMin = new Vector2(0f, 1f);
            scRt.anchorMax = new Vector2(0f, 1f);
            scRt.pivot = new Vector2(0f, 1f);
            scRt.anchoredPosition = new Vector2(28f, -24f);
            scRt.sizeDelta = new Vector2(460f, 148f);

            Image scBorder = CreateUiImage("Border", scRt, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(scBorder.rectTransform);

            // ! 원형 배지
            Image bangBg = CreateUiImage("BangBadge", scRt, sprCircle, ColLime, sliced: false);
            bangBg.rectTransform.anchorMin = new Vector2(0f, 1f);
            bangBg.rectTransform.anchorMax = new Vector2(0f, 1f);
            bangBg.rectTransform.anchoredPosition = new Vector2(28f, -26f);
            bangBg.rectTransform.sizeDelta = new Vector2(26f, 26f);
            TextMeshProUGUI bangTxt = CreateUiText("Bang", bangBg.rectTransform, "!", 16, Color.white, TextAlignmentOptions.Center);
            StretchFull(bangTxt.rectTransform);

            TextMeshProUGUI cardTitle = CreateUiText("CardTitle", scRt, "필드 해금 애니메이션 프로토타입", 21, ColDarkBrown, TextAlignmentOptions.MidlineLeft);
            cardTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            cardTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            cardTitle.rectTransform.pivot = new Vector2(0f, 1f);
            cardTitle.rectTransform.anchoredPosition = new Vector2(50f, -12f);
            cardTitle.rectTransform.sizeDelta = new Vector2(-66f, 30f);

            TextMeshProUGUI cardDesc = CreateUiText("CardDesc", scRt,
                "해금 버튼을 누르면 캐릭터가 이동해 필드를 해금합니다.\n(즉시 해금 ON 시 모드 클릭만으로 자물쇠부터 바로 재생)",
                14, ColSubBrown, TextAlignmentOptions.TopLeft);
            cardDesc.rectTransform.anchorMin = new Vector2(0f, 1f);
            cardDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            cardDesc.rectTransform.pivot = new Vector2(0f, 1f);
            cardDesc.rectTransform.anchoredPosition = new Vector2(18f, -48f);
            cardDesc.rectTransform.sizeDelta = new Vector2(-36f, 46f);

            // 보유 자금 배지
            Image cashBadge = CreateUiImage("CashBadge", scRt, sprRounded, ColBadgeBeige, sliced: true);
            cashBadge.rectTransform.anchorMin = new Vector2(0f, 0f);
            cashBadge.rectTransform.anchorMax = new Vector2(0f, 0f);
            cashBadge.rectTransform.pivot = new Vector2(0f, 0f);
            cashBadge.rectTransform.anchoredPosition = new Vector2(18f, 14f);
            cashBadge.rectTransform.sizeDelta = new Vector2(168f, 32f);
            Image cbBorder = CreateUiImage("Border", cashBadge.rectTransform, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(cbBorder.rectTransform);

            Image cbIcon = CreateUiImage("Icon", cashBadge.rectTransform, sprCash, Color.white, sliced: false);
            cbIcon.rectTransform.anchoredPosition = new Vector2(-60f, 0f);
            cbIcon.rectTransform.sizeDelta = new Vector2(20f, 20f);
            TextMeshProUGUI cbTxt = CreateUiText("CashTxt", cashBadge.rectTransform, "보유 자금: ∞", 15, ColDarkBrown, TextAlignmentOptions.Center);
            cbTxt.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            cbTxt.rectTransform.sizeDelta = new Vector2(130f, 28f);

            // 실시간 상태 배지
            Image statusBadge = CreateUiImage("StatusBadge", scRt, sprRounded, Hex("#FFE8A3"), sliced: true);
            statusBadge.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusBadge.rectTransform.anchorMax = new Vector2(0f, 0f);
            statusBadge.rectTransform.pivot = new Vector2(0f, 0f);
            statusBadge.rectTransform.anchoredPosition = new Vector2(196f, 14f);
            statusBadge.rectTransform.sizeDelta = new Vector2(246f, 32f);
            Image sbBorder = CreateUiImage("Border", statusBadge.rectTransform, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(sbBorder.rectTransform);
            TextMeshProUGUI statusTxt = CreateUiText("StatusTxt", statusBadge.rectTransform, "대기 중 (미해금)", 15, ColDarkBrown, TextAlignmentOptions.Center);
            StretchFull(statusTxt.rectTransform);

            // === 상단 우측 컨트롤 버튼 ===
            GameObject topRight = new GameObject("TopRightControls");
            topRight.transform.SetParent(rootRt, false);
            RectTransform trRt = topRight.AddComponent<RectTransform>();
            trRt.anchorMin = new Vector2(1f, 1f);
            trRt.anchorMax = new Vector2(1f, 1f);
            trRt.pivot = new Vector2(1f, 1f);
            trRt.anchoredPosition = new Vector2(-28f, -24f);
            trRt.sizeDelta = new Vector2(590f, 54f);

            HorizontalLayoutGroup trLayout = topRight.AddComponent<HorizontalLayoutGroup>();
            trLayout.spacing = 10f;
            trLayout.childAlignment = TextAnchor.MiddleRight;
            trLayout.childControlWidth = false;
            trLayout.childControlHeight = false;

            Button btnUnlock = CreateActionButton("BtnUnlock", trRt, 122f, 48f, ColLime, Color.white, sprRounded, sprOutline, sprLock, "해금", out _, out _);
            Button btnInstant = CreateActionButton("BtnInstant", trRt, 198f, 48f, ColCream, ColDarkBrown, sprRounded, sprOutline, sprBolt, "즉시 해금: OFF", out Image instantBg, out TextMeshProUGUI instantTxt);
            Button btnReset = CreateActionButton("BtnReset", trRt, 152f, 48f, ColCream, ColDarkBrown, sprRounded, sprOutline, sprReset, "다시 잠그기", out _, out _);
            Button btnSound = CreateIconOnlyButton("BtnSound", trRt, 52f, 48f, ColCream, sprRounded, sprOutline, sprSoundOn, out Image soundIconImg);

            // === 하단 5종 모드 선택 바 ===
            Image bottomBar = CreateUiImage("BottomModeBar", rootRt, sprRounded, new Color(ColCream.r, ColCream.g, ColCream.b, 0.96f), sliced: true);
            RectTransform bbRt = bottomBar.rectTransform;
            bbRt.anchorMin = new Vector2(0.5f, 0f);
            bbRt.anchorMax = new Vector2(0.5f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = new Vector2(0f, 24f);
            bbRt.sizeDelta = new Vector2(980f, 114f);

            Image bbBorder = CreateUiImage("Border", bbRt, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(bbBorder.rectTransform);

            TextMeshProUGUI barHeader = CreateUiText("BarHeader", bbRt, "해금 애니메이션 스타일 선택 (5종)", 14, ColSubBrown, TextAlignmentOptions.MidlineLeft);
            barHeader.rectTransform.anchorMin = new Vector2(0f, 1f);
            barHeader.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            barHeader.rectTransform.pivot = new Vector2(0f, 1f);
            barHeader.rectTransform.anchoredPosition = new Vector2(22f, -10f);
            barHeader.rectTransform.sizeDelta = new Vector2(360f, 24f);

            TextMeshProUGUI modeDesc = CreateUiText("ModeDescText", bbRt, "소단위 타일이 물결치듯 순차적으로 튀어오르며 해금", 14, Hex("#4D8A10"), TextAlignmentOptions.MidlineRight);
            modeDesc.rectTransform.anchorMin = new Vector2(0.35f, 1f);
            modeDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            modeDesc.rectTransform.pivot = new Vector2(1f, 1f);
            modeDesc.rectTransform.anchoredPosition = new Vector2(-22f, -10f);
            modeDesc.rectTransform.sizeDelta = new Vector2(560f, 24f);

            GameObject modeContainer = new GameObject("ModeButtons");
            modeContainer.transform.SetParent(bbRt, false);
            RectTransform mcRt = modeContainer.AddComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0f, 0f);
            mcRt.anchorMax = new Vector2(1f, 0f);
            mcRt.pivot = new Vector2(0.5f, 0f);
            mcRt.anchoredPosition = new Vector2(0f, 14f);
            mcRt.sizeDelta = new Vector2(-28f, 54f);

            HorizontalLayoutGroup mcLayout = modeContainer.AddComponent<HorizontalLayoutGroup>();
            mcLayout.spacing = 10f;
            mcLayout.childAlignment = TextAnchor.MiddleCenter;
            mcLayout.childControlWidth = false;
            mcLayout.childControlHeight = false;

            string[] labels = { "1. 타일 파도형식", "2. 필드확장", "3. 원형 확산", "4. 필드 낙하", "5. 튕김" };
            Sprite[] icons = { sprWave, sprExpand, sprCircleMode, sprDrop, sprBounce };
            float[] widths = { 198f, 168f, 174f, 174f, 144f };

            Button[] modeBtns = new Button[5];
            Image[] modeBgs = new Image[5];
            TextMeshProUGUI[] modeTxts = new TextMeshProUGUI[5];

            for (int i = 0; i < 5; i++)
            {
                modeBtns[i] = CreateActionButton($"BtnMode_{i}", mcRt, widths[i], 50f, ColCream, ColDarkBrown,
                    sprRounded, sprOutline, icons[i], labels[i], out modeBgs[i], out modeTxts[i]);
            }

            // === 상단 중앙 토스트 ===
            Image toastBg = CreateUiImage("ToastBox", rootRt, sprRounded, ColDarkBrown, sliced: true);
            RectTransform toastRt = toastBg.rectTransform;
            toastRt.anchorMin = new Vector2(0.5f, 1f);
            toastRt.anchorMax = new Vector2(0.5f, 1f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.anchoredPosition = new Vector2(0f, -92f);
            toastRt.sizeDelta = new Vector2(420f, 40f);
            CanvasGroup toastCg = toastBg.gameObject.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;
            toastCg.blocksRaycasts = false;

            TextMeshProUGUI toastTxt = CreateUiText("ToastText", toastRt, "", 16, ColCream, TextAlignmentOptions.Center);
            StretchFull(toastTxt.rectTransform);

            // 직렬화 연결
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("statusBadgeText").objectReferenceValue = statusTxt;
            so.FindProperty("statusBadgeBg").objectReferenceValue = statusBadge;

            so.FindProperty("unlockButton").objectReferenceValue = btnUnlock;
            so.FindProperty("instantToggleButton").objectReferenceValue = btnInstant;
            so.FindProperty("instantToggleBg").objectReferenceValue = instantBg;
            so.FindProperty("instantToggleText").objectReferenceValue = instantTxt;
            so.FindProperty("resetButton").objectReferenceValue = btnReset;
            so.FindProperty("soundButton").objectReferenceValue = btnSound;
            so.FindProperty("soundIconImage").objectReferenceValue = soundIconImg;
            so.FindProperty("soundOnSprite").objectReferenceValue = sprSoundOn;
            so.FindProperty("soundOffSprite").objectReferenceValue = sprSoundOff;

            SerializedProperty btnsProp = so.FindProperty("modeButtons");
            SerializedProperty bgsProp = so.FindProperty("modeButtonBgs");
            SerializedProperty txtsProp = so.FindProperty("modeButtonTexts");
            btnsProp.arraySize = 5;
            bgsProp.arraySize = 5;
            txtsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = modeBtns[i];
                bgsProp.GetArrayElementAtIndex(i).objectReferenceValue = modeBgs[i];
                txtsProp.GetArrayElementAtIndex(i).objectReferenceValue = modeTxts[i];
            }

            so.FindProperty("modeDescText").objectReferenceValue = modeDesc;
            so.FindProperty("toastCanvasGroup").objectReferenceValue = toastCg;
            so.FindProperty("toastText").objectReferenceValue = toastTxt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateActionButton(string name, RectTransform parent, float w, float h, Color bgCol, Color textCol,
            Sprite sprBg, Sprite sprOutline, Sprite icon, string label, out Image bgImg, out TextMeshProUGUI txtComp)
        {
            bgImg = CreateUiImage(name, parent, sprBg, bgCol, sliced: true);
            bgImg.rectTransform.sizeDelta = new Vector2(w, h);
            Button btn = bgImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            Image border = CreateUiImage("Border", bgImg.rectTransform, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(border.rectTransform);

            Image iconImg = CreateUiImage("Icon", bgImg.rectTransform, icon, Color.white, sliced: false);
            iconImg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            iconImg.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            iconImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconImg.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            iconImg.rectTransform.sizeDelta = new Vector2(22f, 22f);

            txtComp = CreateUiText("Label", bgImg.rectTransform, label, 16, textCol, TextAlignmentOptions.MidlineLeft);
            txtComp.rectTransform.anchorMin = Vector2.zero;
            txtComp.rectTransform.anchorMax = Vector2.one;
            txtComp.rectTransform.offsetMin = new Vector2(40f, 0f);
            txtComp.rectTransform.offsetMax = new Vector2(-10f, 0f);

            return btn;
        }

        private static Button CreateIconOnlyButton(string name, RectTransform parent, float w, float h, Color bgCol,
            Sprite sprBg, Sprite sprOutline, Sprite icon, out Image iconImg)
        {
            Image bgImg = CreateUiImage(name, parent, sprBg, bgCol, sliced: true);
            bgImg.rectTransform.sizeDelta = new Vector2(w, h);
            Button btn = bgImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            Image border = CreateUiImage("Border", bgImg.rectTransform, sprOutline, ColDarkBrown, sliced: true);
            StretchFull(border.rectTransform);

            iconImg = CreateUiImage("Icon", bgImg.rectTransform, icon, Color.white, sliced: false);
            iconImg.rectTransform.sizeDelta = new Vector2(26f, 26f);
            return btn;
        }

        private static Image CreateUiImage(string name, RectTransform parent, Sprite sprite, Color col, bool sliced)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = col;
            img.type = (sliced && sprite != null) ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }

        private static TextMeshProUGUI CreateUiText(string name, RectTransform parent, string content, int size, Color col, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
            if (_uiFont != null)
                txt.font = _uiFont;
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = FontStyles.Bold;
            txt.color = col;
            txt.alignment = align;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // =========================================================================
        // 머티리얼 & 프로시저럴 텍스처/스프라이트 생성 유틸
        // =========================================================================

        private static void LoadFont()
        {
            _uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_uiFont == null)
            {
                Debug.LogWarning($"[UnlockSceneBuilder] TMP_FontAsset을 찾을 수 없습니다: {FontPath}. TMP 기본 폰트를 사용합니다.");
                _uiFont = TMP_Settings.defaultFontAsset;
            }
        }

        private static Material CreateColorMaterial(string fileName, Shader shader, Color color, bool transparent = false)
        {
            string path = $"{GenDir}/{fileName}";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateTextureMaterial(string fileName, Shader shader, Texture2D tex, Color tint, bool transparent)
        {
            Material mat = CreateColorMaterial(fileName, shader, tint, transparent);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateCircleRevealMaterial(string fileName, Shader shader)
        {
            string path = $"{GenDir}/{fileName}";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_TopColor", ColGrassTop);
            mat.SetColor("_GridColor", new Color(ColGrassGrid.r, ColGrassGrid.g, ColGrassGrid.b, 0.55f));
            mat.SetColor("_SideLeftColor", ColGrassLeft);
            mat.SetColor("_SideRightColor", ColGrassRight);
            mat.SetVector("_Origin", new Vector4(-0.1f, 0f, 0f, 0f));
            mat.SetFloat("_RevealRadius", 0f);
            mat.SetFloat("_EdgeWidth", 0.24f);
            mat.SetColor("_EdgeColor", new Color(0.95f, 1f, 0.72f, 1f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Sprite CreateRoundedRectSprite(string fileName, int size, int radius, Color col, int border)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxSdf(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f, size * 0.5f - 1f, size * 0.5f - 1f, radius);
                    float a = Mathf.Clamp01(0.5f - d);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            }
            return SaveSpritePng(tex, fileName, new Vector4(border, border, border, border));
        }

        private static Sprite CreateRoundedOutlineSprite(string fileName, int size, int radius, int thickness, Color col, int border)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dOuter = RoundedBoxSdf(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f, size * 0.5f - 1f, size * 0.5f - 1f, radius);
                    float dInner = RoundedBoxSdf(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f, size * 0.5f - 1f - thickness, size * 0.5f - 1f - thickness, Mathf.Max(2f, radius - thickness));
                    float a = Mathf.Clamp01(0.5f - dOuter) * Mathf.Clamp01(dInner + 0.5f);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            }
            return SaveSpritePng(tex, fileName, new Vector4(border, border, border, border));
        }

        private static Sprite CreateCircleSprite(string fileName, int size, Color col)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) - r;
                    float a = Mathf.Clamp01(0.5f - d);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Texture2D CreateDashedZoneTexture(string fileName, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color outerCol = new Color(1f, 1f, 1f, 0.85f);
            Color innerCol = new Color(1f, 1f, 1f, 0.35f);
            Color fillCol = new Color(1f, 1f, 1f, 0.08f);

            int borderW = 10;
            float cell = size / 5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = fillCol;
                    bool onEdgeX = x < borderW || x >= size - borderW;
                    bool onEdgeY = y < borderW || y >= size - borderW;
                    if (onEdgeX || onEdgeY)
                    {
                        int p = onEdgeX && !onEdgeY ? y : x;
                        c = ((p / 24) % 2 == 0) ? outerCol : clear;
                    }
                    else
                    {
                        float rx = x % cell;
                        float ry = y % cell;
                        bool gridX = rx < 3f || rx > cell - 3f;
                        bool gridY = ry < 3f || ry > cell - 3f;
                        if (gridX || gridY)
                        {
                            int gp = gridX ? y : x;
                            if ((gp / 16) % 2 == 0) c = innerCol;
                        }
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            return SaveTexturePng(tex, fileName);
        }

        private static Texture2D CreateGlowRingTexture(string fileName, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    float ring = Mathf.Exp(-Mathf.Pow((r - 0.88f) * 14f, 2f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring)));
                }
            }
            return SaveTexturePng(tex, fileName);
        }

        private static Texture2D CreateDustSquareTexture(string fileName, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    float box = Mathf.Max(nx, ny);
                    float a = Mathf.Exp(-Mathf.Pow((box - 0.88f) * 14f, 2f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            }
            return SaveTexturePng(tex, fileName);
        }

        private static Texture2D CreateDropShadowTexture(string fileName, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float radius = half - 2f;
            Vector2 center = new Vector2(half, half);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float a = Mathf.Clamp01((radius + 0.75f - dist) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return SaveTexturePng(tex, fileName);
        }

        private static Sprite CreatePadlockBodySprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxSdf(x - cx, y - cy, 46f, 40f, 16f);
                    if (d <= 0.5f)
                    {
                        Color col = (d > -6f) ? ColDarkBrown : ColYellow;
                        // 열쇠구멍
                        float dkTop = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy + 7f)) - 9f;
                        float dkBot = RoundedBoxSdf(x - cx, y - (cy - 9f), 4.5f, 12f, 2f);
                        if (dkTop < 0f || dkBot < 0f) col = ColDarkBrown;
                        float aa = Mathf.Clamp01(0.5f - d);
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, aa));
                    }
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreatePadlockShackleSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float cx = size * 0.5f;
            float cy = size * 0.36f;
            Color silver = Hex("#E8EFF5");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float d;
                    if (y >= cy)
                    {
                        float r = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                        d = Mathf.Abs(r - 28f) - 8f;
                    }
                    else
                    {
                        d = Mathf.Abs(dx - 28f) - 8f;
                        if (y < 8f) d = 10f;
                    }
                    if (d <= 0.5f)
                    {
                        Color col = (d > -3.5f) ? ColDarkBrown : silver;
                        float aa = Mathf.Clamp01(0.5f - d);
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, aa));
                    }
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        // --- 아이콘 스프라이트들 ---
        private static Sprite CreateLockIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float body = RoundedBoxSdf(x - c, y - (c - 8f), 17f, 13f, 5f);
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(c, c + 6f));
                    float shackle = (y >= c + 5f) ? Mathf.Abs(r - 11f) - 3.2f : 10f;
                    float d = Mathf.Min(body, shackle);
                    if (d < 0.5f)
                    {
                        Color col = body < 0.5f ? ColYellow : Hex("#E8EFF5");
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, Mathf.Clamp01(0.5f - d)));
                    }
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateBoltIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            Color c = Hex("#F59E0B");
            DrawPolygon(tex, new[] { new Vector2(36, 56), new Vector2(18, 30), new Vector2(31, 30), new Vector2(26, 8), new Vector2(46, 34), new Vector2(33, 34) }, c);
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateResetIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x - c, y - c);
                    float r = p.magnitude;
                    float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (Mathf.Abs(r - 18f) < 4f && (ang > -140f && ang < 130f))
                        tex.SetPixel(x, y, ColDarkBrown);
                }
            }
            DrawPolygon(tex, new[] { new Vector2(14, 46), new Vector2(26, 52), new Vector2(24, 38) }, ColDarkBrown);
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateSoundIconSprite(string fileName, int size, bool on)
        {
            Texture2D tex = NewBlankTex(size);
            DrawPolygon(tex, new[] { new Vector2(12, 24), new Vector2(22, 24), new Vector2(34, 14), new Vector2(34, 50), new Vector2(22, 40), new Vector2(12, 40) }, ColDarkBrown);
            if (on)
            {
                for (int y = 16; y < 48; y++)
                {
                    for (int x = 36; x < 54; x++)
                    {
                        float r = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                        if (Mathf.Abs(r - 11f) < 2f || Mathf.Abs(r - 18f) < 2f)
                            tex.SetPixel(x, y, ColLime);
                    }
                }
            }
            else
            {
                DrawLine(tex, new Vector2(40, 22), new Vector2(54, 42), 3f, ColOrange);
                DrawLine(tex, new Vector2(40, 42), new Vector2(54, 22), 3f, ColOrange);
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateCashIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float c = size * 0.5f;
            Color green = Hex("#65B82A");
            Color lightGreen = Hex("#A9E866");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxSdf(x - c, y - c, 24f, 15f, 4f);
                    if (d < 0.5f)
                    {
                        Color col = d > -3f ? ColDarkBrown : green;
                        float inner = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                        if (inner < 7f) col = lightGreen;
                        tex.SetPixel(x, y, new Color(col.r, col.g, col.b, Mathf.Clamp01(0.5f - d)));
                    }
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreatePadExpandIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(RoundedBoxSdf(x - c, y - c, 20f, 20f, 3f));
                    if (d < 2.2f && (((x + y) / 8) % 2 == 0))
                        tex.SetPixel(x, y, Color.white);
                }
            }
            DrawLine(tex, new Vector2(c, c), new Vector2(c, size - 10), 4f, Color.white);
            DrawLine(tex, new Vector2(c, c), new Vector2(c, 10), 4f, Color.white);
            DrawLine(tex, new Vector2(c, c), new Vector2(10, c), 4f, Color.white);
            DrawLine(tex, new Vector2(c, c), new Vector2(size - 10, c), 4f, Color.white);
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateSparkleSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - c) / c;
                    float dy = Mathf.Abs(y - c) / c;
                    float star = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);
                    float a = Mathf.Clamp01((0.95f - star) * 3.5f);
                    if (a > 0f) tex.SetPixel(x, y, new Color(1f, 0.94f, 0.55f, a));
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateWaveIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            Color col = Hex("#0EA5E9");
            for (int x = 8; x < 56; x++)
            {
                float wy1 = 24f + Mathf.Sin(x * 0.22f) * 6f;
                float wy2 = 40f + Mathf.Sin(x * 0.22f) * 6f;
                for (int dy = -3; dy <= 3; dy++)
                {
                    tex.SetPixel(x, Mathf.RoundToInt(wy1) + dy, col);
                    tex.SetPixel(x, Mathf.RoundToInt(wy2) + dy, col);
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateExpandIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            Color col = Hex("#16A34A");
            DrawLine(tex, new Vector2(14, 32), new Vector2(50, 32), 4f, col);
            DrawPolygon(tex, new[] { new Vector2(8, 32), new Vector2(20, 42), new Vector2(20, 22) }, col);
            DrawPolygon(tex, new[] { new Vector2(56, 32), new Vector2(44, 42), new Vector2(44, 22) }, col);
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateCircleModeIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            Color col = Hex("#EC4899");
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (Mathf.Abs(r - 19f) < 3.5f || r < 7f)
                        tex.SetPixel(x, y, col);
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateDropIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            Color col = Hex("#6366F1");
            DrawLine(tex, new Vector2(32, 52), new Vector2(32, 20), 4.5f, col);
            DrawPolygon(tex, new[] { new Vector2(32, 10), new Vector2(20, 24), new Vector2(44, 24) }, col);
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Sprite CreateBounceIconSprite(string fileName, int size)
        {
            Texture2D tex = NewBlankTex(size);
            Color col = Hex("#06B6D4");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(26, 26)) < 14f ||
                        Vector2.Distance(new Vector2(x, y), new Vector2(45, 44)) < 9f)
                        tex.SetPixel(x, y, col);
                }
            }
            return SaveSpritePng(tex, fileName, Vector4.zero);
        }

        private static Texture2D NewBlankTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clear = new Color[size * size];
            tex.SetPixels(clear);
            return tex;
        }

        private static void DrawLine(Texture2D tex, Vector2 a, Vector2 b, float thickness, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)Mathf.Max(1, steps));
                int r = Mathf.CeilToInt(thickness * 0.5f);
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int px = Mathf.RoundToInt(p.x) + dx;
                        int py = Mathf.RoundToInt(p.y) + dy;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height && dx * dx + dy * dy <= r * r)
                            tex.SetPixel(px, py, col);
                    }
                }
            }
        }

        private static void DrawPolygon(Texture2D tex, Vector2[] pts, Color col)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    if (PointInPoly(new Vector2(x + 0.5f, y + 0.5f), pts))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static bool PointInPoly(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y + 0.0001f) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float RoundedBoxSdf(float px, float py, float halfW, float halfH, float r)
        {
            float qx = Mathf.Abs(px) - halfW + r;
            float qy = Mathf.Abs(py) - halfH + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                   + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static Texture2D SaveTexturePng(Texture2D tex, string fileName)
        {
            tex.Apply();
            string path = $"{GenDir}/{fileName}";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Sprite SaveSpritePng(Texture2D tex, string fileName, Vector4 border)
        {
            tex.Apply();
            string path = $"{GenDir}/{fileName}";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spriteBorder = border;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
