using System.Collections;
using BlueComplex.Core.Complexes;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>컴플렉스 목록의 행 하나. 이름/설명/남은 턴 막대 + 0.25초 호버 상세 팝업 + 발동 시 순차 발광.</summary>
    public sealed class ComplexRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverDelay = 0.25f;

        /// <summary>한 행이 빛나는 데 걸리는 시간(올라갔다 내려오는 전체). ComplexListView.PlaySequence가
        /// 다음 행으로 넘어가는 간격도 이 값을 그대로 쓴다 — 값이 어긋나면 겹치거나 빈 틈이 생긴다.</summary>
        public const float GlowDuration = 0.35f;

        private static readonly Color GlowColor = new Color32(255, 225, 120, 255);

        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Image _durationFill;
        [SerializeField] private TMP_Text _durationText;
        [SerializeField] private Vector3 _tooltipOffset = new(0f, 40f, 0f);

        private TooltipPopup _tooltip;
        private Coroutine _hoverRoutine;
        private ComplexInstance _complex;
        private Image _background;
        private Color _baseColor;
        private Tween _glowTween;

        public ComplexInstance Complex => _complex;

        public void Init(TooltipPopup tooltip) => _tooltip = tooltip;

        private void Awake()
        {
            // BuildComplexXrayPanel(UiLayoutSetupTool.cs)이 이 컴포넌트와 같은 GameObject에
            // 배경 Image를 붙인다 — 직렬화 필드로 안 받아도 항상 찾을 수 있다.
            _background = GetComponent<Image>();
            if (_background != null) _baseColor = _background.color;
        }

        public void Render(ComplexInstance complex)
        {
            _complex = complex;
            gameObject.SetActive(true);

            _nameText.text = complex.Definition.DisplayName;
            _descriptionText.text = complex.Definition.Description;

            var fraction = complex.Definition.DefaultDuration > 0
                ? Mathf.Clamp01(complex.RemainingTurns / (float)complex.Definition.DefaultDuration)
                : 0f;
            // Image.Type.Filled는 스프라이트가 없으면 안 채워지는 경우가 있어, 배경 바 위에
            // 폭을 anchorMax.x로 직접 조절하는 방식(심박수 바와 동일)으로 대신한다.
            var fillRect = _durationFill.rectTransform;
            fillRect.anchorMax = new Vector2(fraction, fillRect.anchorMax.y);
            _durationText.text = complex.RemainingTurns.ToString();
        }

        public void SetEmpty()
        {
            _complex = null;
            HideTooltip();
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_complex == null) return;
            _hoverRoutine = StartCoroutine(HoverThenShow());
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        /// <summary>이 컴플렉스가 이번 턴에 발동했음을 짧게 강조한다. 순서 제어는 ComplexListView가 쥔다 —
        /// 이 메서드는 자기 자신을 한 번 반짝이는 것 외에는 아무것도 모른다.</summary>
        public void PlayGlow()
        {
            if (_background == null) return;

            _glowTween?.Kill();
            _glowTween = DOTween.Sequence()
                .Append(_background.DOColor(GlowColor, GlowDuration / 2f))
                .Append(_background.DOColor(_baseColor, GlowDuration / 2f));
        }

        private void OnDisable()
        {
            HideTooltip();
            _glowTween?.Kill();
            if (_background != null) _background.color = _baseColor;
        }

        private IEnumerator HoverThenShow()
        {
            yield return new WaitForSeconds(HoverDelay);
            if (_complex == null || _tooltip == null) yield break;
            _tooltip.Show(_complex.Definition.DisplayName, _complex.Definition.Description,
                transform.position + _tooltipOffset);
        }

        private void HideTooltip()
        {
            if (_hoverRoutine != null)
            {
                StopCoroutine(_hoverRoutine);
                _hoverRoutine = null;
            }
            _tooltip?.Hide();
        }
    }
}
