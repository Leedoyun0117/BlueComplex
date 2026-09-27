using System.Collections.Generic;
using BlueComplex.Core.Clues;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 시작 화면의 "단서 노트" — 게임 안의 단서 정보 책(ClueBookPanel)과는 별개인 노트다. 지금까지 한 번이라도 본(손패에 들어왔거나 낸)
    /// 단서만 오른쪽 목록에 오르고(<see cref="ClueNote.SeenClues"/>), 누르면 왼쪽 페이지에 그 단서의 사진·이름·시간/인물/감정 쪽지·스토리가 펼쳐진다.
    /// 해금 안 된 태그는 카드와 같은 규칙으로 "?"다(<see cref="ClueCardFormatter"/>). 노트 밖(어두운 막)을 누르면 닫힌다.
    ///
    /// 전부 코드로 짓는다(프리팹 없음) — 아트가 정해지면 색 상수 자리를 그림으로 바꾼다. 좌표는 전부 부모 대비 비율이라 해상도와 무관하다.
    /// </summary>
    internal sealed class ClueNoteBook : MonoBehaviour
    {
        private static readonly Color Dim = new(0f, 0f, 0f, 0.72f);
        private static readonly Color Cover = new Color32(178, 157, 96, 255);     // 서류철 겉(크라프트)
        private static readonly Color Page = new Color32(236, 229, 206, 255);     // 속지
        private static readonly Color PageLine = new Color32(128, 106, 66, 255);  // 속지 테두리
        private static readonly Color Spine = new Color32(52, 46, 40, 255);
        private static readonly Color PhotoBack = new Color32(88, 100, 110, 255);
        private static readonly Color StoryBack = new Color32(246, 242, 228, 255);
        private static readonly Color RowIdle = new(0f, 0f, 0f, 0f);
        private static readonly Color RowHover = new Color32(120, 100, 60, 40);
        private static readonly Color RowSelected = new Color32(120, 100, 60, 90);
        private static readonly Color TimeNote = new Color32(214, 232, 238, 255);
        private static readonly Color PersonNote = new Color32(226, 214, 240, 255);
        private static readonly Color EmotionNote = MockupStyle.Sticky;

        private TMP_FontAsset _font;
        private ClueKnowledgeLedger _ledger;
        private List<ClueDefinition> _clues;

        private GameObject _detail;
        private TMP_Text _empty;
        private Image _icon;
        private TMP_Text _title;
        private TMP_Text _time;
        private TMP_Text _person;
        private TMP_Text _emotion;
        private TMP_Text _story;
        private ScrollRect _storyScroll;
        private ScrollRect _listScroll;
        private readonly List<Image> _rows = new();
        private int _selected = -1;

        /// <summary>지금 목록에 오른 단서 id(검증용).</summary>
        public IEnumerable<string> ListedClueIds
        {
            get { foreach (var def in _clues) yield return def.Id; }
        }

        /// <summary>왼쪽 페이지에 펼친 단서의 표시 문자열(검증용) — 없으면 null.</summary>
        public string DetailSummary => _selected < 0 ? null
            : $"{_title.text} | {_time.text.Replace('\n', ' ')} | {_person.text.Replace('\n', ' ')} | {_emotion.text.Replace('\n', ' ')}";

        /// <summary><paramref name="parent"/>를 꽉 채워 노트를 연다. <paramref name="catalog"/>는 본편 단서 전체(순서 = 목록 순서).</summary>
        public static ClueNoteBook Open(RectTransform parent, TMP_FontAsset font, IEnumerable<ClueDefinition> catalog, ClueKnowledgeLedger ledger)
        {
            var root = RuntimeUi.CreateStretched(parent, "Clue Note Book");
            root.SetAsLastSibling();
            var book = root.gameObject.AddComponent<ClueNoteBook>();
            book._font = font;
            book._ledger = ledger;
            book._clues = ClueNote.SeenClues(catalog, ledger);
            book.Build(root);
            book.Select(book._clues.Count > 0 ? 0 : -1);
            UiSoundHooks.Play(UiSoundCue.Paper);
            return book;
        }

        public void Close()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            Destroy(gameObject);
        }

        /// <summary>목록의 index번째 단서를 왼쪽 페이지에 펼친다(목록 클릭).</summary>
        public void Select(int index)
        {
            if (_selected >= 0 && _selected < _rows.Count) _rows[_selected].color = RowIdle;
            _selected = index;

            var has = index >= 0 && index < _clues.Count;
            _detail.SetActive(has);
            _empty.gameObject.SetActive(!has);
            if (!has) return;

            _rows[index].color = RowSelected;

            var def = _clues[index];
            var vm = ClueCardFormatter.Format(def, _ledger);
            var icon = UiIcons.Get(def.Id);
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _title.text = vm.Title;
            _time.text = $"시간\n{vm.TimeText}";
            _person.text = $"인물\n{string.Join(", ", vm.PersonTexts)}";
            _emotion.text = $"감정\n{string.Join(", ", vm.EmotionTexts)}";
            _story.text = vm.StoryText;

            // 새 단서는 스토리 첫 줄부터.
            Canvas.ForceUpdateCanvases();
            _storyScroll.verticalNormalizedPosition = 1f;
        }

        // ------------------------------------------------------------------
        // 짓기
        // ------------------------------------------------------------------

        private void Build(RectTransform root)
        {
            // 막: 노트 밖을 누르면 닫힌다. 노트(Frame)는 막의 자식이 아니라 형제다 — 자식이면 노트 안 클릭이 막의 버튼까지 올라온다.
            var backdrop = RuntimeUi.CreateImage(root, "Backdrop", Dim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, raycastTarget: true);
            AddButton(backdrop, Close);

            // 16:9를 지키는 판 — 화면 비가 달라도 노트가 찌그러지지 않는다.
            var stage = RuntimeUi.CreateRect(root, "Stage", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;

            var frame = RuntimeUi.CreateImage(stage, "Frame", Cover, new Vector2(0.11f, 0.09f), new Vector2(0.89f, 0.91f),
                Vector2.zero, Vector2.zero, raycastTarget: true).rectTransform;
            MockupStyle.AddShadow(frame.gameObject);

            var left = BuildPage(frame, "Left Page", 0.025f, 0.487f);
            var right = BuildPage(frame, "Right Page", 0.513f, 0.975f);
            BuildSpine(frame);

            BuildDetail(left);
            BuildList(right);
        }

        private static RectTransform BuildPage(RectTransform frame, string name, float xMin, float xMax)
        {
            var edge = RuntimeUi.CreateImage(frame, name, PageLine, new Vector2(xMin, 0.035f), new Vector2(xMax, 0.965f), Vector2.zero, Vector2.zero);
            var page = RuntimeUi.CreateImage(edge.rectTransform, "Paper", Page, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            return page.rectTransform;
        }

        private static void BuildSpine(RectTransform frame)
        {
            RuntimeUi.CreateImage(frame, "Spine", Spine, new Vector2(0.494f, 0.02f), new Vector2(0.506f, 0.98f), Vector2.zero, Vector2.zero);
            const int rings = 18;
            for (var i = 0; i < rings; i++)
            {
                var y = 0.05f + 0.9f * i / (rings - 1);
                RuntimeUi.CreateImage(frame, "Ring", Page, new Vector2(0.4955f, y - 0.008f), new Vector2(0.5045f, y + 0.008f),
                    Vector2.zero, Vector2.zero, RuntimeUi.Circle);
            }
        }

        private void BuildDetail(RectTransform page)
        {
            _empty = RuntimeUi.CreateText(page, "Empty", "아직 살펴본 단서가 없다.\n취조에서 단서를 손에 쥐면 여기에 적힌다.", _font, 30f,
                MockupStyle.InkSoft, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            _empty.textWrappingMode = TextWrappingModes.Normal;

            _detail = RuntimeUi.CreateStretched(page, "Detail").gameObject;
            var detail = (RectTransform)_detail.transform;

            var photo = RuntimeUi.CreateImage(detail, "Photo", PhotoBack, new Vector2(0.06f, 0.50f), new Vector2(0.48f, 0.94f), Vector2.zero, Vector2.zero);
            _icon = RuntimeUi.CreateImage(photo.rectTransform, "Icon", Color.white, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            _icon.preserveAspect = true;

            _title = RuntimeUi.CreateText(detail, "Title", "", _font, 40f, MockupStyle.Ink, TextAlignmentOptions.TopLeft,
                new Vector2(0.53f, 0.78f), new Vector2(0.96f, 0.94f));
            _title.textWrappingMode = TextWrappingModes.Normal;
            _title.fontStyle = FontStyles.Bold;
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 24f;
            _title.fontSizeMax = 40f;

            _time = BuildSticky(detail, "Time Note", TimeNote, new Vector2(0.55f, 0.50f), new Vector2(0.94f, 0.74f), -3f);
            _person = BuildSticky(detail, "Person Note", PersonNote, new Vector2(0.07f, 0.26f), new Vector2(0.47f, 0.46f), 3f);
            _emotion = BuildSticky(detail, "Emotion Note", EmotionNote, new Vector2(0.53f, 0.26f), new Vector2(0.93f, 0.46f), -2f);

            var storyBox = RuntimeUi.CreateImage(detail, "Story", StoryBack, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.22f), Vector2.zero, Vector2.zero);
            MockupStyle.AddPaperEdge(storyBox.gameObject, shadow: false);
            _storyScroll = BuildScroll(storyBox.rectTransform, "Story", new Vector2(14f, 10f), out var storyContent);
            _story = RuntimeUi.CreateText(storyContent, "Text", "", _font, 26f, MockupStyle.Ink, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f));
            _story.textWrappingMode = TextWrappingModes.Normal;
            var storyLayout = storyContent.gameObject.AddComponent<VerticalLayoutGroup>();
            storyLayout.childControlHeight = true;
            storyLayout.childControlWidth = true;
            storyLayout.childForceExpandHeight = false;
            storyContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private TMP_Text BuildSticky(RectTransform parent, string name, Color paper, Vector2 min, Vector2 max, float angle)
        {
            var note = RuntimeUi.CreateImage(parent, name, paper, min, max, Vector2.zero, Vector2.zero);
            note.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            MockupStyle.AddPaperEdge(note.gameObject);
            var text = RuntimeUi.CreateText(note.rectTransform, "Text", "", _font, 30f, MockupStyle.Ink, TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 30f;
            return text;
        }

        private void BuildList(RectTransform page)
        {
            var header = RuntimeUi.CreateText(page, "Header", $"단서 목록  <size=70%>({_clues.Count})</size>", _font, 38f, MockupStyle.Ink,
                TextAlignmentOptions.BottomLeft, new Vector2(0.06f, 0.885f), new Vector2(0.94f, 0.965f));
            header.fontStyle = FontStyles.Bold;
            RuntimeUi.CreateImage(page, "Header Line", PageLine, new Vector2(0.06f, 0.875f), new Vector2(0.94f, 0.875f), new Vector2(0f, -1f), new Vector2(0f, 1f));

            var area = RuntimeUi.CreateRect(page, "List", new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
            _listScroll = BuildScroll(area, "List", Vector2.zero, out var content);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (var i = 0; i < _clues.Count; i++) _rows.Add(BuildRow(content, i, _clues[i]));
        }

        private Image BuildRow(RectTransform content, int index, ClueDefinition def)
        {
            var row = RuntimeUi.CreateImage(content, "Row " + def.Id, RowIdle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, raycastTarget: true);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;

            var button = AddButton(row, () =>
            {
                UiSoundHooks.Play(UiSoundCue.ButtonClick);
                Select(index);
            });
            button.transition = Selectable.Transition.None;
            var hover = row.gameObject.AddComponent<RowHoverTint>();
            hover.Init(row, () => _selected == index);

            var thumbBack = RuntimeUi.CreateImage(row.rectTransform, "Thumb", PhotoBack, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            thumbBack.rectTransform.sizeDelta = new Vector2(76f, 76f);
            thumbBack.rectTransform.anchoredPosition = new Vector2(46f, 0f);
            var icon = RuntimeUi.CreateImage(thumbBack.rectTransform, "Icon", Color.white, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero,
                UiIcons.Get(def.Id));
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;

            var name = RuntimeUi.CreateText(row.rectTransform, "Name", def.DisplayName, _font, 32f, MockupStyle.Ink, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one);
            name.rectTransform.offsetMin = new Vector2(100f, 0f);
            name.overflowMode = TextOverflowModes.Ellipsis;
            return row;
        }

        /// <summary>휠로 넘기는 세로 스크롤 — 단서 책의 스토리 스크롤과 같은 짜임(뷰포트는 RectMask2D, 내용은 ContentSizeFitter로 길이를 정한다).</summary>
        private static ScrollRect BuildScroll(RectTransform area, string name, Vector2 inset, out RectTransform content)
        {
            var viewport = RuntimeUi.CreateImage(area, name + " Viewport", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one,
                inset, -inset, raycastTarget: true).rectTransform; // 투명하지만 레이캐스트를 받아야 휠이 먹는다.
            viewport.gameObject.AddComponent<RectMask2D>();

            content = RuntimeUi.CreateRect(viewport, name + " Content", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);

            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = false;
            return scroll;
        }

        private static Button AddButton(Graphic target, UnityEngine.Events.UnityAction onClick)
        {
            var button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>목록 줄에 마우스를 올리면 옅게 칠한다(선택된 줄은 그대로).</summary>
        private sealed class RowHoverTint : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
        {
            private Image _image;
            private System.Func<bool> _isSelected;

            public void Init(Image image, System.Func<bool> isSelected)
            {
                _image = image;
                _isSelected = isSelected;
            }

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (!_isSelected()) _image.color = RowHover;
            }

            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (!_isSelected()) _image.color = RowIdle;
            }
        }
    }
}
