using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Presentation;
using BlueComplex.UI.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// UI가 CRT를 함께 받도록 하는 렌더링 배선: UI 전용 오프스크린 카메라와 렌더러(Base_Renderer),
    /// 씬의 UI Camera / MainHud / EventSystem 오브젝트를 자동화한다. CrtSetupTool.cs와 동일한
    /// SerializedObject 조작 패턴(멱등)을 쓴다.
    ///
    /// UI 합성은 별도 패스가 아니라 기존 CRT_PostProcess.mat(CrtEffect.shader)의 _UITex 슬롯에
    /// RT_UI를 직접 물려서 CRT 셰이더 안에서 처리한다. 원래는 독립된 "UI Composite"
    /// FullScreenPassRendererFeature를 CRT 앞에 체이닝했지만, 같은 injectionPoint라도 두
    /// FullScreenPassRendererFeature를 연달아 실행하면 두 번째(CRT) 패스의 좌표계가 깨져 화면
    /// 전체가 배럴 클리핑으로 새까맣게 나오는 문제가 있어(프레임 디버거로 확인) 한 패스로 합쳤다.
    /// 그래서 CRT 자체의 FullScreenPassRendererFeature 배선(CrtSetupTool.cs 담당)은 그대로 둔다.
    /// </summary>
    public static class UICompositorSetupTool
    {
        private const string SettingsFolder = "Assets/Settings/UI";
        private const string RtUiAssetPath = SettingsFolder + "/RT_UI.renderTexture";
        private const string CrtMaterialPath = "Assets/Settings/CRT/CRT_PostProcess.mat";
        private const string ObsoleteCompositeMaterialPath = SettingsFolder + "/UIComposite.mat";
        private const string BaseRendererPath = "Assets/Settings/Base_Renderer.asset";
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string UiLayerName = "UI";

        private static readonly string[] RendererPaths =
        {
            "Assets/Settings/PC_Renderer.asset",
            "Assets/Settings/Mobile_Renderer.asset",
        };

        private static readonly string[] PipelineAssetPaths =
        {
            "Assets/Settings/PC_RPAsset.asset",
            "Assets/Settings/Mobile_RPAsset.asset",
        };

        [MenuItem("BlueComplex/UI/Setup UI Compositor")]
        public static void SetupAll()
        {
            var crtMaterial = AssetDatabase.LoadAssetAtPath<Material>(CrtMaterialPath);
            if (crtMaterial == null)
            {
                Fail($"CRT 머티리얼을 찾을 수 없습니다: {CrtMaterialPath}. " +
                     "먼저 BlueComplex/CRT/Setup CRT Effect를 실행하세요.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var rtUi = EnsureRtUiAsset();
            BindUiTexToMaterial(crtMaterial, rtUi);
            var baseRendererIndex = EnsureBaseRendererInPipelines();
            RemoveObsoleteUiCompositeFeature();

            var uiLayer = LayerMask.NameToLayer(UiLayerName);
            if (uiLayer < 0)
            {
                Fail($"'{UiLayerName}' 레이어를 찾을 수 없습니다. ProjectSettings/TagManager.asset에 이미 예약돼 있어야 합니다.");
                return;
            }

            var mainHudPrefab = UiLayoutSetupTool.EnsureMainHudPrefab();

            var uiCameraGo = EnsureUiCamera(uiLayer, baseRendererIndex, rtUi);
            EnsureRig(uiCameraGo);
            EnsureMainHudInstance(mainHudPrefab, uiCameraGo.GetComponent<Camera>());
            ExcludeUiLayerFromMainCamera(uiLayer);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = $"CRT 머티리얼(_UITex): {CrtMaterialPath}\nBase_Renderer 인덱스: {baseRendererIndex}\n" +
                      "씬: UI Camera / MainHud / EventSystem 준비 완료, GameScene.unity 저장됨.";
            Debug.Log("[UICompositorSetupTool] " + msg.Replace("\n", " / "));
            if (Application.isBatchMode) return;
            EditorUtility.DisplayDialog("UI 합성 파이프라인 셋업 완료", msg, "확인");
        }

        private static void Fail(string message)
        {
            Debug.LogError("[UICompositorSetupTool] " + message);
            if (Application.isBatchMode) return;
            EditorUtility.DisplayDialog("셋업 실패", message, "확인");
        }

        /// <summary>
        /// RT_UI를 런타임에 새로 만드는 대신 미리 에셋으로 만들어 둔다 — 그래야 UI 카메라의
        /// Output Texture가 Edit/Play 모드와 무관하게 항상 채워져 있어서, "카메라에 텍스처가
        /// 물려 있는지"와 "그 텍스처에 내용이 들어가는지"를 따로 디버깅할 수 있다.
        /// </summary>
        private static RenderTexture EnsureRtUiAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RtUiAssetPath);
            if (existing != null) return existing;

            EnsureFolder(SettingsFolder);
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = "RT_UI",
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false,
            };
            AssetDatabase.CreateAsset(rt, RtUiAssetPath);
            return rt;
        }

        /// <summary>
        /// 전역 텍스처(Shader.SetGlobalTexture) 대신 머티리얼에 직접 물린다 — 프레임 디버거로
        /// 확인해보니 전역 바인딩이 이 Render Graph 패스까지 도달하지 못하고 Unity 기본 더미
        /// 텍스처가 대신 샘플링되고 있었다. 매번(멱등하게) 다시 물려서 RT_UI 에셋이 재생성돼도
        /// 어긋나지 않게 한다.
        /// </summary>
        private static void BindUiTexToMaterial(Material material, RenderTexture rtUi)
        {
            if (material.GetTexture("_UITex") == rtUi) return;

            material.SetTexture("_UITex", rtUi);
            EditorUtility.SetDirty(material);
        }

        /// <summary>
        /// UI 카메라 전용 렌더러(피처 없음)를 만들어 두 파이프라인 자산의 렌더러 목록에 추가한다.
        /// 반환값은 그 렌더러의 인덱스(UI 카메라의 rendererIndex로 사용) — 두 파이프라인 자산에서
        /// 다르게 나오면 경고만 하고 첫 번째 값을 쓴다(현재 둘 다 렌더러 1개뿐이라 항상 1이 된다).
        /// </summary>
        private static int EnsureBaseRendererInPipelines()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(BaseRendererPath);
            if (rendererData == null)
            {
                EnsureFolder("Assets/Settings");
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, BaseRendererPath);
                rendererData.name = "Base_Renderer";
            }

            var resolvedIndex = -1;
            foreach (var pipelinePath in PipelineAssetPaths)
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
                if (pipeline == null)
                {
                    Debug.LogWarning($"[UICompositorSetupTool] 파이프라인 자산을 찾을 수 없습니다: {pipelinePath}");
                    continue;
                }

                var so = new SerializedObject(pipeline);
                var listProp = so.FindProperty("m_RendererDataList");

                var existingIndex = -1;
                for (var i = 0; i < listProp.arraySize; i++)
                {
                    if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == rendererData)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex < 0)
                {
                    listProp.arraySize++;
                    existingIndex = listProp.arraySize - 1;
                    listProp.GetArrayElementAtIndex(existingIndex).objectReferenceValue = rendererData;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(pipeline);
                }

                if (resolvedIndex < 0) resolvedIndex = existingIndex;
                else if (resolvedIndex != existingIndex)
                    Debug.LogWarning($"[UICompositorSetupTool] Base_Renderer 인덱스가 파이프라인 자산마다 다릅니다 " +
                                      $"({pipelinePath}: {existingIndex}, 이전 값: {resolvedIndex}). UI 카메라의 rendererIndex를 수동으로 확인하세요.");
            }

            return resolvedIndex;
        }

        /// <summary>
        /// 이전 시도(독립된 "UI Composite" FullScreenPassRendererFeature)의 흔적을 정리한다.
        /// UI 합성이 이제 CRT_PostProcess.mat 안에서 처리되므로 더는 필요 없다 — 남아 있으면
        /// 아무 효과도 없이(_BlitTexture만 그대로 되돌려주는) 매 프레임 풀스크린 패스 하나를
        /// 낭비하게 된다. 처음 실행하는 프로젝트에서는 애초에 없으므로 항상 안전한 no-op이다.
        /// </summary>
        private static void RemoveObsoleteUiCompositeFeature()
        {
            foreach (var path in RendererPaths)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (rendererData == null) continue;

                FullScreenPassRendererFeature obsolete = null;
                foreach (var existing in rendererData.rendererFeatures)
                {
                    if (existing is FullScreenPassRendererFeature fs && fs.name == "UI Composite")
                    {
                        obsolete = fs;
                        break;
                    }
                }
                if (obsolete == null) continue;

                var so = new SerializedObject(rendererData);
                var featuresProp = so.FindProperty("m_RendererFeatures");
                var mapProp = so.FindProperty("m_RendererFeatureMap");

                var index = IndexOf(featuresProp, obsolete);
                if (index >= 0)
                {
                    RemoveArrayElementAt(featuresProp, index);
                    if (index < mapProp.arraySize) RemoveArrayElementAt(mapProp, index);
                    so.ApplyModifiedProperties();
                }

                AssetDatabase.RemoveObjectFromAsset(obsolete);
                Object.DestroyImmediate(obsolete, true);
                EditorUtility.SetDirty(rendererData);
                Debug.Log($"[UICompositorSetupTool] {path}에서 구식 \"UI Composite\" 피처를 제거했습니다.");
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(ObsoleteCompositeMaterialPath) != null)
            {
                AssetDatabase.DeleteAsset(ObsoleteCompositeMaterialPath);
                Debug.Log($"[UICompositorSetupTool] 구식 머티리얼을 삭제했습니다: {ObsoleteCompositeMaterialPath}");
            }
        }

        private static int IndexOf(SerializedProperty arrayProp, Object value)
        {
            for (var i = 0; i < arrayProp.arraySize; i++)
                if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == value)
                    return i;
            return -1;
        }

        /// <summary>
        /// SerializedProperty 배열에서 오브젝트 참조 원소를 지울 때, 한 번만 DeleteArrayElementAtIndex를
        /// 부르면 (Unity의 오래된 동작 방식대로) 참조만 null로 비워지고 배열 크기는 그대로다 —
        /// 실제로 칸을 없애려면 한 번 더 불러야 한다. long 같은 값 타입 배열은 한 번으로 충분하다.
        /// </summary>
        private static void RemoveArrayElementAt(SerializedProperty arrayProp, int index)
        {
            var element = arrayProp.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
                arrayProp.DeleteArrayElementAtIndex(index);
            arrayProp.DeleteArrayElementAtIndex(index);
        }

        private static GameObject EnsureUiCamera(int uiLayer, int rendererIndex, RenderTexture targetTexture)
        {
            var go = GameObject.Find("UI Camera");
            if (go == null)
            {
                go = new GameObject("UI Camera", typeof(Camera));
                Undo.RegisterCreatedObjectUndo(go, "Create UI Camera");
            }

            var camera = go.GetComponent<Camera>();
            if (camera == null) camera = go.AddComponent<Camera>();

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.cullingMask = uiLayer >= 0 ? 1 << uiLayer : 0;
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            // 런타임에 스크립트가 할당하는 대신 에셋을 직접 물려 씬에 저장한다 — Output Texture가
            // Edit 모드에서도 None으로 보이지 않아야 나중에 "카메라 배선"과 "텍스처 내용"을 분리해
            // 디버깅할 수 있다. 해상도에 맞춘 리사이즈는 UiCompositorRig가 런타임에 담당한다.
            camera.targetTexture = targetTexture;
            // URP는 타깃(화면/RT)과 무관하게 모든 활성 카메라를 depth 오름차순으로 실행한다.
            // Main Camera(depth -1)의 CRT 패스가 같은 프레임의 RT_UI(_UITex)를 읽으려면 UI Camera가
            // 그보다 먼저 렌더돼야 한다 — 안 그러면 한 프레임 뒤처진 UI가 합성된다.
            camera.depth = -2f;

            var listener = go.GetComponent<AudioListener>();
            if (listener != null) Object.DestroyImmediate(listener);

            var additional = camera.GetUniversalAdditionalCameraData();
            if (rendererIndex >= 0) additional.SetRenderer(rendererIndex);
            additional.renderShadows = false;
            additional.requiresDepthOption = CameraOverrideOption.Off;
            additional.requiresColorOption = CameraOverrideOption.Off;

            return go;
        }

        private static void EnsureRig(GameObject uiCameraGo)
        {
            var rig = uiCameraGo.GetComponent<UiCompositorRig>();
            if (rig == null) rig = uiCameraGo.AddComponent<UiCompositorRig>();

            var so = new SerializedObject(rig);
            so.FindProperty("_uiCamera").objectReferenceValue = uiCameraGo.GetComponent<Camera>();
            so.ApplyModifiedProperties();
        }

        private static void EnsureMainHudInstance(GameObject prefab, Camera uiCamera)
        {
            if (prefab == null) return;

            // 기존 인스턴스는 항상 지우고 프리팹에서 새로 찍는다 — 이 인스턴스는 손으로 편집하는
            // 대상이 아니라서(전부 프리팹이 원본) 캔버스 설정 등을 최신 프리팹과 어긋나지 않게
            // 유지하는 쪽이 "찾으면 그대로 재사용"보다 안전하다.
            var existing = GameObject.Find("MainHud");
            if (existing != null) Object.DestroyImmediate(existing);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create MainHud");

            var canvas = instance.GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = uiCamera;

            WireBootstrapperReferences(instance);
        }

        /// <summary>
        /// 2단계 컨트롤러(SessionBoundView 파생) + MemorySpaceDropZone은 씬의 StageBootstrapper를
        /// 참조해야 하는데, 프리팹 자체는 씬 오브젝트를 참조할 수 없어서 여기서(인스턴스화 이후)
        /// 채워 넣는다. 둘 다 필드 이름이 "_bootstrapper"로 같아서 SerializedObject로 동일하게 쓴다.
        /// </summary>
        private static void WireBootstrapperReferences(GameObject mainHudInstance)
        {
            var bootstrapper = Object.FindFirstObjectByType<StageBootstrapper>();
            if (bootstrapper == null)
            {
                Debug.LogWarning("[UICompositorSetupTool] 씬에서 StageBootstrapper를 찾지 못해 " +
                                  "SessionBoundView/MemorySpaceDropZone의 _bootstrapper를 못 채웠습니다.");
                return;
            }

            foreach (var view in mainHudInstance.GetComponentsInChildren<SessionBoundView>(true))
                SetBootstrapperField(view, bootstrapper);

            var dropZone = mainHudInstance.GetComponentInChildren<MemorySpaceDropZone>(true);
            if (dropZone != null) SetBootstrapperField(dropZone, bootstrapper);
        }

        private static void SetBootstrapperField(Component component, StageBootstrapper bootstrapper)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty("_bootstrapper");
            if (prop == null) return;
            prop.objectReferenceValue = bootstrapper;
            so.ApplyModifiedProperties();
        }

        private static void ExcludeUiLayerFromMainCamera(int uiLayer)
        {
            var mainCameraGo = GameObject.FindWithTag("MainCamera");
            if (mainCameraGo == null) return;

            var camera = mainCameraGo.GetComponent<Camera>();
            if (camera == null) return;

            camera.cullingMask &= ~(1 << uiLayer);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
