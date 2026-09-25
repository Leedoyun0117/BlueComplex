using System.Collections;
using BlueComplex.Core.Traits;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 엑스레이 판넬 위에 뜨는 특성 뱃지 하나(TraitStatusView가 특성마다 하나씩 만든다). 마우스를 0.25초 올리면(또는 클릭하면 바로)
    /// 공유 팝업(<see cref="TooltipPopup"/>)에 특성 효과 설명이 뜬다 — 컴플렉스(BrainRegionView)와 같은 규칙이다.
    /// 드래그는 처리하지 않으므로 뱃지를 잡고 끌어도 이벤트가 위의 판넬 루트까지 올라가 판넬이 따라온다.
    /// </summary>
    public sealed class TraitBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float HoverDelay = 0.25f;

        [SerializeField] private TMP_Text _label;

        private TooltipPopup _tooltip;
        private TraitInstance _trait;
        private Coroutine _hoverRoutine;

        public TraitInstance Trait => _trait;

        public void Bind(TraitInstance trait, TooltipPopup tooltip)
        {
            _trait = trait;
            _tooltip = tooltip;
            // 뱃지는 좁은 판넬 폭에 여러 개가 나란히 들어가야 해서 특수 특성의 "(안정까지)"는 팝업으로 뺀다.
            if (_label != null)
                _label.text = trait.Definition.Kind == TraitKind.Special
                    ? trait.Definition.DisplayName
                    : $"{trait.Definition.DisplayName} {trait.RemainingTurns}턴";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StopHoverRoutine();
            _hoverRoutine = StartCoroutine(HoverThenShow());
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        /// <summary>호버를 기다리지 않고 바로 상세를 띄운다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            StopHoverRoutine();
            ShowTooltip();
        }

        private void OnDisable() => HideTooltip();

        private IEnumerator HoverThenShow()
        {
            yield return new WaitForSecondsRealtime(HoverDelay);
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            if (_trait == null || _tooltip == null) return;

            var definition = _trait.Definition;
            var duration = definition.Kind == TraitKind.Special
                ? "안정 구간에 들어설 때까지"
                : $"남은 지속: {_trait.RemainingTurns}턴";
            _tooltip.Show(definition.DisplayName, $"{definition.Description}\n\n{duration}", transform.position);
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
    }
}
