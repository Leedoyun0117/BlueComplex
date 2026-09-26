using BlueComplex.UI.Motion;
using UnityEngine;

namespace BlueComplex.Audio
{
    /// <summary>
    /// 라이브러리에 클립이 아직 없는 큐를 위한 코드로 만든 임시 소리. <see cref="SoundManager"/>는 <see cref="SoundLibrary"/> 항목이 있으면 그걸 쓰고,
    /// 없을 때만 여기서 가져온다 — 진짜 효과음을 라이브러리에 채우면 이 코드는 저절로 쓰이지 않는다.
    /// </summary>
    public static class ProceduralSounds
    {
        private const int SampleRate = 44100;
        private static AudioClip _tvStatic;

        public static bool TryGet(UiSoundCue cue, out AudioClip clip, out float volume)
        {
            volume = 1f;
            switch (cue)
            {
                case UiSoundCue.TvStatic:
                    if (_tvStatic == null) _tvStatic = BuildTvStatic();
                    clip = _tvStatic;
                    volume = UiMotion.Settings.introStaticVolume;
                    return true;
                default:
                    clip = null;
                    return false;
            }
        }

        /// <summary>지직거리는 TV 잡음 2초: 처음에 센 잡음이 튀고 잦아들면서, 짧은 폭발(15~90ms)이 불규칙하게 끼어든다. 백색 잡음을 살짝 눌러 거친 고음을 덜어낸다.</summary>
        private static AudioClip BuildTvStatic()
        {
            const float seconds = 2f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var samples = new float[count];
            var random = new System.Random(20260926);

            var lowPass = 0f;
            var index = 0;
            while (index < count)
            {
                var length = Mathf.RoundToInt(SampleRate * (0.015f + (float)random.NextDouble() * 0.075f));
                var burst = random.NextDouble() < 0.45;
                var level = burst ? 0.35f + (float)random.NextDouble() * 0.5f : 0.06f + (float)random.NextDouble() * 0.06f;

                for (var i = 0; i < length && index < count; i++, index++)
                {
                    var t = index / (float)SampleRate;
                    var attack = 0.4f + 0.6f * Mathf.Exp(-t * 2.2f);
                    var white = (float)(random.NextDouble() * 2.0 - 1.0);
                    lowPass = lowPass * 0.45f + white * 0.55f;
                    samples[index] = lowPass * level * attack;
                }
            }

            // 끝과 처음을 부드럽게 — 다음 줄에서 다시 재생할 때 딱 소리가 나지 않게.
            var fade = SampleRate / 50;
            for (var i = 0; i < fade; i++)
            {
                var k = i / (float)fade;
                samples[i] *= k;
                samples[count - 1 - i] *= k;
            }

            var clip = AudioClip.Create("TvStatic (procedural)", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
