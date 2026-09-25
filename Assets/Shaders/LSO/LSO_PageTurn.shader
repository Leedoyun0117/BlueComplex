// 단서 정보 책의 오른쪽 페이지가 책등을 축으로 넘어가는 연출.
//
// 넘어가기 직전의 페이지를 한 장의 그림(_MainTex — LSO_PageTurnEffect가 RT_UI에서 잘라온 스냅샷)으로 떠서,
// 그 한 장만 여기서 접어 넘긴다. uGUI는 자식 Graphic이 부모의 정점 변형을 물려받지 않아서, 페이지 배경에
// 셰이더를 걸어봐야 위에 얹힌 글자는 평평하게 남는다 — 그래서 통째로 그림으로 떠서 쓴다.
//
// 쿼드는 오른쪽 페이지에 정확히 겹친다. u=0이 책등(회전축), u=1이 바깥쪽 모서리다.
// 페이지는 0도에서 90도까지만 돈다 — 90도를 넘으면 책등 왼쪽으로 넘어가 이 쿼드 밖이라 잘리기 때문이다.
// 대신 90도에 가까워질수록 서서히 지워서, 젖혀진 장이 사라지고 그 밑의 새 페이지가 드러나는 것으로 읽히게 한다.
//
// 뼈대(프로퍼티·렌더 상태·include)는 유니티 기본 UI-Default.shader를 그대로 따른다. 이 프로젝트 UI가
// 전부 그 셰이더로 그려지고 있으니, 거기서 벗어나지 않아야 파이프라인(URP)에서 확실히 지원된다.
Shader "LSO/PageTurn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Page Snapshot", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Progress ("Progress", Range(0,1)) = 0
        _Bow ("Bow", Range(0,0.5)) = 0.08
        _Shade ("Shade Strength", Range(0,1)) = 0.45
        _FadeOut ("Fade Out Portion", Range(0.01,1)) = 0.3

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _Progress;
            float _Bow;
            float _Shade;
            float _FadeOut;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 책등(u=0)을 축으로 0도 → 90도. 화면에서 보이는 가로 폭이 cos(theta)만큼 줄어든다.
                float theta = _Progress * 1.5707963; // pi/2
                float c = cos(theta);

                // 거의 옆면이면 두께가 없어 그릴 게 없다(0으로 나누는 것도 막는다).
                clip(c - 0.004);

                // 화면 위치 → 종이 위 위치. 종이 밖이면 이 픽셀은 넘어가는 장이 아니다.
                float t = IN.texcoord.x / c;
                clip(1.0 - t);

                // 종이가 활처럼 휘는 정도 — 중간에 가장 크고, 책등과 바깥 모서리에서는 0이다.
                float bow = _Bow * sin(theta) * t * (1.0 - t);
                float pageV = 0.5 + (IN.texcoord.y - 0.5) / (1.0 + bow);
                clip(min(pageV, 1.0 - pageV));

                // 들릴수록, 그리고 책등에서 멀수록 어두워진다.
                float shade = 1.0 - _Shade * sin(theta) * (0.35 + 0.65 * t);

                half4 color = (tex2D(_MainTex, float2(t, pageV)) + _TextureSampleAdd) * IN.color;
                color.rgb *= shade;

                // 다 젖혀진 장이 툭 사라지지 않게 마지막 구간에서 서서히 지운다.
                color.a *= saturate((1.0 - _Progress) / _FadeOut);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                // Blend One OneMinusSrcAlpha — 기본 UI 셰이더와 같은 프리멀티플라이드 합성.
                color.rgb *= color.a;
                return color;
            }
        ENDCG
        }
    }

    Fallback "UI/Default"
}
