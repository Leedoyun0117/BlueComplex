using UnityEngine;
using UnityEngine.Audio;

namespace BlueComplex.Audio
{
    /// <summary>
    /// 침체 시 BGM 그룹의 Lowpass Simple 컷오프를 낮춰 먹먹하게 만든다. 믹서의 exposed 파라미터 <see cref="CutoffParameter"/>만 만진다 —
    /// 채널 볼륨(<see cref="AudioSettingsController"/>의 *Volume 파라미터)과는 별개의 파라미터라 슬라이더·저장값과 겹치지 않는다.
    /// Ambient 그룹에는 영향이 없다.
    /// </summary>
    public static class BgmMuffle
    {
        public const string CutoffParameter = "BGMLowpassCutoff";

        /// <summary>먹먹함 0일 때의 컷오프(가청 대역 밖 — 사실상 통과).</summary>
        public const float OpenCutoffHz = 22000f;

        /// <summary>먹먹함 1(매우 침체)일 때의 컷오프.</summary>
        public const float MuffledCutoffHz = 600f;

        private static AudioMixer _mixer;
        private static float _lastHz = -1f;

        /// <summary>먹먹함 0~1 → 컷오프(Hz). 귀에는 로그 스케일로 들리므로 지수 보간한다.</summary>
        public static float CutoffFor(float amount)
        {
            var t = Mathf.Clamp01(amount);
            return OpenCutoffHz * Mathf.Pow(MuffledCutoffHz / OpenCutoffHz, t);
        }

        /// <summary>먹먹함(0=정상, 1=최대)을 믹서에 적용한다. 값이 바뀌지 않으면 아무것도 하지 않는다.</summary>
        public static void Apply(float amount)
        {
            var hz = CutoffFor(amount);
            if (Mathf.Abs(hz - _lastHz) < 0.5f) return;

            if (_mixer == null) _mixer = Resources.Load<AudioMixer>(AudioSettingsController.DefaultMixerResource);
            if (_mixer == null) return;

            if (_mixer.SetFloat(CutoffParameter, hz)) _lastHz = hz;
            else Debug.LogWarning($"[BgmMuffle] 믹서에 exposed 파라미터 '{CutoffParameter}'이 없다.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _mixer = null;
            _lastHz = -1f;
        }
    }
}
