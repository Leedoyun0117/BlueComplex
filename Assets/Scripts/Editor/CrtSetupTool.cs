using System.Collections.Generic;
using System.IO;
using System.Text;
using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// CRT 포스트프로세싱 배선(머티리얼/프리셋 에셋, 렌더러의 Full Screen Pass Renderer Feature,
    /// 씬의 CRT Controller / StageBootstrapper 오브젝트)을 한 번에 자동화한다.
    /// 이미 존재하는 항목은 재사용하므로 여러 번 실행해도 안전하다(멱등).
    /// </summary>
    public static class CrtSetupTool
    {
        private const string ShaderName = "BlueComplex/CRT/PostProcess";
        private const string SettingsFolder = "Assets/Settings/CRT";
        private const string MaterialPath = SettingsFolder + "/CRT_PostProcess.mat";
        private const string PresetPath = SettingsFolder + "/CrtEffectPreset.asset";

        private static readonly string[] RendererPaths =
        {
            "Assets/Settings/PC_Renderer.asset",
            "Assets/Settings/Mobile_Renderer.asset",
        };

        /// <summary>
        /// PC_Renderer.asset의 CRT passMaterial이 빈 채로 커밋되면(사고 기록: 2026-09-23) 패스가 조용히
        /// 아무 일도 안 해서 CRT 연출이 통째로 빠진 걸 한동안 아무도 못 알아챌 수 있다. 에디터를 열 때와
        /// Play에 들어갈 때마다 검사해서 콘솔에 경고를 남긴다 — 빌드나 Play 자체를 막지는 않는다.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterPassMaterialGuard()
        {
            CheckPassMaterialsAssigned();
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode)
                    CheckPassMaterialsAssigned();
            };
        }

        private static void CheckPassMaterialsAssigned()
        {
            foreach (var path in RendererPaths)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (rendererData == null) continue;

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is FullScreenPassRendererFeature fullScreen && feature.isActive && fullScreen.passMaterial == null)
                    {
                        Debug.LogWarning($"[CrtSetupTool] {path}의 Full Screen Pass Renderer Feature '{feature.name}'이(가) " +
                                          "활성 상태인데 passMaterial이 비어 있습니다. 이 상태로 두면 그 패스는 아무 효과도 " +
                                          "내지 않고(의도한 것이면 무시해도 됨), 반대로 실수로 material을 지운 채 커밋하면 " +
                                          "나중에 다시 채울 때 원인 파악이 어려웠던 전례가 있습니다 — BlueComplex/CRT/Diagnose로 확인하세요.");
                    }
                }
            }
        }

        [MenuItem("BlueComplex/CRT/Setup CRT Effect")]
        public static void SetupAll()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("CRT 셋업 실패",
                    $"셰이더 '{ShaderName}' 를 찾을 수 없습니다.\nAssets/Shaders/CrtEffect.shader가 프로젝트에 있고 컴파일 에러가 없는지 확인하세요.",
                    "확인");
                return;
            }

            var material = EnsureMaterial(shader);
            var preset = EnsurePreset();

            var addedTo = new List<string>();
            var skippedTo = new List<string>();
            foreach (var path in RendererPaths)
            {
                switch (EnsureRendererFeature(path, material))
                {
                    case RendererFeatureResult.Added: addedTo.Add(path); break;
                    case RendererFeatureResult.AlreadyPresent: skippedTo.Add(path); break;
                    case RendererFeatureResult.RendererNotFound:
                        Debug.LogWarning($"[CrtSetupTool] 렌더러 에셋을 찾을 수 없습니다: {path}");
                        break;
                }
            }

            var driver = EnsureCrtController(material, preset);
            EnsureBootstrapper(driver);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = $"머티리얼: {MaterialPath}\n프리셋: {PresetPath}\n" +
                      (addedTo.Count > 0 ? $"Renderer Feature 추가됨: {string.Join(", ", addedTo)}\n" : "") +
                      (skippedTo.Count > 0 ? $"Renderer Feature 이미 있음(건너뜀): {string.Join(", ", skippedTo)}\n" : "") +
                      "씬: CRT Controller / StageBootstrapper 준비 완료.\n\n" +
                      "씬이 수정되었습니다 — Ctrl+S로 저장하세요.";
            EditorUtility.DisplayDialog("CRT 셋업 완료", msg, "확인");
            Debug.Log("[CrtSetupTool] " + msg.Replace("\n", " / "));
        }

        /// <summary>
        /// 셰이더 컴파일 상태, Renderer Feature 리스트, 실제 사용 중인 파이프라인 에셋을 한 번에 찍어본다.
        /// "설정은 다 맞는데 화면엔 아무 효과도 안 보인다"를 진단할 때 쓴다.
        /// </summary>
        [MenuItem("BlueComplex/CRT/Diagnose")]
        public static void Diagnose()
        {
            var sb = new StringBuilder();

            var shader = Shader.Find(ShaderName);
            sb.AppendLine(shader == null
                ? $"셰이더: '{ShaderName}' 를 Shader.Find로 못 찾음 (컴파일 실패 가능성)"
                : $"셰이더: 찾음 ({shader.name})");

            if (shader != null)
            {
                bool hasError = ShaderUtil.ShaderHasError(shader);
                sb.AppendLine($"셰이더 컴파일 에러: {(hasError ? "**있음**" : "없음")}");
                if (hasError)
                {
                    foreach (var m in ShaderUtil.GetShaderMessages(shader))
                        sb.AppendLine($"  [{m.severity}] {m.message}  ({m.file}:{m.line})");
                }
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            sb.AppendLine(material == null
                ? $"머티리얼: {MaterialPath} 를 못 찾음"
                : $"머티리얼: 찾음, shader={(material.shader != null ? material.shader.name : "NULL")}, passCount={material.passCount}");

            foreach (var path in RendererPaths)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (rendererData == null)
                {
                    sb.AppendLine($"{path}: 에셋을 못 찾음");
                    continue;
                }

                sb.AppendLine($"{path}: rendererFeatures 개수={rendererData.rendererFeatures.Count}");
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature == null)
                    {
                        sb.AppendLine("  - NULL 항목 (깨진 참조)");
                        continue;
                    }

                    sb.AppendLine($"  - {feature.name} ({feature.GetType().Name}), isActive={feature.isActive}");
                    if (feature is FullScreenPassRendererFeature fullScreen)
                    {
                        sb.AppendLine($"      injectionPoint={fullScreen.injectionPoint}, fetchColorBuffer={fullScreen.fetchColorBuffer}, " +
                                      $"passIndex={fullScreen.passIndex}, passMaterial={(fullScreen.passMaterial != null ? fullScreen.passMaterial.name : "NULL")}");
                    }
                }
            }

            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            sb.AppendLine($"GraphicsSettings.currentRenderPipeline = {(currentPipeline != null ? currentPipeline.name : "NULL (SRP 비활성 상태!)")}");

            var qualityOverride = QualitySettings.renderPipeline;
            sb.AppendLine($"현재 품질 레벨({QualitySettings.names[QualitySettings.GetQualityLevel()]})의 렌더 파이프라인 오버라이드 = " +
                          $"{(qualityOverride != null ? qualityOverride.name : "없음 (전역 기본값 사용)")}");

            var text = sb.ToString();
            Debug.Log("[CrtSetupTool] 진단 결과\n" + text);
            EditorUtility.DisplayDialog("CRT 진단 결과", text, "확인");
        }

        /// <summary>
        /// 빈 스카이박스만으로는 왜곡/색수차/비네트가 눈에 안 띄어서, 카메라 앞에 색깔 있는
        /// 격자무늬 쿼드를 하나 띄워준다. 임시 디버그용 — 정식 아트가 들어오면 지워도 된다.
        /// </summary>
        [MenuItem("BlueComplex/CRT/Add Test Pattern")]
        public static void AddTestPattern()
        {
            var existing = GameObject.Find("CRT Test Pattern");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorUtility.DisplayDialog("CRT 테스트 패턴", "이미 씬에 있어서 선택만 했습니다.", "확인");
                return;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CRT Test Pattern";
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            var texture = CreateTestPatternTexture();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            var material = new Material(shader) { name = "CRT_TestPatternMaterial", mainTexture = texture };
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                const float distance = 5f;
                var t = quad.transform;
                t.position = mainCamera.transform.position + mainCamera.transform.forward * distance;
                t.rotation = mainCamera.transform.rotation;
                var height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                t.localScale = new Vector3(height * mainCamera.aspect, height, 1f);
            }

            Undo.RegisterCreatedObjectUndo(quad, "Add CRT Test Pattern");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = quad;

            Debug.Log("[CrtSetupTool] CRT Test Pattern 생성 완료. Play 모드로 확인하세요. (씬 저장 필요: Ctrl+S)");
        }

        private static Texture2D CreateTestPatternTexture()
        {
            const int size = 512;
            const int cell = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];

            var red = new Color32(230, 60, 60, 255);
            var green = new Color32(60, 220, 90, 255);
            var blue = new Color32(70, 130, 230, 255);
            var yellow = new Color32(235, 210, 60, 255);
            var gridLine = new Color32(20, 20, 20, 255);
            var white = new Color32(245, 245, 245, 255);

            for (var y = 0; y < size; y++)
            {
                var top = y >= size / 2;
                for (var x = 0; x < size; x++)
                {
                    var onGrid = x % cell < 2 || y % cell < 2;
                    var onBorder = x < 3 || x >= size - 3 || y < 3 || y >= size - 3;
                    Color32 c;
                    if (onBorder) c = white;
                    else if (onGrid) c = gridLine;
                    else
                    {
                        var left = x < size / 2;
                        c = left
                            ? (top ? red : blue)
                            : (top ? green : yellow);
                    }
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Material EnsureMaterial(Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            EnsureFolder(SettingsFolder);
            material = new Material(shader) { name = "CRT_PostProcess" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static CrtEffectPreset EnsurePreset()
        {
            var preset = AssetDatabase.LoadAssetAtPath<CrtEffectPreset>(PresetPath);
            if (preset != null) return preset;

            EnsureFolder(SettingsFolder);
            preset = ScriptableObject.CreateInstance<CrtEffectPreset>();
            AssetDatabase.CreateAsset(preset, PresetPath);
            return preset;
        }

        private enum RendererFeatureResult { Added, AlreadyPresent, RendererNotFound }

        private static RendererFeatureResult EnsureRendererFeature(string rendererPath, Material material)
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            if (rendererData == null) return RendererFeatureResult.RendererNotFound;

            foreach (var existing in rendererData.rendererFeatures)
            {
                if (existing is FullScreenPassRendererFeature existingFullScreen && existingFullScreen.passMaterial == material)
                    return RendererFeatureResult.AlreadyPresent;
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = "CRT";
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.fetchColorBuffer = true;

            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var serializedRenderer = new SerializedObject(rendererData);
            var featuresProp = serializedRenderer.FindProperty("m_RendererFeatures");
            var mapProp = serializedRenderer.FindProperty("m_RendererFeatureMap");

            featuresProp.arraySize++;
            featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = feature;

            mapProp.arraySize++;
            mapProp.GetArrayElementAtIndex(mapProp.arraySize - 1).longValue = localId;

            serializedRenderer.ApplyModifiedProperties();
            EditorUtility.SetDirty(rendererData);

            return RendererFeatureResult.Added;
        }

        private static CrtEffectDriver EnsureCrtController(Material material, CrtEffectPreset preset)
        {
            var driver = Object.FindFirstObjectByType<CrtEffectDriver>();
            if (driver == null)
            {
                var go = new GameObject("CRT Controller");
                driver = go.AddComponent<CrtEffectDriver>();
                Undo.RegisterCreatedObjectUndo(go, "Create CRT Controller");
            }

            var serializedDriver = new SerializedObject(driver);
            serializedDriver.FindProperty("_crtMaterial").objectReferenceValue = material;
            serializedDriver.FindProperty("_preset").objectReferenceValue = preset;
            serializedDriver.ApplyModifiedProperties();

            return driver;
        }

        private static void EnsureBootstrapper(CrtEffectDriver driver)
        {
            var bootstrapper = Object.FindFirstObjectByType<StageBootstrapper>();
            if (bootstrapper == null)
            {
                var go = new GameObject("StageBootstrapper");
                bootstrapper = go.AddComponent<StageBootstrapper>();
                Undo.RegisterCreatedObjectUndo(go, "Create StageBootstrapper");
            }

            var serializedBootstrapper = new SerializedObject(bootstrapper);
            var driverProp = serializedBootstrapper.FindProperty("_crtEffectDriver");
            if (driverProp.objectReferenceValue == null)
                driverProp.objectReferenceValue = driver;
            serializedBootstrapper.ApplyModifiedProperties();
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
