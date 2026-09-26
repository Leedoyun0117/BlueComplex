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

        [Header("타이핑 효과음 (비워 두면 소리 없음)")]
        [SerializeField] private AudioClip typingSfx;
        [SerializeField, Range(0f, 1f)] private float typingSfxVolume = 0.5f;

        [Tooltip("효과음 사이 최소 간격(초). 글자가 빨리 찍혀도 소리가 뭉개지지 않게 한다.")]
        [SerializeField, Min(0f)] private float typingSfxMinInterval = 0.06f;

        [Tooltip("재생할 때마다 이 범위에서 피치를 랜덤으로 골라 단조롭지 않게 한다.")]
        [SerializeField] private Vector2 typingSfxPitch = new(0.95f, 1.05f);

        /// <summary>대사 타이핑을 시작할 때 (Lines의 번호, 직접 넘긴 문장이면 -1).</summary>
        public event System.Action<int> LineStarted;

        /// <summary>대사가 끝까지 다 찍혔을 때 (Complete()로 건너뛴 경우 포함).</summary>
        public event System.Action<int> LineCompleted;

        private int index = -1;
        private int typingLine = -1;
        private Tween typingTween;
        private float lastSfxTime = float.NegativeInfinity;

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
            TypeLine(index, lines[index]);
        }

        /// <summary>특정 번호의 문장을 타이핑한다.</summary>
        public void ShowText(int lineIndex)
        {
            if (lines == null || lineIndex < 0 || lineIndex >= lines.Length) return;
            index = lineIndex;
            TypeLine(index, lines[index]);
        }

        /// <summary>문장을 직접 넘겨서 타이핑한다 (Reaction에서 string 인자로 사용 가능).</summary>
        public void Type(string line) => TypeLine(-1, line);

        private void TypeLine(int lineIndex, string line)
        {
            typingTween?.Kill();
            typingLine = lineIndex;

            text.text = line ?? string.Empty;
            text.ForceMeshUpdate();
            int count = text.textInfo.characterCount; // 리치 텍스트 태그 제외한 실제 글자 수
            text.maxVisibleCharacters = 0;

            typingTween = DOTween.To(
                    () => text.maxVisibleCharacters,
                    SetVisibleCharacters,
                    count,
                    count * charInterval)
                .SetEase(Ease.Linear)
                .OnComplete(() => LineCompleted?.Invoke(lineIndex));

            LineStarted?.Invoke(lineIndex);
        }

        /// <summary>타이핑 중이면 즉시 전체 표시 (남은 글자 효과음은 내지 않는다).</summary>
        public void Complete()
        {
            if (typingTween == null || !typingTween.IsActive()) return;
            typingTween.Kill();
            text.maxVisibleCharacters = text.textInfo.characterCount;
            LineCompleted?.Invoke(typingLine);
        }

        private void SetVisibleCharacters(int visible)
        {
            int previous = text.maxVisibleCharacters;
            text.maxVisibleCharacters = visible;
            if (visible > previous) PlayTypingSfx(previous, visible);
        }

        // 새로 보이게 된 글자 중 공백이 아닌 글자가 있을 때만 소리를 낸다
        private void PlayTypingSfx(int from, int to)
        {
            if (typingSfx == null) return;
            if (Time.unscaledTime - lastSfxTime < typingSfxMinInterval) return;

            var info = text.textInfo.characterInfo;
            for (int i = from; i < to && i < text.textInfo.characterCount; i++)
            {
                if (char.IsWhiteSpace(info[i].character)) continue;

                lastSfxTime = Time.unscaledTime;
                KTH_Sfx.Play(typingSfx, typingSfxVolume, Random.Range(typingSfxPitch.x, typingSfxPitch.y));
                return;
            }
        }
    }
}
