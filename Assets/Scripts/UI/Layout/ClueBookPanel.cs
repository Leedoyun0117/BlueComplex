using BlueComplex.UI.Motion;
using BlueComplex.UI.Presentation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>
    /// 단서 정보 책(파일) UI(UI 가이드 "추가: 단서 정보 인터페이스"). 단서를 클릭하면 전체화면을 덮는 파일철 한 장이 펼쳐진다 —
    /// 왼쪽 페이지엔 그 단서의 사진과 시간/인물/감정 태그 쪽지가 흩어져 붙고(탭과 무관하게 항상 그대로), 오른쪽 페이지는
    /// "단서" 탭(번호·이름·스토리)과 "메뉴얼" 탭(감정·시간대·인물 분류표, 고정 내용) 사이를 오간다. 사진을 클릭하면 확대되고,
    /// 배경이나 "돌아가기"를 누르면 닫힌다.
    ///
    /// 모양은 Resources/UI/ClueBookPanel.prefab에 있다 — MainHud.prefab은 이미 손으로 다듬어진 상태라 그 안에 넣지 않고,
    /// 처음 필요할 때 이 프리팹을 루트 캔버스 아래에 인스턴스화한다(GetOrCreate). 클릭은 프리팹 안 Button의 onClick에
    /// 이 클래스의 Hide/ToggleZoom/ShowClueTab/ShowManualTab이 연결돼 있다. 프리팹은 ClueBookPanelPrefabTool로 처음 구웠다.
    /// </summary>
    public sealed class ClueBookPanel : MonoBehaviour
    {
        private const string ResourcePath = "UI/ClueBookPanel";
        private const float ZoomScale = 1.6f;
        private const float ZoomDuration = 0.25f;

        [Header("Left page")]
        [SerializeField] private RectTransform _photoArea;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _timeCardText;
        [SerializeField] private TMP_Text _personCardText;
        [SerializeField] private TMP_Text _emotionCardText;

        [Header("Right page")]
        [SerializeField] private GameObject _clueTab;
        [SerializeField] private GameObject _manualTab;
        [SerializeField] private TMP_Text _clueNumberText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _storyText;

        [Header("Tab buttons")]
        [SerializeField] private Image _clueTabButton;
        [SerializeField] private Image _manualTabButton;
        [SerializeField] private Color _tabIdle = new Color32(150, 150, 154, 255);
        [SerializeField] private Color _tabActive = new Color32(116, 116, 120, 255);

        private bool _zoomed;
        private Tween _zoomTween;

        public static ClueBookPanel GetOrCreate(Transform canvasRoot)
        {
            var existing = canvasRoot.GetComponentInChildren<ClueBookPanel>(true);
            if (existing != null) return existing;

            var prefab = Resources.Load<ClueBookPanel>(ResourcePath);
            if (prefab == null)
                throw new System.InvalidOperationException($"단서 책 프리팹을 찾을 수 없다: Resources/{ResourcePath}.prefab");

            var panel = Instantiate(prefab, canvasRoot, false);
            panel.name = prefab.name;
            SetLayerRecursively(panel.gameObject, canvasRoot.gameObject.layer);
            panel.transform.SetAsLastSibling();
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
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            _zoomTween?.Kill();
            gameObject.SetActive(false);
        }

        public void ToggleZoom()
        {
            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            _zoomed = !_zoomed;
            _zoomTween?.Kill();
            _zoomTween = _photoArea.DOScale(_zoomed ? ZoomScale : 1f, ZoomDuration);
        }

        public void ShowClueTab()
        {
            _clueTab.SetActive(true);
            _manualTab.SetActive(false);
            _clueTabButton.color = _tabActive;
            _manualTabButton.color = _tabIdle;
        }

        public void ShowManualTab()
        {
            _clueTab.SetActive(false);
            _manualTab.SetActive(true);
            _clueTabButton.color = _tabIdle;
            _manualTabButton.color = _tabActive;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
