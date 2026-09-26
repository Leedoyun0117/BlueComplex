using System.Collections;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 엔딩의 글 화면: 검은 바탕 위에 한 문단이 한 글자씩 타이핑되고, 클릭하면 그 문단이 즉시 완성되고, 다시 클릭하면 다음으로 넘어간다.
    /// 컷신 9 "붙잡히는 유키"와 에필로그 문단이 같이 쓴다 — 화자 이름표 없는 통짜 내레이션이다(화자 스타일이 필요해지면 문단마다 색만 넘기면 된다).
    ///
    /// 문단이 길어(에필로그는 최대 300자) 글자 크기가 문단 길이에 맞춰 줄어드는데, 타이핑하며 글이 자라면 크기가 흔들리므로
    /// 전체 글을 미리 놓고 <see cref="TMP_Text.maxVisibleCharacters"/>만 늘려 드러낸다.
    /// </summary>
    internal sealed class EndingTextView : MonoBehaviour, IPointerClickHandler
    {
        private const float FadeSeconds = 0.25f;
        private static readonly Color Ink = new Color32(238, 240, 246, 255);

        private CanvasGroup _group;
        private TMP_Text _text;
        private CanvasGroup _nextIndicator;
        private Tween _blink;
        private Tween _typing;
        private bool _clicked;

        public static EndingTextView Create(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("Ending Text", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);

            var view = go.AddComponent<EndingTextView>();
            view.Build(font);
            return view;
        }

        /// <summary>문단 하나: 나타남 → 타이핑 → (클릭하면 즉시 완성) → "다음" 표시와 함께 클릭 대기 → 사라짐. 사이사이 글자만 페이드해 검은 바탕은 그대로 남는다.</summary>
        public IEnumerator PlayParagraph(string paragraph)
        {
            var text = paragraph ?? string.Empty;
            _text.text = text;
            _text.ForceMeshUpdate();
            _text.maxVisibleCharacters = 0;
            var total = _text.textInfo.characterCount;

            _group.blocksRaycasts = true;
            _group.alpha = 0f;
            yield return _group.DOFade(1f, FadeSeconds).SetUpdate(true).SetTarget(_group).WaitForCompletion(true);

            _clicked = false;
            var typing = total > 0;
            var shown = 0;
            if (typing)
            {
                _typing = DOTween.To(() => 0f, v =>
                    {
                        var visible = Mathf.Min(total, Mathf.FloorToInt(v) + 1);
                        if (visible == shown) return;

                        shown = visible;
                        _text.maxVisibleCharacters = shown;
                        if (!char.IsWhiteSpace(text[Mathf.Min(shown, text.Length) - 1])) UiSoundHooks.Play(UiSoundCue.Type);
                    }, total, total * UiMotion.Settings.keyTurnSecondsPerChar)
                    .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this)
                    .OnComplete(() => typing = false);
            }

            while (typing && !_clicked) yield return null;

            _typing?.Kill();
            _typing = null;
            _text.maxVisibleCharacters = int.MaxValue;

            ShowNextIndicator(true);
            _clicked = false;
            yield return new WaitUntil(() => _clicked);
            ShowNextIndicator(false);

            _group.blocksRaycasts = false;
            yield return _group.DOFade(0f, FadeSeconds).SetUpdate(true).SetTarget(_group).WaitForCompletion(true);
        }

        public void OnPointerClick(PointerEventData eventData) => _clicked = true;

        /// <summary>진행 중인 타이핑·깜박임을 멈추고 글자를 지운다.</summary>
        public void ResetNow()
        {
            _typing?.Kill();
            _typing = null;
            _blink?.Kill();
            _group.DOKill();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _nextIndicator.alpha = 0f;
            _clicked = false;
        }

        private void ShowNextIndicator(bool visible)
        {
            _blink?.Kill();
            if (!visible)
            {
                _nextIndicator.alpha = 0f;
                return;
            }

            _nextIndicator.alpha = 1f;
            _blink = _nextIndicator.DOFade(0.2f, UiMotion.Settings.dialogueNextBlink).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        private void Build(TMP_FontAsset font)
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var click = GetComponent<Image>();
            click.color = Color.clear;
            click.raycastTarget = true; // 화면 어디를 눌러도 넘어간다.

            var go = new GameObject("Paragraph", typeof(RectTransform)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.14f, 0.18f);
            rect.anchorMax = new Vector2(0.86f, 0.82f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) _text.font = font;
            _text.color = Ink;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.enableAutoSizing = true;
            _text.fontSizeMax = 40f;
            _text.fontSizeMin = 24f;
            _text.lineSpacing = 12f;
            _text.raycastTarget = false;

            var indicatorGo = new GameObject("Next Indicator", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)) { layer = gameObject.layer };
            indicatorGo.transform.SetParent(transform, false);
            var indicatorRect = (RectTransform)indicatorGo.transform;
            indicatorRect.anchorMin = indicatorRect.anchorMax = indicatorRect.pivot = new Vector2(0.5f, 0.12f);
            indicatorRect.sizeDelta = new Vector2(28f, 22f);
            indicatorRect.anchoredPosition = Vector2.zero;

            var indicatorImage = indicatorGo.GetComponent<Image>();
            indicatorImage.sprite = RuntimeUi.TriangleDown;
            indicatorImage.color = Ink;
            indicatorImage.raycastTarget = false;

            _nextIndicator = indicatorGo.GetComponent<CanvasGroup>();
            _nextIndicator.alpha = 0f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
