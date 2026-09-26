// DLJ: standalone mood variant; keeps the existing single-pass scene/UI composition.
Shader "BlueComplex/DLJ/HeartbeatMood"
{
    Properties
    {
        [HideInInspector] _MoodOnly ("Mood Only", Float) = 0
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

        _DepressedAmount ("Depressed Amount", Range(0, 1)) = 0
        _DepressedTint ("Underwater Blue", Color) = (0.04, 0.36, 1, 1)
        _BlueTintStrength ("Blue Tint Strength", Range(0, 1)) = 0.9
        _DepressedContrast ("Depressed Contrast", Range(1, 1.8)) = 1.3
        _DepressedSharpness ("Depressed Sharpness", Range(0, 1.5)) = 0.85
        _WaterLightStrength ("Water Light Strength", Range(0, 2)) = 0.95
        _WaterLightSpread ("Water Light Spread", Range(0.005, 0.6)) = 0.28
        _WaterLightSharpness ("Water Light Sharpness", Range(2, 32)) = 12
        _WaterBeamCount ("Water Beam Count", Int) = 24
        _WaterBeamWidth ("Water Beam Width", Range(0.1, 5)) = 1
        _WaterWidthVariation ("Water Width Variation", Range(0, 1)) = 1
        _WaterWaveStrength ("Water Wave Strength", Range(0, 2)) = 1
        _WaterWaveSpeed ("Water Wave Speed", Range(0, 3)) = 0.8
        _WaterScatterStrength ("Water Scatter Strength", Range(0, 2)) = 1.15
        _WaterAfterglowStrength ("Water Afterglow Strength", Range(0, 2)) = 0.65
        _WaterAfterglowAngle ("Water Afterglow Angle", Range(0, 140)) = 60
        _WaterAfterglowLength ("Water Afterglow Length", Range(0.5, 2)) = 1.25
        _WaterFadeStart ("Water Fade Start", Range(0, 0.95)) = 0.45
        _WaterDistanceFade ("Water Distance Fade", Range(0, 4)) = 1.6
        _WaterScatterAngle ("Water Scatter Angle", Range(0, 160)) = 22
        _WaterLightDirection ("Water Light Direction", Range(-180, 180)) = 0
        _WindowLightStyle ("Window Light Style", Int) = 0
        _WindowBandWidth ("Soft Window Band Width", Range(0.003, 0.05)) = 0.016
        _WindowBandSoftness ("Soft Window Band Softness", Range(0, 1)) = 0.8
        _WindowBandOpacity ("Soft Window Band Opacity", Range(0, 1)) = 1
        _WindowBandCoreBrightness ("Soft Window Band Core Brightness", Range(1, 5)) = 2
        _WindowBandTint ("Soft Window Band Tint", Color) = (1, 0.97, 0.88, 1)
        _WindowShaftGrouping ("Window Shaft Grouping", Range(0, 1)) = 0.75
        _WindowShaftVerticalBias ("Window Shaft Vertical Bias", Range(0, 0.6)) = 0.52
        _WindowShaftTopLength ("Window Shaft Top Length", Range(0.05, 1.5)) = 0.18
        _WindowShaftBottomLength ("Window Shaft Bottom Length", Range(0.05, 1.5)) = 0.85
        _WindowShaftAmbientStrength ("Window Shaft Ambient Strength", Range(0, 0.5)) = 0.2
        _ExcitedAmount ("Excited Amount", Range(0, 1)) = 0
        _ExcitedBlend ("Excited Tint Blend", Range(0, 1)) = 0
        _ChromaticBurst ("Transient Chromatic Burst", Range(0, 1)) = 0
        _PastelSeparation ("Pastel Separation", Range(0, 0.025)) = 0.012
        _PastelStrength ("Pastel Strength", Range(0, 1)) = 0.82
        _PastelPink ("Pastel Pink (sRGB)", Vector) = (0.96, 0.70, 0.86, 1)
        _PastelMint ("Pastel Mint (sRGB)", Vector) = (0.67, 0.94, 0.87, 1)
        _PastelLavender ("Pastel Lavender (sRGB)", Vector) = (0.78, 0.75, 0.97, 1)
        _PastelSaturation ("Pastel Saturation", Range(0, 1)) = 0.58
        _PastelContrast ("Pastel Contrast", Range(0.5, 1)) = 0.84
        _PastelGradeStrength ("Pastel Grade Strength", Range(0, 1)) = 0.8
        _GlitchAmount ("Transient Glitch", Range(0, 1)) = 0
        _GlitchDisplacement ("Glitch Displacement", Range(0, 0.035)) = 0.012
        _MoodTime ("Unscaled Mood Time", Float) = 0

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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            // URP의 Core.hlsl를 먼저 포함한다(URP 자체 CoreBlit.shader와 같은 방식). Blit.hlsl이 쓰는 TEXTURE2D_X()/_Time/unity_StereoEyeIndex 등이
            // 여기서 정의된다. 예전처럼 core 패키지의 TextureXR.hlsl을 직접 포함하면 D3D11에서 TEXTURE2D_X가 Texture2DArray로 풀려,
            // Render Graph가 일반 Texture2D로 바인딩한 _BlitTexture와 타입이 어긋나 Unity가 기본 더미(회색) 텍스처로 대체한다 —
            // 그러면 CRT가 씬을 전혀 읽지 못하고 화면 전체가 균일한 회색이 된다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_UITex);
            SAMPLER(sampler_UITex);

            CBUFFER_START(UnityPerMaterial)
                float _MoodOnly;
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
                float _DepressedAmount;
                float4 _DepressedTint;
                float _BlueTintStrength;
                float _DepressedContrast;
                float _DepressedSharpness;
                float _WaterLightStrength;
                float _WaterLightSpread;
                float _WaterLightSharpness;
                int _WaterBeamCount;
                float _WaterBeamWidth;
                float _WaterWidthVariation;
                float _WaterWaveStrength;
                float _WaterWaveSpeed;
                float _WaterScatterStrength;
                float _WaterAfterglowStrength;
                float _WaterAfterglowAngle;
                float _WaterAfterglowLength;
                float _WaterFadeStart;
                float _WaterDistanceFade;
                float _WaterScatterAngle;
                float _WaterLightDirection;
                int _WaterSourceCount;
                float4 _WaterSources[8];
                int _WindowEdgeCount;
                float4 _WindowEdges[9];
                float4 _WindowDirections[9];
                float4 _WindowStyles[9];
                int _WindowLightStyle;
                float _WindowBandWidth;
                float _WindowBandSoftness;
                float _WindowBandOpacity;
                float _WindowBandCoreBrightness;
                float4 _WindowBandTint;
                float _WindowShaftGrouping;
                float _WindowShaftVerticalBias;
                float _WindowShaftTopLength;
                float _WindowShaftBottomLength;
                float _WindowShaftAmbientStrength;
                float _ExcitedAmount;
                float _ExcitedBlend;
                float _ChromaticBurst;
                float _PastelSeparation;
                float _PastelStrength;
                float4 _PastelPink;
                float4 _PastelMint;
                float4 _PastelLavender;
                float _PastelSaturation;
                float _PastelContrast;
                float _PastelGradeStrength;
                float _GlitchAmount;
                float _GlitchDisplacement;
                float _MoodTime;
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
                [branch]
                if (_MoodOnly > 0.5) return scene;
                float4 ui = SAMPLE_TEXTURE2D(_UITex, sampler_UITex, uv);
                return lerp(scene, ui.rgb, ui.a);
            }

            // 화면 밖 광원 복제를 막고, UI가 아닌 실제 배경의 밝은 부분만 추출한다.
            float WaterHighlight(float2 uv)
            {
                // 밝은 막대/책상 테두리는 광원이 아니다. 지정된 전등의 발광부만 샘플링한다.
                float sourceMask = 0.0;
                [loop]
                for (int sourceIndex = 0; sourceIndex < min(_WaterSourceCount, 8); sourceIndex++)
                {
                    float4 source = _WaterSources[sourceIndex];
                    float2 delta = (uv - source.xy) / max(source.zw, float2(0.001, 0.001));
                    sourceMask = max(sourceMask, 1.0 - smoothstep(0.55, 1.0, length(delta)));
                }
                [branch]
                if (sourceMask <= 0.0) return 0.0;
                float inside = step(0.0, uv.x) * step(uv.x, 1.0)
                    * step(0.0, uv.y) * step(uv.y, 1.0);
                float3 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv)).rgb;
                float lum = dot(source, float3(0.2126, 0.7152, 0.0722));
                return max(lum - max(_BloomThreshold, 0.65), 0.0) * inside * sourceMask;
            }

            // 지정한 길이 비율까지 밝기를 유지하고, 남은 구간에서만 투명해진다.
            float WaterDistanceOpacity(float progress)
            {
                float fadeStart = clamp(_WaterFadeStart, 0.0, 0.95);
                float fadeProgress = saturate((progress - fadeStart) / (1.0 - fadeStart));
                float easedProgress = smoothstep(0.0, 1.0, fadeProgress);
                return (1.0 - easedProgress) * exp2(-easedProgress * max(_WaterDistanceFade, 0.0));
            }

            // 흰 심(x), 주변 산란(y), 넓은 원뿔과 끝부분의 잔광(z)을 독립적으로 누적한다.
            float3 WaterBeams(float2 uv)
            {
                if (_WaterLightStrength <= 0.001) return float3(0.0, 0.0, 0.0);
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float waveTime = _MoodTime * max(_WaterWaveSpeed, 0.0);
                float waveStrength = clamp(_WaterWaveStrength, 0.0, 2.0);
                // 광선의 방위각만 Y축 주위로 회전한다. 아래를 향하는 묶음 축은 고정한다.
                float rotation = waveTime * waveStrength * 0.35;
                float direction = radians(_WaterLightDirection);
                // 현재 CRT 소스 UV는 -Y가 화면 아래. API 기본 UV 원점으로 다시 뒤집지 않는다.
                float2 axis = float2(sin(direction), -cos(direction));
                float2 acrossAxis = float2(cos(direction), sin(direction));
                float3 beams = float3(0.0, 0.0, 0.0);
                float spreadAngle = clamp(_WaterScatterAngle, 0.0, 160.0);
                float wideSpread = saturate((spreadAngle - 22.0) / 98.0);
                int baseCount = clamp(_WaterBeamCount, 8, 48);
                int beamCount = baseCount + (int)ceil((float)baseCount * wideSpread);
                // 넓게 펼쳐도 가운데 축에 한 줄은 반드시 남도록 홀수로 보강한다.
                if (wideSpread > 0.0 && beamCount % 2 == 0) beamCount++;
                beamCount = min(beamCount, 97);
                float densityGain = pow(9.0 / (float)beamCount, 0.65);
                float haloSlope = tan(radians(spreadAngle / (float)(beamCount - 1)) * 1.15);
                float outerSlope = tan(radians(spreadAngle * 0.5));
                float afterglowSlope = tan(radians(clamp(_WaterAfterglowAngle, 0.0, 140.0) * 0.5));
                float afterglowLength = max(_WaterLightSpread * clamp(_WaterAfterglowLength, 0.5, 2.0), 0.001);
                [loop]
                for (int sourceIndex = 0; sourceIndex < min(_WaterSourceCount, 8); sourceIndex++)
                {
                    float4 source = _WaterSources[sourceIndex];
                    float2 delta = (uv - source.xy) * float2(aspect, 1.0);
                    float axialDepth = dot(delta, axis);
                    // 발광부 주변을 부드럽게 연결한다. 공통 평면으로 자르면 넓은 산란광에 가로 경계가 생긴다.
                    float sourceFeather = max(source.w, 0.001);
                    if (axialDepth <= -sourceFeather) continue;
                    float sourceFade = smoothstep(-sourceFeather, sourceFeather, axialDepth);
                    // Y축 회전 중에도 원뿔의 외곽은 유지한다. 개별 광선만 원뿔 안에서 앞뒤로 돈다.
                    // 광선 묶음의 중심은 유지하고 양쪽 외곽은 산란광까지 함께 서서히 지운다.
                    float outerWidth = sourceFeather + max(axialDepth, 0.0) * outerSlope;
                    float outerProgress = abs(dot(delta, acrossAxis)) / outerWidth;
                    float outerFade = 1.0 - smoothstep(0.15, 1.0, outerProgress);
                    // 전등 발광부만 읽는다. 밝은 막대/테두리에서 광선을 생성하지 않는다.
                    float2 dx = float2(source.z * 0.35, 0.0);
                    float2 dy = float2(0.0, source.w * 0.35);
                    float light = WaterHighlight(source.xy) * 0.4
                        + (WaterHighlight(source.xy + dx) + WaterHighlight(source.xy - dx)
                        + WaterHighlight(source.xy + dy) + WaterHighlight(source.xy - dy)) * 0.15;
                    if (light <= 0.0) continue;
                    // 표시한 외곽처럼 전구 아래부터 넓어지는 잔광. 개별 광선의 각도/굵기/개수와 독립적이다.
                    // 좁은 광선 묶음의 outerFade로 자르지 않아 원뿔 양옆까지 잔광이 이어진다.
                    float afterglowDepth = max(axialDepth, 0.0);
                    float afterglowProgress = afterglowDepth / afterglowLength;
                    float afterglowWidth = sourceFeather + afterglowDepth * afterglowSlope;
                    float afterglowLateral = abs(dot(delta, acrossAxis)) / afterglowWidth;
                    float afterglowSideFade = 1.0 - smoothstep(0.25, 1.0, afterglowLateral);
                    float afterglowOpacity = WaterDistanceOpacity(afterglowProgress);
                    beams.z += light * 0.8 * sourceFade * afterglowSideFade * afterglowOpacity;
                    [loop]
                    for (int beamIndex = 0; beamIndex < beamCount; beamIndex++)
                    {
                        float seed = Rand(float2(beamIndex + 3.0, sourceIndex + 7.0));
                        float detail = Rand(float2(beamIndex + 21.0, sourceIndex + 17.0));
                        // 끝 광선은 퍼짐 각도를 지키고, 내부 간격만 조금씩 불규칙하게 만든다.
                        float lane = (float)beamIndex / (float)(beamCount - 1) * 2.0 - 1.0;
                        bool centerBeam = abs(lane) < 0.0001;
                        if (!centerBeam)
                            lane += (seed - 0.5) * (1.1 / (float)(beamCount - 1)) * (1.0 - abs(lane));
                        // 가장자리 각도는 보존하면서 넓은 묶음의 중앙 밀도를 높인다.
                        lane = sign(lane) * pow(abs(lane), lerp(1.0, 1.35, wideSpread));
                        float coneAngle = radians(spreadAngle * 0.5) * abs(lane);
                        // 광선을 서로 다른 방위각에 배치해 옆면에서 한 줄로 겹치는 평면 회전을 피한다.
                        // 줄기 번호로 고정 속도를 분산한다. 시간/픽셀/줄기 수가 바뀌어도 배율은 유지한다.
                        float rotationSpeed = lerp(0.05, 2,
                            frac((beamIndex + 1.0) * 0.61803399 + sourceIndex * 0.38196601));
                        float azimuth = rotation * rotationSpeed + seed * 6.283185;
                        float3 coneDirection = float3(sin(coneAngle) * cos(azimuth),
                            -cos(coneAngle), sin(coneAngle) * sin(azimuth));
                        // Y축 회전한 3D 방향을 화면에 투영한다. Y 성분은 회전 시간과 무관하다.
                        float2 projected = acrossAxis * coneDirection.x - axis * coneDirection.y;
                        float projectedLength = max(length(projected), 0.001);
                        float2 beamAxis = projected / projectedLength;
                        float2 beamAcross = float2(-beamAxis.y, beamAxis.x);
                        // 한 점에서 시작하는 삼각형 대신 전구 폭 안에서 여러 줄이 출발한다.
                        float2 relative = delta - acrossAxis * (abs(lane) * cos(azimuth) * source.w * 0.6);
                        float depth = dot(relative, beamAxis);
                        float beamLength = max(_WaterLightSpread * projectedLength
                            * (centerBeam ? 1.0 : lerp(0.68, 1.0, seed)), 0.001);
                        // 회전한 각 광선의 시작면도 페이드한다. 즉시 끊으면 부채꼴 구역마다 밝기가 튄다.
                        // 심이 사라진 뒤에도 마지막 25% 구간에 잔광이 부드럽게 남는다.
                        if (depth <= -sourceFeather || depth >= beamLength * 1.25) continue;
                        float startFade = smoothstep(-sourceFeather, sourceFeather, depth);
                        float distanceProgress = max(depth / beamLength, 0.0);
                        float progress = saturate(distanceProgress);
                        // 가는 줄 / 중간 줄 / 넓은 띠를 분리해 굵기 편차를 확실히 만든다.
                        float widthClass = (beamIndex % 6 == 0 || centerBeam)
                            ? lerp(2.3, 4.5, detail)
                            : ((beamIndex % 6 == 3) ? lerp(0.7, 1.2, detail) : lerp(0.1, 0.32, detail));
                        float widthScale = lerp(1.0, widthClass, saturate(_WaterWidthVariation));
                        float baseWidth = max(source.w * 0.13 * widthScale
                            * sqrt(12.0 / max(_WaterLightSharpness, 2.0)), 0.0005);
                        baseWidth *= 1.0 + progress * 0.55;
                        // 각 줄기 중앙의 밝은 심을 별도 띠로 만든다. Width는 이 밝은 구간의 폭만 바꾼다.
                        float widthMultiplier = clamp(_WaterBeamWidth, 0.1, 5.0);
                        float coreHalfWidth = baseWidth * 0.35 * widthMultiplier;
                        // 테두리만 짧게 감쇠한다. 굵기를 올려도 흐린 가장자리는 넓어지지 않는다.
                        float coreEdgeWidth = max(baseWidth * 0.12, abs(_BlitTexture_TexelSize.y) * 0.75);
                        // 회전과 별개로 각 빛줄기의 밝기도 천천히 변화한다. 굵기는 유지한다.
                        float phase = seed * 6.28318;
                        float speed = lerp(0.65, 1.5, detail);
                        float lateralDistance = abs(dot(relative, beamAcross));
                        float coreMask = 1.0 - smoothstep(coreHalfWidth,
                            coreHalfWidth + coreEdgeWidth, lateralDistance);
                        // 심과 주변 산란도 지정한 시작 지점 이후부터만 거리 감쇠를 적용한다.
                        float fade = WaterDistanceOpacity(distanceProgress);
                        float shimmer = 1.0 + 0.08 * waveStrength * sin(waveTime * speed * 0.6 + phase);
                        float sourceEnergy = light * lerp(0.55, 1.0, detail) * shimmer * densityGain
                            * sourceFade * startFade * outerFade;
                        float energy = sourceEnergy * fade;
                        // 밝은 심 전체에 중심 밝기를 유지한다. 원래의 가는 프로파일로 다시 제한하지 않는다.
                        beams.x += coreMask * energy;
                        // 코어와 같은 회전 좌표에서 넓은 각도의 외곽 산란을 이웃 줄까지 연결한다.
                        float haloWidth = max(baseWidth * 3.16228, max(depth, 0.0) * haloSlope * wideSpread);
                        float haloLateral = dot(relative, beamAcross) / max(haloWidth, 0.0005);
                        beams.y += exp2(-haloLateral * haloLateral * 3.0) * energy;
                        // 끝을 따라 길게 남는 청록 잔광. 밝은 심의 Width와 독립적이며 빛구슬처럼 솟지 않는다.
                        float tailEnvelope = smoothstep(0.35, 0.65, distanceProgress)
                            * WaterDistanceOpacity(distanceProgress / 1.25);
                        float tailWidth = max(baseWidth * 2.2, beamLength * 0.025)
                            * lerp(1.0, 1.8, smoothstep(0.35, 1.25, distanceProgress));
                        float tailLateral = lateralDistance / max(tailWidth, 0.0005);
                        beams.z += exp2(-tailLateral * tailLateral * 2.0)
                            * tailEnvelope * sourceEnergy;
                    }
                }
                return beams;
            }

            // 방별 선택형: 참고 이미지의 부드러운 흰 빛 띠와 넓은 확산광.
            // 기존 가는 광선과 별도 경로이며 창틀 바깥 반평면 제한은 동일해.
            float3 SoftWindowBands(float2 uv)
            {
                float3 beams = 0.0;
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float2 pixel = uv * float2(aspect, 1.0);
                float time = _MoodTime * max(_WaterWaveSpeed, 0.0);
                float softness = saturate(_WindowBandSoftness);
                int count = clamp(_WaterBeamCount, 8, 48);
                [loop]
                for (int edgeIndex = 0; edgeIndex < min(_WindowEdgeCount, 8); edgeIndex++)
                {
                    float4 edge = _WindowEdges[edgeIndex];
                    float4 settings = _WindowDirections[edgeIndex];
                    float2 a = edge.xy * float2(aspect, 1.0);
                    float2 b = edge.zw * float2(aspect, 1.0);
                    float2 outward = settings.xy;
                    float outwardDepth = dot(pixel - a, outward);
                    if (outwardDepth <= 0.0) continue;
                    float reach = settings.z * 1.3 + _WindowBandWidth * _WaterBeamWidth * 6.0;
                    if (any(pixel < min(a, b) - reach) || any(pixel > max(a, b) + reach)) continue;
                    float2 tangent = normalize(b - a);
                    float startFade = smoothstep(0.0, max(_WindowStyles[edgeIndex].x * 0.5, 0.002), outwardDepth);
                    float density = pow(14.0 / (float)count, 0.65);
                    [loop]
                    for (int i = 0; i < count; i++)
                    {
                        float seed = Rand(float2(i + 13.0, edgeIndex + 41.0));
                        float detail = Rand(float2(i + 31.0, edgeIndex + 9.0));
                        float slot = ((float)i + lerp(0.12, 0.88, seed)) / (float)count;
                        float along = 0.5 - 0.5 * cos(slot * 3.14159265);
                        float lane = along * 2.0 - 1.0;
                        float angle = lane * abs(lane) * 0.959931 + (detail - 0.5) * 0.065;
                        float2 axis = outward * cos(angle) + tangent * sin(angle);
                        float2 across = float2(-axis.y, axis.x);
                        float2 relative = pixel - lerp(a, b, along);
                        float depth = dot(relative, axis);
                        float beamLength = settings.z * lerp(0.72, 1.0, seed);
                        // 기울어진 광선의 시작 평면으로 자르면 창틀 바깥에 검은 쐐기 모양 빈틈이 생겨.
                        // 폭 전체를 창틀까지 연장하고, 시작 경계는 위의 outwardDepth로만 제한해.
                        if (depth > beamLength * 1.3) continue;
                        float progress = max(depth, 0.0) / beamLength;
                        // 넓은 빛 띠를 약하게 휘게 하고, 밝기만 천천히 일렁이게 해.
                        float bend = sin(detail * 6.28318 + progress * 1.8) * beamLength * 0.008 * progress * progress;
                        float lateral = abs(dot(relative, across) - bend);
                        float widthVariation = lerp(1.0, lerp(0.55, 1.4, detail), saturate(_WaterWidthVariation));
                        float bandWidth = _WindowBandWidth * widthVariation * clamp(_WaterBeamWidth, 0.1, 5.0)
                            * sqrt(12.0 / max(_WaterLightSharpness, 2.0)) * lerp(0.8, 1.35, saturate(progress));
                        float profile = lateral / max(bandWidth, 0.001);
                        // 평평한 흰 심 대신 부드러운 종 모양 밝기. 머리카락 같은 선은 만들지 않아.
                        float core = exp2(-profile * profile * lerp(5.0, 2.0, softness));
                        float mist = exp2(-profile * profile * lerp(0.9, 0.35, softness));
                        float shimmer = 1.0 + 0.06 * clamp(_WaterWaveStrength, 0.0, 2.0)
                            * sin(time * lerp(0.25, 0.55, detail) + seed * 6.28318);
                        float energy = settings.w * lerp(0.45, 0.9, seed) * density * startFade * shimmer;
                        float fade = WaterDistanceOpacity(progress);
                        beams.x += core * energy * fade * 0.55;
                        beams.y += mist * energy * fade * 0.65;
                        beams.z += mist * energy * WaterDistanceOpacity(progress / 1.3) * 0.18;
                    }
                }
                return beams;
            }

            // 창문을 정면에서 볼 때의 빛. 기존 테두리 출발과 중앙 출발을 방별로 선택해.
            float3 RoomWindowShafts(float2 uv, bool fromCenter)
            {
                float3 beams = 0.0;
                float cornerLight = 0.0;
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float2 scale = float2(aspect, 1.0);
                float2 pixel = uv * scale;
                float time = _MoodTime * max(_WaterWaveSpeed, 0.0);
                int count = clamp((int)round((float)_WaterBeamCount * 0.36), 4, 7);
                float2 windowCenter = 0.0;
                if (fromCenter)
                {
                    [loop]
                    for (int centerEdge = 0; centerEdge < min(_WindowEdgeCount, 8); centerEdge++)
                        windowCenter += (_WindowEdges[centerEdge].xy + _WindowEdges[centerEdge].zw) * scale;
                    windowCenter /= max(2.0 * _WindowEdgeCount, 1.0);
                    windowCenter += float2(0.0, _WindowShaftVerticalBias * 0.03);
                }
                [loop]
                for (int edgeIndex = 0; edgeIndex < min(_WindowEdgeCount, 8); edgeIndex++)
                {
                    float4 edge = _WindowEdges[edgeIndex];
                    float4 settings = _WindowDirections[edgeIndex];
                    float2 a = edge.xy * scale;
                    float2 b = edge.zw * scale;
                    float2 outward = settings.xy;
                    float outwardDepth = dot(pixel - a, outward);
                    float reach = max(max(settings.z, _WindowShaftTopLength), _WindowShaftBottomLength)
                        * 1.5 + _WindowBandWidth * 4.0;
                    if (fromCenter) reach += distance(windowCenter, (a + b) * 0.5);
                    if (any(pixel < min(a, b) - reach) || any(pixel > max(a, b) + reach)) continue;
                    // 대각선 줄기와 변의 첫 줄기 사이에 생기는 검은 쐐기를 부드럽게 연결해.
                    // 모서리 경계 안쪽 몇 픽셀까지 부드럽게 덮어, 맞닿지 않는 윤곽에도 검은 홈이 남지 않게 해.
                    float cornerDistance = min(distance(pixel, a), distance(pixel, b));
                    float cornerRadius = max(_WindowBandWidth * 5.5, settings.z * 0.4);
                    float cornerProfile = exp2(-pow(cornerDistance / cornerRadius, 2.0) * 1.5);
                    float cornerFade = smoothstep(-0.012, 0.012, outwardDepth);
                    if (!fromCenter) cornerLight = max(cornerLight, cornerProfile * settings.w * cornerFade);
                    if (!fromCenter && outwardDepth <= 0.0) continue;
                    float2 tangent = normalize(b - a);
                    float edgeFade = fromCenter ? 1.0
                        : smoothstep(0.0, max(_WindowStyles[edgeIndex].x * 0.5, 0.006), outwardDepth);
                    int edgeCount = clamp(count + (int)floor(Rand(float2(edgeIndex + 51.0, 19.0)) * 3.0) - 1, 3, 8);
                    float groupShift = (Rand(float2(edgeIndex + 7.0, 73.0)) - 0.5) * 0.1;
                    [loop]
                    for (int i = 0; i < edgeCount + 2; i++)
                    {
                        float seed = Rand(float2(i + 17.0, edgeIndex + 23.0));
                        float detail = Rand(float2(i + 43.0, edgeIndex + 7.0));
                        bool corner = i == 0 || i == edgeCount + 1;
                        float along = corner ? (i == 0 ? 0.0 : 1.0) : 0.5;
                        if (!corner)
                        {
                            int index = i - 1;
                            int firstSize = edgeCount / 2;
                            bool firstGroup = index < firstSize;
                            int localIndex = firstGroup ? index : index - firstSize;
                            int groupSize = firstGroup ? firstSize : edgeCount - firstSize;
                            float regular = ((float)index + 0.5 + (seed - 0.5) * 0.12) / (float)edgeCount;
                            float clustered = (firstGroup ? 0.28 : 0.72) + groupShift
                                + (((float)localIndex + 0.5) / (float)groupSize - 0.5) * (firstGroup ? 0.14 : 0.18)
                                + (seed - 0.5) * 0.025;
                            along = lerp(regular, clustered, saturate(_WindowShaftGrouping));
                        }
                        // 중앙 출발일 때는 각 줄기가 실제 창틀의 다른 지점을 통과해.
                        // 기존 테두리 출발은 이전 각도 계산을 유지해.
                        float angle = corner ? (i == 0 ? -0.70 : 0.70)
                            : (along * 2.0 - 1.0) * 0.28 + (detail - 0.5) * 0.08;
                        float2 target = lerp(a, b, along);
                        float2 axis = fromCenter
                            ? normalize(target - windowCenter)
                            : outward * cos(angle) + tangent * sin(angle);
                        float2 across = float2(-axis.y, axis.x);
                        float2 origin = fromCenter ? windowCenter : lerp(a, b, along)
                            + float2(0.0, _WindowShaftVerticalBias * 0.03);
                        float2 relative = pixel - origin;
                        float depth = dot(relative, axis);
                        float edgeLength = lerp(settings.z, _WindowShaftTopLength, saturate(outward.y));
                        edgeLength = lerp(edgeLength, _WindowShaftBottomLength, saturate(-outward.y));
                        float launchDepth = fromCenter ? max(dot((a + b) * 0.5 - windowCenter, outward), 0.0) : 0.0;
                        float beamLength = launchDepth + edgeLength * lerp(0.68, 1.24, detail);
                        // 광선의 시작 평면으로 폭을 자르지 않아 창틀에 검은 쐐기가 생기지 않아.
                        if ((fromCenter && depth <= 0.0) || depth > beamLength * 1.15) continue;
                        float progress = fromCenter
                            ? max(depth - launchDepth, 0.0) / max(beamLength - launchDepth, 0.005)
                            : depth / max(beamLength, 0.005);
                        float widthVariation = lerp(1.0, lerp(0.38, 1.8, seed), saturate(_WaterWidthVariation));
                        float width = _WindowBandWidth * clamp(_WaterBeamWidth, 0.1, 5.0)
                            * widthVariation * lerp(0.85, 1.32, saturate(progress))
                            * (1.0 + 0.08 * sin(progress * 5.0 + detail * 6.28318));
                        float bend = sin(detail * 6.28318 + progress * 1.8)
                            * beamLength * 0.01 * progress * progress;
                        float profile = abs(dot(relative, across) - bend) / max(width, 0.001);
                        float core = exp2(-profile * profile * lerp(2.8, 1.65, saturate(_WindowBandSoftness)));
                        float haze = exp2(-profile * profile * (fromCenter ? 0.9 : 0.42));
                        float shimmer = 1.0 + sin(time * 0.35 + seed * 6.28318)
                            * clamp(_WaterWaveStrength, 0.0, 2.0) * 0.035;
                        float energy = settings.w * edgeFade * shimmer
                            * (1.0 - outward.y * _WindowShaftVerticalBias * 0.75)
                            * lerp(0.35, 1.5, seed) * (corner ? (fromCenter ? 0.4 : 0.76) : 1.0)
                            * (fromCenter ? 0.28 : 1.0);
                        float fade = WaterDistanceOpacity(progress);
                        beams.x += core * energy * fade * 0.67;
                        beams.y += haze * energy * fade * 0.17;
                        beams.z += haze * energy * WaterDistanceOpacity(progress / 1.2) * 0.045;
                    }
                }
                beams.x += cornerLight * 0.45;
                beams.y += cornerLight;
                return beams;
            }

            // 광선 사이에도 남는 옅은 산란광. 창문 윤곽까지의 거리로만 감쇠해 줄기 간격을 메우되,
            // 줄기 자체의 밝기와 모양은 바꾸지 않아.
            float RoomWindowAmbient(float2 uv)
            {
                if (_WindowEdgeCount <= 0 || _WaterLightStrength <= 0.001
                    || _WindowShaftAmbientStrength <= 0.001) return 0.0;
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float2 pixel = uv * float2(aspect, 1.0);
                float nearest = 1000.0;
                float outside = -1000.0;
                float radius = 0.12;
                float strength = 0.0;
                [loop]
                for (int edgeIndex = 0; edgeIndex < min(_WindowEdgeCount, 8); edgeIndex++)
                {
                    float4 edge = _WindowEdges[edgeIndex];
                    float4 settings = _WindowDirections[edgeIndex];
                    float2 a = edge.xy * float2(aspect, 1.0);
                    float2 b = edge.zw * float2(aspect, 1.0);
                    float2 segment = b - a;
                    float along = saturate(dot(pixel - a, segment) / max(dot(segment, segment), 0.000001));
                    float edgeDistance = distance(pixel, a + segment * along);
                    if (edgeDistance < nearest)
                    {
                        nearest = edgeDistance;
                        float edgeLength = lerp(settings.z, _WindowShaftTopLength, saturate(settings.y));
                        edgeLength = lerp(edgeLength, _WindowShaftBottomLength, saturate(-settings.y));
                        // Window Glow는 위쪽 광선 길이와 무관하게 네 변 모두 같은 폭으로 번져.
                        // 위쪽 길이가 짧더라도 위·왼쪽 위 모서리가 어두워지지 않게 해.
                        radius = _WindowLightStyle == 4 ? 0.12
                            : _WindowLightStyle == 3 ? clamp(edgeLength * 0.18, 0.04, 0.12)
                            : max(edgeLength * 1.25, 0.12);
                    }
                    outside = max(outside, dot(pixel - a, settings.xy));
                    strength = max(strength, settings.w);
                }
                float borderFade = smoothstep(0.0, 0.025, outside);
                float falloff = exp2(-pow(nearest / radius, 2.0) * 1.5);
                return borderFade * falloff * strength * saturate(_WindowShaftAmbientStrength);
            }

            // 창문은 지정된 테두리에서만 출발해. 어두운 창틀도 발광 경계로 쓸 수 있도록 밝기 추출과 분리해.
            // 변마다 고정된 수의 광선을 분배하며 전구 묶음을 지점마다 복제하지 않아.
            // 시계 중심을 지나는 가로선에서 아래로 뻗는다. 시계판의 아래쪽도 옅은 빛에 잠긴다.
            float3 ClockWindowShafts(float2 uv)
            {
                int edgeCount = min(_WindowEdgeCount, 8);
                if (edgeCount < 2) return 0.0;
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float2 scale = float2(aspect, 1.0);
                float2 left = _WindowEdges[0].xy * scale;
                float2 right = _WindowEdges[edgeCount - 1].zw * scale;
                float radius = length(right - left) * 0.5;
                if (radius <= 0.001) return 0.0;
                float2 center = (left + right) * 0.5;
                float2 across = (right - left) / (radius * 2.0);
                float2 down = float2(across.y, -across.x);
                float2 pixel = uv * scale;
                float2 delta = pixel - center;
                float depth = dot(delta, down);
                float reach = max(_WindowDirections[0].z, 0.001);
                if (depth <= 0.0 || depth >= reach) return 0.0;
                float feather = max(abs(_BlitTexture_TexelSize.y) * 1.5, 0.001);
                // 시계판 아래쪽은 살짝만 가리고, 시계탑으로 내려갈수록 빛을 드러내.
                float sourceFade = lerp(0.25, 1.0, smoothstep(0.0, radius * 1.15, depth));
                float slope = tan(radians(clamp(_WaterScatterAngle, 0.0, 100.0) * 0.5));
                float lateral = abs(dot(delta, across));
                float coneWidth = radius + depth * slope;
                float envelope = sourceFade * (1.0 - smoothstep(coneWidth * 0.88, coneWidth, lateral));
                if (envelope <= 0.001) return 0.0;
                float clockFade = WaterDistanceOpacity(depth / reach);
                float strength = _WindowDirections[0].w;
                float3 beams = float3(0.0, 0.0, strength * 0.3 * envelope * clockFade);
                float waveTime = _MoodTime * max(_WaterWaveSpeed, 0.0);
                int count = clamp(_WaterBeamCount, 8, 48);
                [loop]
                for (int i = 0; i < count; i++)
                {
                    float seed = Rand(float2(i + 19.0, 73.0));
                    float detail = Rand(float2(i + 43.0, 29.0));
                    float slot = ((float)i + lerp(0.2, 0.8, seed)) / (float)count * edgeCount;
                    int edge = min((int)floor(slot), edgeCount - 1);
                    float4 endpoints = _WindowEdges[edge];
                    float2 origin = lerp(endpoints.xy, endpoints.zw, frac(slot)) * scale;
                    float lane = dot(origin - center, across) / radius;
                    float2 axis = normalize(down + across * lane * slope);
                    float2 sideways = float2(-axis.y, axis.x);
                    float2 relative = pixel - origin;
                    float along = dot(relative, axis);
                    if (along <= 0.0) continue;
                    float beamLength = max((reach - dot(origin - center, down)) / dot(axis, down), 0.001);
                    float progress = along / (beamLength * lerp(0.86, 1.0, detail));
                    float fade = WaterDistanceOpacity(progress);
                    float bend = sin(progress * 2.0 + waveTime * 0.45 + seed * 6.28318)
                        * 0.004 * clamp(_WaterWaveStrength, 0.0, 2.0) * progress * progress;
                    float distance = abs(dot(relative, sideways) - bend);
                    float variation = i % 5 == 0 ? 1.6 : lerp(0.28, 0.75, detail);
                    float clockWidth = _WindowLightStyle == 7 ? _WindowStyles[0].x : _WindowBandWidth;
                    float width = max(clockWidth * lerp(1.0, variation, _WaterWidthVariation)
                        * clamp(_WaterBeamWidth, 0.1, 5.0) * (1.0 + progress * 0.35), feather);
                    float clockSoftness = _WindowLightStyle == 7 ? 0.0 : _WindowBandSoftness;
                    float softness = lerp(0.2, 1.0, saturate(clockSoftness));
                    float core = 1.0 - smoothstep(width * 0.15, width * (0.4 + softness), distance);
                    float halo = exp2(-pow(distance / max(width * 2.5, feather), 2.0) * 2.0);
                    float shimmer = 1.0 + 0.12 * clamp(_WaterWaveStrength, 0.0, 2.0)
                        * sin(waveTime * lerp(0.6, 1.3, detail) + seed * 6.28318);
                    float energy = _WindowDirections[edge].w * lerp(0.45, 0.8, seed)
                        * pow(24.0 / (float)count, 0.65) * fade * shimmer * envelope
                        * smoothstep(0.0, feather * 3.0, along);
                    beams.x += core * energy * 0.4;
                    beams.y += halo * energy * 0.5;
                }
                return beams;
            }

            // 벽의 세로 출발선에서만 오른쪽으로 비춘다. 선 왼쪽은 빛을 완전히 제외해.
            float3 LeftWallShafts(float2 uv)
            {
                if (_WindowEdgeCount < (_WindowLightStyle == 7 ? 9 : 1)) return 0.0;
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float2 scale = float2(aspect, 1.0);
                int wallEdge = _WindowLightStyle == 7 ? 8 : 0;
                float4 edge = _WindowEdges[wallEdge];
                float2 bottom = edge.xy * scale;
                float2 top = edge.zw * scale;
                float height = length(top - bottom);
                if (height <= 0.001) return 0.0;
                float2 up = (top - bottom) / height;
                float2 right = float2(up.y, -up.x);
                float2 center = (bottom + top) * 0.5;
                float2 delta = uv * scale - center;
                float depth = dot(delta, right);
                float reach = max(_WindowDirections[wallEdge].z * aspect, 0.001);
                if (depth <= 0.0 || depth >= reach) return 0.0;
                float halfHeight = height * 0.5;
                float wallAngle = _WaterScatterAngle * (_WindowLightStyle == 7 ? 17.0 / 22.0 : 1.0);
                float slope = tan(radians(clamp(wallAngle, 0.0, 80.0) * 0.5));
                float halfWidth = halfHeight + depth * slope;
                float lateral = abs(dot(delta, up));
                float feather = max(abs(_BlitTexture_TexelSize.y) * 2.0, 0.002);
                float envelope = (1.0 - smoothstep(halfWidth * 0.88, halfWidth, lateral))
                    * smoothstep(0.0, feather * 3.0, depth);
                if (envelope <= 0.001) return 0.0;
                float fade = WaterDistanceOpacity(depth / reach);
                float strength = _WindowDirections[wallEdge].w;
                float3 beams = float3(0.0, 0.0, strength * 0.3 * envelope * fade);
                float waveTime = _MoodTime * max(_WaterWaveSpeed, 0.0);
                int count = clamp(_WaterBeamCount, 8, 48);
                [loop]
                for (int i = 0; i < count; i++)
                {
                    float seed = Rand(float2(i + 37.0, 91.0));
                    float detail = Rand(float2(i + 71.0, 19.0));
                    float slot = ((float)i + lerp(0.2, 0.8, seed)) / (float)count;
                    float2 origin = lerp(bottom, top, slot);
                    float lane = dot(origin - center, up) / halfHeight;
                    float2 axis = normalize(right + up * lane * slope);
                    float2 sideways = float2(-axis.y, axis.x);
                    float2 relative = uv * scale - origin;
                    float along = dot(relative, axis);
                    if (along <= 0.0) continue;
                    float progress = along * dot(axis, right) / reach;
                    float rayFade = WaterDistanceOpacity(progress);
                    float bend = sin(progress * 2.0 + waveTime * 0.45 + seed * 6.28318)
                        * 0.004 * clamp(_WaterWaveStrength, 0.0, 2.0) * progress * progress;
                    float distance = abs(dot(relative, sideways) - bend);
                    float variation = i % 5 == 0 ? 1.5 : lerp(0.3, 0.8, detail);
                    float wallWidth = _WindowLightStyle == 7 ? _WindowStyles[wallEdge].x : _WindowBandWidth;
                    float width = max(wallWidth * lerp(1.0, variation, _WaterWidthVariation)
                        * clamp(_WaterBeamWidth, 0.1, 5.0) * (1.0 + progress * 0.3), feather);
                    float wallSoftness = _WindowLightStyle == 7 ? 1.0 : _WindowBandSoftness;
                    float softness = lerp(0.2, 1.0, saturate(wallSoftness));
                    float core = 1.0 - smoothstep(width * 0.15, width * (0.4 + softness), distance);
                    float halo = exp2(-pow(distance / max(width * 2.5, feather), 2.0) * 2.0);
                    float shimmer = 1.0 + 0.12 * clamp(_WaterWaveStrength, 0.0, 2.0)
                        * sin(waveTime * lerp(0.6, 1.3, detail) + seed * 6.28318);
                    float energy = strength * lerp(0.4, 0.75, seed)
                        * pow(24.0 / (float)count, 0.65) * rayFade * shimmer * envelope;
                    beams.x += core * energy * 0.35;
                    beams.y += halo * energy * 0.5;
                }
                return beams;
            }

            float3 WindowBeams(float2 uv)
            {
                float3 beams = 0.0;
                if (_WindowEdgeCount <= 0 || _WaterLightStrength <= 0.001) return beams;
                [branch]
                if (_WindowLightStyle == 5) return ClockWindowShafts(uv);
                [branch]
                if (_WindowLightStyle == 6) return LeftWallShafts(uv);
                [branch]
                if (_WindowLightStyle == 7)
                {
                    // 단독 시계 버전의 낮은 불투명도에 맞추고, 두 빛이 겹치는 곳만 다시 올려.
                    float3 clockBeams = ClockWindowShafts(uv) * 0.56;
                    float3 wallBeams = LeftWallShafts(uv);
                    return clockBeams + wallBeams + min(clockBeams, wallBeams) * 0.45;
                }
                [branch]
                if (_WindowLightStyle == 4) return beams;
                [branch]
                if (_WindowLightStyle == 1) return SoftWindowBands(uv);
                [branch]
                if (_WindowLightStyle == 2) return RoomWindowShafts(uv, false);
                [branch]
                if (_WindowLightStyle == 3) return RoomWindowShafts(uv, true);
                float aspect = abs(_BlitTexture_TexelSize.y / _BlitTexture_TexelSize.x);
                float2 scale = float2(aspect, 1.0);
                float waveTime = _MoodTime * max(_WaterWaveSpeed, 0.0);
                int count = clamp(_WaterBeamCount, 8, 48);
                [loop]
                for (int edgeIndex = 0; edgeIndex < min(_WindowEdgeCount, 8); edgeIndex++)
                {
                    float4 edge = _WindowEdges[edgeIndex];
                    float4 settings = _WindowDirections[edgeIndex];
                    float width = max(_WindowStyles[edgeIndex].x, 0.001);
                    float2 a = edge.xy * scale;
                    float2 b = edge.zw * scale;
                    float2 pixel = uv * scale;
                    float reach = settings.z * 1.25 + width * 4.0;
                    if (any(pixel < min(a, b) - reach) || any(pixel > max(a, b) + reach)) continue;
                    float2 outward = settings.xy;
                    // 심·주변 산란·잔광을 모두 창틀의 바깥 반평면에 제한해. 안쪽을 가로지르는 빛은 없어.
                    float outwardDepth = dot(pixel - a, outward);
                    if (outwardDepth <= 0.0) continue;
                    float edgeFade = smoothstep(0.0, max(width * 0.15, abs(_BlitTexture_TexelSize.y)), outwardDepth);
                    float2 tangent = normalize(b - a);
                    float density = pow(24.0 / (float)count, 0.65);
                    [loop]
                    for (int i = 0; i < count; i++)
                    {
                        float seed = Rand(float2(i + 13.0, edgeIndex + 41.0));
                        float detail = Rand(float2(i + 31.0, edgeIndex + 9.0));
                        // 출발점은 고정하되 모서리 가까이에 더 배치해서 대각선 구역을 메워.
                        float slot = ((float)i + lerp(0.08, 0.92, seed)) / (float)count;
                        float along = 0.5 - 0.5 * cos(slot * 3.14159265);
                        float2 origin = lerp(a, b, along);
                        // 중앙은 바깥 법선, 모서리는 약 55도까지 펼쳐 이웃 변의 빛과 대각선에서 이어져.
                        // 기본 방향 주위에만 작은 편차와 느린 흔들림을 줘. 안쪽으로 뒤집히지는 않아.
                        float lane = along * 2.0 - 1.0;
                        float angle = lane * abs(lane) * 0.959931
                            + (detail - 0.5) * 0.12
                            + sin(waveTime * 0.18 + seed * 6.28318)
                                * 0.025 * clamp(_WaterWaveStrength, 0.0, 2.0);
                        float2 beamAxis = outward * cos(angle) + tangent * sin(angle);
                        float2 beamAcross = float2(-beamAxis.y, beamAxis.x);
                        float2 relative = pixel - origin;
                        float depth = dot(relative, beamAxis);
                        float beamLength = settings.z * lerp(0.52, 1.0, seed);
                        if (depth <= 0.0 || depth > beamLength * 1.25) continue;
                        float progress = depth / beamLength;
                        // 곧은 빗살 느낌만 누그러뜨리는 얕은 휨. 시작점과 바깥 반평면은 유지해.
                        float bend = sin(progress * 2.2 + detail * 6.28318 + waveTime * 0.12)
                            * settings.z * 0.012 * progress * progress;
                        float signedLateral = dot(relative, beamAcross) - bend;
                        float lateral = abs(signedLateral);
                        float widthClass = i % 5 == 0 ? lerp(1.6, 2.5, detail) : lerp(0.25, 0.85, detail);
                        float baseWidth = max(width * 0.18 * lerp(1.0, widthClass, _WaterWidthVariation)
                            * sqrt(12.0 / max(_WaterLightSharpness, 2.0)), 0.0005);
                        baseWidth *= 1.0 + saturate(progress) * 0.55;
                        float coreWidth = baseWidth * 0.35 * clamp(_WaterBeamWidth, 0.1, 5.0);
                        float feather = max(baseWidth * 0.25, abs(_BlitTexture_TexelSize.y) * 0.75);
                        float core = 1.0 - smoothstep(coreWidth, coreWidth + feather, lateral);
                        float shimmer = 1.0 + 0.08 * clamp(_WaterWaveStrength, 0.0, 2.0)
                            * sin(waveTime * lerp(0.65, 1.5, detail) * 0.6 + seed * 6.28318);
                        float energy = settings.w * lerp(0.55, 1.0, detail) * density * shimmer * edgeFade * 0.5;
                        float fade = WaterDistanceOpacity(progress);
                        beams.x += core * energy * fade;
                        float halo = lateral / max(baseWidth * 3.2, 0.001);
                        beams.y += exp2(-halo * halo * 3.0) * energy * fade;
                        float tail = lateral / max(width * 0.7 + max(depth, 0.0) * 0.04, 0.001);
                        beams.z += exp2(-tail * tail * 2.0) * energy * 0.35
                            * WaterDistanceOpacity(progress / 1.25);
                    }
                }
                return beams;
            }

            // UI는 산란광 합성에서 제외해.
            float3 Underwater(float2 uv, float3 col)
            {
                float uiAlpha = 0.0;
                [branch]
                if (_MoodOnly < 0.5)
                    uiAlpha = SAMPLE_TEXTURE2D(_UITex, sampler_UITex, uv).a;
                float2 texel = abs(_BlitTexture_TexelSize.xy);
                float3 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float3 neighbors = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, 0)).rgb
                    + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texel.x, 0)).rgb
                    + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, texel.y)).rgb
                    + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, texel.y)).rgb;
                // 한 픽셀 윤곽만 강조하고 오버슈트를 제한한다. UI에는 샤프닝/강한 대비를 적용하지 않는다.
                float edge = clamp(dot(scene - neighbors * 0.25, float3(0.2126, 0.7152, 0.0722)), -0.12, 0.12);
                float3 crisp = max(col + edge * _DepressedSharpness * (1.0 - uiAlpha), 0.0);
                crisp = max((crisp - 0.18) * lerp(1.0, _DepressedContrast, 1.0 - uiAlpha) + 0.18, 0.0);
                float luminance = dot(crisp, float3(0.2126, 0.7152, 0.0722));
                float whiteLight = smoothstep(0.55, 1.15, luminance);
                float3 blue = luminance * _DepressedTint.rgb * 1.55;
                float3 tinted = lerp(crisp, blue, _BlueTintStrength * (1.0 - whiteLight));
                tinted = lerp(tinted, luminance.xxx, whiteLight * 0.55);
                float3 beams = WaterBeams(uv) + WindowBeams(uv);
                // 흰 중심은 선명하게, 청록 외곽은 넓고 옅게. 균일한 삼각형 면은 만들지 않는다.
                float core = 1.0 - exp2(-beams.x * _WaterLightStrength * 3.5);
                float haze = 0.4 * (1.0 - exp2(-beams.y * _WaterLightStrength * _WaterScatterStrength * 2.0));
                float afterglow = 0.35 * (1.0 - exp2(-beams.z * _WaterLightStrength * _WaterAfterglowStrength * 2.0));
                float3 scattering = float3(0.84, 0.96, 1.0) * core
                    + float3(0.24, 0.64, 0.78) * haze
                    + float3(0.3, 0.7, 0.82) * afterglow;
                [branch]
                if (_WindowLightStyle == 1 || _WindowLightStyle == 2 || _WindowLightStyle == 3 || _WindowLightStyle == 4 || _WindowLightStyle == 5 || _WindowLightStyle == 6 || _WindowLightStyle == 7)
                    scattering = _WindowBandTint.rgb * (core * clamp(_WindowBandCoreBrightness, 1.0, 5.0)
                        + haze * 0.85 + afterglow * 0.7)
                        * saturate(_WindowBandOpacity);
                [branch]
                if (_WindowLightStyle == 2 || _WindowLightStyle == 3 || _WindowLightStyle == 4)
                    scattering += _WindowBandTint.rgb * RoomWindowAmbient(uv)
                        * saturate(_WindowBandOpacity);
                tinted += scattering * (1.0 - uiAlpha);
                return lerp(col, tinted, _DepressedAmount);
            }

            float3 Sample3(float2 c)
            {
                // 흥분 중 기본 CRT 색수차도 제거해 진입 연출 종료 후 분리된 테두리가 남지 않게 한다.
                float2 off = (c - 0.5) * _Aberration * (1.0 - _ExcitedBlend);
                float r = SampleComposited(c + off).r;
                float g = SampleComposited(c).g;
                float b = SampleComposited(c - off).b;
                return float3(r, g, b);
            }

            float3 MoodDisplayColor(float3 col)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return max(col, 0.0);
                #else
                    return LinearToSRGB(max(col, 0.0));
                #endif
            }

            float MoodLuma(float3 col)
            {
                return dot(MoodDisplayColor(col), float3(0.2126, 0.7152, 0.0722));
            }

            float3 PastelExcitement(float2 uv, float3 col)
            {
                // 노션 첫 이미지: 살짝 바랜 청회색 바탕과 밀키한 밝은 면.
                // 선형 공간에서 흰색을 더하면 쉽게 과노출되므로 디스플레이 공간에서 채도/대비 조절.
                float3 display = MoodDisplayColor(col);
                float lum = dot(display, float3(0.2126, 0.7152, 0.0722));
                float3 graded = lerp(lum.xxx, display, _PastelSaturation);
                graded = (graded - 0.5) * _PastelContrast + 0.5;
                graded = lerp(graded, lum * float3(0.92, 0.98, 1.04), 0.12);
                graded = lerp(graded, float3(0.94, 0.96, 0.98),
                    smoothstep(0.45, 1.0, lum) * 0.14);
                display = lerp(display, graded, _PastelGradeStrength * _ExcitedAmount);

                // 색수차가 잦아들면 원래 명암을 살린 핑크 단색 톤으로 정착한다.
                float pinkLuma = saturate((lum - 0.5) * _PastelContrast + 0.5);
                display = lerp(display, pinkLuma * _PastelPink.rgb,
                    (1.0 - _ChromaticBurst) * _ExcitedBlend);

                [branch]
                if (_ChromaticBurst > 0.0)
                {
                    float2 shift = float2(_PastelSeparation, _PastelSeparation * 0.14
                        * sin(_MoodTime * 0.8 + uv.y * 5.0)) * _ExcitedAmount * _ChromaticBurst;
                    float center = MoodLuma(SampleComposited(uv));
                    // 두 위치의 실루엣을 겹쳐 부드러운 잔상을 만든다. 흰 영역 안에서는 색 추가를 억제.
                    float pink = 0.6 * MoodLuma(SampleComposited(saturate(uv + shift)))
                        + 0.4 * MoodLuma(SampleComposited(saturate(uv + shift * 0.65)));
                    float mint = 0.6 * MoodLuma(SampleComposited(saturate(uv - shift)))
                        + 0.4 * MoodLuma(SampleComposited(saturate(uv - shift * 0.65)));
                    float lavender = MoodLuma(SampleComposited(saturate(uv + shift * 1.55)));
                    float3 coverage = smoothstep(0.015, 0.28,
                        max(float3(pink, mint, lavender) - center, 0.0));
                    coverage.z *= 0.45;
                    float total = coverage.x + coverage.y + coverage.z;
                    float3 palette = (_PastelPink.rgb * coverage.x + _PastelMint.rgb * coverage.y
                        + _PastelLavender.rgb * coverage.z) / max(total, 0.0001);
                    // 덧셈 대신 실루엣 비율로 혼합해 잔상이 겹쳐도 흰색으로 날아가지 않게 한다.
                    display = lerp(display, palette, saturate(total) * _PastelStrength * _ExcitedAmount * _ChromaticBurst);
                }
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return display;
                #else
                    return SRGBToLinear(max(display, 0.0));
                #endif
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 res = _BlitTexture_TexelSize.zw;
                float time = _Time.y;
                float2 c = input.texcoord;

                // 진입 후 2초만 활성화되는 가로 슬라이스 글리치. 진입 색수차와 별도 제어.
                float frame = floor(_MoodTime * 13.0);
                float slice = floor(c.y * 48.0);
                float selected = step(0.86, Rand(float2(slice, frame)));
                c.x += (Rand(float2(frame + 11.0, slice)) * 2.0 - 1.0)
                    * selected * _GlitchAmount * _GlitchDisplacement;
                c.x += sin(c.y * 9.0 + _MoodTime * 1.4) * 0.0012 * _ExcitedAmount * _ChromaticBurst;

                // 원본 Pass Material이 없는 작업 모드: UI와 CRT 필터 없이 방 연출만 적용해.
                [branch]
                if (_MoodOnly > 0.5)
                {
                    float3 mood = SampleComposited(c);
                    if (_ExcitedAmount > 0.001) mood = PastelExcitement(c, mood);
                    if (_DepressedAmount > 0.001) mood = Underwater(c, mood);
                    return float4(mood, 1.0);
                }

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
                // 침체에서 둥근 블룸을 줄이고 아래의 가는 수직 광선을 우선한다.
                col += acc / 25.0 * _Bloom * 5.5 * lerp(1.0, 0.2, _DepressedAmount);

                [branch]
                if (_ExcitedAmount > 0.001) col = PastelExcitement(c, col);

                [branch]
                if (_DepressedAmount > 0.001) col = Underwater(c, col);

                // 스캔라인 (두께: sin파를 지수로 눌러서 어두운 대역의 폭을 넓힘)
                float sl = sin((c.y + time * _ScanSpeed * 0.05) * _ScanCount * 3.14159);
                float scanWave = pow(saturate(0.5 + 0.5 * sl), 1.0 / max(_ScanThickness, 0.05));
                // 흥분 중 강한 검은 줄/입자가 연한 파스텔을 탁하게 만들지 않도록 완화.
                col *= 1.0 - _ScanIntensity * lerp(1.0, 0.55, _ExcitedAmount) * scanWave;

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
                col += (Rand(c * res + time) - 0.5) * _Noise * lerp(1.0, 0.5, _ExcitedAmount);

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
