// DLJ: standalone mood variant; keeps the existing single-pass scene/UI composition.
Shader "BlueComplex/DLJ/HeartbeatMood"
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

            // 수중 번짐은 배경 하이라이트만 샘플링한다. 글자가 광원처럼 번지지 않게 UI는 제외.
            float3 Underwater(float2 uv, float3 col)
            {
                float uiAlpha = SAMPLE_TEXTURE2D(_UITex, sampler_UITex, uv).a;
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
                float3 beams = WaterBeams(uv);
                // 흰 중심은 선명하게, 청록 외곽은 넓고 옅게. 균일한 삼각형 면은 만들지 않는다.
                float core = 1.0 - exp2(-beams.x * _WaterLightStrength * 3.5);
                float haze = 0.4 * (1.0 - exp2(-beams.y * _WaterLightStrength * _WaterScatterStrength * 2.0));
                float afterglow = 0.35 * (1.0 - exp2(-beams.z * _WaterLightStrength * _WaterAfterglowStrength * 2.0));
                tinted += (float3(0.84, 0.96, 1.0) * core
                    + float3(0.24, 0.64, 0.78) * haze
                    + float3(0.3, 0.7, 0.82) * afterglow) * (1.0 - uiAlpha);
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
