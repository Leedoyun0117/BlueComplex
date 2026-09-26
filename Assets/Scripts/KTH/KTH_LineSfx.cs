using System;
using UnityEngine;

namespace KTH
{
    /// <summary>
    /// 특정 대사가 나올 때(또는 다 찍혔을 때) 효과음을 낸다. 타임라인에 마커를 찍지 않고 인스펙터에서 대사 번호로 지정한다.
    /// 같은 오브젝트의 <see cref="KTH_TypingTextSignal"/> 이벤트를 듣기만 하므로, 대사 시그널 위치가 바뀌어도 따라간다.
    /// </summary>
    public class KTH_LineSfx : MonoBehaviour
    {
        public enum Timing
        {
            [Tooltip("대사 타이핑이 시작될 때")] LineStart,
            [Tooltip("대사가 끝까지 다 찍혔을 때")] LineEnd,
        }

        [Serializable]
        public class Entry
        {
            [Tooltip("KTH_TypingTextSignal Lines의 Element 번호(0부터)")]
            public int line;
            public Timing timing = Timing.LineStart;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;

            [Tooltip("재생할 시간(초). 0이면 끝까지 재생한다.")]
            [Min(0f)] public float duration;

            [Tooltip("클립의 이 지점(초)부터 재생한다. 앞부분 무음을 건너뛸 때 쓴다.")]
            [Min(0f)] public float startAt;
        }

        [SerializeField] private KTH_TypingTextSignal typing;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        private void Awake()
        {
            if (typing == null) typing = GetComponent<KTH_TypingTextSignal>();
        }

        private void OnEnable()
        {
            if (typing == null) return;
            typing.LineStarted += OnLineStarted;
            typing.LineCompleted += OnLineCompleted;
        }

        private void OnDisable()
        {
            if (typing == null) return;
            typing.LineStarted -= OnLineStarted;
            typing.LineCompleted -= OnLineCompleted;
        }

        private void OnLineStarted(int line) => PlayMatching(line, Timing.LineStart);
        private void OnLineCompleted(int line) => PlayMatching(line, Timing.LineEnd);

        private void PlayMatching(int line, Timing timing)
        {
            foreach (var e in entries)
            {
                if (e.line == line && e.timing == timing)
                    KTH_Sfx.PlayFor(e.clip, e.duration, e.volume, startAt: e.startAt);
            }
        }
    }
}
