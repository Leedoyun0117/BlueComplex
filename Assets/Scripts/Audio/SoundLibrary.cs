using System;
using System.Collections.Generic;
using BlueComplex.UI.Motion;
using UnityEngine;

namespace BlueComplex.Audio
{
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
}
