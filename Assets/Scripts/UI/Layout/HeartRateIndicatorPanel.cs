using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>심박수 표시기의 빈 패널. 실제 파형/게이지 렌더링은 2단계에서 붙인다.</summary>
    public sealed class HeartRateIndicatorPanel : MonoBehaviour
    {
        [SerializeField] private Image _background;

        public RectTransform Root => (RectTransform)transform;
        public Image Background => _background;
    }
}
