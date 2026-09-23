using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>모니터 오른쪽 화면: BPM 숫자 + 상태 배지(매우 침체/침체/안정/흥분/매우 흥분). 숫자·배지 색은 심박수 구간을 따른다 —
    /// 침체 쪽은 차가운 색, 흥분 쪽은 따뜻한 색, 안정은 중립(청록).
    /// 값은 HeartRateController(→ Presenter가 타이밍을 쥔다)가 넣어 준다. 숫자는 파형 높이·속도와 같은 시간 동안 새 값으로 올라가거나 내려가고,
    /// 구간이 바뀌면 배지가 깜빡이며 새 이름·색으로 교체된다.</summary>
    public sealed class BpmDisplay : MonoBehaviour
    {
        /// <summary>배지 바탕이 화면 색에서 상태 색 쪽으로 섞이는 정도. 바탕은 반드시 불투명해야 한다 — 테두리(Outline)는 그래픽 모양 그대로 채운 사본을 뒤에 깔아서, 반투명이면 배지가 속이 찬 색 블록으로 보인다.</summary>
        private const float BadgeFillMix = 0.2f;

        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private Image _badgeFill;
        [SerializeField] private Outline _badgeOutline;

        private CanvasGroup _badgeGroup;
        private Tween _numberTween;
        private Tween _numberColorTween;
        private Tween _badgeTween;
        private float _shownNumber;
        private string _shownState;

        public RectTransform Root => (RectTransform)transform;

        public void SetValue(int bpm) => SetValue(bpm, Color.white);

        /// <param name="stateLabel">배지에 적을 상태 이름. null이면 배지는 그대로 둔다.</param>
        /// <param name="animate">false면 숫자·배지를 바로 그 값으로 놓는다(세션 시작·재시작).</param>
        public void SetValue(int bpm, Color color, string stateLabel = null, bool animate = false)
        {
            SetNumber(bpm, color, animate ? UiMotion.Settings.heartTransition : 0f);

            if (stateLabel == null) return;

            if (!animate || _shownState == null || _shownState == stateLabel || !isActiveAndEnabled || _badgeFill == null)
            {
                _badgeTween?.Kill();
                if (_badgeGroup != null) _badgeGroup.alpha = 1f;
                ApplyBadge(stateLabel, color);
                return;
            }

            FlickerBadge(stateLabel, color);
        }

        private void SetNumber(int bpm, Color color, float seconds)
        {
            _numberTween?.Kill();
            _numberColorTween?.Kill();

            if (seconds <= 0f || !isActiveAndEnabled)
            {
                _shownNumber = bpm;
                if (_label != null)
                {
                    _label.color = color;
                    _label.text = bpm.ToString();
                }

                return;
            }

            // 숫자 색도 파형 선 색과 같은 시간 동안 새 구간의 색으로 넘어간다.
            if (_label != null) _numberColorTween = _label.DOColor(color, seconds).SetUpdate(true).SetTarget(this);

            _numberTween = DOTween.To(() => _shownNumber, v =>
                {
                    _shownNumber = v;
                    if (_label != null) _label.text = Mathf.RoundToInt(v).ToString();
                }, bpm, seconds)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this);
        }

        /// <summary>구간이 바뀌면 배지가 꺼졌다 켜지기를 두 번 하며 그 사이에 이름·색이 바뀐다.</summary>
        private void FlickerBadge(string stateLabel, Color color)
        {
            EnsureBadgeGroup();
            _badgeTween?.Kill();

            var unit = UiMotion.Settings.badgeFlicker / 4f;
            _badgeTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(_badgeGroup.DOFade(0.05f, unit))
                .AppendCallback(() => ApplyBadge(stateLabel, color))
                .Append(_badgeGroup.DOFade(1f, unit))
                .Append(_badgeGroup.DOFade(0.2f, unit))
                .Append(_badgeGroup.DOFade(1f, unit));
        }

        private void ApplyBadge(string stateLabel, Color color)
        {
            _shownState = stateLabel;

            if (_badgeText != null)
            {
                _badgeText.text = stateLabel;
                _badgeText.color = color;
            }

            if (_badgeFill != null) _badgeFill.color = Color.Lerp(MockupStyle.MonitorScreen, color, BadgeFillMix);
            if (_badgeOutline != null) _badgeOutline.effectColor = color;
        }

        private void EnsureBadgeGroup()
        {
            if (_badgeGroup != null || _badgeFill == null) return;

            _badgeGroup = _badgeFill.GetComponent<CanvasGroup>();
            if (_badgeGroup == null) _badgeGroup = _badgeFill.gameObject.AddComponent<CanvasGroup>();
        }

        private void OnDisable() => DOTween.Kill(this);
    }
}
