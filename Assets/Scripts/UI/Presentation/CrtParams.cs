using System;

namespace BlueComplex.UI.Presentation
{
    /// <summary>CRT 셰이더의 한 순간의 파라미터 값 묶음. 보간과 머티리얼 적용 양쪽에서 쓰인다.</summary>
    [Serializable]
    public struct CrtParams
    {
        public float ScanIntensity;
        public float ScanCount;
        public float ScanThickness;
        public float ScanSpeed;
        public float Curvature;
        public float Vignette;
        public float Bloom;
        public float Aberration;
        public float Noise;
        public float Flicker;
        public float Shake;
        public float TintR;
        public float Pastel;
        public float Brightness;

        public static CrtParams Lerp(CrtParams a, CrtParams b, float t)
        {
            return new CrtParams
            {
                ScanIntensity = UnityEngine.Mathf.Lerp(a.ScanIntensity, b.ScanIntensity, t),
                ScanCount = UnityEngine.Mathf.Lerp(a.ScanCount, b.ScanCount, t),
                ScanThickness = UnityEngine.Mathf.Lerp(a.ScanThickness, b.ScanThickness, t),
                ScanSpeed = UnityEngine.Mathf.Lerp(a.ScanSpeed, b.ScanSpeed, t),
                Curvature = UnityEngine.Mathf.Lerp(a.Curvature, b.Curvature, t),
                Vignette = UnityEngine.Mathf.Lerp(a.Vignette, b.Vignette, t),
                Bloom = UnityEngine.Mathf.Lerp(a.Bloom, b.Bloom, t),
                Aberration = UnityEngine.Mathf.Lerp(a.Aberration, b.Aberration, t),
                Noise = UnityEngine.Mathf.Lerp(a.Noise, b.Noise, t),
                Flicker = UnityEngine.Mathf.Lerp(a.Flicker, b.Flicker, t),
                Shake = UnityEngine.Mathf.Lerp(a.Shake, b.Shake, t),
                TintR = UnityEngine.Mathf.Lerp(a.TintR, b.TintR, t),
                Pastel = UnityEngine.Mathf.Lerp(a.Pastel, b.Pastel, t),
                Brightness = UnityEngine.Mathf.Lerp(a.Brightness, b.Brightness, t),
            };
        }

        public static CrtParams Default => new CrtParams
        {
            ScanIntensity = 0.35f,
            ScanCount = 420f,
            ScanThickness = 1.0f,
            ScanSpeed = 0f,
            Curvature = 0.18f,
            Vignette = 0.55f,
            Bloom = 0.30f,
            Aberration = 0.002f,
            Noise = 0.06f,
            Flicker = 0.04f,
            Shake = 0f,
            TintR = 0f,
            Pastel = 0f,
            Brightness = 1.0f,
        };
    }
}
