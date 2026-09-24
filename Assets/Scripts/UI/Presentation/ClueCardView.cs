using BlueComplex.Core.Clues;
using BlueComplex.UI.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>단서 카드 한 장의 표시 — 아이콘 + 이름만 보인다(목업). 태그와 스토리는 카드를 클릭하면 뜨는 단서 정보 책 UI(ClueBookPanel)가
    /// <see cref="ViewModel"/>로 보여 준다. 드래그 입력은 별도 ClueCardDragHandler가 맡는다.</summary>
    public sealed class ClueCardView : MonoBehaviour, ITargetHighlight
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
        /// <summary>아이템 대상 선택 모드에서 고를 수 있는 카드면 테두리가 깜박인다(선택적 기억).</summary>
        public void SetTargetable(bool on) => TargetPulse.Set(this, on && !IsEmpty);

        public void SetEmpty()
        {
            Card = null;
            TargetPulse.Set(this, false);
            _titleText.text = string.Empty;
            if (_attributesText != null) _attributesText.text = string.Empty;
            if (_storyText != null) _storyText.text = string.Empty;
            if (_iconImage != null) _iconImage.enabled = false;
            HideLegacyUsesLabel();
            SetVisible(false);
        }

        /// <summary>집어 든 동안 원래 자리에 남는 카드를 흐리게 한다 — 손에 든 카드(고스트)가 자리를 떠난 것처럼 보이게.</summary>
        public void SetLifted(bool lifted)
        {
            if (IsEmpty) return;
            EnsureGroup().alpha = lifted ? 0.28f : 1f;
        }

        /// <summary>드래그가 끝난 뒤 카드의 보임 상태를 손패 내용에 맞춘다(채워졌으면 보이고 비었으면 감춘다).</summary>
        public void RestoreVisibility() => EnsureGroup().alpha = IsEmpty ? 0f : 1f;

        /// <summary>
        /// 지금 카드의 겉모습(배경·아이콘·이름)만 복제한 입력 없는 카드 한 장을 <paramref name="parent"/> 아래에 만든다 — 드래그 중 커서를 따라다니는 "손에 든 카드".
        /// 슬롯 자체(이 오브젝트)는 손패 슬롯이라 드롭 직후 곧바로 다음 카드로 갱신되므로, 날아가는 카드는 슬롯과 분리해야 한다.
        /// </summary>
        public RectTransform BuildGhost(Transform parent)
        {
            var root = new GameObject("Clue Card Ghost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image))
            {
                layer = gameObject.layer
            };
            root.transform.SetParent(parent, false);

            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = ((RectTransform)transform).rect.size;

            var background = root.GetComponent<Image>();
            background.sprite = _background.sprite;
            background.type = _background.type;
            background.color = _background.color;

            // 원본 카드와 같은 윤곽 무늬여야 집어 들었을 때 종이가 바뀌어 보이지 않는다(시드 공유).
            var sourceSkin = _background.GetComponent<PaperPanel>();
            if (sourceSkin != null) PaperPanel.Skin(background, _background.material, sourceSkin.Seed);
            MockupStyle.AddPaperEdge(root);

            if (_iconImage != null && _iconImage.enabled) Instantiate(_iconImage.gameObject, rect, false);
            if (_titleText != null) Instantiate(_titleText.gameObject, rect, false);

            // 고스트는 입력을 받지 않는다 — 커서 밑의 드롭 영역이 가려지면 안 된다.
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            return rect;
        }

        private CanvasGroup EnsureGroup()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            }

            return _group;
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
            var group = EnsureGroup();
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
        }

        private void HideLegacyUsesLabel()
        {
            if (_usesText != null) _usesText.gameObject.SetActive(false);
        }
    }
}
