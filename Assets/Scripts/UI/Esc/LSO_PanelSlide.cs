using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Esc
{
    /// <summary>
    /// 창이 화면 위에서 내려오고 다시 올라가는 연출. "언제 열고 닫을지"는 모르고 "어떻게 보이게 할지"만 안다.
    ///
    /// MonoBehaviour가 아니라 [Serializable] 필드로 두는 이유: 인스펙터에서 값은 조절하고 싶지만,
    /// 컴포넌트로 빼면 오브젝트에 따로 붙이고 참조를 물려야 하고 그 배선이 빠지면 조용히 죽는다.
    /// 이렇게 두면 쓰는 쪽 인스펙터에 접혀서 같이 나오고 배선할 것이 없다.
    /// </summary>
    [Serializable]
    public sealed class LSO_PanelSlide
    {
        [SerializeField] private Ease ease = Ease.OutCubic;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float delay;
        [Tooltip("원래 자리에서 얼마나 위에서 출발할지(px). 0이면 창 높이만큼 — 화면 밖에서 내려온다.")]
        [SerializeField] private float startPos;

        private RectTransform _content;
        private CanvasGroup _group;
        private bool _ignoreTimeScale;

        private Vector2 _shownPos;
        private float _distance;
        private Sequence _tween;

        /// <summary>연출이 게임 시간과 무관하게 도는지. 시간을 멈추는 쪽이 이 값을 맞춰 줘야 한다.</summary>
        public bool IgnoreTimeScale => _ignoreTimeScale;

        public bool HasDuration => duration > 0f;

        public void Bind(RectTransform content, CanvasGroup group, bool ignoreTimeScale)
        {
            _content = content;
            _group = group;
            _ignoreTimeScale = ignoreTimeScale;

            _shownPos = content.anchoredPosition;
            // 0이면 창 높이만큼 위에서 출발한다 — 값을 안 넣어도 화면 밖에서 내려온다.
            _distance = startPos > 0f ? startPos : content.rect.height;
        }

        /// <summary>연출 없이 닫힌 모습으로 맞춘다(시작 시점).</summary>
        public void SnapClosed()
        {
            Kill();
            _content.anchoredPosition = HiddenPos;
            _group.alpha = 0f;
        }

        /// <summary>위에서 내려온다. 연출 도중 다시 불려도 항상 위에서 다시 시작한다.</summary>
        public void Show()
        {
            _content.anchoredPosition = HiddenPos;
            Play(_shownPos, 1f, null);
        }

        /// <summary>위로 올라가 사라진다. <paramref name="onDone"/>은 다 올라간 뒤에 불린다.</summary>
        public void Hide(Action onDone) => Play(HiddenPos, 0f, onDone);

        public void Kill()
        {
            _tween?.Kill();
            _tween = null;
        }

        private void Play(Vector2 target, float targetAlpha, Action onDone)
        {
            Kill();

            if (!HasDuration)
            {
                _content.anchoredPosition = target;
                _group.alpha = targetAlpha;
                onDone?.Invoke();
                return;
            }

            _tween = DOTween.Sequence()
                .SetUpdate(_ignoreTimeScale)
                .SetTarget(_content)
                .PrependInterval(delay)
                .Append(_content.DOAnchorPos(target, duration).SetEase(ease))
                // 배경은 창보다 빨리 나타났다 사라진다 — 창이 도착하기 전에 이미 어두워져 있어야 자연스럽다.
                .Join(_group.DOFade(targetAlpha, duration * 0.6f))
                .OnComplete(() =>
                {
                    _tween = null;
                    onDone?.Invoke();
                });
        }

        private Vector2 HiddenPos => _shownPos + Vector2.up * _distance;
    }
}
