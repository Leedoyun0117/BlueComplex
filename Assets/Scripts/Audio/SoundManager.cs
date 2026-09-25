using System.Collections.Generic;
using BlueComplex.UI.Motion;
using UnityEngine;
using UnityEngine.Audio;
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

        [Tooltip("채널 볼륨(Master/SFX/BGM/Ambient 그룹)을 가진 믹서. 비워 두면 Resources/GameAudioMixer를 쓴다. 볼륨 슬라이더는 AudioSettingsController가 맡는다.")]
        [SerializeField] private AudioMixer _mixer;

        [Tooltip("같은 큐가 이 시간(초) 안에 다시 오면 무시한다. 서로 다른 큐끼리는 영향받지 않는다.")]
        [SerializeField] [Min(0f)] private float _sameQueueCooldown = 0.09f;

        [Header("배경음(루프)")]
        [Tooltip("배경음이 다른 배경음으로 갈아탈 때 크로스페이드 시간(초).")]
        [SerializeField] [Min(0f)] private float _bedCrossfade = 0.6f;

        [Header("상시 배경음 레이어(Fragile Notes)")]
        [Tooltip("스테이지 시작 때 상시 배경음이 올라오는 시간(초).")]
        [SerializeField] [Min(0f)] private float _ambientFadeIn = 2f;

        [Tooltip("스테이지가 끝날 때 상시 배경음이 사라지는 시간(초).")]
        [SerializeField] [Min(0f)] private float _ambientFadeOut = 2.5f;

        [Tooltip("곡이 끝나기 이 시간(초) 전부터 처음과 겹쳐 크로스페이드해 루프 이음매를 가린다.")]
        [SerializeField] [Min(0.1f)] private float _ambientLoopSeam = 3f;

        private AudioSource[] _pool;
        private int _next;

        /// <summary>배경음 전용 소스 둘 — 하나가 사라지는 동안 다른 하나가 올라온다.</summary>
        private AudioSource[] _bedSources;
        private int _bedActive = -1;
        private float _bedTargetVolume;
        private readonly float[] _bedFade = new float[2];
        private AmbientLayer _ambient;
        private readonly Dictionary<UiSoundCue, float> _lastPlayedAt = new();

        private SoundLibrary Library => _library != null ? _library : UiSoundLibrary.Current;

        private void Awake()
        {
            // 믹서가 없으면(에셋 누락) 그룹이 null이라 소스가 기본 출력으로 나간다 — 소리는 나되 채널 볼륨만 안 먹는다.
            var mixer = _mixer != null ? _mixer : Resources.Load<AudioMixer>(AudioSettingsController.DefaultMixerResource);
            var sfxGroup = FindGroup(mixer, AudioSettingsController.SfxGroupPath);
            var bgmGroup = FindGroup(mixer, AudioSettingsController.BgmGroupPath);
            var ambientGroup = FindGroup(mixer, AudioSettingsController.AmbientGroupPath);

            _pool = new AudioSource[Mathf.Max(1, _voices)];
            for (var i = 0; i < _pool.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = sfxGroup;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _pool[i] = source;
            }

            _bedSources = new AudioSource[2];
            for (var i = 0; i < _bedSources.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = bgmGroup;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = true;
                source.volume = 0f;
                _bedSources[i] = source;
            }

            // 상시 배경음 레이어는 심박수 배경음 소스와 완전히 별개의 소스 둘을 쓴다.
            _ambient = new AmbientLayer(gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>(), ambientGroup);
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string path)
        {
            if (mixer == null) return null;

            var groups = mixer.FindMatchingGroups(path);
            if (groups == null || groups.Length == 0)
            {
                Debug.LogWarning($"[SoundManager] 믹서 '{mixer.name}'에 그룹 '{path}'가 없다 — 이 채널은 믹서를 거치지 않는다.");
                return null;
            }

            return groups[0];
        }

        private void OnEnable()
        {
            UiSoundHooks.Cue += Play;
            UiSoundHooks.BedChanged += SetBed;
            SetBed(UiSoundHooks.CurrentBed); // 배경음이 이미 요청된 뒤에 켜졌어도 따라잡는다.

            UiSoundHooks.AmbientStarted += StartAmbient;
            UiSoundHooks.AmbientStopped += StopAmbient;
            if (UiSoundHooks.CurrentAmbient.HasValue && !_ambient.IsActive) StartAmbient(UiSoundHooks.CurrentAmbient.Value);
        }

        private void OnDisable()
        {
            UiSoundHooks.Cue -= Play;
            UiSoundHooks.BedChanged -= SetBed;
            UiSoundHooks.AmbientStarted -= StartAmbient;
            UiSoundHooks.AmbientStopped -= StopAmbient;
        }

        private void Update()
        {
            if (_bedSources == null) return;

            _ambient.Tick(Time.unscaledDeltaTime);

            var step = _bedCrossfade > 0f ? Time.unscaledDeltaTime / _bedCrossfade : 1f;
            for (var i = 0; i < _bedSources.Length; i++)
            {
                var source = _bedSources[i];
                var goal = i == _bedActive ? _bedTargetVolume : 0f;
                source.volume = Mathf.MoveTowards(source.volume, goal, step * Mathf.Max(_bedTargetVolume, 0.01f));

                // 완전히 사라진 소스는 멈춘다(재생 위치를 붙들고 있지 않게).
                if (i != _bedActive && source.isPlaying && source.volume <= 0f) source.Stop();
            }
        }

        /// <summary>배경음(루프)을 바꾼다. 라이브러리에 클립이 없는 큐면 배경음을 끈다. 새 배경음은 처음부터 시작해 크로스페이드로 올라온다.</summary>
        public void SetBed(UiSoundCue? cue)
        {
            if (_bedSources == null) return;

            SoundEntry entry = null;
            AudioClip clip = null;
            var library = Library;
            if (cue.HasValue && library != null && library.TryGet(cue.Value, out entry) && entry.clips.Length > 0)
                clip = entry.clips[0];

            if (clip == null)
            {
                _bedActive = -1; // 활성 소스가 없으면 Update가 둘 다 페이드아웃 후 멈춘다.
                return;
            }

            // 지금 켜져 있는 소스와 다른 쪽을 새 배경음으로 쓴다.
            _bedActive = _bedActive == 0 ? 1 : 0;
            var source = _bedSources[_bedActive];
            source.clip = clip;
            source.pitch = 1f;
            source.volume = 0f;
            _bedTargetVolume = entry.volume;
            source.Play();
        }

        /// <summary>상시 배경음 레이어를 처음부터 시작한다(이미 재생 중이면 재시작). 라이브러리에 클립이 없으면 조용히 지나간다.</summary>
        public void StartAmbient(UiSoundCue cue)
        {
            var library = Library;
            if (library == null || !library.TryGet(cue, out var entry) || entry.clips.Length == 0 || entry.clips[0] == null) return;

            _ambient.Begin(entry.clips[0], entry.volume, _ambientFadeIn, _ambientLoopSeam);
        }

        public void StopAmbient() => _ambient.End(_ambientFadeOut);

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
            source.volume = entry.volume;
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
