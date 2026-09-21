using System;
using System.Collections;
using BlueComplex.Core.Items;
using BlueComplex.UI.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>아이템 슬롯 하나의 표시 + 클릭 입력 중계. 사용 판정은 하지 않는다.
    /// 슬롯에는 아이콘과 이름만 보이고, 설명은 공유 TooltipPopup(ComplexRowView와 동일 패턴)으로 뺐다 —
    /// 슬롯 자체엔 담기엔 너무 길다. 비어 있는 슬롯은 아예 감춘다(빈 상자를 늘어놓지 않는다).</summary>
    public sealed class ItemSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverDelay = 0.25f;

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TooltipPopup _tooltip;

        private static readonly Color FilledColor = MockupStyle.Card;

        private Coroutine _hoverRoutine;

        public ItemDefinition Item { get; private set; }

        /// <summary>슬롯 클릭을 그대로 중계한다 — TurnRunner.UseItem 호출은 컨트롤러가 한다.</summary>
        public event Action<ItemDefinition> Clicked;

        public void Render(ItemDefinition item)
        {
            Item = item;
            gameObject.SetActive(true);
            _background.color = FilledColor;
            _nameText.text = item.DisplayName;

            if (_iconImage == null) return;
            var icon = UiIcons.Get(item.Id);
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        public void SetEmpty()
        {
            Item = null;
            HideTooltip();
            _nameText.text = string.Empty;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Item != null) Clicked?.Invoke(Item);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Item == null) return;
            _hoverRoutine = StartCoroutine(HoverThenShow());
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        private void OnDisable() => HideTooltip();

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
