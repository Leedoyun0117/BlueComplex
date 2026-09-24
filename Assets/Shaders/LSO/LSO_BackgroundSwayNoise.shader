Shader "BlueComplex/LSO/BackgroundSwayNoise"
{
    // BlueComplex/LSO/BackgroundSway 의 노이즈 텍스처 버전.
    //
    // 사인만 쓰는 쪽은 아무리 섞어도 "왔다 갔다" 하는 규칙성이 남는다. 이 버전은 흘러가는 노이즈를
    // 읽어서 바람이 불규칙하게 몰아치는(gust) 느낌을 낸다.
    //  - 노이즈는 정점의 월드 좌표로 샘플한다. 옆에 선 포기끼리 다른 값을 읽어 자연히 어긋나고,
    //    한 포기 안에서도 위아래가 미묘하게 달라 결이 타고 올라간다(_NoiseScale 로 조절).
    //  - 노이즈 UV를 시간으로 가로로 흘려 바람이 지나가는 것처럼 만든다.
    //  - _NoiseInfluence 로 사인과 섞는다. 0이면 LSO_BackgroundSway 와 같고, 1이면 순수 노이즈다.
    //    사인을 조금 남겨두면 노이즈가 평평한 구간에서도 완전히 멈추지 않는다.
    //
    // 노이즈 텍스처는 반드시 물려야 한다. 안 물리면 기본값 "gray"(0.5)가 잡혀 노이즈 기여가 0이 되고,
    // _NoiseInfluence 가 1이면 아예 안 움직인다(기본값 0.75라 사인 몫만 조금 남는다).
    // 텍스처 임포트 설정 세 가지를 맞출 것 — 어긋나면 흔들림이 어색해진다:
    //   Wrap Mode = Repeat   (월드 좌표로 샘플하므로 계속 반복된다. Clamp면 멀리서 값이 고정된다)
    //   sRGB (Color Texture) = 해제   (밝기 값을 그대로 써야 한다. 켜두면 감마 변환이 껴서 치우친다)
    //   Filter Mode = Bilinear   (Point면 흔들림이 계단처럼 툭툭 끊긴다)
    // 이음매 없이 반복되는(타일링) 회색조 노이즈여야 경계가 안 보인다.
    //
    // 그 밖의 것(조명·그림자·알파 보정)은 BlueComplex/Background/Layer 와 완전히 같다.
    // 두 패스의 CBUFFER 는 순서까지 똑같아야 한다(SRP Batcher) — 프로퍼티를 추가할 땐 양쪽 다 고칠 것.
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _AlphaPower ("Alpha Power (linear-blend fix)", Range(0.5, 3)) = 1.8
        _LightGain ("Light Gain", Range(0, 4)) = 1
        _LightClamp ("Light Clamp (max added fraction)", Range(0, 2)) = 0.6
        _SurfaceGlow ("Surface Glow (linear, added by light)", Range(0, 0.5)) = 0
        _ShadowAlphaCutoff ("Shadow Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Wind)]
        _WindStrength ("Wind Strength (quad width ratio)", Range(0, 0.3)) = 0.035
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.2
        _SwayFalloff ("Root Stiffness", Range(0.5, 8)) = 2.5
        _WindPhase ("Per-Object Phase Spread", Range(0, 5)) = 1

        [Header(Noise)]
        [NoScaleOffset] _NoiseTex ("Wind Noise (R channel)", 2D) = "gray" {}
        _NoiseInfluence ("Noise Influence (0 = pure sine)", Range(0, 1)) = 0.75
        _NoiseScale ("Noise Scale (world units)", Range(0.01, 5)) = 0.35
        _NoiseScroll ("Noise Scroll Speed", Range(0, 2)) = 0.35
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
            Name "BackgroundSwayNoise"
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _AlphaPower;
                half _LightGain;
                half _LightClamp;
                half _SurfaceGlow;
                half _ShadowAlphaCutoff;
                half _WindStrength;
                half _WindSpeed;
                half _SwayFalloff;
                half _WindPhase;
                half _NoiseInfluence;
                half _NoiseScale;
                half _NoiseScroll;
            CBUFFER_END

            // 두 패스가 같은 식을 써야 그림자가 본체를 따라온다. 고칠 땐 아래 그림자 패스의 같은 함수도 같이 고칠 것.
            float3 ApplyWindSway(float3 positionOS, float2 uv)
            {
                float weight = pow(saturate(uv.y), _SwayFalloff);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);

                // 정점 셰이더에는 화면 미분값이 없어 밉을 고를 수 없다 — LOD 0으로 고정해 샘플한다.
                float2 noiseUV = positionWS.xy * _NoiseScale - float2(_Time.y * _NoiseScroll, 0.0);
                float gust = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, noiseUV, 0).r * 2.0 - 1.0;

                float phase = _Time.y * _WindSpeed + (originWS.x * 0.7 + originWS.y * 0.3) * _WindPhase;
                float wave = lerp(sin(phase), gust, _NoiseInfluence);

                float offset = wave * _WindStrength * weight;
                positionOS.x += offset;
                positionOS.y -= abs(offset) * 0.35; // 기울어진 만큼 끝이 내려앉는다(길이 보정).
                return positionOS;
            }

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
                float3 swayedOS = ApplyWindSway(input.positionOS.xyz, input.uv);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(swayedOS);
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

                half3 lightSum = 0;

                float4 mainShadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(mainShadowCoord);
                lightSum += mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light dirLight = GetAdditionalLight(dirIndex, input.positionWS, half4(1, 1, 1, 1));
                        lightSum += dirLight.color * dirLight.distanceAttenuation * dirLight.shadowAttenuation;
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        lightSum += light.color * light.distanceAttenuation * light.shadowAttenuation;
                    LIGHT_LOOP_END
                #endif

                half3 boost = min(lightSum * _LightGain, _LightClamp.xxx);
                return half4(art * (1.0h + boost) + boost * _SurfaceGlow, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _AlphaPower;
                half _LightGain;
                half _LightClamp;
                half _SurfaceGlow;
                half _ShadowAlphaCutoff;
                half _WindStrength;
                half _WindSpeed;
                half _SwayFalloff;
                half _WindPhase;
                half _NoiseInfluence;
                half _NoiseScale;
                half _NoiseScroll;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            // 본체 패스와 같은 식 — 그림자가 흔들림을 따라오게 한다. 한쪽만 고치면 그림자가 제자리에 남는다.
            float3 ApplyWindSway(float3 positionOS, float2 uv)
            {
                float weight = pow(saturate(uv.y), _SwayFalloff);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);

                float2 noiseUV = positionWS.xy * _NoiseScale - float2(_Time.y * _NoiseScroll, 0.0);
                float gust = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, noiseUV, 0).r * 2.0 - 1.0;

                float phase = _Time.y * _WindSpeed + (originWS.x * 0.7 + originWS.y * 0.3) * _WindPhase;
                float wave = lerp(sin(phase), gust, _NoiseInfluence);

                float offset = wave * _WindStrength * weight;
                positionOS.x += offset;
                positionOS.y -= abs(offset) * 0.35;
                return positionOS;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                float3 swayedOS = ApplyWindSway(input.positionOS.xyz, input.uv);
                float3 positionWS = TransformObjectToWorld(swayedOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, float3(0, 0, 0), lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half texAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                half alpha = pow(saturate(texAlpha), _AlphaPower);
                clip(alpha - _ShadowAlphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "BlueComplex/Background/Layer"
}
