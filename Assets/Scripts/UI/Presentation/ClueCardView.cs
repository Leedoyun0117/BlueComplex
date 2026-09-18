using BlueComplex.Core.Clues;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>단서 카드 한 장의 표시. 드래그 입력은 별도 ClueCardDragHandler가 맡는다.</summary>
    public sealed class ClueCardView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _attributesText;
        [SerializeField] private TMP_Text _storyText;
        [SerializeField] private TMP_Text _usesText;

        public Image Background => _background;
        public ClueInstance Card { get; private set; }
        public bool IsEmpty => Card == null;

        /// <summary>마지막으로 그린 뷰모델 — 단서 정보 책 UI(ClueBookPanel)가 클릭 시 같은 내용을 그대로 보여주려고 캐시해 둔다.</summary>
        public ClueCardViewModel ViewModel { get; private set; }

        public void Render(ClueInstance card, ClueCardViewModel vm)
        {
            Card = card;
            ViewModel = vm;
            gameObject.SetActive(true);

            _titleText.text = vm.Title;
            _attributesText.text = vm.AttributesHidden
                ? string.Empty
                : $"시간: {vm.TimeText}\n인물: {string.Join(", ", vm.PersonTexts)}\n감정: {string.Join(", ", vm.EmotionTexts)}";
            _storyText.text = vm.StoryText;
            _usesText.text = $"남은 사용: {vm.RemainingUses}회";
        }

        public void SetEmpty()
        {
            Card = null;
            _titleText.text = string.Empty;
            _attributesText.text = string.Empty;
            _storyText.text = string.Empty;
            _usesText.text = string.Empty;
        }
    }
}
