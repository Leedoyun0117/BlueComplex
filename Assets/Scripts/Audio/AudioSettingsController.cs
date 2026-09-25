using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace BlueComplex.Audio
{
    public enum AudioChannel
    {
        Master,
        Sfx,
        Bgm,
        Ambient,
    }

    /// <summary>
    /// 옵션 UI 슬라이더(0~1) ↔ 믹서 채널 볼륨 ↔ PlayerPrefs. <see cref="SoundManager"/>와 분리돼 있다 — SoundManager는 소스를 믹서 그룹에
    /// 물리기만 하고, 채널 볼륨은 여기서 믹서의 exposed 파라미터(MasterVolume/SFXVolume/BGMVolume/AmbientVolume)로만 만진다.
    /// 슬라이더는 인스펙터에 채워 두면 값 동기화·리스너 연결까지 하고, 비워 두면 <see cref="SetVolume"/>을 직접 불러 쓴다.
    /// 시작할 때 저장된 값을 믹서에 적용하므로 옵션 화면이 없어도(슬라이더 미할당) 이 컴포넌트가 씬에 있으면 저장값이 살아난다.
    /// </summary>
    public sealed class AudioSettingsController : MonoBehaviour
    {
        public const string DefaultMixerResource = "GameAudioMixer";
        public const string SfxGroupPath = "Master/SFX";
        public const string BgmGroupPath = "Master/BGM";
        public const string AmbientGroupPath = "Master/Ambient";

        /// <summary>dB로 바꿀 때의 바닥 — 슬라이더 0(무음)은 이 값으로 잘린다.</summary>
        public const float MinDecibels = -80f;

        private static readonly string[] ExposedParameters = { "MasterVolume", "SFXVolume", "BGMVolume", "AmbientVolume" };
        private static readonly string[] PrefKeys = { "Volume_Master", "Volume_SFX", "Volume_BGM", "Volume_Ambient" };

        [Tooltip("비워 두면 Resources/GameAudioMixer를 쓴다. SoundManager와 같은 믹서여야 한다.")]
        [SerializeField] private AudioMixer _mixer;

        [Header("옵션 UI 슬라이더(선택) — 값 범위 0~1")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _ambientSlider;

        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };

        /// <summary>선형 볼륨(0~1) → dB. 0 이하는 <see cref="MinDecibels"/>, 1 이상은 0dB(믹서 그룹은 증폭하지 않는다).</summary>
        public static float VolumeToDecibels(float linear)
        {
            if (linear <= 0f) return MinDecibels;
            return Mathf.Clamp(20f * Mathf.Log10(Mathf.Min(linear, 1f)), MinDecibels, 0f);
        }

        public static string PrefKey(AudioChannel channel) => PrefKeys[(int)channel];

        public float GetVolume(AudioChannel channel) => _volumes[(int)channel];

        private AudioMixer Mixer
        {
            get
            {
                if (_mixer == null) _mixer = Resources.Load<AudioMixer>(DefaultMixerResource);
                return _mixer;
            }
        }

        // 믹서 파라미터는 Awake 시점엔 초기 스냅샷이 덮어쓸 수 있어 Start에서 적용한다.
        private void Start()
        {
            for (var i = 0; i < _volumes.Length; i++)
                _volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeys[i], 1f));

            Bind(AudioChannel.Master, _masterSlider);
            Bind(AudioChannel.Sfx, _sfxSlider);
            Bind(AudioChannel.Bgm, _bgmSlider);
            Bind(AudioChannel.Ambient, _ambientSlider);

            for (var i = 0; i < _volumes.Length; i++)
                ApplyToMixer((AudioChannel)i);
        }

        private void OnDestroy()
        {
            Unbind(AudioChannel.Master, _masterSlider);
            Unbind(AudioChannel.Sfx, _sfxSlider);
            Unbind(AudioChannel.Bgm, _bgmSlider);
            Unbind(AudioChannel.Ambient, _ambientSlider);
        }

        /// <summary>채널 볼륨(0~1)을 믹서에 적용하고 PlayerPrefs에 저장한다.</summary>
        public void SetVolume(AudioChannel channel, float linear)
        {
            var value = Mathf.Clamp01(linear);
            _volumes[(int)channel] = value;
            ApplyToMixer(channel);

            PlayerPrefs.SetFloat(PrefKeys[(int)channel], value);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float linear) => SetVolume(AudioChannel.Master, linear);
        public void SetSfxVolume(float linear) => SetVolume(AudioChannel.Sfx, linear);
        public void SetBgmVolume(float linear) => SetVolume(AudioChannel.Bgm, linear);
        public void SetAmbientVolume(float linear) => SetVolume(AudioChannel.Ambient, linear);

        private void ApplyToMixer(AudioChannel channel)
        {
            var mixer = Mixer;
            if (mixer == null) return;

            var name = ExposedParameters[(int)channel];
            if (!mixer.SetFloat(name, VolumeToDecibels(_volumes[(int)channel])))
                Debug.LogWarning($"[AudioSettingsController] 믹서에 exposed 파라미터 '{name}'이 없다.");
        }

        private void Bind(AudioChannel channel, Slider slider)
        {
            if (slider == null) return;

            slider.SetValueWithoutNotify(_volumes[(int)channel]);
            slider.onValueChanged.AddListener(GetListener(channel));
        }

        private void Unbind(AudioChannel channel, Slider slider)
        {
            if (slider == null) return;

            slider.onValueChanged.RemoveListener(GetListener(channel));
        }

        private UnityEngine.Events.UnityAction<float> GetListener(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => SetMasterVolume,
            AudioChannel.Sfx => SetSfxVolume,
            AudioChannel.Bgm => SetBgmVolume,
            _ => SetAmbientVolume,
        };
    }
}
