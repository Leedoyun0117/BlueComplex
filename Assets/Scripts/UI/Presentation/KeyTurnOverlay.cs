using System.Collections;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 키 턴 연출의 암전 막 + 나츠 독백. 화면 전체를 덮는 어두운 막이 페이드로 어두워지고, 그 위에 독백이 한 글자씩 나온 뒤 다시 밝아진다.
    /// 막이 떠 있는 동안은 뒤 UI의 클릭을 막고, 독백은 클릭하면 건너뛴다.
    /// 표시만 한다 — 언제 어둡게/밝게 할지는 <see cref="PostitDirector"/>(= Presenter)가 정한다.
    /// </summary>
    public sealed class KeyTurnOverlay : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color SpeakerInk = new Color32(146, 168, 204, 255);
        private static readonly Color LineInk = new Color32(238, 240, 246, 255);

        private CanvasGroup _group;
        private CanvasGroup _textGroup;
        private Image _dim;
        private TMP_Text _speaker;
        private TMP_Text _line;
        private Tween _typing;
        private bool _skip;

        public static KeyTurnOverlay Create(Transform canvasRoot, TMP_FontAsset font)
        {
            var rect = new GameObject("Key Turn Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)) { layer = canvasRoot.gameObject.layer }
                .GetComponent<RectTransform>();
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var overlay = rect.gameObject.AddComponent<KeyTurnOverlay>();
            overlay.Build(font);
            return overlay;
        }

        /// <summary>어두워진다. 이미 어두우면 그대로 둔다.</summary>
        public Tween FadeIn()
        {
            var settings = UiMotion.Settings;
            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // 그 사이에 생긴 다른 패널 위로.
            _dim.color = new Color(0.016f, 0.024f, 0.05f, settings.keyTurnDim);
            _group.blocksRaycasts = true;
            _textGroup.alpha = 0f;

            _group.DOKill();
            return _group.DOFade(1f, settings.keyTurnFade).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(_group);
        }

        /// <summary>다시 밝아진다. 다 밝아지면 막을 치운다.</summary>
        public Tween FadeOut()
        {
            _group.DOKill();
            _group.blocksRaycasts = false;
            return _group.DOFade(0f, UiMotion.Settings.keyTurnFade).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(_group)
                .OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>막이 어두워진 상태에서 독백을 보여 준다: 화자 이름 → 한 글자씩(글자마다 타자 소리) → 잠깐 머묾 → 사라짐. 클릭하면 건너뛴다.</summary>
        public IEnumerator PlayMonologue(string speaker, string line)
        {
            var settings = UiMotion.Settings;
            _speaker.text = speaker;
            _line.text = string.Empty;
            _skip = false;

            _textGroup.DOKill();
            yield return _textGroup.DOFade(1f, 0.2f).SetUpdate(true).SetTarget(_textGroup).WaitForCompletion(true);

            var count = line.Length;
            var shown = 0;
            var typing = count > 0;
            if (typing)
            {
                _typing = DOTween.To(() => 0f, v =>
                    {
                        var visible = Mathf.Min(count, Mathf.FloorToInt(v) + 1);
                        if (visible == shown) return;

                        shown = visible;
                        _line.text = line.Substring(0, shown);
                        if (!char.IsWhiteSpace(line[shown - 1])) UiSoundHooks.Play(UiSoundCue.Type);
                    }, count, count * settings.keyTurnSecondsPerChar)
                    .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this)
                    .OnComplete(() => typing = false);
            }

            while (typing && !_skip) yield return null;

            _typing?.Kill();
            _typing = null;
            _line.text = line;

            _skip = false;
            for (var elapsed = 0f; elapsed < settings.keyTurnHold && !_skip; elapsed += Time.unscaledDeltaTime)
                yield return null;

            yield return _textGroup.DOFade(0f, 0.25f).SetUpdate(true).SetTarget(_textGroup).WaitForCompletion(true);
        }

        /// <summary>진행 중인 연출을 멈추고 막을 치운다(재시작).</summary>
        public void ResetNow()
        {
            _typing?.Kill();
            _typing = null;
            _group.DOKill();
            _textGroup.DOKill();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) => _skip = true;

        private void Build(TMP_FontAsset font)
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _dim = GetComponent<Image>();
            _dim.raycastTarget = true;

            var text = new GameObject("Monologue", typeof(RectTransform), typeof(CanvasGroup)) { layer = gameObject.layer };
            text.transform.SetParent(transform, false);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            _textGroup = text.GetComponent<CanvasGroup>();
            _textGroup.alpha = 0f;
            _textGroup.blocksRaycasts = false;

            _speaker = CreateText(textRect, "Speaker", font, 26f, SpeakerInk, new Vector2(0.2f, 0.53f), new Vector2(0.8f, 0.6f));
            _line = CreateText(textRect, "Line", font, 42f, LineInk, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.53f));

            gameObject.SetActive(false);
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size, Color color, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer }.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
