using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>BPM 숫자 표시. 2단계에서 Heartbeat.Changed를 구독해 SetValue를 호출하면 된다.</summary>
    public sealed class BpmDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public RectTransform Root => (RectTransform)transform;

        public void SetValue(int bpm)
        {
            if (_label != null) _label.text = bpm.ToString();
        }
    }
}
