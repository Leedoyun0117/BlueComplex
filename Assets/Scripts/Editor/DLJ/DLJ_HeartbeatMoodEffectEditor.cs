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
    [CustomEditor(typeof(DLJ_HeartbeatMoodEffectController))]
    public sealed class DLJ_HeartbeatMoodEffectEditor : UnityEditor.Editor
    {
        private static readonly string[] RoomInspectorExclusions = typeof(DLJ_HeartbeatMoodEffectController.MoodSettings)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Select(field => "_" + char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1))
            .Concat(new[] { "m_Script", "_lights", "_sceneCamera", "_scatterLights" }).ToArray();

        public override void OnInspectorGUI()
        {
            var effect = (DLJ_HeartbeatMoodEffectController)target;
            if (effect.NeedsRoomSettingsInitialization)
            {
                if (!Application.isPlaying) Undo.RecordObject(effect, "Migrate DLJ room settings");
                effect.InitializeRoomSettings();
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(effect);
                    if (effect.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(effect.gameObject.scene);
                }
            }
            serializedObject.Update();
            var rooms = serializedObject.FindProperty("_rooms");
            if (rooms.arraySize == 0)
                DrawPropertiesExcluding(serializedObject, "m_Script");
            else
                DrawPropertiesExcluding(serializedObject, RoomInspectorExclusions);
            serializedObject.ApplyModifiedProperties();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (rooms.arraySize == 0 && GUILayout.Button("기존 연결을 방 1로 옮기고 방 2·3 목록 만들기"))
                    CreateRoomList(effect);
                if (rooms.arraySize > 0 && GUILayout.Button("선택한 방에 창문 테두리 지점 만들기"))
                    CreateWindowOutline(effect);
            }
            if (rooms.arraySize > 0)
            {
                EditorGUILayout.HelpBox("Rooms > 각 방 > 연출 설정에서 침체·흥분·전환 수치를 따로 조절해. 다른 방의 값은 바뀌지 않아. Active Room Index는 0=방 1, 1=방 2, 2=방 3이며 방 전환 시 SelectRoom(index)를 호출해. 창문 길이·폭은 Windows 항목에서 조절해.", MessageType.Info);
                ValidateRoom(effect);
            }
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

        private static void ValidateRoom(DLJ_HeartbeatMoodEffectController effect)
        {
            if (effect.ActiveRoomIndex < 0 || effect.ActiveRoomIndex >= effect.Rooms.Count)
            {
                EditorGUILayout.HelpBox("Active Room Index가 목록 범위를 벗어났어. 산란과 광원 변경은 꺼져 있어.", MessageType.Warning);
                return;
            }
            var room = effect.Rooms[effect.ActiveRoomIndex];
            if (room == null) return;
            if (room.SceneCamera == null)
                EditorGUILayout.HelpBox("선택한 방의 Scene Camera가 비어 있어. 배경 카메라를 연결해.", MessageType.Warning);
            if (room.Mode != DLJ_HeartbeatMoodEffectController.ScatterMode.WindowEdges) return;
            var edges = 0;
            if (room.Windows != null)
                foreach (var window in room.Windows)
                {
                    if (window == null || window.Corners == null) continue;
                    edges += window.Closed && window.Corners.Count > 2 ? window.Corners.Count : Mathf.Max(0, window.Corners.Count - 1);
                    if (window.Corners.Any(corner => corner == null))
                        EditorGUILayout.HelpBox($"{window.Name}: 비어 있는 테두리 지점이 있어. 해당 변은 표시되지 않아.", MessageType.Warning);
                }
            if (edges == 0)
                EditorGUILayout.HelpBox("창문 테두리 지점을 만들고 Scene 뷰에서 실제 창틀 모서리에 맞춰 줘. 빛은 각 테두리의 바깥 방향으로 퍼져.", MessageType.Warning);
            if (edges > DLJ_HeartbeatMoodEffectController.MaxWindowEdges)
                EditorGUILayout.HelpBox("창문 변은 한 방당 8개까지 표시돼. 초과한 변은 제외돼.", MessageType.Warning);
        }

        public static void CreateRoomList(DLJ_HeartbeatMoodEffectController effect)
        {
            var data = new SerializedObject(effect);
            var rooms = data.FindProperty("_rooms");
            if (rooms.arraySize != 0) return;
            Undo.RecordObject(effect, "Create DLJ room bindings");
            rooms.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                var room = rooms.GetArrayElementAtIndex(i);
                room.FindPropertyRelative("Name").stringValue = $"방 {i + 1}";
                room.FindPropertyRelative("Root").objectReferenceValue = null;
                room.FindPropertyRelative("SceneCamera").objectReferenceValue = null;
                room.FindPropertyRelative("Lights").arraySize = 0;
                room.FindPropertyRelative("ScatterLights").arraySize = 0;
                room.FindPropertyRelative("Windows").arraySize = 0;
                room.FindPropertyRelative("Mode").enumValueIndex = i == 2 ? 1 : 0;
                room.FindPropertyRelative("SettingsInitialized").boolValue = false;
            }
            var first = rooms.GetArrayElementAtIndex(0);
            var roots = effect.gameObject.scene.GetRootGameObjects();
            first.FindPropertyRelative("SceneCamera").objectReferenceValue = data.FindProperty("_sceneCamera").objectReferenceValue
                ?? roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).FirstOrDefault(camera => camera.CompareTag("MainCamera"));
            var lights = ReadLights(data.FindProperty("_lights"));
            if (lights.Length == 0)
                lights = roots.SelectMany(root => root.GetComponentsInChildren<Light>())
                    .Where(light => light.type == LightType.Point || light.type == LightType.Spot).ToArray();
            WriteLights(first.FindPropertyRelative("Lights"), lights);
            var scatter = ReadLights(data.FindProperty("_scatterLights"));
            WriteLights(first.FindPropertyRelative("ScatterLights"), scatter.Length > 0 ? scatter : lights.Where(light => light != null && light.type == LightType.Point).ToArray());
            data.FindProperty("_activeRoomIndex").intValue = 0;
            data.ApplyModifiedProperties();
            effect.InitializeRoomSettings();
            EditorUtility.SetDirty(effect);
            EditorSceneManager.MarkSceneDirty(effect.gameObject.scene);
        }

        private static Light[] ReadLights(SerializedProperty property) => Enumerable.Range(0, property.arraySize)
            .Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue as Light).ToArray();

        private static void WriteLights(SerializedProperty property, Light[] lights)
        {
            property.arraySize = lights.Length;
            for (var i = 0; i < lights.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
        }

        private static void CreateWindowOutline(DLJ_HeartbeatMoodEffectController effect)
        {
            if (effect.ActiveRoomIndex < 0 || effect.ActiveRoomIndex >= effect.Rooms.Count) return;
            var room = effect.Rooms[effect.ActiveRoomIndex];
            if (room == null) return;
            var camera = room.SceneCamera;
            if (camera == null)
            {
                Debug.LogWarning("[DLJ Mood] 선택한 방의 Scene Camera부터 연결해 줘.", effect);
                return;
            }
            Undo.RecordObject(effect, "Add DLJ window outline");
            var group = new GameObject($"DLJ {room.Name} Window Outline");
            SceneManager.MoveGameObjectToScene(group, effect.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(group, "Add DLJ window outline");
            group.transform.SetParent(room.Root != null ? room.Root.transform : effect.transform, false);
            var window = new DLJ_HeartbeatMoodEffectController.WindowOutline();
            var depth = Mathf.Clamp(10f, camera.nearClipPlane + 0.1f, camera.farClipPlane - 0.1f);
            Transform Point(string name, float x, float y)
            {
                var point = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(point, "Add DLJ window point");
                point.transform.SetParent(group.transform, false);
                point.transform.position = camera.ViewportToWorldPoint(new Vector3(x, y, depth));
                return point.transform;
            }
            window.Corners.Add(Point("01 Top Left", 0.6f, 0.85f));
            window.Corners.Add(Point("02 Top Right", 0.9f, 0.85f));
            window.Corners.Add(Point("03 Bottom Right", 0.9f, 0.5f));
            window.Corners.Add(Point("04 Bottom Left", 0.6f, 0.5f));
            room.Windows ??= new System.Collections.Generic.List<DLJ_HeartbeatMoodEffectController.WindowOutline>();
            room.Windows.Add(window);
            room.Mode = DLJ_HeartbeatMoodEffectController.ScatterMode.WindowEdges;
            EditorUtility.SetDirty(effect);
            EditorSceneManager.MarkSceneDirty(effect.gameObject.scene);
            Selection.activeGameObject = group;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawWindowGizmos(DLJ_HeartbeatMoodEffectController effect, GizmoType gizmoType)
        {
            if (effect.ActiveRoomIndex < 0 || effect.ActiveRoomIndex >= effect.Rooms.Count) return;
            var room = effect.Rooms[effect.ActiveRoomIndex];
            if (room?.Windows == null || room.Mode != DLJ_HeartbeatMoodEffectController.ScatterMode.WindowEdges) return;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
            foreach (var window in room.Windows)
            {
                if (window?.Corners == null || window.Corners.Count < 2) continue;
                var count = window.Closed && window.Corners.Count > 2 ? window.Corners.Count : window.Corners.Count - 1;
                for (var i = 0; i < count; i++)
                {
                    var a = window.Corners[i];
                    var b = window.Corners[(i + 1) % window.Corners.Count];
                    if (a == null || b == null) continue;
                    Gizmos.DrawLine(a.position, b.position);
                }
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
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DLJ/DLJ_HeartbeatMood.shader");
            if (feature == null || shader == null)
            {
                Debug.LogError("[DLJ Mood] PC_Renderer의 CRT 패스 또는 DLJ_HeartbeatMood.shader를 찾지 못했어.");
                return;
            }
            var roots = scene.GetRootGameObjects();
            var effect = roots.SelectMany(r => r.GetComponentsInChildren<DLJ_HeartbeatMoodEffectController>(true)).FirstOrDefault();
            if (effect == null)
            {
                var go = new GameObject("DLJ Heartbeat Mood Effects");
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Add DLJ Mood Effects");
                effect = Undo.AddComponent<DLJ_HeartbeatMoodEffectController>(go);
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
            CreateRoomList(effect);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = effect.gameObject;
            Debug.Log("[DLJ Mood] 현재 씬에 연결했어. 씬 저장 후 Play에서 인스펙터 미리보기로 확인해 줘.", effect);
        }
    }
}
