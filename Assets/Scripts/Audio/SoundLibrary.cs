using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using UnityEngine;

namespace BlueComplex.Audio
{
    /// <summary>
    /// <see cref="UiSoundCue"/> 하나에 재생할 클립들을 묶어 둔 항목. 클립을 여러 개 넣으면 재생할 때마다 그중 하나를
    /// 무작위로 고른다(같은 소리가 반복돼 단조롭지 않게). 새 소리를 추가할 때는 코드를 건드릴 필요 없이
    /// 인스펙터에서 이 배열에 항목을 늘리거나 기존 항목의 Clips에 파일을 끌어넣으면 된다.
    /// </summary>
    [Serializable]
    public sealed class SoundEntry
    {
        public UiSoundCue cue;
        public AudioClip[] clips = Array.Empty<AudioClip>();

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("재생할 때마다 이 범위 안에서 피치를 무작위로 살짝 바꿔 반복 재생이 기계적으로 들리지 않게 한다.")]
        public Vector2 pitchRange = new(0.97f, 1.03f);

        [Tooltip("0이면 공용 보이스를 돌려 쓴다. 1 이상이면 이 큐만 쓰는 전용 보이스를 그 수만큼 둔다 — 긴 클립(뉴스 잡음·빛 번짐)이 다른 소리에 끊기지 않고, " +
                 "1이면 다시 불렀을 때 이전 소리를 끊고 처음부터 다시 울린다(겹쳐 쌓이지 않는다). 짧게 연달아 울리는 소리(타자음)는 3~5.")]
        [Min(0)] public int dedicatedVoices;

        [Tooltip("같은 큐가 이 시간(초) 안에 다시 오면 무시한다. 0(비워 둠)이면 SoundManager의 기본값을 쓴다. 글자마다 울리는 타자음처럼 촘촘한 큐는 짧게 둔다.")]
        [Min(0f)] public float minInterval;

        [Tooltip("0(비워 둠)이면 클립을 끝까지 울린다. 0보다 크면 이 시간(초)까지만 쓰고 끝의 0.25초를 페이드아웃해 자른다 — 클립이 너무 길 때(빛 번짐 7초 중 3초만).")]
        [Min(0f)] public float maxSeconds;
    }

    /// <summary>
    /// UI 사운드 큐(<see cref="UiSoundHooks"/>)와 실제 오디오 클립을 잇는 표. UiMotionSettings와 같은 방식으로
    /// Assets/Resources에 하나 두고 인스펙터에서만 채운다(BlueComplex/Audio/Ensure Sound Library로 없으면 만든다).
    /// 여러 스테이지·씬에서 다른 소리 세트를 쓰고 싶으면 이 에셋을 복제해 SoundManager에 따로 물리면 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "BlueComplex/Audio/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        public const string ResourcePath = "SoundLibrary";

        [SerializeField] private SoundEntry[] _entries = Array.Empty<SoundEntry>();

        private Dictionary<UiSoundCue, SoundEntry> _byCue;

        public bool TryGet(UiSoundCue cue, out SoundEntry entry)
        {
            _byCue ??= BuildLookup();
            return _byCue.TryGetValue(cue, out entry);
        }

        private void OnValidate() => _byCue = null;

        private Dictionary<UiSoundCue, SoundEntry> BuildLookup()
        {
            var map = new Dictionary<UiSoundCue, SoundEntry>();
            foreach (var entry in _entries)
                if (entry != null && entry.clips is { Length: > 0 })
                    map[entry.cue] = entry;
            return map;
        }
    }

    /// <summary>라이브러리 조회. 에셋이 없으면(아직 Ensure를 안 돌렸거나 Resources 밖에 있으면) 빈 라이브러리로 동작해
    /// 아무 소리도 나지 않을 뿐 예외는 나지 않는다.</summary>
    public static class UiSoundLibrary
    {
        private static SoundLibrary _current;

        public static SoundLibrary Current
        {
            get
            {
                if (_current != null) return _current;

                _current = Resources.Load<SoundLibrary>(SoundLibrary.ResourcePath);
                if (_current != null) return _current;

                _current = ScriptableObject.CreateInstance<SoundLibrary>();
                _current.hideFlags = HideFlags.HideAndDontSave;
                return _current;
            }
        }
    }
}
