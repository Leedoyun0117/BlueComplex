Shader "BlueComplex/UI/PaperPanel"
{
    // 손으로 그은 테두리만 있는 백지 패널 — 대사창, 단서 패널/카드, 아이템 패널/칸, 키 카드. 안쪽은 색·해칭·얼룩·도트 없이 완전히 깨끗한 종이색이고,
    // 윤곽선만 다르다: 둥근 사각형(살짝 둥근 모서리)의 선 위치를 낮은 주파수 노이즈로 흔들고 두께도 들쭉날쭉하게 하며,
    // 두 번째 가는 선을 살짝 어긋나게 겹쳐 그려 "겹쳐 그은" 모서리·손떨림 느낌을 낸다. 격자·계단·양자화 없이 연속된 선이다.
    // 선 바깥은 투명이라 종이의 실루엣이 곧 그 흔들리는 선이다(그림자 복제본도 같은 실루엣을 따른다).
    //
    // 패널 크기·시드는 PaperPanel(BaseMeshEffect)이 정점 UV0에 실어 준다(uv0.xy = 0..1 좌표, uv0.zw = px 크기 + 시드 소수부).
    // 머티리얼의 _BlockSize = 1 로 두어 셀 수 = 캔버스 px 크기가 되게 한다. PaperPanel 없이 쓰면 단색 UI 셰이더로 동작한다.
    // 색이 종이(밝고 무채색)가 아닌 정점(그림자·강조)은 선을 그리지 않고 그 색 그대로 실루엣만 따른다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (unused)", 2D) = "white" {}
        _Color ("Tint (paper)", Color) = (1, 1, 1, 1)
        _BlockSize ("Grid Cell (canvas px) - keep 1", Range(1, 16)) = 1

        [Header(Line)]
        _InkColor ("Ink", Color) = (0.04, 0.04, 0.05, 1)
        _LineWidth ("Line Half Width (px)", Range(0.3, 3)) = 0.8
        _Inset ("Inset From Quad (px)", Range(1, 8)) = 3.5
        _CornerRadius ("Corner Radius (px)", Range(0, 16)) = 5

        [Header(Wobble)]
        _WobbleAmp ("Position Wobble (px)", Range(0, 3)) = 1.0
        _WobbleScale ("Wobble Wavelength (px)", Range(8, 120)) = 34
        _DoubleStroke ("Second Stroke Strength", Range(0, 1)) = 0.75

        [Header(Dither shading)]
        // 안쪽 면의 명암을 베이어 디더링(순서 디더) 도트로 표현한다. 색은 그대로 두고 밝기만 단계(0/1/2)로 나눠 종이색과 그보다 살짝 어두운 톤 사이를 오간다.
        // 0이면 꺼짐(완전한 백지). 격자는 패널 왼쪽 위 기준 캔버스 px.
        _DitherDepth ("Dither Darkest Step (fraction darker)", Range(0, 0.3)) = 0
        _DitherCell ("Dither Cell (px)", Range(1, 12)) = 4
        _DitherMatrix ("Bayer Matrix Size (4 or 8)", Range(4, 8)) = 4
        _DitherAmount ("Shaded Area Amount", Range(0, 1)) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "PaperPanel"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float4 _ClipRect;

            fixed4 _InkColor;
            float _LineWidth;
            float _Inset;
            float _CornerRadius;
            float _WobbleAmp;
            float _WobbleScale;
            float _DoubleStroke;
            float _DitherDepth;
            float _DitherCell;
            float _DitherMatrix;
            float _DitherAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv0 = v.uv0;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float H(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 2D 값 노이즈(0..1, 부드럽게 보간 → 연속된 흔들림).
            float VN(float2 p, float s)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float2 o = float2(s, s * 0.37);
                return lerp(lerp(H(i + o), H(i + float2(1.0, 0.0) + o), u.x),
                            lerp(H(i + float2(0.0, 1.0) + o), H(i + float2(1.0, 1.0) + o), u.x), u.y);
            }

            // 둥근 사각형 부호 거리(안쪽 음수). b = 반 크기, r = 모서리 반지름.
            float RoundBox(float2 q, float2 b, float r)
            {
                float2 d = abs(q) - b + r;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;
            }

            // 베이어 순서 디더 임계값(0..1). 4x4 또는 8x8.
            float Bayer(float2 cell, float size)
            {
                int x = (int)fmod(cell.x, size);
                int y = (int)fmod(cell.y, size);
                if (size < 6.0)
                {
                    const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                    return (m[y * 4 + x] + 0.5) / 16.0;
                }

                int xy = x ^ y;
                int v = ((xy & 1) << 5) | ((x & 1) << 4) | ((xy & 2) << 2) | ((x & 2) << 1) | ((xy & 4) >> 1) | ((x & 4) >> 2);
                return (v + 0.5) / 64.0;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = IN.color;

                float luma = dot(color.rgb, float3(0.3333, 0.3333, 0.3333));
                float chroma = max(color.r, max(color.g, color.b)) - min(color.r, min(color.g, color.b));
                bool isPaper = luma > 0.7 && chroma < 0.15;

                float2 size = floor(IN.uv0.zw);
                if (size.x >= 1.0 && size.y >= 1.0)
                {
                    float seed = round(frac(IN.uv0.z) * 1000.0) + round(frac(IN.uv0.w) * 1000.0) * 0.013;
                    float2 p = IN.uv0.xy * size;
                    float2 q = p - size * 0.5;
                    float2 hb = size * 0.5 - _Inset;

                    // 주 선: 낮은 주파수 노이즈(느린 굽이) + 아주 작은 손떨림으로 선의 위치를 흔든다. 두께도 위치에 따라 달라진다.
                    float wobbleA = (VN(p / _WobbleScale, seed) - 0.5) * 2.0 * _WobbleAmp + (VN(p / 9.0, seed + 3.0) - 0.5) * 0.7;
                    float sdA = RoundBox(q, hb, _CornerRadius) + wobbleA;
                    float widthA = _LineWidth * (0.75 + 0.6 * VN(p / 17.0, seed + 7.0));

                    // 겹쳐 그은 두 번째 선: 살짝 안쪽·어긋난 위치, 더 가늘고, 모서리 반지름이 달라 모서리에서 두 선이 엇갈린다.
                    float2 shift = (float2(H(float2(seed, 1.3)), H(float2(seed, 2.9))) - 0.5) * 2.4;
                    float wobbleB = (VN(p / (_WobbleScale * 0.8) + 13.7, seed + 11.0) - 0.5) * 2.0 * _WobbleAmp + (VN(p / 7.0, seed + 5.0) - 0.5) * 0.6;
                    float sdB = RoundBox(q + shift, hb - 1.4, _CornerRadius * 0.55) + wobbleB;
                    float widthB = _LineWidth * 0.6 * (0.6 + 0.8 * VN(p / 13.0, seed + 19.0));

                    float aa = max(fwidth(sdA), 0.6);
                    float inkA = 1.0 - smoothstep(widthA - aa * 0.5, widthA + aa * 0.5, abs(sdA));
                    float inkB = (1.0 - smoothstep(widthB - aa * 0.5, widthB + aa * 0.5, abs(sdB))) * _DoubleStroke;

                    // 실루엣: 주 선 안쪽(선 포함) 또는 두 번째 선.
                    float cover = max(1.0 - smoothstep(widthA - aa * 0.5, widthA + aa * 0.5, sdA), inkB);

                    // 안쪽 면 명암: 오른쪽 아래·가장자리 안쪽·저주파 얼룩진 곳일수록 어둡다고 보고(0..1), 그 값을 3단계로 나눠 베이어 도트로 섞는다.
                    // 대부분은 0단계(종이색 그대로)라 면이 지저분해지지 않고, 그늘진 곳에만 도트가 생긴다.
                    if (isPaper && _DitherDepth > 0.0)
                    {
                        float diagonal = saturate(IN.uv0.x * 0.45 + (1.0 - IN.uv0.y) * 0.55);
                        float blotch = VN(p / 48.0, seed + 21.0);
                        float rim = 1.0 - saturate(-sdA / 16.0);
                        float shade = saturate((diagonal * 0.55 + blotch * 0.45 + rim * 0.35 - 0.28) * 1.5) * _DitherAmount;
                        float step2 = floor(shade * 2.0 + Bayer(floor(p / _DitherCell), _DitherMatrix));
                        color.rgb *= 1.0 - _DitherDepth * saturate(step2) * 0.5 * (step2 > 1.5 ? 2.0 : 1.0);
                    }

                    if (isPaper) color.rgb = lerp(color.rgb, _InkColor.rgb, max(inkA, inkB));
                    color.a *= cover;
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
