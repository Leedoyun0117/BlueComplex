using System;
using System.Collections.Generic;
using System.Reflection;
using BlueComplex.UI.Effects.DLJ;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlueComplex.Editor.DLJ
{
    public static class DLJ_HeartbeatMoodRoomChecks
    {
        [MenuItem("Tools/BlueComplex/DLJ/Validate Mood Room Bindings")]
        public static void RunInEditor() => Debug.Log($"[DLJ Mood] {Run()} room checks passed.");

        public static int Run()
        {
            var checks = 0;
            void Check(bool condition, string reason)
            {
                if (!condition) throw new InvalidOperationException(reason);
                checks++;
            }
            void Edge(Vector3 a, Vector3 b, bool expected, string reason)
            {
                Check(DLJ_HeartbeatMoodEffectController.ClipWindowEdge(ref a, ref b, 0.3f, 100f) == expected, reason);
                if (expected) Check(a.x >= 0 && a.x <= 1 && b.x >= 0 && b.x <= 1
                    && a.y >= 0 && a.y <= 1 && b.y >= 0 && b.y <= 1, reason + " stays inside viewport");
            }
            Edge(new Vector3(-1, 0.5f, 10), new Vector3(2, 0.5f, 10), true, "Crossing horizontal edge survives");
            Edge(new Vector3(0.5f, -1, 10), new Vector3(0.5f, 2, 10), true, "Crossing vertical edge survives");
            Edge(new Vector3(-1, 2, 10), new Vector3(2, -1, 10), true, "Crossing diagonal survives");
            Edge(new Vector3(-1, 0, 10), new Vector3(-1, 1, 10), false, "Offscreen edge excluded");
            Edge(new Vector3(0, 0, -1), new Vector3(1, 1, 10), false, "Behind camera excluded");
            Edge(new Vector3(0, 0, 101), new Vector3(1, 1, 101), false, "Beyond far plane excluded");
            Edge(new Vector3(0.5f, 0.5f, 10), new Vector3(0.5f, 0.5f, 10), false, "Zero length excluded");

            var owner = new GameObject("DLJ room check (temporary)");
            owner.SetActive(false);
            var sceneObjects = new GameObject("DLJ room check sources (temporary)");
            var shader = Shader.Find("BlueComplex/DLJ/HeartbeatMood");
            Check(shader != null, "Mood shader imported");
            var material = new Material(shader);
            var effect = owner.AddComponent<DLJ_HeartbeatMoodEffectController>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            void Set(string field, object value) => typeof(DLJ_HeartbeatMoodEffectController).GetField(field, flags).SetValue(effect, value);
            void Call(string method) => typeof(DLJ_HeartbeatMoodEffectController).GetMethod(method, flags).Invoke(effect, null);
            Transform Point(string name, Vector3 position)
            {
                var point = new GameObject(name).transform;
                point.SetParent(sceneObjects.transform);
                point.position = position;
                return point;
            }
            try
            {
                var camera = Point("Camera", Vector3.zero).gameObject.AddComponent<Camera>();
                camera.aspect = 16f / 9f;
                var lamp = Point("Room 1 light", new Vector3(0, 1, 10)).gameObject.AddComponent<Light>();
                lamp.color = Color.red;
                lamp.useColorTemperature = true;
                var windowLight = Point("Room 2 light", new Vector3(1, 1, 10)).gameObject.AddComponent<Light>();
                windowLight.color = Color.blue;
                var window = new DLJ_HeartbeatMoodEffectController.WindowOutline
                {
                    Corners = new List<Transform>
                    {
                        Point("TL", new Vector3(-2, 2, 10)), Point("TR", new Vector3(2, 2, 10)),
                        Point("BR", new Vector3(2, -2, 10)), Point("BL", new Vector3(-2, -2, 10))
                    },
                    InteriorTarget = Point("Inside", new Vector3(-3, -3, 10))
                };
                var rooms = new List<DLJ_HeartbeatMoodEffectController.RoomBinding>
                {
                    new() { Name = "1", SceneCamera = camera, Lights = new() { lamp }, ScatterLights = new() { lamp } },
                    new() { Name = "2", SceneCamera = camera, Lights = new() { windowLight },
                        Mode = DLJ_HeartbeatMoodEffectController.ScatterMode.WindowEdges, Windows = new() { window } },
                    new() { Name = "3", SceneCamera = camera }
                };
                Set("_rooms", rooms);
                Set("_runtimeMaterial", material);
                var state = (DLJ_HeartbeatMoodEffectState)typeof(DLJ_HeartbeatMoodEffectController).GetField("_state", flags).GetValue(effect);
                state.Show(25, new BlueComplex.Core.Stability.HeartbeatZone(), true);
                effect.SelectRoom(0);
                Check(lamp.color == Color.white && !lamp.useColorTemperature, "Selected room turns white");
                Check(windowLight.color == Color.blue, "Other room untouched");
                Check(material.GetInt("_WaterSourceCount") == 1 && material.GetInt("_WindowEdgeCount") == 0, "Lamp room uses lamp only");
                effect.SelectRoom(1);
                Check(lamp.color == Color.red && lamp.useColorTemperature, "Previous room restores color and temperature");
                Check(windowLight.color == Color.white, "New room turns white");
                Check(material.GetInt("_WaterSourceCount") == 0 && material.GetInt("_WindowEdgeCount") == 4, "Window room emits four edges only");
                var direction = material.GetVectorArray("_WindowDirections")[0];
                Check(Mathf.Abs(direction.x) < 0.001f && direction.y * (SystemInfo.graphicsUVStartsAtTop ? -1 : 1) > 0.99f,
                    "Top edge points up regardless of legacy target");
                window.Corners.Reverse();
                Call("ApplyVisuals");
                var reversedTop = material.GetVectorArray("_WindowDirections")[2];
                Check(Vector2.Distance(direction, reversedTop) < 0.001f, "Reversed corners preserve outward direction");
                window.Corners.Reverse();
                var rotatedNormal = DLJ_HeartbeatMoodEffectController.WindowOutwardNormal(Vector2.zero, new Vector2(1, 1), 1);
                Check(Vector2.Dot(rotatedNormal, new Vector2(1, -1).normalized) > 0.999f, "Rotated edge uses geometric normal");
                rooms[1].SceneCamera = null;
                Call("ApplyVisuals");
                Check(material.GetInt("_WindowEdgeCount") == 0, "Missing camera clears previous window data");
                rooms[1].SceneCamera = camera;
                window.Corners[0].gameObject.SetActive(false);
                Call("ApplyVisuals");
                Check(material.GetInt("_WindowEdgeCount") == 2, "Inactive corner excludes adjacent edges");
                window.Corners[0].gameObject.SetActive(true);
                window.Closed = false;
                Call("ApplyVisuals");
                Check(material.GetInt("_WindowEdgeCount") == 3, "Open polyline does not close");
                window.Closed = true;
                rooms[1].Windows.Add(window);
                rooms[1].Windows.Add(window);
                Call("ApplyVisuals");
                Check(material.GetInt("_WindowEdgeCount") == 8, "Window edge budget enforced");
                rooms[1].Root = owner;
                Call("LateUpdate");
                Check(windowLight.color == Color.blue && material.GetInt("_WindowEdgeCount") == 0, "Inactive room restores lighting and clears sources");
                effect.SelectRoom(2);
                Check(material.GetInt("_WaterSourceCount") == 0 && material.GetInt("_WindowEdgeCount") == 0, "Empty room never borrows other sources");
                effect.SelectRoom(99);
                Check(effect.ActiveRoomIndex == 2, "Invalid selection ignored");
                state.Show(125, new BlueComplex.Core.Stability.HeartbeatZone(), false);
                state.Advance(1);
                var chromatic = state.ChromaticRemaining;
                effect.SelectRoom(0);
                Check(Mathf.Approximately(chromatic, state.ChromaticRemaining), "Room change never restarts excitement timer");
                Call("RestoreLights");
                Check(lamp.color == Color.red && windowLight.color == Color.blue, "Cleanup restores both rooms");

                // 이전에 조절한 공통 값은 최초 이전 시 보존하고, 이후에는 방끼리 공유하지 않아.
                Set("_blueTintStrength", 0.37f);
                Set("_pastelPink", new Color(0.21f, 0.42f, 0.63f));
                Set("_chromaticSeconds", 4.25f);
                foreach (var room in rooms) room.SettingsInitialized = false;
                Check(effect.InitializeRoomSettings(), "Legacy settings migrate once");
                Check(Mathf.Approximately(rooms[0].Settings.BlueTintStrength, 0.37f)
                    && rooms[1].Settings.PastelPink == new Color(0.21f, 0.42f, 0.63f)
                    && Mathf.Approximately(rooms[2].Settings.ChromaticSeconds, 4.25f), "Tuned color, strength and duration preserved");
                Check(!ReferenceEquals(rooms[0].Settings, rooms[1].Settings)
                    && !ReferenceEquals(rooms[1].Settings, rooms[2].Settings), "Each room owns separate settings");
                rooms[0].Settings.BlueTintStrength = 0.12f;
                Check(!effect.InitializeRoomSettings() && Mathf.Approximately(rooms[0].Settings.BlueTintStrength, 0.12f), "Repeated migration never overwrites edits");
                Check(Mathf.Approximately(rooms[1].Settings.BlueTintStrength, 0.37f), "Editing room 1 leaves room 2 unchanged");
                rooms[0].Settings.WaterLightStrength = 0.2f;
                rooms[1].Settings.WaterLightStrength = 0.8f;
                effect.SelectRoom(0);
                Check(Mathf.Approximately(material.GetFloat("_WaterLightStrength"), 0.2f), "Room 1 shader uses its settings");
                effect.SelectRoom(1);
                Check(Mathf.Approximately(material.GetFloat("_WaterLightStrength"), 0.8f), "Room 2 shader uses its settings");
                var serialized = new SerializedObject(effect);
                serialized.FindProperty("_rooms").GetArrayElementAtIndex(1).FindPropertyRelative("Settings")
                    .FindPropertyRelative("WaterLightStrength").floatValue = 0.61f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Call("ApplyVisuals");
                Check(Mathf.Approximately(material.GetFloat("_WaterLightStrength"), 0.61f), "Inspector edit updates selected room immediately");
                Check(Mathf.Approximately(effect.Rooms[0].Settings.WaterLightStrength, 0.2f), "Inspector edit does not leak to other room");

                // 직렬화 왕복 후에도 초기화 플래그와 방별 값이 남아 재마이그레이션되지 않아.
                var copyGo = new GameObject("Serialized settings copy");
                copyGo.SetActive(false);
                copyGo.transform.SetParent(owner.transform);
                var copy = copyGo.AddComponent<DLJ_HeartbeatMoodEffectController>();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(effect), copy);
                Check(!copy.NeedsRoomSettingsInitialization
                    && Mathf.Approximately(copy.Rooms[1].Settings.WaterLightStrength, 0.61f), "Saved settings survive serialization");

                var selected = effect.Rooms[1].Settings;
                selected.ChromaticSeconds = 0.8f;
                selected.ChromaticFadeSeconds = 0.25f;
                selected.GlitchSeconds = 0.4f;
                selected.TransitionSeconds = 0.1f;
                Call("ApplyVisuals");
                state.Show(80, new BlueComplex.Core.Stability.HeartbeatZone(), true);
                state.Show(125, new BlueComplex.Core.Stability.HeartbeatZone(), false);
                Check(Mathf.Approximately(state.ChromaticRemaining, 0.8f) && Mathf.Approximately(state.GlitchRemaining, 0.4f), "Next excitement entry uses selected room durations");
                state.Advance(0.2f);
                effect.SelectRoom(0);
                Check(Mathf.Approximately(state.ChromaticRemaining, 0.6f), "Different room duration never restarts running burst");
                selected.NormalStateStrength = 0.42f;
                effect.SelectRoom(1);
                state.Advance(0.1f);
                Check(Mathf.Approximately(state.Excited, 0.42f), "Room strength and transition apply to current mood");
                effect.Rooms[2].Mode = DLJ_HeartbeatMoodEffectController.ScatterMode.WindowEdges;
                effect.Rooms[2].Settings.WindowStyle = DLJ_HeartbeatMoodEffectController.WindowLightStyle.SoftBands;
                effect.SelectRoom(2);
                Check(material.GetInt("_WindowLightStyle") == 1, "Room 3 activates soft window bands");
                effect.SelectRoom(1);
                Check(material.GetInt("_WindowLightStyle") == 0, "Other window rooms keep existing rays");
                effect.Rooms[0].Settings.WindowStyle = DLJ_HeartbeatMoodEffectController.WindowLightStyle.SoftBands;
                effect.SelectRoom(0);
                Check(material.GetInt("_WindowLightStyle") == 0, "Lamp rooms never use window band style");
            }
            finally
            {
                Call("RestoreLights");
                Set("_runtimeMaterial", null);
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(sceneObjects);
                Object.DestroyImmediate(material);
            }
            return checks;
        }
    }
}
