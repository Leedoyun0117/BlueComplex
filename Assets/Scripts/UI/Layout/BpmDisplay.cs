using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>모니터 오른쪽 화면: BPM 숫자 + 상태 배지(매우 침체/침체/안정/흥분/매우 흥분). 숫자·배지 색은 심박수 구간을 따른다.
    /// 값은 HeartRateController(→ Presenter가 타이밍을 쥔다)가 넣어 준다.</summary>
    public sealed class BpmDisplay : MonoBehaviour
    {
        /// <summary>배지 바탕이 화면 색에서 상태 색 쪽으로 섞이는 정도. 바탕은 반드시 불투명해야 한다 — 테두리(Outline)는 그래픽 모양 그대로 채운 사본을 뒤에 깔아서, 반투명이면 배지가 속이 찬 색 블록으로 보인다.</summary>
        private const float BadgeFillMix = 0.2f;

        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private Image _badgeFill;
        [SerializeField] private Outline _badgeOutline;

        public RectTransform Root => (RectTransform)transform;

        public void SetValue(int bpm) => SetValue(bpm, Color.white);

        /// <param name="stateLabel">배지에 적을 상태 이름. null이면 배지는 그대로 둔다.</param>
        public void SetValue(int bpm, Color color, string stateLabel = null)
        {
            if (_label != null)
            {
                _label.text = bpm.ToString();
                _label.color = color;
            }

            if (stateLabel == null) return;

            if (_badgeText != null)
            {
                _badgeText.text = stateLabel;
                _badgeText.color = color;
            }

            if (_badgeFill != null) _badgeFill.color = Color.Lerp(MockupStyle.MonitorScreen, color, BadgeFillMix);
            if (_badgeOutline != null) _badgeOutline.effectColor = color;
        }
    }
}
