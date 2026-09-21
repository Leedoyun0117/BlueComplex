using BlueComplex.Core.Clues;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>단서 카드 한 장의 표시 — 아이콘 + 이름만 보인다(목업). 태그와 스토리는 카드를 클릭하면 뜨는 단서 정보 책 UI(ClueBookPanel)가
    /// <see cref="ViewModel"/>로 보여 준다. 드래그 입력은 별도 ClueCardDragHandler가 맡는다.</summary>
    public sealed class ClueCardView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Image _iconImage;
        // 속성/스토리 글자는 카드에서 걷어냈다(책 UI로 넘어감). 예전에 구운 프리팹이 남아 있어도 깨지지 않게 참조만 받아 비어 있으면 무시한다.
        [SerializeField] private TMP_Text _attributesText;
        [SerializeField] private TMP_Text _storyText;
        // 사용 횟수 제한이 없어져 더 쓰지 않는다. 이미 구워진 프리팹에 라벨 오브젝트가 남아 있으므로 참조만 유지해 숨긴다.
        [SerializeField] private TMP_Text _usesText;

        public Image Background => _background;
        public ClueInstance Card { get; private set; }
        public bool IsEmpty => Card == null;

        private CanvasGroup _group;

        /// <summary>마지막으로 그린 뷰모델 — 단서 정보 책 UI(ClueBookPanel)가 클릭 시 같은 내용을 그대로 보여주려고 캐시해 둔다.</summary>
        public ClueCardViewModel ViewModel { get; private set; }

        public void Render(ClueInstance card, ClueCardViewModel vm)
        {
            Card = card;
            ViewModel = vm;
            gameObject.SetActive(true);
            SetVisible(true);

            _titleText.text = vm.Title;
            SetIcon(card.Definition.Id);
            if (_attributesText != null)
            {
                _attributesText.text = vm.AttributesHidden
                    ? string.Empty
                    : $"시간: {vm.TimeText}\n인물: {string.Join(", ", vm.PersonTexts)}\n감정: {string.Join(", ", vm.EmotionTexts)}";
            }

            if (_storyText != null) _storyText.text = vm.StoryText;
            HideLegacyUsesLabel();
        }

        /// <summary>손패에서 빠진 슬롯. 글자만 지우면 카드 배경이 빈 상자로 남아 손패가 그대로인 것처럼 보이므로 슬롯 자체를 감춘다.
        /// 오브젝트를 끄지 않고 투명하게만 두는 건 트레이 레이아웃에서 남은 카드의 폭과 자리가 흔들리지 않게 하려는 것이다.</summary>
        public void SetEmpty()
        {
            Card = null;
            _titleText.text = string.Empty;
            if (_attributesText != null) _attributesText.text = string.Empty;
            if (_storyText != null) _storyText.text = string.Empty;
            if (_iconImage != null) _iconImage.enabled = false;
            HideLegacyUsesLabel();
            SetVisible(false);
        }

        private void SetIcon(string clueId)
        {
            if (_iconImage == null) return;

            var icon = UiIcons.Get(clueId);
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        private void SetVisible(bool visible)
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            }

            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
        }

        private void HideLegacyUsesLabel()
        {
            if (_usesText != null) _usesText.gameObject.SetActive(false);
        }
    }
}
