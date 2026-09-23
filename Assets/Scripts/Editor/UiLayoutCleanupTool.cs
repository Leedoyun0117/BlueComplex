using System.Collections.Generic;
using BlueComplex.Core.Complexes;
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

            // 기억 풍선: 목업 표의 T는 0.17~0.20(슬라이드마다 다르다). 원래 이 가운데 값을 썼는데, 엑스레이 뇌/얼굴 자리(YukiDrop)와 정면으로
            // 겹쳐서(뇌를 가로지르는 흰 선) 뇌 오른쪽 빈 공간으로 옮겼다. 그 옆 좁은 자리엔 쿼터 진행 포스트잇(T 0.115~0.235)과 키 카드(T 0.562~0.712,
            // L 0.61~0.85)가 더 있어서 그 사이 틈에 맞춰 넣었다. 원(Tablet)이 사용자 지시로 왼쪽 위로 옮겨가면서(오른쪽 끝이 L 0.371로 줄었다)
            // 다시 멀어져서, 원 테두리에 딱 붙게 왼쪽으로 당기고 "조금 크게"(0.26×0.28 → 0.28×0.30) 키웠다. L 0.375로 처음 당겼을 때 렌더로 보니
            // 손그림 선의 삐뚤빼뚤함 때문에 국지적으로 틈이 남아 있어서 0.357로 더 당겼다. 이후 "상자가 너무 크다"는 지적으로 원 지름이
            // 500.6→425로 줄면서 오른쪽 끝이 L 0.371→0.351로 더 줄어서, 다시 그 폭만큼(0.020) 따라 당겼다.
            //
            // 폭 0.28 × 높이 0.30은 1920×1080에서 537.6×324px — 정사각형이 아니다. HandDrawnStrokeGraphic이 각 축을 rect.width/height로
            // 따로 늘려 그리던 예전 방식에선 이 비율이 타원을 눌러 "원처럼 보이게" 눈속임했지만, 그래픽을 실제 원(짧은 변 기준 내접 정사각형)으로
            // 고치고 나니 이 상자에 내접하는 원은 짧은 변(324px)만큼만 그려져 왼쪽 끝이 647px(옛 L 그대로)에서 754px로 밀리며 엑스레이 원
            // 오른쪽 끝(674.5px)에서 79px 떨어졌다. L·T·H(왼쪽 끝·세로 자리)는 위 문단대로 이미 세밀하게 맞춰져 있으므로 그대로 두고,
            // W만 H와 같은 픽셀 폭(324/1920=0.16875)으로 줄여 진짜 정사각형(→ 진짜 원)을 만든다.
            //
            // 그런데도 렌더에서 여전히 엑스레이 원과 떨어져 있다(ComplexXrayPanel 상수로 역산한 위치가 실제 화면과 안 맞음 — CRT
            // 배럴 보정 등 이 계산에 안 잡히는 변수가 더 있는 듯하다). 더 역산하는 대신 손으로 맞추는 오프셋을 둔다 —
            // 렌더 보고 아래 두 값만 바꿔가며 값을 잡는다(MemoryOffset.x가 +면 오른쪽/–면 왼쪽, .y가 +면 위/–면 아래로 밀린다).
            // 바꾼 뒤엔 반드시 BlueComplex/UI/Apply Layout Cleanup을 다시 돌려야 MainHud.prefab에 반영된다.
            //
            // 렌더로 확인한 -0.05 오프셋을 기준 좌표에 접어 넣고(0.337→0.287), 그 중심은 그대로 둔 채 지름만 15% 키웠다
            // (0.16875×0.30 → 0.1941×0.345, 둘 다 1920×1080에서 같은 372.7px 정사각형). 오프셋은 다시 0으로 — 위치를 더
            // 밀어야 하면 이 값을 계속 쓴다.
            private static readonly Vector2 MemoryOffset = new Vector2(0f, 0.05f);

            public static readonly (Vector2 Min, Vector2 Max) Memory = Box(0.2743f + MemoryOffset.x, 0.2325f - MemoryOffset.y, 0.1941f, 0.345f);

            // 나츠 초상화(UI 가이드 5번, 목업엔 있지만 지금까지 안 만들어져 있었다 — Notion 스펙 재확인 후 새로 추가).
            // 목업 원본 좌표(L 0.61, W 0.23 근방)는 지금 레이아웃(기억 풍선·키 카드가 그 자리를 이미 차지)과 겹쳐서 못 쓴다 —
            // 기억 풍선 오른쪽 끝(L 0.617)과 키 카드 위(T 0.562) 사이의 빈 자리에 세로로 긴 카드로 새로 잡았다.
            public static readonly (Vector2 Min, Vector2 Max) Natsu = Box(0.665f, 0.25f, 0.137f, 0.30f);

            /// <summary>엑스레이 판넬 컨테이너 — 접힌 위치(스테이지 표기 바로 밑 왼쪽 구석)부터, 펼쳤을 때 화면에 실제로 보이는 유키 초상화(<see cref="YukiDrop"/>)
            /// 위로 뻗는 원 자리까지를 덮는 투명 영역이다. 목업에는 없는 일시적 요소다. 안의 배치는 ComplexXrayPanel이 이 크기(1080p에서 960×918)를
            /// 기준으로 계산한다 — 컨테이너 크기를 바꾸면 그쪽 ReferenceSize도 같이 맞춘다.</summary>
            public static readonly (Vector2 Min, Vector2 Max) Xray = Box(0.00f, 0.02f, 0.50f, 0.85f);

            /// <summary>유키 초상화 — 실제로 보인다(CleanPortrait) + 엑스레이 판넬을 끌어다 놓는 드롭 영역(작동 조건 1)도 겸한다.
            /// <b>기준은 엑스레이 원이다</b>(배경의 3D 캐릭터가 아니라 — 그쪽은 아직 플레이스홀더라 기준으로 쓰지 않는다).
            /// "상자가 너무 크다"는 지적으로 원 중심은 그대로(화면 462, 458.6) 두고 지름만 500.6→425로 줄였다. 동시에 "여백이 줄게"
            /// 머리 채움 비율도 72%→78%로 키웠다: 머리 폭 331.5px(스프라이트 스케일 2.21배).
            /// 스프라이트(163×201) 안에서 머리(머리카락 위~턱)는 (9,3)~(159,147) = 150×144, 머리 중심은 (84,75)다.
            /// 스프라이트 전체는 360×444가 되고, 머리 중심이 원 중심에 오려면 스프라이트 중심을 (456, 515)에 둬야 한다 → 아래 박스.
            /// 크기를 바꿀 땐 ComplexXrayPanel.OpenWrist/TabletSize(원)와 BrainArea(뇌, 채움 비율이 바뀌면 같이 바뀐다)도 같이 맞춘다.</summary>
            public static readonly (Vector2 Min, Vector2 Max) YukiDrop = Box(0.1439f, 0.2712f, 0.1876f, 0.4113f);
        }

        // 심박수 모니터 안의 구역(모니터 폭·높이 대비, 아래쪽 원점). 스크린샷에서 잰 값.
        private static readonly Rect EcgScreenInMonitor = Rect.MinMaxRect(0.039f, 0.163f, 0.720f, 0.888f);
        private static readonly Rect BpmScreenInMonitor = Rect.MinMaxRect(0.738f, 0.163f, 0.985f, 0.888f);

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
            EditElement("ComplexXrayPanel", root => CleanXrayPanel(root, font));
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

        /// <summary>유키 초상화 — 실제로 보인다(사용자 지시: 캐릭터가 화면에 보이게 하고, 그 위치에 뇌를 맞춘다). 엑스레이 판넬을
        /// 여기로 끌어다 놓는 드롭 판정은 그대로 유지한다. 좌우 반전해 엑스레이 원 안의 뇌와 같은 방향(오른쪽)을 보게 한다(사용자 지시).
        /// 컴플렉스가 발동해 뇌가 빛날 때마다 CinematicTurnResultPresenter가 PortraitXrayView.Flash()를 불러 잠깐 반응 표정으로 바뀐다
        /// (예전엔 엑스레이 판 안의 작은 사진에 붙어 있었는데, 그 사진을 없애면서 실제로 보이는 이 초상화로 옮겼다 — CleanMainHud가 연결한다).</summary>
        private static void CleanPortrait(GameObject root)
        {
            var image = root.GetComponentInChildren<Image>(true);
            image.sprite = LoadPortraitSprite("Yuki/yuki_neutral.png");
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.transform.localScale = new Vector3(-1f, 1f, 1f);

            var portraitView = GetOrAdd<PortraitXrayView>(image.gameObject);
            SetRef(portraitView, "_image", image);
            SetRef(portraitView, "_neutral", LoadPortraitSprite("Yuki/yuki_neutral.png"));
            SetRef(portraitView, "_reactive", LoadPortraitSprite("Yuki/yuki_reactive.png"));
        }

        // ---------------------------------------------------------------
        // 컴플렉스
        // ---------------------------------------------------------------

        // 엑스레이 판넬의 금속 틀과 유리, 뇌 영역 글자색. 사용자가 참고 이미지(크림색 베젤 + 관절 팔 의료 모니터)를 보여주고
        // "이런 느낌이면 좋겠다"고 해서 예전 회청색 금속 대신 크림/탄색 팔레트로 바꿨다 — 화면(Glass) 안은 여전히 투명(직전 지시 유지),
        // 진짜 픽셀아트 베벨 음영까지는 흉내 못 내고 크림 톤 + 두꺼운 테두리 + 테두리선(AddPaperEdge)으로 "덩어리진 계기판" 느낌만 근접시켰다.
        private static readonly Color XrayMetal = new Color32(228, 210, 168, 255);
        private static readonly Color XrayMetalDark = new Color32(158, 128, 82, 255);
        // 유리(Glass) 색 — 완전 투명(Color.clear)이면 밋밋해서, 사용자 지시로 "엑스레이 보듯" 옅은 파란 색조를 얹었다. 알파를 낮게
        // 잡아서(약 22%) 그 뒤 캐릭터·뇌가 여전히 또렷이 비치면서 필터를 통해 보는 느낌만 낸다.
        private static readonly Color XrayGlass = new Color32(70, 150, 230, 56);
        private static readonly Color XrayCyan = new Color32(120, 226, 236, 255);
        private static readonly Color BrainLabelInk = new Color32(74, 22, 36, 255);

        /// <summary>도트 뇌 영역(0 위쪽 띠, 1 아래 왼쪽, 2 오른쪽 큰 엽)의 글자 중심(뇌 이미지 대비 0~1, 원점 왼쪽 아래). 아트에서 영역 마스크의 중심을 잰 값이다.
        /// 영역 수는 컴플렉스 최대 중첩(<see cref="ComplexBoard.DefaultMaxSlots"/>)과 같다 — 뇌의 세 부분과 슬롯은 1:1이다.</summary>
        private static readonly Vector2[] BrainLabelCenters =
        {
            new Vector2(0.31f, 0.73f), new Vector2(0.29f, 0.38f), new Vector2(0.72f, 0.51f),
        };

        /// <summary>초상화 원본(yuki_neutral.png/yuki_reactive.png) 크기 163×201의 가로세로 비 — 둘이 같은 크기로 잘라 둬서 표정이 바뀌어도 틀 안에서 안 흔들린다.</summary>
        private const float PortraitAspect = 163f / 201f;

        /// <summary>
        /// 엑스레이 판넬 = 관절 팔에 매달린 금속 틀의 판넬. 루트는 접힌 위치부터 펼친 위치까지를 덮는 투명 컨테이너이고(그래픽 없음),
        /// 안에 어깨 받침대 · 관절 팔(선분 두 개 + 관절 원 세 개) · 접힌 상태 손잡이 안내표 · 판넬(틀 + 유리 + 뇌)이 들어간다.
        /// 뇌는 기획서의 도트 아트(Assets/Art/UI/Brain) 위에 영역별 오버레이 세 엽을 얹고, 영역마다 컴플렉스 이름·남은 턴 글자를 단다.
        /// 위치·크기는 런타임에 ComplexXrayPanel이 접힘/펼침에 맞춰 놓는다 — 여기서는 구조와 참조만 만든다(멱등).
        /// 예전 목록형(Background + ComplexList 행 4개)은 걷어낸다.
        /// </summary>
        private static void CleanXrayPanel(GameObject root, TMP_FontAsset font)
        {
            Remove(root.transform, "Background");
            Remove(root.transform, "ComplexList");
            var oldGroup = root.GetComponent<CanvasGroup>();
            if (oldGroup != null) Object.DestroyImmediate(oldGroup);

            var panel = root.GetComponent<ComplexXrayPanel>();

            var basePlate = Ensure(root.transform, "BasePlate");
            TopLeft(basePlate, new Vector2(0.5f, 0.5f));
            var plateImage = GetOrAdd<Image>(basePlate.gameObject);
            plateImage.color = XrayMetalDark;
            plateImage.raycastTarget = false;
            MockupStyle.AddPaperEdge(basePlate.gameObject);

            // 관절 팔: 선분은 왼쪽 끝이 축(pivot 0, 0.5), 관절은 원(스프라이트는 런타임에 입힌다).
            var armRect = Ensure(root.transform, "Arm");
            SetRect(armRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var arm = GetOrAdd<XrayArm>(armRect.gameObject);
            var upper = EnsureArmPiece(armRect, "Upper", new Vector2(0f, 0.5f), XrayMetal);
            var lower = EnsureArmPiece(armRect, "Lower", new Vector2(0f, 0.5f), XrayMetal);
            var shoulder = EnsureArmPiece(armRect, "Shoulder", new Vector2(0.5f, 0.5f), XrayMetalDark);
            var elbow = EnsureArmPiece(armRect, "Elbow", new Vector2(0.5f, 0.5f), XrayMetalDark);
            var wrist = EnsureArmPiece(armRect, "Wrist", new Vector2(0.5f, 0.5f), XrayMetalDark);
            SetRef(arm, "_upper", upper);
            SetRef(arm, "_lower", lower);
            SetRef(arm, "_shoulder", shoulder);
            SetRef(arm, "_elbow", elbow);
            SetRef(arm, "_wrist", wrist);

            // 접힌 상태 손잡이 안내표: 작은 접힌 판넬 밑에 뜨고 끌 수 있다(레이캐스트 켬 — 이벤트가 루트의 드래그 핸들러까지 올라간다).
            var handleRect = Ensure(root.transform, "HandleTag");
            TopLeft(handleRect, new Vector2(0.5f, 0.5f));
            var handleImage = GetOrAdd<Image>(handleRect.gameObject);
            handleImage.color = new Color32(20, 44, 50, 235);
            handleImage.raycastTarget = true;
            MockupStyle.AddPaperEdge(handleRect.gameObject);
            var handleGroup = GetOrAdd<CanvasGroup>(handleRect.gameObject);
            var handleText = EnsureText(handleRect, "Text", "엑스레이 · 끌어서 열기", font, 17f, XrayCyan, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyles.Bold);
            handleText.textWrappingMode = TextWrappingModes.NoWrap;

            // 판넬 = 관절 팔에 매달린 모니터(사용자 지시: 손그린 원으로 바꿨던 건 되돌린다 — 테두리는 원래 UI 예시대로
            // 모니터 형태여야 한다). 유리는 완전히 투명해서 그 안으로 캐릭터·뇌가 그대로 비쳐야 하므로(사용자 지시: "테두리만 남기고
            // 안은 뚫기"), 틀을 판 전체를 덮는 사각형 하나가 아니라 네 개의 막대(상하좌우)로 짜서 가운데(유리 자리)는 아예 그래픽이
            // 없게 한다 — 꽉 찬 Image였을 때는 유리가 투명해도 그 밑의 틀 자체가 불투명해서 뒤의 초상화를 가렸다.
            // 참고 이미지(크림색 의료 모니터)처럼 두꺼운 테두리 느낌을 내려고 막대 두께를 3~3.5%에서 6%로 키우고 AddPaperEdge(테두리선+그림자)를
            // 얹었다 — 머리(원 지름의 72%)보다는 여전히 훨씬 안쪽에서 시작해서 얼굴을 가리지 않는다.
            var tablet = Ensure(root.transform, "Tablet");
            TopLeft(tablet, new Vector2(0.5f, 0.5f));
            tablet.sizeDelta = new Vector2(500.6f, 500.6f);
            Remove(tablet, "Frame"); // 예전(꽉 찬 사각형 한 장)·손그림 원 버전 잔재를 걷어내고 아래 네 막대로 대신한다.
            var frameTop = EnsureImage(tablet, "FrameTop", XrayMetal, new Vector2(0f, 0.94f), new Vector2(1f, 1f), raycast: true);
            var frameBottom = EnsureImage(tablet, "FrameBottom", XrayMetal, new Vector2(0f, 0f), new Vector2(1f, 0.06f), raycast: true);
            var frameLeft = EnsureImage(tablet, "FrameLeft", XrayMetal, new Vector2(0f, 0.06f), new Vector2(0.06f, 0.94f), raycast: true);
            var frameRight = EnsureImage(tablet, "FrameRight", XrayMetal, new Vector2(0.94f, 0.06f), new Vector2(1f, 0.94f), raycast: true);
            foreach (var bar in new[] { frameTop, frameBottom, frameLeft, frameRight }) MockupStyle.AddPaperEdge(bar.gameObject);
            var frame = frameTop; // _frame 참조용 대표 한 장(네 막대 다 같은 색·역할이라 어느 쪽이든 상관없다).
            // Glass 자체는 다시 투명(드래그 판정 전용) — 파란 색조는 GlassTint로 뺐다. Glass가 Content보다 먼저 그려져서 그 파란기가
            // 뇌(Content 자식)는 안 물들이고 캐릭터만 물들였었는데, "뇌도 파랗게" 요청으로 뇌 위에 한 번 더 덮는 레이어가 필요해졌다.
            var glass = EnsureImage(tablet, "Glass", Color.clear, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), raycast: true);

            var content = Ensure(tablet, "Content");
            SetRect(content, new Vector2(0.035f, 0.03f), new Vector2(0.965f, 0.97f), Vector2.zero, Vector2.zero);
            var contentGroup = GetOrAdd<CanvasGroup>(content.gameObject);

            // 타이틀·접기 버튼은 원 <b>바깥 위쪽</b>에 둔다(1.0 넘는 앵커) — 원 안은 이제 그녀의 머리와 뇌가 꽉 채우고 있어서
            // 안에 글자를 얹으면 얼굴 위에 겹친다. Content의 CanvasGroup 자식이라 펼침/접힘에 따라 같이 나타나고 사라진다.
            EnsureText(content, "Title", "엑스레이", font, 22f, XrayCyan, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 1.03f), new Vector2(0.5f, 1.12f), Vector2.zero, Vector2.zero, FontStyles.Bold);

            var foldButtonImage = EnsureImage(content, "FoldButton", new Color32(30, 72, 84, 255), new Vector2(0.74f, 1.03f), new Vector2(0.98f, 1.115f), raycast: true);
            var foldButton = GetOrAdd<Button>(foldButtonImage.gameObject);
            foldButton.targetGraphic = foldButtonImage;
            MockupStyle.AddPaperEdge(foldButtonImage.gameObject, shadow: false);
            EnsureText(foldButtonImage.transform, "Label", "접기", font, 18f, XrayCyan, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyles.Bold);

            // 판정에 관여하지 않는 작은 사진 초상화는 걷어냈다(사용자 지시: 뇌가 실제 보이는 유키 머리 크기·자리에 딱 맞아야 한다 —
            // 별도로 작게 떠 있는 사진과 뇌가 서로 다른 크기·위치라 어긋나 보였다). 반응 표정(PortraitXrayView.Flash)은 이제
            // 화면에 실제로 보이는 큰 유키 초상화(CleanPortrait, MainHud의 "Yuki Portrait") 쪽에 붙인다 — CleanMainHud가 연결한다.
            // 예전에 구운 프리팹엔 그 사진 자리(PortraitSlot)가 남아 있을 수 있어 걷어낸다(멱등 — 없으면 아무 일도 안 한다).
            Remove(content, "PortraitSlot");

            // 뇌 크기·자리: 초상화(Anchors.YukiDrop)의 머리 중심이 곧 이 판(Tablet)의 중심이므로, 뇌를 머리와 같은 비율로 줄여서 판 한가운데
            // 놓으면 머리에 그대로 겹친다. 88%→65%→50%→72%로 옮겨 다니다가, "상자 안 여백을 줄이라"는 지시로 78%까지 키웠다
            // (판의 0.780×0.749, 63:54 대비 원래 비율 그대로 유지).
            // 뇌 아트 비율(63:54)이 머리보다 납작해서 AspectRatioFitter가 폭을 채우고 위아래로 조금 남긴다 — 머리카락 끝이
            // 뇌 위아래로 살짝 보이는 게 자연스럽다.
            // 아트(Brain)는 좌우 반전해 뇌가 오른쪽을 보게 한다(사용자 지시) — 글자(Labels)는 반전하면 거울상이 되어 못 읽으므로
            // 별도 형제로 빼서 안 뒤집힌 채로 두고, 위치 fraction만 좌우로 뒤집어(1-x) 뒤집힌 아트와 자리를 맞춘다.
            var area = Ensure(content, "BrainArea");
            SetRect(area, new Vector2(0.0806f, 0.1015f), new Vector2(0.9194f, 0.8985f), Vector2.zero, Vector2.zero);
            var brainRect = Ensure(area, "Brain");
            SetRect(brainRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fitter = GetOrAdd<AspectRatioFitter>(brainRect.gameObject);
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 63f / 54f;
            brainRect.localScale = new Vector3(-1f, 1f, 1f);

            var baseImage = EnsureImage(brainRect, "Base", Color.white, Vector2.zero, Vector2.one);
            baseImage.sprite = LoadBrainSprite("brain_base.png");

            var regions = new BrainRegionView[BrainLabelCenters.Length];
            var labelsRoot = Ensure(area, "Labels");

            // 예전에 네 번째 영역(뇌간)이 있던 프리팹이면 그 오버레이와 글자를 걷어낸다(최대 중첩 4 → 3).
            for (var stale = regions.Length; stale < regions.Length + 4; stale++)
            {
                Remove(brainRect, $"Lobe {stale}");
                Remove(labelsRoot, $"Label {stale}");
            }

            // Labels는 Brain의 형제라 Brain과 똑같은 비율 고정(63:54, FitInParent)을 따로 걸어야 같은 크기·자리에 온다.
            SetRect(labelsRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var labelsFitter = GetOrAdd<AspectRatioFitter>(labelsRoot.gameObject);
            labelsFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            labelsFitter.aspectRatio = 63f / 54f;

            for (var i = 0; i < regions.Length; i++)
            {
                var lit = EnsureImage(brainRect, $"Lobe {i}", Color.white, Vector2.zero, Vector2.one);
                lit.sprite = LoadBrainSprite($"brain_lobe_{i}.png");
                lit.transform.SetSiblingIndex(1 + i); // 기본 아트 위, 글자 밑.

                var center = BrainLabelCenters[i];
                var mirroredCenter = new Vector2(1f - center.x, center.y);
                var label = EnsureText(labelsRoot, $"Label {i}", string.Empty, font, 19f, BrainLabelInk,
                    TextAlignmentOptions.Center, mirroredCenter - new Vector2(0.21f, 0.09f), mirroredCenter + new Vector2(0.21f, 0.09f),
                    Vector2.zero, Vector2.zero, FontStyles.Bold);
                label.textWrappingMode = TextWrappingModes.Normal;
                label.lineSpacing = -14f;

                var region = GetOrAdd<BrainRegionView>(lit.gameObject);
                SetRef(region, "_lit", lit);
                SetRef(region, "_label", label);
                regions[i] = region;
            }

            labelsRoot.SetAsLastSibling();

            var brain = GetOrAdd<BrainView>(brainRect.gameObject);
            SetRefs(brain, "_regions", regions);

            // 파란 엑스레이 색조를 뇌 위에도 한 번 더 덮는다(사용자 지시: "뇌가 엑스레이 안쪽으로 들어가서 파란 효과를 받게") — Content의
            // 맨 마지막 자식이라 Brain(BrainArea)보다 나중에 그려져 그 위를 덮지만, raycastTarget이 꺼져 있어 뇌 영역 클릭·호버는 그대로 통과한다.
            var glassTint = EnsureImage(content, "GlassTint", XrayGlass, Vector2.zero, Vector2.one, raycast: false);
            glassTint.transform.SetAsLastSibling();

            SetRef(panel, "_tablet", tablet);
            SetRef(panel, "_basePlate", basePlate);
            SetRef(panel, "_handleTag", handleRect);
            SetRef(panel, "_frame", frame);
            SetRef(panel, "_glass", glass);
            SetRef(panel, "_arm", arm);
            SetRef(panel, "_contentGroup", contentGroup);
            SetRef(panel, "_handleGroup", handleGroup);
            SetRef(panel, "_foldButton", foldButton);
            SetRef(panel, "_brain", brain);

            // 그리는 순서: 받침대 → 팔 → 손잡이 안내표 → 판넬(팔이 판넬 뒤로 들어간다).
            basePlate.SetSiblingIndex(0);
            armRect.SetSiblingIndex(1);
            handleRect.SetSiblingIndex(2);
            tablet.SetSiblingIndex(3);
        }

        private static Sprite LoadBrainSprite(string fileName) => LoadArtSprite(BrainArtImporter.Folder, fileName);

        private static Sprite LoadPortraitSprite(string fileName) => LoadArtSprite(PortraitArtImporter.Folder, fileName);

        private static Sprite LoadArtSprite(string folder, string fileName)
        {
            var path = folder + fileName;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            // 정말 처음 임포트되는 에셋(예: 방금 추가한 초상화)은 V2 임포트 파이프라인이 비동기로 처리해서, ForceUpdate만으로는
            // 바로 다음 줄의 LoadAssetAtPath가 아직 못 찾을 수 있다 — ForceSynchronousImport로 끝날 때까지 기다린다.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[UiLayoutCleanupTool] 아트를 스프라이트로 못 읽었다: {path}");
            return sprite;
        }

        private static RectTransform EnsureArmPiece(Transform parent, string name, Vector2 pivot, Color color)
        {
            var rect = Ensure(parent, name);
            TopLeft(rect, pivot);
            var image = GetOrAdd<Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = false;
            MockupStyle.AddPaperEdge(rect.gameObject, shadow: false);
            return rect;
        }

        /// <summary>왼쪽 위 기준점 + 지정한 피벗 — 위치는 런타임에 컨테이너 크기 비율로 환산해 anchoredPosition으로 놓는다.</summary>
        private static void TopLeft(RectTransform rect, Vector2 pivot)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = pivot;
        }

        /// <summary>컴플렉스 최대 중첩이 4에서 3으로 줄었으므로 상태 목록의 남는 행을 걷어내고 목록의 행 배열을 다시 잇는다(이미 맞으면 아무것도 안 한다).</summary>
        private static void TrimStatusRows(GameObject root, int maxRows)
        {
            var rows = root.GetComponentsInChildren<ComplexRowView>(true);
            if (rows.Length <= maxRows) return;

            var kept = new ComplexRowView[maxRows];
            for (var i = 0; i < rows.Length; i++)
            {
                if (i < maxRows) kept[i] = rows[i];
                else Object.DestroyImmediate(rows[i].gameObject);
            }

            SetRefs(root.GetComponent<ComplexListView>(), "_rows", kept);
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

            TrimStatusRows(root, ComplexBoard.DefaultMaxSlots);

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

            // 내용 없이 색만 채운 자리 — 지속시간은 컴플렉스 포스트잇이 보여준다(막대는 목업 쪽 선택, Notion 원문은 막대+텍스트 — 보고 항목).
            Remove(hud, "Complex Duration Display");

            // 나츠 초상화(UI 가이드 5번) — 정지 사진 카드라 유키처럼 반응 표정·엑스레이 드롭 판정이 필요 없어서 프리팹 없이 여기서
            // 바로 짓는다(Ensure, 멱등). 예전엔 "나츠는 배경 스프라이트가 그린다"며 통째로 지웠었는데, 배경엔 실제로 나츠를 그리는 게
            // 없어서(3D 플레이스홀더는 유키 자리다) 빠져 있던 요소였다 — Notion 스펙 재확인 후 새로 만든다.
            var natsu = Ensure(hud, "Natsu Portrait");
            natsu.anchorMin = Anchors.Natsu.Min;
            natsu.anchorMax = Anchors.Natsu.Max;
            natsu.offsetMin = Vector2.zero;
            natsu.offsetMax = Vector2.zero;
            var natsuImage = GetOrAdd<Image>(natsu.gameObject);
            natsuImage.sprite = LoadPortraitSprite("Natsu/natsu_neutral.png");
            natsuImage.color = Color.white;
            natsuImage.preserveAspect = true;
            natsuImage.raycastTarget = false;
            MockupStyle.AddPaperEdge(natsu.gameObject);

            // 유키 초상화는 맨 밑에 깐다 — 위에 있으면 엑스레이 판넬/카드 입력을 가로채고, 엑스레이가 펼쳐질 때 그 위로 뇌가 겹쳐야 한다.
            var yuki = Place(hud, "Yuki Portrait", Anchors.YukiDrop);
            if (yuki != null)
            {
                yuki.SetAsFirstSibling();

                // 엑스레이 판 안의 작은 사진을 없애면서, 반응 표정(Flash)은 이 실제 초상화로 옮겼다 — 여기서 둘을 연결한다.
                var yukiPortraitView = yuki.GetComponentInChildren<PortraitXrayView>(true);
                var xrayPanel = root.GetComponentInChildren<ComplexXrayPanel>(true);
                if (yukiPortraitView != null && xrayPanel != null) SetRef(xrayPanel, "_portrait", yukiPortraitView);
            }

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

            // 뇌의 영역들도 같은 공유 팝업을 쓴다(0.25초 호버 상세).
            foreach (var brain in root.GetComponentsInChildren<BrainView>(true))
                SetTooltip(brain, tooltip);

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

        /// <summary>있으면 정확히 그 타입의 컴포넌트를 지운다(예전 모양을 새 모양으로 바꿀 때). <typeparamref name="T"/>가 상위 타입이면
        /// 하위 타입 인스턴스는 안 지운다 — 예를 들어 Outline(Shadow의 하위 타입)이 있어도 <c>RemoveIfPresent&lt;Shadow&gt;</c>는 그건 안 건드린다.</summary>
        private static void RemoveIfPresent<T>(GameObject go) where T : Component
        {
            foreach (var component in go.GetComponents<T>())
                if (component.GetType() == typeof(T)) Object.DestroyImmediate(component);
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
