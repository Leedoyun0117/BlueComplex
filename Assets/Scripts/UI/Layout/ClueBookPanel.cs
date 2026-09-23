using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 단서 정보 책(파일) UI(UI 가이드 "추가: 단서 정보 인터페이스"). 단서를 클릭하면 전체화면을 덮는 파일철 한 장이 펼쳐진다 —
    /// 왼쪽 페이지엔 그 단서의 사진과 시간/인물/감정 태그 쪽지가 흩어져 붙고(탭과 무관하게 항상 그대로), 오른쪽 페이지는
    /// "단서" 탭(번호·이름·스토리)과 "메뉴얼" 탭(감정·시간대·인물 분류표, 고정 내용) 사이를 오간다. 사진을 클릭하면 확대되고,
    /// 배경이나 "돌아가기"를 누르면 닫힌다.
    ///
    /// MainHud.prefab은 이미 손으로 다듬어진 상태라(git 상 uncommitted 변경 존재) 재생성하면
    /// 그 작업을 날린다 — 그래서 이 패널은 프리팹에 미리 만들어 두지 않고, 처음 필요할 때
    /// 루트 캔버스 아래에 코드로 직접 짓는다(GetOrCreate). UiLayoutSetupTool 같은 에디터 툴이
    /// 아니라 순수 런타임 코드라 UnityEditor를 참조하지 않는다.
    /// </summary>
    public sealed class ClueBookPanel : MonoBehaviour
    {
        private const float ZoomScale = 1.6f;
        private const float ZoomDuration = 0.25f;

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

        private const int RingCount = 12;

        private RectTransform _photoArea;
        private Image _icon;
        private bool _zoomed;
        private Tween _zoomTween;

        private TMP_Text _clueNumberText;
        private TMP_Text _titleText;
        private TMP_Text _storyText;
        private TMP_Text _timeCardText;
        private TMP_Text _personCardText;
        private TMP_Text _emotionCardText;

        private GameObject _overlay;
        private GameObject _clueTab;
        private GameObject _manualTab;
        private Image _clueTabButton;
        private Image _manualTabButton;

        public static ClueBookPanel GetOrCreate(Transform canvasRoot, TMP_FontAsset font)
        {
            var existing = canvasRoot.GetComponentInChildren<ClueBookPanel>(true);
            if (existing != null) return existing;

            var go = new GameObject("Clue Book Panel", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            var panel = go.AddComponent<ClueBookPanel>();
            panel.Build(font);
            return panel;
        }

        /// <param name="clueNumber">손패 안에서 이 카드의 자리(1부터) — 목업의 "단서 1" 번호로 쓴다. 고유 식별자라기보다
        /// "지금 손에 든 몇 번째 단서인지" 표시다.</param>
        public void Show(ClueCardViewModel vm, Sprite icon, int clueNumber)
        {
            _zoomed = false;
            _zoomTween?.Kill();
            _photoArea.localScale = Vector3.one;

            _icon.sprite = icon;
            _icon.enabled = icon != null;

            _clueNumberText.text = $"단서 {clueNumber}";
            _titleText.text = vm.Title;

            if (vm.AttributesHidden)
            {
                _timeCardText.text = "시간 ?";
                _personCardText.text = "인물 ?";
                _emotionCardText.text = "감정\n?, ?";
            }
            else
            {
                _timeCardText.text = $"시간\n{vm.TimeText}";
                _personCardText.text = $"인물\n{string.Join(", ", vm.PersonTexts)}";
                _emotionCardText.text = $"감정\n{string.Join(", ", vm.EmotionTexts)}";
            }

            _storyText.text = vm.StoryText;

            ShowClueTab();
            _overlay.SetActive(true);
        }

        public void Hide()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            _overlay.SetActive(false);
        }

        private void ToggleZoom()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            _zoomed = !_zoomed;
            _zoomTween?.Kill();
            _zoomTween = _photoArea.DOScale(_zoomed ? ZoomScale : 1f, ZoomDuration);
        }

        private void ShowClueTab()
        {
            _clueTab.SetActive(true);
            _manualTab.SetActive(false);
            _clueTabButton.color = TabActive;
            _manualTabButton.color = TabIdle;
        }

        private void ShowManualTab()
        {
            _clueTab.SetActive(false);
            _manualTab.SetActive(true);
            _clueTabButton.color = TabIdle;
            _manualTabButton.color = TabActive;
        }

        private void Build(TMP_FontAsset font)
        {
            var backdrop = CreateImage(transform, "Backdrop", new Color(0f, 0f, 0f, 0.8f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = true;
            AddClickHandler(backdrop.gameObject, Hide);

            var spread = CreateImage(backdrop.transform, "Spread", Color.clear, new Vector2(0.10f, 0.06f), new Vector2(0.97f, 0.94f),
                Vector2.zero, Vector2.zero);
            spread.raycastTarget = true; // 페이지 위 빈 곳을 눌러도 배경까지 새지 않는다(안 닫힘).

            BuildTabs(backdrop.transform, font);
            BuildLeftPage(spread.transform, font);
            BuildGutter(spread.transform);
            BuildRightPage(spread.transform, font);

            // 테두리는 페이지들을 다 얹은 다음 마지막에 그려야 위로 덮이지 않는다(페이지 배경이 가장자리까지 꽉 차 있다).
            AddBorder(spread.transform, FileAccent, 3f);

            _overlay = gameObject;
            _overlay.SetActive(false);
        }

        // -----------------------------------------------------------------
        // 왼쪽 페이지 — 탭과 무관하게 항상 같다: 단서 사진 + 시간/인물/감정 쪽지 세 장.
        // -----------------------------------------------------------------

        private void BuildLeftPage(Transform spread, TMP_FontAsset font)
        {
            var left = CreateImage(spread, "Left Page", PageBg, Vector2.zero, new Vector2(0.495f, 1f), Vector2.zero, Vector2.zero);

            _photoArea = CreateRect(left.transform, "Photo Area", new Vector2(0.32f, 0.55f), new Vector2(0.92f, 0.92f));
            var photo = CreateImage(_photoArea, "Photo", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            photo.preserveAspect = true;
            photo.raycastTarget = true;
            AddClickHandler(photo.gameObject, ToggleZoom);
            _icon = photo;

            // 쪽지 세 장 — 시간(뒤) → 인물(가운데) → 감정(맨 앞) 순서로 겹친다(목업 그대로).
            _timeCardText = BuildTagCard(left.transform, "Time Card", new Vector2(0.05f, 0.28f), new Vector2(0.42f, 0.55f),
                -8f, TimeCard, InkLight, font);
            _personCardText = BuildTagCard(left.transform, "Person Card", new Vector2(0.28f, 0.18f), new Vector2(0.65f, 0.45f),
                6f, PersonCard, InkLight, font);
            _emotionCardText = BuildTagCard(left.transform, "Emotion Card", new Vector2(0.03f, 0.02f), new Vector2(0.40f, 0.29f),
                -4f, EmotionCard, InkDark, font);
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

        // -----------------------------------------------------------------
        // 가운데 — 바인더 링이 늘어선 좁은 홈.
        // -----------------------------------------------------------------

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

        // -----------------------------------------------------------------
        // 오른쪽 페이지 — "단서"/"메뉴얼" 탭이 이 안만 바꾼다.
        // -----------------------------------------------------------------

        private void BuildRightPage(Transform spread, TMP_FontAsset font)
        {
            var right = CreateImage(spread, "Right Page", PageBg, new Vector2(0.515f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

            BuildClueTabContent(right.transform, font);
            BuildManualTabContent(right.transform, font);

            var back = CreateTmpText(right.transform, "Back", "돌아가기", font, 20,
                TextAlignmentOptions.MidlineRight, new Vector2(0.4f, 0.02f), new Vector2(0.97f, 0.08f), Vector2.zero, Vector2.zero);
            back.color = InkDark;
            back.raycastTarget = true;
            back.transform.SetAsLastSibling(); // 두 탭 위에 항상 떠 있어야 한다(둘 다에서 눌려야 하므로).
            AddClickHandler(back.gameObject, Hide);
        }

        private void BuildClueTabContent(Transform right, TMP_FontAsset font)
        {
            _clueTab = new GameObject("Clue Tab", typeof(RectTransform));
            _clueTab.transform.SetParent(right, false);
            SetStretch((RectTransform)_clueTab.transform, Vector2.zero, Vector2.one);

            var header = CreateImage(_clueTab.transform, "Header", HeaderBg, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.95f),
                Vector2.zero, Vector2.zero);

            var numberBox = CreateImage(header.transform, "Number Box", Color.white,
                new Vector2(0.01f, 0.1f), new Vector2(0.22f, 0.9f), Vector2.zero, Vector2.zero);
            _clueNumberText = CreateTmpText(numberBox.transform, "Number", "단서 1", font, 20,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _clueNumberText.color = InkDark;

            _titleText = CreateTmpText(header.transform, "Title", string.Empty, font, 22,
                TextAlignmentOptions.Center, new Vector2(0.24f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _titleText.color = InkLight;

            _storyText = CreateTmpText(_clueTab.transform, "Story", string.Empty, font, 20,
                TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.82f),
                Vector2.zero, Vector2.zero);
            _storyText.color = InkDark;
        }

        private void BuildManualTabContent(Transform right, TMP_FontAsset font)
        {
            _manualTab = new GameObject("Manual Tab", typeof(RectTransform));
            _manualTab.transform.SetParent(right, false);
            SetStretch((RectTransform)_manualTab.transform, Vector2.zero, Vector2.one);
            _manualTab.SetActive(false);

            CreateImage(_manualTab.transform, "Header", HeaderBg, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.95f),
                Vector2.zero, Vector2.zero);
            var manualTitle = CreateTmpText(_manualTab.transform, "Manual Title", "메뉴얼", font, 22,
                TextAlignmentOptions.Center, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);
            manualTitle.color = InkLight;

            BuildLegendGroup(_manualTab.transform, "흥분 감정", "행복, 사랑, 분노", ExcitedColor,
                new Vector2(0.03f, 0.58f), new Vector2(0.49f, 0.82f), font);
            BuildLegendGroup(_manualTab.transform, "침체 감정", "슬픔, 공포, 혐오", SubduedColor,
                new Vector2(0.51f, 0.58f), new Vector2(0.97f, 0.82f), font);
            BuildLegendGroup(_manualTab.transform, "시간대", "과거, 현재, 미래", LegendBox,
                new Vector2(0.03f, 0.35f), new Vector2(0.97f, 0.51f), font);
            BuildLegendGroup(_manualTab.transform, "인물", "가족, 친구, 연인, 타인", LegendBox,
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
        private void BuildTabs(Transform parent, TMP_FontAsset font)
        {
            _clueTabButton = CreateImage(parent, "Tab Clue", TabActive, new Vector2(0.02f, 0.60f), new Vector2(0.095f, 0.68f),
                Vector2.zero, Vector2.zero);
            _clueTabButton.raycastTarget = true;
            var clueLabel = CreateTmpText(_clueTabButton.transform, "Label", "단서", font, 18,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            clueLabel.color = InkLight;
            AddClickHandler(_clueTabButton.gameObject, ShowClueTab);

            _manualTabButton = CreateImage(parent, "Tab Manual", TabIdle, new Vector2(0.02f, 0.50f), new Vector2(0.095f, 0.58f),
                Vector2.zero, Vector2.zero);
            _manualTabButton.raycastTarget = true;
            var manualLabel = CreateTmpText(_manualTabButton.transform, "Label", "메뉴얼", font, 18,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            manualLabel.color = InkLight;
            AddClickHandler(_manualTabButton.gameObject, ShowManualTab);
        }

        // -----------------------------------------------------------------
        // 작은 헬퍼들.
        // -----------------------------------------------------------------

        /// <summary>네 변을 얇은 막대 네 개로 둘러 진짜 사각 테두리를 만든다 — Unity Outline 이펙트는 그림자처럼 두 변만
        /// 비쳐서(오프셋 복제라) 쓰지 않는다(엑스레이 모니터 틀에서도 같은 이유로 막대 방식을 썼다). <paramref name="target"/>이
        /// 회전(쪽지 기울기)돼 있으면 막대도 그 자식이라 같이 돌아간다.</summary>
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

        private static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddClickHandler(GameObject go, UnityEngine.Events.UnityAction onClick)
        {
            var trigger = go.AddComponent<ClickForward>();
            trigger.Clicked += onClick;
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

        /// <summary>배경/사진 클릭을 UnityAction 이벤트로 중계하는 최소 헬퍼 — Image에 Button을
        /// 붙이면 생기는 기본 색 트랜지션 등 불필요한 장식 없이 클릭만 받으면 돼서 직접 구현한다.</summary>
        private sealed class ClickForward : MonoBehaviour, IPointerClickHandler
        {
            public event UnityEngine.Events.UnityAction Clicked;
            public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
        }
    }
}
