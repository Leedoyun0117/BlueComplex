using System.Collections;
using BlueComplex.Core.Complexes;
using BlueComplex.UI.Layout;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>컴플렉스 목록의 행 하나. 이름/남은 턴 막대 + 상세 팝업(호버 0.25초 또는 클릭 — 설명은 여기에만 뜬다) + 발동 시 순차 발광.
    /// 엑스레이 판넬의 목록과 상시 표시(UI 가이드 8번)가 같은 행을 쓴다.</summary>
    public sealed class ComplexRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ITargetHighlight
    {
        private const float HoverDelay = 0.25f;

        /// <summary>남은 턴이 줄 때 막대가 줄어드는 시간. 심박수 마커 이동(0.25초)과 비슷하게 맞췄다.</summary>
        private const float BarShrinkDuration = 0.3f;

        /// <summary>한 행이 빛나는 데 걸리는 시간(올라갔다 내려오는 전체).</summary>
        public const float GlowDuration = 0.35f;

        [SerializeField] private TMP_Text _nameText;
        // 남은 턴 막대. 노란 포스트잇 목록(목업: "이름 N턴")에는 막대가 없어 비어 있을 수 있다.
        [SerializeField] private Image _durationFill;
        [SerializeField] private TMP_Text _durationText;
        // 노란 포스트잇은 제목이 이미 "컴플렉스"라 목록에서는 이름 끝의 "컴플렉스"를 뗀다(목업: "소꿉친구  1턴"). 표시용 가공일 뿐 정의는 그대로다.
        [SerializeField] private bool _trimComplexSuffix;
        // 발광 색. 어두운 판넬 행은 노랑이 잘 보이지만 노란 포스트잇 위에서는 안 보여서, 프리팹마다 정할 수 있게 뺐다.
        [SerializeField] private Color _glowColor = new Color32(255, 225, 120, 255);

        private TooltipPopup _tooltip;
        private Coroutine _hoverRoutine;
        private ComplexInstance _complex;
        private Image _background;
        private Color _baseColor;
        private Tween _glowTween;
        private Tween _barTween;
        private TMP_Text _newMark;
        private Tween _newMarkTween;
        private bool _newMarkShown;

        public ComplexInstance Complex => _complex;

        public void Init(TooltipPopup tooltip) => _tooltip = tooltip;

        private void Awake()
        {
            // BuildComplexXrayPanel(UiLayoutSetupTool.cs)이 이 컴포넌트와 같은 GameObject에
            // 배경 Image를 붙인다 — 직렬화 필드로 안 받아도 항상 찾을 수 있다.
            _background = GetComponent<Image>();
            if (_background != null) _baseColor = _background.color;
        }

        /// <param name="isNew">이번에 새로 발현된 컴플렉스 — 이름 옆에 빨간 펜 "NEW"를 쓴다(기획서에 없는 구분 표시). 실제로 보이는 건 <see cref="RevealNewMark"/>가 부른 뒤다.</param>
        public void Render(ComplexInstance complex, bool isNew = false)
        {
            var previous = _complex;
            _complex = complex;
            gameObject.SetActive(true);

            _nameText.text = _trimComplexSuffix ? TrimSuffix(complex.Definition.DisplayName) : complex.Definition.DisplayName;

            _barTween?.Kill();
            if (_durationFill != null)
            {
                var fraction = complex.Definition.DefaultDuration > 0
                    ? Mathf.Clamp01(complex.RemainingTurns / (float)complex.Definition.DefaultDuration)
                    : 0f;
                // Image.Type.Filled는 스프라이트가 없으면 안 채워지는 경우가 있어, 배경 바 위에
                // 폭을 anchorMax.x로 직접 조절하는 방식(심박수 바와 동일)으로 대신한다.
                var fillRect = _durationFill.rectTransform;
                if (previous == complex && !Mathf.Approximately(fillRect.anchorMax.x, fraction))
                {
                    // 같은 컴플렉스의 남은 턴이 줄었다 — 막대가 함께 줄어든다(UI 가이드 명시).
                    _barTween = DOVirtual.Float(fillRect.anchorMax.x, fraction, BarShrinkDuration,
                        value => fillRect.anchorMax = new Vector2(value, fillRect.anchorMax.y)).SetEase(Ease.OutQuad);
                }
                else
                {
                    fillRect.anchorMax = new Vector2(fraction, fillRect.anchorMax.y);
                }
            }

            _durationText.text = $"{complex.RemainingTurns}턴";
            SetNewMark(isNew);
        }

        /// <summary>새 컴플렉스 표시를 준비한다(숨긴 채로). 이름 글자 바로 뒤에 놓인다.</summary>
        private void SetNewMark(bool isNew)
        {
            if (!isNew)
            {
                _newMarkTween?.Kill();
                _newMarkShown = false;
                if (_newMark != null) _newMark.gameObject.SetActive(false);
                return;
            }

            if (_newMark == null) _newMark = BuildNewMark();

            _nameText.ForceMeshUpdate();
            var nameRect = _nameText.rectTransform;
            var textWidth = Mathf.Min(_nameText.preferredWidth, nameRect.rect.width);
            _newMark.rectTransform.anchoredPosition = new Vector2(nameRect.offsetMin.x + textWidth + 4f, 9f);

            // 이미 드러난 표시는 그대로 둔다(글자만 다시 그려질 때마다 다시 찍히면 안 된다).
            if (_newMarkShown) return;

            _newMarkTween?.Kill();
            _newMark.alpha = 0f;
            _newMark.gameObject.SetActive(true);
        }

        /// <summary>빨간 펜으로 휘갈겨 쓴 듯 "NEW"가 눌려 찍힌다. 포스트잇이 다 붙은 뒤에 부른다.</summary>
        public void RevealNewMark()
        {
            if (_newMark == null || !_newMark.gameObject.activeSelf || _newMarkShown) return;

            _newMarkShown = true;
            _newMarkTween?.Kill();
            var mark = _newMark.rectTransform;
            mark.localScale = Vector3.one * 1.6f;
            _newMark.alpha = 0f;
            _newMarkTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(DOTween.To(() => _newMark.alpha, value => _newMark.alpha = value, 1f, 0.12f))
                .Join(mark.DOScale(1f, 0.22f).SetEase(Ease.OutBack, 2f));
        }

        private TMP_Text BuildNewMark()
        {
            var go = new GameObject("New Mark", typeof(RectTransform)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(52f, 24f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 9f);

            var label = go.AddComponent<TextMeshProUGUI>();
            var hand = PostitStyle.HandFont;
            if (hand != null) label.font = hand;
            label.text = "NEW";
            label.fontSize = 22f;
            label.color = PostitStyle.RedPen;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static string TrimSuffix(string displayName)
        {
            const string suffix = "컴플렉스";
            return displayName.EndsWith(suffix) ? displayName.Substring(0, displayName.Length - suffix.Length).TrimEnd() : displayName;
        }

        /// <summary>아이템 대상 선택 모드에서 고를 수 있는 대상이면 테두리가 깜박인다.</summary>
        public void SetTargetable(bool on) => TargetPulse.Set(this, on, _trimComplexSuffix ? TargetPulse.InkBlue : TargetPulse.Gold);

        public void SetEmpty()
        {
            _complex = null;
            SetNewMark(false);
            TargetPulse.Set(this, false);
            HideTooltip();
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_complex == null) return;
            _hoverRoutine = StartCoroutine(HoverThenShow());
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        /// <summary>호버를 기다리지 않고 바로 상세를 띄운다. 마우스를 올려 봐야 알 수 있다는 걸 클릭이 보완한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_complex == null) return;

            // 아이템 대상 선택 중이면 클릭은 대상 선택이다(상세 팝업을 띄우지 않는다).
            if (ItemTargetSelector.TryPick(_complex)) return;

            StopHoverRoutine();
            ShowTooltip();
        }

        /// <summary>이 컴플렉스가 이번 턴에 발동했음을 짧게 강조한다. 순서 제어는 ComplexListView가 쥔다 —
        /// 이 메서드는 자기 자신을 한 번 반짝이는 것 외에는 아무것도 모른다.</summary>
        public void PlayGlow()
        {
            if (_background == null) return;

            _glowTween?.Kill();
            _glowTween = DOTween.Sequence()
                .Append(_background.DOColor(_glowColor, GlowDuration / 2f))
                .Append(_background.DOColor(_baseColor, GlowDuration / 2f));
        }

        private void OnDisable()
        {
            HideTooltip();
            _glowTween?.Kill();
            _barTween?.Kill();
            _newMarkTween?.Kill();
            if (_background != null) _background.color = _baseColor;
        }

        private IEnumerator HoverThenShow()
        {
            yield return new WaitForSeconds(HoverDelay);
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            if (_complex == null || _tooltip == null) return;

            var definition = _complex.Definition;
            _tooltip.Show(definition.DisplayName,
                $"{definition.Description}\n\n남은 지속: {_complex.RemainingTurns}턴",
                transform.position);
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
