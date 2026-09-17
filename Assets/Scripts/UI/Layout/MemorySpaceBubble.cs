using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>기억 공간(말풍선)의 빈 영역. 텍스트/연출은 2단계 이후.</summary>
    public sealed class MemorySpaceBubble : MonoBehaviour
    {
        [SerializeField] private Image _bubbleBackground;

        public RectTransform Root => (RectTransform)transform;
        public Image BubbleBackground => _bubbleBackground;
    }
}
