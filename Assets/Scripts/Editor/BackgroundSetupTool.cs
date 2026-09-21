using System.Collections.Generic;
using System.IO;
using System.Text;
using BlueComplex.UI.Background;
using BlueComplex.UI.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 3D 공간 배경(레이어 배치 + 라이팅 + 시계/램프 배선)을 한 번에 만든다.
    /// 텍스처 임포트 설정, 레이어 머티리얼 에셋, 씬의 Background Rig(레이어/광원/시계), 메인 카메라 클리어 설정,
    /// StageBootstrapper 배선, CRT 블룸 임계값까지 처리한다. 이미 있는 항목은 재사용하므로 여러 번 실행해도 안전하다
    /// (레이어는 매번 새로 배치하고, 광원 값과 머티리얼 수치는 처음 만들 때만 넣어 손으로 조정한 값을 지키지 않는다).
    /// </summary>
    public static class BackgroundSetupTool
    {
        private const string ArtFolder = "Assets/Assets";
        private const string MaterialFolder = "Assets/Settings/Background";
        private const string ShaderName = "BlueComplex/Background/Layer";
        private const string CrtMaterialPath = "Assets/Settings/CRT/CRT_PostProcess.mat";

        private const string RigName = "Background Rig";
        private const string LayersName = "Layers";
        private const string LampLightName = "Lamp Point Light";
        private const string WindowLightName = "Window Spot Light";
        private const string ClockControllerName = "Clock Controller";

        private const float PixelsPerUnit = 100f;
        private const int MaxTextureSize = 4096;
        private const int BaseRenderQueue = 3000;

        /// <summary>CRT 블룸 시작 밝기(선형). 셰이더 기본 0.45보다 높여 전구/창문 하이라이트에만 걸리게 한다.</summary>
        private const float CrtBloomThreshold = 0.6f;

        // 시계 손의 회전축(캔버스 픽셀, 좌상단 원점). 두 손이 만나는 허브의 중심.
        private static readonly Vector2 ClockPivotPixel = new Vector2(1605f, 240f);
        // 전구의 밝은 중심(Lamp.png에서 가장 밝은 픽셀).
        private static readonly Vector2 BulbPixel = new Vector2(1610f, 880f);

        private sealed class LayerSpec
        {
            public string Name;
            public string Texture;
            public float Distance;      // 카메라 정지 위치에서의 거리(유닛)
            public float LightGain;     // 0 = 조명 영향 없음(아트 그대로)
            public float AlphaPower;    // 선형 블렌딩 보정(1 = 보정 없음)
            public float SurfaceGlow;   // 조명이 비추는 만큼 조명 색으로 더하는 양(선형). 거의 검정인 벽 전용
            public bool ClockHandPivot; // true면 ClockPivotPixel을 회전축으로 하는 피벗을 만든다
        }

        // 카메라에서 먼 것부터. 그룹 간격은 1유닛, 시계 손은 시계 면 바로 앞(0.05)에 붙인다.
        private static readonly LayerSpec[] Layers =
        {
            // 벽은 거의 검정이라 "아트색 × 조명"으론 밝아지지 않는다 — 빛이 닿는 만큼만 조명 색을 직접 더한다(할 일: 벽면 빛 밝게).
            new LayerSpec { Name = "BG",           Texture = "BG",          Distance = 15.00f, LightGain = 1f, AlphaPower = 1.8f, SurfaceGlow = 0.08f },
            new LayerSpec { Name = "Window",       Texture = "Window",      Distance = 14.00f, LightGain = 1f, AlphaPower = 1.8f },
            new LayerSpec { Name = "Clock",        Texture = "Clock",       Distance = 13.00f, LightGain = 1f, AlphaPower = 1.8f },
            new LayerSpec { Name = "Min",          Texture = "Min",         Distance = 12.95f, LightGain = 1f, AlphaPower = 1.8f, ClockHandPivot = true },
            new LayerSpec { Name = "Hour",         Texture = "Hour",        Distance = 12.90f, LightGain = 1f, AlphaPower = 1.8f, ClockHandPivot = true },
            // 램프는 스스로 빛나는 아트(글로우 포함)라 조명으로 또 밝히면 이중 노출이 된다.
            new LayerSpec { Name = "Lamp",         Texture = "Lamp",        Distance = 12.00f, LightGain = 0f, AlphaPower = 1.8f },
            new LayerSpec { Name = "Things",       Texture = "Things",      Distance = 11.00f, LightGain = 1f, AlphaPower = 1.8f },
            new LayerSpec { Name = "LeftPerson",   Texture = "LeftPerson",  Distance = 10.00f, LightGain = 1f, AlphaPower = 1.8f },
            new LayerSpec { Name = "RightPerson",  Texture = "RightPerson", Distance = 10.00f, LightGain = 1f, AlphaPower = 1.8f },
            // Shadow.png는 흰 마스크가 아니라 "검정 + 알파" 비네트다 — 모든 레이어 위에 알파 블렌드로 얹으면 곱하기와 같다.
            // 완성본 = 모든 레이어 위에 이 오버레이를 얹은 것(실측 평균 오차 0.0008). 알파 보정은 하지 않는다.
            new LayerSpec { Name = "ShadowOverlay", Texture = "Shadow",     Distance =  9.50f, LightGain = 0f, AlphaPower = 1f },
        };

        private const float LampLightDistance = 11.4f;

        [MenuItem("BlueComplex/Background/Setup Background")]
        public static void SetupAll()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("배경 셋업 실패",
                    $"셰이더 '{ShaderName}' 를 찾을 수 없습니다.\nAssets/Shaders/BackgroundLayer.shader가 있고 컴파일 에러가 없는지 확인하세요.",
                    "확인");
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                EditorUtility.DisplayDialog("배경 셋업 실패", "MainCamera 태그가 붙은 카메라를 씬에서 찾을 수 없습니다.", "확인");
                return;
            }

            var log = new StringBuilder();

            ConfigureTextures(log);
            var materials = EnsureMaterials(shader);
            var rig = BuildRig(camera, materials, out var clockController, log);
            var lampDriver = EnsureLights(rig, log);
            ConfigureCamera(camera, log);
            DisableConflictingSceneObjects(log);
            WireBootstrapper(clockController, lampDriver, log);
            EnsureCrtBloomThreshold(log);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = log.ToString() + "\n씬이 수정되었습니다 — Ctrl+S로 저장하세요.";
            EditorUtility.DisplayDialog("배경 셋업 완료", msg, "확인");
            Debug.Log("[BackgroundSetupTool]\n" + msg);
        }

        // ── 텍스처 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 도트 아트 설정: Sprite(PPU 통일) / Point / 압축 없음 / 밉맵 없음 / Clamp.
        /// 레이어 PNG는 3200×1800인데 임포터 기본 최대 크기가 2048이라 그대로 두면 비정수 배율로 축소되어 도트가 번진다 — 4096으로 올린다.
        /// (원본은 320×180 도트를 정확히 10배 확대한 것이라 Point 샘플링이면 어떤 해상도에서도 도트 경계가 유지된다.)
        /// </summary>
        private static void ConfigureTextures(StringBuilder log)
        {
            var changed = 0;
            foreach (var path in ArtTexturePaths())
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                var dirty = importer.textureType != TextureImporterType.Sprite
                            || importer.spriteImportMode != SpriteImportMode.Single
                            || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
                            || importer.filterMode != FilterMode.Point
                            || importer.textureCompression != TextureImporterCompression.Uncompressed
                            || importer.mipmapEnabled
                            || importer.wrapMode != TextureWrapMode.Clamp
                            || !importer.alphaIsTransparency
                            || importer.maxTextureSize != MaxTextureSize
                            || settings.spriteMeshType != SpriteMeshType.FullRect;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.maxTextureSize = MaxTextureSize;
                importer.npotScale = TextureImporterNPOTScale.None;

                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                // 플랫폼별 오버라이드가 켜져 있으면 기본값이 무시되므로 같이 맞춘다.
                foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                {
                    var platformSettings = importer.GetPlatformTextureSettings(platform);
                    if (!platformSettings.overridden) continue;

                    platformSettings.maxTextureSize = MaxTextureSize;
                    platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(platformSettings);
                    dirty = true;
                }

                if (!dirty) continue;

                importer.SaveAndReimport();
                changed++;
            }

            log.AppendLine($"텍스처: {changed}개 임포트 설정 갱신 (Sprite / PPU {PixelsPerUnit} / Point / 압축 없음 / 최대 {MaxTextureSize})");
        }

        private static IEnumerable<string> ArtTexturePaths()
        {
            foreach (var spec in Layers) yield return $"{ArtFolder}/{spec.Texture}.png";
            yield return $"{ArtFolder}/GreenRoomAddPeopleFIx.png";
        }

        // ── 머티리얼 ──────────────────────────────────────────────────────────────

        private static Dictionary<string, Material> EnsureMaterials(Shader shader)
        {
            EnsureFolder(MaterialFolder);

            var result = new Dictionary<string, Material>();
            for (var i = 0; i < Layers.Length; i++)
            {
                var spec = Layers[i];
                var path = $"{MaterialFolder}/BG_{spec.Name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                var created = material == null;
                if (created)
                {
                    material = new Material(shader) { name = $"BG_{spec.Name}" };
                    AssetDatabase.CreateAsset(material, path);
                }

                material.shader = shader;
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/{spec.Texture}.png"));
                // 같은 큐 안에서도 항상 이 순서로 그려지도록 큐를 레이어 순서대로 벌린다(먼 것부터).
                material.renderQueue = BaseRenderQueue + i;

                if (created)
                {
                    material.SetFloat("_AlphaPower", spec.AlphaPower);
                    material.SetFloat("_LightGain", spec.LightGain);
                    material.SetFloat("_SurfaceGlow", spec.SurfaceGlow);
                }

                EditorUtility.SetDirty(material);
                result[spec.Name] = material;
            }

            return result;
        }

        // ── 씬: 레이어 리그 ───────────────────────────────────────────────────────

        private static BackgroundLayerRig BuildRig(Camera camera, Dictionary<string, Material> materials,
                                                  out ClockController clockController, StringBuilder log)
        {
            var rigObject = GameObject.Find(RigName);
            var newRig = rigObject == null;
            if (newRig)
            {
                rigObject = new GameObject(RigName);
                Undo.RegisterCreatedObjectUndo(rigObject, "Create Background Rig");
                // 리그 = 카메라 정지 자세. 처음 만들 때만 카메라에 맞추고, 이후엔 손으로 옮긴 위치를 존중한다.
                rigObject.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            }

            var rig = rigObject.GetComponent<BackgroundLayerRig>();
            if (rig == null) rig = rigObject.AddComponent<BackgroundLayerRig>();

            // 레이어는 매번 새로 만든다(자식 광원/컨트롤러는 건드리지 않는다).
            var oldLayers = rigObject.transform.Find(LayersName);
            if (oldLayers != null) Object.DestroyImmediate(oldLayers.gameObject);
            var layersRoot = new GameObject(LayersName).transform;
            layersRoot.SetParent(rigObject.transform, false);

            var pivotOffset = new Vector2(ClockPivotPixel.x - 1600f, 900f - ClockPivotPixel.y) / PixelsPerUnit;
            var canvasUnits = new Vector2(3200f, 1800f) / PixelsPerUnit;

            var rigLayers = new List<BackgroundLayerRig.Layer>();
            Transform minutePivot = null;
            Transform hourPivot = null;

            for (var i = 0; i < Layers.Length; i++)
            {
                var spec = Layers[i];
                var root = new GameObject(spec.Name).transform;
                root.SetParent(layersRoot, false);

                Transform quadParent = root;
                if (spec.ClockHandPivot)
                {
                    var pivot = new GameObject($"{spec.Name} Pivot").transform;
                    pivot.SetParent(root, false);
                    pivot.localPosition = new Vector3(pivotOffset.x, pivotOffset.y, 0f);
                    quadParent = pivot;
                    if (spec.Name == "Min") minutePivot = pivot;
                    else hourPivot = pivot;
                }

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Quad";
                var collider = quad.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
                quad.transform.SetParent(quadParent, false);
                // 피벗 아래에서는 캔버스 중심이 피벗과 어긋난 만큼 되돌려 놓아야 손이 아트 위치에 그대로 보인다.
                quad.transform.localPosition = spec.ClockHandPivot
                    ? new Vector3(-pivotOffset.x, -pivotOffset.y, 0f)
                    : Vector3.zero;
                quad.transform.localScale = new Vector3(canvasUnits.x, canvasUnits.y, 1f);

                var meshRenderer = quad.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = materials[spec.Name];
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.sortingOrder = i;

                rigLayers.Add(new BackgroundLayerRig.Layer { Root = root, Distance = spec.Distance });
            }

            rig.SetLayers(camera, rigLayers.ToArray());
            rig.Apply();
            EditorUtility.SetDirty(rig);

            clockController = EnsureClockController(rigObject.transform, minutePivot, hourPivot);

            log.AppendLine($"레이어 리그: '{RigName}' {(newRig ? "새로 생성" : "재사용")} — 레이어 {Layers.Length}개, " +
                           $"거리 {Layers[Layers.Length - 1].Distance:0.##}~{Layers[0].Distance:0.##}, " +
                           $"정지 자세 기준 풀프레임 거리 {rig.FullFrameDistance:0.###}");
            return rig;
        }

        private static ClockController EnsureClockController(Transform rig, Transform minutePivot, Transform hourPivot)
        {
            var existing = rig.Find(ClockControllerName);
            var go = existing != null ? existing.gameObject : new GameObject(ClockControllerName);
            if (existing == null)
            {
                go.transform.SetParent(rig, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Clock Controller");
            }

            var controller = go.GetComponent<ClockController>();
            if (controller == null) controller = go.AddComponent<ClockController>();

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_minuteHand").objectReferenceValue = minutePivot;
            serialized.FindProperty("_hourHand").objectReferenceValue = hourPivot;

            var bootstrapper = Object.FindFirstObjectByType<StageBootstrapper>();
            // SessionBoundView의 직렬화 필드 — 새 세션이 시작될 때 시계를 시작 시각으로 되돌리는 데 쓴다.
            var bootstrapperProp = serialized.FindProperty("_bootstrapper");
            if (bootstrapperProp != null && bootstrapper != null) bootstrapperProp.objectReferenceValue = bootstrapper;
            serialized.ApplyModifiedProperties();

            return controller;
        }

        // ── 씬: 광원 ──────────────────────────────────────────────────────────────

        private static LampLightDriver EnsureLights(BackgroundLayerRig rig, StringBuilder log)
        {
            var lampDriver = EnsureLampLight(rig, log);
            EnsureWindowLight(rig, log);
            return lampDriver;
        }

        /// <summary>천장 램프의 Point Light. 위치는 전구 픽셀과 같은 시선 위(카메라에서 보면 전구와 겹침)이고,
        /// 램프 면보다 0.6유닛 카메라 쪽에 둔다 — 램프 면 위에서 거리가 0에 가까워 감쇠가 폭주하는 것을 막는다.</summary>
        private static LampLightDriver EnsureLampLight(BackgroundLayerRig rig, StringBuilder log)
        {
            var (go, created) = FindOrCreateChild(rig.transform, LampLightName);
            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            EnsureAdditionalLightData(go);

            if (created)
            {
                go.transform.position = rig.CanvasPixelToWorld(BulbPixel, LampLightDistance);
                light.type = LightType.Point;
                // 아트의 청록빛(전구 글로우 ≈ #4AFFD0).
                light.color = new Color32(0x4A, 0xFF, 0xD0, 0xFF);
                light.intensity = 2.0f;
                // 테이블과 인물 상체까지만: 전구 아래 약 4.5유닛(캔버스 ~700px) 이내.
                light.range = 5.0f;
                light.shadows = LightShadows.None;
            }

            var driver = go.GetComponent<LampLightDriver>();
            if (driver == null) driver = go.AddComponent<LampLightDriver>();
            var serialized = new SerializedObject(driver);
            serialized.FindProperty("_light").objectReferenceValue = light;
            serialized.ApplyModifiedProperties();

            log.AppendLine($"  {LampLightName}: {(created ? "생성" : "재사용")} — {light.type}, color #{ColorUtility.ToHtmlStringRGB(light.color)}, " +
                           $"intensity {light.intensity:0.##}, range {light.range:0.##}, pos {go.transform.position}");
            return driver;
        }

        /// <summary>창문 빛. 좌상단 앞쪽에서 창문 왼쪽 면을 향하는 Spot Light — 왼쪽 벽이 가장 밝고 오른쪽으로 갈수록 옅어진다.</summary>
        private static void EnsureWindowLight(BackgroundLayerRig rig, StringBuilder log)
        {
            var (go, created) = FindOrCreateChild(rig.transform, WindowLightName);
            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            EnsureAdditionalLightData(go);

            if (created)
            {
                var position = rig.CanvasPixelToWorld(new Vector2(-1200f, -900f), 8f);
                var target = rig.CanvasPixelToWorld(new Vector2(1250f, 780f), 14f);
                go.transform.position = position;
                go.transform.rotation = Quaternion.LookRotation(target - position, rig.transform.up);

                light.type = LightType.Spot;
                // 창문 유리(녹색)를 통과한 빛: 청록보다 옅고 흰 기가 도는 민트.
                light.color = new Color(0.60f, 1.0f, 0.85f);
                light.intensity = 14f;
                light.range = 40f;
                light.spotAngle = 80f;
                light.innerSpotAngle = 30f;
                light.shadows = LightShadows.None;
            }

            log.AppendLine($"  {WindowLightName}: {(created ? "생성" : "재사용")} — {light.type}, color #{ColorUtility.ToHtmlStringRGB(light.color)}, " +
                           $"intensity {light.intensity:0.##}, range {light.range:0.##}, angle {light.spotAngle:0.#}/inner {light.innerSpotAngle:0.#}, " +
                           $"pos {go.transform.position}, euler {go.transform.eulerAngles}");
        }

        private static void EnsureAdditionalLightData(GameObject go)
        {
            if (go.GetComponent<UniversalAdditionalLightData>() == null)
                go.AddComponent<UniversalAdditionalLightData>();
        }

        private static (GameObject go, bool created) FindOrCreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return (child.gameObject, false);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return (go, true);
        }

        // ── 씬: 카메라 / 충돌하는 기존 오브젝트 ───────────────────────────────────

        /// <summary>스카이박스 대신 검정으로 지운다 — 카메라가 움직여 레이어 가장자리가 드러날 때 파란 하늘이 보이면 안 된다.
        /// (Perspective/FOV는 그대로. 레이어 스케일이 이 카메라의 FOV를 기준으로 계산돼 있다.)</summary>
        private static void ConfigureCamera(Camera camera, StringBuilder log)
        {
            Undo.RecordObject(camera, "Configure Background Camera");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            EditorUtility.SetDirty(camera);
            log.AppendLine($"메인 카메라: Perspective FOV {camera.fieldOfView:0.#}, Clear Flags = Solid Color(검정), 위치 {camera.transform.position} (이동 없음)");
        }

        /// <summary>기존 Directional Light(따뜻한 흰색, 강도 1)는 배경을 그대로 하얗게 날리고, CRT 테스트 패턴 쿼드는 배경 앞을 가린다.</summary>
        private static void DisableConflictingSceneObjects(StringBuilder log)
        {
            foreach (var name in new[] { "Directional Light", "CRT Test Pattern" })
            {
                var go = GameObject.Find(name);
                if (go == null || !go.activeSelf) continue;

                Undo.RecordObject(go, $"Disable {name}");
                go.SetActive(false);
                log.AppendLine($"비활성화: '{name}' (배경과 충돌)");
            }
        }

        private static void WireBootstrapper(ClockController clockController, LampLightDriver lampDriver, StringBuilder log)
        {
            var bootstrapper = Object.FindFirstObjectByType<StageBootstrapper>();
            if (bootstrapper == null)
            {
                log.AppendLine("경고: 씬에 StageBootstrapper가 없어 램프 심박수 연동을 배선하지 못했습니다.");
                return;
            }

            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("_lampLightDriver").objectReferenceValue = lampDriver;
            serialized.ApplyModifiedProperties();
            log.AppendLine("StageBootstrapper._lampLightDriver → Lamp Point Light");
        }

        // ── CRT ───────────────────────────────────────────────────────────────────

        private static void EnsureCrtBloomThreshold(StringBuilder log)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(CrtMaterialPath);
            if (material == null)
            {
                log.AppendLine($"경고: CRT 머티리얼({CrtMaterialPath})을 못 찾아 블룸 임계값을 건드리지 못했습니다.");
                return;
            }

            if (!material.HasProperty("_BloomThreshold"))
            {
                log.AppendLine("경고: CRT 셰이더에 _BloomThreshold가 없습니다(컴파일 에러 확인).");
                return;
            }

            material.SetFloat("_BloomThreshold", CrtBloomThreshold);
            EditorUtility.SetDirty(material);
            log.AppendLine($"CRT 블룸: 이 배경 전용 URP Bloom은 추가하지 않음(메인 카메라 후처리 OFF). CRT _BloomThreshold = {CrtBloomThreshold}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
