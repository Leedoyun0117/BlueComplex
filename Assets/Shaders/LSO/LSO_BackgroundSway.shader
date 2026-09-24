Shader "BlueComplex/LSO/BackgroundSway"
{
    // BlueComplex/Background/Layer 에 바람 흔들림만 얹은 변종. 조명·그림자·알파 보정은 원본과 완전히 같다
    // (원본을 고치지 않으려고 통째로 복제했다 — 원본은 LDY 담당이고 배경 전 레이어가 쓰고 있다).
    //
    // 흔들림은 정점에서 준다:
    //  - 뿌리(아래)는 고정이고 위로 갈수록 크게 흔들린다. 쿼드 UV의 v를 "높이"로 쓴다(v=0 아래, v=1 위).
    //    _SwayFalloff 를 올리면 아래쪽이 더 뻣뻣해져서 줄기 느낌이 난다.
    //  - 사인 두 개를 섞어 주기가 딱 떨어지지 않게 한다. 하나만 쓰면 기계처럼 왔다갔다 한다.
    //  - 오브젝트 원점을 위상에 섞어, 같은 머티리얼을 쓰는 화분이 여럿이어도 한 박자로 흔들리지 않게 한다.
    //  - 기울어진 만큼 끝을 살짝 내려 길이가 늘어나 보이는 걸 막는다(원호 보정).
    //
    // _WindStrength 는 오브젝트 로컬 단위다. 배경 판이 보통 1×1 쿼드라 "폭 대비 비율"로 읽으면 된다
    // (0.03 = 폭의 3%). 판을 크게 키우면 흔들림도 같이 커진다 — 큰 식물이 더 크게 흔들리는 게 자연스럽다.
    //
    // 그림자 패스에도 같은 흔들림을 넣는다. 안 그러면 그림자만 제자리에 서 있어서 바로 눈에 띈다.
    // 두 패스의 CBUFFER 는 순서까지 똑같아야 한다(SRP Batcher 요구사항) — 프로퍼티를 추가할 땐 양쪽 다 고칠 것.
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
        _WindStrength ("Wind Strength (quad width ratio)", Range(0, 0.3)) = 0.03
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.2
        _WindTurbulence ("Wind Turbulence (second wave)", Range(0, 1)) = 0.4
        _SwayFalloff ("Root Stiffness", Range(0.5, 8)) = 2.5
        _WindPhase ("Per-Object Phase Spread", Range(0, 5)) = 1
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
            Name "BackgroundSway"
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
                half _WindTurbulence;
                half _SwayFalloff;
                half _WindPhase;
            CBUFFER_END

            // 두 패스가 같은 식을 써야 그림자가 본체를 따라온다. 고칠 땐 아래 그림자 패스의 같은 함수도 같이 고칠 것.
            float3 ApplyWindSway(float3 positionOS, float2 uv)
            {
                float weight = pow(saturate(uv.y), _SwayFalloff);

                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float phase = _Time.y * _WindSpeed + (originWS.x * 0.7 + originWS.y * 0.3) * _WindPhase;

                float wave = (sin(phase) + sin(phase * 1.73 + 1.3) * _WindTurbulence) / (1.0 + _WindTurbulence);
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
                half _WindTurbulence;
                half _SwayFalloff;
                half _WindPhase;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            // 본체 패스와 같은 식 — 그림자가 흔들림을 따라오게 한다. 한쪽만 고치면 그림자가 제자리에 남는다.
            float3 ApplyWindSway(float3 positionOS, float2 uv)
            {
                float weight = pow(saturate(uv.y), _SwayFalloff);

                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float phase = _Time.y * _WindSpeed + (originWS.x * 0.7 + originWS.y * 0.3) * _WindPhase;

                float wave = (sin(phase) + sin(phase * 1.73 + 1.3) * _WindTurbulence) / (1.0 + _WindTurbulence);
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
