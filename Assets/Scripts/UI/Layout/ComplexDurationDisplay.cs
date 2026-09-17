using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>컴플렉스 지속 시간 표시의 빈 영역. 숫자/게이지는 2단계에서 붙인다.</summary>
    public sealed class ComplexDurationDisplay : MonoBehaviour
    {
        public RectTransform Root => (RectTransform)transform;
    }
}
