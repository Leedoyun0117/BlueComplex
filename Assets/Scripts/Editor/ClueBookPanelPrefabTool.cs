using BlueComplex.UI.Layout;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 단서 정보 책(<see cref="ClueBookPanel"/>)을 Resources/UI/ClueBookPanel.prefab으로 처음 굽는다. 이미 프리팹이 있으면
    /// 아무것도 안 한다 — 그 뒤로는 프리팹을 인스펙터에서 직접 고치는 게 원본이라, 다시 구우면 손 작업을 날린다.
    /// 다시 굽고 싶으면 프리팹을 지운 뒤 실행한다.
    /// </summary>
    public static class ClueBookPanelPrefabTool
    {
        public const string PrefabPath = "Assets/Resources/UI/ClueBookPanel.prefab";

        private const int RingCount = 12;

        private static readonly Color PageBg = new Color32(191, 191, 191, 255);
        private static readonly Color HeaderBg = new Color32(116, 116, 116, 255);
        private static readonly Color TabIdle = new Color32(150, 150, 154, 255);
        private static readonly Color TabActive = new Color32(116, 116, 120, 255);
        private static readonly Color ExcitedColor = new Color32(192, 79, 21, 255);
        private static readonly Color SubduedColor = new Color32(78, 149, 217, 255);
        private static readonly Color LegendBox = new Color32(127, 127, 127, 255);
        private static readonly Color InkLight = Color.white;
        private static readonly Color InkDark = new Color32(30, 30, 32, 255);

        // 파일 철 느낌의 짙은 청록 — 테두리 선, 바인더 링, "시간" 쪽지 색으로 같이 쓴다(사용자가 준 목업에서 잰 색).
        private static readonly Color FileAccent = new Color32(8, 40, 58, 255);
        private static readonly Color TimeCard = new Color32(21, 96, 130, 255);
        private static readonly Color PersonCard = new Color32(78, 149, 217, 255);
        private static readonly Color EmotionCard = new Color32(242, 242, 242, 255);

        [MenuItem("BlueComplex/UI/Bake Clue Book Prefab")]
        public static void Bake()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                Debug.Log($"[ClueBookPanelPrefabTool] 이미 있다 — 건드리지 않음: {PrefabPath}");
                return;
            }

            var font = TmpKoreanFontSetupTool.EnsureKoreanFontAsset();
            var uiLayer = LayerMask.NameToLayer("UI");

            var root = new GameObject("ClueBookPanel", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)root.transform;
                SetStretch(rt, Vector2.zero, Vector2.one);
                var panel = root.AddComponent<ClueBookPanel>();

                Build(root.transform, panel, font);

                if (uiLayer >= 0) SetLayerRecursively(root, uiLayer);
                root.SetActive(false); // Show가 켠다.

                EnsureFolder("Assets/Resources/UI");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ClueBookPanelPrefabTool] 구움: {PrefabPath}");
        }

        private static void Build(Transform root, ClueBookPanel panel, TMP_FontAsset font)
        {
            var so = new SerializedObject(panel);

            var backdrop = CreateImage(root, "Backdrop", new Color(0f, 0f, 0f, 0.8f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = true;
            AddButton(backdrop, panel.Hide);

            var spread = CreateImage(backdrop.transform, "Spread", Color.clear, new Vector2(0.10f, 0.06f), new Vector2(0.97f, 0.94f),
                Vector2.zero, Vector2.zero);
            spread.raycastTarget = true; // 페이지 위 빈 곳을 눌러도 배경까지 새지 않는다(안 닫힘).

            BuildTabs(backdrop.transform, panel, so, font);
            BuildLeftPage(spread.transform, so, font);
            BuildGutter(spread.transform);
            BuildRightPage(spread.transform, panel, so, font);

            // 테두리는 페이지들을 다 얹은 다음 마지막에 그려야 위로 덮이지 않는다(페이지 배경이 가장자리까지 꽉 차 있다).
            AddBorder(spread.transform, FileAccent, 3f);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------
        // 왼쪽 페이지 — 탭과 무관하게 항상 같다: 단서 사진 + 시간/인물/감정 쪽지 세 장.
        // ---------------------------------------------------------------

        private static void BuildLeftPage(Transform spread, SerializedObject so, TMP_FontAsset font)
        {
            var left = CreateImage(spread, "Left Page", PageBg, Vector2.zero, new Vector2(0.495f, 1f), Vector2.zero, Vector2.zero);

            var photoArea = CreateRect(left.transform, "Photo Area", new Vector2(0.32f, 0.55f), new Vector2(0.92f, 0.92f));
            var photo = CreateImage(photoArea, "Photo", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            photo.preserveAspect = true;
            // 확대가 기획에서 빠져 사진은 클릭을 받지 않는다. 꺼도 책이 닫히지는 않는다 —
            // 뒤의 Left Page와 Spread가 레이캐스트를 받아 배경(Backdrop)까지 새지 않는다.
            photo.raycastTarget = false;
            Wire(so, "_icon", photo);

            // 쪽지 세 장 — 시간(뒤) → 인물(가운데) → 감정(맨 앞) 순서로 겹친다(목업 그대로).
            Wire(so, "_timeCardText", BuildTagCard(left.transform, "Time Card", new Vector2(0.05f, 0.28f), new Vector2(0.42f, 0.55f),
                -8f, TimeCard, InkLight, font));
            Wire(so, "_personCardText", BuildTagCard(left.transform, "Person Card", new Vector2(0.28f, 0.18f), new Vector2(0.65f, 0.45f),
                6f, PersonCard, InkLight, font));
            Wire(so, "_emotionCardText", BuildTagCard(left.transform, "Emotion Card", new Vector2(0.03f, 0.02f), new Vector2(0.40f, 0.29f),
                -4f, EmotionCard, InkDark, font));
        }

        private static TMP_Text BuildTagCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            float tiltDegrees, Color bg, Color ink, TMP_FontAsset font)
        {
            var card = CreateImage(parent, name, bg, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees);
            AddBorder(card.transform, new Color(0f, 0f, 0f, 0.55f), 1.5f);

            var text = CreateTmpText(card.transform, "Text", string.Empty, font, 22,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            text.color = ink;
            text.fontStyle = FontStyles.Bold;
            return text;
        }

        // ---------------------------------------------------------------
        // 가운데 — 바인더 링이 늘어선 좁은 홈.
        // ---------------------------------------------------------------

        private static void BuildGutter(Transform spread)
        {
            var gutter = CreateRect(spread, "Gutter", new Vector2(0.495f, 0f), new Vector2(0.515f, 1f));
            var strip = CreateImage(gutter, "Strip", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            strip.raycastTarget = false;

            for (var i = 0; i < RingCount; i++)
            {
                var t = (i + 0.5f) / RingCount;
                var ring = CreateImage(gutter, $"Ring {i}", FileAccent,
                    new Vector2(0.15f, t - 0.02f), new Vector2(0.85f, t + 0.02f), Vector2.zero, Vector2.zero);
                ring.raycastTarget = false;
            }
        }

        // ---------------------------------------------------------------
        // 오른쪽 페이지 — "단서"/"메뉴얼" 탭이 이 안만 바꾼다.
        // ---------------------------------------------------------------

        private static void BuildRightPage(Transform spread, ClueBookPanel panel, SerializedObject so, TMP_FontAsset font)
        {
            var right = CreateImage(spread, "Right Page", PageBg, new Vector2(0.515f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

            BuildClueTabContent(right.transform, so, font);
            BuildManualTabContent(right.transform, so, font);

            var back = CreateTmpText(right.transform, "Back", "돌아가기", font, 20,
                TextAlignmentOptions.MidlineRight, new Vector2(0.4f, 0.02f), new Vector2(0.97f, 0.08f), Vector2.zero, Vector2.zero);
            back.color = InkDark;
            back.raycastTarget = true;
            back.transform.SetAsLastSibling(); // 두 탭 위에 항상 떠 있어야 한다(둘 다에서 눌려야 하므로).
            AddButton(back, panel.Hide);
        }

        private static void BuildClueTabContent(Transform right, SerializedObject so, TMP_FontAsset font)
        {
            var clueTab = CreateRect(right, "Clue Tab", Vector2.zero, Vector2.one);
            Wire(so, "_clueTab", clueTab.gameObject);

            var header = CreateImage(clueTab, "Header", HeaderBg, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.95f),
                Vector2.zero, Vector2.zero);

            var numberBox = CreateImage(header.transform, "Number Box", Color.white,
                new Vector2(0.01f, 0.1f), new Vector2(0.22f, 0.9f), Vector2.zero, Vector2.zero);
            var number = CreateTmpText(numberBox.transform, "Number", "단서 1", font, 20,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            number.color = InkDark;
            Wire(so, "_clueNumberText", number);

            var title = CreateTmpText(header.transform, "Title", string.Empty, font, 22,
                TextAlignmentOptions.Center, new Vector2(0.24f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            title.color = InkLight;
            Wire(so, "_titleText", title);

            var story = CreateTmpText(clueTab, "Story", string.Empty, font, 20,
                TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.82f),
                Vector2.zero, Vector2.zero);
            story.color = InkDark;
            Wire(so, "_storyText", story);
        }

        private static void BuildManualTabContent(Transform right, SerializedObject so, TMP_FontAsset font)
        {
            var manualTab = CreateRect(right, "Manual Tab", Vector2.zero, Vector2.one);
            manualTab.gameObject.SetActive(false);
            Wire(so, "_manualTab", manualTab.gameObject);

            CreateImage(manualTab, "Header", HeaderBg, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.95f),
                Vector2.zero, Vector2.zero);
            var manualTitle = CreateTmpText(manualTab, "Manual Title", "메뉴얼", font, 22,
                TextAlignmentOptions.Center, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);
            manualTitle.color = InkLight;

            BuildLegendGroup(manualTab, "흥분 감정", "행복, 사랑, 분노", ExcitedColor,
                new Vector2(0.03f, 0.58f), new Vector2(0.49f, 0.82f), font);
            BuildLegendGroup(manualTab, "침체 감정", "슬픔, 공포, 혐오", SubduedColor,
                new Vector2(0.51f, 0.58f), new Vector2(0.97f, 0.82f), font);
            BuildLegendGroup(manualTab, "시간대", "과거, 현재, 미래", LegendBox,
                new Vector2(0.03f, 0.35f), new Vector2(0.97f, 0.51f), font);
            BuildLegendGroup(manualTab, "인물", "가족, 친구, 연인, 타인", LegendBox,
                new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.28f), font);
        }

        /// <summary>메뉴얼 항목 하나 — 라벨(박스 위 왼쪽) + 색 박스(가운데 글자).</summary>
        private static void BuildLegendGroup(Transform parent, string label, string body, Color boxColor,
            Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
        {
            var group = CreateRect(parent, label + " Group", anchorMin, anchorMax);

            var labelText = CreateTmpText(group, "Label", label, font, 18,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            labelText.color = InkDark;
            labelText.fontStyle = FontStyles.Bold;

            var box = CreateImage(group, "Box", boxColor, Vector2.zero, new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
            var bodyText = CreateTmpText(box.transform, "Body", body, font, 19,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bodyText.color = InkLight;
        }

        /// <summary>탭 두 개 — 파일 왼쪽 바깥에 붙는다(목업 스타일). 항상 둘 다 보이고, 지금 탭만 진하다.</summary>
        private static void BuildTabs(Transform parent, ClueBookPanel panel, SerializedObject so, TMP_FontAsset font)
        {
            // 탭은 오른쪽이 페이지(Spread, 왼쪽 끝 x=0.10) 안으로 0.04만큼 파고들어야 한다 — 그 겹치는 띠가
            // ClueBookPanel.BringTabToFront의 순서 교체가 눈에 보이는 유일한 곳이다. 가로 앵커를 바꿀 땐 겹침을 유지할 것.
            // 세로 값이 딱 떨어지지 않는 건 예전에 오프셋(anchoredPosition 42,226)으로 밀어 두었던 위치를 앵커로 옮겨 적었기 때문이다 —
            // 오프셋은 픽셀 고정이라 화면 비율이 바뀌면(ScreenMatchMode 0.5) 페이지 대비 위치가 틀어져서 비율로 바꿨다.
            var clueTab = CreateImage(parent, "Tab Clue", TabActive, new Vector2(0.041875f, 0.8092593f), new Vector2(0.14f, 0.8892593f),
                Vector2.zero, Vector2.zero);
            clueTab.raycastTarget = true;
            CreateTmpText(clueTab.transform, "Label", "단서", font, 18,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).color = InkLight;
            AddButton(clueTab, panel.ShowClueTab);
            Wire(so, "_clueTabButton", clueTab);

            var manualTab = CreateImage(parent, "Tab Manual", TabIdle, new Vector2(0.041875f, 0.7092593f), new Vector2(0.14f, 0.7892593f),
                Vector2.zero, Vector2.zero);
            manualTab.raycastTarget = true;
            CreateTmpText(manualTab.transform, "Label", "메뉴얼", font, 18,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).color = InkLight;
            AddButton(manualTab, panel.ShowManualTab);
            Wire(so, "_manualTabButton", manualTab);

            // 처음 열릴 땐 "단서" 탭이라 그 탭이 페이지를 덮고 "메뉴얼"은 페이지 뒤에 있어야 한다
            // (런타임 순서 교체는 ClueBookPanel.BringTabToFront가 한다 — 구운 프리팹의 초기 상태를 거기 맞춘다).
            // 탭 오른쪽이 페이지(Spread) 왼쪽 가장자리와 겹쳐 있어야 이 순서가 눈에 보인다 — 가로 앵커를 바꿀 땐 겹침을 유지할 것.
            manualTab.transform.SetAsFirstSibling();
            clueTab.transform.SetAsLastSibling();
        }

        // ---------------------------------------------------------------
        // 작은 헬퍼들.
        // ---------------------------------------------------------------

        /// <summary>네 변을 얇은 막대 네 개로 둘러 진짜 사각 테두리를 만든다 — Unity Outline 이펙트는 그림자처럼 두 변만
        /// 비쳐서(오프셋 복제라) 쓰지 않는다. target이 회전(쪽지 기울기)돼 있으면 막대도 그 자식이라 같이 돈다.</summary>
        private static void AddBorder(Transform target, Color color, float thickness)
        {
            var border = CreateRect(target, "Border", Vector2.zero, Vector2.one);
            border.SetAsLastSibling();
            var t = new Vector2(thickness, thickness);

            var top = CreateImage(border, "Top", color, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -t.y), Vector2.zero);
            var bottom = CreateImage(border, "Bottom", color, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, t.y));
            var left = CreateImage(border, "Left", color, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(t.x, 0f));
            var right = CreateImage(border, "Right", color, new Vector2(1f, 0f), Vector2.one, new Vector2(-t.x, 0f), Vector2.zero);
            foreach (var bar in new[] { top, bottom, left, right }) bar.raycastTarget = false;
        }

        /// <summary>색 트랜지션·키보드 내비게이션 없는 클릭 전용 Button — 기존 "클릭만 받는" 동작 그대로다.</summary>
        private static void AddButton(Graphic target, UnityEngine.Events.UnityAction onClick)
        {
            var button = target.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.targetGraphic = target;
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }

        private static void Wire(SerializedObject so, string field, Object value)
        {
            so.FindProperty(field).objectReferenceValue = value;
        }

        private static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            SetStretch(rt, anchorMin, anchorMax);
            return rt;
        }

        private static Image CreateImage(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateTmpText(Transform parent, string name, string placeholder, TMP_FontAsset font,
            int fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = placeholder;
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            return text;
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
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
