using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 단서 정보 책 UI. 단서를 클릭하면 전체화면을 덮는 책 형태 패널이 뜨고, 한 번 더(페이지를)
    /// 클릭하면 확대된다. 배경(백드롭)을 클릭하면 닫힌다.
    ///
    /// MainHud.prefab은 이미 손으로 다듬어진 상태라(git 상 uncommitted 변경 존재) 재생성하면
    /// 그 작업을 날린다 — 그래서 이 패널은 프리팹에 미리 만들어 두지 않고, 처음 필요할 때
    /// 루트 캔버스 아래에 코드로 직접 짓는다(GetOrCreate). UiLayoutSetupTool 같은 에디터 툴이
    /// 아니라 순수 런타임 코드라 UnityEditor를 참조하지 않는다.
    /// </summary>
    public sealed class ClueBookPanel : MonoBehaviour
    {
        private const float ZoomScale = 1.5f;
        private const float ZoomDuration = 0.25f;

        private RectTransform _page;
        private TMP_Text _titleText;
        private TMP_Text _attributesText;
        private TMP_Text _storyText;
        private GameObject _overlay;
        private bool _zoomed;
        private Tween _zoomTween;

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

        public void Show(ClueCardViewModel vm)
        {
            _zoomed = false;
            _zoomTween?.Kill();
            _page.localScale = Vector3.one;

            _titleText.text = vm.Title;
            _attributesText.text = vm.AttributesHidden
                ? string.Empty
                : $"시간: {vm.TimeText}\n인물: {string.Join(", ", vm.PersonTexts)}\n감정: {string.Join(", ", vm.EmotionTexts)}";
            _storyText.text = vm.StoryText;

            _overlay.SetActive(true);
        }

        public void Hide() => _overlay.SetActive(false);

        private void ToggleZoom()
        {
            _zoomed = !_zoomed;
            _zoomTween?.Kill();
            _zoomTween = _page.DOScale(_zoomed ? ZoomScale : 1f, ZoomDuration);
        }

        private void Build(TMP_FontAsset font)
        {
            var backdrop = CreateImage(transform, "Backdrop", new Color(0f, 0f, 0f, 0.85f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = true;
            AddClickHandler(backdrop.gameObject, Hide);

            var page = CreateImage(backdrop.transform, "Page", new Color32(235, 225, 200, 255),
                new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.88f), Vector2.zero, Vector2.zero);
            page.raycastTarget = true;
            AddClickHandler(page.gameObject, ToggleZoom);
            _page = page.rectTransform;

            _titleText = CreateTmpText(page.transform, "Title", string.Empty, font, 30,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.85f), Vector2.one,
                new Vector2(24f, 0f), new Vector2(-24f, -20f));
            _titleText.color = Color.black;

            _attributesText = CreateTmpText(page.transform, "Attributes", string.Empty, font, 18,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.6f), new Vector2(1f, 0.85f),
                new Vector2(24f, 0f), new Vector2(-24f, 0f));
            _attributesText.color = Color.black;

            _storyText = CreateTmpText(page.transform, "Story", string.Empty, font, 18,
                TextAlignmentOptions.TopLeft, Vector2.zero, new Vector2(1f, 0.6f),
                new Vector2(24f, 20f), new Vector2(-24f, 0f));
            _storyText.color = Color.black;

            _overlay = gameObject;
            _overlay.SetActive(false);
        }

        private static void AddClickHandler(GameObject go, UnityEngine.Events.UnityAction onClick)
        {
            var trigger = go.AddComponent<ClickForward>();
            trigger.Clicked += onClick;
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

        /// <summary>배경/페이지 클릭을 UnityAction 이벤트로 중계하는 최소 헬퍼 — Image에 Button을
        /// 붙이면 생기는 기본 색 트랜지션 등 불필요한 장식 없이 클릭만 받으면 돼서 직접 구현한다.</summary>
        private sealed class ClickForward : MonoBehaviour, IPointerClickHandler
        {
            public event UnityEngine.Events.UnityAction Clicked;
            public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
        }
    }
}
