using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlueComplex.UI.Layout
{
    /// <summary>유키 대사 한 줄. 한 글자씩 출력하고, 출력 중 클릭하면 즉시 전체 표시로 건너뛴다.</summary>
    public sealed class DialogueText : MonoBehaviour, IPointerClickHandler
    {
        private const float SecondsPerChar = 0.03f;

        [SerializeField] private TMP_Text _label;

        private Coroutine _typing;
        private string _fullLine = string.Empty;

        public RectTransform Root => (RectTransform)transform;

        /// <summary>타이핑이 끝났을 때(자연 종료든 클릭으로 건너뛰었든) 한 번 쏜다 — 3단계
        /// CinematicTurnResultPresenter가 이걸로 다음 연출 단계로 넘어갈 타이밍을 잡는다.</summary>
        public event Action TypingComplete;

        /// <summary>즉시 표시(연출 없음). 1단계부터 있던 메서드 — 초기화·되돌리기용으로 유지.</summary>
        public void SetLine(string line)
        {
            StopTyping();
            _fullLine = line ?? string.Empty;
            if (_label != null) _label.text = _fullLine;
        }

        /// <summary>한 글자씩 재생한다. 재생 중이면 새 줄로 갈아친다.</summary>
        public void PlayTyped(string line)
        {
            StopTyping();
            _fullLine = line ?? string.Empty;
            if (_label != null) _label.text = string.Empty;
            _typing = StartCoroutine(TypeRoutine());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_typing == null) return;
            StopTyping();
            if (_label != null) _label.text = _fullLine;
            TypingComplete?.Invoke();
        }

        private void OnDisable() => StopTyping();

        private void StopTyping()
        {
            if (_typing == null) return;
            StopCoroutine(_typing);
            _typing = null;
        }

        private IEnumerator TypeRoutine()
        {
            var wait = new WaitForSeconds(SecondsPerChar);
            for (var i = 1; i <= _fullLine.Length; i++)
            {
                if (_label != null) _label.text = _fullLine.Substring(0, i);
                yield return wait;
            }
            _typing = null;
            TypingComplete?.Invoke();
        }
    }
}
