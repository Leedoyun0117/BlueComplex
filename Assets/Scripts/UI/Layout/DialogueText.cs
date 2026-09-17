using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>유키 대사 한 줄 placeholder. 2단계에서 대사 진행 로직이 SetLine을 호출한다.</summary>
    public sealed class DialogueText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public RectTransform Root => (RectTransform)transform;

        public void SetLine(string line)
        {
            if (_label != null) _label.text = line;
        }
    }
}
