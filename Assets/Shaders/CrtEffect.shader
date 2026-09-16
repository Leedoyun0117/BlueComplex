Shader "BlueComplex/CRT/PostProcess"
{
    Properties
    {
        _ScanIntensity ("Scanline Intensity", Range(0, 1)) = 0.35
        _ScanCount ("Scanline Count", Range(100, 900)) = 420
        _ScanThickness ("Scanline Thickness", Range(0.2, 4)) = 1.0
        _ScanSpeed ("Scanline Speed", Range(0, 2)) = 0
        _Curvature ("Barrel Curvature", Range(0, 0.6)) = 0.18
        _Vignette ("Vignette", Range(0, 1.5)) = 0.55
        _Bloom ("Bloom", Range(0, 1)) = 0.30
        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.002
        _Noise ("Noise", Range(0, 0.4)) = 0.06
        _Flicker ("Flicker", Range(0, 0.3)) = 0.04
        _Shake ("Screen Shake", Range(0, 0.03)) = 0
        _TintR ("Depressed Red Tint", Range(0, 1)) = 0
        _Pastel ("Excited Pastel Shift", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0.3, 1.6)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "CRT"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Blit.hlsl references TEXTURE2D_X()/UnpackNormalOctQuadEncode()/_Time etc. from headers it doesn't
            // include itself, so pull them in first (same order as com.unity.render-pipelines.core's CoreCopy.shader).
            // UnityInput.hlsl has to come before Blit.hlsl: Blit.hlsl pulls in UnityInstancing.hlsl, which
            // #defines unity_StereoEyeIndex as a plain "0" outside of stereo builds, and UnityInput.hlsl
            // declares an actual "int unity_StereoEyeIndex;" variable — if that #define is already active,
            // the declaration expands to "int 0;" and fails to parse.
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _ScanIntensity;
                float _ScanCount;
                float _ScanThickness;
                float _ScanSpeed;
                float _Curvature;
                float _Vignette;
                float _Bloom;
                float _Aberration;
                float _Noise;
                float _Flicker;
                float _Shake;
                float _TintR;
                float _Pastel;
                float _Brightness;
            CBUFFER_END

            float Rand(float2 c)
            {
                return frac(sin(dot(c, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 Barrel(float2 c)
            {
                float2 cc = c - 0.5;
                float d = dot(cc, cc);
                return c + cc * d * _Curvature * 1.4;
            }

            float3 Sample3(float2 c)
            {
                float2 off = (c - 0.5) * _Aberration;
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, c + off).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, c).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, c - off).b;
                return float3(r, g, b);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 res = _BlitTexture_TexelSize.zw;
                float time = _Time.y;
                float2 c = input.texcoord;

                // 화면 흔들림
                c.x += sin(time * 37.0) * _Shake * 0.5;
                c.y += cos(time * 23.0) * _Shake * 0.35;

                // 배럴 왜곡
                c = Barrel(c);

                // 화면 밖 클리핑
                if (c.x < 0.0 || c.x > 1.0 || c.y < 0.0 || c.y > 1.0)
                    return float4(0, 0, 0, 1);

                // 색수차
                float3 col = Sample3(c);

                // 블룸
                float3 acc = float3(0, 0, 0);
                float px = 1.6 * _BlitTexture_TexelSize.x;
                float py = 1.6 * _BlitTexture_TexelSize.y;
                [unroll]
                for (int i = -2; i <= 2; i++)
                {
                    [unroll]
                    for (int j = -2; j <= 2; j++)
                    {
                        float3 s = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, c + float2(i * px, j * py)).rgb;
                        acc += max(s - 0.45, 0.0);
                    }
                }
                col += acc / 25.0 * _Bloom * 5.5;

                // 스캔라인 (두께: sin파를 지수로 눌러서 어두운 대역의 폭을 넓힘)
                float sl = sin((c.y + time * _ScanSpeed * 0.05) * _ScanCount * 3.14159);
                float scanWave = pow(saturate(0.5 + 0.5 * sl), 1.0 / max(_ScanThickness, 0.05));
                col *= 1.0 - _ScanIntensity * scanWave;

                // 섀도우 마스크
                float m = fmod(input.positionCS.x, 3.0);
                float3 mask = (m < 1.0) ? float3(1.06, 0.96, 0.96)
                            : (m < 2.0) ? float3(0.96, 1.06, 0.96)
                                        : float3(0.96, 0.96, 1.06);
                col *= mask;

                // 파스텔 시프트 (흥분)
                float3 warp = float3(col.r * 1.10 + col.b * 0.16,
                                      col.g * 1.02 + col.r * 0.12,
                                      col.b * 1.14 + col.g * 0.10);
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                warp = lerp(warp, warp + float3(0.22, 0.10, 0.26) * lum, 0.6);
                col = lerp(col, warp, _Pastel);

                // 붉은 틴트 (침체)
                float lum2 = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, float3(lum2 * 1.32, lum2 * 0.38, lum2 * 0.34), _TintR);

                // 노이즈
                col += (Rand(c * res + time) - 0.5) * _Noise;

                // 깜빡임
                col *= 1.0 - _Flicker * (0.5 + 0.5 * sin(time * 48.0));

                // 비네트
                float2 vc = c - 0.5;
                col *= 1.0 - _Vignette * dot(vc, vc) * 2.1;

                // 밝기
                col *= _Brightness;

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
