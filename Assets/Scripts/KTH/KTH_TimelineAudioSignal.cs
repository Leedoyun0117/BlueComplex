using BlueComplex.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace KTH
{
    /// <summary>
    /// 타임라인 Signal로 사운드를 켜고 끈다.
    /// 타임라인과 별개의 AudioSource를 쓰므로 KTH_TimelineSpeed로 속도를 바꿔도 피치/속도가 유지된다.
    /// SoundOn: AudioSource 활성화 → 볼륨 0 → volume 페이드인
    /// SoundOff: 볼륨 → 0 페이드아웃 → AudioSource 비활성화
    ///
    /// 볼륨 역할 분리:
    /// - 플레이어 설정 볼륨(옵션 슬라이더): 믹서 채널이 맡는다. AudioSource를 channel 그룹에 연결하므로
    ///   AudioSettingsController.SetVolume(AudioChannel.Bgm, x)만 부르면 이 소리도 같이 조절된다.
    /// - 곡 기본 볼륨: volume 필드(또는 Volume 프로퍼티).
    /// - 페이드: 이 컴포넌트 내부에서만 audioSource.volume을 만진다 — 외부에서 audioSource.volume을 직접 건드리지 않는다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class KTH_TimelineAudioSignal : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;

        [Tooltip("곡 기본 볼륨. 페이드인은 이 값까지 올라간다. 플레이어 옵션 볼륨은 믹서가 따로 곱한다.")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float fadeOutDuration = 1f;

        [Header("믹서 연결")]
        [Tooltip("출력할 믹서 채널. AudioSource의 Output을 인스펙터에서 직접 지정했으면 그쪽이 우선한다.")]
        [SerializeField] private AudioChannel channel = AudioChannel.Bgm;

        [Tooltip("비워 두면 Resources/GameAudioMixer를 쓴다.")]
        [SerializeField] private AudioMixer mixer;

        private AudioSource audioSource;
        private Tween fadeTween;
        private bool isOn;

        /// <summary>켜져 있는(또는 켜지는 중인) 상태인지.</summary>
        public bool IsOn => isOn;

        public AudioChannel Channel => channel;

        /// <summary>곡 기본 볼륨(0~1). 재생 중에 바꾸면 페이드인 시간 동안 새 값으로 옮겨간다.</summary>
        public float Volume
        {
            get => volume;
            set
            {
                volume = Mathf.Clamp01(value);
                if (!isOn || audioSource == null || !audioSource.enabled) return;

                fadeTween?.Kill();
                fadeTween = Fade(volume, fadeInDuration);
            }
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.pitch = 1f;
            audioSource.volume = 0f;
            audioSource.enabled = false;

            if (audioSource.outputAudioMixerGroup == null)
                audioSource.outputAudioMixerGroup = FindGroup();
        }

        private void OnDisable()
        {
            // 오브젝트가 꺼지면 페이드 도중이어도 정리한다(다시 켜질 때 꼬이지 않게).
            fadeTween?.Kill();
            fadeTween = null;
            isOn = false;
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.volume = 0f;
            audioSource.enabled = false;
        }

        /// <summary>시그널 하나로 켜고 끌 때 사용. 꺼져 있으면 켜고, 켜져 있으면 끈다.</summary>
        public void SoundToggle()
        {
            if (isOn) SoundOff();
            else SoundOn();
        }

        public void SoundOn()
        {
            isOn = true;
            fadeTween?.Kill();

            audioSource.enabled = true;
            audioSource.pitch = 1f;
            if (clip != null) audioSource.clip = clip;
            if (!audioSource.isPlaying)
            {
                audioSource.volume = 0f;
                audioSource.Play();
            }

            fadeTween = Fade(volume, fadeInDuration);
        }

        public void SoundOff()
        {
            isOn = false;
            if (!audioSource.enabled) return;

            fadeTween?.Kill();
            fadeTween = Fade(0f, fadeOutDuration)
                .OnComplete(() =>
                {
                    audioSource.Stop();
                    audioSource.enabled = false;
                });
        }

        /// <summary>
        /// timeScale과 무관하게 페이드한다 — AudioSource는 timeScale=0(ESC 일시정지)에서도 계속 재생되므로
        /// 페이드도 같이 진행돼야 볼륨이 중간값에 멈춰 있지 않는다.
        /// </summary>
        private Tween Fade(float target, float duration) =>
            audioSource.DOFade(target, duration).SetUpdate(true).SetLink(gameObject);

        private AudioMixerGroup FindGroup()
        {
            var m = mixer != null ? mixer : Resources.Load<AudioMixer>(AudioSettingsController.DefaultMixerResource);
            if (m == null) return null;

            var path = channel switch
            {
                AudioChannel.Sfx => AudioSettingsController.SfxGroupPath,
                AudioChannel.Bgm => AudioSettingsController.BgmGroupPath,
                AudioChannel.Ambient => AudioSettingsController.AmbientGroupPath,
                _ => "Master",
            };

            var groups = m.FindMatchingGroups(path);
            if (groups == null || groups.Length == 0)
            {
                Debug.LogWarning($"[KTH_TimelineAudioSignal] 믹서 '{m.name}'에 그룹 '{path}'가 없다 — 옵션 볼륨이 적용되지 않는다.", this);
                return null;
            }

            return groups[0];
        }
    }
}
