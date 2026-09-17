using System.IO;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Rendering;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 정식 UI 1단계 레이아웃 골격: 10개 요소를 각각 프리팹으로 만들고, MainHud 프리팹에
    /// 앵커 배치로 중첩한다. 위치는 요청서 표(상단/좌우 portrait/하단 등)에 대한 합리적 추정이며,
    /// 실제 UI 디자인 가이드 문서가 저장소에 없어 정확한 좌표 기준은 없다 — 각 요소가 독립
    /// 프리팹이라 나중에 앵커만 바꾸면 된다. 기준 해상도 1920x1080.
    /// </summary>
    public static class UiLayoutSetupTool
    {
        private const string ElementsFolder = "Assets/Prefabs/UI/Elements";
        private const string MainHudPrefabPath = "Assets/Prefabs/UI/MainHud.prefab";

        private static readonly Color PanelRed = new Color32(200, 70, 80, 90);
        private static readonly Color PanelSlot = new Color32(120, 120, 130, 110);
        private static readonly Color PanelPortrait = new Color32(90, 90, 100, 130);
        private static readonly Color PanelXray = new Color32(140, 190, 210, 25);
        private static readonly Color PanelBubble = new Color32(150, 190, 230, 65);
        private static readonly Color PanelDuration = new Color32(210, 170, 80, 65);
        private static readonly Color PanelCard = new Color32(120, 130, 150, 110);

        [MenuItem("BlueComplex/UI/Setup Layout Skeleton")]
        public static void SetupAll()
        {
            var prefab = EnsureMainHudPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var msg = prefab != null
                ? $"MainHud 프리팹 준비 완료: {MainHudPrefabPath}"
                : "MainHud 프리팹 생성 실패 — Console 로그를 확인하세요.";
            Debug.Log("[UiLayoutSetupTool] " + msg);
            if (Application.isBatchMode) return;
            EditorUtility.DisplayDialog("레이아웃 골격 셋업", msg, "확인");
        }

        public static GameObject EnsureMainHudPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MainHudPrefabPath);
            if (existing != null) return existing;

            var font = TmpKoreanFontSetupTool.EnsureKoreanFontAsset();

            var heartRate = EnsureElementPrefab("HeartRateIndicatorPanel.prefab", "HeartRateIndicatorPanel",
                go => go.AddComponent<HeartRateIndicatorPanel>(), (go, comp) =>
                {
                    var bg = CreateImage(go.transform, "Background", PanelRed, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_background", bg);
                });

            var bpm = EnsureElementPrefab("BpmDisplay.prefab", "BpmDisplay",
                go => go.AddComponent<BpmDisplay>(), (go, comp) =>
                {
                    var label = CreateTmpText(go.transform, "Label", "80", font, 40, TextAlignmentOptions.Center,
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_label", label);
                });

            var items = EnsureElementPrefab("ItemDisplayPanel.prefab", "ItemDisplayPanel",
                go => go.AddComponent<ItemDisplayPanel>(), (go, comp) =>
                {
                    var layout = go.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 8;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = true;

                    var slot0 = CreateImage(go.transform, "Slot 0", PanelSlot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    var slot1 = CreateImage(go.transform, "Slot 1", PanelSlot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_slots", new[] { slot0, slot1 });
                });

            var portrait = EnsureElementPrefab("PortraitView.prefab", "PortraitView",
                go => go.AddComponent<PortraitView>(), (go, comp) =>
                {
                    var image = CreateImage(go.transform, "Portrait", PanelPortrait, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_portraitImage", image);
                });

            var xray = EnsureElementPrefab("ComplexXrayPanel.prefab", "ComplexXrayPanel",
                go => go.AddComponent<ComplexXrayPanel>(), (go, comp) =>
                {
                    CreateImage(go.transform, "Background", PanelXray, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                });

            var memory = EnsureElementPrefab("MemorySpaceBubble.prefab", "MemorySpaceBubble",
                go => go.AddComponent<MemorySpaceBubble>(), (go, comp) =>
                {
                    var bg = CreateImage(go.transform, "Bubble", PanelBubble, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_bubbleBackground", bg);
                });

            var duration = EnsureElementPrefab("ComplexDurationDisplay.prefab", "ComplexDurationDisplay",
                go => go.AddComponent<ComplexDurationDisplay>(), (go, comp) =>
                {
                    CreateImage(go.transform, "Background", PanelDuration, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                });

            var dialogue = EnsureElementPrefab("DialogueText.prefab", "DialogueText",
                go => go.AddComponent<DialogueText>(), (go, comp) =>
                {
                    var label = CreateTmpText(go.transform, "Label", "...", font, 32, TextAlignmentOptions.TopLeft,
                        Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-16, -12));
                    Wire(comp, "_label", label);
                });

            var clues = EnsureElementPrefab("ClueCardTray.prefab", "ClueCardTray",
                go => go.AddComponent<ClueCardTray>(), (go, comp) =>
                {
                    var layout = go.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 10;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = true;

                    var cards = new Image[4];
                    for (var i = 0; i < 4; i++)
                        cards[i] = CreateImage(go.transform, $"Card {i}", PanelCard, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_cards", cards);
                });

            return BuildMainHud(heartRate, bpm, items, portrait, xray, memory, duration, dialogue, clues, font);
        }

        private static GameObject BuildMainHud(GameObject heartRate, GameObject bpm, GameObject items, GameObject portrait,
            GameObject xray, GameObject memory, GameObject duration, GameObject dialogue, GameObject clues, TMP_FontAsset font)
        {
            var uiLayer = LayerMask.NameToLayer("UI");

            var root = new GameObject("MainHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(DistortionCorrectedGraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            // UI Camera의 farClipPlane(100)과 겹치면 캔버스 평면이 far plane에 걸려 통째로 컬링된다 —
            // 여유를 두고 훨씬 가깝게 잡는다.
            canvas.planeDistance = 10f;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            PlaceInstance(root.transform, items, "Item Display", 0.02f, 0.90f, 0.16f, 0.99f);
            PlaceInstance(root.transform, heartRate, "Heart Rate Indicator", 0.40f, 0.90f, 0.60f, 0.99f);
            PlaceInstance(root.transform, bpm, "BPM Display", 0.605f, 0.90f, 0.68f, 0.99f);
            PlaceInstance(root.transform, duration, "Complex Duration Display", 0.68f, 0.90f, 0.78f, 0.99f);
            PlaceInstance(root.transform, xray, "Complex X-ray Panel", 0.20f, 0.50f, 0.80f, 0.88f);
            PlaceInstance(root.transform, clues, "Clue Card Tray", 0.30f, 0.36f, 0.70f, 0.49f);
            PlaceInstance(root.transform, memory, "Memory Space Bubble", 0.30f, 0.20f, 0.70f, 0.34f);
            PlaceInstance(root.transform, portrait, "Yuki Portrait", 0.02f, 0.06f, 0.17f, 0.36f);
            PlaceInstance(root.transform, portrait, "Natsu Portrait", 0.83f, 0.06f, 0.98f, 0.36f);
            PlaceInstance(root.transform, dialogue, "Yuki Dialogue Text", 0.18f, 0.02f, 0.82f, 0.18f);

            if (uiLayer >= 0) SetLayerRecursively(root, uiLayer);

            EnsureFolder("Assets/Prefabs/UI");
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, MainHudPrefabPath);
            Object.DestroyImmediate(root);

            var raycaster = savedPrefab.GetComponent<DistortionCorrectedGraphicRaycaster>();
            var crtMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/CRT/CRT_PostProcess.mat");
            if (raycaster != null && crtMaterial != null)
            {
                var so = new SerializedObject(raycaster);
                so.FindProperty("_crtMaterial").objectReferenceValue = crtMaterial;
                so.ApplyModifiedProperties();
                PrefabUtility.SavePrefabAsset(savedPrefab);
            }

            return savedPrefab;
        }

        private static void PlaceInstance(Transform parent, GameObject prefab, string instanceName,
            float xMin, float yMin, float xMax, float yMax)
        {
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            var rt = (RectTransform)instance.transform;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject EnsureElementPrefab(string fileName, string rootName,
            System.Func<GameObject, Component> addComponent, System.Action<GameObject, Component> configure)
        {
            var path = ElementsFolder + "/" + fileName;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            EnsureFolder(ElementsFolder);

            var go = new GameObject(rootName, typeof(RectTransform));
            var component = addComponent(go);
            configure(go, component);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateTmpText(Transform parent, string name, string placeholder, TMP_FontAsset font,
            int fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = placeholder;
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void Wire(Component component, string fieldName, Object value)
        {
            var so = new SerializedObject(component);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void Wire(Component component, string fieldName, Object[] values)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
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
