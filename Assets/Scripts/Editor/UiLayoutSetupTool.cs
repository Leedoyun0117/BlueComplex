using System.IO;
using BlueComplex.Core.Stability;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Presentation;
using BlueComplex.UI.Rendering;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.EditorTools
{
    /// <summary>
    /// 정식 UI 레이아웃 골격 + 2단계 기능 요소를 전부 프리팹으로 만들고, MainHud 프리팹에
    /// 앵커 배치로 중첩한다. 위치는 요청서 표에 대한 합리적 추정이며, 각 요소가 독립 프리팹이라
    /// 나중에 앵커만 바꾸면 된다. 기준 해상도 1920x1080.
    ///
    /// StageBootstrapper 참조(SessionBoundView._bootstrapper, MemorySpaceDropZone._bootstrapper)는
    /// 여기서 와이어링하지 않는다 — 프리팹은 씬 오브젝트를 참조할 수 없으므로, MainHud를 씬에
    /// 배치한 뒤 UICompositorSetupTool이 채워 넣는다.
    /// </summary>
    public static class UiLayoutSetupTool
    {
        private const string ElementsFolder = "Assets/Prefabs/UI/Elements";
        private const string MainHudPrefabPath = "Assets/Prefabs/UI/MainHud.prefab";

        // 중립 어두운 회색 — 붉은 배경이면 반투명 구간 색(초록/파랑)이 탁해지고 즉사 구간과 헷갈린다.
        private static readonly Color HeartPanelBackground = new Color32(28, 28, 32, 200);
        private static readonly Color PanelSlot = new Color32(120, 120, 130, 179);
        private static readonly Color PanelPortrait = new Color32(90, 90, 100, 179);
        private static readonly Color PanelXray = new Color32(140, 190, 210, 40);
        private static readonly Color PanelBubble = new Color32(150, 190, 230, 179);
        private static readonly Color PanelDuration = new Color32(210, 170, 80, 179);
        private static readonly Color PanelCard = new Color32(120, 130, 150, 200);
        private static readonly Color RowBackground = new Color32(70, 70, 90, 180);
        private static readonly Color DurationBarBg = new Color32(40, 40, 45, 200);
        private static readonly Color DurationBarFill = new Color32(210, 170, 80, 220);
        private static readonly Color KeyZoneUpcoming = new Color32(140, 140, 150, 140);
        private static readonly Color OverlayBackground = new Color32(10, 10, 15, 220);
        private static readonly Color ButtonColor = new Color32(90, 90, 120, 230);
        private static readonly Color TooltipBackground = new Color32(20, 20, 25, 235);

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
                go => go.AddComponent<HeartRateIndicatorPanel>(), (go, comp) => BuildHeartRatePanel(go, comp, font));

            var bpm = EnsureElementPrefab("BpmDisplay.prefab", "BpmDisplay",
                go => go.AddComponent<BpmDisplay>(), (go, comp) =>
                {
                    var label = CreateTmpText(go.transform, "Label", "80", font, 68, TextAlignmentOptions.Center,
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_label", label);
                });

            var items = EnsureElementPrefab("ItemDisplayPanel.prefab", "ItemDisplayPanel",
                go => go.AddComponent<ItemDisplayPanel>(), (go, comp) => BuildItemDisplayPanel(go, comp, font));

            var portrait = EnsureElementPrefab("PortraitView.prefab", "PortraitView",
                go => go.AddComponent<PortraitView>(), (go, comp) =>
                {
                    var image = CreateImage(go.transform, "Portrait", PanelPortrait, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Wire(comp, "_portraitImage", image);
                });

            var xray = EnsureElementPrefab("ComplexXrayPanel.prefab", "ComplexXrayPanel",
                go => go.AddComponent<ComplexXrayPanel>(), (go, comp) => BuildComplexXrayPanel(go, comp, font));

            var memory = EnsureElementPrefab("MemorySpaceBubble.prefab", "MemorySpaceBubble",
                go => go.AddComponent<MemorySpaceBubble>(), (go, comp) => BuildMemorySpaceBubble(go, comp, font));

            var duration = EnsureElementPrefab("ComplexDurationDisplay.prefab", "ComplexDurationDisplay",
                go => go.AddComponent<ComplexDurationDisplay>(), (go, comp) =>
                {
                    CreateImage(go.transform, "Background", PanelDuration, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                });

            var dialogue = EnsureElementPrefab("DialogueText.prefab", "DialogueText",
                go => go.AddComponent<DialogueText>(), (go, comp) =>
                {
                    // 클릭(스킵) 감지를 위해 루트에 레이캐스트 가능한 배경이 필요하다.
                    var clickCatcher = CreateImage(go.transform, "ClickCatcher", new Color(0f, 0f, 0f, 0f),
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    clickCatcher.raycastTarget = true;

                    var label = CreateTmpText(go.transform, "Label", "...", font, 32, TextAlignmentOptions.TopLeft,
                        Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-16, -12));
                    Wire(comp, "_label", label);
                });

            var clues = EnsureElementPrefab("ClueCardTray.prefab", "ClueCardTray",
                go => go.AddComponent<ClueCardTray>(), (go, comp) => BuildClueCardTray(go, comp, font));

            var stageEnd = EnsureElementPrefab("StageEndPanel.prefab", "StageEndPanel",
                go => go.AddComponent<StageEndPanel>(), (go, comp) => BuildStageEndPanel(go, comp, font));

            return BuildMainHud(heartRate, bpm, items, portrait, xray, memory, duration, dialogue, clues, stageEnd, font);
        }

        // ---------------------------------------------------------------
        // 1. 심박수 표시기
        // ---------------------------------------------------------------

        /// <summary>
        /// 세 개 층으로 분리해서 쌓는다: 위쪽 KeyZoneRow(턴 라벨이 붙은 키 구역 탭, 바와는 별개
        /// 레이어라 서로 안 섞인다) → 가운데 BarArea(구간 색 + 마커) → 아래쪽 FatalLabelRow(즉사
        /// 구간 라벨). 색상은 HeartbeatVisuals(런타임과 공유하는 표)를 그대로 쓴다.
        /// </summary>
        private static void BuildHeartRatePanel(GameObject go, Component comp, TMP_FontAsset font)
        {
            var bg = CreateImage(go.transform, "Background", HeartPanelBackground, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Wire(comp, "_background", bg);

            var keyZoneRow = CreateRect(go.transform, "KeyZoneRow", new Vector2(0.02f, 0.54f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero);

            var barArea = CreateRect(go.transform, "BarArea", new Vector2(0.02f, 0.20f), new Vector2(0.98f, 0.52f),
                Vector2.zero, Vector2.zero);

            // BarArea와 같은 x 범위(0.02~0.98)를 써야 세그먼트 바로 아래 라벨이 정확히 겹친다.
            var fatalLabelRow = CreateRect(go.transform, "FatalLabelRow", new Vector2(0.02f, 0f), new Vector2(0.98f, 0.18f),
                Vector2.zero, Vector2.zero);

            foreach (var boundary in HeartbeatZone.DefaultBoundaries)
            {
                var minX = boundary.Min / (float)Heartbeat.MaxValue;
                var maxX = (boundary.Max + 1) / (float)Heartbeat.MaxValue;
                var segment = CreateImage(barArea, $"Segment {boundary.State}", HeartbeatVisuals.SegmentColor(boundary.State),
                    new Vector2(minX, 0f), new Vector2(maxX, 1f), Vector2.zero, Vector2.zero);
                segment.raycastTarget = false;

                if (boundary.State != HeartbeatState.Fatal) continue;

                var fatalLabel = CreateTmpText(fatalLabelRow, $"FatalLabel {boundary.Min}", "즉사", font, 15,
                    TextAlignmentOptions.Center, new Vector2(minX, 0f), new Vector2(maxX, 1f), Vector2.zero, Vector2.zero);
                fatalLabel.color = new Color32(255, 110, 110, 255);
                fatalLabel.fontStyle = FontStyles.Bold;
                fatalLabel.raycastTarget = false;
            }

            // 마커: 굵은 흰 선 + 위로 튀어나온 캡. 배경 세그먼트 색이 뭐든 항상 도드라지게
            // 검은 아웃라인을 두르고, 바 위아래로 살짝 삐져나오게 해서 위치를 즉시 알아볼 수 있다.
            var marker = CreateRect(barArea, "Marker", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(-4f, -6f), new Vector2(4f, 6f));
            var markerLine = marker.gameObject.AddComponent<Image>();
            markerLine.color = Color.white;
            markerLine.raycastTarget = false;
            var markerOutline = marker.gameObject.AddComponent<Outline>();
            markerOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            markerOutline.effectDistance = new Vector2(1.5f, 1.5f);

            var cap = CreateImage(marker, "Cap", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-7f, 0f), new Vector2(7f, 14f));
            cap.raycastTarget = false;
            var capOutline = cap.gameObject.AddComponent<Outline>();
            capOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            capOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // 키 구역 탭 4개 — 바 위 별도 레이어. 실제 anchorMin/Max(범위)와 라벨 텍스트는
            // 런타임에 HeartRateBarView.SetKeyZones가 채운다.
            var overlays = new RectTransform[4];
            var overlayImages = new Image[4];
            var overlayLabels = new TMP_Text[4];
            for (var i = 0; i < overlays.Length; i++)
            {
                var tab = CreateImage(keyZoneRow, $"KeyZone {i}", KeyZoneUpcoming, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                tab.raycastTarget = false;
                tab.gameObject.SetActive(false);

                var label = CreateTmpText(tab.transform, "Label", string.Empty, font, 16, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                label.raycastTarget = false;
                label.fontStyle = FontStyles.Bold;

                overlays[i] = tab.rectTransform;
                overlayImages[i] = tab;
                overlayLabels[i] = label;
            }

            var barView = go.AddComponent<HeartRateBarView>();
            Wire(barView, "_barArea", barArea);
            Wire(barView, "_marker", marker);
            Wire(barView, "_keyZoneOverlays", overlays);
            Wire(barView, "_keyZoneImages", overlayImages);
            Wire(barView, "_keyZoneLabels", overlayLabels);
        }

        // ---------------------------------------------------------------
        // 4. 아이템
        // ---------------------------------------------------------------

        private static void BuildItemDisplayPanel(GameObject go, Component comp, TMP_FontAsset font)
        {
            // 세로 배치 — 좌측 중단(왜곡이 덜한 자리)으로 옮기면서 폭보다 높이가 넉넉해졌다.
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var slots = new ItemSlotView[2];
            for (var i = 0; i < slots.Length; i++)
            {
                var slotGo = new GameObject($"Slot {i}", typeof(RectTransform));
                slotGo.transform.SetParent(go.transform, false);

                var bg = slotGo.AddComponent<Image>();
                bg.color = PanelSlot;

                // 이름만 항상 크게 보여준다. 긴 설명은 TooltipPopup 호버로 뺀다(ComplexRowView와
                // 동일 패턴) — _tooltip은 공유 팝업이 만들어진 뒤 BuildMainHud에서 와이어링한다.
                var nameText = CreateTmpText(slotGo.transform, "Name", string.Empty, font, 24, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));

                var view = slotGo.AddComponent<ItemSlotView>();
                Wire(view, "_background", bg);
                Wire(view, "_nameText", nameText);
                slots[i] = view;
            }

            Wire(comp, "_slots", slots);
        }

        // ---------------------------------------------------------------
        // 5. 컴플렉스 인터페이스 (목록)
        // ---------------------------------------------------------------

        private static void BuildComplexXrayPanel(GameObject go, Component comp, TMP_FontAsset font)
        {
            // raycastTarget은 true로 둔다 — ComplexXrayPanel 자체가 IBeginDrag/IDrag/IEndDrag로
            // "판넬을 유키 위에 드래그해서 놓기" 조건을 처리하므로 Background가 히트테스트를 받아야 한다.
            CreateImage(go.transform, "Background", PanelXray, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var listRoot = CreateRect(go.transform, "ComplexList", Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

            var vLayout = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 6;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            var rows = new ComplexRowView[4];
            for (var i = 0; i < rows.Length; i++)
            {
                var rowGo = new GameObject($"Row {i}", typeof(RectTransform));
                rowGo.transform.SetParent(listRoot, false);

                var layoutElement = rowGo.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 76;

                var bg = rowGo.AddComponent<Image>();
                bg.color = RowBackground;

                var nameText = CreateTmpText(rowGo.transform, "Name", string.Empty, font, 20, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 0.5f), Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, -4f));
                var descText = CreateTmpText(rowGo.transform, "Description", string.Empty, font, 13, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 0.18f), new Vector2(0.72f, 0.5f), new Vector2(10f, 0f), new Vector2(-4f, 0f));

                var barBg = CreateImage(rowGo.transform, "DurationBarBg", DurationBarBg,
                    new Vector2(0f, 0f), new Vector2(1f, 0.18f), new Vector2(10f, 4f), new Vector2(-10f, 0f));
                barBg.raycastTarget = false;
                var barFill = CreateImage(barBg.transform, "DurationBarFill", DurationBarFill,
                    Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                barFill.raycastTarget = false;
                var durationText = CreateTmpText(rowGo.transform, "DurationNumber", string.Empty, font, 15,
                    TextAlignmentOptions.MidlineRight, new Vector2(0.72f, 0.18f), new Vector2(1f, 0.5f),
                    Vector2.zero, new Vector2(-10f, 0f));

                var row = rowGo.AddComponent<ComplexRowView>();
                Wire(row, "_nameText", nameText);
                Wire(row, "_descriptionText", descText);
                Wire(row, "_durationFill", barFill);
                Wire(row, "_durationText", durationText);
                rows[i] = row;

                rowGo.SetActive(false);
            }

            var listView = listRoot.gameObject.AddComponent<ComplexListView>();
            Wire(listView, "_rows", rows);
            // _tooltip은 공유 TooltipPopup이 만들어진 뒤 BuildMainHud에서 인스턴스 단위로 와이어링한다.
        }

        // ---------------------------------------------------------------
        // 3. 단서 제시 (드래그 앤 드롭) — 기억 공간 드롭존
        // ---------------------------------------------------------------

        private static void BuildMemorySpaceBubble(GameObject go, Component comp, TMP_FontAsset font)
        {
            var bg = CreateImage(go.transform, "Bubble", PanelBubble, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Wire(comp, "_bubbleBackground", bg);

            // 최종 감정 고정 표시 — 위로 떠오르며 사라지는 연출(PlayRemainingTags)과 별개로 항상
            // 남아 있는다. 버블 하단 띠에 둬서 위쪽 상승 공간과 겹치지 않는다.
            var summary = CreateTmpText(go.transform, "PersistentSummary", string.Empty, font, 16,
                TextAlignmentOptions.Center, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.28f), Vector2.zero, Vector2.zero);
            summary.raycastTarget = false;
            Wire(comp, "_summaryText", summary);

            // 판정 영역은 시각 영역보다 사방 32px 크게 — _Shake 흥분 최대치(±11.5px/±4.5px)와
            // 요청된 20px 여유를 합친 것보다 넉넉하다.
            var dropZone = CreateImage(go.transform, "DropZone", new Color(0f, 0f, 0f, 0f),
                Vector2.zero, Vector2.one, new Vector2(-32f, -32f), new Vector2(32f, 32f));
            dropZone.raycastTarget = true;
            dropZone.gameObject.AddComponent<MemorySpaceDropZone>();
        }

        // ---------------------------------------------------------------
        // 2 & 3. 단서 카드 (해금 표시 + 드래그)
        // ---------------------------------------------------------------

        private static void BuildClueCardTray(GameObject go, Component comp, TMP_FontAsset font)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var cards = new ClueCardView[4];
            for (var i = 0; i < cards.Length; i++)
            {
                var cardGo = new GameObject($"Card {i}", typeof(RectTransform));
                cardGo.transform.SetParent(go.transform, false);

                var bg = cardGo.AddComponent<Image>();
                bg.color = PanelCard;

                var canvasGroup = cardGo.AddComponent<CanvasGroup>();

                var titleText = CreateTmpText(cardGo.transform, "Title", string.Empty, font, 17, TextAlignmentOptions.Center,
                    new Vector2(0f, 0.85f), Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, -2f));
                var attributesText = CreateTmpText(cardGo.transform, "Attributes", string.Empty, font, 12, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 0.45f), new Vector2(1f, 0.85f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
                var storyText = CreateTmpText(cardGo.transform, "Story", string.Empty, font, 12, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 0.15f), new Vector2(1f, 0.45f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
                var usesText = CreateTmpText(cardGo.transform, "Uses", string.Empty, font, 11, TextAlignmentOptions.BottomRight,
                    Vector2.zero, new Vector2(1f, 0.15f), new Vector2(4f, 2f), new Vector2(-4f, 0f));

                var view = cardGo.AddComponent<ClueCardView>();
                Wire(view, "_background", bg);
                Wire(view, "_titleText", titleText);
                Wire(view, "_attributesText", attributesText);
                Wire(view, "_storyText", storyText);
                Wire(view, "_usesText", usesText);

                var dragHandler = cardGo.AddComponent<ClueCardDragHandler>();
                Wire(dragHandler, "_canvasGroup", canvasGroup);

                cards[i] = view;
            }

            Wire(comp, "_cards", cards);
        }

        // ---------------------------------------------------------------
        // 8. 스테이지 종료
        // ---------------------------------------------------------------

        private static void BuildStageEndPanel(GameObject go, Component comp, TMP_FontAsset font)
        {
            var overlay = new GameObject("Overlay", typeof(RectTransform));
            overlay.transform.SetParent(go.transform, false);
            var overlayRt = (RectTransform)overlay.transform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            overlay.AddComponent<Image>().color = OverlayBackground;

            var outcomeText = CreateTmpText(overlay.transform, "OutcomeText", string.Empty, font, 48, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.8f), Vector2.zero, Vector2.zero);

            var sameSeedButton = CreateButton(overlay.transform, "RestartSameSeed", "같은 시드로 재시작", font,
                new Vector2(0.3f, 0.35f), new Vector2(0.48f, 0.48f));
            var newSeedButton = CreateButton(overlay.transform, "RestartNewSeed", "새 시드로 재시작", font,
                new Vector2(0.52f, 0.35f), new Vector2(0.7f, 0.48f));

            overlay.SetActive(false);

            Wire(comp, "_overlay", overlay);
            Wire(comp, "_outcomeText", outcomeText);
            Wire(comp, "_restartSameSeedButton", sameSeedButton);
            Wire(comp, "_restartNewSeedButton", newSeedButton);
        }

        // ---------------------------------------------------------------
        // MainHud 조립
        // ---------------------------------------------------------------

        private static GameObject BuildMainHud(GameObject heartRate, GameObject bpm, GameObject items, GameObject portrait,
            GameObject xray, GameObject memory, GameObject duration, GameObject dialogue, GameObject clues,
            GameObject stageEnd, TMP_FontAsset font)
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

            // 상단 레이아웃 재배치: 심박수 바(최우선 정보)를 화면 폭 78%까지 크게 잡고, 그 위에
            // BPM 큰 숫자를 얹는다. 아이템 표시는 배럴 왜곡이 가장 심한 좌상단 코너에서
            // 좌측 중단(세로로 넓은 자리)으로 옮겨 폰트를 키울 공간을 확보했다. 컴플렉스 지속시간
            // 표시는 그 대칭 자리(우측 중단)로 옮겨 자리를 비켜준다.
            var itemsInstance = PlaceInstance(root.transform, items, "Item Display", 0.02f, 0.40f, 0.17f, 0.74f);
            var heartRateInstance = PlaceInstance(root.transform, heartRate, "Heart Rate Indicator", 0.11f, 0.74f, 0.89f, 0.92f);
            var bpmInstance = PlaceInstance(root.transform, bpm, "BPM Display", 0.40f, 0.92f, 0.60f, 0.995f);
            PlaceInstance(root.transform, duration, "Complex Duration Display", 0.83f, 0.40f, 0.98f, 0.74f);
            var xrayInstance = PlaceInstance(root.transform, xray, "Complex X-ray Panel", 0.20f, 0.50f, 0.80f, 0.88f);
            var cluesInstance = PlaceInstance(root.transform, clues, "Clue Card Tray", 0.30f, 0.36f, 0.70f, 0.49f);
            var memoryInstance = PlaceInstance(root.transform, memory, "Memory Space Bubble", 0.30f, 0.19f, 0.70f, 0.355f);
            PlaceInstance(root.transform, portrait, "Yuki Portrait", 0.02f, 0.06f, 0.17f, 0.36f);
            PlaceInstance(root.transform, portrait, "Natsu Portrait", 0.83f, 0.06f, 0.98f, 0.36f);
            var dialogueInstance = PlaceInstance(root.transform, dialogue, "Yuki Dialogue Text", 0.18f, 0.02f, 0.82f, 0.18f);
            var stageEndInstance = PlaceInstance(root.transform, stageEnd, "Stage End Panel", 0f, 0f, 1f, 1f);

            var tooltip = BuildTooltipPopup(root.transform, font);

            var xrayListView = xrayInstance.GetComponentInChildren<ComplexListView>(true);
            Wire(xrayListView, "_tooltip", tooltip);

            var xrayPanel = xrayInstance.GetComponent<ComplexXrayPanel>();
            Wire(xrayPanel, "_clueTray", cluesInstance.GetComponent<ClueCardTray>());

            var clueTray = cluesInstance.GetComponent<ClueCardTray>();
            var itemPanel = itemsInstance.GetComponent<ItemDisplayPanel>();
            var heartBar = heartRateInstance.GetComponent<HeartRateBarView>();
            var bpmDisplay = bpmInstance.GetComponent<BpmDisplay>();
            var dialogueText = dialogueInstance.GetComponent<DialogueText>();
            var stageEndPanel = stageEndInstance.GetComponent<StageEndPanel>();
            var memoryBubble = memoryInstance.GetComponent<MemorySpaceBubble>();

            // 아이템 슬롯도 컴플렉스 행과 같은 공유 TooltipPopup을 쓴다 — 이름만 항상 보이고,
            // 설명은 호버로 뜬다.
            for (var i = 0; i < itemPanel.SlotCount; i++)
                Wire(itemPanel.GetSlot(i), "_tooltip", tooltip);

            BuildController<HeartRateController>(root.transform, "Heart Rate Controller", controller =>
            {
                Wire(controller, "_bar", heartBar);
                Wire(controller, "_bpm", bpmDisplay);
            });
            var heartRateController = root.GetComponentInChildren<HeartRateController>(true);

            BuildController<ClueHandController>(root.transform, "Clue Hand Controller", controller =>
            {
                Wire(controller, "_tray", clueTray);
            });

            BuildController<ItemController>(root.transform, "Item Controller", controller =>
            {
                Wire(controller, "_panel", itemPanel);
            });

            BuildController<ImmediateTurnResultPresenter>(root.transform, "Turn Result Presenter", controller =>
            {
                Wire(controller, "_complexList", xrayListView);
                Wire(controller, "_dialogue", dialogueText);
                Wire(controller, "_clueTray", clueTray);
                Wire(controller, "_heartRate", heartRateController);
                Wire(controller, "_memoryBubble", memoryBubble);
            });

            BuildController<StageEndController>(root.transform, "Stage End Controller", controller =>
            {
                Wire(controller, "_panel", stageEndPanel);
            });

            // MemorySpaceDropZone._bootstrapper도 SessionBoundView와 마찬가지로 씬 배치 후
            // UICompositorSetupTool이 채운다 — 필드 이름은 같게 맞춰 뒀다.

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
            }

            PrefabUtility.SavePrefabAsset(savedPrefab);
            return savedPrefab;
        }

        private static TooltipPopup BuildTooltipPopup(Transform parent, TMP_FontAsset font)
        {
            var root = CreateRect(parent, "Tooltip Popup", new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.72f), Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<Image>().color = TooltipBackground;

            var titleText = CreateTmpText(root, "Title", string.Empty, font, 18, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0.55f), Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, -6f));
            var bodyText = CreateTmpText(root, "Body", string.Empty, font, 14, TextAlignmentOptions.TopLeft,
                Vector2.zero, new Vector2(1f, 0.55f), new Vector2(10f, 6f), new Vector2(-10f, 0f));

            var popup = root.gameObject.AddComponent<TooltipPopup>();
            Wire(popup, "_root", root);
            Wire(popup, "_titleText", titleText);
            Wire(popup, "_bodyText", bodyText);

            return popup;
        }

        private static void BuildController<T>(Transform parent, string name, System.Action<T> configure) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var controller = go.AddComponent<T>();
            configure(controller);
        }

        private static GameObject PlaceInstance(Transform parent, GameObject prefab, string instanceName,
            float xMin, float yMin, float xMax, float yMax)
        {
            if (prefab == null) return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            var rt = (RectTransform)instance.transform;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return instance;
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

        private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var bg = CreateImage(parent, name, ButtonColor, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            CreateTmpText(bg.transform, "Label", label, font, 20, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
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
