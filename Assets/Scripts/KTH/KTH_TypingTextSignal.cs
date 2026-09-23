using DG.Tweening;
using TMPro;
using UnityEngine;

namespace KTH
{
    /// <summary>
    /// 타임라인 Signal을 받을 때마다 다음 문장으로 바꾸고 한 글자씩 타이핑한다.
    /// SignalReceiver의 Reaction에 NextText()를 연결해서 사용.
    /// </summary>
    public class KTH_TypingTextSignal : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField, TextArea(2, 5)] private string[] lines;
        [SerializeField] private float charInterval = 0.05f;
        [SerializeField] private bool clearOnEnable = true;

        private int index = -1;
        private Tween typingTween;

        private void OnEnable()
        {
            index = -1;
            if (clearOnEnable && text != null) text.text = string.Empty;
        }

        private void OnDisable()
        {
            typingTween?.Kill();
        }

        /// <summary>시그널마다 호출 — 다음 문장을 타이핑한다.</summary>
        public void NextText()
        {
            if (lines == null || lines.Length == 0) return;
            index = Mathf.Min(index + 1, lines.Length - 1);
            Type(lines[index]);
        }

        /// <summary>특정 번호의 문장을 타이핑한다.</summary>
        public void ShowText(int lineIndex)
        {
            if (lines == null || lineIndex < 0 || lineIndex >= lines.Length) return;
            index = lineIndex;
            Type(lines[index]);
        }

        /// <summary>문장을 직접 넘겨서 타이핑한다 (Reaction에서 string 인자로 사용 가능).</summary>
        public void Type(string line)
        {
            typingTween?.Kill();

            text.text = line ?? string.Empty;
            text.ForceMeshUpdate();
            int count = text.textInfo.characterCount; // 리치 텍스트 태그 제외한 실제 글자 수
            text.maxVisibleCharacters = 0;

            typingTween = DOTween.To(
                    () => text.maxVisibleCharacters,
                    x => text.maxVisibleCharacters = x,
                    count,
                    count * charInterval)
                .SetEase(Ease.Linear);
        }

        /// <summary>타이핑 중이면 즉시 전체 표시.</summary>
        public void Complete()
        {
            typingTween?.Complete();
        }
    }
}
