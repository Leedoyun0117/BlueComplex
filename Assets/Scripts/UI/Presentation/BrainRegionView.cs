using System.Collections;
using BlueComplex.Core.Complexes;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 뇌의 한 영역(세 엽 + 뇌간). 이 컴포넌트가 붙은 Image는 그 영역만 초록으로 칠한 도트 오버레이다 — 평소엔 투명하고, 컴플렉스가 발동하면 빛난다.
    /// 영역 모양의 픽셀 알파로 호버를 판정하므로(alphaHitTestMinimumThreshold) 영역 밖은 뒤의 판넬 드래그를 막지 않는다.
    /// 컴플렉스가 할당된 영역만 이름 + 남은 턴 글자가 뜨고, 비어 있는 영역은 아무것도 안 보이며 입력도 안 받는다.
    /// 마우스를 0.25초 올리면(또는 클릭하면 바로) 컴플렉스 상세 팝업이 뜬다(ComplexRowView와 같은 규칙).
    /// </summary>
    public sealed class BrainRegionView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float HoverDelay = 0.25f;
        private const float HoverAlpha = 0.28f;

        private static readonly Color Rest = new(1f, 1f, 1f, 0f);
        private static readonly Color Hover = new(1f, 1f, 1f, HoverAlpha);
        private static readonly Color Peak = new(1f, 1f, 1f, 1f);

        [SerializeField] private Image _lit;
        [SerializeField] private TMP_Text _label;

        private TooltipPopup _tooltip;
        private Coroutine _hoverRoutine;
        private Tween _glowTween;
        private Tween _labelTween;

        public ComplexInstance Complex { get; private set; }

        public void Init(TooltipPopup tooltip) => _tooltip = tooltip;

        private void Awake()
        {
            if (_lit == null) _lit = GetComponent<Image>();
            // 알파 0인 메시는 캔버스가 그리지 않고 레이캐스트에서도 뺀다 — 호버 판정은 알파 0인 채로도 살아 있어야 한다.
            _lit.canvasRenderer.cullTransparentMesh = false;
            _lit.alphaHitTestMinimumThreshold = 0.5f;
            _lit.color = Rest;
            _lit.raycastTarget = false;
        }

        /// <summary>이 영역에 컴플렉스를 할당한다(null이면 비운다). 남은 턴이 바뀌면 다시 불러 글자를 갱신한다.</summary>
        public void Assign(ComplexInstance complex)
        {
            Complex = complex;
            _lit.raycastTarget = complex != null;

            if (complex == null)
            {
                HideTooltip();
                _label.text = string.Empty;
                _glowTween?.Kill();
                _lit.color = Rest;
                return;
            }

            _label.text = $"{TrimSuffix(complex.Definition.DisplayName)}\n{complex.RemainingTurns}턴";
        }

        /// <summary>이 영역이 한 번 밝게 빛난다(발동). 순서는 Presenter가 쥔다.</summary>
        public void PlayGlow()
        {
            _glowTween?.Kill();
            _labelTween?.Kill();

            var half = UiMotion.Settings.brainGlow * 0.5f;
            _glowTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(_lit.DOColor(Peak, half).SetEase(Ease.OutQuad))
                .Append(_lit.DOColor(_hovered ? Hover : Rest, half).SetEase(Ease.InQuad));

            var rect = _label.rectTransform;
            rect.localScale = Vector3.one;
            _labelTween = rect.DOScale(1.25f, half).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
        }

        private bool _hovered;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Complex == null) return;

            _hovered = true;
            if (_glowTween == null || !_glowTween.IsActive()) _lit.DOColor(Hover, 0.12f).SetUpdate(true).SetTarget(this);
            _hoverRoutine = StartCoroutine(HoverThenShow());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            if (_glowTween == null || !_glowTween.IsActive()) _lit.DOColor(Rest, 0.12f).SetUpdate(true).SetTarget(this);
            HideTooltip();
        }

        /// <summary>호버를 기다리지 않고 바로 상세를 띄운다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Complex == null) return;

            StopHoverRoutine();
            ShowTooltip();
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            _hovered = false;
            _lit.color = Rest;
            HideTooltip();
        }

        private IEnumerator HoverThenShow()
        {
            yield return new WaitForSecondsRealtime(HoverDelay);
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            if (Complex == null || _tooltip == null) return;

            var definition = Complex.Definition;
            _tooltip.Show(definition.DisplayName, $"{definition.Description}\n\n남은 지속: {Complex.RemainingTurns}턴", transform.position);
        }

        private void StopHoverRoutine()
        {
            if (_hoverRoutine == null) return;

            StopCoroutine(_hoverRoutine);
            _hoverRoutine = null;
        }

        private void HideTooltip()
        {
            StopHoverRoutine();
            _tooltip?.Hide();
        }

        /// <summary>영역 글자는 좁아서 이름 끝의 "컴플렉스"를 뗀다(표시용 가공일 뿐 정의는 그대로다).</summary>
        private static string TrimSuffix(string displayName)
        {
            const string suffix = "컴플렉스";
            return displayName.EndsWith(suffix) ? displayName.Substring(0, displayName.Length - suffix.Length).TrimEnd() : displayName;
        }
    }
}
