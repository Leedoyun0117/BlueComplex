using BlueComplex.UI.Presentation;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>아이템 슬롯 2개. 표시만 한다 — 판정 없음, 코어 이벤트도 직접 구독하지 않는다.</summary>
    public sealed class ItemDisplayPanel : MonoBehaviour
    {
        [SerializeField] private ItemSlotView[] _slots;

        public RectTransform Root => (RectTransform)transform;
        public int SlotCount => _slots?.Length ?? 0;

        public ItemSlotView GetSlot(int index) => _slots[index];
    }
}
