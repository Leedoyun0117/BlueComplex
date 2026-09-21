Shader "BlueComplex/Background/Layer"
{
    // 도트 아트 배경 레이어용 셰이더 (Unlit 베이스 + 조명 "가산 보강").
    //
    // 아트에 이미 빛(램프 글로우, 창문 빛줄기, 어두운 비네트)이 그려져 있으므로 일반 Lit처럼
    // 알베도 × 조명 으로 다시 칠하면 이중 노출이 되어 도트가 뭉개진다. 그래서 조명이 꺼진 상태의
    // 결과는 아트 그대로이고, 조명은 그 위에 "아트색 × (1 + 조명)" 으로 미세하게 얹힌다.
    //  - 조명은 노멀을 쓰지 않는다(납작한 도트 판은 노멀이 의미 없고, 램프보다 카메라 쪽에 있는
    //    인물/테이블 판에서 NdotL 이 음수가 되어 조명이 사라지는 문제를 피한다). 3D 거리 감쇠만 쓴다.
    //  - _LightClamp 로 조명 기여의 상한을 걸어 아트가 하얗게 날아가지 않게 한다.
    //
    // _AlphaPower: 프로젝트가 Linear 컬러스페이스라 하드웨어 알파 블렌딩이 선형 공간에서 일어나는데,
    // 아트는 sRGB(감마) 공간에서 합성돼 있다. 반투명 가장자리/글로우가 완성본보다 밝아지는 것을
    // 알파를 거듭제곱해 보정한다. (레이어 합성 실측: 1.0 → 완성본 대비 +4.6% 밝음, 1.8 → -0.3%)
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _AlphaPower ("Alpha Power (linear-blend fix)", Range(0.5, 3)) = 1.8
        _LightGain ("Light Gain", Range(0, 4)) = 1
        _LightClamp ("Light Clamp (max added fraction)", Range(0, 2)) = 0.6
        // 아트색과 무관하게 조명 색으로 더하는 양(선형). 거의 검정인 벽처럼 "아트색 × 조명"으로는 밝아지지 않는 면을 빛이
        // 비추는 만큼만 살짝 들어 올린다. 0이면 순수 가산 보강만 한다.
        _SurfaceGlow ("Surface Glow (linear, added by light)", Range(0, 0.5)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BackgroundLayer"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _AlphaPower;
                half _LightGain;
                half _LightClamp;
                half _SurfaceGlow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 art = tex.rgb;
                half alpha = pow(saturate(tex.a), _AlphaPower);

                // 조명 기여(노멀 없음, 감쇠 × 색). 셰도우 없음.
                half3 lightSum = 0;

                Light mainLight = GetMainLight();
                lightSum += mainLight.color * mainLight.distanceAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                    // LIGHT_LOOP_BEGIN 이 클러스터 루프에서 참조하는 필드만 채운다.
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light dirLight = GetAdditionalLight(dirIndex, input.positionWS);
                        lightSum += dirLight.color * dirLight.distanceAttenuation;
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS);
                        lightSum += light.color * light.distanceAttenuation;
                    LIGHT_LOOP_END
                #endif

                half3 boost = min(lightSum * _LightGain, _LightClamp.xxx);
                return half4(art * (1.0h + boost) + boost * _SurfaceGlow, alpha);
            }
            ENDHLSL
        }
    }
}
