using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using LogisticsTycoon.Transitions;

namespace LogisticsTycoon.Transitions.Editor
{
    public static class TransitionPrefabBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/Transitions";

        [MenuItem("Tools/Logistics Tycoon/Rebuild All Transition Prefabs & Setup Both Scenes")]
        public static void BuildAll()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
                AssetDatabase.Refresh();
            }

            // 구형 지게차(승강) 프리팹 삭제
            AssetDatabase.DeleteAsset("Assets/Prefabs/Transitions/ForkliftLiftTransition.prefab");
            AssetDatabase.DeleteAsset("Assets/Resources/Transitions/ForkliftLiftTransition.prefab");

            Sprite circleSprite = GetOrCreateCircleSprite();
            Sprite arcSprite = GetOrCreateArcSprite();
            Sprite capsuleSprite = GetOrCreateCapsuleSprite();
            Sprite starTailSprite = GetOrCreateShootingStarTailSprite();

            // 1. 기존 개선 프리팹 생성 (톱니바퀴 2종: 연속 회전 vs 철컥 스냅 분리)
            GameObject gearSmoothPrefab = CreateGearCirclePrefab(circleSprite);
            GameObject gearTickPrefab = CreateGearCircleTickPrefab(circleSprite);
            GameObject boxPrefab = CreateBoxFlapPrefab();
            GameObject truckPrefab = CreateTruckWipePrefab(circleSprite, arcSprite);
            GameObject shutterPrefab = CreateConveyorShutterPrefab();
            GameObject stampPrefab = CreateStampInvoicePrefab(circleSprite);

            // 2. 신규 리뉴얼 물류 트랜지션 생성
            GameObject forkliftPrefab = CreateForkliftDrivePrefab();
            GameObject barcodePrefab = CreateBarcodeScanPrefab();
            GameObject receiptRollPrefab = CreateReceiptRollPrefab(circleSprite);
            GameObject containerPrefab = CreateContainerDoorPrefab();
            GameObject fragileTapePrefab = CreateFragileTapePrefab(circleSprite);

            // 3. 지도(스테이지) 전용 WorldMapPin 트랜지션 생성
            GameObject mapPinPrefab = CreateWorldMapPinPrefab(circleSprite);

            // 4. 대각선 스트라이프 & 슬림 별똥별 택배 DiagonalMeteor 트랜지션 생성
            GameObject meteorPrefab = CreateDiagonalMeteorPrefab(capsuleSprite, circleSprite, starTailSprite);

            // 5. 물리 바운싱 볼 BouncingBall 트랜지션 생성
            GameObject bouncingBallPrefab = CreateBouncingBallPrefab(circleSprite);

            // Resources/Transitions 디렉토리에 프리팹 동기화 복사
            SyncPrefabsToResources();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5. Build Settings에 SceneA, SceneB 등록
            EnsureBuildSettingsScenes();

            // 6. SceneA 및 SceneB 셋업 (에디터 플레이 중이 아닐 때만 씬 오픈 수행)
            if (!EditorApplication.isPlaying)
            {
                SetupSceneA(gearSmoothPrefab, gearTickPrefab, boxPrefab, truckPrefab, shutterPrefab, stampPrefab,
                    forkliftPrefab, barcodePrefab, receiptRollPrefab, containerPrefab, fragileTapePrefab, mapPinPrefab, meteorPrefab, bouncingBallPrefab);

                SetupSceneB(gearSmoothPrefab, gearTickPrefab, boxPrefab, truckPrefab, shutterPrefab, stampPrefab,
                    forkliftPrefab, barcodePrefab, receiptRollPrefab, containerPrefab, fragileTapePrefab, mapPinPrefab, meteorPrefab, bouncingBallPrefab);
            }

            Debug.Log("<color=#00FF00>[Logistics Tycoon]</color> 모든 트랜지션 프리팹 14종 리뉴얼 및 SceneA, SceneB, BuildSettings 셋업이 완벽히 완료되었습니다!");
        }

        public static void SyncPrefabsToResources()
        {
            string resDir = "Assets/Resources/Transitions";
            if (!Directory.Exists(resDir))
                Directory.CreateDirectory(resDir);

            if (Directory.Exists(PrefabDir))
            {
                var files = Directory.GetFiles(PrefabDir, "*.prefab");
                foreach (var f in files)
                {
                    string fname = Path.GetFileName(f);
                    string target = Path.Combine(resDir, fname);
                    File.Copy(f, target, true);
                }
            }
        }

        public static void EnsureBuildSettingsScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Scenes/ButtonScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SceneA.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SceneB.unity", true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("<color=#00BFFF>[Build Settings]</color> ButtonScene, SceneA, SceneB 3개 씬이 Build Settings에 정상 등록되었습니다.");
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            string path = "Assets/Resources/CircleMask.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.48f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateRoundedArcSprite(string name, float startAngle, float endAngle, float innerRRatio, float outerRRatio)
        {
            string path = $"Assets/Resources/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float meanR = size * (innerRRatio + outerRRatio) * 0.5f;
            float halfT = size * (outerRRatio - innerRRatio) * 0.5f;

            float rad1 = startAngle * Mathf.Deg2Rad;
            float rad2 = endAngle * Mathf.Deg2Rad;
            Vector2 cap1 = center + new Vector2(Mathf.Cos(rad1), Mathf.Sin(rad1)) * meanR;
            Vector2 cap2 = center + new Vector2(Mathf.Cos(rad2), Mathf.Sin(rad2)) * meanR;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float distCenter = Vector2.Distance(p, center);
                    float ang = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                    if (ang < 0) ang += 360f;

                    bool inAngle = (ang >= startAngle && ang <= endAngle);
                    float bestDist = -100f;

                    if (inAngle)
                    {
                        float dRad = Mathf.Abs(distCenter - meanR);
                        bestDist = Mathf.Max(bestDist, halfT - dRad);
                    }

                    float dCap1 = Vector2.Distance(p, cap1);
                    bestDist = Mathf.Max(bestDist, halfT - dCap1);

                    float dCap2 = Vector2.Distance(p, cap2);
                    bestDist = Mathf.Max(bestDist, halfT - dCap2);

                    float alpha = Mathf.Clamp01(bestDist + 0.5f);
                    if (alpha > 0f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateArcSprite()
        {
            string path = "Assets/Resources/ArcLine.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerRadius = size * 0.46f;
            float innerRadius = size * 0.36f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);
                    float angle = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // 120도(1/3원) 부채꼴 호
                    bool inArcAngle = angle >= 30f && angle <= 150f;

                    if (dist >= innerRadius && dist <= outerRadius && inArcAngle)
                    {
                        float edgeDist = Mathf.Min(dist - innerRadius, outerRadius - dist);
                        float alpha = Mathf.Clamp01(edgeDist / 3f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateCapsuleSprite()
        {
            string path = "Assets/Resources/CapsuleRounded.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            int width = 256;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float radius = height * 0.5f; // 64f
            Vector2 leftCenter = new Vector2(radius, radius);
            Vector2 rightCenter = new Vector2(width - radius, radius);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist;
                    if (x < radius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), leftCenter);
                    }
                    else if (x > width - radius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), rightCenter);
                    }
                    else
                    {
                        dist = Mathf.Abs(y - radius);
                    }

                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.spriteBorder = new Vector4(64, 64, 64, 64);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateShootingStarTailSprite()
        {
            string path = "Assets/Resources/ShootingStarTail.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            int width = 512;
            int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float centerY = (height - 1) * 0.5f; // 31.5f
            float headRadius = 14f;
            float headCenterX = width - 1 - headRadius; // 머리 중심 x (약 497)

            Color tailColor = new Color(1.0f, 0.85f, 0.45f); // 웜 골드
            Color midColor = new Color(1.0f, 0.95f, 0.70f);  // 웜 옐로우
            Color headColor = new Color(1.0f, 1.0f, 1.0f);   // 퓨어 웜 화이트

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distY = Mathf.Abs(y - centerY);
                    float alpha = 0f;
                    Color col = headColor;

                    if (x <= headCenterX)
                    {
                        // 0 ~ headCenterX: 테이퍼드 꼬리
                        float t = Mathf.Clamp01(x / headCenterX);
                        float currentRadius = headRadius * Mathf.Pow(t, 1.25f);

                        if (distY <= currentRadius + 1.0f)
                        {
                            float edgeDist = currentRadius - distY;
                            float edgeAlpha = Mathf.Clamp01(edgeDist + 0.5f);
                            float xAlpha = Mathf.Pow(t, 1.15f);

                            if (t < 0.5f)
                                col = Color.Lerp(tailColor, midColor, t * 2f);
                            else
                                col = Color.Lerp(midColor, headColor, (t - 0.5f) * 2f);

                            float coreFactor = 1.0f - Mathf.Clamp01(distY / (currentRadius + 0.001f));
                            col = Color.Lerp(col, Color.white, coreFactor * 0.5f * t);

                            alpha = edgeAlpha * xAlpha;
                            col.a = alpha;
                        }
                    }
                    else
                    {
                        // headCenterX ~ width: 머리 둥근 반원
                        float dx = x - headCenterX;
                        float dist = Mathf.Sqrt(dx * dx + distY * distY);

                        if (dist <= headRadius + 1.0f)
                        {
                            float edgeAlpha = Mathf.Clamp01(headRadius - dist + 0.5f);
                            col = headColor;
                            alpha = edgeAlpha;
                            col.a = alpha;
                        }
                    }

                    tex.SetPixel(x, y, (alpha > 0f) ? col : Color.clear);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateGearCirclePrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("GearCircleTransition");
            var comp = root.AddComponent<GearCircleTransition>();

            // 1. Circle Cover
            GameObject circleGo = new GameObject("CircleCover", typeof(RectTransform), typeof(Image));
            circleGo.transform.SetParent(root.transform, false);
            RectTransform circleRt = circleGo.GetComponent<RectTransform>();
            circleRt.sizeDelta = new Vector2(700, 700);
            circleRt.localScale = Vector3.zero;
            Image circleImg = circleGo.GetComponent<Image>();
            circleImg.sprite = circleSprite;
            circleImg.color = new Color(0.96f, 0.58f, 0.23f); // 따뜻한 앰버 오렌지

            // 2. Gear Root (대폭 확대: 420x420)
            GameObject gearGo = new GameObject("GearRoot", typeof(RectTransform));
            gearGo.transform.SetParent(root.transform, false);
            RectTransform gearRt = gearGo.GetComponent<RectTransform>();
            gearRt.sizeDelta = new Vector2(420, 420);
            gearRt.localScale = Vector3.zero;

            // Gear Hub
            GameObject gearHub = new GameObject("GearHub", typeof(RectTransform), typeof(Image));
            gearHub.transform.SetParent(gearGo.transform, false);
            RectTransform hubRt = gearHub.GetComponent<RectTransform>();
            hubRt.sizeDelta = new Vector2(280, 280);
            Image hubImg = gearHub.GetComponent<Image>();
            hubImg.sprite = circleSprite;
            hubImg.color = new Color(0.99f, 0.95f, 0.88f); // 크림 아이보리

            // Inner Hole
            GameObject holeGo = new GameObject("InnerHole", typeof(RectTransform), typeof(Image));
            holeGo.transform.SetParent(gearHub.transform, false);
            RectTransform holeRt = holeGo.GetComponent<RectTransform>();
            holeRt.sizeDelta = new Vector2(110, 110);
            Image holeImg = holeGo.GetComponent<Image>();
            holeImg.sprite = circleSprite;
            holeImg.color = new Color(0.55f, 0.32f, 0.16f); // 웜 브라운

            // 12 Teeth around hub for rich mechanical snap
            List<Image> gearImgs = new List<Image> { hubImg, holeImg };
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                GameObject tooth = new GameObject($"Tooth_{i}", typeof(RectTransform), typeof(Image));
                tooth.transform.SetParent(gearGo.transform, false);
                RectTransform toothRt = tooth.GetComponent<RectTransform>();
                toothRt.sizeDelta = new Vector2(48, 380);
                toothRt.localRotation = Quaternion.Euler(0, 0, angle);
                Image toothImg = tooth.GetComponent<Image>();
                toothImg.color = new Color(0.99f, 0.95f, 0.88f);
                gearImgs.Add(toothImg);
            }
            gearHub.transform.SetAsLastSibling();
            holeGo.transform.SetAsLastSibling();

            // Large Gear Emoji Text (⚙️)
            GameObject emojiGo = new GameObject("GearEmojiText", typeof(RectTransform), typeof(Text));
            emojiGo.transform.SetParent(gearGo.transform, false);
            RectTransform emojiRt = emojiGo.GetComponent<RectTransform>();
            emojiRt.sizeDelta = new Vector2(260, 260);
            Text emojiText = emojiGo.GetComponent<Text>();
            emojiText.text = "⚙";
            emojiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emojiText.fontSize = 140;
            emojiText.alignment = TextAnchor.MiddleCenter;
            emojiText.color = new Color(0.96f, 0.58f, 0.23f, 0.95f);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("circleCover").objectReferenceValue = circleRt;
            so.FindProperty("gearRoot").objectReferenceValue = gearRt;
            so.FindProperty("circleImage").objectReferenceValue = circleImg;
            so.FindProperty("gearEmojiText").objectReferenceValue = emojiText;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/GearCircleTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateGearCircleTickPrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("GearCircleTickTransition");
            var comp = root.AddComponent<GearCircleTickTransition>();

            // 1. Circle Cover (Warm Logistics 웜 앰버 오렌지 패밀리룩)
            GameObject circleGo = new GameObject("CircleCover", typeof(RectTransform), typeof(Image));
            circleGo.transform.SetParent(root.transform, false);
            RectTransform circleRt = circleGo.GetComponent<RectTransform>();
            circleRt.sizeDelta = new Vector2(700, 700);
            circleRt.localScale = Vector3.zero;
            Image circleImg = circleGo.GetComponent<Image>();
            circleImg.sprite = circleSprite;
            circleImg.color = new Color(0.96f, 0.58f, 0.23f); // 따뜻한 앰버 오렌지 (#F5943A)

            // 2. Gear Root (대형 420x420)
            GameObject gearGo = new GameObject("GearRoot", typeof(RectTransform));
            gearGo.transform.SetParent(root.transform, false);
            RectTransform gearRt = gearGo.GetComponent<RectTransform>();
            gearRt.sizeDelta = new Vector2(420, 420);
            gearRt.localScale = Vector3.zero;

            // Gear Hub (크림 아이보리)
            GameObject gearHub = new GameObject("GearHub", typeof(RectTransform), typeof(Image));
            gearHub.transform.SetParent(gearGo.transform, false);
            RectTransform hubRt = gearHub.GetComponent<RectTransform>();
            hubRt.sizeDelta = new Vector2(280, 280);
            Image hubImg = gearHub.GetComponent<Image>();
            hubImg.sprite = circleSprite;
            hubImg.color = new Color(0.99f, 0.95f, 0.88f); // 크림 아이보리 (#FCF2E0)

            // Inner Hole (웜 브라운)
            GameObject holeGo = new GameObject("InnerHole", typeof(RectTransform), typeof(Image));
            holeGo.transform.SetParent(gearHub.transform, false);
            RectTransform holeRt = holeGo.GetComponent<RectTransform>();
            holeRt.sizeDelta = new Vector2(110, 110);
            Image holeImg = holeGo.GetComponent<Image>();
            holeImg.sprite = circleSprite;
            holeImg.color = new Color(0.55f, 0.32f, 0.16f); // 웜 브라운 (#8C5229)

            // 12 Teeth around hub (크림 아이보리 12치 인더스트리얼 기어)
            List<Image> gearImgs = new List<Image> { hubImg, holeImg };
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                GameObject tooth = new GameObject($"Tooth_{i}", typeof(RectTransform), typeof(Image));
                tooth.transform.SetParent(gearGo.transform, false);
                RectTransform toothRt = tooth.GetComponent<RectTransform>();
                toothRt.sizeDelta = new Vector2(48, 380);
                toothRt.localRotation = Quaternion.Euler(0, 0, angle);
                Image toothImg = tooth.GetComponent<Image>();
                toothImg.color = new Color(0.99f, 0.95f, 0.88f);
                gearImgs.Add(toothImg);
            }
            gearHub.transform.SetAsLastSibling();
            holeGo.transform.SetAsLastSibling();

            // Gear Emoji Text (⚙) - 이질적인 시계판/초침 제거 완료
            GameObject emojiGo = new GameObject("GearEmojiText", typeof(RectTransform), typeof(Text));
            emojiGo.transform.SetParent(gearGo.transform, false);
            RectTransform emojiRt = emojiGo.GetComponent<RectTransform>();
            emojiRt.sizeDelta = new Vector2(260, 260);
            Text emojiText = emojiGo.GetComponent<Text>();
            emojiText.text = "⚙";
            emojiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emojiText.fontSize = 140;
            emojiText.alignment = TextAnchor.MiddleCenter;
            emojiText.color = new Color(0.96f, 0.58f, 0.23f, 0.95f);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("circleCover").objectReferenceValue = circleRt;
            so.FindProperty("gearRoot").objectReferenceValue = gearRt;
            so.FindProperty("circleImage").objectReferenceValue = circleImg;
            so.FindProperty("gearEmojiText").objectReferenceValue = emojiText;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/GearCircleTickTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBoxFlapPrefab()
        {
            GameObject root = CreateBaseTransitionRoot("BoxFlapTransition");
            var comp = root.AddComponent<BoxFlapTransition>();

            // Top Flap
            GameObject topGo = new GameObject("TopFlap", typeof(RectTransform), typeof(Image));
            topGo.transform.SetParent(root.transform, false);
            RectTransform topRt = topGo.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 0.5f);
            topRt.anchorMax = new Vector2(1, 1f);
            topRt.offsetMin = Vector2.zero;
            topRt.offsetMax = Vector2.zero;
            Image topImg = topGo.GetComponent<Image>();
            topImg.color = new Color(0.72f, 0.49f, 0.28f);

            // Bottom Flap
            GameObject botGo = new GameObject("BottomFlap", typeof(RectTransform), typeof(Image));
            botGo.transform.SetParent(root.transform, false);
            RectTransform botRt = botGo.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0, 0f);
            botRt.anchorMax = new Vector2(1, 0.5f);
            botRt.offsetMin = Vector2.zero;
            botRt.offsetMax = Vector2.zero;
            Image botImg = botGo.GetComponent<Image>();
            botImg.color = new Color(0.65f, 0.44f, 0.25f);

            // Tape Strip (Left-to-Right Tape with Pivot (0, 0.5))
            GameObject tapeGo = new GameObject("TapeStrip", typeof(RectTransform), typeof(Image));
            tapeGo.transform.SetParent(root.transform, false);
            RectTransform tapeRt = tapeGo.GetComponent<RectTransform>();
            tapeRt.anchorMin = new Vector2(0, 0.5f);
            tapeRt.anchorMax = new Vector2(1, 0.5f);
            tapeRt.pivot = new Vector2(0f, 0.5f);
            tapeRt.sizeDelta = new Vector2(0, 110);
            tapeRt.localScale = new Vector3(0, 1, 1);
            Image tapeImg = tapeGo.GetComponent<Image>();
            tapeImg.color = new Color(0.96f, 0.77f, 0.42f);

            // Tape Label Text (Upgraded Font Size 36 & Dark Ink Contrast #2A1808)
            GameObject textGo = new GameObject("TapeLabel", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(tapeGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text labelText = textGo.GetComponent<Text>();
            labelText.text = "★ EXPRESS COURIER BOX SEALED ★";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 36;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.16f, 0.09f, 0.03f, 0f); // #2A1808

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("topFlap").objectReferenceValue = topRt;
            so.FindProperty("bottomFlap").objectReferenceValue = botRt;
            so.FindProperty("tapeStrip").objectReferenceValue = tapeRt;
            so.FindProperty("tapeImage").objectReferenceValue = tapeImg;
            so.FindProperty("boxLabelText").objectReferenceValue = labelText;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/BoxFlapTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateTruckWipePrefab(Sprite circleSprite, Sprite arcSprite)
        {
            GameObject root = CreateBaseTransitionRoot("TruckWipeTransition");
            var comp = root.AddComponent<TruckWipeTransition>();

            // 1. Curtain Panel
            GameObject curtainGo = new GameObject("CurtainPanel", typeof(RectTransform), typeof(Image));
            curtainGo.transform.SetParent(root.transform, false);
            RectTransform curtainRt = curtainGo.GetComponent<RectTransform>();
            curtainRt.anchorMin = Vector2.zero;
            curtainRt.anchorMax = Vector2.one;
            curtainRt.offsetMin = Vector2.zero;
            curtainRt.offsetMax = Vector2.zero;
            curtainRt.pivot = new Vector2(0, 0.5f);
            curtainRt.localScale = new Vector3(0, 1, 1);
            Image curtainImg = curtainGo.GetComponent<Image>();
            curtainImg.color = new Color(0.98f, 0.77f, 0.28f); // 머스타드 옐로우

            // 2. Arcs Container (트럭 하단 3단 차등 속도 곡선 컨테이너)
            GameObject arcsContainerGo = new GameObject("ArcsContainer", typeof(RectTransform));
            arcsContainerGo.transform.SetParent(root.transform, false);
            RectTransform arcsContainerRt = arcsContainerGo.GetComponent<RectTransform>();
            arcsContainerRt.anchorMin = Vector2.zero;
            arcsContainerRt.anchorMax = Vector2.one;
            arcsContainerRt.offsetMin = Vector2.zero;
            arcsContainerRt.offsetMax = Vector2.zero;
            arcsContainerRt.anchoredPosition = Vector2.zero;

            // 3단 둥근 아크 스프라이트 생성
            Sprite arcWide = GetOrCreateRoundedArcSprite("RoundedArc_Wide", 40f, 140f, 0.38f, 0.44f);
            Sprite arcMedium = GetOrCreateRoundedArcSprite("RoundedArc_Medium", 45f, 120f, 0.39f, 0.43f);
            Sprite arcBase = GetOrCreateRoundedArcSprite("RoundedArc_Base", 30f, 150f, 0.37f, 0.44f);

            // Track 3 (하단 딥차콜선: 3.0s 안정속)
            GameObject t3Go = new GameObject("Track3_Charcoal", typeof(RectTransform));
            t3Go.transform.SetParent(arcsContainerGo.transform, false);
            RectTransform t3Rt = t3Go.GetComponent<RectTransform>();
            t3Rt.sizeDelta = new Vector2(1300, 1300);
            t3Rt.anchoredPosition = new Vector2(0, -660);

            GameObject arc3ImgGo = new GameObject("Arc3Visual", typeof(RectTransform), typeof(Image));
            arc3ImgGo.transform.SetParent(t3Go.transform, false);
            RectTransform arc3ImgRt = arc3ImgGo.GetComponent<RectTransform>();
            arc3ImgRt.anchorMin = Vector2.zero;
            arc3ImgRt.anchorMax = Vector2.one;
            arc3ImgRt.offsetMin = Vector2.zero;
            arc3ImgRt.offsetMax = Vector2.zero;
            Image arc3Img = arc3ImgGo.GetComponent<Image>();
            arc3Img.sprite = arcBase;
            arc3Img.color = new Color(0.21f, 0.19f, 0.20f, 0.95f); // 딥 차콜 (#363032)

            // Track 2 (중간 크림선: 2.1s 중속)
            GameObject t2Go = new GameObject("Track2_Cream", typeof(RectTransform));
            t2Go.transform.SetParent(arcsContainerGo.transform, false);
            RectTransform t2Rt = t2Go.GetComponent<RectTransform>();
            t2Rt.sizeDelta = new Vector2(1370, 1370);
            t2Rt.anchoredPosition = new Vector2(0, -660);

            GameObject arc2ImgGo = new GameObject("Arc2Visual", typeof(RectTransform), typeof(Image));
            arc2ImgGo.transform.SetParent(t2Go.transform, false);
            RectTransform arc2ImgRt = arc2ImgGo.GetComponent<RectTransform>();
            arc2ImgRt.anchorMin = Vector2.zero;
            arc2ImgRt.anchorMax = Vector2.one;
            arc2ImgRt.offsetMin = Vector2.zero;
            arc2ImgRt.offsetMax = Vector2.zero;
            Image arc2Img = arc2ImgGo.GetComponent<Image>();
            arc2Img.sprite = arcMedium;
            arc2Img.color = new Color(0.96f, 0.90f, 0.70f, 0.95f); // 소프트 크림 베이지 (#F5E6B3)

            // Track 1 (상단 화이트 접지선: 1.4s 최고속)
            GameObject t1Go = new GameObject("Track1_White", typeof(RectTransform));
            t1Go.transform.SetParent(arcsContainerGo.transform, false);
            RectTransform t1Rt = t1Go.GetComponent<RectTransform>();
            t1Rt.sizeDelta = new Vector2(1440, 1440);
            t1Rt.anchoredPosition = new Vector2(0, -660);

            GameObject arc1ImgGo = new GameObject("Arc1Visual", typeof(RectTransform), typeof(Image));
            arc1ImgGo.transform.SetParent(t1Go.transform, false);
            RectTransform arc1ImgRt = arc1ImgGo.GetComponent<RectTransform>();
            arc1ImgRt.anchorMin = Vector2.zero;
            arc1ImgRt.anchorMax = Vector2.one;
            arc1ImgRt.offsetMin = Vector2.zero;
            arc1ImgRt.offsetMax = Vector2.zero;
            Image arc1Img = arc1ImgGo.GetComponent<Image>();
            arc1Img.sprite = arcWide;
            arc1Img.color = Color.white; // 크리스프 화이트

            // Speed Dots Track (잔상 도트선: 1.8s)
            GameObject dotsGo = new GameObject("SpeedDotsTrack", typeof(RectTransform));
            dotsGo.transform.SetParent(arcsContainerGo.transform, false);
            RectTransform dotsRt = dotsGo.GetComponent<RectTransform>();
            dotsRt.sizeDelta = new Vector2(1400, 1400);
            dotsRt.anchoredPosition = new Vector2(0, -660);

            Vector2[] dotAngles = new Vector2[] { new Vector2(150f, 13f), new Vector2(158f, 10f), new Vector2(166f, 8f) };
            for (int i = 0; i < dotAngles.Length; i++)
            {
                float rad = dotAngles[i].x * Mathf.Deg2Rad;
                float dRadius = 1400f * 0.41f;
                GameObject d = new GameObject($"SpeedDot_{i}", typeof(RectTransform), typeof(Image));
                d.transform.SetParent(dotsGo.transform, false);
                RectTransform drt = d.GetComponent<RectTransform>();
                drt.anchoredPosition = new Vector2(Mathf.Cos(rad) * dRadius, Mathf.Sin(rad) * dRadius);
                drt.sizeDelta = new Vector2(dotAngles[i].y, dotAngles[i].y);
                Image dimg = d.GetComponent<Image>();
                dimg.sprite = circleSprite;
                dimg.color = (i == 0) ? Color.white : new Color(0.96f, 0.90f, 0.70f, 0.85f);
            }

            // 3. Truck Root (좌표 이동 전담)
            GameObject truckGo = new GameObject("TruckRoot", typeof(RectTransform));
            truckGo.transform.SetParent(root.transform, false);
            RectTransform truckRt = truckGo.GetComponent<RectTransform>();
            truckRt.sizeDelta = new Vector2(300, 180);
            truckRt.anchoredPosition = new Vector2(0, -10f); // 1번 화이트 아크 정점에 바퀴 접지

            // 4. Visual Body (진동/흔들림 전담)
            GameObject visualGo = new GameObject("VisualBody", typeof(RectTransform));
            visualGo.transform.SetParent(truckGo.transform, false);
            RectTransform visualRt = visualGo.GetComponent<RectTransform>();
            visualRt.anchorMin = Vector2.zero;
            visualRt.anchorMax = Vector2.one;
            visualRt.offsetMin = Vector2.zero;
            visualRt.offsetMax = Vector2.zero;

            // Truck Cargo Body
            GameObject bodyGo = new GameObject("CargoBody", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(visualGo.transform, false);
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchoredPosition = new Vector2(-45, 18);
            bodyRt.sizeDelta = new Vector2(190, 130);
            Image bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.color = new Color(0.91f, 0.35f, 0.24f); // 웜 레드오렌지

            // Truck Cab
            GameObject cabGo = new GameObject("TruckCab", typeof(RectTransform), typeof(Image));
            cabGo.transform.SetParent(visualGo.transform, false);
            RectTransform cabRt = cabGo.GetComponent<RectTransform>();
            cabRt.anchoredPosition = new Vector2(95, 0);
            cabRt.sizeDelta = new Vector2(90, 90);
            Image cabImg = cabGo.GetComponent<Image>();
            cabImg.color = new Color(0.82f, 0.28f, 0.18f);

            // Cab Window
            GameObject winGo = new GameObject("CabWindow", typeof(RectTransform), typeof(Image));
            winGo.transform.SetParent(cabGo.transform, false);
            RectTransform winRt = winGo.GetComponent<RectTransform>();
            winRt.anchoredPosition = new Vector2(12, 16);
            winRt.sizeDelta = new Vector2(40, 34);
            Image winImg = winGo.GetComponent<Image>();
            winImg.color = new Color(0.9f, 0.95f, 1f);

            // Express Text on Cargo
            GameObject logoGo = new GameObject("ExpressLogo", typeof(RectTransform), typeof(Text));
            logoGo.transform.SetParent(bodyGo.transform, false);
            RectTransform logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = Vector2.zero;
            logoRt.anchorMax = Vector2.one;
            logoRt.offsetMin = Vector2.zero;
            logoRt.offsetMax = Vector2.zero;
            Text logoTxt = logoGo.GetComponent<Text>();
            logoTxt.text = "⚡ EXPRESS";
            logoTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logoTxt.fontSize = 24;
            logoTxt.fontStyle = FontStyle.Bold;
            logoTxt.alignment = TextAnchor.MiddleCenter;
            logoTxt.color = Color.white;

            // Wheels
            RectTransform frontWheelRt = CreateWheel(visualGo.transform, new Vector2(80, -48), circleSprite, "FrontWheel");
            RectTransform rearWheelRt = CreateWheel(visualGo.transform, new Vector2(-80, -48), circleSprite, "RearWheel");

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("curtainPanel").objectReferenceValue = curtainRt;
            so.FindProperty("truckRoot").objectReferenceValue = truckRt;
            so.FindProperty("truckBodyVisual").objectReferenceValue = visualRt;
            so.FindProperty("frontWheel").objectReferenceValue = frontWheelRt;
            so.FindProperty("rearWheel").objectReferenceValue = rearWheelRt;

            so.FindProperty("arcsContainer").objectReferenceValue = arcsContainerRt;
            so.FindProperty("track1White").objectReferenceValue = t1Rt;
            so.FindProperty("track2Cream").objectReferenceValue = t2Rt;
            so.FindProperty("track3Charcoal").objectReferenceValue = t3Rt;
            so.FindProperty("speedDotsTrack").objectReferenceValue = dotsRt;

            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/TruckWipeTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform CreateWheel(Transform parent, Vector2 pos, Sprite circleSprite, string name)
        {
            GameObject wheel = new GameObject(name, typeof(RectTransform), typeof(Image));
            wheel.transform.SetParent(parent, false);
            RectTransform rt = wheel.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(46, 46);
            Image img = wheel.GetComponent<Image>();
            img.sprite = circleSprite;
            img.color = new Color(0.2f, 0.2f, 0.22f);

            // Rim
            GameObject rim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
            rim.transform.SetParent(wheel.transform, false);
            RectTransform rimRt = rim.GetComponent<RectTransform>();
            rimRt.sizeDelta = new Vector2(20, 20);
            Image rimImg = rim.GetComponent<Image>();
            rimImg.sprite = circleSprite;
            rimImg.color = new Color(0.88f, 0.88f, 0.88f);

            return rt;
        }

        private static RectTransform CreateWheelArc(Transform parent, Vector2 pos, float size, Sprite arcSprite, string name)
        {
            GameObject arcGo = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            arcGo.transform.SetParent(parent, false);
            RectTransform rt = arcGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            Image img = arcGo.GetComponent<Image>();
            img.sprite = arcSprite;
            img.color = new Color(1f, 1f, 1f, 0.75f);
            CanvasGroup cg = arcGo.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            return rt;
        }

        private static GameObject CreateConveyorShutterPrefab()
        {
            GameObject root = CreateBaseTransitionRoot("ConveyorShutterTransition");
            var comp = root.AddComponent<ConveyorShutterTransition>();

            // Shutter Root Container (수직 이동 컨테이너)
            GameObject shutterRootGo = new GameObject("ShutterRoot", typeof(RectTransform));
            shutterRootGo.transform.SetParent(root.transform, false);
            RectTransform shutterRootRt = shutterRootGo.GetComponent<RectTransform>();
            shutterRootRt.anchorMin = Vector2.zero;
            shutterRootRt.anchorMax = Vector2.one;
            shutterRootRt.offsetMin = Vector2.zero;
            shutterRootRt.offsetMax = Vector2.zero;

            // 10개의 다크 스틸 롤 셔터 슬랫 (입체적인 음영 하이라이트)
            int slatCount = 10;
            RectTransform[] slats = new RectTransform[slatCount];
            float slatHeight = 1f / slatCount;

            Color colDark = new Color(0.18f, 0.22f, 0.26f);
            Color colLight = new Color(0.24f, 0.29f, 0.34f);
            Color highlightColor = new Color(0.38f, 0.44f, 0.52f, 0.7f);
            Color shadowColor = new Color(0.10f, 0.12f, 0.15f, 0.9f);

            for (int i = 0; i < slatCount; i++)
            {
                GameObject slat = new GameObject($"Slat_{i}", typeof(RectTransform), typeof(Image));
                slat.transform.SetParent(shutterRootGo.transform, false);
                RectTransform rt = slat.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, i * slatHeight);
                rt.anchorMax = new Vector2(1, (i + 1) * slatHeight);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Image img = slat.GetComponent<Image>();
                img.color = (i % 2 == 0) ? colDark : colLight;
                slats[i] = rt;

                // 상단 엠보싱 하이라이트 라인
                GameObject hl = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
                hl.transform.SetParent(slat.transform, false);
                RectTransform hlRt = hl.GetComponent<RectTransform>();
                hlRt.anchorMin = new Vector2(0, 1);
                hlRt.anchorMax = new Vector2(1, 1);
                hlRt.pivot = new Vector2(0.5f, 1f);
                hlRt.sizeDelta = new Vector2(0, 4);
                Image hlImg = hl.GetComponent<Image>();
                hlImg.color = highlightColor;

                // 하단 그림자 라인
                GameObject sd = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                sd.transform.SetParent(slat.transform, false);
                RectTransform sdRt = sd.GetComponent<RectTransform>();
                sdRt.anchorMin = new Vector2(0, 0);
                sdRt.anchorMax = new Vector2(1, 0);
                sdRt.pivot = new Vector2(0.5f, 0f);
                sdRt.sizeDelta = new Vector2(0, 4);
                Image sdImg = sd.GetComponent<Image>();
                sdImg.color = shadowColor;

                // 좌우 롤러 가이드 캡
                for (int side = 0; side < 2; side++)
                {
                    GameObject cap = new GameObject(side == 0 ? "Cap_L" : "Cap_R", typeof(RectTransform), typeof(Image));
                    cap.transform.SetParent(slat.transform, false);
                    RectTransform capRt = cap.GetComponent<RectTransform>();
                    capRt.anchorMin = new Vector2(side == 0 ? 0 : 1, 0);
                    capRt.anchorMax = new Vector2(side == 0 ? 0 : 1, 1);
                    capRt.pivot = new Vector2(side == 0 ? 0 : 1, 0.5f);
                    capRt.sizeDelta = new Vector2(30, 0);
                    Image capImg = cap.GetComponent<Image>();
                    capImg.color = new Color(0.12f, 0.14f, 0.17f);
                }
            }

            // BottomBar: 하단 묵직한 강철 바 + 안전 사선 스트라이프(Caution Hazard Stripe) + 손잡이
            GameObject bottomBarGo = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bottomBarGo.transform.SetParent(shutterRootGo.transform, false);
            RectTransform bbarRt = bottomBarGo.GetComponent<RectTransform>();
            bbarRt.anchorMin = new Vector2(0, 0);
            bbarRt.anchorMax = new Vector2(1, 0);
            bbarRt.pivot = new Vector2(0.5f, 0);
            bbarRt.sizeDelta = new Vector2(0, 85);
            Image bbarImg = bottomBarGo.GetComponent<Image>();
            bbarImg.color = new Color(0.14f, 0.16f, 0.19f);

            // 상단 은빛 메탈 립
            GameObject bbarLip = new GameObject("Lip", typeof(RectTransform), typeof(Image));
            bbarLip.transform.SetParent(bottomBarGo.transform, false);
            RectTransform lipRt = bbarLip.GetComponent<RectTransform>();
            lipRt.anchorMin = new Vector2(0, 1);
            lipRt.anchorMax = new Vector2(1, 1);
            lipRt.pivot = new Vector2(0.5f, 1f);
            lipRt.sizeDelta = new Vector2(0, 6);
            Image lipImg = bbarLip.GetComponent<Image>();
            lipImg.color = new Color(0.6f, 0.65f, 0.7f);

            // 노랑/검정 사선 위험 스트라이프 바 (Caution Hazard Stripe Bar)
            GameObject hazardBar = new GameObject("HazardStripeBar", typeof(RectTransform), typeof(Image));
            hazardBar.transform.SetParent(bottomBarGo.transform, false);
            RectTransform hbarRt = hazardBar.GetComponent<RectTransform>();
            hbarRt.anchorMin = new Vector2(0.02f, 0.35f);
            hbarRt.anchorMax = new Vector2(0.98f, 0.85f);
            hbarRt.offsetMin = Vector2.zero;
            hbarRt.offsetMax = Vector2.zero;
            Image hbarImg = hazardBar.GetComponent<Image>();
            hbarImg.color = new Color(0.1f, 0.1f, 0.1f);

            // 18개의 노란 사선 스트라이프
            for (int s = 0; s < 18; s++)
            {
                GameObject stripe = new GameObject($"Stripe_{s}", typeof(RectTransform), typeof(Image));
                stripe.transform.SetParent(hazardBar.transform, false);
                RectTransform sRt = stripe.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(s / 18f, 0);
                sRt.anchorMax = new Vector2(s / 18f, 1);
                sRt.sizeDelta = new Vector2(24, 0);
                sRt.localRotation = Quaternion.Euler(0, 0, -35f);
                Image sImg = stripe.GetComponent<Image>();
                sImg.color = new Color(0.96f, 0.76f, 0.15f);
            }

            // 하단 강철 손잡이 2개
            for (int h = 0; h < 2; h++)
            {
                GameObject handle = new GameObject(h == 0 ? "Handle_L" : "Handle_R", typeof(RectTransform), typeof(Image));
                handle.transform.SetParent(bottomBarGo.transform, false);
                RectTransform handleRt = handle.GetComponent<RectTransform>();
                handleRt.anchorMin = new Vector2(h == 0 ? 0.32f : 0.68f, 0.15f);
                handleRt.anchorMax = new Vector2(h == 0 ? 0.32f : 0.68f, 0.15f);
                handleRt.sizeDelta = new Vector2(90, 16);
                Image handleImg = handle.GetComponent<Image>();
                handleImg.color = new Color(0.68f, 0.72f, 0.78f);
            }

            // Center Warning Badge ("⚠️ WAREHOUSE SHUTTER CLOSED" 스텐실)
            GameObject badgeGo = new GameObject("CenterBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(shutterRootGo.transform, false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRt.sizeDelta = new Vector2(620, 120);
            Image badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = new Color(0.12f, 0.14f, 0.17f, 0.95f);

            // 배지 노란 테두리
            GameObject badgeBorder = new GameObject("Border", typeof(RectTransform), typeof(Image));
            badgeBorder.transform.SetParent(badgeGo.transform, false);
            RectTransform bbRt = badgeBorder.GetComponent<RectTransform>();
            bbRt.anchorMin = Vector2.zero;
            bbRt.anchorMax = Vector2.one;
            bbRt.sizeDelta = Vector2.zero;
            Image bbImg = badgeBorder.GetComponent<Image>();
            bbImg.color = new Color(0.96f, 0.76f, 0.15f);

            GameObject badgeInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            badgeInner.transform.SetParent(badgeGo.transform, false);
            RectTransform biRt = badgeInner.GetComponent<RectTransform>();
            biRt.anchorMin = Vector2.zero;
            biRt.anchorMax = Vector2.one;
            biRt.offsetMin = new Vector2(4, 4);
            biRt.offsetMax = new Vector2(-4, -4);
            Image biImg = badgeInner.GetComponent<Image>();
            biImg.color = new Color(0.14f, 0.16f, 0.20f);

            // 상단 주황 경광등 (Warning Beacon Light)
            GameObject beaconGo = new GameObject("WarningBeacon", typeof(RectTransform), typeof(Image));
            beaconGo.transform.SetParent(badgeGo.transform, false);
            RectTransform beaconRt = beaconGo.GetComponent<RectTransform>();
            beaconRt.anchoredPosition = new Vector2(0, 75);
            beaconRt.sizeDelta = new Vector2(46, 46);
            Image beaconImg = beaconGo.GetComponent<Image>();
            beaconImg.color = new Color(1f, 0.35f, 0.05f);

            // 스텐실 텍스트
            GameObject textGo = new GameObject("BadgeText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(badgeGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.text = "⚠️ WAREHOUSE SHUTTER CLOSED ⚠️\nLOGISTICS CONTROL CENTER";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.76f, 0.15f);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("shutterRoot").objectReferenceValue = shutterRootRt;
            SerializedProperty slatProp = so.FindProperty("shutterSlats");
            slatProp.arraySize = slatCount;
            for (int i = 0; i < slatCount; i++)
                slatProp.GetArrayElementAtIndex(i).objectReferenceValue = slats[i];
            so.FindProperty("bottomBar").objectReferenceValue = bbarRt;
            so.FindProperty("centerBadge").objectReferenceValue = badgeRt;
            so.FindProperty("badgeText").objectReferenceValue = text;
            so.FindProperty("warningBeacon").objectReferenceValue = beaconImg;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/ConveyorShutterTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateStampInvoicePrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("StampInvoiceTransition");
            var comp = root.AddComponent<StampInvoiceTransition>();

            // 1. 실감 서류봉투 (Korean Manila Courier Envelope)
            GameObject paperGo = new GameObject("InvoicePaper", typeof(RectTransform), typeof(Image));
            paperGo.transform.SetParent(root.transform, false);
            RectTransform paperRt = paperGo.GetComponent<RectTransform>();
            paperRt.anchorMin = new Vector2(0.5f, 0.5f);
            paperRt.anchorMax = new Vector2(0.5f, 0.5f);
            paperRt.sizeDelta = new Vector2(1460, 940);
            Image paperImg = paperGo.GetComponent<Image>();
            paperImg.color = new Color(0.89f, 0.77f, 0.58f); // 크라프트 서류봉투 황토색

            // 봉투 그림자 및 외곽 라인
            GameObject shadowGo = new GameObject("EnvelopeBorder", typeof(RectTransform), typeof(Image));
            shadowGo.transform.SetParent(paperGo.transform, false);
            shadowGo.transform.SetAsFirstSibling();
            RectTransform sRt = shadowGo.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(-6, -6);
            sRt.offsetMax = new Vector2(6, 6);
            Image sImg = shadowGo.GetComponent<Image>();
            sImg.color = new Color(0.68f, 0.55f, 0.38f, 0.4f);

            // 좌측 상단: [ 보내는 사람 ] 섹션
            GameObject senderTitle = new GameObject("SenderTitle", typeof(RectTransform), typeof(Text));
            senderTitle.transform.SetParent(paperGo.transform, false);
            RectTransform stRt = senderTitle.GetComponent<RectTransform>();
            stRt.anchoredPosition = new Vector2(-460, 360);
            stRt.sizeDelta = new Vector2(240, 38);
            Text stTxt = senderTitle.GetComponent<Text>();
            stTxt.text = "보내는 사람";
            stTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stTxt.fontSize = 24;
            stTxt.fontStyle = FontStyle.Bold;
            stTxt.color = new Color(0.24f, 0.18f, 0.12f);

            // 보내는 사람 밑줄선 2개
            for (int i = 0; i < 2; i++)
            {
                GameObject line = new GameObject($"SenderLine_{i}", typeof(RectTransform), typeof(Image));
                line.transform.SetParent(paperGo.transform, false);
                RectTransform lRt = line.GetComponent<RectTransform>();
                lRt.anchoredPosition = new Vector2(-360, 305 - (i * 48));
                lRt.sizeDelta = new Vector2(460, 2);
                Image lImg = line.GetComponent<Image>();
                lImg.color = new Color(0.55f, 0.45f, 0.35f, 0.7f);
            }

            // 보내는 사람 전화번호 라인 (☎):
            GameObject sTel = new GameObject("SenderTel", typeof(RectTransform), typeof(Text));
            sTel.transform.SetParent(paperGo.transform, false);
            RectTransform stlRt = sTel.GetComponent<RectTransform>();
            stlRt.anchoredPosition = new Vector2(-540, 210);
            stlRt.sizeDelta = new Vector2(100, 32);
            Text stlTxt = sTel.GetComponent<Text>();
            stlTxt.text = "( ☎ ):";
            stlTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stlTxt.fontSize = 20;
            stlTxt.fontStyle = FontStyle.Bold;
            stlTxt.color = new Color(0.24f, 0.18f, 0.12f);

            GameObject sTelLine = new GameObject("SenderTelLine", typeof(RectTransform), typeof(Image));
            sTelLine.transform.SetParent(paperGo.transform, false);
            RectTransform stlLineRt = sTelLine.GetComponent<RectTransform>();
            stlLineRt.anchoredPosition = new Vector2(-330, 206);
            stlLineRt.sizeDelta = new Vector2(400, 2);
            Image stlLineImg = sTelLine.GetComponent<Image>();
            stlLineImg.color = new Color(0.55f, 0.45f, 0.35f, 0.7f);

            // 보내는 사람 5자리 빨간 우편번호 박스 [ ][ ][ ][ ][ ]
            CreateZipCodeBoxes(paperGo.transform, new Vector2(-460, 130), new Color(0.82f, 0.22f, 0.18f));

            // 우측 하단: [ 받는 사람 ] 섹션
            GameObject receiverTitle = new GameObject("ReceiverTitle", typeof(RectTransform), typeof(Text));
            receiverTitle.transform.SetParent(paperGo.transform, false);
            RectTransform rtRt = receiverTitle.GetComponent<RectTransform>();
            rtRt.anchoredPosition = new Vector2(30, 30);
            rtRt.sizeDelta = new Vector2(240, 38);
            Text rtTxt = receiverTitle.GetComponent<Text>();
            rtTxt.text = "받는 사람";
            rtTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rtTxt.fontSize = 26;
            rtTxt.fontStyle = FontStyle.Bold;
            rtTxt.color = new Color(0.24f, 0.18f, 0.12f);

            // 받는 사람 배송지 밑줄선 2개
            for (int i = 0; i < 2; i++)
            {
                GameObject line = new GameObject($"ReceiverLine_{i}", typeof(RectTransform), typeof(Image));
                line.transform.SetParent(paperGo.transform, false);
                RectTransform lRt = line.GetComponent<RectTransform>();
                lRt.anchoredPosition = new Vector2(210, -25 - (i * 54));
                lRt.sizeDelta = new Vector2(580, 2);
                Image lImg = line.GetComponent<Image>();
                lImg.color = new Color(0.55f, 0.45f, 0.35f, 0.7f);
            }

            // 받는 사람 전화번호 라인 (☎):
            GameObject rTel = new GameObject("ReceiverTel", typeof(RectTransform), typeof(Text));
            rTel.transform.SetParent(paperGo.transform, false);
            RectTransform rtlRt = rTel.GetComponent<RectTransform>();
            rtlRt.anchoredPosition = new Vector2(0, -132);
            rtlRt.sizeDelta = new Vector2(100, 32);
            Text rtlTxt = rTel.GetComponent<Text>();
            rtlTxt.text = "( ☎ ):";
            rtlTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rtlTxt.fontSize = 20;
            rtlTxt.fontStyle = FontStyle.Bold;
            rtlTxt.color = new Color(0.24f, 0.18f, 0.12f);

            GameObject rTelLine = new GameObject("ReceiverTelLine", typeof(RectTransform), typeof(Image));
            rTelLine.transform.SetParent(paperGo.transform, false);
            RectTransform rtlLineRt = rTelLine.GetComponent<RectTransform>();
            rtlLineRt.anchoredPosition = new Vector2(240, -136);
            rtlLineRt.sizeDelta = new Vector2(520, 2);
            Image rtlLineImg = rTelLine.GetComponent<Image>();
            rtlLineImg.color = new Color(0.55f, 0.45f, 0.35f, 0.7f);

            // 받는 사람 5자리 빨간 우편번호 박스 [ ][ ][ ][ ][ ]
            CreateZipCodeBoxes(paperGo.transform, new Vector2(360, -220), new Color(0.82f, 0.22f, 0.18f));

            // 우측 상단: [ 요금별납 ] 우편 마크
            GameObject postageGo = new GameObject("PostageStamp", typeof(RectTransform), typeof(Image));
            postageGo.transform.SetParent(paperGo.transform, false);
            RectTransform postRt = postageGo.GetComponent<RectTransform>();
            postRt.anchoredPosition = new Vector2(540, 340);
            postRt.sizeDelta = new Vector2(105, 90);
            Image postImg = postageGo.GetComponent<Image>();
            postImg.color = new Color(0.82f, 0.22f, 0.18f);

            GameObject postInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            postInner.transform.SetParent(postageGo.transform, false);
            RectTransform piRt = postInner.GetComponent<RectTransform>();
            piRt.anchorMin = Vector2.zero;
            piRt.anchorMax = Vector2.one;
            piRt.offsetMin = new Vector2(3, 3);
            piRt.offsetMax = new Vector2(-3, -3);
            Image piImg = postInner.GetComponent<Image>();
            piImg.color = new Color(0.89f, 0.77f, 0.58f);

            GameObject postTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            postTxtGo.transform.SetParent(postageGo.transform, false);
            RectTransform ptRt = postTxtGo.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero;
            ptRt.offsetMax = Vector2.zero;
            Text ptTxt = postTxtGo.GetComponent<Text>();
            ptTxt.text = "요 금\n별 납\n(우편)";
            ptTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ptTxt.fontSize = 17;
            ptTxt.fontStyle = FontStyle.Bold;
            ptTxt.alignment = TextAnchor.MiddleCenter;
            ptTxt.color = new Color(0.82f, 0.22f, 0.18f);

            // 좌측 하단: [ 물류 운송장 바코드 ]
            GameObject barcodeGo = new GameObject("InvoiceBarcode", typeof(RectTransform), typeof(Image));
            barcodeGo.transform.SetParent(paperGo.transform, false);
            RectTransform bcRt = barcodeGo.GetComponent<RectTransform>();
            bcRt.anchoredPosition = new Vector2(-420, -280);
            bcRt.sizeDelta = new Vector2(360, 75);
            Image bcImg = barcodeGo.GetComponent<Image>();
            bcImg.color = new Color(0.96f, 0.94f, 0.88f);

            // 바코드 스트라이프들
            for (int b = 0; b < 16; b++)
            {
                GameObject bar = new GameObject($"Bar_{b}", typeof(RectTransform), typeof(Image));
                bar.transform.SetParent(barcodeGo.transform, false);
                RectTransform bRt = bar.GetComponent<RectTransform>();
                bRt.anchoredPosition = new Vector2(-150 + (b * 20), 8);
                bRt.sizeDelta = new Vector2((b % 3 == 0) ? 8 : 4, 38);
                Image barImg = bar.GetComponent<Image>();
                barImg.color = new Color(0.2f, 0.2f, 0.2f);
            }

            GameObject bcTxtGo = new GameObject("CodeText", typeof(RectTransform), typeof(Text));
            bcTxtGo.transform.SetParent(barcodeGo.transform, false);
            RectTransform bctRt = bcTxtGo.GetComponent<RectTransform>();
            bctRt.anchoredPosition = new Vector2(0, -22);
            bctRt.sizeDelta = new Vector2(340, 20);
            Text bctTxt = bcTxtGo.GetComponent<Text>();
            bctTxt.text = "REG-KR 9840-2026-X";
            bctTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bctTxt.fontSize = 14;
            bctTxt.alignment = TextAnchor.MiddleCenter;
            bctTxt.color = new Color(0.3f, 0.3f, 0.3f);

            // 2. 대형 배송 승인 스탬프 (쾅! 강타 마크)
            GameObject stampGo = new GameObject("StampMark", typeof(RectTransform), typeof(Image));
            stampGo.transform.SetParent(paperGo.transform, false);
            RectTransform stampRt = stampGo.GetComponent<RectTransform>();
            stampRt.anchoredPosition = new Vector2(10, 20);
            stampRt.sizeDelta = new Vector2(460, 160);
            stampRt.localRotation = Quaternion.Euler(0, 0, -12f);
            stampRt.localScale = Vector3.one * 2.5f;
            Image stampBorder = stampGo.GetComponent<Image>();
            stampBorder.color = new Color(0.85f, 0.20f, 0.16f);

            GameObject stampInner = new GameObject("StampInner", typeof(RectTransform), typeof(Image));
            stampInner.transform.SetParent(stampGo.transform, false);
            RectTransform innerRt = stampInner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(7, 7);
            innerRt.offsetMax = new Vector2(-7, -7);
            Image innerImg = stampInner.GetComponent<Image>();
            innerImg.color = new Color(0.89f, 0.77f, 0.58f);

            GameObject textGo = new GameObject("StampText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(stampGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.text = "[ 배송 승인 / APPROVED ]\nLOGISTICS EXPRESS\n✔ 규격 서류 접수완료";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.85f, 0.20f, 0.16f, 0);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("invoicePaper").objectReferenceValue = paperRt;
            so.FindProperty("stampMark").objectReferenceValue = stampRt;
            so.FindProperty("stampText").objectReferenceValue = text;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/StampInvoiceTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateZipCodeBoxes(Transform parent, Vector2 anchoredPos, Color redColor)
        {
            GameObject zipGroup = new GameObject("ZipCodeBoxes", typeof(RectTransform));
            zipGroup.transform.SetParent(parent, false);
            RectTransform grpRt = zipGroup.GetComponent<RectTransform>();
            grpRt.anchoredPosition = anchoredPos;
            grpRt.sizeDelta = new Vector2(240, 42);

            for (int i = 0; i < 5; i++)
            {
                GameObject boxOuter = new GameObject($"ZipBox_{i}", typeof(RectTransform), typeof(Image));
                boxOuter.transform.SetParent(zipGroup.transform, false);
                RectTransform oRt = boxOuter.GetComponent<RectTransform>();
                oRt.anchoredPosition = new Vector2(-96 + (i * 48), 0);
                oRt.sizeDelta = new Vector2(40, 40);
                Image oImg = boxOuter.GetComponent<Image>();
                oImg.color = redColor;

                GameObject boxInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
                boxInner.transform.SetParent(boxOuter.transform, false);
                RectTransform iRt = boxInner.GetComponent<RectTransform>();
                iRt.anchorMin = Vector2.zero;
                iRt.anchorMax = Vector2.one;
                iRt.offsetMin = new Vector2(2, 2);
                iRt.offsetMax = new Vector2(-2, -2);
                Image iImg = boxInner.GetComponent<Image>();
                iImg.color = new Color(0.89f, 0.77f, 0.58f);
            }
        }

        // ==================== 신규 복귀 물류 트랜지션 5종 빌더 ====================

        private static GameObject CreateForkliftLiftPrefab()
        {
            GameObject root = CreateBaseTransitionRoot("ForkliftLiftTransition");
            var comp = root.AddComponent<ForkliftLiftTransition>();

            // Lift Container (수직 리프팅 전체 컨테이너)
            GameObject liftGo = new GameObject("LiftContainer", typeof(RectTransform));
            liftGo.transform.SetParent(root.transform, false);
            RectTransform liftRt = liftGo.GetComponent<RectTransform>();
            liftRt.anchorMin = Vector2.zero;
            liftRt.anchorMax = Vector2.one;
            liftRt.offsetMin = Vector2.zero;
            liftRt.offsetMax = Vector2.zero;

            // 0. 산업용 물류 창고 배경 패널 (화면 100% 암전 커버 보장)
            GameObject bayBg = new GameObject("WarehouseBayBackdrop", typeof(RectTransform), typeof(Image));
            bayBg.transform.SetParent(liftGo.transform, false);
            RectTransform bgRt = bayBg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bayBg.GetComponent<Image>();
            bgImg.color = new Color(0.14f, 0.17f, 0.22f); // 딥 인더스트리얼 슬레이트

            // 바닥 안전 구역 옐로우/블랙 해저드 라인
            GameObject hazard = new GameObject("FloorHazardLine", typeof(RectTransform), typeof(Image));
            hazard.transform.SetParent(bayBg.transform, false);
            RectTransform hzRt = hazard.GetComponent<RectTransform>();
            hzRt.anchorMin = new Vector2(0, 0);
            hzRt.anchorMax = new Vector2(1, 0);
            hzRt.pivot = new Vector2(0.5f, 0);
            hzRt.sizeDelta = new Vector2(0, 36);
            Image hzImg = hazard.GetComponent<Image>();
            hzImg.color = new Color(0.96f, 0.74f, 0.18f);

            // 1. Forklift Body (중앙에 시원하고 웅장하게 서 있는 정면 지게차)
            GameObject forkGo = new GameObject("ForkliftBody", typeof(RectTransform));
            forkGo.transform.SetParent(liftGo.transform, false);
            RectTransform forkRt = forkGo.GetComponent<RectTransform>();
            forkRt.anchorMin = new Vector2(0.5f, 0.5f);
            forkRt.anchorMax = new Vector2(0.5f, 0.5f);
            forkRt.anchoredPosition = new Vector2(0, -60);
            forkRt.sizeDelta = new Vector2(980, 780);

            // 1-1. 2단 수직 승강 마스트 강철 레일 기둥 2개 (Mast)
            for (int m = 0; m < 2; m++)
            {
                GameObject mast = new GameObject(m == 0 ? "Mast_L" : "Mast_R", typeof(RectTransform), typeof(Image));
                mast.transform.SetParent(forkGo.transform, false);
                RectTransform mRt = mast.GetComponent<RectTransform>();
                mRt.anchoredPosition = new Vector2(m == 0 ? -220 : 220, 80);
                mRt.sizeDelta = new Vector2(56, 720);
                Image mImg = mast.GetComponent<Image>();
                mImg.color = new Color(0.24f, 0.27f, 0.33f); // 강철 다크 그레이

                // 마스트 내부 슬라이드 레일
                GameObject rail = new GameObject("InnerRail", typeof(RectTransform), typeof(Image));
                rail.transform.SetParent(mast.transform, false);
                RectTransform rRt = rail.GetComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0.25f, 0);
                rRt.anchorMax = new Vector2(0.75f, 1);
                rRt.offsetMin = Vector2.zero;
                rRt.offsetMax = Vector2.zero;
                Image rImg = rail.GetComponent<Image>();
                rImg.color = new Color(0.15f, 0.17f, 0.21f);
            }

            // 마스트 중앙 유압 피스톤 실린더
            GameObject piston = new GameObject("HydraulicPiston", typeof(RectTransform), typeof(Image));
            piston.transform.SetParent(forkGo.transform, false);
            RectTransform pRt = piston.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0, 60);
            pRt.sizeDelta = new Vector2(34, 620);
            Image pImg = piston.GetComponent<Image>();
            pImg.color = new Color(0.76f, 0.80f, 0.88f); // 은빛 크롬

            // 1-2. 은빛 L자형 강철 포크 블레이드 2개
            for (int f = 0; f < 2; f++)
            {
                GameObject forkItem = new GameObject(f == 0 ? "ForkBlade_L" : "ForkBlade_R", typeof(RectTransform), typeof(Image));
                forkItem.transform.SetParent(forkGo.transform, false);
                RectTransform fbRt = forkItem.GetComponent<RectTransform>();
                fbRt.anchoredPosition = new Vector2(f == 0 ? -220 : 220, 100);
                fbRt.sizeDelta = new Vector2(44, 260);
                Image fbImg = forkItem.GetComponent<Image>();
                fbImg.color = new Color(0.85f, 0.88f, 0.94f); // 밝은 은빛 강철 포크
            }

            // 1-3. 지게차 캐빈 차체 (Cabin Body)
            GameObject cabin = new GameObject("CabinBody", typeof(RectTransform), typeof(Image));
            cabin.transform.SetParent(forkGo.transform, false);
            RectTransform cabinRt = cabin.GetComponent<RectTransform>();
            cabinRt.anchoredPosition = new Vector2(0, -130);
            cabinRt.sizeDelta = new Vector2(620, 270);
            Image cabinImg = cabin.GetComponent<Image>();
            cabinImg.color = new Color(0.96f, 0.72f, 0.15f); // 인더스트리얼 옐로우

            // 하단 카운터웨이트 및 섀시
            GameObject counterweight = new GameObject("Counterweight", typeof(RectTransform), typeof(Image));
            counterweight.transform.SetParent(cabin.transform, false);
            RectTransform cwRt = counterweight.GetComponent<RectTransform>();
            cwRt.anchorMin = new Vector2(0, 0);
            cwRt.anchorMax = new Vector2(1, 0.42f);
            cwRt.offsetMin = Vector2.zero;
            cwRt.offsetMax = Vector2.zero;
            Image cwImg = counterweight.GetComponent<Image>();
            cwImg.color = new Color(0.18f, 0.20f, 0.25f);

            // 1-4. 운전석 롤 케이지 (Overhead Guard)
            GameObject cage = new GameObject("RollCage", typeof(RectTransform));
            cage.transform.SetParent(cabin.transform, false);
            RectTransform cageRt = cage.GetComponent<RectTransform>();
            cageRt.anchoredPosition = new Vector2(0, 200);
            cageRt.sizeDelta = new Vector2(440, 210);

            for (int p = 0; p < 3; p++)
            {
                GameObject pillar = new GameObject($"CagePillar_{p}", typeof(RectTransform), typeof(Image));
                pillar.transform.SetParent(cage.transform, false);
                RectTransform pilRt = pillar.GetComponent<RectTransform>();
                float px = (p == 0) ? -190 : (p == 1 ? 0 : 190);
                pilRt.anchoredPosition = new Vector2(px, 0);
                pilRt.sizeDelta = new Vector2(22, 210);
                Image pilImg = pillar.GetComponent<Image>();
                pilImg.color = new Color(0.2f, 0.22f, 0.27f);
            }

            // 케이지 상단 루프 가드
            GameObject roof = new GameObject("RoofGuard", typeof(RectTransform), typeof(Image));
            roof.transform.SetParent(cage.transform, false);
            RectTransform rfRt = roof.GetComponent<RectTransform>();
            rfRt.anchorMin = new Vector2(0, 1);
            rfRt.anchorMax = new Vector2(1, 1);
            rfRt.pivot = new Vector2(0.5f, 1);
            rfRt.sizeDelta = new Vector2(24, 26);
            Image rfImg = roof.GetComponent<Image>();
            rfImg.color = new Color(0.2f, 0.22f, 0.27f);

            // 1-5. 상단 주황 회전 경광등 (Warning Beacon)
            GameObject beaconGo = new GameObject("WarningBeacon", typeof(RectTransform), typeof(Image));
            beaconGo.transform.SetParent(roof.transform, false);
            RectTransform beaconRt = beaconGo.GetComponent<RectTransform>();
            beaconRt.anchoredPosition = new Vector2(0, 36);
            beaconRt.sizeDelta = new Vector2(56, 56);
            Image beaconImg = beaconGo.GetComponent<Image>();
            beaconImg.color = new Color(1f, 0.45f, 0.08f);

            // 1-6. 대형 바퀴 2개
            for (int w = 0; w < 2; w++)
            {
                GameObject wheel = new GameObject(w == 0 ? "Wheel_L" : "Wheel_R", typeof(RectTransform), typeof(Image));
                wheel.transform.SetParent(forkGo.transform, false);
                RectTransform wRt = wheel.GetComponent<RectTransform>();
                wRt.anchoredPosition = new Vector2(w == 0 ? -330 : 330, -220);
                wRt.sizeDelta = new Vector2(130, 130);
                Image wImg = wheel.GetComponent<Image>();
                wImg.color = new Color(0.10f, 0.11f, 0.14f);

                GameObject rim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
                rim.transform.SetParent(wheel.transform, false);
                RectTransform rimRt = rim.GetComponent<RectTransform>();
                rimRt.sizeDelta = new Vector2(65, 65);
                Image rimImg = rim.GetComponent<Image>();
                rimImg.color = new Color(0.65f, 0.70f, 0.76f);
            }

            // 2. Cargo Pallet (지게차 포크 위에 얹혀 번쩍 들려 올라가는 원목 파렛트 & 적재 카톤 박스)
            GameObject cargoGo = new GameObject("CargoPallet", typeof(RectTransform));
            cargoGo.transform.SetParent(liftGo.transform, false);
            RectTransform cargoRt = cargoGo.GetComponent<RectTransform>();
            cargoRt.anchorMin = new Vector2(0.5f, 0.5f);
            cargoRt.anchorMax = new Vector2(0.5f, 0.5f);
            cargoRt.anchoredPosition = new Vector2(0, 60);
            cargoRt.sizeDelta = new Vector2(1300, 620);

            // 하단 대형 원목 팔레트 (Wooden Pallet)
            GameObject palletGo = new GameObject("WoodenPallet", typeof(RectTransform), typeof(Image));
            palletGo.transform.SetParent(cargoGo.transform, false);
            RectTransform palletRt = palletGo.GetComponent<RectTransform>();
            palletRt.anchorMin = new Vector2(0.5f, 0);
            palletRt.anchorMax = new Vector2(0.5f, 0);
            palletRt.pivot = new Vector2(0.5f, 0);
            palletRt.sizeDelta = new Vector2(1260, 95);
            palletRt.anchoredPosition = Vector2.zero;
            Image palletImg = palletGo.GetComponent<Image>();
            palletImg.color = new Color(0.62f, 0.45f, 0.28f);

            // 포크 진입 홀 2개
            for (int f = 0; f < 2; f++)
            {
                GameObject fHole = new GameObject(f == 0 ? "ForkHole_L" : "ForkHole_R", typeof(RectTransform), typeof(Image));
                fHole.transform.SetParent(palletGo.transform, false);
                RectTransform fhRt = fHole.GetComponent<RectTransform>();
                fhRt.anchoredPosition = new Vector2(f == 0 ? -220 : 220, 42);
                fhRt.sizeDelta = new Vector2(130, 48);
                Image fhImg = fHole.GetComponent<Image>();
                fhImg.color = new Color(0.16f, 0.12f, 0.08f);
            }

            // 적재된 3단 카톤 박스 블록들
            GameObject boxStack = new GameObject("BoxStack", typeof(RectTransform), typeof(Image));
            boxStack.transform.SetParent(cargoGo.transform, false);
            RectTransform bsRt = boxStack.GetComponent<RectTransform>();
            bsRt.anchorMin = new Vector2(0.5f, 0);
            bsRt.anchorMax = new Vector2(0.5f, 0);
            bsRt.pivot = new Vector2(0.5f, 0);
            bsRt.anchoredPosition = new Vector2(0, 95);
            bsRt.sizeDelta = new Vector2(1200, 480);
            Image bsImg = boxStack.GetComponent<Image>();
            bsImg.color = new Color(0.78f, 0.60f, 0.40f); // 크라프트 카톤 베이스

            // 박스 테이프 라인들
            for (int r = 1; r <= 2; r++)
            {
                GameObject tape = new GameObject($"Tape_{r}", typeof(RectTransform), typeof(Image));
                tape.transform.SetParent(boxStack.transform, false);
                RectTransform tRt = tape.GetComponent<RectTransform>();
                tRt.anchoredPosition = new Vector2(0, r * 150);
                tRt.sizeDelta = new Vector2(1200, 20);
                Image tImg = tape.GetComponent<Image>();
                tImg.color = new Color(0.92f, 0.68f, 0.24f, 0.8f);
            }

            // 박스 물류 라벨들
            Vector2[] labelPositions = new Vector2[]
            {
                new Vector2(-360, 360), new Vector2(360, 360),
                new Vector2(-360, 180), new Vector2(360, 180),
                new Vector2(0, 270)
            };
            string[] labelTexts = new string[]
            {
                "📦 CARGO #A-102\nFRAGILE ⚠️",
                "★ EXPRESS DELIVERY ★\nHEAVY LOAD 40KG",
                "📦 CARGO #B-882\nHANDLE WITH CARE",
                "LOGISTICS EXPRESS\nTERMINAL CHECKED ✔",
                "▲ HEAVY FORKLIFT LIFTING ▲\nHYDRAULIC LOAD 2.5T"
            };

            for (int i = 0; i < labelPositions.Length; i++)
            {
                GameObject labelGo = new GameObject($"BoxLabel_{i}", typeof(RectTransform), typeof(Image));
                labelGo.transform.SetParent(boxStack.transform, false);
                RectTransform lRt = labelGo.GetComponent<RectTransform>();
                lRt.anchoredPosition = labelPositions[i];
                lRt.sizeDelta = new Vector2(300, 80);
                Image lImg = labelGo.GetComponent<Image>();
                lImg.color = new Color(0.96f, 0.94f, 0.88f, 0.95f);

                GameObject ltxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                ltxtGo.transform.SetParent(labelGo.transform, false);
                RectTransform ltRt = ltxtGo.GetComponent<RectTransform>();
                ltRt.anchorMin = Vector2.zero;
                ltRt.anchorMax = Vector2.one;
                ltRt.offsetMin = Vector2.zero;
                ltRt.offsetMax = Vector2.zero;
                Text lt = ltxtGo.GetComponent<Text>();
                lt.text = labelTexts[i];
                lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lt.fontSize = 17;
                lt.fontStyle = FontStyle.Bold;
                lt.alignment = TextAnchor.MiddleCenter;
                lt.color = new Color(0.2f, 0.15f, 0.1f);
            }

            // 3. Status Text
            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(cargoGo.transform, false);
            RectTransform srt2 = statusGo.GetComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0.5f, 0.5f);
            srt2.anchorMax = new Vector2(0.5f, 0.5f);
            srt2.anchoredPosition = new Vector2(0, -220);
            srt2.sizeDelta = new Vector2(800, 60);
            Text stxt = statusGo.GetComponent<Text>();
            stxt.text = "🚜 FORKLIFT HYDRAULIC LIFT COMPLETE";
            stxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stxt.fontSize = 24;
            stxt.fontStyle = FontStyle.Bold;
            stxt.alignment = TextAnchor.MiddleCenter;
            stxt.color = new Color(0.95f, 0.75f, 0.2f, 0);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("liftContainer").objectReferenceValue = liftRt;
            so.FindProperty("cargoPallet").objectReferenceValue = cargoRt;
            so.FindProperty("forkliftBody").objectReferenceValue = forkRt;
            so.FindProperty("warningBeacon").objectReferenceValue = beaconImg;
            so.FindProperty("statusText").objectReferenceValue = stxt;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/ForkliftLiftTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateForkliftDrivePrefab()
        {
            GameObject root = CreateBaseTransitionRoot("ForkliftDriveTransition");
            var comp = root.AddComponent<ForkliftDriveTransition>();

            // 1. Curtain Panel (암전 슬레이트 배경 커튼: 좌->우 스케일 확장)
            GameObject curtainGo = new GameObject("CurtainPanel", typeof(RectTransform), typeof(Image));
            curtainGo.transform.SetParent(root.transform, false);
            RectTransform curtainRt = curtainGo.GetComponent<RectTransform>();
            curtainRt.anchorMin = Vector2.zero;
            curtainRt.anchorMax = Vector2.one;
            curtainRt.offsetMin = Vector2.zero;
            curtainRt.offsetMax = Vector2.zero;
            curtainRt.pivot = new Vector2(0, 0.5f);
            Image curtainImg = curtainGo.GetComponent<Image>();
            curtainImg.color = new Color(0.14f, 0.17f, 0.23f); // 인더스트리얼 슬레이트

            // 바닥 주행 가이드 해저드 라인 (바닥면 접점 Y: -200)
            GameObject floorHazard = new GameObject("FloorHazardLine", typeof(RectTransform), typeof(Image));
            floorHazard.transform.SetParent(curtainGo.transform, false);
            RectTransform fhzRt = floorHazard.GetComponent<RectTransform>();
            fhzRt.anchorMin = new Vector2(0, 0.5f);
            fhzRt.anchorMax = new Vector2(1, 0.5f);
            fhzRt.pivot = new Vector2(0.5f, 0.5f);
            fhzRt.anchoredPosition = new Vector2(0, -200);
            fhzRt.sizeDelta = new Vector2(0, 24);
            Image fhzImg = floorHazard.GetComponent<Image>();
            fhzImg.color = new Color(0.96f, 0.72f, 0.15f, 0.8f);

            // =========================================================================
            // [Layer 1 - 뒤]: Pallet_Back (파렛트 뒷면 판자 / 어두운 내부 그림자)
            // =========================================================================
            GameObject palletBackGo = new GameObject("Pallet_Back", typeof(RectTransform));
            palletBackGo.transform.SetParent(root.transform, false);
            RectTransform palletBackRt = palletBackGo.GetComponent<RectTransform>();
            palletBackRt.anchorMin = new Vector2(0.5f, 0.5f);
            palletBackRt.anchorMax = new Vector2(0.5f, 0.5f);
            palletBackRt.pivot = new Vector2(0.5f, 0.5f);
            palletBackRt.anchoredPosition = new Vector2(0, -120);
            palletBackRt.sizeDelta = new Vector2(420, 320);

            // 파렛트 뒷면 목재 베이스 (어두운 내부 그림자)
            GameObject woodBack = new GameObject("WoodPalletBack", typeof(RectTransform), typeof(Image));
            woodBack.transform.SetParent(palletBackGo.transform, false);
            RectTransform wbRt = woodBack.GetComponent<RectTransform>();
            wbRt.anchorMin = new Vector2(0.5f, 0.5f);
            wbRt.anchorMax = new Vector2(0.5f, 0.5f);
            wbRt.pivot = new Vector2(0.5f, 0.5f);
            wbRt.anchoredPosition = Vector2.zero;
            wbRt.sizeDelta = new Vector2(420, 56);
            Image wbImg = woodBack.GetComponent<Image>();
            wbImg.color = new Color(0.18f, 0.13f, 0.08f); // 파렛트 안쪽 깊은 그림자

            // 포크 포켓 내부 그림자 좌/우 (포크가 들어왔을 때 뒤에 깔리는 깊이감)
            for (int f = 0; f < 2; f++)
            {
                GameObject fPocket = new GameObject(f == 0 ? "ForkPocketShadow_L" : "ForkPocketShadow_R", typeof(RectTransform), typeof(Image));
                fPocket.transform.SetParent(woodBack.transform, false);
                RectTransform fpRt = fPocket.GetComponent<RectTransform>();
                fpRt.anchorMin = new Vector2(0.5f, 0.5f);
                fpRt.anchorMax = new Vector2(0.5f, 0.5f);
                fpRt.pivot = new Vector2(0.5f, 0.5f);
                fpRt.anchoredPosition = new Vector2(f == 0 ? -90 : 90, 0);
                fpRt.sizeDelta = new Vector2(130, 26);
                Image fpImg = fPocket.GetComponent<Image>();
                fpImg.color = new Color(0.11f, 0.08f, 0.05f);
            }

            // =========================================================================
            // [Layer 2 - 중간]: ForkliftRoot (좌측에서 진입하는 은빛 강철 포크 날 Y: -120)
            // =========================================================================
            GameObject rootForkGo = new GameObject("ForkliftRoot", typeof(RectTransform));
            rootForkGo.transform.SetParent(root.transform, false);
            RectTransform rootForkRt = rootForkGo.GetComponent<RectTransform>();
            rootForkRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootForkRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootForkRt.pivot = new Vector2(0.5f, 0.5f);
            rootForkRt.anchoredPosition = new Vector2(-1550, 0);
            rootForkRt.sizeDelta = new Vector2(1100, 600);

            // 3-1. Forklift Visual (측면 프로필 지게차 비주얼 본체)
            GameObject visualGo = new GameObject("ForkliftVisual", typeof(RectTransform));
            visualGo.transform.SetParent(rootForkGo.transform, false);
            RectTransform visualRt = visualGo.GetComponent<RectTransform>();
            visualRt.anchorMin = new Vector2(0.5f, 0.5f);
            visualRt.anchorMax = new Vector2(0.5f, 0.5f);
            visualRt.pivot = new Vector2(0.5f, 0.5f);
            visualRt.anchoredPosition = Vector2.zero;
            visualRt.sizeDelta = new Vector2(900, 500);

            // 옐로우 섀시 바디
            GameObject chassis = new GameObject("ChassisBody", typeof(RectTransform), typeof(Image));
            chassis.transform.SetParent(visualGo.transform, false);
            RectTransform chRt = chassis.GetComponent<RectTransform>();
            chRt.anchoredPosition = new Vector2(-70, -40);
            chRt.sizeDelta = new Vector2(420, 180);
            Image chImg = chassis.GetComponent<Image>();
            chImg.color = new Color(0.96f, 0.72f, 0.15f);

            // 후방 블랙 카운터웨이트
            GameObject rearWeight = new GameObject("Counterweight", typeof(RectTransform), typeof(Image));
            rearWeight.transform.SetParent(visualGo.transform, false);
            RectTransform rwRt = rearWeight.GetComponent<RectTransform>();
            rwRt.anchoredPosition = new Vector2(-280, -30);
            rwRt.sizeDelta = new Vector2(160, 170);
            Image rwImg = rearWeight.GetComponent<Image>();
            rwImg.color = new Color(0.18f, 0.20f, 0.26f);

            // 카운터웨이트 옐로우 스트라이프
            GameObject cwStripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            cwStripe.transform.SetParent(rearWeight.transform, false);
            RectTransform cwsRt = cwStripe.GetComponent<RectTransform>();
            cwsRt.anchoredPosition = new Vector2(-10, 0);
            cwsRt.sizeDelta = new Vector2(28, 170);
            Image cwsImg = cwStripe.GetComponent<Image>();
            cwsImg.color = new Color(0.96f, 0.72f, 0.15f);

            // 운전석 롤 케이지 (블랙 강철 튜브)
            GameObject sideCage = new GameObject("SideRollCage", typeof(RectTransform), typeof(Image));
            sideCage.transform.SetParent(visualGo.transform, false);
            RectTransform scRt = sideCage.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(-120, 115);
            scRt.sizeDelta = new Vector2(270, 200);
            Image scImg = sideCage.GetComponent<Image>();
            scImg.color = new Color(0.20f, 0.22f, 0.28f);

            GameObject cageHole = new GameObject("CageHole", typeof(RectTransform), typeof(Image));
            cageHole.transform.SetParent(sideCage.transform, false);
            RectTransform choleRt = cageHole.GetComponent<RectTransform>();
            choleRt.anchorMin = Vector2.zero;
            choleRt.anchorMax = Vector2.one;
            choleRt.offsetMin = new Vector2(18, 18);
            choleRt.offsetMax = new Vector2(-18, -18);
            Image choleImg = cageHole.GetComponent<Image>();
            choleImg.color = new Color(0.14f, 0.17f, 0.23f);

            // 운전석 시트 & 핸들
            GameObject seat = new GameObject("Seat", typeof(RectTransform), typeof(Image));
            seat.transform.SetParent(sideCage.transform, false);
            RectTransform seatRt = seat.GetComponent<RectTransform>();
            seatRt.anchoredPosition = new Vector2(-55, -20);
            seatRt.sizeDelta = new Vector2(50, 75);
            Image seatImg = seat.GetComponent<Image>();
            seatImg.color = new Color(0.12f, 0.14f, 0.18f);

            // 상단 주황 경광등 (Warning Beacon)
            GameObject beaconGo = new GameObject("WarningBeacon", typeof(RectTransform), typeof(Image));
            beaconGo.transform.SetParent(sideCage.transform, false);
            RectTransform beaconRt = beaconGo.GetComponent<RectTransform>();
            beaconRt.anchoredPosition = new Vector2(0, 120);
            beaconRt.sizeDelta = new Vector2(46, 46);
            Image beaconImg = beaconGo.GetComponent<Image>();
            beaconImg.color = new Color(1f, 0.45f, 0.08f);

            // 전방 수직 마스트 (Vertical Mast)
            GameObject mast = new GameObject("FrontMast", typeof(RectTransform), typeof(Image));
            mast.transform.SetParent(visualGo.transform, false);
            RectTransform mRt = mast.GetComponent<RectTransform>();
            mRt.anchoredPosition = new Vector2(115, 60);
            mRt.sizeDelta = new Vector2(44, 400);
            Image mImg = mast.GetComponent<Image>();
            mImg.color = new Color(0.22f, 0.25f, 0.30f);

            // 전방으로 뻗은 은빛 강철 포크 날 (파렛트 포크 홀 중심 Y: -120과 정확히 1:1 일치!)
            GameObject forks = new GameObject("SteelForks", typeof(RectTransform), typeof(Image));
            forks.transform.SetParent(visualGo.transform, false);
            RectTransform fksRt = forks.GetComponent<RectTransform>();
            fksRt.anchoredPosition = new Vector2(250, -120);
            fksRt.sizeDelta = new Vector2(280, 22);
            Image fksImg = forks.GetComponent<Image>();
            fksImg.color = new Color(0.85f, 0.88f, 0.94f); // 은빛 강철

            // 3-2. 전륜 대형 바퀴 & 후륜 조향 바퀴
            GameObject frontWheelGo = new GameObject("FrontWheel", typeof(RectTransform), typeof(Image));
            frontWheelGo.transform.SetParent(visualGo.transform, false);
            RectTransform frontWheelRt = frontWheelGo.GetComponent<RectTransform>();
            frontWheelRt.anchoredPosition = new Vector2(70, -135);
            frontWheelRt.sizeDelta = new Vector2(130, 130);
            Image fwImg = frontWheelGo.GetComponent<Image>();
            fwImg.color = new Color(0.10f, 0.11f, 0.14f);

            GameObject fwRim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
            fwRim.transform.SetParent(frontWheelGo.transform, false);
            RectTransform fwrRt = fwRim.GetComponent<RectTransform>();
            fwrRt.sizeDelta = new Vector2(65, 65);
            Image fwrImg = fwRim.GetComponent<Image>();
            fwrImg.color = new Color(0.68f, 0.72f, 0.78f);

            GameObject rearWheelGo = new GameObject("RearWheel", typeof(RectTransform), typeof(Image));
            rearWheelGo.transform.SetParent(visualGo.transform, false);
            RectTransform rearWheelRt = rearWheelGo.GetComponent<RectTransform>();
            rearWheelRt.anchoredPosition = new Vector2(-230, -150);
            rearWheelRt.sizeDelta = new Vector2(100, 100);
            Image rwWheelImg = rearWheelGo.GetComponent<Image>();
            rwWheelImg.color = new Color(0.10f, 0.11f, 0.14f);

            GameObject rwRim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
            rwRim.transform.SetParent(rearWheelGo.transform, false);
            RectTransform rwrRt = rwRim.GetComponent<RectTransform>();
            rwrRt.sizeDelta = new Vector2(50, 50);
            Image rwrImg = rwRim.GetComponent<Image>();
            rwrImg.color = new Color(0.68f, 0.72f, 0.78f);

            // =========================================================================
            // [Layer 3 - 앞]: Pallet_Front (전면 상/하 가로판자, 좌/우/중앙 세로 목재 기둥들 & 적재 박스)
            // 포크가 기둥 뒤로 들어가 가려지고 비어있는 포크 홀 개구부 사이로만 은빛 포크가 관통되어 보임!
            // =========================================================================
            GameObject palletFrontGo = new GameObject("Pallet_Front", typeof(RectTransform));
            palletFrontGo.transform.SetParent(root.transform, false);
            RectTransform palletFrontRt = palletFrontGo.GetComponent<RectTransform>();
            palletFrontRt.anchorMin = new Vector2(0.5f, 0.5f);
            palletFrontRt.anchorMax = new Vector2(0.5f, 0.5f);
            palletFrontRt.pivot = new Vector2(0.5f, 0.5f);
            palletFrontRt.anchoredPosition = new Vector2(0, -120);
            palletFrontRt.sizeDelta = new Vector2(420, 320);

            // 전면 목재 프레임 (중심 Y: 0 -> 루트 기준 Y: -120)
            GameObject frontFrame = new GameObject("PalletFrontFrame", typeof(RectTransform));
            frontFrame.transform.SetParent(palletFrontGo.transform, false);
            RectTransform ffRt = frontFrame.GetComponent<RectTransform>();
            ffRt.anchorMin = new Vector2(0.5f, 0.5f);
            ffRt.anchorMax = new Vector2(0.5f, 0.5f);
            ffRt.pivot = new Vector2(0.5f, 0.5f);
            ffRt.anchoredPosition = Vector2.zero;
            ffRt.sizeDelta = new Vector2(420, 56);

            // 상단 가로 목재 판자 (Top Deck Board, Y: +21)
            GameObject topDeck = new GameObject("TopDeckBoard", typeof(RectTransform), typeof(Image));
            topDeck.transform.SetParent(frontFrame.transform, false);
            RectTransform tdRt = topDeck.GetComponent<RectTransform>();
            tdRt.anchoredPosition = new Vector2(0, 21);
            tdRt.sizeDelta = new Vector2(420, 14);
            Image tdImg = topDeck.GetComponent<Image>();
            tdImg.color = new Color(0.65f, 0.47f, 0.29f); // 따뜻한 원목 브라운

            // 하단 가로 목재 판자 (Bottom Deck Board, Y: -21)
            GameObject botDeck = new GameObject("BottomDeckBoard", typeof(RectTransform), typeof(Image));
            botDeck.transform.SetParent(frontFrame.transform, false);
            RectTransform bdRt = botDeck.GetComponent<RectTransform>();
            bdRt.anchoredPosition = new Vector2(0, -21);
            bdRt.sizeDelta = new Vector2(420, 14);
            Image bdImg = botDeck.GetComponent<Image>();
            bdImg.color = new Color(0.55f, 0.38f, 0.23f); // 하단 받침 목재

            // 세로 목재 기둥 블록 3개 (좌측 / 중앙 / 우측)
            // ★ 좌측 기둥과 중앙 기둥 사이, 중앙 기둥과 우측 기둥 사이가 투명한 '포크 관통 홀' 개구부가 됨!
            float[] stringerPosX = new float[] { -185f, 0f, 185f };
            string[] stringerNames = new string[] { "LeftStringer", "CenterStringer", "RightStringer" };
            for (int s = 0; s < 3; s++)
            {
                GameObject stringer = new GameObject(stringerNames[s], typeof(RectTransform), typeof(Image));
                stringer.transform.SetParent(frontFrame.transform, false);
                RectTransform stRt = stringer.GetComponent<RectTransform>();
                stRt.anchoredPosition = new Vector2(stringerPosX[s], 0);
                stRt.sizeDelta = new Vector2(50, 28);
                Image stImg = stringer.GetComponent<Image>();
                stImg.color = new Color(0.62f, 0.44f, 0.27f);
            }

            // 적재된 2단 박스 (파렛트 프레임 상단 위 Y: 28부터 적재)
            GameObject cargoBox = new GameObject("CartonBox", typeof(RectTransform), typeof(Image));
            cargoBox.transform.SetParent(palletFrontGo.transform, false);
            RectTransform cbRt = cargoBox.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 0.5f);
            cbRt.anchorMax = new Vector2(0.5f, 0.5f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.anchoredPosition = new Vector2(0, 28);
            cbRt.sizeDelta = new Vector2(390, 250);
            Image cbImg = cargoBox.GetComponent<Image>();
            cbImg.color = new Color(0.78f, 0.60f, 0.40f);

            GameObject boxLabel = new GameObject("BoxLabel", typeof(RectTransform), typeof(Image));
            boxLabel.transform.SetParent(cargoBox.transform, false);
            RectTransform blRt = boxLabel.GetComponent<RectTransform>();
            blRt.anchoredPosition = new Vector2(0, 45);
            blRt.sizeDelta = new Vector2(260, 80);
            Image blImg = boxLabel.GetComponent<Image>();
            blImg.color = new Color(0.96f, 0.94f, 0.88f);

            GameObject blTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            blTxtGo.transform.SetParent(boxLabel.transform, false);
            RectTransform bltRt = blTxtGo.GetComponent<RectTransform>();
            bltRt.anchorMin = Vector2.zero;
            bltRt.anchorMax = Vector2.one;
            bltRt.offsetMin = Vector2.zero;
            bltRt.offsetMax = Vector2.zero;
            Text blTxt = blTxtGo.GetComponent<Text>();
            blTxt.text = "EXPRESS CARGO\n[ PRIORITY HEAVY ]";
            blTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            blTxt.fontSize = 17;
            blTxt.fontStyle = FontStyle.Bold;
            blTxt.alignment = TextAnchor.MiddleCenter;
            blTxt.color = new Color(0.2f, 0.15f, 0.1f);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("curtainPanel").objectReferenceValue = curtainRt;
            so.FindProperty("forkliftRoot").objectReferenceValue = rootForkRt;
            so.FindProperty("forkliftVisual").objectReferenceValue = visualRt;
            so.FindProperty("steelForks").objectReferenceValue = fksRt;
            so.FindProperty("frontWheel").objectReferenceValue = frontWheelRt;
            so.FindProperty("rearWheel").objectReferenceValue = rearWheelRt;
            so.FindProperty("palletBack").objectReferenceValue = palletBackRt;
            so.FindProperty("palletFront").objectReferenceValue = palletFrontRt;
            so.FindProperty("cargoPallet").objectReferenceValue = palletFrontRt;
            so.FindProperty("warningBeacon").objectReferenceValue = beaconImg;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/ForkliftDriveTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBarcodeScanPrefab()
        {
            GameObject root = CreateBaseTransitionRoot("BarcodeScanTransition");
            var comp = root.AddComponent<BarcodeScanTransition>();

            // Left Curtain
            GameObject leftGo = new GameObject("LeftCurtain", typeof(RectTransform), typeof(Image));
            leftGo.transform.SetParent(root.transform, false);
            RectTransform leftRt = leftGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.5f, 1);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;
            Image leftImg = leftGo.GetComponent<Image>();
            leftImg.color = new Color(0.12f, 0.12f, 0.14f);

            // Right Curtain
            GameObject rightGo = new GameObject("RightCurtain", typeof(RectTransform), typeof(Image));
            rightGo.transform.SetParent(root.transform, false);
            RectTransform rightRt = rightGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;
            Image rightImg = rightGo.GetComponent<Image>();
            rightImg.color = new Color(0.12f, 0.12f, 0.14f);

            // Add Barcode Lines across left and right
            for (int i = 0; i < 18; i++)
            {
                Transform parent = (i < 9) ? leftGo.transform : rightGo.transform;
                GameObject bLine = new GameObject($"BarLine_{i}", typeof(RectTransform), typeof(Image));
                bLine.transform.SetParent(parent, false);
                RectTransform brt = bLine.GetComponent<RectTransform>();
                float xNorm = (i % 9) / 9f + 0.05f;
                brt.anchorMin = new Vector2(xNorm, 0.25f);
                brt.anchorMax = new Vector2(xNorm, 0.75f);
                brt.sizeDelta = new Vector2((i % 3 == 0) ? 14 : 6, 0);
                Image bimg = bLine.GetComponent<Image>();
                bimg.color = Color.white;
            }

            // Laser Beam Plane (두껍고 부드러운 반투명 54px 레이저 광선 면)
            GameObject laserGo = new GameObject("ScanLaserLine", typeof(RectTransform), typeof(Image));
            laserGo.transform.SetParent(root.transform, false);
            RectTransform laserRt = laserGo.GetComponent<RectTransform>();
            laserRt.anchorMin = new Vector2(0, 0.5f);
            laserRt.anchorMax = new Vector2(1, 0.5f);
            laserRt.sizeDelta = new Vector2(0, 54);
            Image laserImg = laserGo.GetComponent<Image>();
            laserImg.color = new Color(1f, 0.2f, 0.25f, 0.45f); // 반투명 붉은 광선 면

            // 레이저 중심부 고광도 네온 코어 라인 (10px 백색/레드 코어)
            GameObject coreGo = new GameObject("LaserCore", typeof(RectTransform), typeof(Image));
            coreGo.transform.SetParent(laserGo.transform, false);
            RectTransform coreRt = coreGo.GetComponent<RectTransform>();
            coreRt.anchorMin = new Vector2(0, 0.5f);
            coreRt.anchorMax = new Vector2(1, 0.5f);
            coreRt.sizeDelta = new Vector2(0, 10);
            Image coreImg = coreGo.GetComponent<Image>();
            coreImg.color = new Color(1f, 0.92f, 0.94f, 0.95f);

            // Status Badge
            GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(root.transform, false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(420, 90);
            Image badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = new Color(0.15f, 0.75f, 0.35f, 0);

            // Status Text
            GameObject txtGo = new GameObject("ScanText", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(badgeGo.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            Text txt = txtGo.GetComponent<Text>();
            txt.text = "✔ BARCODE VERIFIED [OK]";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("leftCurtain").objectReferenceValue = leftRt;
            so.FindProperty("rightCurtain").objectReferenceValue = rightRt;
            so.FindProperty("scanLaserLine").objectReferenceValue = laserRt;
            so.FindProperty("scanLaserImage").objectReferenceValue = laserImg;
            so.FindProperty("barcodeStatusBadge").objectReferenceValue = badgeImg;
            so.FindProperty("scanStatusText").objectReferenceValue = txt;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/BarcodeScanTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject CreateReceiptRollPrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("ReceiptRollTransition");
            var comp = root.AddComponent<ReceiptRollTransition>();

            // 화면 전체를 덮는 다크 백드롭 (트랜지션 차폐 보장)
            GameObject backdropGo = new GameObject("DeskBackdrop", typeof(RectTransform), typeof(Image));
            backdropGo.transform.SetParent(root.transform, false);
            RectTransform bdRt = backdropGo.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            Image bdImg = backdropGo.GetComponent<Image>();
            bdImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            // Receipt Paper (크림 화이트 영수증 종이, 너비 920)
            GameObject paperGo = new GameObject("ReceiptPaper", typeof(RectTransform), typeof(Image));
            paperGo.transform.SetParent(root.transform, false);
            RectTransform paperRt = paperGo.GetComponent<RectTransform>();
            paperRt.anchorMin = new Vector2(0.5f, 0);
            paperRt.anchorMax = new Vector2(0.5f, 1);
            paperRt.pivot = new Vector2(0.5f, 1);
            paperRt.sizeDelta = new Vector2(920, 0);
            Image paperImg = paperGo.GetComponent<Image>();
            paperImg.color = new Color(0.97f, 0.96f, 0.92f); // 크림 화이트 영수증 종이

            // 영수증 종이 테두리 섀도우
            GameObject shadowL = new GameObject("Shadow_L", typeof(RectTransform), typeof(Image));
            shadowL.transform.SetParent(paperGo.transform, false);
            RectTransform slRt = shadowL.GetComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0, 0);
            slRt.anchorMax = new Vector2(0, 1);
            slRt.pivot = new Vector2(0, 0.5f);
            slRt.sizeDelta = new Vector2(12, 0);
            Image slImg = shadowL.GetComponent<Image>();
            slImg.color = new Color(0, 0, 0, 0.15f);

            GameObject shadowR = new GameObject("Shadow_R", typeof(RectTransform), typeof(Image));
            shadowR.transform.SetParent(paperGo.transform, false);
            RectTransform srRt = shadowR.GetComponent<RectTransform>();
            srRt.anchorMin = new Vector2(1, 0);
            srRt.anchorMax = new Vector2(1, 1);
            srRt.pivot = new Vector2(1, 0.5f);
            srRt.sizeDelta = new Vector2(12, 0);
            Image srImg = shadowR.GetComponent<Image>();
            srImg.color = new Color(0, 0, 0, 0.15f);

            // 1. 헤더 타이틀
            GameObject headerGo = new GameObject("HeaderTitle", typeof(RectTransform), typeof(Text));
            headerGo.transform.SetParent(paperGo.transform, false);
            RectTransform hRt = headerGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0.88f);
            hRt.anchorMax = new Vector2(0.5f, 0.88f);
            hRt.sizeDelta = new Vector2(820, 100);
            Text hTxt = headerGo.GetComponent<Text>();
            hTxt.text = "[ LOGISTICS TYCOON EXPRESS ]\nWAREHOUSE DISPATCH RECEIPT";
            hTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hTxt.fontSize = 28;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = new Color(0.15f, 0.15f, 0.18f);

            // 구분선 1
            GameObject div1 = new GameObject("Div_1", typeof(RectTransform), typeof(Image));
            div1.transform.SetParent(paperGo.transform, false);
            RectTransform d1Rt = div1.GetComponent<RectTransform>();
            d1Rt.anchorMin = new Vector2(0.06f, 0.81f);
            d1Rt.anchorMax = new Vector2(0.94f, 0.81f);
            d1Rt.sizeDelta = new Vector2(0, 3);
            Image d1Img = div1.GetComponent<Image>();
            d1Img.color = new Color(0.3f, 0.3f, 0.35f);

            // 2. 발송 정보 텍스트
            GameObject infoGo = new GameObject("DispatchInfo", typeof(RectTransform), typeof(Text));
            infoGo.transform.SetParent(paperGo.transform, false);
            RectTransform infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0.5f, 0.72f);
            infoRt.anchorMax = new Vector2(0.5f, 0.72f);
            infoRt.sizeDelta = new Vector2(780, 90);
            Text infoTxt = infoGo.GetComponent<Text>();
            infoTxt.text = "★ 물류 허브 화물 이송 완료 (TERMINAL A → HUB B)\nORDER NO: #LT-8849-EXPRESS   |   DATE: 2026-11-08\nVEHICLE: HEAVY FORKLIFT & FREIGHT CARGO";
            infoTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoTxt.fontSize = 20;
            infoTxt.alignment = TextAnchor.MiddleLeft;
            infoTxt.color = new Color(0.25f, 0.25f, 0.3f);

            // 3. 대형 흑백 바코드 줄무늬 (Barcode)
            GameObject bcGo = new GameObject("BarcodeGroup", typeof(RectTransform), typeof(Image));
            bcGo.transform.SetParent(paperGo.transform, false);
            RectTransform bcRt = bcGo.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.58f);
            bcRt.anchorMax = new Vector2(0.5f, 0.58f);
            bcRt.sizeDelta = new Vector2(780, 110);
            Image bcBg = bcGo.GetComponent<Image>();
            bcBg.color = Color.white;

            // 24개의 다양한 굵기 바코드 라인
            float[] barWidths = new float[] { 4, 12, 6, 8, 14, 4, 10, 6, 16, 4, 8, 12, 6, 14, 4, 8, 12, 6, 10, 4, 14, 6, 8, 4 };
            float curX = -350;
            for (int b = 0; b < barWidths.Length; b++)
            {
                GameObject bar = new GameObject($"Bar_{b}", typeof(RectTransform), typeof(Image));
                bar.transform.SetParent(bcGo.transform, false);
                RectTransform bRt = bar.GetComponent<RectTransform>();
                bRt.anchoredPosition = new Vector2(curX, 0);
                bRt.sizeDelta = new Vector2(barWidths[b], 85);
                Image bImg = bar.GetComponent<Image>();
                bImg.color = Color.black;
                curX += barWidths[b] + 16f;
            }

            // 바코드 하단 번호
            GameObject bcNumGo = new GameObject("BarcodeNum", typeof(RectTransform), typeof(Text));
            bcNumGo.transform.SetParent(bcGo.transform, false);
            RectTransform bcnRt = bcNumGo.GetComponent<RectTransform>();
            bcnRt.anchorMin = new Vector2(0, 0);
            bcnRt.anchorMax = new Vector2(1, 0.25f);
            bcnRt.offsetMin = Vector2.zero;
            bcnRt.offsetMax = Vector2.zero;
            Text bcnTxt = bcNumGo.GetComponent<Text>();
            bcnTxt.text = "* 8 8 0 9 2 4 1 0 5 8 9 2 *";
            bcnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bcnTxt.fontSize = 16;
            bcnTxt.alignment = TextAnchor.MiddleCenter;
            bcnTxt.color = Color.black;

            // 4. 품목 상세 및 합계 금액
            GameObject itemsGo = new GameObject("ItemsDetail", typeof(RectTransform), typeof(Text));
            itemsGo.transform.SetParent(paperGo.transform, false);
            RectTransform itRt = itemsGo.GetComponent<RectTransform>();
            itRt.anchorMin = new Vector2(0.5f, 0.40f);
            itRt.anchorMax = new Vector2(0.5f, 0.40f);
            itRt.sizeDelta = new Vector2(780, 140);
            Text itTxt = itemsGo.GetComponent<Text>();
            itTxt.text = "---------------------------------------------------------\nITEM                                       QTY     STATUS\n---------------------------------------------------------\n팔레트 적재 카톤 박스                      120 EA  정상 배송\n콜드체인 특송 패키지                        35 EA  인수 완료\n═════════════════════════════════════════════════════════\nTOTAL: ₩ 158,000 / 정상 승인 완료";
            itTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itTxt.fontSize = 20;
            itTxt.fontStyle = FontStyle.Bold;
            itTxt.alignment = TextAnchor.MiddleLeft;
            itTxt.color = new Color(0.12f, 0.15f, 0.2f);

            // 5. 붉은색 승인 도장 [ PAID ]
            GameObject stampGo = new GameObject("PaidStamp", typeof(RectTransform), typeof(Image));
            stampGo.transform.SetParent(paperGo.transform, false);
            RectTransform stampRt = stampGo.GetComponent<RectTransform>();
            stampRt.anchorMin = new Vector2(0.5f, 0.24f);
            stampRt.anchorMax = new Vector2(0.5f, 0.24f);
            stampRt.sizeDelta = new Vector2(300, 105);
            stampRt.localRotation = Quaternion.Euler(0, 0, -12f);
            stampRt.localScale = Vector3.zero;
            Image stampImg = stampGo.GetComponent<Image>();
            stampImg.color = new Color(0.85f, 0.18f, 0.15f);

            GameObject stampInner = new GameObject("StampInner", typeof(RectTransform), typeof(Image));
            stampInner.transform.SetParent(stampGo.transform, false);
            RectTransform siRt = stampInner.GetComponent<RectTransform>();
            siRt.anchorMin = Vector2.zero;
            siRt.anchorMax = Vector2.one;
            siRt.offsetMin = new Vector2(5, 5);
            siRt.offsetMax = new Vector2(-5, -5);
            Image siImg = stampInner.GetComponent<Image>();
            siImg.color = new Color(0.97f, 0.96f, 0.92f);

            GameObject stampTextGo = new GameObject("StampText", typeof(RectTransform), typeof(Text));
            stampTextGo.transform.SetParent(stampGo.transform, false);
            RectTransform stRt = stampTextGo.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;
            Text stTxt = stampTextGo.GetComponent<Text>();
            stTxt.text = "★ PAID ★\nAPPROVED";
            stTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stTxt.fontSize = 30;
            stTxt.fontStyle = FontStyle.Bold;
            stTxt.alignment = TextAnchor.MiddleCenter;
            stTxt.color = new Color(0.85f, 0.18f, 0.15f);

            // 6. 하단 톱니모양 절취선 (Perforated Line)
            GameObject perfGo = new GameObject("PerforatedLine", typeof(RectTransform), typeof(Image));
            perfGo.transform.SetParent(paperGo.transform, false);
            RectTransform perfRt = perfGo.GetComponent<RectTransform>();
            perfRt.anchorMin = new Vector2(0, 0.05f);
            perfRt.anchorMax = new Vector2(1, 0.05f);
            perfRt.sizeDelta = new Vector2(0, 24);
            Image perfImg = perfGo.GetComponent<Image>();
            perfImg.color = new Color(0.85f, 0.85f, 0.85f, 0.3f);

            GameObject cutTxtGo = new GameObject("CutText", typeof(RectTransform), typeof(Text));
            cutTxtGo.transform.SetParent(perfGo.transform, false);
            RectTransform ctRt = cutTxtGo.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;
            Text ctTxt = cutTxtGo.GetComponent<Text>();
            ctTxt.text = "✂ - - - - - - - - - - - - - - - - CUT HERE - - - - - - - - - - - - - - - - ✂";
            ctTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctTxt.fontSize = 18;
            ctTxt.alignment = TextAnchor.MiddleCenter;
            ctTxt.color = new Color(0.45f, 0.45f, 0.5f);

            // 7. 상단 블랙 메탈 영수증 배출구 (PrinterSlot)
            GameObject slotGo = new GameObject("PrinterSlot", typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(root.transform, false);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0.5f, 1);
            slotRt.anchorMax = new Vector2(0.5f, 1);
            slotRt.pivot = new Vector2(0.5f, 1);
            slotRt.sizeDelta = new Vector2(1000, 55);
            Image slotImg = slotGo.GetComponent<Image>();
            slotImg.color = new Color(0.14f, 0.15f, 0.18f); // 블랙 알루미늄

            // 배출구 슬릿
            GameObject slit = new GameObject("Slit", typeof(RectTransform), typeof(Image));
            slit.transform.SetParent(slotGo.transform, false);
            RectTransform slitRt = slit.GetComponent<RectTransform>();
            slitRt.anchorMin = new Vector2(0.04f, 0.15f);
            slitRt.anchorMax = new Vector2(0.96f, 0.35f);
            slitRt.offsetMin = Vector2.zero;
            slitRt.offsetMax = Vector2.zero;
            Image slitImg = slit.GetComponent<Image>();
            slitImg.color = new Color(0.05f, 0.05f, 0.06f);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("printerSlot").objectReferenceValue = slotRt;
            so.FindProperty("receiptPaper").objectReferenceValue = paperRt;
            so.FindProperty("paidStamp").objectReferenceValue = stampRt;
            so.FindProperty("paidStampText").objectReferenceValue = stTxt;
            so.FindProperty("perforatedLine").objectReferenceValue = perfRt;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/ReceiptRollTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // 호환용 BubbleWrapTransition.prefab으로도 저장
            string bubblePath = $"{PrefabDir}/BubbleWrapTransition.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, bubblePath);

            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBubbleWrapPrefab(Sprite circleSprite)
        {
            return CreateReceiptRollPrefab(circleSprite);
        }

        private static GameObject CreateContainerDoorPrefab()
        {
            GameObject root = CreateBaseTransitionRoot("ContainerDoorTransition");
            var comp = root.AddComponent<ContainerDoorTransition>();

            // Door Left
            GameObject leftGo = new GameObject("DoorLeft", typeof(RectTransform), typeof(Image));
            leftGo.transform.SetParent(root.transform, false);
            RectTransform leftRt = leftGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.5f, 1);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;
            Image leftImg = leftGo.GetComponent<Image>();
            leftImg.color = new Color(0.18f, 0.44f, 0.72f); // 오션 블루

            // Door Right
            GameObject rightGo = new GameObject("DoorRight", typeof(RectTransform), typeof(Image));
            rightGo.transform.SetParent(root.transform, false);
            RectTransform rightRt = rightGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;
            Image rightImg = rightGo.GetComponent<Image>();
            rightImg.color = new Color(0.18f, 0.44f, 0.72f);

            // Container Ribs Graphic
            for (int i = 0; i < 8; i++)
            {
                Transform p = (i < 4) ? leftGo.transform : rightGo.transform;
                GameObject rib = new GameObject($"Rib_{i}", typeof(RectTransform), typeof(Image));
                rib.transform.SetParent(p, false);
                RectTransform rrt = rib.GetComponent<RectTransform>();
                float xNorm = (i % 4) * 0.24f + 0.12f;
                rrt.anchorMin = new Vector2(xNorm, 0.05f);
                rrt.anchorMax = new Vector2(xNorm, 0.95f);
                rrt.sizeDelta = new Vector2(24, 0);
                Image rimg = rib.GetComponent<Image>();
                rimg.color = new Color(0.14f, 0.35f, 0.58f);
            }

            // Lock Rod (중앙 세로 잠금 봉)
            GameObject rodGo = new GameObject("LockRod", typeof(RectTransform), typeof(Image));
            rodGo.transform.SetParent(root.transform, false);
            RectTransform rodRt = rodGo.GetComponent<RectTransform>();
            rodRt.sizeDelta = new Vector2(26, 650);
            rodRt.anchoredPosition = Vector2.zero;
            Image rodImg = rodGo.GetComponent<Image>();
            rodImg.color = new Color(0.85f, 0.85f, 0.88f);

            // Lock Handle
            GameObject handleGo = new GameObject("LockHandle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(rodGo.transform, false);
            RectTransform handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(85, 24);
            handleRt.anchoredPosition = new Vector2(25, 0);
            Image handleImg = handleGo.GetComponent<Image>();
            handleImg.color = new Color(0.95f, 0.35f, 0.15f);

            // Serial Text
            GameObject txtGo = new GameObject("ContainerSerial", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(root.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(550, 70);
            trt.anchoredPosition = new Vector2(0, -280);
            Text txt = txtGo.GetComponent<Text>();
            txt.text = "[ ISO STEEL CONTAINER: MSKU-842910 ]";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1, 1, 1, 0);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("doorLeft").objectReferenceValue = leftRt;
            so.FindProperty("doorRight").objectReferenceValue = rightRt;
            so.FindProperty("lockRod").objectReferenceValue = rodRt;
            so.FindProperty("lockHandle").objectReferenceValue = handleImg;
            so.FindProperty("containerSerialText").objectReferenceValue = txt;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/ContainerDoorTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject CreateFragileTapePrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("FragileTapeTransition");
            var comp = root.AddComponent<FragileTapeTransition>();

            // 1. 크라프트 박스 브라운 배경 패널 (화면 100% 덮음, 좌우 분할 패널)
            GameObject leftGo = new GameObject("CardboardLeft", typeof(RectTransform), typeof(Image));
            leftGo.transform.SetParent(root.transform, false);
            RectTransform leftRt = leftGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.5f, 1);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;
            Image leftImg = leftGo.GetComponent<Image>();
            leftImg.color = new Color(0.78f, 0.58f, 0.38f); // 크라프트 박스 브라운

            GameObject rightGo = new GameObject("CardboardRight", typeof(RectTransform), typeof(Image));
            rightGo.transform.SetParent(root.transform, false);
            RectTransform rightRt = rightGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.5f, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;
            Image rightImg = rightGo.GetComponent<Image>();
            rightImg.color = new Color(0.75f, 0.55f, 0.35f);

            // 박스 테두리 이음새 음영
            for (int s = 0; s < 2; s++)
            {
                GameObject seam = new GameObject("SeamShadow", typeof(RectTransform), typeof(Image));
                seam.transform.SetParent(s == 0 ? leftGo.transform : rightGo.transform, false);
                RectTransform seamRt = seam.GetComponent<RectTransform>();
                seamRt.anchorMin = new Vector2(s == 0 ? 0.98f : 0f, 0);
                seamRt.anchorMax = new Vector2(s == 0 ? 1f : 0.02f, 1);
                seamRt.offsetMin = Vector2.zero;
                seamRt.offsetMax = Vector2.zero;
                Image seamImg = seam.GetComponent<Image>();
                seamImg.color = new Color(0.35f, 0.22f, 0.12f, 0.7f);
            }

            // 2. 테이프들 (RectMask2D 적용 5개 스트립)
            List<RectTransform> tapes = new List<RectTransform>();

            // Tape 0: 상단 가로 보강 테이프 (블루)
            RectTransform t0 = CreateTapeStrip(root.transform, "Tape_Top",
                new Vector2(-1100f, 380f), 0f, 2200f, 60f,
                new Color(0.18f, 0.38f, 0.65f),
                "★ LOGISTICS TYCOON EXPRESS DELIVERY ★  [ SEALED CARGO ]  ★",
                Color.white, 22);
            tapes.Add(t0);

            // Tape 1: 하단 가로 보강 테이프 (그린)
            RectTransform t1 = CreateTapeStrip(root.transform, "Tape_Bottom",
                new Vector2(-1100f, -380f), 0f, 2200f, 60f,
                new Color(0.20f, 0.55f, 0.35f),
                "✔ QUALITY ASSURED  /  物流 檢査 完了  /  AUTHENTIC PRODUCT ✔",
                Color.white, 22);
            tapes.Add(t1);

            // Tape 2: 사선 -32도 레드 - ⚠️ FRAGILE / 취급주의 / HANDLE WITH CARE
            RectTransform t2 = CreateTapeStrip(root.transform, "Tape_Fragile_Red",
                new Vector2(-1102f, 689f), -32f, 2600f, 90f,
                new Color(0.85f, 0.18f, 0.15f),
                "⚠️ FRAGILE  /  취급주의  /  HANDLE WITH CARE  /  파손주의 ⚠️",
                new Color(0.98f, 0.88f, 0.2f), 26);
            tapes.Add(t2);

            // Tape 3: 사선 +32도 옐로우 - ⚡ PRIORITY EXPRESS / 당일 로켓 특송 ⚡
            RectTransform t3 = CreateTapeStrip(root.transform, "Tape_Priority_Yellow",
                new Vector2(-1102f, -689f), 32f, 2600f, 90f,
                new Color(0.96f, 0.78f, 0.12f),
                "⚡ PRIORITY EXPRESS  /  당일 로켓 특송  /  DO NOT DROP ⚡",
                new Color(0.12f, 0.12f, 0.14f), 26);
            tapes.Add(t3);

            // Tape 4: 중앙 메인 보안 테이프 (오렌지) - 마지막 밀봉 테이프
            RectTransform t4 = CreateTapeStrip(root.transform, "Tape_Sealed_Orange",
                new Vector2(-1100f, 0f), 0f, 2200f, 105f,
                new Color(0.92f, 0.45f, 0.12f),
                "🔒 SEALED CARGO  /  검수 완료 보안 밀봉  /  INSPECTION PASSED 🔒",
                Color.white, 26);
            tapes.Add(t4);

            // 3. 중앙 검수 완료 스탬프 [ EXAMINED & VERIFIED ]
            GameObject stampGo = new GameObject("ExamStamp", typeof(RectTransform), typeof(Image));
            stampGo.transform.SetParent(root.transform, false);
            RectTransform stampRt = stampGo.GetComponent<RectTransform>();
            stampRt.anchoredPosition = Vector2.zero;
            stampRt.sizeDelta = new Vector2(400, 140);
            stampRt.localRotation = Quaternion.Euler(0, 0, -8f);
            Image stampImg = stampGo.GetComponent<Image>();
            stampImg.color = new Color(0.85f, 0.22f, 0.18f); // 레드 스탬프

            GameObject stampInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            stampInner.transform.SetParent(stampGo.transform, false);
            RectTransform siRt = stampInner.GetComponent<RectTransform>();
            siRt.anchorMin = Vector2.zero;
            siRt.anchorMax = Vector2.one;
            siRt.offsetMin = new Vector2(5, 5);
            siRt.offsetMax = new Vector2(-5, -5);
            Image siImg = stampInner.GetComponent<Image>();
            siImg.color = new Color(0.97f, 0.94f, 0.88f);

            GameObject stampTxtGo = new GameObject("StampText", typeof(RectTransform), typeof(Text));
            stampTxtGo.transform.SetParent(stampGo.transform, false);
            RectTransform stRt = stampTxtGo.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;
            Text st = stampTxtGo.GetComponent<Text>();
            st.text = "[ EXAMINED & VERIFIED ]\nLOGISTICS QUALITY CONTROL\n★ PASS ★";
            st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            st.fontSize = 22;
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.color = new Color(0.85f, 0.22f, 0.18f);

            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("splitLeft").objectReferenceValue = leftRt;
            so.FindProperty("splitRight").objectReferenceValue = rightRt;
            SerializedProperty tapeProp = so.FindProperty("tapeStripes");
            tapeProp.arraySize = tapes.Count;
            for (int i = 0; i < tapes.Count; i++)
                tapeProp.GetArrayElementAtIndex(i).objectReferenceValue = tapes[i];
            so.FindProperty("examStamp").objectReferenceValue = stampRt;
            so.FindProperty("examStampText").objectReferenceValue = st;
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/FragileTapeTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // 호환용 DeliveryDroneTransition.prefab으로도 저장
            string dronePath = $"{PrefabDir}/DeliveryDroneTransition.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, dronePath);

            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform CreateTapeStrip(
            Transform parent,
            string name,
            Vector2 startPos,
            float angleDeg,
            float totalWidth,
            float height,
            Color bgColor,
            string textMessage,
            Color textColor,
            int fontSize)
        {
            GameObject tapeGo = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            tapeGo.transform.SetParent(parent, false);
            RectTransform tapeRt = tapeGo.GetComponent<RectTransform>();
            tapeRt.pivot = new Vector2(0f, 0.5f);
            tapeRt.anchoredPosition = startPos;
            tapeRt.sizeDelta = new Vector2(totalWidth, height);
            tapeRt.localRotation = Quaternion.Euler(0, 0, angleDeg);

            // Tape Background Image
            GameObject bgGo = new GameObject("TapeBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(tapeGo.transform, false);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(totalWidth, height);
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.color = bgColor;

            // Tape Text (Fixed size, not stretching)
            GameObject txtGo = new GameObject("TapeText", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(tapeGo.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchorMin = new Vector2(0, 0.5f);
            trt.anchorMax = new Vector2(0, 0.5f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(totalWidth, height);
            Text txt = txtGo.GetComponent<Text>();
            txt.text = textMessage;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor;

            return tapeRt;
        }

        private static GameObject CreateDeliveryDronePrefab(Sprite circleSprite)
        {
            return CreateFragileTapePrefab(circleSprite);
        }

        private static GameObject CreateWorldMapPinPrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("WorldMapPinTransition");
            var comp = root.AddComponent<WorldMapPinTransition>();

            // Map Container (중앙 피벗 (0.5, 0.5)으로 줌인 스케일의 중심)
            GameObject mapContainerGo = new GameObject("MapContainer", typeof(RectTransform));
            mapContainerGo.transform.SetParent(root.transform, false);
            RectTransform mapContainerRt = mapContainerGo.GetComponent<RectTransform>();
            mapContainerRt.anchorMin = Vector2.zero;
            mapContainerRt.anchorMax = Vector2.one;
            mapContainerRt.offsetMin = Vector2.zero;
            mapContainerRt.offsetMax = Vector2.zero;
            mapContainerRt.pivot = new Vector2(0.5f, 0.5f);

            // Map Sheet (크림 베이지 기본 바탕 #F5EEDB)
            GameObject mapSheetGo = new GameObject("MapSheet", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            mapSheetGo.transform.SetParent(mapContainerGo.transform, false);
            RectTransform sheetRt = mapSheetGo.GetComponent<RectTransform>();
            sheetRt.anchorMin = Vector2.zero;
            sheetRt.anchorMax = Vector2.one;
            sheetRt.offsetMin = Vector2.zero;
            sheetRt.offsetMax = Vector2.zero;
            Image sheetImg = mapSheetGo.GetComponent<Image>();
            sheetImg.color = new Color(0.96f, 0.93f, 0.86f); // 크림 베이지
            CanvasGroup mapCg = mapSheetGo.GetComponent<CanvasGroup>();

            // 1. Grid Lines Group
            GameObject gridGo = new GameObject("GridLinesGroup", typeof(RectTransform), typeof(CanvasGroup));
            gridGo.transform.SetParent(mapSheetGo.transform, false);
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = Vector2.one;
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;
            CanvasGroup gridCg = gridGo.GetComponent<CanvasGroup>();

            Color gridColor = new Color(0.85f, 0.81f, 0.72f, 0.65f);
            // 가로 격자선 8개
            for (int i = 1; i <= 8; i++)
            {
                float normY = i / 9.0f;
                GameObject hLine = new GameObject($"HLine_{i}", typeof(RectTransform), typeof(Image));
                hLine.transform.SetParent(gridGo.transform, false);
                RectTransform hlRt = hLine.GetComponent<RectTransform>();
                hlRt.anchorMin = new Vector2(0, normY);
                hlRt.anchorMax = new Vector2(1, normY);
                hlRt.sizeDelta = new Vector2(0, 2);
                Image hlImg = hLine.GetComponent<Image>();
                hlImg.color = gridColor;
            }
            // 세로 격자선 12개
            for (int i = 1; i <= 12; i++)
            {
                float normX = i / 13.0f;
                GameObject vLine = new GameObject($"VLine_{i}", typeof(RectTransform), typeof(Image));
                vLine.transform.SetParent(gridGo.transform, false);
                RectTransform vlRt = vLine.GetComponent<RectTransform>();
                vlRt.anchorMin = new Vector2(normX, 0);
                vlRt.anchorMax = new Vector2(normX, 1);
                vlRt.sizeDelta = new Vector2(2, 0);
                Image vlImg = vLine.GetComponent<Image>();
                vlImg.color = gridColor;
            }

            // 2. Route Lines Group (물류 배송망 & 웨이포인트 노드)
            GameObject routesGo = new GameObject("RouteLinesGroup", typeof(RectTransform), typeof(CanvasGroup));
            routesGo.transform.SetParent(mapSheetGo.transform, false);
            RectTransform routesRt = routesGo.GetComponent<RectTransform>();
            routesRt.anchorMin = Vector2.zero;
            routesRt.anchorMax = Vector2.one;
            routesRt.offsetMin = Vector2.zero;
            routesRt.offsetMax = Vector2.zero;
            CanvasGroup routesCg = routesGo.GetComponent<CanvasGroup>();

            // 주요 배송 도로선 (굵은 주황색/청록색 라인)
            // 메인 고속 배송로 (좌->우 횡단)
            GameObject mainRoute = new GameObject("MainExpressHighway", typeof(RectTransform), typeof(Image));
            mainRoute.transform.SetParent(routesGo.transform, false);
            RectTransform mrRt = mainRoute.GetComponent<RectTransform>();
            mrRt.anchorMin = new Vector2(0.1f, 0.52f);
            mrRt.anchorMax = new Vector2(0.9f, 0.52f);
            mrRt.sizeDelta = new Vector2(0, 8);
            Image mrImg = mainRoute.GetComponent<Image>();
            mrImg.color = new Color(0.92f, 0.44f, 0.20f, 0.85f);

            // 대각선 간선로 1
            GameObject diag1 = new GameObject("BranchRoute_1", typeof(RectTransform), typeof(Image));
            diag1.transform.SetParent(routesGo.transform, false);
            RectTransform d1Rt = diag1.GetComponent<RectTransform>();
            d1Rt.anchorMin = new Vector2(0.5f, 0.5f);
            d1Rt.anchorMax = new Vector2(0.5f, 0.5f);
            d1Rt.sizeDelta = new Vector2(750, 6);
            d1Rt.localRotation = Quaternion.Euler(0, 0, 32);
            Image d1Img = diag1.GetComponent<Image>();
            d1Img.color = new Color(0.32f, 0.65f, 0.85f, 0.75f);

            // 대각선 간선로 2
            GameObject diag2 = new GameObject("BranchRoute_2", typeof(RectTransform), typeof(Image));
            diag2.transform.SetParent(routesGo.transform, false);
            RectTransform d2Rt = diag2.GetComponent<RectTransform>();
            d2Rt.anchorMin = new Vector2(0.5f, 0.5f);
            d2Rt.anchorMax = new Vector2(0.5f, 0.5f);
            d2Rt.sizeDelta = new Vector2(620, 6);
            d2Rt.localRotation = Quaternion.Euler(0, 0, -38);
            Image d2Img = diag2.GetComponent<Image>();
            d2Img.color = new Color(0.95f, 0.65f, 0.25f, 0.75f);

            // 웨이포인트 노드들 (Waypoint Hub Nodes)
            Vector2[] nodeAnchors = new Vector2[]
            {
                new Vector2(0.2f, 0.52f),
                new Vector2(0.35f, 0.65f),
                new Vector2(0.65f, 0.38f),
                new Vector2(0.8f, 0.52f),
                new Vector2(0.5f, 0.5f) // 중앙 허브 (목적지)
            };
            for (int i = 0; i < nodeAnchors.Length; i++)
            {
                GameObject nodeGo = new GameObject($"Node_{i}", typeof(RectTransform), typeof(Image));
                nodeGo.transform.SetParent(routesGo.transform, false);
                RectTransform nRt = nodeGo.GetComponent<RectTransform>();
                nRt.anchorMin = nodeAnchors[i];
                nRt.anchorMax = nodeAnchors[i];
                nRt.sizeDelta = (i == 4) ? new Vector2(40, 40) : new Vector2(24, 24);
                Image nImg = nodeGo.GetComponent<Image>();
                nImg.sprite = circleSprite;
                nImg.color = (i == 4) ? new Color(0.92f, 0.25f, 0.20f) : new Color(0.32f, 0.65f, 0.85f);
            }

            // 지도 워터마크 텍스트들
            GameObject wm1 = new GameObject("MapWatermark_Left", typeof(RectTransform), typeof(Text));
            wm1.transform.SetParent(routesGo.transform, false);
            RectTransform wm1Rt = wm1.GetComponent<RectTransform>();
            wm1Rt.anchorMin = new Vector2(0.04f, 0.88f);
            wm1Rt.anchorMax = new Vector2(0.45f, 0.95f);
            wm1Rt.offsetMin = Vector2.zero;
            wm1Rt.offsetMax = Vector2.zero;
            Text wm1Txt = wm1.GetComponent<Text>();
            wm1Txt.text = "LOGISTICS REGION #4 [EXPRESS CARGO MAP]";
            wm1Txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            wm1Txt.fontSize = 20;
            wm1Txt.fontStyle = FontStyle.Bold;
            wm1Txt.alignment = TextAnchor.MiddleLeft;
            wm1Txt.color = new Color(0.65f, 0.60f, 0.52f, 0.85f);

            GameObject wm2 = new GameObject("MapWatermark_Right", typeof(RectTransform), typeof(Text));
            wm2.transform.SetParent(routesGo.transform, false);
            RectTransform wm2Rt = wm2.GetComponent<RectTransform>();
            wm2Rt.anchorMin = new Vector2(0.55f, 0.05f);
            wm2Rt.anchorMax = new Vector2(0.96f, 0.12f);
            wm2Rt.offsetMin = Vector2.zero;
            wm2Rt.offsetMax = Vector2.zero;
            Text wm2Txt = wm2.GetComponent<Text>();
            wm2Txt.text = "DESTINATION: ZONE B LOGISTICS HUB [PINPOINT 📍]";
            wm2Txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            wm2Txt.fontSize = 18;
            wm2Txt.fontStyle = FontStyle.Bold;
            wm2Txt.alignment = TextAnchor.MiddleRight;
            wm2Txt.color = new Color(0.65f, 0.60f, 0.52f, 0.85f);

            // 3. 3단 접이식 날개 패널들 (Left, Center, Right)
            // Center Fold
            GameObject centerGo = new GameObject("MapFoldCenter", typeof(RectTransform), typeof(Image));
            centerGo.transform.SetParent(mapSheetGo.transform, false);
            RectTransform centerRt = centerGo.GetComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.333f, 0);
            centerRt.anchorMax = new Vector2(0.667f, 1);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.offsetMin = Vector2.zero;
            centerRt.offsetMax = Vector2.zero;
            Image centerImg = centerGo.GetComponent<Image>();
            centerImg.color = new Color(0.96f, 0.93f, 0.86f, 0.35f);

            // Left Fold
            GameObject leftGo = new GameObject("MapFoldLeft", typeof(RectTransform), typeof(Image));
            leftGo.transform.SetParent(mapSheetGo.transform, false);
            RectTransform leftRt = leftGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.333f, 1);
            leftRt.pivot = new Vector2(0f, 0.5f);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;
            Image leftImg = leftGo.GetComponent<Image>();
            leftImg.color = new Color(0.91f, 0.87f, 0.79f, 0.65f); // 접힘 음영 톤

            // Left Fold Right Edge Seam Shadow
            GameObject leftSeam = new GameObject("SeamShadow", typeof(RectTransform), typeof(Image));
            leftSeam.transform.SetParent(leftGo.transform, false);
            RectTransform lsRt = leftSeam.GetComponent<RectTransform>();
            lsRt.anchorMin = new Vector2(1, 0);
            lsRt.anchorMax = new Vector2(1, 1);
            lsRt.pivot = new Vector2(1, 0.5f);
            lsRt.sizeDelta = new Vector2(5, 0);
            Image lsImg = leftSeam.GetComponent<Image>();
            lsImg.color = new Color(0.65f, 0.58f, 0.48f, 0.7f);

            // Right Fold
            GameObject rightGo = new GameObject("MapFoldRight", typeof(RectTransform), typeof(Image));
            rightGo.transform.SetParent(mapSheetGo.transform, false);
            RectTransform rightRt = rightGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.667f, 0);
            rightRt.anchorMax = new Vector2(1f, 1);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;
            Image rightImg = rightGo.GetComponent<Image>();
            rightImg.color = new Color(0.91f, 0.87f, 0.79f, 0.65f); // 접힘 음영 톤

            // Right Fold Left Edge Seam Shadow
            GameObject rightSeam = new GameObject("SeamShadow", typeof(RectTransform), typeof(Image));
            rightSeam.transform.SetParent(rightGo.transform, false);
            RectTransform rsRt = rightSeam.GetComponent<RectTransform>();
            rsRt.anchorMin = new Vector2(0, 0);
            rsRt.anchorMax = new Vector2(0, 1);
            rsRt.pivot = new Vector2(0, 0.5f);
            rsRt.sizeDelta = new Vector2(5, 0);
            Image rsImg = rightSeam.GetComponent<Image>();
            rsImg.color = new Color(0.65f, 0.58f, 0.48f, 0.7f);

            // 4. GPS Pin Root & Components (중앙 배치)
            GameObject pinRootGo = new GameObject("PinRoot", typeof(RectTransform));
            pinRootGo.transform.SetParent(root.transform, false);
            RectTransform pinRootRt = pinRootGo.GetComponent<RectTransform>();
            pinRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            pinRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            pinRootRt.sizeDelta = new Vector2(300, 300);
            pinRootRt.anchoredPosition = Vector2.zero;

            // Radar Wave Rings (3개)
            List<RectTransform> rings = new List<RectTransform>();
            for (int i = 0; i < 3; i++)
            {
                GameObject ringGo = new GameObject($"RadarRing_{i}", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(pinRootGo.transform, false);
                RectTransform ringRt = ringGo.GetComponent<RectTransform>();
                ringRt.anchorMin = new Vector2(0.5f, 0.5f);
                ringRt.anchorMax = new Vector2(0.5f, 0.5f);
                ringRt.sizeDelta = new Vector2(120, 120);
                ringRt.anchoredPosition = Vector2.zero;
                ringRt.localScale = Vector3.zero;
                Image ringImg = ringGo.GetComponent<Image>();
                ringImg.sprite = circleSprite;
                ringImg.color = new Color(0.92f, 0.25f, 0.20f, 0.85f);
                rings.Add(ringRt);
            }

            // Pin Shadow (바닥 타원 그림자)
            GameObject shadowGo = new GameObject("PinShadow", typeof(RectTransform), typeof(Image));
            shadowGo.transform.SetParent(pinRootGo.transform, false);
            RectTransform shadowRt = shadowGo.GetComponent<RectTransform>();
            shadowRt.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRt.sizeDelta = new Vector2(90, 28);
            shadowRt.anchoredPosition = new Vector2(0, -10);
            Image shadowImg = shadowGo.GetComponent<Image>();
            shadowImg.sprite = circleSprite;
            shadowImg.color = new Color(0, 0, 0, 0.45f);

            // Pin Visual Root (쿵! 낙하 및 통통 바운스)
            GameObject pinVisGo = new GameObject("PinVisual", typeof(RectTransform));
            pinVisGo.transform.SetParent(pinRootGo.transform, false);
            RectTransform pinVisRt = pinVisGo.GetComponent<RectTransform>();
            pinVisRt.anchorMin = new Vector2(0.5f, 0.5f);
            pinVisRt.anchorMax = new Vector2(0.5f, 0.5f);
            pinVisRt.pivot = new Vector2(0.5f, 0f);
            pinVisRt.sizeDelta = new Vector2(100, 140);
            pinVisRt.anchoredPosition = Vector2.zero;

            // Pin Sharp Needle
            GameObject needleGo = new GameObject("PinNeedle", typeof(RectTransform), typeof(Image));
            needleGo.transform.SetParent(pinVisGo.transform, false);
            RectTransform ndlRt = needleGo.GetComponent<RectTransform>();
            ndlRt.anchorMin = new Vector2(0.5f, 0f);
            ndlRt.anchorMax = new Vector2(0.5f, 0f);
            ndlRt.pivot = new Vector2(0.5f, 0f);
            ndlRt.sizeDelta = new Vector2(18, 55);
            ndlRt.anchoredPosition = new Vector2(0, 0);
            Image ndlImg = needleGo.GetComponent<Image>();
            ndlImg.color = new Color(0.78f, 0.16f, 0.14f);

            // Pin Round Head
            GameObject headGo = new GameObject("PinHead", typeof(RectTransform), typeof(Image));
            headGo.transform.SetParent(pinVisGo.transform, false);
            RectTransform headRt = headGo.GetComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0.5f, 0f);
            headRt.anchorMax = new Vector2(0.5f, 0f);
            headRt.pivot = new Vector2(0.5f, 0.5f);
            headRt.sizeDelta = new Vector2(80, 80);
            headRt.anchoredPosition = new Vector2(0, 75);
            Image headImg = headGo.GetComponent<Image>();
            headImg.sprite = circleSprite;
            headImg.color = new Color(0.92f, 0.22f, 0.18f); // 선명한 레드

            // Pin Inner Point
            GameObject innerGo = new GameObject("PinInnerPoint", typeof(RectTransform), typeof(Image));
            innerGo.transform.SetParent(headGo.transform, false);
            RectTransform innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.5f, 0.5f);
            innerRt.anchorMax = new Vector2(0.5f, 0.5f);
            innerRt.sizeDelta = new Vector2(28, 28);
            Image innerImg = innerGo.GetComponent<Image>();
            innerImg.sprite = circleSprite;
            innerImg.color = Color.white;

            // Pin Emoji Overlay (📍)
            GameObject emojiGo = new GameObject("PinEmoji", typeof(RectTransform), typeof(Text));
            emojiGo.transform.SetParent(headGo.transform, false);
            RectTransform emRt = emojiGo.GetComponent<RectTransform>();
            emRt.anchorMin = Vector2.zero;
            emRt.anchorMax = Vector2.one;
            emRt.offsetMin = Vector2.zero;
            emRt.offsetMax = Vector2.zero;
            Text emTxt = emojiGo.GetComponent<Text>();
            emTxt.text = "📍";
            emTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emTxt.fontSize = 40;
            emTxt.alignment = TextAnchor.MiddleCenter;

            // Label Bubble (미니 말풍선 "스테이지 B 물류 허브")
            GameObject bubbleGo = new GameObject("LabelBubble", typeof(RectTransform), typeof(Image));
            bubbleGo.transform.SetParent(pinRootGo.transform, false);
            RectTransform bblRt = bubbleGo.GetComponent<RectTransform>();
            bblRt.anchorMin = new Vector2(0.5f, 0.5f);
            bblRt.anchorMax = new Vector2(0.5f, 0.5f);
            bblRt.pivot = new Vector2(0.5f, 0f);
            bblRt.sizeDelta = new Vector2(340, 60);
            bblRt.anchoredPosition = new Vector2(0, 130);
            bblRt.localScale = Vector3.zero;
            Image bblImg = bubbleGo.GetComponent<Image>();
            bblImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f); // 다크 네이비 캡슐

            GameObject bblTxtGo = new GameObject("LabelText", typeof(RectTransform), typeof(Text));
            bblTxtGo.transform.SetParent(bubbleGo.transform, false);
            RectTransform btRt = bblTxtGo.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;
            Text bblTxt = bblTxtGo.GetComponent<Text>();
            bblTxt.text = "📍 스테이지 B 물류 허브";
            bblTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bblTxt.fontSize = 24;
            bblTxt.fontStyle = FontStyle.Bold;
            bblTxt.alignment = TextAnchor.MiddleCenter;
            bblTxt.color = new Color(1f, 0.88f, 0.45f); // 웜 골드

            // SerializedObject 바인딩
            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("mapContainer").objectReferenceValue = mapContainerRt;
            so.FindProperty("mapSheet").objectReferenceValue = sheetRt;
            so.FindProperty("mapFoldLeft").objectReferenceValue = leftRt;
            so.FindProperty("mapFoldRight").objectReferenceValue = rightRt;
            so.FindProperty("mapFoldCenter").objectReferenceValue = centerRt;
            so.FindProperty("mapCanvasGroup").objectReferenceValue = mapCg;

            so.FindProperty("pinRoot").objectReferenceValue = pinRootRt;
            so.FindProperty("pinVisual").objectReferenceValue = pinVisRt;
            so.FindProperty("pinShadow").objectReferenceValue = shadowImg;

            SerializedProperty ringsProp = so.FindProperty("radarRings");
            ringsProp.arraySize = rings.Count;
            for (int i = 0; i < rings.Count; i++)
                ringsProp.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];

            so.FindProperty("labelBubble").objectReferenceValue = bblRt;
            so.FindProperty("labelBubbleText").objectReferenceValue = bblTxt;

            so.FindProperty("gridGroup").objectReferenceValue = gridCg;
            so.FindProperty("routeLinesGroup").objectReferenceValue = routesCg;

            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/WorldMapPinTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateDiagonalMeteorPrefab(Sprite capsuleSprite, Sprite circleSprite, Sprite starTailSprite)
        {
            GameObject root = CreateBaseTransitionRoot("DiagonalMeteorTransition");
            var comp = root.AddComponent<DiagonalMeteorTransition>();

            // 1. Stripe Container (대각선 각도 -40도 회전)
            GameObject stripeContainerGo = new GameObject("StripeContainer", typeof(RectTransform));
            stripeContainerGo.transform.SetParent(root.transform, false);
            RectTransform stripeContainerRt = stripeContainerGo.GetComponent<RectTransform>();
            stripeContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            stripeContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            stripeContainerRt.anchoredPosition = Vector2.zero;
            stripeContainerRt.sizeDelta = new Vector2(3800, 2400);
            stripeContainerRt.localEulerAngles = new Vector3(0, 0, -40f);

            // 7단계 골판지 카톤/크라프트 웜 브라운 팔레트
            Color[] colors = new Color[]
            {
                new Color(0.31f, 0.19f, 0.11f), // Stripe 0: 딥 카톤 브라운 (#4E311B)
                new Color(0.49f, 0.31f, 0.18f), // Stripe 1: 크라프트 박스 브라운 (#7D4E2D)
                new Color(0.65f, 0.41f, 0.24f), // Stripe 2: 카라멜 골판지 브라운 (#A6683C)
                new Color(0.76f, 0.49f, 0.29f), // Stripe 3: 웜 시나몬 브라운 (#C27E4B)
                new Color(0.85f, 0.59f, 0.36f), // Stripe 4: 골판지 코퍼 베이지 (#D9965B)
                new Color(0.90f, 0.70f, 0.52f), // Stripe 5: 소프트 밀크티 베이지 (#E5B285)
                new Color(0.24f, 0.15f, 0.08f)  // Stripe 6: 리치 다크 초콜릿 브라운 (#3E2514)
            };

            float[] yOffsets = new float[] { 900f, 600f, 300f, 0f, -300f, -600f, -900f };
            float stripeLength = 3800f;
            float stripeThickness = 380f;

            RectTransform[] stripeRts = new RectTransform[7];
            Image[] stripeImgs = new Image[7];

            for (int i = 0; i < 7; i++)
            {
                GameObject sGo = new GameObject($"Stripe_{i}", typeof(RectTransform), typeof(Image));
                sGo.transform.SetParent(stripeContainerGo.transform, false);
                RectTransform sRt = sGo.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.pivot = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(stripeLength, stripeThickness);
                sRt.anchoredPosition = new Vector2(-3800f, yOffsets[i]); // 초기 화면 밖

                Image sImg = sGo.GetComponent<Image>();
                sImg.sprite = capsuleSprite;
                sImg.type = Image.Type.Sliced;
                sImg.color = colors[i];

                stripeRts[i] = sRt;
                stripeImgs[i] = sImg;
            }

            // 2. Star Dots Background (은은한 밤하늘 별빛 도트 18개)
            GameObject starDotsGo = new GameObject("StarDots", typeof(RectTransform), typeof(CanvasGroup));
            starDotsGo.transform.SetParent(root.transform, false);
            RectTransform starDotsRt = starDotsGo.GetComponent<RectTransform>();
            starDotsRt.anchorMin = Vector2.zero;
            starDotsRt.anchorMax = Vector2.one;
            starDotsRt.offsetMin = Vector2.zero;
            starDotsRt.offsetMax = Vector2.zero;
            starDotsGo.SetActive(false);

            int dotCount = 18;
            Image[] dotImgs = new Image[dotCount];
            Vector2[] dotPositions = new Vector2[]
            {
                new Vector2(-550f, 380f), new Vector2(-320f, 420f), new Vector2(120f, 450f),
                new Vector2(480f, 360f),  new Vector2(620f, 220f),  new Vector2(-680f, 180f),
                new Vector2(-420f, 120f), new Vector2(-150f, 190f), new Vector2(250f, 150f),
                new Vector2(580f, 50f),   new Vector2(-600f, -80f), new Vector2(-280f, -140f),
                new Vector2(180f, -120f), new Vector2(450f, -160f), new Vector2(-520f, -320f),
                new Vector2(-180f, -380f),new Vector2(120f, -400f), new Vector2(520f, -350f)
            };
            float[] dotSizes = new float[] { 5f, 7f, 4f, 6f, 5f, 6f, 4f, 8f, 5f, 6f, 4f, 7f, 5f, 6f, 4f, 7f, 5f, 6f };

            for (int d = 0; d < dotCount; d++)
            {
                GameObject dotGo = new GameObject($"Dot_{d}", typeof(RectTransform), typeof(Image));
                dotGo.transform.SetParent(starDotsGo.transform, false);
                RectTransform dRt = dotGo.GetComponent<RectTransform>();
                dRt.anchoredPosition = dotPositions[d];
                dRt.sizeDelta = new Vector2(dotSizes[d], dotSizes[d]);
                Image dImg = dotGo.GetComponent<Image>();
                dImg.sprite = circleSprite;
                dImg.color = new Color(1f, 0.95f, 0.85f, 0.4f);
                dotImgs[d] = dImg;
            }

            // 3. Shooting Star Container (대각선 각도 -40도 회전)
            GameObject shootingStarContainerGo = new GameObject("ShootingStarContainer", typeof(RectTransform));
            shootingStarContainerGo.transform.SetParent(root.transform, false);
            RectTransform shootingStarContainerRt = shootingStarContainerGo.GetComponent<RectTransform>();
            shootingStarContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            shootingStarContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            shootingStarContainerRt.anchoredPosition = Vector2.zero;
            shootingStarContainerRt.sizeDelta = new Vector2(3800, 2400);
            shootingStarContainerRt.localEulerAngles = new Vector3(0, 0, -40f);

            // Star 1 (상단 궤적: Y +280, X -180, 꼬리 480px, 택배 42pt)
            var star1 = CreateSingleShootingStar(
                shootingStarContainerGo.transform, "Star1_Upper",
                new Vector2(-180f, 280f), 480f, 20f, 42,
                starTailSprite, circleSprite);

            // Star 2 (중앙 궤적: Y -30, X +80, 꼬리 560px, 택배 48pt 메인)
            var star2 = CreateSingleShootingStar(
                shootingStarContainerGo.transform, "Star2_Center",
                new Vector2(80f, -30f), 560f, 24f, 48,
                starTailSprite, circleSprite);

            // Star 3 (하단 궤적: Y -260, X -120, 꼬리 420px, 택배 38pt)
            var star3 = CreateSingleShootingStar(
                shootingStarContainerGo.transform, "Star3_Lower",
                new Vector2(-120f, -260f), 420f, 18f, 38,
                starTailSprite, circleSprite);

            // Sub Notice Text
            GameObject subNoticeGo = new GameObject("SubNotice", typeof(RectTransform), typeof(Text), typeof(Outline));
            subNoticeGo.transform.SetParent(root.transform, false);
            RectTransform snRt = subNoticeGo.GetComponent<RectTransform>();
            snRt.anchorMin = new Vector2(0.5f, 0f);
            snRt.anchorMax = new Vector2(0.5f, 0f);
            snRt.sizeDelta = new Vector2(600, 60);
            snRt.anchoredPosition = new Vector2(0, 65);
            Text snTxt = subNoticeGo.GetComponent<Text>();
            snTxt.text = "특급 별똥별 택배 배송 중... 📦✨";
            snTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            snTxt.fontSize = 24;
            snTxt.fontStyle = FontStyle.Bold;
            snTxt.alignment = TextAnchor.MiddleCenter;
            snTxt.color = new Color(1f, 0.96f, 0.82f, 0f);
            Outline snOut = subNoticeGo.GetComponent<Outline>();
            snOut.effectColor = new Color(0.12f, 0.08f, 0.05f, 0.90f);
            snOut.effectDistance = new Vector2(1.5f, -1.5f);

            // Serialization Binding
            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("stripeContainer").objectReferenceValue = stripeContainerRt;

            SerializedProperty stripesProp = so.FindProperty("stripes");
            stripesProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
                stripesProp.GetArrayElementAtIndex(i).objectReferenceValue = stripeRts[i];

            SerializedProperty stripeImgsProp = so.FindProperty("stripeImages");
            stripeImgsProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
                stripeImgsProp.GetArrayElementAtIndex(i).objectReferenceValue = stripeImgs[i];

            so.FindProperty("starDotsContainer").objectReferenceValue = starDotsRt;
            SerializedProperty dotsProp = so.FindProperty("starDots");
            dotsProp.arraySize = dotCount;
            for (int d = 0; d < dotCount; d++)
                dotsProp.GetArrayElementAtIndex(d).objectReferenceValue = dotImgs[d];

            so.FindProperty("shootingStarContainer").objectReferenceValue = shootingStarContainerRt;

            // Star 1
            so.FindProperty("star1Root").objectReferenceValue = star1.root;
            so.FindProperty("star1Tail").objectReferenceValue = star1.tailRt;
            so.FindProperty("star1TailImg").objectReferenceValue = star1.tailImg;
            so.FindProperty("star1Glow").objectReferenceValue = star1.glowImg;
            so.FindProperty("star1ParcelText").objectReferenceValue = star1.parcelTxt;
            so.FindProperty("star1CanvasGroup").objectReferenceValue = star1.cg;

            // Star 2
            so.FindProperty("star2Root").objectReferenceValue = star2.root;
            so.FindProperty("star2Tail").objectReferenceValue = star2.tailRt;
            so.FindProperty("star2TailImg").objectReferenceValue = star2.tailImg;
            so.FindProperty("star2Glow").objectReferenceValue = star2.glowImg;
            so.FindProperty("star2ParcelText").objectReferenceValue = star2.parcelTxt;
            so.FindProperty("star2CanvasGroup").objectReferenceValue = star2.cg;

            // Star 3
            so.FindProperty("star3Root").objectReferenceValue = star3.root;
            so.FindProperty("star3Tail").objectReferenceValue = star3.tailRt;
            so.FindProperty("star3TailImg").objectReferenceValue = star3.tailImg;
            so.FindProperty("star3Glow").objectReferenceValue = star3.glowImg;
            so.FindProperty("star3ParcelText").objectReferenceValue = star3.parcelTxt;
            so.FindProperty("star3CanvasGroup").objectReferenceValue = star3.cg;

            so.FindProperty("subNoticeText").objectReferenceValue = snTxt;

            // 레거시 필드 호환
            so.FindProperty("meteorContainer").objectReferenceValue = shootingStarContainerRt;
            so.FindProperty("meteorRoot").objectReferenceValue = star2.root;
            so.FindProperty("leaderParcelRt").objectReferenceValue = star2.root;
            so.FindProperty("leaderParcelText").objectReferenceValue = star2.parcelTxt;
            so.FindProperty("follower1Rt").objectReferenceValue = star1.root;
            so.FindProperty("follower1Text").objectReferenceValue = star1.parcelTxt;
            so.FindProperty("follower2Rt").objectReferenceValue = star3.root;
            so.FindProperty("follower2Text").objectReferenceValue = star3.parcelTxt;
            so.FindProperty("parcelEmoji").objectReferenceValue = star2.parcelTxt;

            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/DiagonalMeteorTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private struct ShootingStarComponents
        {
            public RectTransform root;
            public CanvasGroup cg;
            public RectTransform tailRt;
            public Image tailImg;
            public Image glowImg;
            public Text parcelTxt;
        }

        private static ShootingStarComponents CreateSingleShootingStar(
            Transform parent, string name, Vector2 targetPos, float tailLength, float tailThickness, int fontSize,
            Sprite tailSprite, Sprite circleSprite)
        {
            ShootingStarComponents c = new ShootingStarComponents();

            // Root
            GameObject starGo = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            starGo.transform.SetParent(parent, false);
            c.root = starGo.GetComponent<RectTransform>();
            c.root.anchorMin = new Vector2(0.5f, 0.5f);
            c.root.anchorMax = new Vector2(0.5f, 0.5f);
            c.root.pivot = new Vector2(0.5f, 0.5f);
            c.root.anchoredPosition = new Vector2(targetPos.x - 2600f, targetPos.y); // 화면 밖
            c.root.sizeDelta = new Vector2(tailLength + 100f, 80f);
            starGo.SetActive(false);

            c.cg = starGo.GetComponent<CanvasGroup>();

            // 1. Tapered Tail (머리는 둥글고 밝으며 뒤로 갈수록 뾰족해지는 별똥별 꼬리)
            GameObject tailGo = new GameObject("Tail", typeof(RectTransform), typeof(Image));
            tailGo.transform.SetParent(starGo.transform, false);
            c.tailRt = tailGo.GetComponent<RectTransform>();
            c.tailRt.anchorMin = new Vector2(0.5f, 0.5f);
            c.tailRt.anchorMax = new Vector2(0.5f, 0.5f);
            c.tailRt.pivot = new Vector2(1.0f, 0.5f); // 우측 피벗: 머리 쪽에 고정되어 후방으로 길이 신축
            c.tailRt.anchoredPosition = new Vector2(10f, 0f);
            c.tailRt.sizeDelta = new Vector2(tailLength, tailThickness);
            c.tailRt.localScale = new Vector3(0.6f, 1f, 1f);

            c.tailImg = tailGo.GetComponent<Image>();
            c.tailImg.sprite = tailSprite;
            c.tailImg.color = Color.white;

            // 2. Head Glow (머리 주변 부드러운 별빛 글로우)
            GameObject glowGo = new GameObject("HeadGlow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(starGo.transform, false);
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.anchoredPosition = Vector2.zero;
            glowRt.sizeDelta = new Vector2(tailThickness * 2.4f, tailThickness * 2.4f);
            c.glowImg = glowGo.GetComponent<Image>();
            c.glowImg.sprite = circleSprite;
            c.glowImg.color = new Color(1f, 0.95f, 0.75f, 0.65f);

            // 3. Parcel Emoji Text (앙증맞은 40~50pt 택배 📦)
            GameObject parcelGo = new GameObject("ParcelText", typeof(RectTransform), typeof(Text), typeof(Outline));
            parcelGo.transform.SetParent(starGo.transform, false);
            RectTransform pRt = parcelGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.sizeDelta = new Vector2(fontSize * 1.8f, fontSize * 1.8f);

            c.parcelTxt = parcelGo.GetComponent<Text>();
            c.parcelTxt.text = "📦";
            c.parcelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            c.parcelTxt.fontSize = fontSize;
            c.parcelTxt.alignment = TextAnchor.MiddleCenter;
            c.parcelTxt.color = Color.white;

            Outline pOut = parcelGo.GetComponent<Outline>();
            pOut.effectColor = new Color(0.18f, 0.10f, 0.05f, 0.90f);
            pOut.effectDistance = new Vector2(1.5f, -1.5f);

            return c;
        }

        private static GameObject CreateBaseTransitionRoot(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            return root;
        }

        private static GameObject CreateBouncingBallPrefab(Sprite circleSprite)
        {
            GameObject root = CreateBaseTransitionRoot("BouncingBallTransition");
            var trans = root.AddComponent<BouncingBallTransition>();

            // 1. 크림 배경 패널 (#EFEAE0)
            GameObject bgGo = new GameObject("BgPanel", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(root.transform, false);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.937f, 0.918f, 0.878f, 0f);
            bgImg.raycastTarget = false;

            // 2. 바닥 라인 (#DCD4C5, y = -400, 두께 3px)
            GameObject floorGo = new GameObject("FloorLine", typeof(RectTransform), typeof(Image));
            floorGo.transform.SetParent(root.transform, false);
            RectTransform floorRt = floorGo.GetComponent<RectTransform>();
            floorRt.anchorMin = new Vector2(0f, 0.5f);
            floorRt.anchorMax = new Vector2(1f, 0.5f);
            floorRt.pivot = new Vector2(0.5f, 1f);
            floorRt.anchoredPosition = new Vector2(0f, -400f);
            floorRt.sizeDelta = new Vector2(0f, 3f);
            Image floorImg = floorGo.GetComponent<Image>();
            floorImg.color = new Color(0.863f, 0.831f, 0.773f, 0f);
            floorImg.raycastTarget = false;

            // 3. 점선 궤적 도트 풀 (6x6 원형, #C4B8A5)
            GameObject trailGo = new GameObject("TrailContainer", typeof(RectTransform));
            trailGo.transform.SetParent(root.transform, false);
            RectTransform trailRt = trailGo.GetComponent<RectTransform>();
            trailRt.anchorMin = Vector2.zero;
            trailRt.anchorMax = Vector2.one;
            trailRt.offsetMin = Vector2.zero;
            trailRt.offsetMax = Vector2.zero;

            const int DotPoolSize = 160;
            Image[] dots = new Image[DotPoolSize];
            Color trailColor = new Color(0.769f, 0.722f, 0.647f, 1f);
            for (int i = 0; i < DotPoolSize; i++)
            {
                GameObject dotGo = new GameObject($"Dot_{i:000}", typeof(RectTransform), typeof(Image));
                dotGo.transform.SetParent(trailRt, false);
                RectTransform dotRt = dotGo.GetComponent<RectTransform>();
                dotRt.anchorMin = new Vector2(0.5f, 0.5f);
                dotRt.anchorMax = new Vector2(0.5f, 0.5f);
                dotRt.pivot = new Vector2(0.5f, 0.5f);
                dotRt.sizeDelta = new Vector2(6f, 6f);
                dotRt.anchoredPosition = Vector2.zero;

                Image dotImg = dotGo.GetComponent<Image>();
                dotImg.sprite = circleSprite;
                dotImg.color = trailColor;
                dotImg.raycastTarget = false;

                dotGo.SetActive(false);
                dots[i] = dotImg;
            }

            // 4. 메인 공 (#E4462B, 100x100)
            GameObject ballGo = new GameObject("Ball", typeof(RectTransform), typeof(Image));
            ballGo.transform.SetParent(root.transform, false);
            RectTransform ballRt = ballGo.GetComponent<RectTransform>();
            ballRt.anchorMin = new Vector2(0.5f, 0.5f);
            ballRt.anchorMax = new Vector2(0.5f, 0.5f);
            ballRt.pivot = new Vector2(0.5f, 0.5f);
            ballRt.sizeDelta = new Vector2(100f, 100f);
            ballRt.anchoredPosition = new Vector2(-700f, 170f);
            ballRt.localScale = Vector3.zero;
            Image ballImg = ballGo.GetComponent<Image>();
            ballImg.sprite = circleSprite;
            ballImg.color = new Color(0.894f, 0.275f, 0.169f, 1f);
            ballImg.raycastTarget = false;

            // SerializedObject 바인딩
            SerializedObject so = new SerializedObject(trans);
            so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("container").objectReferenceValue = root.GetComponent<RectTransform>();
            so.FindProperty("bgPanel").objectReferenceValue = bgImg;
            so.FindProperty("floorLine").objectReferenceValue = floorImg;
            so.FindProperty("trailContainer").objectReferenceValue = trailRt;
            SerializedProperty dotsProp = so.FindProperty("trailDots");
            dotsProp.arraySize = DotPoolSize;
            for (int i = 0; i < DotPoolSize; i++)
            {
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
            }
            so.FindProperty("ballRect").objectReferenceValue = ballRt;
            so.FindProperty("ballImage").objectReferenceValue = ballImg;
            so.ApplyModifiedProperties();

            string prefabPath = $"{PrefabDir}/BouncingBallTransition.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ==================== SceneA & SceneB 자동 셋업 ====================

        public static void SetupSceneA(
            GameObject gearSmoothPf, GameObject gearTickPf, GameObject boxPf, GameObject truckPf, GameObject shutterPf, GameObject stampPf,
            GameObject forkliftPf, GameObject barcodePf, GameObject receiptRollPf, GameObject containerPf, GameObject fragileTapePf,
            GameObject mapPinPf, GameObject meteorPf, GameObject bouncingBallPf = null)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SceneA.unity", OpenSceneMode.Single);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("TransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            // EventSystem
            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null) esGo.AddComponent(inputSystemType);
                else esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // TransitionManager Singleton Object
            GameObject tmGo = GameObject.Find("[TransitionManager]");
            if (tmGo == null) tmGo = new GameObject("[TransitionManager]");
            var transMgr = tmGo.GetComponent<TransitionManager>();
            if (transMgr == null) transMgr = tmGo.AddComponent<TransitionManager>();
            transMgr.SetPrefabs(gearSmoothPf, gearTickPf, boxPf, truckPf, shutterPf, stampPf, forkliftPf, barcodePf, receiptRollPf, containerPf, fragileTapePf, mapPinPf, meteorPf, bouncingBallPf);
            transMgr.MinimumLoadingTime = 1.45f;

            // Demo Controller Object
            GameObject demoGo = GameObject.Find("TransitionDemoSystem");
            if (demoGo == null) demoGo = new GameObject("TransitionDemoSystem");
            var demoCtrl = demoGo.GetComponent<TransitionDemoController>();
            if (demoCtrl == null) demoCtrl = demoGo.AddComponent<TransitionDemoController>();

            // Setup 14 Unified Transition Buttons (5열 그리드: 5 + 5 + 4)
            SetupTransitionControlPanel(canvasGo, demoCtrl, "🏭 물류 센터 A구역 (Scene A)", "SceneA");

            EditorUtility.SetDirty(demoCtrl);
            EditorUtility.SetDirty(transMgr);
            demoCtrl.BindButtons();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        public static void SetupSceneB(
            GameObject gearSmoothPf, GameObject gearTickPf, GameObject boxPf, GameObject truckPf, GameObject shutterPf, GameObject stampPf,
            GameObject forkliftPf, GameObject barcodePf, GameObject receiptRollPf, GameObject containerPf, GameObject fragileTapePf,
            GameObject mapPinPf, GameObject meteorPf, GameObject bouncingBallPf = null)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SceneB.unity", OpenSceneMode.Single);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("SceneBCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null) esGo.AddComponent(inputSystemType);
                else esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // SceneB TransitionManager (안전용)
            GameObject tmGo = GameObject.Find("[TransitionManager]");
            if (tmGo == null) tmGo = new GameObject("[TransitionManager]");
            var transMgr = tmGo.GetComponent<TransitionManager>();
            if (transMgr == null) transMgr = tmGo.AddComponent<TransitionManager>();
            transMgr.SetPrefabs(gearSmoothPf, gearTickPf, boxPf, truckPf, shutterPf, stampPf, forkliftPf, barcodePf, receiptRollPf, containerPf, fragileTapePf, mapPinPf, meteorPf, bouncingBallPf);
            transMgr.MinimumLoadingTime = 1.45f;

            // SceneB Background Panel
            Transform oldBg = canvasGo.transform.Find("HubBackground");
            if (oldBg != null) Object.DestroyImmediate(oldBg.gameObject);

            GameObject bgGo = new GameObject("HubBackground", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            bgGo.transform.SetAsFirstSibling();
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.14f, 0.18f, 0.24f); // 세련된 물류 허브 딥 슬레이트

            // Hub Header Title (상단 대형 타이틀)
            GameObject headerGo = new GameObject("HeaderTitle", typeof(RectTransform), typeof(Text));
            headerGo.transform.SetParent(bgGo.transform, false);
            RectTransform headRt = headerGo.GetComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0.5f, 0.86f);
            headRt.anchorMax = new Vector2(0.5f, 0.86f);
            headRt.sizeDelta = new Vector2(1000, 70);
            Text headTxt = headerGo.GetComponent<Text>();
            headTxt.text = "📦 물류 허브 B구역 (Logistics Hub Zone B)";
            headTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headTxt.fontSize = 38;
            headTxt.fontStyle = FontStyle.Bold;
            headTxt.alignment = TextAnchor.MiddleCenter;
            headTxt.color = new Color(0.96f, 0.77f, 0.28f);

            // Sub Info
            GameObject subGo = new GameObject("SubInfo", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(bgGo.transform, false);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.80f);
            subRt.anchorMax = new Vector2(0.5f, 0.80f);
            subRt.sizeDelta = new Vector2(900, 36);
            Text subTxt = subGo.GetComponent<Text>();
            subTxt.text = "크로스 씬 전환 성공! 아래 트랜지션 버튼을 누르면 씬 A로 전환됩니다.";
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 20;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.85f, 0.88f, 0.92f);

            // Controller
            GameObject ctrlGo = GameObject.Find("SceneBSystem");
            if (ctrlGo == null) ctrlGo = new GameObject("SceneBSystem");
            var bCtrl = ctrlGo.GetComponent<SceneBController>();
            if (bCtrl == null) bCtrl = ctrlGo.AddComponent<SceneBController>();

            // Setup 14 Unified Transition Buttons (5열 그리드: 5 + 5 + 4)
            SetupTransitionControlPanel(canvasGo, bCtrl, "📦 물류 허브 B구역 (Scene B)", "SceneB");

            EditorUtility.SetDirty(bCtrl);
            EditorUtility.SetDirty(transMgr);
            bCtrl.BindButtons();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            // 다시 SceneA로 복귀 오픈 및 저장
            EditorSceneManager.OpenScene("Assets/Scenes/SceneA.unity", OpenSceneMode.Single);
        }

        private static void SetupTransitionControlPanel(
            GameObject canvasGo,
            TransitionDemoController controller,
            string currentSceneTitle,
            string currentSceneName)
        {
            Transform oldPanel = canvasGo.transform.Find("DemoControlPanel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
            Transform oldReturnPanel = canvasGo.transform.Find("ReturnPanel");
            if (oldReturnPanel != null) Object.DestroyImmediate(oldReturnPanel.gameObject);
            Transform oldNavBtn = canvasGo.transform.Find("Btn_GoToButtonScene");
            if (oldNavBtn != null) Object.DestroyImmediate(oldNavBtn.gameObject);

            // 하단 컨트롤 패널 (1760 x 300)
            GameObject panelGo = new GameObject("DemoControlPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.02f);
            panelRt.anchorMax = new Vector2(0.5f, 0.02f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(1760, 300);
            Image panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0.10f, 0.12f, 0.16f, 0.94f);

            // 1. 헤더 & 상태 안내 텍스트
            GameObject headGo = new GameObject("HeaderText", typeof(RectTransform), typeof(Text));
            headGo.transform.SetParent(panelGo.transform, false);
            RectTransform headRt = headGo.GetComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0.02f, 0.84f);
            headRt.anchorMax = new Vector2(0.98f, 0.98f);
            headRt.offsetMin = Vector2.zero;
            headRt.offsetMax = Vector2.zero;
            Text headTxt = headGo.GetComponent<Text>();
            headTxt.text = currentSceneTitle;
            headTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headTxt.fontSize = 26;
            headTxt.fontStyle = FontStyle.Bold;
            headTxt.alignment = TextAnchor.MiddleCenter;
            headTxt.color = new Color(0.98f, 0.78f, 0.28f);

            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(panelGo.transform, false);
            RectTransform statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.02f, 0.71f);
            statusRt.anchorMax = new Vector2(0.98f, 0.84f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;
            Text statusTxt = statusGo.GetComponent<Text>();
            string targetScene = (currentSceneName == "SceneA") ? "SceneB" : "SceneA";
            statusTxt.text = $"★ 원하는 트랜지션 버튼을 누르면 {targetScene}로 크로스 씬 전환됩니다.";
            statusTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusTxt.fontSize = 20;
            statusTxt.alignment = TextAnchor.MiddleCenter;
            statusTxt.color = new Color(0.85f, 0.88f, 0.92f);

            // 2. 15개 버튼 그리드 컨테이너 (GridLayoutGroup - 5열 배치: 5 + 5 + 5)
            GameObject gridGo = new GameObject("ButtonGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(panelGo.transform, false);
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.015f, 0.04f);
            gridRt.anchorMax = new Vector2(0.985f, 0.69f);
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;

            GridLayoutGroup grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(325, 54);
            grid.spacing = new Vector2(16, 10);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            // 15개 버튼 생성 (5열: 5 + 5 + 5)
            Button b1 = CreateDemoButton(gridGo.transform, "01. ⚙️ 톱니(회전)", new Color(0.96f, 0.58f, 0.23f));
            Button b2 = CreateDemoButton(gridGo.transform, "02. ⚙️ 톱니(철컥)", new Color(0.88f, 0.68f, 0.26f));
            Button b3 = CreateDemoButton(gridGo.transform, "03. 📦 택배박스", new Color(0.72f, 0.49f, 0.28f));
            Button b4 = CreateDemoButton(gridGo.transform, "04. 🚚 배송트럭", new Color(0.91f, 0.35f, 0.24f));
            Button b5 = CreateDemoButton(gridGo.transform, "05. 🪟 롤 셔터", new Color(0.33f, 0.45f, 0.55f));

            Button b6 = CreateDemoButton(gridGo.transform, "06. 🔖 송장봉투", new Color(0.85f, 0.25f, 0.28f));
            Button b7 = CreateDemoButton(gridGo.transform, "07. 🚜 지게차", new Color(0.96f, 0.72f, 0.15f));
            Button b8 = CreateDemoButton(gridGo.transform, "08. 🏷️ 바코드", new Color(0.85f, 0.22f, 0.25f));
            Button b9 = CreateDemoButton(gridGo.transform, "09. 🧾 영수증", new Color(0.25f, 0.65f, 0.85f));
            Button b10 = CreateDemoButton(gridGo.transform, "10. 🚪 컨테이너", new Color(0.22f, 0.48f, 0.78f));

            Button b11 = CreateDemoButton(gridGo.transform, "11. ⚠️ 주의테이프", new Color(0.85f, 0.55f, 0.15f));
            Button b12 = CreateDemoButton(gridGo.transform, "12. 🗺️ 월드맵", new Color(0.30f, 0.65f, 0.40f));
            Button b13 = CreateDemoButton(gridGo.transform, "13. 🌠 별똥별메테오", new Color(0.95f, 0.48f, 0.24f));
            Button b14 = CreateDemoButton(gridGo.transform, "14. 🔴 바운싱 볼", new Color(0.894f, 0.275f, 0.169f));
            Button b15 = CreateDemoButton(gridGo.transform, "15. 🎲 전체 랜덤", new Color(0.55f, 0.35f, 0.75f));

            // SerializedObject 바인딩
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("headerText").objectReferenceValue = headTxt;
            so.FindProperty("statusText").objectReferenceValue = statusTxt;

            so.FindProperty("btn01GearCircleSmooth").objectReferenceValue = b1;
            so.FindProperty("btn02GearCircleTick").objectReferenceValue = b2;
            so.FindProperty("btn03BoxFlap").objectReferenceValue = b3;
            so.FindProperty("btn04TruckWipe").objectReferenceValue = b4;
            so.FindProperty("btn05ConveyorShutter").objectReferenceValue = b5;
            so.FindProperty("btn06StampInvoice").objectReferenceValue = b6;
            so.FindProperty("btn07Forklift").objectReferenceValue = b7;
            so.FindProperty("btn08BarcodeScan").objectReferenceValue = b8;
            so.FindProperty("btn09ReceiptRoll").objectReferenceValue = b9;
            so.FindProperty("btn10ContainerDoor").objectReferenceValue = b10;
            so.FindProperty("btn11FragileTape").objectReferenceValue = b11;
            so.FindProperty("btn12WorldMapPin").objectReferenceValue = b12;
            so.FindProperty("btn13DiagonalMeteor").objectReferenceValue = b13;
            so.FindProperty("btn14BouncingBall").objectReferenceValue = b14;
            so.FindProperty("btn15RandomAll").objectReferenceValue = b15;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            controller.BindButtons();
        }

        private static Button CreateDemoButton(Transform parent, string label, Color color)
        {
            GameObject btnGo = new GameObject("Btn_" + label.Replace("\n", " ").Replace(" ", "_"), typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            Image img = btnGo.GetComponent<Image>();
            img.color = color;
            Button btn = btnGo.GetComponent<Button>();

            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(6, 2);
            trt.offsetMax = new Vector2(-6, -2);
            Text txt = txtGo.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }
    }
}
