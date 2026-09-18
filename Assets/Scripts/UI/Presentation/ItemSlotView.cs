using System;
using BlueComplex.Core.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>아이템 슬롯 하나의 표시 + 클릭 입력 중계. 사용 판정은 하지 않는다.</summary>
    public sealed class ItemSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        private static readonly Color FilledColor = new Color32(120, 120, 130, 200);
        private static readonly Color EmptyColor = new Color32(60, 60, 65, 100);

        public ItemDefinition Item { get; private set; }

        /// <summary>슬롯 클릭을 그대로 중계한다 — TurnRunner.UseItem 호출은 컨트롤러가 한다.</summary>
        public event Action<ItemDefinition> Clicked;

        public void Render(ItemDefinition item)
        {
            Item = item;
            _background.color = FilledColor;
            _nameText.text = item.DisplayName;
            _descriptionText.text = item.Description;
        }

        public void SetEmpty()
        {
            Item = null;
            _background.color = EmptyColor;
            _nameText.text = "(비어있음)";
            _descriptionText.text = string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Item != null) Clicked?.Invoke(Item);
        }
    }
}
