using System.Collections.Generic;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 1단계 골격이 남긴 빈 색 패널을 걷어내고, 디자인 목업(책상 위에 종이·포스트잇·카드를 붙여둔 느낌)의 배치와 모양으로 맞춘다.
    /// UI 배경은 투명하거나 종이/포스트잇 색 — 3D 배경은 요소 사이로 그대로 비친다.
    ///
    /// 이미 굽힌 프리팹을 <b>제자리에서 고친다</b> — UiLayoutSetupTool은 프리팹이 있으면 다시 만들지 않고, 다시 구우면 씬 인스턴스에 걸린
    /// 오버라이드(StageBootstrapper 참조 등)의 fileID가 바뀌어 배선이 끊기기 때문이다. 여러 번 돌려도 같은 결과가 나온다(멱등):
    /// 자식은 이름으로 찾아 없으면 만들고 있으면 값만 다시 맞춘다.
    /// UiLayoutSetupTool이 프리팹을 처음부터 새로 구울 때도 마지막에 이 도구를 돌려 최종 모양을 한 곳에서 정한다.
    ///
    /// 위치는 캔버스 비율 앵커(1920x1080 기준, 아래쪽 원점). 바꾸려면 아래 <see cref="Anchors"/>만 고치면 된다.
    /// 표의 좌표는 목업의 (왼쪽, 위쪽, 너비, 높이) — 위쪽 원점 — 이고 <see cref="Box"/>가 앵커로 바꾼다.
    /// 키 카드와 쿼터 진행 포스트잇은 프리팹이 아니라 코드로 짓는 HUD(QuarterHud)가 자기 앵커를 쥔다.
    /// </summary>
    public static class UiLayoutCleanupTool
    {
        private const string ElementsFolder = "Assets/Prefabs/UI/Elements/";
        private const string MainHudPath = "Assets/Prefabs/UI/MainHud.prefab";

        private const int ItemSlotCount = 4;

        /// <summary>목업 표의 (왼쪽, 위쪽, 너비, 높이)를 앵커(아래쪽 원점)로 바꾼다.</summary>
        private static (Vector2 Min, Vector2 Max) Box(float left, float top, float width, float height) =>
            (new Vector2(left, 1f - top - height), new Vector2(left + width, 1f - top));

        private static class Anchors
        {
            public static readonly (Vector2 Min, Vector2 Max) StageTitle = Box(0.03f, 0.00f, 0.11f, 0.08f);

            // 목업 표는 H 0.13이지만 스크린샷 실측은 0.123 — 아래 끝이 쿼터 진행 포스트잇(T 0.14)과 겹치지 않게 0.125로 잡는다.
            public static readonly (Vector2 Min, Vector2 Max) Monitor = Box(0.27f, 0.01f, 0.39f, 0.125f);

            public static readonly (Vector2 Min, Vector2 Max) Items = Box(0.87f, 0.03f, 0.10f, 0.69f);
            public static readonly (Vector2 Min, Vector2 Max) Status = Box(0.02f, 0.57f, 0.12f, 0.19f);
            public static readonly (Vector2 Min, Vector2 Max) Trait = Box(0.40f, 0.585f, 0.07f, 0.03f);
            public static readonly (Vector2 Min, Vector2 Max) Dialogue = Box(0.01f, 0.77f, 0.47f, 0.19f);
            public static readonly (Vector2 Min, Vector2 Max) Clues = Box(0.50f, 0.74f, 0.46f, 0.21f);

            // 기억 풍선: 목업 표의 T는 0.17~0.20(슬라이드마다 다르다). 가운데 값.
            public static readonly (Vector2 Min, Vector2 Max) Memory = Box(0.21f, 0.19f, 0.23f, 0.39f);

            /// <summary>엑스레이 판넬이 펼쳐진 자리 — 목업에는 없는 일시적 요소라, 어떤 상시 요소(스테이지 표기·기억 풍선·컴플렉스 포스트잇)와도 안 겹치는 왼쪽 위 빈 자리에 둔다.</summary>
            public static readonly (Vector2 Min, Vector2 Max) Xray = Box(0.02f, 0.18f, 0.18f, 0.37f);

            /// <summary>보이지 않는 드롭 영역 — 엑스레이 판넬을 끌어다 유키 위에 놓는 조건(작동 조건 1)에 쓴다. 목업의 유키 초상화 자리(왼쪽)다.</summary>
            public static readonly (Vector2 Min, Vector2 Max) YukiDrop = Box(0.08f, 0.34f, 0.14f, 0.34f);
        }

        // 심박수 모니터 안의 구역(모니터 폭·높이 대비, 아래쪽 원점). 스크린샷에서 잰 값.
        private static readonly Rect EcgScreenInMonitor = Rect.MinMaxRect(0.039f, 0.163f, 0.720f, 0.888f);
        private static readonly Rect BpmScreenInMonitor = Rect.MinMaxRect(0.738f, 0.163f, 0.985f, 0.888f);

        private static readonly Color DarkPanel = new Color32(8, 12, 18, 170);
        private static readonly Color DarkRow = new Color32(16, 20, 30, 185);
        private static readonly Color EndOverlay = new Color32(6, 8, 14, 140);
        private static readonly Color FoldTabColor = new Color32(74, 133, 222, 255);
        private static readonly Color StickyGlow = new Color32(255, 150, 60, 215);
        private static readonly Color LightLabel = new Color32(226, 230, 240, 255);

        [MenuItem("BlueComplex/UI/Apply Layout Cleanup")]
        public static void Apply()
        {
            var font = TmpKoreanFontSetupTool.EnsureKoreanFontAsset();
            UiMotionSettingsTool.Ensure();

            EditElement("HeartRateIndicatorPanel", root => CleanHeartMonitor(root, font));
            EditElement("BpmDisplay", root => CleanBpmDisplay(root, font));
            EditElement("ItemDisplayPanel", root => CleanItemPanel(root, font));
            EditElement("PortraitView", CleanPortrait);
            EditElement("ComplexXrayPanel", CleanXrayPanel);
            EditElement("ClueCardTray", root => CleanClueTray(root, font));
            EditElement("MemorySpaceBubble", CleanMemoryBubble);
            EditElement("DialogueText", root => CleanDialogue(root, font));
            EditElement("StageEndPanel", CleanStageEnd);

            var status = UiLayoutSetupTool.EnsureComplexStatusPrefab();
            EditElement("ComplexStatusPanel", root => CleanComplexStatus(root, font));
            Edit(MainHudPath, root => CleanMainHud(root, status, font));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiLayoutCleanupTool] 레이아웃 정리 완료: placeholder 패널 제거, 목업 배치 적용.");
        }

        /// <summary>배치모드용 진입점(-executeMethod).</summary>
        public static void ApplyBatch() => Apply();

        // ---------------------------------------------------------------
        // 심박수 모니터
        // ---------------------------------------------------------------

        /// <summary>
        /// 색 구간 막대·0~200 눈금 띠를 걷어내고 심전도 모니터로 바꾼다: 모니터 몸체 + 왼쪽 화면(심전도 파형 + 목표 띠, "목표 N~M" 글자) + 오른쪽 화면 바탕.
        /// 왼쪽 화면 전체가 파형 그래픽이다 — 세로축은 심박수 눈금(HeartbeatMonitorScale)이라 목표 띠도 별도 오브젝트 없이 그 그래픽 안에서 같은 눈금으로 그려진다.
        /// 오른쪽 화면의 글자(BPM 숫자·"BPM"·상태 배지)는 BPM Display 프리팹이 그 위에 얹는다.
        /// </summary>
        private static void CleanHeartMonitor(GameObject root, TMP_FontAsset font)
        {
            foreach (var legacy in new[] { "KeyZoneRow", "BarArea", "FatalLabelRow" })
                Remove(root.transform, legacy);

            var caseRect = (RectTransform)root.transform.Find("Background");
            var caseImage = caseRect.GetComponent<Image>();
            caseImage.color = MockupStyle.MonitorCase;
            caseImage.raycastTarget = false;
            MockupStyle.AddPaperEdge(caseRect.gameObject);

            var screen = EnsureImage(root.transform, "EcgScreen", MockupStyle.MonitorScreen, EcgScreenInMonitor.min, EcgScreenInMonitor.max);

            // 예전 0~200 눈금 띠(구역 탭·즉사 끝·마커 포함)는 목표 띠가 파형과 같은 화면 안으로 들어오면서 필요 없어졌다.
            Remove(screen.transform, "TargetStrip");

            var waveRect = Ensure(screen.transform, "EcgWave");
            SetRect(waveRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var wave = GetOrAdd<EcgWaveGraphic>(waveRect.gameObject);
            wave.color = MockupStyle.Cyan;
            wave.raycastTarget = false;

            var targetLabel = EnsureText(screen.transform, "TargetLabel", string.Empty, font, 16f, MockupStyle.Cyan,
                TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.72f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);
            targetLabel.transform.SetAsLastSibling();

            EnsureImage(root.transform, "ScreenRight", MockupStyle.MonitorScreen, BpmScreenInMonitor.min, BpmScreenInMonitor.max);

            var view = root.GetComponent<HeartRateBarView>();
            SetRefs(view, "_keyZoneLabels", new Object[] { targetLabel });
            SetRef(view, "_ecg", wave);
        }

        /// <summary>모니터 오른쪽 화면 위 글자: 큰 BPM 숫자 + 작은 "BPM" + 아래 상태 배지. 색은 런타임에 심박수 구간을 따른다.</summary>
        private static void CleanBpmDisplay(GameObject root, TMP_FontAsset font)
        {
            var number = EnsureText(root.transform, "Label", "80", font, 58f, MockupStyle.Cyan, TextAlignmentOptions.Center,
                new Vector2(0f, 0.32f), new Vector2(0.64f, 1f), Vector2.zero, Vector2.zero);

            EnsureText(root.transform, "Unit", "BPM", font, 22f, new Color32(72, 222, 234, 200), TextAlignmentOptions.MidlineLeft,
                new Vector2(0.62f, 0.58f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);

            var badge = EnsureImage(root.transform, "Badge", Color.Lerp(MockupStyle.MonitorScreen, MockupStyle.Cyan, 0.2f),
                new Vector2(0.10f, 0.05f), new Vector2(0.90f, 0.34f));
            var outline = GetOrAdd<Outline>(badge.gameObject);
            outline.effectColor = MockupStyle.Cyan;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;

            var badgeText = EnsureText(badge.transform, "State", "안정", font, 20f, MockupStyle.Cyan, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var view = root.GetComponent<BpmDisplay>();
            SetRef(view, "_label", number);
            SetRef(view, "_badgeText", badgeText);
            SetRef(view, "_badgeFill", badge);
            SetRef(view, "_badgeOutline", outline);
        }

        // ---------------------------------------------------------------
        // 아이템
        // ---------------------------------------------------------------

        /// <summary>흰 종이 패널: 머리글 "아이템" + 아이콘·이름 카드 4칸을 위에서부터 쌓고, 왼쪽 옆구리에 "접기" 탭을 붙인다. 비어 있는 슬롯은 런타임에 ItemSlotView가 감춘다.</summary>
        private static void CleanItemPanel(GameObject root, TMP_FontAsset font)
        {
            var panel = GetOrAdd<Image>(root);
            panel.color = MockupStyle.Paper;
            panel.raycastTarget = true;
            MockupStyle.AddPaperEdge(root);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 96, 16);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = EnsureText(root.transform, "Header", "아이템", font, 36f, MockupStyle.Ink, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -88f), new Vector2(0f, -12f), FontStyles.Bold);
            GetOrAdd<LayoutElement>(header.gameObject).ignoreLayout = true;

            var slots = new List<ItemSlotView>(root.GetComponentsInChildren<ItemSlotView>(true));
            while (slots.Count < ItemSlotCount)
            {
                var clone = Object.Instantiate(slots[0].gameObject, root.transform);
                slots.Add(clone.GetComponent<ItemSlotView>());
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.gameObject.name = $"Slot {i}";

                var element = GetOrAdd<LayoutElement>(slot.gameObject);
                element.minHeight = 132f;
                element.preferredHeight = 132f;

                var background = slot.GetComponent<Image>();
                background.color = MockupStyle.Card;
                background.raycastTarget = true;
                MockupStyle.AddPaperEdge(slot.gameObject, shadow: false);

                var icon = EnsureImage(slot.transform, "Icon", Color.white, new Vector2(0.22f, 0.36f), new Vector2(0.78f, 0.94f));
                icon.preserveAspect = true;
                icon.enabled = false; // 스프라이트는 런타임에 Render가 입힌다 — 그 전에 흰 사각형이 비치지 않게.
                icon.transform.SetAsFirstSibling();

                var label = EnsureText(slot.transform, "Name", string.Empty, font, 22f, MockupStyle.Ink, TextAlignmentOptions.Center,
                    new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.36f), Vector2.zero, Vector2.zero, FontStyles.Bold);
                label.textWrappingMode = TextWrappingModes.Normal;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16f;
                label.fontSizeMax = 22f;

                SetRef(slot, "_iconImage", icon);
                SetRef(slot, "_nameText", label);
            }

            // 접기 탭: 패널 왼쪽 옆구리(목업: 패널 왼쪽 밖, 위에서 약 0.27 지점).
            var tab = EnsureImage(root.transform, "FoldTab", FoldTabColor, new Vector2(0f, 0.732f), new Vector2(0f, 0.732f), raycast: true);
            tab.rectTransform.pivot = new Vector2(1f, 0.5f);
            tab.rectTransform.sizeDelta = new Vector2(75f, 97f);
            tab.rectTransform.anchoredPosition = new Vector2(-13f, 0f);
            GetOrAdd<LayoutElement>(tab.gameObject).ignoreLayout = true;
            MockupStyle.AddPaperEdge(tab.gameObject);

            var button = GetOrAdd<Button>(tab.gameObject);
            button.targetGraphic = tab;

            var tabLabel = EnsureText(tab.transform, "Label", "접기", font, 28f, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 접힌 뒤의 "펼치기"(3글자)가 75px 탭에 들어가도록.
            tabLabel.enableAutoSizing = true;
            tabLabel.fontSizeMin = 18f;
            tabLabel.fontSizeMax = 28f;

            var panelView = root.GetComponent<ItemDisplayPanel>();
            var property = new SerializedObject(panelView).FindProperty("_slots");
            property.serializedObject.Update();
            property.arraySize = slots.Count;
            for (var i = 0; i < slots.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            property.serializedObject.ApplyModifiedProperties();

            SetRef(panelView, "_foldButton", button);
            SetRef(panelView, "_foldLabel", tabLabel);
        }

        /// <summary>Portrait 아트가 없으므로 아무것도 그리지 않는다(배경의 인물 스프라이트가 이미 유키·나츠를 그린다 — 목업의 초상화는 방향 제시용).
        /// 이미지 컴포넌트는 남겨 둔다 — 유키 쪽은 엑스레이 판넬 드롭 영역으로 쓰이고, 투명해도 레이캐스트는 받는다.</summary>
        private static void CleanPortrait(GameObject root)
        {
            var image = root.GetComponentInChildren<Image>(true);
            image.color = Color.clear;
            image.raycastTarget = true;
        }

        // ---------------------------------------------------------------
        // 컴플렉스
        // ---------------------------------------------------------------

        /// <summary>엑스레이 판넬(단서를 집으면 펼쳐지는 일시적 요소): 목록을 좁은 세로 띠로 — 이름 + 남은 턴 막대(폭 제한) + 남은 턴 숫자. 설명은 호버 팝업에만 뜬다.</summary>
        private static void CleanXrayPanel(GameObject root)
        {
            root.transform.Find("Background").GetComponent<Image>().color = DarkPanel;

            var list = root.transform.Find("ComplexList");
            var layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;

            foreach (var row in list.GetComponentsInChildren<ComplexRowView>(true))
            {
                var rowTransform = row.transform;
                GetOrAdd<LayoutElement>(row.gameObject).preferredHeight = 56f;
                row.GetComponent<Image>().color = DarkRow;

                var description = rowTransform.Find("Description");
                if (description != null) Object.DestroyImmediate(description.gameObject);

                // 막대는 행 폭의 62%까지만 — 화면 폭 전체로 늘어나지 않는다.
                SetRect(rowTransform.Find("Name"), new Vector2(0f, 0.40f), new Vector2(0.62f, 1f), new Vector2(10f, 0f), new Vector2(0f, -4f));
                SetRect(rowTransform.Find("DurationBarBg"), new Vector2(0f, 0.12f), new Vector2(0.62f, 0.30f), new Vector2(10f, 0f), Vector2.zero);
                SetRect(rowTransform.Find("DurationNumber"), new Vector2(0.64f, 0f), Vector2.one, Vector2.zero, new Vector2(-10f, 0f));

                StyleLabel(rowTransform.Find("Name"), 20f, TextAlignmentOptions.MidlineLeft);
                StyleLabel(rowTransform.Find("DurationNumber"), 20f, TextAlignmentOptions.MidlineRight);
            }
        }

        /// <summary>
        /// 컴플렉스 상시 표시 = 목업의 왼쪽 노란 포스트잇: "컴플렉스" 제목 + "이름 N턴" 목록(막대 없음). 행을 누르거나 올리면 상세 팝업이 뜬다(ComplexRowView).
        /// 남은 턴 막대는 목업에 없어 뺐다 — 기획서 원문(UI 가이드 8번)은 "막대의 길이, 텍스트"로 적혀 있어 어긋난다(보고 항목).
        /// </summary>
        private static void CleanComplexStatus(GameObject root, TMP_FontAsset font)
        {
            var note = GetOrAdd<Image>(root);
            note.color = MockupStyle.Sticky;
            note.raycastTarget = false;
            MockupStyle.AddShadow(root);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, 3f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 58, 10);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = EnsureText(root.transform, "Title", "컴플렉스", font, 26f, MockupStyle.Ink, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -52f), new Vector2(-16f, -10f), FontStyles.Bold);
            GetOrAdd<LayoutElement>(title.gameObject).ignoreLayout = true;

            foreach (var row in root.GetComponentsInChildren<ComplexRowView>(true))
            {
                var rowTransform = row.transform;
                GetOrAdd<LayoutElement>(row.gameObject).preferredHeight = 32f;

                // 배경은 투명(호버/클릭용 레이캐스트만). 발광 색은 노란 종이 위에서 보이는 주황이고, 기본색을 같은 RGB의 투명으로 둬서 알파만 오르내린다.
                var background = row.GetComponent<Image>();
                background.color = new Color(StickyGlow.r, StickyGlow.g, StickyGlow.b, 0f);
                background.raycastTarget = true;

                Remove(rowTransform, "DurationBarBg");
                SetRef(row, "_durationFill", null);
                SetColor(row, "_glowColor", StickyGlow);

                var name = EnsureText(rowTransform, "Name", string.Empty, font, 22f, MockupStyle.Ink, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 0f), new Vector2(0.66f, 1f), new Vector2(12f, 0f), Vector2.zero, FontStyles.Bold);
                var turns = EnsureText(rowTransform, "DurationNumber", string.Empty, font, 22f, MockupStyle.Ink, TextAlignmentOptions.MidlineRight,
                    new Vector2(0.66f, 0f), Vector2.one, Vector2.zero, new Vector2(-12f, 0f), FontStyles.Bold);
                foreach (var text in new[] { name, turns })
                {
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                }

                // 이름이 길면 줄어들어 한 줄에 들어가게 한다(목업의 짧은 이름보다 긴 컴플렉스가 있다).
                name.enableAutoSizing = true;
                name.fontSizeMin = 15f;
                name.fontSizeMax = 22f;
                SetBool(row, "_trimComplexSuffix", true);

                SetRef(row, "_nameText", name);
                SetRef(row, "_durationText", turns);
            }
        }

        // ---------------------------------------------------------------
        // 단서
        // ---------------------------------------------------------------

        /// <summary>흰 종이 패널: 머리글 "단서" + 카드 4장(아이콘 + 이름만). 태그와 스토리는 카드를 클릭하면 뜨는 단서 정보 책 UI가 보여 준다.</summary>
        private static void CleanClueTray(GameObject root, TMP_FontAsset font)
        {
            var panel = GetOrAdd<Image>(root);
            panel.color = MockupStyle.Paper;
            panel.raycastTarget = true;
            MockupStyle.AddPaperEdge(root);

            var layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 66, 24);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var header = EnsureText(root.transform, "Header", "단서", font, 34f, MockupStyle.Ink, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 1f), Vector2.one, new Vector2(28f, -62f), new Vector2(-28f, -14f), FontStyles.Bold);
            GetOrAdd<LayoutElement>(header.gameObject).ignoreLayout = true;

            foreach (var card in root.GetComponentsInChildren<ClueCardView>(true))
            {
                var background = card.GetComponent<Image>();
                background.color = MockupStyle.Card;
                MockupStyle.AddPaperEdge(card.gameObject, shadow: false);

                Remove(card.transform, "Attributes");
                Remove(card.transform, "Story");
                Remove(card.transform, "Uses");
                SetRef(card, "_attributesText", null);
                SetRef(card, "_storyText", null);
                SetRef(card, "_usesText", null);

                var icon = EnsureImage(card.transform, "Icon", Color.white, new Vector2(0.24f, 0.36f), new Vector2(0.76f, 0.94f));
                icon.preserveAspect = true;
                icon.enabled = false; // 스프라이트는 런타임에 Render가 입힌다 — 그 전에 흰 사각형이 비치지 않게.
                icon.transform.SetAsFirstSibling();

                var title = EnsureText(card.transform, "Title", string.Empty, font, 22f, MockupStyle.Ink, TextAlignmentOptions.Center,
                    new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.36f), Vector2.zero, Vector2.zero, FontStyles.Bold);
                title.textWrappingMode = TextWrappingModes.Normal;
                title.enableAutoSizing = true;
                title.fontSizeMin = 16f;
                title.fontSizeMax = 22f;

                SetRef(card, "_iconImage", icon);
                SetRef(card, "_titleText", title);
            }
        }

        // ---------------------------------------------------------------
        // 기억 풍선, 대사, 종료
        // ---------------------------------------------------------------

        /// <summary>손으로 그린 흰 선 하나(스프라이트는 MemorySpaceBubble이 런타임에 입힌다). 평소엔 흐리고 단서를 집으면 또렷하다.</summary>
        private static void CleanMemoryBubble(GameObject root)
        {
            var bubble = root.transform.Find("Bubble").GetComponent<Image>();
            bubble.color = new Color(1f, 1f, 1f, 0.55f);
            bubble.raycastTarget = false;

            // 최종 감정 요약은 선 안쪽 아래에 둔다.
            var summary = root.transform.Find("PersistentSummary");
            SetRect(summary, new Vector2(0.24f, 0.12f), new Vector2(0.80f, 0.30f), Vector2.zero, Vector2.zero);
            StyleLabel(summary, 18f, TextAlignmentOptions.Center);
        }

        /// <summary>흰 종이 패널: 왼쪽 위에 이름표("유키"), 그 아래 대사. 오른쪽 아래의 "다음" 표시(▼)는 DialogueText가 런타임에 짓는다.
        /// 배경이 클릭(스킵) 감지용 레이캐스트 배경 역할을 그대로 한다.</summary>
        private static void CleanDialogue(GameObject root, TMP_FontAsset font)
        {
            var backdrop = root.transform.Find("ClickCatcher").GetComponent<Image>();
            backdrop.color = MockupStyle.Paper;
            backdrop.raycastTarget = true;
            MockupStyle.AddPaperEdge(backdrop.gameObject);

            var label = root.transform.Find("Label").GetComponent<TMP_Text>();
            SetRect(label.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.64f), Vector2.zero, Vector2.zero);
            label.fontSize = 32f;
            label.color = MockupStyle.Ink;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;

            var tag = EnsureImage(root.transform, "NameTag", MockupStyle.Card, new Vector2(0.034f, 0.69f), new Vector2(0.165f, 0.925f));
            MockupStyle.AddPaperEdge(tag.gameObject, shadow: false);
            EnsureText(tag.transform, "Name", "유키", font, 28f, MockupStyle.Ink, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyles.Bold);
        }

        /// <summary>스테이지가 끝나면 화면 전체를 덮는 오버레이 — 거의 불투명하면 3D 배경이 통째로 사라지므로 반투명으로 둔다(글자·버튼은 그 위에서 그대로 읽힌다).</summary>
        private static void CleanStageEnd(GameObject root)
        {
            root.transform.Find("Overlay").GetComponent<Image>().color = EndOverlay;
        }

        // ---------------------------------------------------------------
        // MainHud
        // ---------------------------------------------------------------

        private static void CleanMainHud(GameObject root, GameObject statusPrefab, TMP_FontAsset font)
        {
            var hud = root.transform;

            // 요소 프리팹에서 지운 오브젝트(행의 설명 글자, 예전 구간 막대 등)에 걸려 있던 오버라이드를 먼저 걷어낸다 — 남아 있으면 그 인스턴스에 새 오버라이드가 안 남는다.
            RemoveStaleOverrides(root);

            UseCinematicPresenter(hud);

            // 컴플렉스 상시 표시는 엑스레이 판넬 바로 뒤에 쌓는다 — 공유 툴팁(MainHud 자식)이 그 위에 그려져야 한다.
            EnsureInstance(hud, "Complex Status", statusPrefab, afterSibling: "Complex X-ray Panel");

            Place(hud, "Heart Rate Indicator", Anchors.Monitor);
            Place(hud, "BPM Display", Within(Anchors.Monitor, BpmScreenInMonitor));
            Place(hud, "Item Display", Anchors.Items);
            Place(hud, "Complex Status", Anchors.Status);
            Place(hud, "Complex X-ray Panel", Anchors.Xray);
            Place(hud, "Clue Card Tray", Anchors.Clues);
            Place(hud, "Memory Space Bubble", Anchors.Memory);
            Place(hud, "Yuki Dialogue Text", Anchors.Dialogue);

            // 스테이지 표기와 특성 표시는 프리팹 없이 MainHud에 바로 둔다 — 글자 하나짜리라 따로 재사용할 일이 없다.
            EnsureLabelView<StageTitleView>(hud, "Stage Title", Anchors.StageTitle, "STAGE 01\n가라앉다", font, 36f, LightLabel,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            EnsureLabelView<TraitStatusView>(hud, "Trait Status", Anchors.Trait, "특성 없음", font, 22f, new Color32(222, 228, 240, 255),
                TextAlignmentOptions.Center, FontStyles.Normal);

            // 내용 없이 색만 채운 자리 — 지속시간은 컴플렉스 포스트잇이 보여주고, 나츠는 배경 스프라이트가 그린다.
            Remove(hud, "Complex Duration Display");
            Remove(hud, "Natsu Portrait");

            // 유키 드롭 영역은 맨 밑에 깐다 — 투명하지만 레이캐스트를 받으므로 위에 있으면 엑스레이 판넬/카드 입력을 가로챈다.
            var yuki = Place(hud, "Yuki Portrait", Anchors.YukiDrop);
            if (yuki != null) yuki.SetAsFirstSibling();

            // 요소 프리팹은 기본 레이어라, MainHud가 UI 레이어(UI 카메라가 그리는 유일한 레이어)를 인스턴스 오버라이드로 입혀 둔다.
            // 새로 만든 슬롯에도 같은 오버라이드가 필요하다 — 없으면 화면에 안 나온다.
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) SetLayerRecursively(root, uiLayer);

            // 새로 생긴 슬롯까지 공유 팝업을 물린다(기존 슬롯은 이미 물려 있다).
            var tooltip = root.GetComponentInChildren<TooltipPopup>(true);
            if (tooltip == null) return;

            var items = root.GetComponentInChildren<ItemDisplayPanel>(true);
            for (var i = 0; items != null && i < items.SlotCount; i++)
                SetTooltip(items.GetSlot(i), tooltip);

            // 컴플렉스 목록(엑스레이 판넬 안, 포스트잇)도 같은 공유 팝업을 쓴다.
            foreach (var list in root.GetComponentsInChildren<ComplexListView>(true))
                SetTooltip(list, tooltip);

            // 새 글자 뷰는 툴팁 팝업 밑에 둔다 — 팝업이 항상 맨 위여야 한다.
            foreach (var name in new[] { "Stage Title", "Trait Status" })
            {
                var view = hud.Find(name);
                if (view != null && view.GetSiblingIndex() > tooltip.transform.GetSiblingIndex())
                    view.SetSiblingIndex(tooltip.transform.GetSiblingIndex());
            }
        }

        /// <summary>부모 영역 안의 부분 영역(부모 폭·높이 대비 비율)을 앵커로 돌려준다.</summary>
        private static (Vector2 Min, Vector2 Max) Within((Vector2 Min, Vector2 Max) parent, Rect fraction)
        {
            var size = parent.Max - parent.Min;
            return (parent.Min + Vector2.Scale(size, fraction.min), parent.Min + Vector2.Scale(size, fraction.max));
        }

        private static void EnsureLabelView<T>(Transform hud, string name, (Vector2 Min, Vector2 Max) anchors, string sample,
            TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment, FontStyles style) where T : Component
        {
            var rect = Ensure(hud, name);
            SetRect(rect, anchors.Min, anchors.Max, Vector2.zero, Vector2.zero);

            var label = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            if (font != null) label.font = font;
            label.text = sample;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            SetRef(GetOrAdd<T>(rect.gameObject), "_label", label);
        }

        /// <summary>턴 결과를 즉시 반영하던 2단계 Presenter를 3단계 연출 Presenter로 갈아끼운다 — 컴플렉스 발광·이벤트 대사·태그 상승·심박수 이동·아이템 카드 끼우기가 전부 이 Presenter가 잡는 순서로 돈다.
        /// 필드 참조는 Presenter가 같은 HUD 아래에서 스스로 찾는다(CinematicTurnResultPresenter.ResolveReferences). 이미 갈아끼워져 있으면 아무것도 안 한다(멱등).</summary>
        private static void UseCinematicPresenter(Transform hud)
        {
            var holder = hud.Find("Turn Result Presenter");
            if (holder == null)
            {
                Debug.LogWarning("[UiLayoutCleanupTool] MainHud에서 'Turn Result Presenter'를 찾지 못했다 — 연출 Presenter를 못 붙였다.");
                return;
            }

            var immediate = holder.GetComponent<ImmediateTurnResultPresenter>();
            if (immediate != null) Object.DestroyImmediate(immediate);
            GetOrAdd<CinematicTurnResultPresenter>(holder.gameObject);
        }

        private static void SetTooltip(Component target, TooltipPopup tooltip) => SetRef(target, "_tooltip", tooltip);

        /// <summary>이미 있으면 그대로 두고, 없으면 프리팹 인스턴스를 만들어 지정한 형제 바로 뒤에 끼운다.</summary>
        private static void EnsureInstance(Transform parent, string instanceName, GameObject prefab, string afterSibling)
        {
            if (parent.Find(instanceName) != null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;

            var anchor = parent.Find(afterSibling);
            if (anchor != null) instance.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void RemoveStaleOverrides(GameObject root)
        {
            var instances = new List<GameObject>();
            foreach (Transform child in root.transform)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) instances.Add(child.gameObject);
            }

            PrefabUtility.RemoveUnusedOverrides(instances.ToArray(), InteractionMode.AutomatedAction);
        }

        private static RectTransform Place(Transform parent, string childName, (Vector2 Min, Vector2 Max) anchors)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                Debug.LogWarning($"[UiLayoutCleanupTool] MainHud에서 '{childName}'을(를) 찾지 못했다 — 건너뜀.");
                return null;
            }

            var rect = (RectTransform)child;
            rect.anchorMin = anchors.Min;
            rect.anchorMax = anchors.Max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Remove(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        // ---------------------------------------------------------------
        // 공용
        // ---------------------------------------------------------------

        private static void EditElement(string name, System.Action<GameObject> edit) => Edit(ElementsFolder + name + ".prefab", root =>
        {
            edit(root);

            // 요소 프리팹도 UI 레이어로 둔다(MainHud의 인스턴스 오버라이드와 값이 같아 충돌하지 않는다) — 새로 만든 자식이 기본 레이어로 남지 않게.
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) SetLayerRecursively(root, uiLayer);
        });

        private static void Edit(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        /// <summary>이름으로 자식을 찾고 없으면 RectTransform만 든 빈 오브젝트를 만든다(비활성 자식도 찾는다).</summary>
        private static RectTransform Ensure(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image EnsureImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            bool raycast = false)
        {
            var rect = Ensure(parent, name);
            SetRect(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            var image = GetOrAdd<Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TMP_Text EnsureText(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            FontStyles style = FontStyles.Normal)
        {
            var rect = Ensure(parent, name);
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);

            var label = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static void StyleLabel(Transform child, float size, TextAlignmentOptions alignment)
        {
            var label = child.GetComponent<TMP_Text>();
            label.fontSize = size;
            label.alignment = alignment;
            label.raycastTarget = false;
        }

        private static void SetRect(Transform child, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = (RectTransform)child;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetRef(Component target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning($"[UiLayoutCleanupTool] {target.GetType().Name}에 '{field}' 필드가 없다 — 건너뜀.");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetRefs(Component target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }

        private static void SetBool(Component target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetColor(Component target, string field, Color value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).colorValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
