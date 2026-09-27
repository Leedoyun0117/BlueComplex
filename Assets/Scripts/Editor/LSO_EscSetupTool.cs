using System;
using System.Linq;
using BlueComplex.Audio;
using UI.Esc;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    // Explicit migration only: never runs on import or regenerates MainHud.
    public static class LSO_EscSetupTool
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Esc.prefab";
        private const string ScenePath = "Assets/Scenes/LSO/LSO_room2.unity";
        private const string MaterialPath = "Assets/Settings/LSO/LSO_ScreenBrightness.mat";

        [MenuItem("BlueComplex/ESC/Complete Settings Wiring %&#e")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    WirePanel(prefab.GetComponent<LSO_EscPanel>());
                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }

                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (material == null) throw new InvalidOperationException("Missing brightness material");
                foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
                    WireRenderer(path, material);
                IncludeShader("Assets/Shaders/LSO/LSO_ScreenBrightness.shader");

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var panels = FindInScene<LSO_EscPanel>(scene);
                if (panels.Length == 0) throw new InvalidOperationException("No ESC instance in room2");
                foreach (var panel in panels) WirePanel(panel);

                // Brightness must survive disabling the settings UI.
                var appliers = FindInScene<LSO_BrightnessApplier>(scene);
                var applier = appliers.FirstOrDefault(a => a.GetComponentInParent<LSO_EscPanel>() == null);
                if (applier == null) applier = new GameObject("LSO Game Settings").AddComponent<LSO_BrightnessApplier>();
                SetReference(applier, "brightnessMaterial", material);
                foreach (var old in appliers.Where(a => a != applier)) UnityEngine.Object.DestroyImmediate(old);
                if (FindInScene<AudioSettingsController>(scene).Length == 0)
                    applier.gameObject.AddComponent<AudioSettingsController>();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Verify();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static void WirePanel(LSO_EscPanel panel)
        {
            if (panel == null || panel.Content == null) throw new InvalidOperationException("ESC content missing");
            Stretch((RectTransform)panel.transform);
            var backdrop = panel.GetComponent<LSO_EscBackdrop>();
            if (backdrop == null) backdrop = panel.gameObject.AddComponent<LSO_EscBackdrop>();
            var backdropData = new SerializedObject(backdrop);
            var color = backdropData.FindProperty("color").colorValue;
            color.a = 1f;
            backdropData.FindProperty("color").colorValue = color;
            backdropData.ApplyModifiedPropertiesWithoutUndo();

            // 실루엣은 LSO_EscPanel.Start에서 배경막 위에 자동 생성한다.

            foreach (var slider in panel.GetComponentsInChildren<Slider>(true))
            {
                var ancestor = slider.transform.parent;
                while (ancestor != null && ancestor != panel.transform &&
                       ancestor.name != "Brightness" && !Enum.TryParse<AudioChannel>(ancestor.name, true, out _))
                    ancestor = ancestor.parent;
                if (ancestor == null || ancestor == panel.transform)
                    throw new InvalidOperationException($"Unrecognized setting: {slider.name}");

                if (ancestor.name == "Brightness")
                {
                    var binding = SingleBinding<LSO_BrightnessSlider>(slider.gameObject);
                    SetReference(binding, "slider", slider);
                    SetReference(binding, "panel", panel);
                }
                else
                {
                    var binding = SingleBinding<LSO_VolumeSlider>(slider.gameObject);
                    SetReference(binding, "slider", slider);
                    var data = new SerializedObject(binding);
                    data.FindProperty("channel").enumValueIndex = (int)Enum.Parse<AudioChannel>(ancestor.name, true);
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Record(binding);
                }
            }
            // Older room2 instances stored volume bindings on the row, not on its Slider.
            foreach (var old in panel.GetComponentsInChildren<LSO_VolumeSlider>(true))
            {
                var data = new SerializedObject(old);
                var targetSlider = data.FindProperty("slider").objectReferenceValue as Slider;
                if (targetSlider == null || targetSlider.gameObject == old.gameObject) continue;
                var binding = targetSlider.GetComponent<LSO_VolumeSlider>();
                if (binding == null) continue;
                foreach (var field in new[] { "controller", "valueLabel" })
                {
                    var value = data.FindProperty(field).objectReferenceValue;
                    if (value != null) SetReference(binding, field, value);
                }
                UnityEngine.Object.DestroyImmediate(old);
            }
            Record(panel.transform);
            Record(backdrop);
        }

        private static T SingleBinding<T>(GameObject go) where T : Component
        {
            var bindings = go.GetComponents<T>().OrderBy(PrefabUtility.IsAddedComponentOverride).ToArray();
            var binding = bindings.Length == 0 ? go.AddComponent<T>() : bindings[0];
            foreach (var duplicate in bindings.Skip(1)) UnityEngine.Object.DestroyImmediate(duplicate);
            return binding;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
            Record(target);
        }

        private static void Record(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static void WireRenderer(string path, Material material)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var feature = renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(f => f.passMaterial == material);
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                AssetDatabase.AddObjectToAsset(feature, renderer);
            }
            feature.name = "LSO Screen Brightness";
            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = UnityEngine.Rendering.Universal.ScriptableRenderPassInput.None;
            feature.SetActive(true);
            renderer.rendererFeatures.Remove(feature);
            renderer.rendererFeatures.Add(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
        }

        private static void IncludeShader(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) throw new InvalidOperationException($"Missing shader {path}");
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            for (var i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            shaders.InsertArrayElementAtIndex(shaders.arraySize);
            shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Verify()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var panel in FindInScene<LSO_EscPanel>(scene))
            {
                foreach (var slider in panel.GetComponentsInChildren<Slider>(true))
                    Require(slider.GetComponents<LSO_BrightnessSlider>().Length + slider.GetComponents<LSO_VolumeSlider>().Length == 1,
                        "Exactly one settings binding per slider");
                Require(panel.GetComponentsInChildren<LSO_VolumeSlider>(true).Length == 3, "No duplicate volume bindings");
            }
            Require(FindInScene<LSO_BrightnessApplier>(scene).Length == 1, "One brightness applier");
            Require(FindInScene<LSO_BrightnessApplier>(scene)[0].GetComponentInParent<LSO_EscPanel>() == null, "Independent brightness applier");
            Require(FindInScene<AudioSettingsController>(scene).Length == 1, "One audio controller");
            foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                Require(renderer.rendererFeatures.Last() is FullScreenPassRendererFeature f &&
                    f.passMaterial == AssetDatabase.LoadAssetAtPath<Material>(MaterialPath), "Brightness after CRT");
            }
            Debug.Log("[LSO ESC] Wiring verification passed.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [MenuItem("BlueComplex/ESC/Capture Rendering Diagnostic %&#d")]
        private static void CaptureRendering()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode first."); return; }
            var panel = UnityEngine.Object.FindFirstObjectByType<LSO_EscPanel>();
            if (panel == null) throw new InvalidOperationException("No active ESC panel");
            if (!panel.IsOpen) panel.SendMessage("OpenPanel");
            var ready = EditorApplication.timeSinceStartup + 1.5;
            void Capture()
            {
                if (EditorApplication.timeSinceStartup < ready) return;
                EditorApplication.update -= Capture;
                if (panel == null) return;
                var report = new System.Text.StringBuilder();
                foreach (var rect in panel.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect != panel.transform && rect.parent != panel.transform && rect != panel.Content) continue;
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    var graphic = rect.GetComponent<Graphic>();
                    report.AppendLine($"{rect.name}: active={rect.gameObject.activeInHierarchy}, rect={rect.rect}, pos={rect.anchoredPosition}, world={string.Join(", ", corners.Select(c => c.ToString()))}, graphic={graphic?.color}, shader={graphic?.material.shader.name}");
                }
                foreach (var group in panel.GetComponentsInParent<CanvasGroup>())
                    report.AppendLine($"Group {group.name}: alpha={group.alpha}");
                var canvas = panel.GetComponentInParent<Canvas>();
                var rt = canvas.worldCamera.targetTexture;
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                var pixels = texture.GetPixels();
                report.AppendLine($"RT alpha min={pixels.Min(p=>p.a)} max={pixels.Max(p=>p.a)} center={texture.GetPixel(rt.width/2,rt.height/2)}");
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.WriteAllBytes("Logs/esc-ui.png", texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                System.IO.File.WriteAllText("Logs/esc-rendering.txt", report.ToString());
                ScreenCapture.CaptureScreenshot("Logs/esc-screen.png");
                Debug.Log("[LSO ESC] Rendering diagnostic saved in Logs.");
            }
            EditorApplication.update += Capture;
        }
    }
}
