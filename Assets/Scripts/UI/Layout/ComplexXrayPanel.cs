using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>컴플렉스 인터페이스(엑스레이 판넬)의 빈 영역. 판넬 연출은 3단계에서 만든다.</summary>
    public sealed class ComplexXrayPanel : MonoBehaviour
    {
        public RectTransform Root => (RectTransform)transform;
    }
}
