using UnityEngine;

namespace BlueComplex.UI.Presentation
{
    /// <summary>
    /// 중립 / 침체 최대치 / 흥분 최대치 세 지점의 CRT 파라미터.
    /// CrtEffectDriver는 인디케이터 위치를 -1(침체 끝)~0(중앙)~1(흥분 끝)로 정규화한 뒤 이 프리셋에서 보간값을 얻는다.
    /// </summary>
    [CreateAssetMenu(fileName = "CrtEffectPreset", menuName = "BlueComplex/UI/CRT Effect Preset")]
    public sealed class CrtEffectPreset : ScriptableObject
    {
        [Header("중앙 (기본값)")]
        public CrtParams Neutral = CrtParams.Default;

        [Header("침체 5칸 (왼쪽 끝)")]
        public CrtParams DepressedMax = new CrtParams
        {
            ScanIntensity = 0.45f,
            ScanCount = 360f,
            ScanSpeed = 0.15f,
            Curvature = 0.24f,
            Vignette = 1.05f,
            Bloom = 0.18f,
            Aberration = 0.004f,
            Noise = 0.14f,
            Flicker = 0.10f,
            Shake = 0.004f,
            TintR = 0.55f,
            Pastel = 0f,
            Brightness = 0.62f,
        };

        [Header("흥분 5칸 (오른쪽 끝)")]
        public CrtParams ExcitedMax = new CrtParams
        {
            ScanIntensity = 0.28f,
            ScanCount = 500f,
            ScanSpeed = 0.8f,
            Curvature = 0.20f,
            Vignette = 0.40f,
            Bloom = 0.62f,
            Aberration = 0.012f,
            Noise = 0.09f,
            Flicker = 0.07f,
            Shake = 0.012f,
            TintR = 0f,
            Pastel = 0.75f,
            Brightness = 1.22f,
        };

        /// <summary>t: -1(침체 최대) ~ 0(중앙) ~ 1(흥분 최대).</summary>
        public CrtParams Evaluate(float t)
        {
            t = Mathf.Clamp(t, -1f, 1f);
            return t < 0f
                ? CrtParams.Lerp(Neutral, DepressedMax, -t)
                : CrtParams.Lerp(Neutral, ExcitedMax, t);
        }
    }
}
