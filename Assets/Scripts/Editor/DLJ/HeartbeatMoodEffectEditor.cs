using System.Linq;
using BlueComplex.UI.Bootstrap;
using BlueComplex.UI.Effects.DLJ;
using BlueComplex.UI.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BlueComplex.Editor.DLJ
{
    [CustomEditor(typeof(HeartbeatMoodEffectController))]
    public sealed class HeartbeatMoodEffectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var effect = (HeartbeatMoodEffectController)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Play 중 미리보기 버튼으로 화면만 바꿀 수 있어. 게임 심박수와 BGM에는 영향이 없어. 흥분 진입 시 기본 글리치 2초, 색수차 3.5초(마지막 1초 감쇠) 뒤 핑크 단색 톤이 유지돼.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || !effect.isActiveAndEnabled))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("매우 침체 25")) effect.PreviewHeartbeat(25);
                if (GUILayout.Button("침체 55")) effect.PreviewHeartbeat(55);
                if (GUILayout.Button("안정 80")) effect.PreviewHeartbeat(80);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("흥분 125")) effect.PreviewHeartbeat(125);
                if (GUILayout.Button("매우 흥분 170")) effect.PreviewHeartbeat(170);
                if (GUILayout.Button("실제 상태 복귀")) effect.ResumeLive();
                EditorGUILayout.EndHorizontal();
            }
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(effect.IsPreviewing ? "미리보기" : "게임 연동", effect.DisplayedHeartbeat.ToString());
                EditorGUILayout.LabelField("글리치 남은 시간", effect.GlitchRemaining.ToString("F2") + "s");
                EditorGUILayout.LabelField("색수차 남은 시간", effect.ChromaticRemaining.ToString("F2") + "s");
                Repaint();
            }
        }

        [MenuItem("Tools/BlueComplex/DLJ/Setup Mood Effects In Active Scene")]
        public static void SetupActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[DLJ Mood] 씬 연결은 Play를 멈춘 뒤 실행해 줘.");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var feature = renderer != null ? renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(f => f.name == "CRT") : null;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DLJ/HeartbeatMood.shader");
            if (feature == null || shader == null)
            {
                Debug.LogError("[DLJ Mood] PC_Renderer의 CRT 패스 또는 HeartbeatMood.shader를 찾지 못했어.");
                return;
            }
            var roots = scene.GetRootGameObjects();
            var effect = roots.SelectMany(r => r.GetComponentsInChildren<HeartbeatMoodEffectController>(true)).FirstOrDefault();
            if (effect == null)
            {
                var go = new GameObject("DLJ Heartbeat Mood Effects");
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Add DLJ Mood Effects");
                effect = Undo.AddComponent<HeartbeatMoodEffectController>(go);
            }
            Undo.RecordObject(effect, "Connect DLJ Mood Effects");
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("_crtFeature").objectReferenceValue = feature;
            serialized.FindProperty("_effectShader").objectReferenceValue = shader;
            serialized.FindProperty("_basePreset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CrtEffectPreset>("Assets/Settings/CRT/CrtEffectPreset.asset");
            serialized.FindProperty("_bootstrapper").objectReferenceValue = roots.SelectMany(r => r.GetComponentsInChildren<StageBootstrapper>(true)).FirstOrDefault();
            serialized.FindProperty("_heartRate").objectReferenceValue = roots.SelectMany(r => r.GetComponentsInChildren<HeartRateController>(true)).FirstOrDefault();
            var lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true))
                .Where(l => l.type == LightType.Point || l.type == LightType.Spot).ToArray();
            var property = serialized.FindProperty("_lights");
            property.arraySize = lights.Length;
            for (var i = 0; i < lights.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = effect.gameObject;
            Debug.Log("[DLJ Mood] 현재 씬에 연결했어. 씬 저장 후 Play에서 인스펙터 미리보기로 확인해 줘.", effect);
        }
    }
}
