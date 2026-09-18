using System;
using System.Collections;
using BlueComplex.Core.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>아이템 슬롯 하나의 표시 + 클릭 입력 중계. 사용 판정은 하지 않는다.
    /// 이름은 항상 크게 보이고, 설명은 CRT 왜곡이 덜한 안쪽에서도 뭉개지지 않도록 공유
    /// TooltipPopup(ComplexRowView와 동일 패턴)으로 뺐다 — 슬롯 자체엔 늘 담기엔 너무 길다.</summary>
    public sealed class ItemSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverDelay = 0.25f;

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TooltipPopup _tooltip;
        [SerializeField] private Vector3 _tooltipOffset = new(0f, 60f, 0f);

        private static readonly Color FilledColor = new Color32(120, 120, 130, 200);
        private static readonly Color EmptyColor = new Color32(60, 60, 65, 100);

        private Coroutine _hoverRoutine;

        public ItemDefinition Item { get; private set; }

        /// <summary>슬롯 클릭을 그대로 중계한다 — TurnRunner.UseItem 호출은 컨트롤러가 한다.</summary>
        public event Action<ItemDefinition> Clicked;

        public void Render(ItemDefinition item)
        {
            Item = item;
            _background.color = FilledColor;
            _nameText.text = item.DisplayName;
        }

        public void SetEmpty()
        {
            Item = null;
            HideTooltip();
            _background.color = EmptyColor;
            _nameText.text = "(비어있음)";
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
            _tooltip.Show(Item.DisplayName, Item.Description, transform.position + _tooltipOffset);
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
