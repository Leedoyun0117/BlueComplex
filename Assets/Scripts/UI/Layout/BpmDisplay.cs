using TMPro;
using UnityEngine;

namespace BlueComplex.UI.Layout
{
    /// <summary>BPM 숫자 표시. 2단계에서 Heartbeat.Changed를 구독해 SetValue를 호출하면 된다.</summary>
    public sealed class BpmDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public RectTransform Root => (RectTransform)transform;

        public void SetValue(int bpm) => SetValue(bpm, Color.white);

        public void SetValue(int bpm, Color color)
        {
            if (_label == null) return;
            _label.text = bpm.ToString();
            _label.color = color;
        }
    }
}
