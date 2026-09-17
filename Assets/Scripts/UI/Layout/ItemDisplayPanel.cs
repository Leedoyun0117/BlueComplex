using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>아이템 슬롯 2개. 2단계에서 슬롯별 아이콘/개수를 채운다.</summary>
    public sealed class ItemDisplayPanel : MonoBehaviour
    {
        [SerializeField] private Image[] _slots;

        public RectTransform Root => (RectTransform)transform;
        public int SlotCount => _slots?.Length ?? 0;

        public Image GetSlot(int index) => _slots[index];
    }
}
