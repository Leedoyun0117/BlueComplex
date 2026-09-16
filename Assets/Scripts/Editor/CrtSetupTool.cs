using System.Collections.Generic;
using System.IO;
using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
