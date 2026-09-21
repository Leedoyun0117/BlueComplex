Shader "BlueComplex/CRT/PostProcess"
{
    Properties
    {
        _ScanIntensity ("Scanline Intensity", Range(0, 1)) = 0.35
        _ScanCount ("Scanline Count", Range(100, 900)) = 420
        _ScanThickness ("Scanline Thickness", Range(0.2, 4)) = 1.0
        _ScanSpeed ("Scanline Speed", Range(0, 2)) = 0
        _Curvature ("Barrel Curvature", Range(0, 0.6)) = 0.12
        _Vignette ("Vignette", Range(0, 1.5)) = 0.55
        _Bloom ("Bloom", Range(0, 1)) = 0.30
        // 블룸이 시작되는 밝기(선형). 배경 라이팅이 들어온 뒤 전구/창문 같은 하이라이트에만 걸리도록 머티리얼에서 올린다.
        // 프리셋(CrtParams)이 다루는 값이 아니라 심박수로 변하지 않는다. 기본값 0.45는 기존 동작과 같다.
        _BloomThreshold ("Bloom Threshold", Range(0, 2)) = 0.45
        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.002
        _Noise ("Noise", Range(0, 0.4)) = 0.06
        _Flicker ("Flicker", Range(0, 0.3)) = 0.04
        _Shake ("Screen Shake", Range(0, 0.03)) = 0
        _TintR ("Depressed Red Tint", Range(0, 1)) = 0
        _Pastel ("Excited Pastel Shift", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0.3, 1.6)) = 1.0

        // UI 합성용. 별도 패스(UIComposite)로 CRT 앞에 체이닝했더니 같은 injectionPoint라도
        // 두 FullScreenPassRendererFeature를 연달아 돌리는 과정에서 두 번째(CRT) 패스의 좌표계가
        // 깨져 화면 전체가 배럴 클리핑으로 새까맣게 나오는 문제가 있었다 — 그래서 한 패스로 합쳤다.
        // RT_UI를 에디터에서 이 슬롯에 직접 할당한다(전역 텍스처 아님).
        _UITex ("UI Texture", 2D) = "black" {}
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

            // URP의 Core.hlsl를 먼저 포함한다(URP 자체 CoreBlit.shader와 같은 방식). Blit.hlsl이 쓰는 TEXTURE2D_X()/_Time/unity_StereoEyeIndex 등이
            // 여기서 정의된다. 예전처럼 core 패키지의 TextureXR.hlsl을 직접 포함하면 D3D11에서 TEXTURE2D_X가 Texture2DArray로 풀려,
            // Render Graph가 일반 Texture2D로 바인딩한 _BlitTexture와 타입이 어긋나 Unity가 기본 더미(회색) 텍스처로 대체한다 —
            // 그러면 CRT가 씬을 전혀 읽지 못하고 화면 전체가 균일한 회색이 된다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_UITex);
            SAMPLER(sampler_UITex);

            CBUFFER_START(UnityPerMaterial)
                float _ScanIntensity;
                float _ScanCount;
                float _ScanThickness;
                float _ScanSpeed;
                float _Curvature;
                float _Vignette;
                float _Bloom;
                float _BloomThreshold;
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

            // 3D 씬(_BlitTexture) 위에 UI(_UITex)를 알파 오버 합성한 값을 돌려준다. Sample3()의
            // 색수차 오프셋 샘플링과 블룸 누적이 이 함수를 통해서만 _BlitTexture를 읽도록 해서,
            // 이하의 모든 CRT 로직(스캔라인/새도우마스크/파스텔/틴트/노이즈/비네트/밝기)은
            // 손대지 않은 원래 그대로 유지한다 — 입력만 "씬"에서 "씬+UI 합성 결과"로 바뀐다.
            float3 SampleComposited(float2 uv)
            {
                float3 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float4 ui = SAMPLE_TEXTURE2D(_UITex, sampler_UITex, uv);
                return lerp(scene, ui.rgb, ui.a);
            }

            float3 Sample3(float2 c)
            {
                float2 off = (c - 0.5) * _Aberration;
                float r = SampleComposited(c + off).r;
                float g = SampleComposited(c).g;
                float b = SampleComposited(c - off).b;
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
                        float3 s = SampleComposited(c + float2(i * px, j * py));
                        acc += max(s - _BloomThreshold, 0.0);
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
