using System.Collections.Generic;
using BlueComplex.UI.Motion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlueComplex.Audio
{
    /// <summary>
    /// UI 사운드 큐(<see cref="UiSoundHooks"/>)를 구독해 실제로 소리를 내는 유일한 컴포넌트. 씬에 하나만 있으면 된다
    /// (BlueComplex/Audio/Ensure Sound Manager로 없으면 만든다). 어떤 소리를 낼지는 코드가 아니라 <see cref="SoundLibrary"/>
    /// 에셋이 정한다 — 소리가 없는 큐는 그냥 조용히 지나간다(라이브러리에 아직 클립을 안 채운 것뿐, 오류 아님).
    ///
    /// AudioSource 여러 개를 돌려 쓴다(라운드로빈) — 한 프레임에 서로 다른 큐가 겹쳐 울려도 서로 끊기지 않는다.
    /// 다만 <b>같은</b> 큐가 아주 짧은 간격으로 연달아 오면(예: 단서 카드 여러 장 위로 마우스가 스칠 때) 무시한다 —
    /// 안 그러면 같은 소리가 겹쳐 뭉개진다(디바운스).
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        [Tooltip("비워 두면 Resources/SoundLibrary(UiSoundLibrary.Current)를 쓴다. 스테이지별로 다른 소리 세트를 쓸 때만 채운다.")]
        [SerializeField] private SoundLibrary _library;

        [Tooltip("동시에 겹칠 수 있는 소리 수.")]
        [SerializeField] [Min(1)] private int _voices = 6;

        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;

        [Tooltip("같은 큐가 이 시간(초) 안에 다시 오면 무시한다. 서로 다른 큐끼리는 영향받지 않는다.")]
        [SerializeField] [Min(0f)] private float _sameQueueCooldown = 0.09f;

        private AudioSource[] _pool;
        private int _next;
        private readonly Dictionary<UiSoundCue, float> _lastPlayedAt = new();

        private SoundLibrary Library => _library != null ? _library : UiSoundLibrary.Current;

        private void Awake()
        {
            _pool = new AudioSource[Mathf.Max(1, _voices)];
            for (var i = 0; i < _pool.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _pool[i] = source;
            }
        }

        private void OnEnable() => UiSoundHooks.Cue += Play;
        private void OnDisable() => UiSoundHooks.Cue -= Play;

        public void Play(UiSoundCue cue)
        {
            var now = Time.unscaledTime;
            if (_lastPlayedAt.TryGetValue(cue, out var last) && now - last < _sameQueueCooldown) return;

            var library = Library;
            if (library == null || !library.TryGet(cue, out var entry)) return;
            if (entry.clips.Length == 0) return;

            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;

            _lastPlayedAt[cue] = now;

            var source = NextVoice();
            source.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y);
            source.volume = entry.volume * _masterVolume;
            source.clip = clip;
            source.Play();
        }

        private AudioSource NextVoice()
        {
            var source = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            return source;
        }
    }
}
