using BlueComplex.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace KTH
{
    /// <summary>
    /// 효과음을 재생하는 공용 진입점. 필요한 곳에서 클립을 넘겨 부르면 된다 — 시그널/트랙 설정이 필요 없다.
    ///   KTH_Sfx.Play(clip);             // 끝까지 한 번
    ///   KTH_Sfx.Play(clip, 0.7f);
    ///   KTH_Sfx.PlayFor(clip, 5f);      // 5초만 재생하고 짧게 페이드아웃
    /// - 믹서 SFX 채널로 내보내므로 옵션의 SFX 볼륨이 적용된다.
    /// - 타임라인과 별개의 AudioSource라 KTH_TimelineSpeed로 속도를 바꿔도 피치/속도가 유지된다.
    /// - 소스 여러 개를 돌려 쓰므로 효과음끼리, 그리고 BGM과도 서로 끊기지 않는다.
    /// 처음 호출될 때 씬 전환에도 남는 전용 오브젝트([KTH_Sfx])를 만든다.
    /// </summary>
    public static class KTH_Sfx
    {
        private const int OneShotVoices = 6;

        // 시간 지정 재생은 도중에 멈춰야 하므로 원샷용 소스와 나눈다(원샷이 피치를 바꿔도 영향받지 않게)
        private const int TimedVoices = 3;

        private static AudioSource[] oneShotPool;
        private static int nextOneShot;

        private static AudioSource[] timedPool;
        private static Tween[] timedTweens;
        private static int nextTimed;

        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            // 에디터 프리뷰에서 부르면 씬에 오브젝트가 남으므로 플레이 중에만 재생한다
            if (clip == null || !Application.isPlaying) return;
            EnsurePools();

            var source = oneShotPool[nextOneShot];
            nextOneShot = (nextOneShot + 1) % oneShotPool.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// 클립을 startAt초 지점부터 duration초만 재생하고 fadeOut초 동안 줄이며 멈춘다.
        /// duration이 0 이하이면 끝까지 재생한다. startAt은 앞부분 무음을 건너뛸 때 쓴다.
        /// </summary>
        public static void PlayFor(AudioClip clip, float duration, float volume = 1f, float fadeOut = 0.3f, float startAt = 0f)
        {
            if (clip == null || !Application.isPlaying) return;
            startAt = Mathf.Clamp(startAt, 0f, clip.length);
            var remaining = clip.length - startAt;
            if (startAt <= 0f && (duration <= 0f || duration >= remaining))
            {
                Play(clip, volume);
                return;
            }
            if (duration <= 0f || duration > remaining) duration = remaining;
            EnsurePools();

            var i = nextTimed;
            nextTimed = (nextTimed + 1) % timedPool.Length;
            var source = timedPool[i];

            timedTweens[i]?.Kill();
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.Play();
            source.time = startAt;

            fadeOut = Mathf.Clamp(fadeOut, 0f, duration);
            timedTweens[i] = DOTween.Sequence()
                .AppendInterval(duration - fadeOut)
                .Append(source.DOFade(0f, fadeOut))
                .AppendCallback(source.Stop)
                .SetUpdate(true)
                .SetLink(source.gameObject);
        }

        public static void StopAll()
        {
            if (oneShotPool == null || oneShotPool[0] == null) return;
            foreach (var source in oneShotPool) source.Stop();
            for (var i = 0; i < timedPool.Length; i++)
            {
                timedTweens[i]?.Kill();
                timedPool[i].Stop();
            }
        }

        private static void EnsurePools()
        {
            // 도메인 리로드를 끈 상태로 플레이를 다시 시작하면 static은 남고 오브젝트만 사라지므로 다시 만든다
            if (oneShotPool != null && oneShotPool[0] != null) return;

            var go = new GameObject("[KTH_Sfx]");
            Object.DontDestroyOnLoad(go);
            var group = FindSfxGroup();

            oneShotPool = CreateSources(go, OneShotVoices, group);
            timedPool = CreateSources(go, TimedVoices, group);
            timedTweens = new Tween[TimedVoices];
            nextOneShot = 0;
            nextTimed = 0;
        }

        private static AudioSource[] CreateSources(GameObject go, int count, AudioMixerGroup group)
        {
            var sources = new AudioSource[count];
            for (var i = 0; i < count; i++)
            {
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = group;
                sources[i] = source;
            }
            return sources;
        }

        private static AudioMixerGroup FindSfxGroup()
        {
            var mixer = Resources.Load<AudioMixer>(AudioSettingsController.DefaultMixerResource);
            if (mixer == null) return null;

            var groups = mixer.FindMatchingGroups(AudioSettingsController.SfxGroupPath);
            if (groups == null || groups.Length == 0)
            {
                Debug.LogWarning($"[KTH_Sfx] 믹서 '{mixer.name}'에 그룹 '{AudioSettingsController.SfxGroupPath}'가 없다 — 옵션 볼륨이 적용되지 않는다.");
                return null;
            }

            return groups[0];
        }
    }
}
