Shader "BlueComplex/LSO/BackgroundFly"
{
    // 날벌레 레이어용. BlueComplex/Background/Layer 에 "탁탁 튀는 이동 + 제자리 떨림"을 얹었다.
    // 조명·그림자·알파 보정은 원본과 완전히 같다.
    //
    // 날벌레는 매끄럽게 떠다니지 않는다. 사인 곡선을 그대로 쓰면 반딧불이처럼 둥실둥실 부유해서
    // 정반대 인상이 된다. 그래서 시간을 칸칸이 잘라 칸마다 임의의 목표점을 새로 뽑고,
    // 칸 앞부분에서 거의 다 이동해 버린 뒤 남은 시간은 그 자리에 머무는 식으로 만든다.
    // 이동이 순간적이고 사이에 정지가 끼어야 "경박하게" 보인다.
    //  - _DartRate 는 초당 몇 번 방향을 꺾을지. 모든 개체가 같은 박자를 쓰되 시작 지점만 어긋나 있다.
    //  - _DartSpeed 는 실제 이동 속도이고 개체·구간과 무관하게 고정이다. 칸마다 목표점까지의 거리가
    //    다른데 그걸 같은 시간에 나눠 가면 속도가 들쭉날쭉해지므로, 거리에 비례한 시간만 움직이고
    //    먼저 도착하면 멈춰서 기다린다. 이 "도착 후 정지"가 툭툭 끊기는 인상을 만든다.
    //  - 그 위에 잘고 빠른 떨림(_Buzz)을 얹어 멈춰 있는 동안에도 가만히 있지 않게 한다.
    //
    // ─── _Seed 를 머티리얼마다 다르게 줄 것 ───────────────────────────────
    // 이 프로젝트의 날벌레는 Fly.png ~ Fly7.png 가 전부 같은 크기(460x230) 캔버스에 각자 위치가
    // 다르게 그려진 배경 레이어라, 쿼드 8장이 같은 자리에 겹쳐 놓이기 쉽다. 그러면 오브젝트 위치로
    // 시드를 잡아도 8장이 전부 같은 값이 되어 완전히 똑같이 움직인다("랜덤인데 다 같이 간다").
    // 그래서 시드를 머티리얼 프로퍼티로 뺐다 — Room2_Fly 부터 Room2_Fly 7 까지 0,1,2...7 로 다르게 준다.
    // 오브젝트 위치도 시드에 섞으므로, 쿼드를 따로 떨어뜨려 놓은 경우에도 서로 어긋난다.
    //
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

        [Header(Dart)]
        _Seed ("Seed (MUST differ per material)", Range(0, 64)) = 0
        _DartRate ("Direction Changes Per Second", Range(0.2, 12)) = 3
        _DartSpeed ("Move Speed (quad ratio per sec)", Range(0, 1)) = 0.12
        _DartAspectY ("Vertical Ratio", Range(0, 2)) = 0.8

        [Header(Orbit)]
        _OrbitAmount ("Orbit Amount (0 = wander, 1 = circle light)", Range(0, 1)) = 0
        _OrbitCenter ("Light Offset From Fly (quad ratio XY)", Vector) = (0.12, 0.06, 0, 0)
        _OrbitSpeed ("Orbit Speed (quad ratio per sec)", Range(0, 1)) = 0.12
        _OrbitWobble ("Radius Wobble", Range(0, 0.8)) = 0.25
        _OrbitWobbleRate ("Wobble Per Lap", Range(1, 12)) = 3

        [Header(Buzz)]
        _BuzzAmount ("Buzz Amount (quad ratio)", Range(0, 0.02)) = 0.004
        _BuzzSpeed ("Buzz Hz", Range(0, 20)) = 3.5
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
            Name "BackgroundFly"
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
                half _Seed;
                half _DartRate;
                half _DartSpeed;
                half _DartAspectY;
                float4 _OrbitCenter;
                half _OrbitAmount;
                half _OrbitSpeed;
                half _OrbitWobble;
                half _OrbitWobbleRate;
                half _BuzzAmount;
                half _BuzzSpeed;
            CBUFFER_END

            // 경로 한 바퀴를 이루는 칸 수. 이게 곧 루프 길이다 — 한 바퀴에 LSO_FLY_STEPS / _DartRate 초 걸린다.
            #define LSO_FLY_STEPS 32

            float LSO_Hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            // 머티리얼 시드 + 오브젝트 위치. 겹쳐 놓았으면 _Seed 가, 떨어뜨려 놓았으면 위치가 구분해 준다.
            float FlySeed()
            {
                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                return _Seed * 7.13 + originWS.x * 3.7 + originWS.y * 2.3;
            }

            // 두 패스가 같은 식을 써야 그림자가 본체를 따라온다 — 그림자 패스의 같은 함수도 같이 고칠 것.
            // 닫힌 경로의 k번째 꼭짓점. 무작위 방향의 걸음을 k번 더하되, 전체 이동량의 k/N 만큼을 빼서
            // N번째가 0번째와 정확히 같아지게 만든다 — 한 바퀴가 끝나면 이음매 없이 처음으로 이어진다.
            // 걸음 길이가 전부 같으므로 구간마다 속도가 달라지지 않는다.
            //
            // 인덱스를 N 안에서만 돌리는 것이 중요하다. 시간에 비례해 계속 커지는 값을 해시에 넣으면
            // sin 의 정밀도가 무너져 오래 켜둘수록 값이 뭉개진다(파리가 한 점에 멈추거나 튄다).
            float2 FlyWaypoint(float seed, float k)
            {
                float2 total = 0.0;
                float2 partial = 0.0;

                [unroll]
                for (int j = 0; j < LSO_FLY_STEPS; j++)
                {
                    float angle = LSO_Hash11(seed + (float)j) * 6.2831853;
                    float2 stepDir = float2(cos(angle), sin(angle));
                    total += stepDir;
                    partial += ((float)j < k) ? stepDir : 0.0;
                }

                return partial - total * (k / (float)LSO_FLY_STEPS);
            }

            // 빛 주위를 도는 궤도. 셰이더는 빛이 어디 있는지 모르므로 _OrbitCenter 로 알려 준다 —
            // "이 파리가 그려진 자리에서 빛까지의 방향과 거리"(쿼드 폭 대비)다. 그 길이가 곧 궤도 반지름이 된다.
            //
            // 각속도를 반지름에 반비례시켜(omega = 속도 / 반지름) 선속도를 _OrbitSpeed 로 고정한다 —
            // 안 그러면 빛에서 먼 파리가 훨씬 빠르게 돈다.
            // 원운동이라 궤적이 저절로 닫히고, 반지름 흔들림도 한 바퀴의 정수배로 맞춰 루프가 이어진다.
            float2 FlyOrbit(float seed)
            {
                float2 toLight = _OrbitCenter.xy;
                float radius = max(length(toLight), 1e-4);

                // t=0 에 파리가 그려진 자리에 오도록, 빛에서 파리를 향하는 각도에서 출발한다.
                float baseAngle = atan2(-toLight.y, -toLight.x);
                float omega = _OrbitSpeed / radius;

                // 개체마다 도는 방향과 출발 위치를 흩어 8마리가 한 줄로 붙어 돌지 않게 한다.
                float spin = (LSO_Hash11(seed + 3.1) < 0.5) ? -1.0 : 1.0;
                float phase = LSO_Hash11(seed + 7.7) * 6.2831853;

                float angle = baseAngle + phase + spin * omega * _Time.y;

                // 빛에 가까워졌다 멀어졌다 — 한 바퀴에 정수 번 흔들려야 루프가 닫힌다.
                float laps = max(round(_OrbitWobbleRate), 1.0);
                float r = radius * (1.0 + _OrbitWobble * sin(angle * laps + phase * 1.3));

                // 파리가 그려진 자리 기준의 오프셋으로 환산한다.
                return toLight + float2(cos(angle), sin(angle)) * r;
            }

            float2 FlyDrift(float seed)
            {
                float rate = max(_DartRate, 1e-3);
                float stepLen = _DartSpeed / rate; // 한 칸에 가는 거리 = 속도 ÷ 꺾는 빈도

                // 박자는 모든 개체가 똑같다. 시작 지점만 시드로 한 바퀴 안에서 어긋나 있어 동시에 꺾지는 않는다.
                // fmod 로 한 바퀴 안에 가둬 두므로 시간이 아무리 흘러도 해시 인자가 커지지 않는다.
                float t = _Time.y * rate + LSO_Hash11(seed + 5.5) * (float)LSO_FLY_STEPS;
                float k = fmod(t, (float)LSO_FLY_STEPS);
                float i = floor(k);
                float f = frac(k);

                // 이 칸이 끝나는 자리가 다음 칸이 시작하는 자리와 정확히 같다 — 그래서 건너뛰지 않는다.
                float2 dart = lerp(FlyWaypoint(seed, i), FlyWaypoint(seed, i + 1.0), f) * stepLen;

                // 잘고 빠른 떨림. 주파수를 한 바퀴 주파수의 정수배로 맞춰 경로와 같은 주기로 닫히게 한다 —
                // 안 맞추면 루프가 이어지는 지점에서 떨림만 툭 끊긴다. 위상은 시드로 흩어 서로 겹치지 않게 한다.
                float loopHz = rate / (float)LSO_FLY_STEPS;
                float buzzHz = loopHz * max(round(_BuzzSpeed / max(loopHz, 1e-4)), 1.0);
                float bp = LSO_Hash11(seed + 53.3) * 6.2831853;
                float2 buzz = float2(sin(_Time.y * 6.2831853 * buzzHz + bp),
                                     sin(_Time.y * 6.2831853 * buzzHz * 2.0 + bp * 1.7)) * _BuzzAmount;

                float2 move = lerp(dart, FlyOrbit(seed), _OrbitAmount);
                return (move + buzz) * float2(1.0, _DartAspectY);
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
                float3 movedOS = input.positionOS.xyz;
                movedOS.xy += FlyDrift(FlySeed()); // 판이 통째로 움직인다(그림이 늘어나지 않는다).

                VertexPositionInputs vertexInput = GetVertexPositionInputs(movedOS);
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
                half _Seed;
                half _DartRate;
                half _DartSpeed;
                half _DartAspectY;
                float4 _OrbitCenter;
                half _OrbitAmount;
                half _OrbitSpeed;
                half _OrbitWobble;
                half _OrbitWobbleRate;
                half _BuzzAmount;
                half _BuzzSpeed;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            // 경로 한 바퀴를 이루는 칸 수. 이게 곧 루프 길이다 — 한 바퀴에 LSO_FLY_STEPS / _DartRate 초 걸린다.
            #define LSO_FLY_STEPS 32

            float LSO_Hash11(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            float FlySeed()
            {
                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                return _Seed * 7.13 + originWS.x * 3.7 + originWS.y * 2.3;
            }

            // 본체 패스와 같은 식 — 그림자가 튀는 걸 따라온다. 한쪽만 고치면 그림자가 제자리에 남는다.
            // 닫힌 경로의 k번째 꼭짓점. 무작위 방향의 걸음을 k번 더하되, 전체 이동량의 k/N 만큼을 빼서
            // N번째가 0번째와 정확히 같아지게 만든다 — 한 바퀴가 끝나면 이음매 없이 처음으로 이어진다.
            // 걸음 길이가 전부 같으므로 구간마다 속도가 달라지지 않는다.
            //
            // 인덱스를 N 안에서만 돌리는 것이 중요하다. 시간에 비례해 계속 커지는 값을 해시에 넣으면
            // sin 의 정밀도가 무너져 오래 켜둘수록 값이 뭉개진다(파리가 한 점에 멈추거나 튄다).
            float2 FlyWaypoint(float seed, float k)
            {
                float2 total = 0.0;
                float2 partial = 0.0;

                [unroll]
                for (int j = 0; j < LSO_FLY_STEPS; j++)
                {
                    float angle = LSO_Hash11(seed + (float)j) * 6.2831853;
                    float2 stepDir = float2(cos(angle), sin(angle));
                    total += stepDir;
                    partial += ((float)j < k) ? stepDir : 0.0;
                }

                return partial - total * (k / (float)LSO_FLY_STEPS);
            }

            // 빛 주위를 도는 궤도. 셰이더는 빛이 어디 있는지 모르므로 _OrbitCenter 로 알려 준다 —
            // "이 파리가 그려진 자리에서 빛까지의 방향과 거리"(쿼드 폭 대비)다. 그 길이가 곧 궤도 반지름이 된다.
            //
            // 각속도를 반지름에 반비례시켜(omega = 속도 / 반지름) 선속도를 _OrbitSpeed 로 고정한다 —
            // 안 그러면 빛에서 먼 파리가 훨씬 빠르게 돈다.
            // 원운동이라 궤적이 저절로 닫히고, 반지름 흔들림도 한 바퀴의 정수배로 맞춰 루프가 이어진다.
            float2 FlyOrbit(float seed)
            {
                float2 toLight = _OrbitCenter.xy;
                float radius = max(length(toLight), 1e-4);

                // t=0 에 파리가 그려진 자리에 오도록, 빛에서 파리를 향하는 각도에서 출발한다.
                float baseAngle = atan2(-toLight.y, -toLight.x);
                float omega = _OrbitSpeed / radius;

                // 개체마다 도는 방향과 출발 위치를 흩어 8마리가 한 줄로 붙어 돌지 않게 한다.
                float spin = (LSO_Hash11(seed + 3.1) < 0.5) ? -1.0 : 1.0;
                float phase = LSO_Hash11(seed + 7.7) * 6.2831853;

                float angle = baseAngle + phase + spin * omega * _Time.y;

                // 빛에 가까워졌다 멀어졌다 — 한 바퀴에 정수 번 흔들려야 루프가 닫힌다.
                float laps = max(round(_OrbitWobbleRate), 1.0);
                float r = radius * (1.0 + _OrbitWobble * sin(angle * laps + phase * 1.3));

                // 파리가 그려진 자리 기준의 오프셋으로 환산한다.
                return toLight + float2(cos(angle), sin(angle)) * r;
            }

            float2 FlyDrift(float seed)
            {
                float rate = max(_DartRate, 1e-3);
                float stepLen = _DartSpeed / rate;

                float t = _Time.y * rate + LSO_Hash11(seed + 5.5) * (float)LSO_FLY_STEPS;
                float k = fmod(t, (float)LSO_FLY_STEPS);
                float i = floor(k);
                float f = frac(k);

                float2 dart = lerp(FlyWaypoint(seed, i), FlyWaypoint(seed, i + 1.0), f) * stepLen;

                float loopHz = rate / (float)LSO_FLY_STEPS;
                float buzzHz = loopHz * max(round(_BuzzSpeed / max(loopHz, 1e-4)), 1.0);
                float bp = LSO_Hash11(seed + 53.3) * 6.2831853;
                float2 buzz = float2(sin(_Time.y * 6.2831853 * buzzHz + bp),
                                     sin(_Time.y * 6.2831853 * buzzHz * 2.0 + bp * 1.7)) * _BuzzAmount;

                float2 move = lerp(dart, FlyOrbit(seed), _OrbitAmount);
                return (move + buzz) * float2(1.0, _DartAspectY);
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

                float3 movedOS = input.positionOS.xyz;
                movedOS.xy += FlyDrift(FlySeed());
                float3 positionWS = TransformObjectToWorld(movedOS);

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
