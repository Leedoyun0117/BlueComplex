using System;
using System.Collections;
using BlueComplex.Core.Items;
using BlueComplex.UI.Effects.DLJ;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>아이템 슬롯 하나(종이 카드 한 장의 자리)의 표시 + 클릭 입력 중계. 사용 판정은 하지 않는다.
    /// 슬롯에는 아이콘과 이름만 보이고, 설명은 공유 TooltipPopup(ComplexRowView와 동일 패턴)으로 뺐다 —
    /// 슬롯 자체엔 담기엔 너무 길다. 카드가 없는 자리는 파인 빈 칸(<see cref="SetEmpty"/>)으로 남는다.
    ///
    /// 움직임: 사용한 카드는 구겨지며 사라지고(<see cref="PlayUse"/>), 새 카드는 빈 칸에 눌려 끼워진다(<see cref="Render"/>의 insert).
    /// 등장 시 DLJ 시각 복제본만 움직여 세로 레이아웃 그룹의 슬롯 위치는 유지한다.
    /// 등장 시간은 DLJItemArrivalSettings, 사용 시간은 UiMotionSettings(인스펙터)에서 온다.</summary>
    public sealed class ItemSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverDelay = 0.25f;
        private const float EmptyEdgeAlpha = 0.45f;

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TooltipPopup _tooltip;

        private static readonly Color FilledColor = MockupStyle.Card;

        /// <summary>카드가 빠진 자리: 종이(패널)보다 살짝 어두운 불투명한 파인 자리(종이 셰이더는 밝은 무채색만 윤곽선을 그리므로 너무 어둡거나 색이 있으면 안 된다). 반투명으로 두면 안 된다 — 테두리(Outline)가 그래픽 모양 그대로 채운 사본을 뒤에 깔아서 속이 찬 어두운 블록으로 보인다.</summary>
        private static readonly Color EmptyColor = new Color32(224, 228, 224, 255);
        private static readonly Color CrumpledColor = new Color32(192, 196, 192, 255);

        private Coroutine _hoverRoutine;
        private Outline _edge;
        private Color _edgeColor;
        private CanvasGroup _group;
        private Tween _motion;
        private bool _inserting;
        private Tween _selectTween;
        private bool _selected;
        private static readonly Color SelectedEdge = new Color32(255, 200, 60, 255);

        public ItemDefinition Item { get; private set; }

        /// <summary>슬롯 클릭을 그대로 중계한다 — TurnRunner.UseItem 호출은 컨트롤러가 한다.</summary>
        public event Action<ItemDefinition> Clicked;

        private void Awake()
        {
            _edge = GetComponent<Outline>();
            if (_edge != null) _edgeColor = _edge.effectColor;
            EnsureGroup();
        }

        /// <param name="insert">true면 카드가 빈 칸에 눌려 끼워지는 움직임을 재생한다.</param>
        public void Render(ItemDefinition item, bool insert = false)
        {
            Item = item;
            gameObject.SetActive(true);
            ResetPose();
            _background.color = FilledColor;
            _background.raycastTarget = true;
            SetEdgeAlpha(1f);
            _nameText.text = item.DisplayName;

            if (_iconImage != null)
            {
                var icon = UiIcons.Get(item.Id);
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }

            if (insert) PlayInsert();
        }

        /// <summary>DLJ: ItemController의 레이아웃 리빌드 후 알림과 호환되는 진입점.
        /// 등장 연출은 슬롯 위치를 바꾸지 않고 복제본이 현재 슬롯을 따라가므로 초기화할 오프셋이 없다.</summary>
        public void ForgetLayoutOffset()
        {
            // DLJ_ItemArrivalMotion.FollowSlot이 매 프레임 리빌드된 슬롯 위치를 반영한다.
        }

        /// <summary>아이템 대상 선택 모드에서 이 카드가 "지금 쓰려는 카드"임을 보인다 — 살짝 들리고 테두리가 금빛이 된다. 끄면 원래대로.</summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            _selectTween?.Kill();
            if (Item == null) return;

            var rect = (RectTransform)transform;
            _selectTween = rect.DOScale(selected ? 1.06f : 1f, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
            if (_edge != null) _edge.effectColor = selected ? SelectedEdge : _edgeColor;
        }

        /// <summary>쓸 수 없는 아이템을 눌렀을 때 카드가 좌우로 짧게 떨린다(대상이 없거나 스테이지가 끝난 경우).</summary>
        public void PlayRejected()
        {
            if (Item == null) return;

            _selectTween?.Kill();
            var rect = (RectTransform)transform;
            rect.localRotation = Quaternion.identity;
            _selectTween = rect.DOPunchRotation(new Vector3(0f, 0f, 4f), 0.3f, 14, 0.6f).SetUpdate(true).SetTarget(this)
                .OnComplete(() => rect.localRotation = Quaternion.identity);
        }

        /// <summary>카드가 없는 자리 — 파인 빈 칸만 남긴다(다음에 카드가 끼워질 자리).</summary>
        public void SetEmpty()
        {
            Item = null;
            HideTooltip();
            gameObject.SetActive(true);
            ResetPose();

            _nameText.text = string.Empty;
            if (_iconImage != null) _iconImage.enabled = false;

            _background.color = EmptyColor;
            _background.raycastTarget = false;
            SetEdgeAlpha(EmptyEdgeAlpha);
        }

        /// <summary>보유 한도 밖의 자리 — 아예 감춘다(빈 상자를 한도보다 많이 늘어놓지 않는다).</summary>
        public void Hide()
        {
            Item = null;
            HideTooltip();
            ResetPose();
            gameObject.SetActive(false);
        }

        /// <summary>사용한 카드가 구겨져 던져지듯 사라진다: 꾹 눌려 찌그러지고 → 구겨진 색으로 오그라들며 기울고 → 작아지며 사라진다.
        /// 끝나면 슬롯은 원래 자세로 돌아가 있고 <paramref name="onDone"/>이 불린다(그때 슬롯을 비운다).</summary>
        public void PlayUse(Action onDone)
        {
            HideTooltip();
            _motion?.Kill();
            _selectTween?.Kill();
            _background.raycastTarget = false;
            UiSoundHooks.Play(UiSoundCue.ItemUse);

            var total = UiMotion.Settings.itemUse;
            var rect = (RectTransform)transform;
            var group = EnsureGroup();

            _motion = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(rect.DOScale(new Vector3(1.06f, 0.84f, 1f), total * 0.2f).SetEase(Ease.OutQuad))
                .Join(rect.DOLocalRotate(new Vector3(0f, 0f, 3f), total * 0.2f))
                .Append(rect.DOScale(0.7f, total * 0.35f).SetEase(Ease.InOutQuad))
                .Join(rect.DOLocalRotate(new Vector3(0f, 0f, -13f), total * 0.35f))
                .Join(_background.DOColor(CrumpledColor, total * 0.35f))
                .Append(rect.DOScale(0.05f, total * 0.45f).SetEase(Ease.InBack))
                .Join(rect.DOLocalRotate(new Vector3(0f, 0f, -42f), total * 0.45f).SetEase(Ease.InQuad))
                .Join(group.DOFade(0f, total * 0.45f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    ResetPose();
                    onDone?.Invoke();
                });
        }

        /// <summary>빈 칸 옆에서 매우 작게 튀어나와 빠르게 커지고(1) → 커진 채로 빈 칸 쪽으로 움직이고(2) →
        /// 빠르게 끼워지며 찰칵 소리가 난다(3).</summary>
        private void PlayInsert()
        {
            _motion?.Kill();
            HideTooltip();
            _inserting = true;
            _motion = DLJ_ItemArrivalMotion.Play((RectTransform)transform, _background, _nameText, _iconImage,
                EmptyColor, () => _inserting = false)
                .AppendCallback(() => UiSoundHooks.Play(UiSoundCue.Pin));
        }

        private void ResetPose()
        {
            _motion?.Kill();
            _selectTween?.Kill();
            _selected = false;
            if (_edge != null) _edge.effectColor = _edgeColor;
            var rect = (RectTransform)transform;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            EnsureGroup().alpha = 1f;
        }

        private CanvasGroup EnsureGroup()
        {
            if (_group != null) return _group;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            return _group;
        }

        private void SetEdgeAlpha(float factor)
        {
            if (_edge == null) return;

            var color = _selected ? SelectedEdge : _edgeColor;
            color.a *= factor;
            _edge.effectColor = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 오른쪽 클릭은 대상 선택 취소용이라 아이템 사용으로 치지 않는다.
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Item != null && !_inserting) Clicked?.Invoke(Item);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Item == null || _inserting) return;
            _hoverRoutine = StartCoroutine(HoverThenShow());
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        private void OnDisable()
        {
            _motion?.Kill();
            _selectTween?.Kill();
            HideTooltip();
        }

        private IEnumerator HoverThenShow()
        {
            yield return new WaitForSeconds(HoverDelay);
            if (Item == null || _tooltip == null) yield break;
            _tooltip.Show(Item.DisplayName, Item.Description, transform.position);
        }

        private void HideTooltip()
        {
            if (_hoverRoutine != null)
            {
                StopCoroutine(_hoverRoutine);
                _hoverRoutine = null;
            }
            _tooltip?.Hide();
        }
    }
}
