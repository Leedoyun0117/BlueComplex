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

        [Header(Per Blade)]
        _BladeStrength ("Per-Blade Sway X (UV units)", Range(0, 0.08)) = 0.015
        _BladeLift ("Per-Blade Sway Y (UV units)", Range(0, 0.06)) = 0.008
        _BladeVariation ("Per-Blade Bands (roughly blade count)", Range(1, 60)) = 18
        _BladeRandom ("Per-Blade Randomness", Range(0, 1)) = 1

        [Header(Noise)]
        [NoScaleOffset] _NoiseTex ("Wind Noise (R channel)", 2D) = "gray" {}
        _NoiseInfluence ("Gust Influence", Range(0, 1)) = 0.75
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
                half _BladeStrength;
                half _BladeLift;
                half _BladeVariation;
                half _BladeRandom;
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

            // 잎 하나하나가 따로 흔들리게 한다.
            //
            // 쿼드는 정점이 4개뿐이라 정점만 밀어서는 판 전체가 한 덩어리로 기울 수밖에 없다 — 그림 안에 잎이
            // 여러 개여도 뭉쳐서 움직인다. 그래서 정점이 아니라 "텍스처에서 읽어오는 자리"를 픽셀마다 다르게 민다.
            // 가로를 _BladeVariation 개의 칸으로 나누고 칸마다 난수로 위상·진폭·속도를 흩는다. 대략 잎 개수에
            // 맞추면 잎 하나가 칸 하나를 차지해 제각기 논다. _BladeRandom 을 0으로 내리면 난수를 끄고
            // 가로 위치에 비례하는 규칙적인 위상으로 돌아간다(잔물결처럼 한 방향으로 쓸린다).
            //
            // 가로(_BladeStrength)와 세로(_BladeLift)를 따로 준다. 세로는 위상과 속도를 어긋나게 둬서
            // 잎 끝이 직선이 아니라 타원을 그리며 돈다 — 가로만 있으면 좌우로 쓸리기만 해서 뻣뻣해 보인다.
            // 세로는 가로보다 작게 주는 게 자연스럽다(잎은 옆으로 쓸리지 위아래로 튀지 않는다).
            //
            // 이건 그림을 휘는 것이라 세게 주면 잎이 늘어져 번진다. 둘 다 UV 단위(폭·높이 대비 비율)로,
            // 가로는 0.02, 세로는 0.015 를 넘기면 티가 나기 시작한다.
            // 큰 움직임은 정점 쪽(_WindStrength)에 맡기고 여기선 결만 준다.
            //
            // 두 패스가 같은 식을 써야 그림자 실루엣이 본체와 맞는다 — 그림자 패스의 같은 함수도 같이 고칠 것.
            float LSO_Hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            // 1D 밸류 노이즈 — 정수 칸마다 임의값을 하나씩 뽑고 그 사이를 부드럽게 잇는다.
            // 칸 값을 그대로 쓰면(계단) 칸 경계에서 잎이 좌우로 찢어지므로 반드시 부드럽게 이어야 한다.
            // 텍스처가 필요 없어 노이즈 맵을 안 물려도 잎별 랜덤이 동작한다.
            float LSO_ValueNoise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(LSO_Hash11(i), LSO_Hash11(i + 1.0), f);
            }

            float2 WarpBladeUV(float2 uv)
            {
                float weight = pow(saturate(uv.y), _SwayFalloff);

                // 가로를 _BladeVariation 개의 칸으로 나누고 칸마다 다른 난수를 뽑는다.
                // 오프셋(37.7, 91.3)은 세 값이 서로 상관되지 않게 띄워 둔 것이다 — 같은 자리를 읽으면 셋이 함께 움직인다.
                float bands = uv.x * _BladeVariation;
                float rPhase = LSO_ValueNoise1D(bands);
                float rAmp   = LSO_ValueNoise1D(bands + 37.7);
                float rSpeed = LSO_ValueNoise1D(bands + 91.3);

                // _BladeRandom 0 이면 예전처럼 가로 위치에 비례하는 규칙적 위상, 1 이면 칸마다 임의 위상.
                float seed = lerp(uv.x * _BladeVariation, rPhase * 6.2831853, _BladeRandom);

                // 속도까지 흩어야 잎들이 영영 다시 맞아떨어지지 않는다 — 위상만 다르면 주기가 같아 언젠가 다시 겹친다.
                float speedJitter = lerp(1.0, lerp(0.75, 1.30, rSpeed), _BladeRandom);
                float ampJitter = lerp(1.0, lerp(0.55, 1.45, rAmp), _BladeRandom);

                float t = _Time.y * _WindSpeed * speedJitter;

                float waveX = sin(t + seed) * 0.65 + sin(t * 1.37 + seed * 1.9 + 2.1) * 0.35;

                // 세로는 가로와 1/4 주기(pi/2) 어긋나게 두고 속도도 다르게 준다 — 위상이 같으면 대각선으로만
                // 오가서 결국 직선 운동이라 뻣뻣해 보인다. 어긋나야 잎 끝이 타원을 그리며 돌아 살아 있어 보인다.
                float waveY = sin(t * 1.13 + seed * 1.27 + 1.5707963) * 0.7 + sin(t * 0.61 + seed * 0.8) * 0.3;

                // 돌풍 — 노이즈가 진폭을 키웠다 줄였다 한다.
                // 노이즈를 안 물리면 기본값 gray(0.5) → 배율이 정확히 1이라 아무 영향이 없다(잎별 흔들림은 그대로 동작한다).
                float gust = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                    float2(uv.x * _NoiseScale - _Time.y * _NoiseScroll, 0.5)).r;
                float gustScale = lerp(1.0, gust * 2.0, _NoiseInfluence) * ampJitter;
                waveX *= gustScale;
                waveY *= gustScale;

                uv.x += waveX * _BladeStrength * weight;
                uv.y += waveY * _BladeLift * weight;
                return uv;
            }

            // 휘어서 텍스처 밖을 읽으면 가장자리 픽셀이 옆으로 늘어져 번진다 — 밖이면 비운다.
            // _BaseMap 의 Tiling 1,1 / Offset 0,0 을 전제로 한다(배경 레이어는 전부 그렇다).
            half InsideBaseMap(float2 uv)
            {
                return (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0) ? 1.0h : 0.0h;
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
                float2 bladeUV = WarpBladeUV(input.uv);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, bladeUV) * _BaseColor;
                half3 art = tex.rgb;
                half alpha = pow(saturate(tex.a), _AlphaPower) * InsideBaseMap(bladeUV);

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
                half _BladeStrength;
                half _BladeLift;
                half _BladeVariation;
                half _BladeRandom;
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

            // 본체 패스와 같은 식 — 그림자 실루엣이 잎별 흔들림을 따라온다. 한쪽만 고치면 그림자 모양이 어긋난다.
            float LSO_Hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            // 1D 밸류 노이즈 — 정수 칸마다 임의값을 하나씩 뽑고 그 사이를 부드럽게 잇는다.
            // 칸 값을 그대로 쓰면(계단) 칸 경계에서 잎이 좌우로 찢어지므로 반드시 부드럽게 이어야 한다.
            // 텍스처가 필요 없어 노이즈 맵을 안 물려도 잎별 랜덤이 동작한다.
            float LSO_ValueNoise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(LSO_Hash11(i), LSO_Hash11(i + 1.0), f);
            }

            float2 WarpBladeUV(float2 uv)
            {
                float weight = pow(saturate(uv.y), _SwayFalloff);

                // 가로를 _BladeVariation 개의 칸으로 나누고 칸마다 다른 난수를 뽑는다.
                // 오프셋(37.7, 91.3)은 세 값이 서로 상관되지 않게 띄워 둔 것이다 — 같은 자리를 읽으면 셋이 함께 움직인다.
                float bands = uv.x * _BladeVariation;
                float rPhase = LSO_ValueNoise1D(bands);
                float rAmp   = LSO_ValueNoise1D(bands + 37.7);
                float rSpeed = LSO_ValueNoise1D(bands + 91.3);

                // _BladeRandom 0 이면 예전처럼 가로 위치에 비례하는 규칙적 위상, 1 이면 칸마다 임의 위상.
                float seed = lerp(uv.x * _BladeVariation, rPhase * 6.2831853, _BladeRandom);

                // 속도까지 흩어야 잎들이 영영 다시 맞아떨어지지 않는다 — 위상만 다르면 주기가 같아 언젠가 다시 겹친다.
                float speedJitter = lerp(1.0, lerp(0.75, 1.30, rSpeed), _BladeRandom);
                float ampJitter = lerp(1.0, lerp(0.55, 1.45, rAmp), _BladeRandom);

                float t = _Time.y * _WindSpeed * speedJitter;

                float waveX = sin(t + seed) * 0.65 + sin(t * 1.37 + seed * 1.9 + 2.1) * 0.35;

                // 세로는 가로와 1/4 주기(pi/2) 어긋나게 두고 속도도 다르게 준다 — 위상이 같으면 대각선으로만
                // 오가서 결국 직선 운동이라 뻣뻣해 보인다. 어긋나야 잎 끝이 타원을 그리며 돌아 살아 있어 보인다.
                float waveY = sin(t * 1.13 + seed * 1.27 + 1.5707963) * 0.7 + sin(t * 0.61 + seed * 0.8) * 0.3;

                float gust = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                    float2(uv.x * _NoiseScale - _Time.y * _NoiseScroll, 0.5)).r;
                float gustScale = lerp(1.0, gust * 2.0, _NoiseInfluence) * ampJitter;
                waveX *= gustScale;
                waveY *= gustScale;

                uv.x += waveX * _BladeStrength * weight;
                uv.y += waveY * _BladeLift * weight;
                return uv;
            }

            half InsideBaseMap(float2 uv)
            {
                return (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0) ? 1.0h : 0.0h;
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
                float2 bladeUV = WarpBladeUV(input.uv);
                half texAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, bladeUV).a * _BaseColor.a;
                half alpha = pow(saturate(texAlpha), _AlphaPower) * InsideBaseMap(bladeUV);
                clip(alpha - _ShadowAlphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "BlueComplex/Background/Layer"
}
