using System;
using BlueComplex.UI.Motion;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlueComplex.UI.Layout
{
    /// <summary>유키 대사 한 줄(흰 종이 패널 + 이름표). 한 글자씩 출력하고(글자마다 타자 소리 훅), 출력 중 클릭하면 즉시 전체 표시로 건너뛴다.
    /// 다 나온 뒤에는 오른쪽 아래에 "다음" 표시(▼)가 천천히 깜빡인다 — 스프라이트로 그려서 글꼴에 ▼ 글리프가 없어도 나온다.
    /// 컴플렉스가 발동할 때의 짧은 이벤트 대사도 같은 창에 나온다(<see cref="PlayTyped(string, bool)"/>의 isEvent — 글자 색만 다르다).
    /// 글자 간격·깜빡임 시간은 UiMotionSettings(인스펙터)에서 온다.</summary>
    public sealed class DialogueText : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>이벤트 대사(컴플렉스 발동)의 글자색 — 종이 위 잉크색보다 붉은 갈색.</summary>
        private static readonly Color EventInk = new Color32(150, 58, 38, 255);

        [SerializeField] private TMP_Text _label;

        private Tween _typeTween;
        private string _fullLine = string.Empty;
        private CanvasGroup _nextIndicator;
        private Tween _blink;
        private Color _lineInk = MockupStyle.Ink;

        public RectTransform Root => (RectTransform)transform;

        /// <summary>타이핑이 끝났을 때(자연 종료든 클릭으로 건너뛰었든) 한 번 쏜다 — 3단계
        /// CinematicTurnResultPresenter가 이걸로 다음 연출 단계로 넘어갈 타이밍을 잡는다.</summary>
        public event Action TypingComplete;

        private void Awake()
        {
            if (_label != null) _lineInk = _label.color;
            BuildNextIndicator();
        }

        /// <summary>즉시 표시(연출 없음). 1단계부터 있던 메서드 — 초기화·되돌리기용으로 유지.</summary>
        public void SetLine(string line)
        {
            StopTyping();
            _fullLine = line ?? string.Empty;
            if (_label != null)
            {
                _label.color = _lineInk;
                _label.text = _fullLine;
            }

            ShowNextIndicator(true);
        }

        /// <summary>한 글자씩 재생한다. 재생 중이면 새 줄로 갈아친다.</summary>
        public void PlayTyped(string line) => PlayTyped(line, isEvent: false);

        /// <param name="isEvent">컴플렉스 발동 같은 짧은 이벤트 대사 — 같은 창에 나오되 글자색이 다르다.</param>
        public void PlayTyped(string line, bool isEvent)
        {
            StopTyping();
            _fullLine = line ?? string.Empty;
            if (_label != null)
            {
                _label.color = isEvent ? EventInk : _lineInk;
                _label.text = string.Empty;
            }
            ShowNextIndicator(false);
            StartTyping();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_typeTween == null) return;
            FinishTyping();
        }

        private void OnDisable()
        {
            StopTyping();
            _blink?.Kill();
        }

        private void StopTyping()
        {
            _typeTween?.Kill();
            _typeTween = null;
        }

        /// <summary>한 글자씩 드러낸다(DOTween 정수 스텝) — 첫 글자는 바로, 그다음부터 글자당 <c>dialogueSecondsPerChar</c>초. 소리는 글자마다(공백 제외) 훅으로 알린다.</summary>
        private void StartTyping()
        {
            var motion = UiMotion.Settings;
            var count = _fullLine.Length;
            if (count == 0)
            {
                FinishTyping();
                return;
            }

            var shown = 0;
            var sinceSound = 0;

            void Reveal(int visible)
            {
                if (visible == shown) return;
                shown = visible;
                if (_label != null) _label.text = _fullLine.Substring(0, shown);

                // 소리는 motion.dialogueSoundEvery 글자마다 한 번.
                if (!char.IsWhiteSpace(_fullLine[shown - 1]) && sinceSound++ % motion.dialogueSoundEvery == 0)
                    UiSoundHooks.Play(UiSoundCue.Type);
            }

            Reveal(1);
            _typeTween = DOTween.To(() => 0f, v => Reveal(Mathf.Min(count, Mathf.FloorToInt(v) + 1)),
                    count, count * motion.dialogueSecondsPerChar)
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this)
                .OnComplete(FinishTyping);
        }

        /// <summary>자연 종료든 클릭으로 건너뛰었든 전체 줄을 보여 주고 "다음" 표시를 켠 뒤 완료를 알린다.</summary>
        private void FinishTyping()
        {
            StopTyping();
            if (_label != null) _label.text = _fullLine;
            ShowNextIndicator(true);
            TypingComplete?.Invoke();
        }

        private void BuildNextIndicator()
        {
            var go = new GameObject("Next Indicator", typeof(RectTransform), typeof(CanvasGroup), typeof(Image))
            {
                layer = gameObject.layer
            };
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-30f, 22f);
            rect.sizeDelta = new Vector2(30f, 24f);

            var image = go.GetComponent<Image>();
            image.sprite = RuntimeUi.TriangleDown;
            image.color = MockupStyle.Ink;
            image.raycastTarget = false;

            _nextIndicator = go.GetComponent<CanvasGroup>();
            _nextIndicator.alpha = 0f;
        }

        private void ShowNextIndicator(bool visible)
        {
            if (_nextIndicator == null) return;

            _blink?.Kill();
            if (!visible)
            {
                _nextIndicator.alpha = 0f;
                return;
            }

            _nextIndicator.alpha = 1f;
            _blink = _nextIndicator.DOFade(0.2f, UiMotion.Settings.dialogueNextBlink).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
}
