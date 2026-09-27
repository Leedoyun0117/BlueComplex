Shader "BlueComplex/LSO/ScreenSilhouette"
{
    // 떠 온 UI 그림에서 "형태"만 남겨 단색으로 칠한다. 설정창 뒤에 가려진 UI의 실루엣을 비추는 데 쓴다.
    //
    // 색도 무늬도 버리고 알파만 본다. UI 카메라가 RT_UI를 (0,0,0,0)으로 비우고 UI만 알파를 쓰므로,
    // RT_UI의 알파 채널이 그대로 "여기에 UI가 있다"는 마스크다 — 따로 만들 필요가 없다.
    //
    // 뼈대(프로퍼티·렌더 상태·include)는 유니티 기본 UI-Default.shader를 따른다. 이 프로젝트 UI가
    // 전부 그 셰이더로 그려지니 거기서 벗어나지 않아야 파이프라인(URP)에서 확실히 지원된다.
    Properties
    {
        [PerRendererData] _MainTex ("UI Snapshot", 2D) = "black" {}
        _Color ("Silhouette Color", Color) = (1,1,1,0.18)

        _Threshold ("Alpha Threshold", Range(0, 1)) = 0.08
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.12

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

            float _Threshold;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                // 정점 색에 CanvasGroup 알파가 실려 온다 — 창이 내려오며 실루엣도 같이 번져 들어온다.
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 알파만 본다. 색·무늬를 버려야 "형태"만 남는다.
                half mask = tex2D(_MainTex, IN.texcoord).a;
                mask = smoothstep(_Threshold, _Threshold + _Softness, mask);

                half4 color = _Color;
                color.a *= mask;
                color *= IN.color;

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
