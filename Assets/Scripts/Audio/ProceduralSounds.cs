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
        /// <summary>뉴스가 줄마다 같은 잡음으로 들리지 않게 — 세 가지 잡음(시드가 다른 폭발 무늬)을 번갈아 쓰고, 볼륨 배율도 번갈아 달리한다(0.5 / 0.45 / 0.55).</summary>
        private static readonly float[] TvStaticVolumeFactor = { 1f, 0.9f, 1.1f };

        private static readonly Vector2 TvStaticPitch = new(0.95f, 1.05f);

        private static AudioClip[] _tvStatic;
        private static int _tvStaticNext;

        public static bool TryGet(UiSoundCue cue, out AudioClip clip, out float volume, out Vector2 pitchRange)
        {
            volume = 1f;
            pitchRange = Vector2.one;
            switch (cue)
            {
                case UiSoundCue.TvStatic:
                    _tvStatic ??= new[] { BuildTvStatic(0), BuildTvStatic(1), BuildTvStatic(2) };
                    var variant = _tvStaticNext++ % _tvStatic.Length;
                    clip = _tvStatic[variant];
                    volume = UiMotion.Settings.introStaticVolume * TvStaticVolumeFactor[variant];
                    pitchRange = TvStaticPitch; // 재생마다 피치도 무작위로 살짝.
                    return true;
                default:
                    clip = null;
                    return false;
            }
        }

        /// <summary>지직거리는 TV 잡음 2초: 처음에 센 잡음이 튀고 잦아들면서, 짧은 폭발(15~90ms)이 불규칙하게 끼어든다. 백색 잡음을 살짝 눌러 거친 고음을 덜어낸다.</summary>
        private static AudioClip BuildTvStatic(int variant)
        {
            const float seconds = 2f;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var samples = new float[count];
            var random = new System.Random(20260926 + variant * 7919);

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

            var clip = AudioClip.Create($"TvStatic {variant} (procedural)", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
