using UnityEngine;
using DG.Tweening;

namespace KTH
{
    /// <summary>
    /// 타임라인 Signal로 사운드를 켜고 끈다.
    /// 타임라인과 별개의 AudioSource를 쓰므로 KTH_TimelineSpeed로 속도를 바꿔도 피치/속도가 유지된다.
    /// SoundOn: AudioSource 활성화 → 볼륨 0 → 1 페이드인
    /// SoundOff: 볼륨 → 0 페이드아웃 → AudioSource 비활성화
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class KTH_TimelineAudioSignal : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float fadeOutDuration = 1f;

        private AudioSource audioSource;
        private Tween fadeTween;
        private bool isOn;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.pitch = 1f;
            audioSource.volume = 0f;
            audioSource.enabled = false;
        }

        private void OnDestroy()
        {
            fadeTween?.Kill();
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

            fadeTween = audioSource.DOFade(1f, fadeInDuration);
        }

        public void SoundOff()
        {
            isOn = false;
            if (!audioSource.enabled) return;

            fadeTween?.Kill();
            fadeTween = audioSource.DOFade(0f, fadeOutDuration)
                .OnComplete(() =>
                {
                    audioSource.Stop();
                    audioSource.enabled = false;
                });
        }
    }
}
