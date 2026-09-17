using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>단서 카드 4장의 빈 카드 트레이. 카드별 내용/포맷은 2단계에서 채운다.</summary>
    public sealed class ClueCardTray : MonoBehaviour
    {
        [SerializeField] private Image[] _cards;

        public RectTransform Root => (RectTransform)transform;
        public int CardCount => _cards?.Length ?? 0;

        public Image GetCard(int index) => _cards[index];
    }
}
