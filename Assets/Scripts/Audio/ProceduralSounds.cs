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

        private static AudioClip _carEngine;
        private static Vector2 _carEngineKey;

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
                case UiSoundCue.CarEngine:
                    var settings = UiMotion.Settings;
                    var key = new Vector2(settings.introCarLead, settings.introCarDrive);
                    if (_carEngine == null || _carEngineKey != key)
                    {
                        if (_carEngine != null) Object.Destroy(_carEngine);
                        _carEngine = BuildCarEngine(key.x, key.y);
                        _carEngineKey = key;
                    }

                    clip = _carEngine;
                    volume = settings.introEngineVolume;
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

        /// <summary>
        /// 컷신의 자동차 엔진·배기음. 시간은 컷신 순서와 같다: 암전 속에서 멀리서 아이들링하는 소리가 커지다가(<paramref name="lead"/>),
        /// 차가 화면을 지나가는 동안(<paramref name="drive"/>) 회전수가 오르고 가까워질수록 커졌다가 도플러로 음이 내려가며 멀어지고, 꼬리가 잦아든다.
        /// 4기통 배기 펀치(짧게 감쇠하는 저음 펄스 열)에 실린더마다 세기를 조금씩 달리한 울퉁불퉁함과 잔 지직임을 더해 낮게 깐다.
        /// </summary>
        private static AudioClip BuildCarEngine(float lead, float drive)
        {
            const float tail = 0.5f;
            var seconds = lead + drive + tail;
            var count = Mathf.RoundToInt(SampleRate * seconds);
            var samples = new float[count];
            var random = new System.Random(20260927);

            var phase = 0f;
            var cylinder = 1f;
            var fast = 0f;
            var boom = 0f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var p = Mathf.Clamp01((t - lead) / drive); // 주행 진행도(0 = 등장, 0.5 = 정면, 1 = 사라짐).

                // 펀치 빈도(초당): 아이들 → 가속. 지나가는 순간(p≈0.5)에 음이 내려간다(도플러).
                var rpm = t < lead ? Mathf.Lerp(24f, 28f, t / lead) : Mathf.Lerp(28f, 44f, p);
                var doppler = 1f + 0.07f * (1f - 2f * Mathf.SmoothStep(0f, 1f, p));
                var frequency = rpm * doppler;

                phase += frequency / SampleRate;
                if (phase >= 1f)
                {
                    phase -= 1f;
                    cylinder = 0.75f + (float)random.NextDouble() * 0.5f; // 실린더마다 세기가 조금씩 다르다.
                }

                var cycle = phase;
                var pulse = Mathf.Exp(-7f * cycle) - 0.143f;
                var crackle = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-30f * cycle) * 0.35f;
                var raw = (pulse + crackle) * cylinder;

                // 두 단 저역 통과: 배기 펀치의 몸통(fast)과 깔리는 저음(boom).
                fast += 0.14f * (raw - fast);
                boom += 0.03f * (fast - boom);
                var wave = fast * 0.75f + boom * 2.4f;

                // 크기: 멀리서 커지다가(lead) → 지나가며 절정(p=0.5) → 멀어지고 꼬리가 잦아든다.
                float level;
                if (t < lead)
                {
                    var k = t / lead;
                    level = 0.12f + 0.38f * k * k;
                }
                else if (t < lead + drive)
                {
                    var closeness = 1f - Mathf.Abs(1f - 2f * p);
                    level = 0.5f + 0.5f * closeness * closeness;
                    if (p > 0.5f) level *= Mathf.Lerp(1f, 0.55f, (p - 0.5f) * 2f);
                }
                else
                {
                    var k = (t - lead - drive) / tail;
                    level = 0.55f * 0.55f * (1f - k) * (1f - k);
                }

                samples[i] = wave * level;
            }

            // 안 클리핑되도록 최대값에 맞추고, 처음과 끝을 부드럽게.
            var peak = 0.0001f;
            foreach (var s in samples) peak = Mathf.Max(peak, Mathf.Abs(s));
            var gain = 0.9f / peak;
            var fade = SampleRate / 50;
            for (var i = 0; i < count; i++)
            {
                var edge = Mathf.Min(1f, Mathf.Min(i, count - 1 - i) / (float)fade);
                samples[i] *= gain * edge;
            }

            var clip = AudioClip.Create("CarEngine (procedural)", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
