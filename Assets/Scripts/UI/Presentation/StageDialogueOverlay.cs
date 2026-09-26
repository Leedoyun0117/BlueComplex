using System;
using System.Collections;
using BlueComplex.Core.Stage;
using BlueComplex.UI.Layout;
using BlueComplex.UI.Motion;
using BlueComplex.UI.Rendering;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 스테이지 시작/쿼터 분기/스테이지 클리어 대화의 암전 막 + 여러 줄 대사. <see cref="KeyTurnOverlay"/>(키 턴 나츠 독백)와 같은 뼈대지만
    /// 한 줄이 아니라 <see cref="DialogueVariant"/>(여러 줄)를 차례로 재생하고, 줄마다 화자(플레이어/유키)에 따라 이름표 색이 바뀐다.
    /// 각 줄은 클릭으로 다음 줄로 넘어간다(타이핑 중 클릭하면 그 줄만 즉시 완성). 막이 떠 있는 동안은 뒤 UI의 클릭을 막는다.
    /// </summary>
    public sealed class StageDialogueOverlay : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color YukiInk = new Color32(146, 168, 204, 255);
        private static readonly Color PlayerInk = new Color32(214, 178, 128, 255);
        private static readonly Color ChiefInk = new Color32(150, 190, 160, 255);
        private static readonly Color LineInk = new Color32(238, 240, 246, 255);

        private CanvasGroup _group;
        private CanvasGroup _textGroup;
        private Image _dim;
        private TMP_Text _speaker;
        private TMP_Text _line;
        private CanvasGroup _nextIndicator;
        private Tween _blink;
        private Tween _typing;
        private bool _skip;

        // 선택지 줄: 타이핑 없이 누를 수 있는 "/ …" 한 줄이 나오고, 그걸 눌러야만 넘어간다(배경 클릭은 무시).
        private GameObject _choiceButton;
        private TMP_Text _choiceLabel;
        private bool _lineIsChoice;
        private bool _choicePicked;

        public static StageDialogueOverlay Create(Transform canvasRoot, TMP_FontAsset font)
        {
            var rect = new GameObject("Stage Dialogue Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)) { layer = canvasRoot.gameObject.layer }
                .GetComponent<RectTransform>();
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var overlay = rect.gameObject.AddComponent<StageDialogueOverlay>();
            overlay.Build(font);
            return overlay;
        }

        /// <summary>막을 어둡게 하고 변형의 줄을 순서대로 재생한 뒤 다시 밝아진다. 줄이 없으면 아무것도 안 한다.</summary>
        public IEnumerator PlaySequence(DialogueVariant variant)
        {
            if (variant.lines == null || variant.lines.Length == 0) yield break;

            var settings = UiMotion.Settings;
            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // 그 사이에 생긴 다른 패널 위로.
            _dim.color = new Color(0.016f, 0.024f, 0.05f, settings.keyTurnDim);
            _group.blocksRaycasts = true;
            _textGroup.alpha = 0f;

            _group.DOKill();
            yield return _group.DOFade(1f, settings.keyTurnFade).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(_group).WaitForCompletion(true);

            _textGroup.DOKill();
            yield return _textGroup.DOFade(1f, 0.2f).SetUpdate(true).SetTarget(_textGroup).WaitForCompletion(true);

            foreach (var line in variant.lines)
                yield return PlayLine(line);

            yield return _textGroup.DOFade(0f, 0.2f).SetUpdate(true).SetTarget(_textGroup).WaitForCompletion(true);

            _group.DOKill();
            _group.blocksRaycasts = false;
            yield return _group.DOFade(0f, settings.keyTurnFade).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(_group).WaitForCompletion(true);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 자기 암전 막 없이 줄만 재생한다 — 이미 다른 막(스테이지 클리어의 자물쇠 화면)이 화면을 어둡게 덮고 있을 때 쓴다.
        /// 한 줄이 끝날 때마다 <paramref name="onLineDone"/>에 진행도(끝난 줄 수 / 전체 줄 수, 0~1)를 알려 주어, 부른 쪽이 화면을 그만큼씩 밝힐 수 있다.
        /// 줄을 넘기는 클릭은 이 오브젝트가 받는다(투명한 막이 클릭을 받는다).
        /// </summary>
        public IEnumerator PlayLinesOver(DialogueVariant variant, Action<float> onLineDone)
        {
            if (variant.lines == null || variant.lines.Length == 0) yield break;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _dim.color = Color.clear;
            _group.DOKill();
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _textGroup.alpha = 0f;

            _textGroup.DOKill();
            yield return _textGroup.DOFade(1f, 0.2f).SetUpdate(true).SetTarget(_textGroup).WaitForCompletion(true);

            for (var i = 0; i < variant.lines.Length; i++)
            {
                yield return PlayLine(variant.lines[i]);
                onLineDone?.Invoke((i + 1f) / variant.lines.Length);
            }

            yield return _textGroup.DOFade(0f, 0.2f).SetUpdate(true).SetTarget(_textGroup).WaitForCompletion(true);

            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>한 줄: 화자 이름표 색 → 한 글자씩(글자마다 타자 소리, 클릭하면 즉시 완성) → "다음" 표시가 뜨고 클릭을 기다린다.</summary>
        private IEnumerator PlayLine(DialogueLine line)
        {
            var settings = UiMotion.Settings;
            _speaker.text = SpeakerName(line.speaker);
            _speaker.color = SpeakerInk(line.speaker);
            _line.text = string.Empty;
            ShowNextIndicator(false);
            _skip = false;

            if (line.isChoice)
            {
                yield return PlayChoice(line);
                yield break;
            }

            var text = line.text ?? string.Empty;
            var count = text.Length;
            var shown = 0;
            var typing = count > 0;
            if (typing)
            {
                _typing = DOTween.To(() => 0f, v =>
                    {
                        var visible = Mathf.Min(count, Mathf.FloorToInt(v) + 1);
                        if (visible == shown) return;

                        shown = visible;
                        _line.text = text.Substring(0, shown);
                        if (!char.IsWhiteSpace(text[shown - 1])) UiSoundHooks.Play(UiSoundCue.Type);
                    }, count, count * settings.keyTurnSecondsPerChar)
                    .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this)
                    .OnComplete(() => typing = false);
            }

            while (typing && !_skip) yield return null;

            _typing?.Kill();
            _typing = null;
            _line.text = text;

            ShowNextIndicator(true);
            _skip = false;
            yield return new WaitUntil(() => _skip);
            ShowNextIndicator(false);
        }

        /// <summary>선택지 한 줄: "/ …"가 떠 있고 플레이어가 그걸 누르면 다음 줄로 간다. 배경을 눌러서는 넘어가지 않는다(<see cref="DialogueAdvanceRule"/>).</summary>
        private IEnumerator PlayChoice(DialogueLine line)
        {
            _lineIsChoice = true;
            _choicePicked = false;
            _choiceLabel.text = "/ " + (line.text ?? string.Empty);
            _choiceButton.SetActive(true);

            while (!_choicePicked) yield return null;

            _choiceButton.SetActive(false);
            _lineIsChoice = false;
            _skip = false;
        }

        /// <summary>선택지 버튼의 onClick.</summary>
        private void OnChoiceClicked()
        {
            if (!DialogueAdvanceRule.Accepts(_lineIsChoice, clickedTheChoice: true)) return;

            UiSoundHooks.Play(UiSoundCue.ButtonClick);
            _choicePicked = true;
        }

        private static string SpeakerName(DialogueSpeaker speaker) => speaker switch
        {
            DialogueSpeaker.Yuki => "유키",
            DialogueSpeaker.Chief => TutorialGuideContent.GuideName,
            _ => "나"
        };

        private static Color SpeakerInk(DialogueSpeaker speaker) => speaker switch
        {
            DialogueSpeaker.Yuki => YukiInk,
            DialogueSpeaker.Chief => ChiefInk,
            _ => PlayerInk
        };

        public void OnPointerClick(PointerEventData eventData)
        {
            // 선택지 줄에서는 배경 클릭으로 넘어가지 않는다.
            if (!DialogueAdvanceRule.Accepts(_lineIsChoice, clickedTheChoice: false)) return;
            _skip = true;
        }


        /// <summary>진행 중인 연출을 멈추고 막을 치운다(재시작).</summary>
        public void ResetNow()
        {
            _typing?.Kill();
            _typing = null;
            _blink?.Kill();
            _group.DOKill();
            _textGroup.DOKill();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            if (_choiceButton != null) _choiceButton.SetActive(false);
            _lineIsChoice = false;
            gameObject.SetActive(false);
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
            _group.ignoreParentGroups = true; // 분기 대사 장면에서 게임 UI를 숨기는 캔버스 그룹(BranchSceneDirector)의 영향을 받지 않는다.

            // 최상위 캔버스는 그 그룹의 알파가 0이면 통째로 그려지지 않아 ignoreParentGroups만으로는 대사 글자까지 사라진다 —
            // 자기 캔버스(중첩)를 가지면 따로 그려진다. 중첩 캔버스의 그래픽은 자기 GraphicRaycaster가 있어야 클릭을 받는다(왜곡 보정도 부모와 같게).
            gameObject.AddComponent<Canvas>();
            var raycaster = gameObject.AddComponent<DistortionCorrectedGraphicRaycaster>();
            var parentRaycaster = transform.parent != null ? transform.parent.GetComponentInParent<DistortionCorrectedGraphicRaycaster>() : null;
            if (parentRaycaster != null) raycaster.SetCrtMaterial(parentRaycaster.CrtMaterial);

            _dim = GetComponent<Image>();
            _dim.raycastTarget = true;

            var text = new GameObject("Dialogue", typeof(RectTransform), typeof(CanvasGroup)) { layer = gameObject.layer };
            text.transform.SetParent(transform, false);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            _textGroup = text.GetComponent<CanvasGroup>();
            _textGroup.alpha = 0f;
            _textGroup.blocksRaycasts = false;

            _speaker = CreateText(textRect, "Speaker", font, 26f, YukiInk, new Vector2(0.2f, 0.53f), new Vector2(0.8f, 0.6f));
            _line = CreateText(textRect, "Line", font, 38f, LineInk, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.53f));

            var indicatorGo = new GameObject("Next Indicator", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)) { layer = gameObject.layer };
            indicatorGo.transform.SetParent(textRect, false);
            var indicatorRect = (RectTransform)indicatorGo.transform;
            indicatorRect.anchorMin = indicatorRect.anchorMax = indicatorRect.pivot = new Vector2(0.5f, 0.4f);
            indicatorRect.sizeDelta = new Vector2(28f, 22f);
            indicatorRect.anchoredPosition = Vector2.zero;

            var indicatorImage = indicatorGo.GetComponent<Image>();
            indicatorImage.sprite = RuntimeUi.TriangleDown;
            indicatorImage.color = LineInk;
            indicatorImage.raycastTarget = false;

            _nextIndicator = indicatorGo.GetComponent<CanvasGroup>();
            _nextIndicator.alpha = 0f;

            BuildChoiceButton(textRect, font);

            gameObject.SetActive(false);
        }

        /// <summary>선택지 줄에 쓰는 버튼(평소엔 꺼져 있다). 대사 줄 자리에 놓이고, 어두운 판 + 플레이어 색 테두리 + 그 색의 글자다.</summary>
        private void BuildChoiceButton(Transform parent, TMP_FontAsset font)
        {
            _choiceButton = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(Button)) { layer = gameObject.layer };
            _choiceButton.transform.SetParent(parent, false);

            var rect = (RectTransform)_choiceButton.transform;
            rect.anchorMin = new Vector2(0.14f, 0.405f);
            rect.anchorMax = new Vector2(0.86f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = _choiceButton.GetComponent<Image>();
            image.sprite = RuntimeUi.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.09f, 0.11f, 0.17f, 0.92f);

            var outline = _choiceButton.AddComponent<Outline>();
            outline.effectColor = PlayerInk;
            outline.effectDistance = new Vector2(2f, -2f);

            var button = _choiceButton.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.35f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
            button.colors = colors;
            button.onClick.AddListener(OnChoiceClicked);

            _choiceLabel = CreateText(rect, "Label", font, 34f, PlayerInk, Vector2.zero, Vector2.one);
            _choiceLabel.margin = new Vector4(24f, 6f, 24f, 6f);

            _choiceButton.SetActive(false);
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
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }
    }
}
